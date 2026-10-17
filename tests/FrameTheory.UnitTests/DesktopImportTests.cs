using FrameTheory.Application;
using FrameTheory.Desktop;
using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class DesktopImportTests
{
    [Xunit.Fact]
    public async Task CorruptMediaIsReportedInTheImportStatusInsteadOfEscapingTheUi()
    {
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new ThrowingImporter());

        await viewModel.ImportFilesAsync(["corrupt.png"], CancellationToken.None);

        Xunit.Assert.Contains("Failed", viewModel.MediaImportStatusText);
        Xunit.Assert.Contains("corrupt input", viewModel.MediaImportStatusText);
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

    private sealed class ThrowingImporter : IMediaImporter
    {
        public Task<ImportedMedia> ImportAsync(Uri file, CancellationToken cancellationToken) =>
            throw new MediaProbeException("corrupt input");
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
}
