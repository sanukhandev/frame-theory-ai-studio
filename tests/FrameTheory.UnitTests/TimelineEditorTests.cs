using FrameTheory.Captions;
using FrameTheory.Domain;
using FrameTheory.Desktop;

namespace FrameTheory.UnitTests;

public sealed class TimelineEditorTests
{
    [Xunit.Fact]
    public void TimelineEditorAddsRealMediaTracksAndKeepsExactFrameSelection()
    {
        var source = new Uri("file:///clip.mp4");
        var editor = new TimelineEditorViewModel(new RationalFrameRate(30, 1));
        editor.AddMedia(Metadata(source, "a", 90));
        editor.AddMedia(Metadata(new Uri("file:///clip-2.mp4"), "b", 60, hasAudio: false));

        editor.SnapEnabled = false;
        var seek = editor.SeekFrame(29);
        editor.SelectAt(29, "Video 1");

        Xunit.Assert.Equal(29, seek.TimelineFrame);
        Xunit.Assert.Equal(150, editor.TotalFrames);
        Xunit.Assert.Equal(2, editor.TrackRows.Count);
        Xunit.Assert.True(editor.TrackRows.Single(row => row.Id == "Video 1").Clips.First().IsSelected);
        Xunit.Assert.Contains("Selected Video clip", editor.SelectionContextText, StringComparison.Ordinal);
        Xunit.Assert.Equal(TimeSpan.FromSeconds(29d / 30), editor.CurrentPosition);
    }

