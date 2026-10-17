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
    public void TimelineStateRejectsMalformedJson()
    {
        Xunit.Assert.Throws<InvalidDataException>(() => TimelineProjectStateJson.Deserialize("{not-json"));
        Xunit.Assert.Throws<InvalidDataException>(() => TimelineProjectStateJson.Deserialize("{\"videoClips\":null}"));
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
