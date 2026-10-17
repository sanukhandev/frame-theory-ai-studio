namespace FrameTheory.Domain;

public enum TimelineTrackKind
{
    Video,
    Audio
}

public sealed record TimelineClip
{
    private TimelineClip(
        MediaId mediaId,
        long sourceStartUnit,
        long sourceUnitCount,
        long timelineStartFrame,
        long timelineDurationFrames,
        RationalFrameRate? sourceFrameRate,
        int audioSampleRate,
        long sourceStartSample)
    {
        if (string.IsNullOrWhiteSpace(mediaId.Value))
        {
            throw new ArgumentException("A media ID is required.", nameof(mediaId));
        }

        if (sourceStartUnit < 0 || sourceUnitCount <= 0 || timelineStartFrame < 0 || timelineDurationFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceStartUnit), "Clip positions and lengths must be positive or zero as appropriate.");
        }

        if (sourceStartUnit > long.MaxValue - sourceUnitCount)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceUnitCount), "The source range exceeds the supported range.");
        }

        if (timelineStartFrame > long.MaxValue - timelineDurationFrames)
        {
            throw new ArgumentOutOfRangeException(nameof(timelineDurationFrames), "The timeline range exceeds the supported range.");
        }

        if (sourceFrameRate is null && audioSampleRate <= 0)
        {
            throw new ArgumentException("A video frame rate or audio sample rate is required.");
        }

        if (sourceFrameRate is not null && audioSampleRate > 0)
        {
            throw new ArgumentException("A clip cannot define both video and audio timing.");
        }

        if (sourceStartSample < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceStartSample));
        }

        MediaId = mediaId;
        SourceStartUnit = sourceStartUnit;
        SourceUnitCount = sourceUnitCount;
        TimelineRange = new FrameRange(timelineStartFrame, timelineDurationFrames);
        SourceFrameRate = sourceFrameRate;
        AudioSampleRate = audioSampleRate;
        SourceStartSample = sourceStartSample;
    }

    public MediaId MediaId { get; }

    public long SourceStartUnit { get; }

    public long SourceUnitCount { get; }

    public FrameRange TimelineRange { get; }

    public RationalFrameRate? SourceFrameRate { get; }

    public int AudioSampleRate { get; }

    public long SourceStartSample { get; }

    public bool IsVideo => SourceFrameRate is not null;

    public bool IsAudio => AudioSampleRate > 0;

    public static TimelineClip Video(
        MediaId mediaId,
        long sourceStartFrame,
        long sourceFrameCount,
        long timelineStartFrame,
        long timelineDurationFrames,
        RationalFrameRate sourceFrameRate) => new(
            mediaId,
            sourceStartFrame,
            sourceFrameCount,
            timelineStartFrame,
            timelineDurationFrames,
            sourceFrameRate,
            0,
            0);

    public static TimelineClip Audio(
        MediaId mediaId,
        long sourceStartSample,
        long sourceSampleCount,
        long timelineStartFrame,
        long timelineDurationFrames,
        int sampleRate) => new(
            mediaId,
            sourceStartSample,
            sourceSampleCount,
            timelineStartFrame,
            timelineDurationFrames,
            null,
            sampleRate,
            sourceStartSample);

    public long SourceFrameAt(long timelineFrame, RationalFrameRate projectFrameRate)
    {
        EnsureContains(timelineFrame);
        var sourceFrameRate = SourceFrameRate ?? throw new InvalidOperationException("The clip is not video.");
        var relativeTimelineFrame = timelineFrame - TimelineRange.StartFrame;
        var sourceOffset = (Int128)relativeTimelineFrame * sourceFrameRate.Numerator * projectFrameRate.Denominator /
            ((Int128)sourceFrameRate.Denominator * projectFrameRate.Numerator);
        return SourceStartUnit + Math.Min((long)sourceOffset, SourceUnitCount - 1);
    }

    public long SourceSampleAt(long timelineFrame, RationalFrameRate projectFrameRate)
    {
        EnsureContains(timelineFrame);
        if (!IsAudio)
        {
            throw new InvalidOperationException("The clip is not audio.");
        }

        var relativeTimelineFrame = timelineFrame - TimelineRange.StartFrame;
        var sampleOffset = (Int128)relativeTimelineFrame * AudioSampleRate * projectFrameRate.Denominator /
            projectFrameRate.Numerator;
        return SourceStartSample + Math.Min((long)sampleOffset, SourceUnitCount - 1);
    }

    private void EnsureContains(long timelineFrame)
    {
        if (!TimelineRange.Contains(timelineFrame))
        {
            throw new ArgumentOutOfRangeException(nameof(timelineFrame), "The timeline frame is outside the clip.");
        }
    }
}

