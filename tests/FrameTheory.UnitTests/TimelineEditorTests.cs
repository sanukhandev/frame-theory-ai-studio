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

        var audio = editor.BuildRenderPlan(1920, 1080, new Dictionary<MediaId, Uri> { [metadata.Id] = source }).AudioLayers.Single().Segments.Single();

        Xunit.Assert.Equal(3, audio.GainDb);
        Xunit.Assert.Equal(2, audio.FadeInFrames);
        Xunit.Assert.Equal(2, audio.FadeOutFrames);
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
