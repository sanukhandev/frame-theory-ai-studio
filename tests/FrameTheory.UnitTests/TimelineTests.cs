using FrameTheory.Domain;

namespace FrameTheory.UnitTests;

public sealed class TimelineTests
{
    [Xunit.Fact]
    public void SeekScrubAndStepKeepVideoAndAudioSynchronized()
    {
        var videoId = MediaId.Parse(new string('a', 64));
        var audioId = MediaId.Parse(new string('b', 64));
        var projectRate = new RationalFrameRate(30, 1);
        var graph = new TimelineGraph(projectRate,
        [
            new TimelineTrack("video-1", TimelineTrackKind.Video,
                [TimelineClip.Video(videoId, 0, 90, 0, 90, new RationalFrameRate(30, 1))]),
            new TimelineTrack("audio-1", TimelineTrackKind.Audio,
                [TimelineClip.Audio(audioId, 0, 144000, 0, 90, 48000)])
        ]);
        var transport = new TimelineTransport(graph);

        var seek = transport.Seek(30);
        Xunit.Assert.Equal(TimeSpan.FromSeconds(1), seek.Position);
        Xunit.Assert.Equal(2, seek.Samples.Count);
        Xunit.Assert.Equal(30, seek.Samples.Single(sample => sample.TrackKind == TimelineTrackKind.Video).SourceFrame);
        Xunit.Assert.Equal(48000, seek.Samples.Single(sample => sample.TrackKind == TimelineTrackKind.Audio).SourceSample);

        var scrub = transport.Scrub(TimeSpan.FromSeconds(1.5));
        Xunit.Assert.Equal(45, scrub.TimelineFrame);
        Xunit.Assert.Equal(72000, scrub.Samples.Single(sample => sample.TrackKind == TimelineTrackKind.Audio).SourceSample);

        var stepped = transport.Step(15);
        Xunit.Assert.Equal(60, stepped.TimelineFrame);
        Xunit.Assert.Equal(60, transport.CurrentFrame);
    }

    [Xunit.Fact]
    public void TimelineRejectsOverlapsMismatchedTracksAndInvalidSeek()
    {
        var mediaId = MediaId.Parse(new string('c', 64));
        var videoClip = TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1));
        Xunit.Assert.Throws<ArgumentException>(() => new TimelineTrack("video", TimelineTrackKind.Video,
            [videoClip, TimelineClip.Video(mediaId, 30, 30, 15, 30, new RationalFrameRate(30, 1))]));
        Xunit.Assert.Throws<ArgumentException>(() => new TimelineTrack("audio", TimelineTrackKind.Audio, [videoClip]));

        var graph = new TimelineGraph(new RationalFrameRate(30, 1), []);
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => graph.Seek(-1));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => TimelineClip.Video(
            mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1)).SourceFrameAt(30, new RationalFrameRate(30, 1)));
        Xunit.Assert.Throws<ArgumentException>(() => new TimelineTrack("video", TimelineTrackKind.Video, [null!]));
        Xunit.Assert.Throws<ArgumentException>(() => new TimelineGraph(new RationalFrameRate(30, 1), [null!]));
        Xunit.Assert.Throws<ArgumentException>(() => new TimelineSpeedRamp([null!, new TimelineSpeedPoint(1, 1)]));
    }

    [Xunit.Fact]
    public void TimelineClipReportsTheInvalidRangeParameter()
    {
        var mediaId = MediaId.Parse(new string('d', 64));

        Xunit.Assert.Equal("sourceUnitCount", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => TimelineClip.Video(
            mediaId, 0, 0, 0, 30, new RationalFrameRate(30, 1))).ParamName);
        Xunit.Assert.Equal("timelineDurationFrames", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => TimelineClip.Video(
            mediaId, 0, 30, 0, 0, new RationalFrameRate(30, 1))).ParamName);
    }

    [Xunit.Fact]
    public void TimelineCollectionsExposeReadOnlyViews()
    {
        var mediaId = MediaId.Parse(new string('e', 64));
        var clip = TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1));
        var track = new TimelineTrack("video", TimelineTrackKind.Video, [clip]);
        var graph = new TimelineGraph(new RationalFrameRate(30, 1), [track]);

        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<TimelineClip>>(track.Clips).IsReadOnly);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<TimelineTrack>>(graph.Tracks).IsReadOnly);
    }

    [Xunit.Fact]
    public void SeekResultSnapshotsSamples()
    {
        var samples = new List<TimelineSample> { new("video", TimelineTrackKind.Video, MediaId.Parse(new string('f', 64)), 1, null) };
        var result = new TimelineSeekResult(1, TimeSpan.FromSeconds(1), samples);

        samples.Clear();

        Xunit.Assert.Single(result.Samples);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<TimelineSample>>(result.Samples).IsReadOnly);
    }
}