public sealed class TimelineTrack
{
    public TimelineTrack(string id, TimelineTrackKind kind, IEnumerable<TimelineClip> clips)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A track ID is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(clips);
        var orderedClips = clips.OrderBy(clip => clip.TimelineRange.StartFrame).ToArray();
        if (orderedClips.Any(clip => kind == TimelineTrackKind.Video ? !clip.IsVideo : !clip.IsAudio))
        {
            throw new ArgumentException("Track clips must match the track kind.", nameof(clips));
        }

        for (var index = 1; index < orderedClips.Length; index++)
        {
            if (orderedClips[index - 1].TimelineRange.EndFrameExclusive > orderedClips[index].TimelineRange.StartFrame)
            {
                throw new ArgumentException("Clips on one track cannot overlap.", nameof(clips));
            }
        }

        Id = id.Trim();
        Kind = kind;
        Clips = orderedClips;
    }

    public string Id { get; }

    public TimelineTrackKind Kind { get; }

    public IReadOnlyList<TimelineClip> Clips { get; }

    public TimelineClip? ClipAt(long timelineFrame) =>
        Clips.FirstOrDefault(clip => clip.TimelineRange.Contains(timelineFrame));
}

public sealed record TimelineSample(
    string TrackId,
    TimelineTrackKind TrackKind,
    MediaId MediaId,
    long? SourceFrame,
    long? SourceSample);

public sealed record TimelineSeekResult(
    long TimelineFrame,
    TimeSpan Position,
    IReadOnlyList<TimelineSample> Samples);

public sealed class TimelineGraph
{
    public TimelineGraph(RationalFrameRate projectFrameRate, IEnumerable<TimelineTrack> tracks)
    {
        ArgumentNullException.ThrowIfNull(tracks);
        var timelineTracks = tracks.ToArray();
        if (timelineTracks.GroupBy(track => track.Id, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Track IDs must be unique.", nameof(tracks));
        }

        ProjectFrameRate = projectFrameRate;
        Tracks = timelineTracks;
    }

    public RationalFrameRate ProjectFrameRate { get; }

    public IReadOnlyList<TimelineTrack> Tracks { get; }

    public TimelineSeekResult Seek(long timelineFrame)
    {
        if (timelineFrame < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timelineFrame));
        }

        var samples = Tracks
            .Select(track => (Track: track, Clip: track.ClipAt(timelineFrame)))
            .Where(item => item.Clip is not null)
            .Select(item => CreateSample(item.Track, item.Clip!, timelineFrame))
            .ToArray();
        return new TimelineSeekResult(timelineFrame, ProjectFrameRate.TimeAtFrame(timelineFrame), samples);
    }

    private TimelineSample CreateSample(TimelineTrack track, TimelineClip clip, long timelineFrame) =>
        track.Kind == TimelineTrackKind.Video
            ? new TimelineSample(track.Id, track.Kind, clip.MediaId, clip.SourceFrameAt(timelineFrame, ProjectFrameRate), null)
            : new TimelineSample(track.Id, track.Kind, clip.MediaId, null, clip.SourceSampleAt(timelineFrame, ProjectFrameRate));
}

public sealed class TimelineTransport
{
    private readonly TimelineGraph _graph;

    public TimelineTransport(TimelineGraph graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    public long CurrentFrame { get; private set; }

    public TimelineSeekResult Seek(long timelineFrame)
    {
        var result = _graph.Seek(timelineFrame);
        CurrentFrame = result.TimelineFrame;
        return result;
    }

    public TimelineSeekResult Scrub(TimeSpan position) => Seek(_graph.ProjectFrameRate.FrameIndexAt(position));

    public TimelineSeekResult Step(long frameDelta) => Seek(checked(CurrentFrame + frameDelta));
}
