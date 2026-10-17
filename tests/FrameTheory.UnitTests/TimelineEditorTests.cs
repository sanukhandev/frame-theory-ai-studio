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
