using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameTheory.Application;
using FrameTheory.Desktop;
using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

[Xunit.CollectionDefinition("Avalonia headless", DisableParallelization = true)]
public sealed class AvaloniaHeadlessCollectionDefinition
{
}

[Xunit.Collection("Avalonia headless")]
public sealed class DesktopImportTests
{
    [Xunit.Fact]
    public async Task CorruptMediaIsReportedInTheImportStatusInsteadOfEscapingTheUi()
    {
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new ThrowingImporter());

        Xunit.Assert.Equal("No assets", viewModel.ImportedMediaSummary);
        await viewModel.ImportFilesAsync(["corrupt.png"], CancellationToken.None);

        Xunit.Assert.Contains("Failed", viewModel.MediaImportStatusText);
        Xunit.Assert.Contains("corrupt input", viewModel.MediaImportStatusText);
    }

    [Xunit.Fact]
    public async Task ImportingInBatchesKeepsEarlierMediaVisibleWithoutDuplicatingIt()
    {
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new BatchImporter());

        await viewModel.ImportFilesAsync(["first.mp4"], CancellationToken.None);
        await viewModel.ImportFilesAsync(["second.mp4", "first.mp4"], CancellationToken.None);

        Xunit.Assert.Equal(2, viewModel.ImportedMedia.Count);
        Xunit.Assert.Equal("2 assets", viewModel.ImportedMediaSummary);
        Xunit.Assert.All(viewModel.ImportedMedia, media => Xunit.Assert.Equal(MediaKind.Video, media.Kind));
        Xunit.Assert.Equal(2, viewModel.TimelineEditor.TrackRows.Single(track => track.Kind == TimelineTrackKind.Video).Clips.Count);
        Xunit.Assert.Contains("2 file(s) available", viewModel.MediaImportStatusText, StringComparison.Ordinal);
    }

    [Xunit.Fact]
    public async Task SelectingImportedMediaSelectsItsTimelineClip()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApplication));
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new BatchImporter());
        await session.Dispatch(() =>
        {
            viewModel.ImportFilesAsync(["clip.mp4"], CancellationToken.None).GetAwaiter().GetResult();
            var window = new MainWindow(viewModel);
            try
            {
                window.Show();
                var mediaList = window.GetVisualDescendants()
                    .OfType<ListBox>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Imported media");
                var clipButton = window.GetVisualDescendants()
                    .OfType<Button>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control)?.StartsWith("Select Video clip", StringComparison.Ordinal) == true);
                var mediaCard = window.GetVisualDescendants()
                    .OfType<Border>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Imported media card: clip.mp4");
                var videoLane = window.GetVisualDescendants()
                    .OfType<Border>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Video timeline lane");
                var videoAccent = videoLane.GetVisualDescendants()
                    .OfType<Border>()
                    .Single(control => control.Width == 4);
                Xunit.Assert.Contains("frames 0–29", ToolTip.GetTip(clipButton) as string, StringComparison.Ordinal);
                Xunit.Assert.Contains("clip.mp4", clipButton.GetVisualDescendants().OfType<TextBlock>().Single().Text, StringComparison.Ordinal);
                Xunit.Assert.Contains("Video · clip.mp4", ToolTip.GetTip(mediaCard) as string, StringComparison.Ordinal);
                Xunit.Assert.Equal(124, mediaCard.Width);
                Xunit.Assert.Equal(110, mediaCard.GetVisualDescendants().OfType<Image>().Single().Width);
                Xunit.Assert.Contains("Video track Video 1", ToolTip.GetTip(videoLane) as string, StringComparison.Ordinal);
                Xunit.Assert.Equal(
                    Avalonia.Media.Color.FromRgb(95, 125, 255),
                    Xunit.Assert.IsType<Avalonia.Media.SolidColorBrush>(videoAccent.Background).Color);
                Xunit.Assert.Equal(128, clipButton.MinWidth);
                Xunit.Assert.Equal(HorizontalAlignment.Left, clipButton.HorizontalContentAlignment);
                mediaList.SelectedIndex = 0;
                Dispatcher.UIThread.RunJobs();

                Xunit.Assert.Contains("Selected Video clip", viewModel.TimelineEditor.SelectionContextText, StringComparison.Ordinal);
                Xunit.Assert.Equal(
                    Avalonia.Media.Color.FromRgb(49, 67, 111),
                    Xunit.Assert.IsType<Avalonia.Media.SolidColorBrush>(mediaCard.Background).Color);
                Xunit.Assert.Equal(
                    Avalonia.Media.Color.FromRgb(95, 125, 255),
                    Xunit.Assert.IsType<Avalonia.Media.SolidColorBrush>(mediaCard.BorderBrush).Color);
                var selectedClipButton = window.GetVisualDescendants()
                    .OfType<Button>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control)?.StartsWith("Select Video clip", StringComparison.Ordinal) == true);
                Xunit.Assert.Contains("(selected)", Avalonia.Automation.AutomationProperties.GetName(selectedClipButton), StringComparison.Ordinal);
                Xunit.Assert.Contains("Selected.", ToolTip.GetTip(selectedClipButton) as string, StringComparison.Ordinal);

                var secondWindowClipButton = window.GetVisualDescendants()
                    .OfType<Button>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control)?.StartsWith("Select Video clip", StringComparison.Ordinal) == true);
                secondWindowClipButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal("clip.mp4", Xunit.Assert.IsType<ImportedMediaViewModel>(mediaList.SelectedItem).Name);
                Xunit.Assert.Equal(
                    Avalonia.Media.Color.FromRgb(49, 67, 111),
                    Xunit.Assert.IsType<Avalonia.Media.SolidColorBrush>(mediaCard.Background).Color);
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);
    }

    [Xunit.Fact]
    public async Task TimelineLanePositionsClipsByFrameStart()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApplication));
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new BatchImporter());
        await session.Dispatch(() =>
        {
            viewModel.ImportFilesAsync(["first.mp4", "second.mp4"], CancellationToken.None).GetAwaiter().GetResult();
            var window = new MainWindow(viewModel);
            try
            {
                window.Width = 960;
                window.Height = 640;
                window.Show();
                Xunit.Assert.True(window.GetVisualDescendants()
                    .OfType<Button>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Play timeline preview")
                    .IsEnabled);
                var canvas = window.GetVisualDescendants()
                    .OfType<Canvas>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Video timeline clip canvas");
                var clips = canvas.Children.OfType<Button>().ToArray();

                Xunit.Assert.Equal(2, clips.Length);
                Xunit.Assert.Equal(
                    "Video 1",
                    window.GetVisualDescendants()
                        .OfType<TextBlock>()
                        .Single(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Video timeline track label")
                        .Text);
                Xunit.Assert.Equal(0, Canvas.GetLeft(clips[0]));
                Xunit.Assert.Equal(30, Canvas.GetLeft(clips[1]));
                Xunit.Assert.All(clips, clip => Xunit.Assert.Equal(128, clip.MinWidth));
                var waveform = clips[0].GetVisualDescendants().OfType<Canvas>().Single();
                Xunit.Assert.Contains("waveform for", Avalonia.Automation.AutomationProperties.GetName(waveform), StringComparison.Ordinal);
                Xunit.Assert.NotEmpty(waveform.Children);

                viewModel.TimelineEditor.SetZoom(0.05);
                Dispatcher.UIThread.RunJobs();
                var viewport = window.GetVisualDescendants()
                    .OfType<ScrollViewer>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Video timeline clip viewport");
                viewport.Offset = new Avalonia.Vector(30, 0);
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal(2, viewModel.TimelineEditor.ScrollFrame);

                viewModel.TimelineEditor.ScrollBy(-2);
                Dispatcher.UIThread.RunJobs();
                var rulerViewport = window.GetVisualDescendants()
                    .OfType<ScrollViewer>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Timeline ruler viewport");
                rulerViewport.Offset = new Avalonia.Vector(30, 0);
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal(2, viewModel.TimelineEditor.ScrollFrame);
                Xunit.Assert.Equal(40, rulerViewport.Offset.X);
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);
    }

    [Xunit.Fact]
    public async Task MediaBrowserSearchFiltersNoMatchStateAndClearWithImportedAssets()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApplication));
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new BatchImporter());

        await session.Dispatch(() =>
        {
            viewModel.ImportFilesAsync(["first.mp4", "second.mp4"], CancellationToken.None).GetAwaiter().GetResult();
            var window = new MainWindow(viewModel);
            try
            {
                window.Show();
                var mediaList = window.GetVisualDescendants()
                    .OfType<ListBox>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Imported media");
                var search = window.GetVisualDescendants()
                    .OfType<TextBox>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Search imported media");
                var typeFilter = window.GetVisualDescendants()
                    .OfType<ComboBox>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Filter imported media by type");
                var clear = window.GetVisualDescendants()
                    .OfType<Button>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Clear media search");
                var emptyState = window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Single(control => Avalonia.Automation.AutomationProperties.GetName(control) == "Media search empty state");

                Xunit.Assert.Equal(2, mediaList.Items.Count);
                typeFilter.SelectedIndex = 2;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Empty(mediaList.Items);
                Xunit.Assert.True(emptyState.IsVisible);
                typeFilter.SelectedIndex = 1;
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal(2, mediaList.Items.Count);
                mediaList.SelectedIndex = 1;
                Dispatcher.UIThread.RunJobs();
                search.Text = "second";
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Single(mediaList.Items);
                Xunit.Assert.Equal("second.mp4", Xunit.Assert.IsType<ImportedMediaViewModel>(mediaList.SelectedItem).Name);
                Xunit.Assert.True(clear.IsEnabled);
                Xunit.Assert.False(emptyState.IsVisible);

                search.Text = "not-found";
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Empty(mediaList.Items);
                Xunit.Assert.True(clear.IsEnabled);
                Xunit.Assert.True(emptyState.IsVisible);

                clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Dispatcher.UIThread.RunJobs();
                Xunit.Assert.Equal(2, mediaList.Items.Count);
                Xunit.Assert.Equal("second.mp4", Xunit.Assert.IsType<ImportedMediaViewModel>(mediaList.SelectedItem).Name);
                Xunit.Assert.False(clear.IsEnabled);
                Xunit.Assert.False(emptyState.IsVisible);
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);
    }

    [Xunit.Fact]
    public async Task MediaImportRejectsSymlinkBeforeCallingImporter()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-desktop-import-boundary-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-desktop-import-external-");
        try
        {
            var targetPath = Path.Combine(externalDirectory.FullName, "clip.mp4");
            var linkedPath = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllBytesAsync(targetPath, [1, 2, 3]);
            File.CreateSymbolicLink(linkedPath, targetPath);
            var importer = new CountingImporter();
            var viewModel = new MainWindowViewModel(new WorkspaceCatalog(), new StubCapabilityDetector(), importer);

            await viewModel.ImportFilesAsync([linkedPath], CancellationToken.None);

            Xunit.Assert.Equal(0, importer.Calls);
            Xunit.Assert.Contains("symlink", viewModel.MediaImportStatusText, StringComparison.OrdinalIgnoreCase);
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (IOException)
        {
        }
        finally
        {
            directory.Delete(true);
            externalDirectory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task TimelinePreviewDecodesTheCurrentVideoSample()
    {
        var decoder = new StubPreviewDecoder();
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new VideoImporter(),
            timelinePreviewDecoder: decoder);

        await viewModel.ImportFilesAsync(["preview.mp4"], CancellationToken.None);
        viewModel.TimelineEditor.SeekFrame(5);

        Xunit.Assert.True(await viewModel.PreviewCurrentTimelineFrameAsync(CancellationToken.None));
        Xunit.Assert.Equal(5, decoder.FrameIndex);
        Xunit.Assert.Equal("P6\n1 1\n255\n\0\0\0"u8.ToArray(), viewModel.TimelinePreviewPpm);
        Xunit.Assert.Contains("frame 5", viewModel.TimelinePreviewStatusText, StringComparison.Ordinal);
        Xunit.Assert.Contains("decode_fps=", viewModel.TimelinePerformanceStatusText, StringComparison.Ordinal);
        Xunit.Assert.Contains("working_set_bytes=", viewModel.TimelinePerformanceStatusText, StringComparison.Ordinal);
    }

    [Xunit.Fact]
    public async Task TimelinePreviewCanUseAndReuseBoundedProxy()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"frame-theory-preview-source-{Guid.NewGuid():N}.mp4");
        var cachePath = Path.Combine(Path.GetTempPath(), $"frame-theory-preview-cache-{Guid.NewGuid():N}");
        File.WriteAllBytes(sourcePath, [1, 2, 3]);
        try
        {
            var decoder = new StubPreviewDecoder();
            var processRunner = new ProxyProcessRunner();
            var viewModel = new MainWindowViewModel(
                new WorkspaceCatalog(),
                new StubCapabilityDetector(),
                new VideoImporter(),
                timelinePreviewDecoder: decoder,
                timelinePreviewProxy: new FfmpegPreviewProxyService(processRunner, cachePath));

            await viewModel.ImportFilesAsync([sourcePath], CancellationToken.None);
            viewModel.UseTimelinePreviewProxy = true;

            Xunit.Assert.True(await viewModel.PreviewCurrentTimelineFrameAsync(CancellationToken.None));
            Xunit.Assert.True(await viewModel.PreviewCurrentTimelineFrameAsync(CancellationToken.None));
            Xunit.Assert.Equal(1, processRunner.Calls);
            Xunit.Assert.Equal(2, decoder.Files.Count);
            Xunit.Assert.NotEqual(new Uri(sourcePath), decoder.Files[0]);
            Xunit.Assert.Equal(decoder.Files[0], decoder.Files[1]);
            Xunit.Assert.Contains("cached preview proxy", viewModel.TimelinePreviewStatusText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(sourcePath);
            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, recursive: true);
            }
        }
    }

    [Xunit.Fact]
    public async Task TimelineCompositorPreviewFailureIsReportedWithoutEscapingTheUi()
    {
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new VideoImporter(),
            timelinePreviewRenderer: new FailingTimelinePreviewRenderer());

        await viewModel.ImportFilesAsync(["preview-failure.mp4"], CancellationToken.None);

        Xunit.Assert.False(await viewModel.PreviewCurrentTimelineFrameAsync(CancellationToken.None));
        Xunit.Assert.Contains("Timeline preview failed", viewModel.TimelinePreviewStatusText, StringComparison.Ordinal);
    }

    [Xunit.Fact]
    public async Task TimelineCompositorPreviewReportsItsRenderMode()
    {
        var renderer = new SuccessfulTimelinePreviewRenderer();
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new VideoImporter(),
            timelinePreviewRenderer: renderer);

        await viewModel.ImportFilesAsync(["preview-compositor.mp4"], CancellationToken.None);
        viewModel.TimelineEditor.SeekFrame(4);

        Xunit.Assert.True(await viewModel.PreviewCurrentTimelineFrameAsync(CancellationToken.None));
        Xunit.Assert.Equal(4, renderer.FrameIndex);
        Xunit.Assert.Contains("timeline compositor", viewModel.TimelinePreviewStatusText, StringComparison.Ordinal);
    }

    [Xunit.Fact]
    public async Task TimelineCompositorArgumentFailureIsReportedWithoutEscapingTheUi()
    {
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new VideoImporter(),
            timelinePreviewRenderer: new ArgumentFailingTimelinePreviewRenderer());

        await viewModel.ImportFilesAsync(["preview-argument-failure.mp4"], CancellationToken.None);

        Xunit.Assert.False(await viewModel.PreviewCurrentTimelineFrameAsync(CancellationToken.None));
        Xunit.Assert.Contains("Timeline preview failed", viewModel.TimelinePreviewStatusText, StringComparison.Ordinal);
    }

    [Xunit.Fact]
    public async Task TimelineMotionAnalysisUsesDecodedFramesAndRestoresThePlayhead()
    {
        var decoder = new StubPreviewDecoder();
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new VideoImporter(),
            timelinePreviewDecoder: decoder);

        await viewModel.ImportFilesAsync(["motion.mp4"], CancellationToken.None);
        viewModel.TimelineEditor.SeekFrame(4);

        Xunit.Assert.True(await viewModel.AnalyzeTimelineMotionAsync(CancellationToken.None));
        Xunit.Assert.Equal(4, viewModel.TimelineEditor.CurrentFrame);
        Xunit.Assert.Equal(12, decoder.Frames.Count);
        Xunit.Assert.Contains("Analyzed 12 frames", viewModel.MotionAnalysisStatusText, StringComparison.Ordinal);
    }

    [Xunit.Fact]
    public async Task TimelinePlaybackDecodesEveryFrameWithoutBoundarySnapping()
    {
        var decoder = new StubPreviewDecoder();
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new ShortVideoImporter(),
            timelinePreviewDecoder: decoder);

        await viewModel.ImportFilesAsync(["playback.mp4"], CancellationToken.None);

        Xunit.Assert.True(await viewModel.PlayTimelinePreviewAsync(CancellationToken.None));
        Xunit.Assert.Equal([0L, 1L, 2L], decoder.Frames);
        Xunit.Assert.Equal(2, viewModel.TimelineEditor.CurrentFrame);
        Xunit.Assert.True(viewModel.TimelineEditor.SnapEnabled);
        Xunit.Assert.False(viewModel.IsTimelinePreviewPlaying);
        Xunit.Assert.Contains("finished", viewModel.TimelinePreviewStatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Xunit.Fact]
    public async Task TimelinePlaybackStopsWhenCanceled()
    {
        var decoder = new BlockingPreviewDecoder();
        var cancellation = new CancellationTokenSource();
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new ShortVideoImporter(),
            timelinePreviewDecoder: decoder);

        await viewModel.ImportFilesAsync(["playback-cancel.mp4"], CancellationToken.None);
        var playback = viewModel.PlayTimelinePreviewAsync(cancellation.Token);
        await decoder.Started.Task;
        cancellation.Cancel();

        Xunit.Assert.False(await playback);
        Xunit.Assert.False(viewModel.IsTimelinePreviewPlaying);
        Xunit.Assert.True(viewModel.TimelineEditor.SnapEnabled);
    }

    private sealed class StubCapabilityDetector : ICapabilityDetector
    {
        public Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken) => Task.FromResult(
            new CapabilityReport("Test", "x64", [new("Storage", CapabilityState.Available, "ready")]));
    }

    private sealed class HeadlessTestApplication : Avalonia.Application
    {
        public HeadlessTestApplication()
        {
            Styles.Add(new FluentTheme());
        }
    }

    private sealed class ThrowingImporter : IMediaImporter
    {
        public Task<ImportedMedia> ImportAsync(Uri file, CancellationToken cancellationToken) =>
            throw new MediaProbeException("corrupt input");
    }

    private sealed class CountingImporter : IMediaImporter
    {
        public int Calls { get; private set; }

        public Task<ImportedMedia> ImportAsync(Uri file, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("The importer should not receive a redirected path.");
        }
    }

    private sealed class VideoImporter : IMediaImporter
    {
        public Task<ImportedMedia> ImportAsync(Uri file, CancellationToken cancellationToken)
        {
            var metadata = new MediaSourceMetadata(
                MediaId.Create(file, "preview-test"),
                file,
                MediaKind.Video,
                16,
                9,
                new RationalFrameRate(30, 1),
                30,
                TimeSpan.FromSeconds(1));
            return Task.FromResult(new ImportedMedia(metadata, null, []));
        }
    }

    private sealed class BatchImporter : IMediaImporter
    {
        public Task<ImportedMedia> ImportAsync(Uri file, CancellationToken cancellationToken)
        {
            var metadata = new MediaSourceMetadata(
                MediaId.Create(file, "batch-test"),
                file,
                MediaKind.Video,
                16,
                9,
                new RationalFrameRate(30, 1),
                30,
                TimeSpan.FromSeconds(1));
            return Task.FromResult(new ImportedMedia(metadata, null, [0.25f, 0.75f]));
        }
    }

    private sealed class ShortVideoImporter : IMediaImporter
    {
        public Task<ImportedMedia> ImportAsync(Uri file, CancellationToken cancellationToken)
        {
            var metadata = new MediaSourceMetadata(
                MediaId.Create(file, "playback-test"),
                file,
                MediaKind.Video,
                16,
                9,
                new RationalFrameRate(30, 1),
                3,
                TimeSpan.FromSeconds(0.1));
            return Task.FromResult(new ImportedMedia(metadata, null, []));
        }
    }

    private sealed class StubPreviewDecoder : IPreviewFrameDecoder
    {
        public long FrameIndex { get; private set; }

        public List<long> Frames { get; } = [];

        public List<Uri> Files { get; } = [];

        public Task<byte[]> DecodeFrameAsync(Uri file, RationalFrameRate frameRate, long frameIndex, CancellationToken cancellationToken)
        {
            Files.Add(file);
            FrameIndex = frameIndex;
            Frames.Add(frameIndex);
            return Task.FromResult("P6\n1 1\n255\n\0\0\0"u8.ToArray());
        }
    }

    private sealed class ProxyProcessRunner : IMediaProcessRunner
    {
        public int Calls { get; private set; }

        public Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            Calls++;
            File.WriteAllBytes(arguments[^1], [1]);
            return Task.FromResult(new MediaProcessResult(0, string.Empty, string.Empty));
        }
    }

    private sealed class BlockingPreviewDecoder : IPreviewFrameDecoder
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<byte[]> DecodeFrameAsync(Uri file, RationalFrameRate frameRate, long frameIndex, CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return [];
        }
    }

    private sealed class FailingTimelinePreviewRenderer : ITimelinePreviewRenderer
    {
        public Task<byte[]> RenderFrameAsync(TimelineVideoRenderPlan plan, long frameIndex, CancellationToken cancellationToken) =>
            throw new MediaExportException("invalid preview graph");
    }

    private sealed class SuccessfulTimelinePreviewRenderer : ITimelinePreviewRenderer
    {
        public long FrameIndex { get; private set; }

        public Task<byte[]> RenderFrameAsync(TimelineVideoRenderPlan plan, long frameIndex, CancellationToken cancellationToken) =>
            RenderAsync(frameIndex);

        private Task<byte[]> RenderAsync(long frameIndex)
        {
            FrameIndex = frameIndex;
            return Task.FromResult("P6\n1 1\n255\n\0\0\0"u8.ToArray());
        }
    }

    private sealed class ArgumentFailingTimelinePreviewRenderer : ITimelinePreviewRenderer
    {
        public Task<byte[]> RenderFrameAsync(TimelineVideoRenderPlan plan, long frameIndex, CancellationToken cancellationToken) =>
            throw new ArgumentException("invalid preview arguments");
    }
}
