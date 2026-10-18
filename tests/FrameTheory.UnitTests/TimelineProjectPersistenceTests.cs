using FrameTheory.Application;
using FrameTheory.Captions;
using FrameTheory.Color;
using FrameTheory.Desktop;
using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class TimelineProjectPersistenceTests
{
    [Xunit.Fact]
    public void TimelineStateRoundTripPreservesEditableOperationsAndRemapsMedia()
    {
        var originalId = MediaId.Parse(new string('a', 64));
        var remappedId = MediaId.Parse(new string('b', 64));
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(new MediaSourceMetadata(
            originalId,
            new Uri(Path.GetFullPath("clip.mp4")),
            MediaKind.Video,
            16,
            9,
            new RationalFrameRate(30, 1),
            30,
            TimeSpan.FromSeconds(1)));
        editor.SelectFirst(TimelineTrackKind.Video);
        editor.ApplyStaticTransformToSelected(1.2, 3, 4, 0);
        editor.ApplyColorGradeToMedia(originalId.Value, new ColorGrade(exposureEV: 1, saturation: 0.25f));
        editor.AddTitleAt("Opening", "licensed-font.ttf", 0, 10, TimelineAnimationPreset.FadeIn);
        editor.AddCaptionAt("Verified caption", CaptionProvenance.AuthoredCaption, 2, 5, "licensed-font.ttf");
        editor.ApplyBeforeAfterStyle(TimelineRevealStyle.Wipe);
        editor.SeekFrame(5);

        var json = TimelineProjectStateJson.Serialize(editor.CaptureProjectState());
        var restored = new TimelineEditorViewModel();
        restored.RestoreProjectState(
            TimelineProjectStateJson.Deserialize(json),
            new Dictionary<string, string> { [originalId.Value] = remappedId.Value });

        Xunit.Assert.Equal(30, restored.TotalFrames);
        Xunit.Assert.Equal(5, restored.CurrentFrame);
        Xunit.Assert.Equal(remappedId.Value, restored.TrackRows.Single(row => row.Kind == TimelineTrackKind.Video).Clips.Single().MediaId);
        Xunit.Assert.Equal("Verified caption", restored.Captions.Single().Text);
        Xunit.Assert.Equal(TimelineRevealStyle.Wipe, restored.CaptureProjectState().BeforeAfterStyle);
        Xunit.Assert.Single(restored.CaptureProjectState().ColorGrades);
        Xunit.Assert.NotEmpty(restored.CaptureProjectState().VideoSettings);
    }

    [Xunit.Fact]
    public void TimelineStateRoundTripPreservesKineticCaptionWords()
    {
        var mediaId = MediaId.Parse(new string('d', 64));
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(new MediaSourceMetadata(
            mediaId,
            new Uri(Path.GetFullPath("caption.mp4")),
            MediaKind.Video,
            16,
            9,
            new RationalFrameRate(30, 1),
            30,
            TimeSpan.FromSeconds(1)));
        editor.ImportTranscript(new TranscriptResult(
            new Uri("file:///spoken.wav"),
            "sha256:spoken",
            [
                new TranscriptWord("one", TimeSpan.Zero, TimeSpan.FromMilliseconds(250)),
                new TranscriptWord("two", TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(600))
            ]));
        editor.KineticCaptions = true;

        var restored = new TimelineEditorViewModel();
        restored.RestoreProjectState(TimelineProjectStateJson.Deserialize(TimelineProjectStateJson.Serialize(editor.CaptureProjectState())));

        Xunit.Assert.True(restored.KineticCaptions);
        Xunit.Assert.Equal(["one", "two"], restored.Captions.Single().Words.Select(word => word.Text));
        Xunit.Assert.Equal(CaptionProvenance.VerifiedTranscript, restored.Captions.Single().Provenance);
        Xunit.Assert.Equal("sha256:spoken", restored.TranscriptSourceFingerprint);
    }

    [Xunit.Fact]
    public void TimelineStateRoundTripResolvesApprovedWatermarkFromPortableAssetReference()
    {
        var editor = new TimelineEditorViewModel();
        editor.SetApprovedWatermark();

        var state = editor.CaptureProjectState();
        Xunit.Assert.NotNull(state.Watermark);
        Xunit.Assert.True(state.Watermark!.IsApprovedAsset);
        Xunit.Assert.Equal("assets/brand/frame-theory-original.png", state.Watermark.SourcePath);

        var restored = new TimelineEditorViewModel();
        restored.RestoreProjectState(TimelineProjectStateJson.Deserialize(TimelineProjectStateJson.Serialize(state)));

        Xunit.Assert.Equal(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "assets/brand/frame-theory-original.png")),
            restored.Watermark!.SourcePath);
    }

    [Xunit.Fact]
    public void TimelineStateRoundTripPreservesSpeedRamp()
    {
        var mediaId = MediaId.Parse(new string('e', 64));
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(new MediaSourceMetadata(
            mediaId,
            new Uri(Path.GetFullPath("speed-ramp.mp4")),
            MediaKind.Video,
            16,
            9,
            new RationalFrameRate(30, 1),
            60,
            TimeSpan.FromSeconds(2),
            hasAudio: false));
        editor.SelectFirst(TimelineTrackKind.Video);
        editor.ApplySpeedRampToSelected(0.5, 2);

        var restored = new TimelineEditorViewModel();
        restored.RestoreProjectState(TimelineProjectStateJson.Deserialize(TimelineProjectStateJson.Serialize(editor.CaptureProjectState())));

        var ramp = restored.BuildRenderPlan(16, 9, new Dictionary<MediaId, Uri> { [mediaId] = new Uri(Path.GetFullPath("speed-ramp.mp4")) })
            .Layers.Single().Segments.Single().SpeedRamp;
        Xunit.Assert.NotNull(ramp);
        Xunit.Assert.Equal(0.5, ramp!.Points[0].Speed);
        Xunit.Assert.Equal(2, ramp.Points[^1].Speed);
    }

    [Xunit.Fact]
    public void TimelineStateRoundTripPreservesStabilization()
    {
        var mediaId = MediaId.Parse(new string('f', 64));
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(new MediaSourceMetadata(
            mediaId,
            new Uri(Path.GetFullPath("stabilization.mp4")),
            MediaKind.Video,
            16,
            9,
            new RationalFrameRate(30, 1),
            30,
            TimeSpan.FromSeconds(1),
            hasAudio: false));
        editor.SelectFirst(TimelineTrackKind.Video);
        editor.ApplyStabilizationToSelected(12);

        var restored = new TimelineEditorViewModel();
        restored.RestoreProjectState(TimelineProjectStateJson.Deserialize(TimelineProjectStateJson.Serialize(editor.CaptureProjectState())));

        var stabilization = restored.BuildRenderPlan(16, 9, new Dictionary<MediaId, Uri> { [mediaId] = new Uri(Path.GetFullPath("stabilization.mp4")) })
            .Layers.Single().Segments.Single().Stabilization;
        Xunit.Assert.NotNull(stabilization);
        Xunit.Assert.Equal(12, stabilization!.SearchRadius);
    }

    [Xunit.Fact]
    public async Task DesktopProjectSaveAndOpenReimportsRelativeMediaAndRestoresTimeline()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"frame-theory-project-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var mediaPath = Path.Combine(directory, "clip.mp4");
            await File.WriteAllBytesAsync(mediaPath, [1, 2, 3]);
            var projectPath = Path.Combine(directory, "project.ftstudio");
            var first = CreateViewModel();

            await first.ImportFilesAsync([mediaPath], CancellationToken.None);
            Xunit.Assert.True(await first.SaveTimelineProjectAsync(projectPath, CancellationToken.None));

            var second = CreateViewModel();
            Xunit.Assert.True(await second.OpenTimelineProjectAsync(projectPath, CancellationToken.None));

            Xunit.Assert.Single(second.ImportedMedia);
            Xunit.Assert.Single(second.TimelineEditor.TrackRows.Single(row => row.Kind == TimelineTrackKind.Video).Clips);
            Xunit.Assert.Contains("Opened timeline project", second.TimelineProjectStatusText, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Xunit.Fact]
    public async Task TimelineProjectOpenOffersMissingMediaRelinkBeforeMutation()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"frame-theory-relink-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var mediaPath = Path.Combine(directory, "clip.mp4");
            var replacementPath = Path.Combine(directory, "clip-relinked.mp4");
            await File.WriteAllBytesAsync(mediaPath, [1, 2, 3]);
            var projectPath = Path.Combine(directory, "project.ftstudio");
            var first = CreateViewModel();
            await first.ImportFilesAsync([mediaPath], CancellationToken.None);
            Xunit.Assert.True(await first.SaveTimelineProjectAsync(projectPath, CancellationToken.None));

            File.Move(mediaPath, replacementPath);
            var second = CreateViewModel();

            Xunit.Assert.False(await second.OpenTimelineProjectAsync(projectPath, CancellationToken.None));
            Xunit.Assert.Single(second.MissingTimelineMedia);
            Xunit.Assert.Contains("missing media", second.TimelineProjectStatusText, StringComparison.OrdinalIgnoreCase);
            Xunit.Assert.Empty(second.ImportedMedia);

            Xunit.Assert.True(await second.RelinkSelectedTimelineMediaAsync(replacementPath, CancellationToken.None));
            Xunit.Assert.Empty(second.MissingTimelineMedia);
            Xunit.Assert.Single(second.ImportedMedia);
            Xunit.Assert.Contains("Opened timeline project", second.TimelineProjectStatusText, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Xunit.Fact]
    public async Task TimelineProjectOpenWorksAfterProjectAndMediaMove()
    {
        var sourceDirectory = Path.Combine(Path.GetTempPath(), $"frame-theory-move-{Guid.NewGuid():N}");
        var movedDirectory = sourceDirectory + "-moved";
        Directory.CreateDirectory(sourceDirectory);
        try
        {
            var mediaPath = Path.Combine(sourceDirectory, "clip.mp4");
            await File.WriteAllBytesAsync(mediaPath, [1, 2, 3]);
            var projectPath = Path.Combine(sourceDirectory, "project.ftstudio");
            var first = CreateViewModel();
            await first.ImportFilesAsync([mediaPath], CancellationToken.None);
            Xunit.Assert.True(await first.SaveTimelineProjectAsync(projectPath, CancellationToken.None));

            Directory.Move(sourceDirectory, movedDirectory);
            var movedProjectPath = Path.Combine(movedDirectory, "project.ftstudio");
            var second = CreateViewModel();

            Xunit.Assert.True(await second.OpenTimelineProjectAsync(movedProjectPath, CancellationToken.None));
            Xunit.Assert.Single(second.ImportedMedia);
            Xunit.Assert.Empty(second.MissingTimelineMedia);
        }
        finally
        {
            if (Directory.Exists(sourceDirectory))
            {
                Directory.Delete(sourceDirectory, recursive: true);
            }

            if (Directory.Exists(movedDirectory))
            {
                Directory.Delete(movedDirectory, recursive: true);
            }
        }
    }

    [Xunit.Fact]
    public async Task TimelineProjectImportRejectsSymlinkedMediaDirectory()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-timeline-boundary-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-timeline-external-");
        try
        {
            var projectDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "project"));
            var mediaDirectory = Path.Combine(projectDirectory.FullName, "media");
            Directory.CreateSymbolicLink(mediaDirectory, externalDirectory.FullName);
            var sourcePath = Path.Combine(mediaDirectory, "clip.mp4");
            await File.WriteAllBytesAsync(Path.Combine(externalDirectory.FullName, "clip.mp4"), [1, 2, 3]);
            var viewModel = CreateViewModel();

            await viewModel.ImportFilesAsync([sourcePath], CancellationToken.None);

            Xunit.Assert.Empty(viewModel.ImportedMedia);
            Xunit.Assert.Contains("reparse", viewModel.MediaImportStatusText, StringComparison.OrdinalIgnoreCase);
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
    public void TimelineStateRejectsMalformedJson()
    {
        Xunit.Assert.Throws<InvalidDataException>(() => TimelineProjectStateJson.Deserialize("{not-json"));
        Xunit.Assert.Throws<InvalidDataException>(() => TimelineProjectStateJson.Deserialize("{\"videoClips\":null}"));
    }

    [Xunit.Fact]
    public void InvalidRestoredGradeDoesNotPartiallyMutateTheCurrentTimeline()
    {
        var mediaId = MediaId.Parse(new string('c', 64));
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(new MediaSourceMetadata(
            mediaId,
            new Uri(Path.GetFullPath("clip.mp4")),
            MediaKind.Video,
            16,
            9,
            new RationalFrameRate(30, 1),
            30,
            TimeSpan.FromSeconds(1)));
        var before = editor.TrackRows;
        var invalidState = editor.CaptureProjectState();
        invalidState.ColorGrades.Add(new TimelineColorGradeProjectState { MediaId = "not-a-media-id" });

        Xunit.Assert.Throws<ArgumentException>(() => editor.RestoreProjectState(invalidState));
        Xunit.Assert.Equal(before, editor.TrackRows);
    }

    private static MainWindowViewModel CreateViewModel() => new(
        new WorkspaceCatalog(),
        new StubCapabilityDetector(),
        new VideoImporter());

    private sealed class StubCapabilityDetector : ICapabilityDetector
    {
        public Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken) => Task.FromResult(
            new CapabilityReport("Test", "x64", [new("Storage", CapabilityState.Available, "ready")]));
    }

    private sealed class VideoImporter : IMediaImporter
    {
        public Task<ImportedMedia> ImportAsync(Uri file, CancellationToken cancellationToken)
        {
            var metadata = new MediaSourceMetadata(
                MediaId.Create(file, "persist-test"),
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
}