    [Xunit.Fact]
    public void TimelineEditorSnapsZoomsScrollsAndRejectsInvalidRulerRequests()
    {
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(Metadata(new Uri("file:///clip.mp4"), "c", 90));
        Xunit.Assert.NotEmpty(editor.TrackRows);

        var snapped = editor.SeekFrame(1);
        editor.SetZoom(2);
        editor.ScrollBy(10);
        editor.ScrubPixels(5);
        var ticks = editor.BuildRulerTicks(320);

        Xunit.Assert.Equal(0, snapped.TimelineFrame);
        Xunit.Assert.Equal(2, editor.FramesPerPixel);
        Xunit.Assert.NotEmpty(ticks);
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => editor.BuildRulerTicks(0));
    }

    [Xunit.Fact]
    public void TimelineRulerRemainsBoundedForDenseHiDpiViewport()
    {
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(Metadata(new Uri("file:///long-clip.mp4"), "long", 1_000_000, hasAudio: false));
        editor.SetZoom(0.05);

        var ticks = editor.BuildRulerTicks(1280 * 2);

        Xunit.Assert.InRange(ticks.Count, 1, 2560);
        Xunit.Assert.Equal(0, ticks[0].Frame);
    }

    [Xunit.Fact]
    public void TimelineEditorSupportsCopyPasteSplitTrimRippleDeleteAndOverlapProtection()
    {
        var mediaId = MediaId.Parse(new string('d', 64));
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(Metadata(new Uri("file:///clip.mp4"), "d", 90, hasAudio: false));
        editor.SelectAt(10, "Video 1");
        var clipboard = editor.CopySelected();

        Xunit.Assert.NotNull(clipboard);
        editor.Paste(clipboard!, 90);
        editor.SplitSelectedAt(120);
        editor.TrimSelected(120, 150);
        editor.RippleDeleteSelected();

        Xunit.Assert.Equal(120, editor.TotalFrames);
        Xunit.Assert.Equal(2, editor.TrackRows.Single().Clips.Count);
        Xunit.Assert.Throws<ArgumentException>(() => editor.InsertClip(
            TimelineTrackKind.Video,
            TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1))));
        Xunit.Assert.Equal(120, editor.TotalFrames);
    }

    [Xunit.Fact]
    public void TimelineEditorAppliesAnimationPresetsToRenderPlans()
    {
        var source = new Uri("file:///animated.mp4");
        var metadata = Metadata(source, "animated", 30, hasAudio: false);
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(metadata);
        editor.SelectAt(0, "Video 1");
        editor.ApplyAnimationPresetToSelected(TimelineAnimationPreset.FadeIn);

        var plan = editor.BuildRenderPlan(1920, 1080, new Dictionary<MediaId, Uri> { [metadata.Id] = source });
        var segment = plan.Layers.Single().Segments.Single();

        Xunit.Assert.NotNull(segment.Animation);
        Xunit.Assert.Equal(0, segment.Animation!.Evaluate(0).Opacity);
        Xunit.Assert.Equal(1, segment.Animation.Evaluate(29).Opacity);
        editor.ClearSelectedAnimation();
        Xunit.Assert.Null(editor.BuildRenderPlan(1920, 1080, new Dictionary<MediaId, Uri> { [metadata.Id] = source }).Layers.Single().Segments.Single().Animation);
    }

    [Xunit.Fact]
    public void TimelineEditorAppliesAudioSettingsToSynchronizedRenderPlan()
    {
        var source = new Uri("file:///audio.mp4");
        var metadata = Metadata(source, "audio", 30);
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(metadata);
        editor.SelectAt(0, "Audio 1");
        editor.ApplyAudioSettingsToSelected(3, muted: false, fadeInFrames: 2, fadeOutFrames: 2);
        editor.ApplyAudioDuckingToSelected();

        var audio = editor.BuildRenderPlan(1920, 1080, new Dictionary<MediaId, Uri> { [metadata.Id] = source }).AudioLayers.Single().Segments.Single();

        Xunit.Assert.Equal(3, audio.GainDb);
        Xunit.Assert.Equal(2, audio.FadeInFrames);
        Xunit.Assert.Equal(2, audio.FadeOutFrames);
        Xunit.Assert.Equal(12, audio.DuckingDb);
    }

    [Xunit.Fact]
    public void TimelineEditorCreatesBasicBeatMarkersFromImportedWaveformPeaks()
    {
        var source = new Uri("file:///beats.mp4");
        var metadata = Metadata(source, "beats", 90);
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(metadata);

        editor.AddBeatMarkers(metadata.Id, [0, 0.8f, 0, 0.7f, 0]);

        Xunit.Assert.Equal([18L, 54L], editor.BeatMarkers);
        Xunit.Assert.Contains("18", editor.BeatMarkersText, StringComparison.Ordinal);
    }

    [Xunit.Fact]
    public void TimelineEditorCanRebuildMarkersUsingEnergyOnsets()
    {
        var source = new Uri("file:///energy-beats.mp4");
        var metadata = Metadata(source, "energy-beats", 90);
        var editor = new TimelineEditorViewModel
        {
            BeatDetectionMode = BeatDetectionMode.EnergyOnsets
        };
        editor.AddMedia(metadata);

        editor.AddBeatMarkers(metadata.Id, [0.1f, 0.4f, 0.8f, 0.2f, 0.3f]);

        Xunit.Assert.Equal([36L], editor.BeatMarkers);
        editor.BeatDetectionMode = BeatDetectionMode.AmplitudePeaks;
        Xunit.Assert.Equal([36L], editor.BeatMarkers);
    }

    [Xunit.Fact]
    public void TimelineEditorImportsTranscriptWordsAsVerifiedTimedCues()
    {
        var words = Enumerable.Range(0, 8)
            .Select(index => new TranscriptWord($"word-{index}", TimeSpan.FromMilliseconds(index * 500), TimeSpan.FromMilliseconds(index * 500 + 250)))
            .ToArray();
        var transcript = new TranscriptResult(new Uri("file:///spoken.wav"), "sha256:spoken", words);
        var editor = new TimelineEditorViewModel();

        editor.ImportTranscript(transcript);

        Xunit.Assert.Equal(2, editor.Captions.Count);
        Xunit.Assert.Equal("word-0 word-1 word-2 word-3 word-4 word-5 word-6", editor.Captions[0].Text);
        Xunit.Assert.Equal(CaptionProvenance.VerifiedTranscript, editor.Captions[0].Provenance);
        Xunit.Assert.Equal("sha256:spoken", editor.TranscriptSourceFingerprint);
    }

    [Xunit.Fact]
    public void TimelineEditorAddsTitlesAtTheCurrentFrame()
    {
        var source = new Uri("file:///title.mp4");
        var metadata = Metadata(source, "title", 90, hasAudio: false);
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(metadata);
        editor.SeekFrame(10);
        editor.AddTitle("Hook", "C:\\Fonts\\licensed.ttf");

        var title = editor.BuildRenderPlan(1920, 1080, new Dictionary<MediaId, Uri> { [metadata.Id] = source }).Titles.Single();
        Xunit.Assert.Equal("Hook", title.Text);
        Xunit.Assert.Equal(10, title.TimelineRange.StartFrame);
    }

    [Xunit.Fact]
    public void TimelineEditorCarriesCaptionFontIntoRenderPlan()
    {
        var source = new Uri("file:///caption.mp4");
        var metadata = Metadata(source, "caption", 90, hasAudio: false);
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(metadata);
        editor.ImportCaptions("1\n00:00:00,000 --> 00:00:01,000\nHello");
        editor.SetCaptionFontPath("C:\\Fonts\\licensed.ttf");

        var plan = editor.BuildRenderPlan(1920, 1080, new Dictionary<MediaId, Uri> { [metadata.Id] = source });
        Xunit.Assert.Single(plan.Captions);
        Xunit.Assert.NotNull(plan.CaptionStyle);
    }

    [Xunit.Fact]
    public void TimelineEditorAppliesInspectableAnimationToLastTitle()
    {
        var source = new Uri("file:///animated-title.mp4");
        var metadata = Metadata(source, "animated-title", 90, hasAudio: false);
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(metadata);
        editor.AddTitle("Animated", "C:\\Fonts\\licensed.ttf");
        editor.ApplyAnimationPresetToLastTitle(TimelineAnimationPreset.PushIn);

        var title = editor.BuildRenderPlan(1920, 1080, new Dictionary<MediaId, Uri> { [metadata.Id] = source }).Titles.Single();
        Xunit.Assert.NotNull(title.Animation);
        Xunit.Assert.Equal(2, title.Animation!.Keyframes.Count);
        Xunit.Assert.Equal(0.8, title.Animation.Keyframes[0].Value.Scale);
        Xunit.Assert.Equal(1, title.Animation.Keyframes[1].Value.Scale);
    }

    private static MediaSourceMetadata Metadata(Uri uri, string fingerprint, long durationFrames, bool hasAudio = true) => new(
        MediaId.Create(uri, fingerprint),
        uri,
        MediaKind.Video,
        1920,
        1080,
        new RationalFrameRate(30, 1),
        durationFrames,
        TimeSpan.FromSeconds((double)durationFrames / 30),
        hasAudio: hasAudio);
}
