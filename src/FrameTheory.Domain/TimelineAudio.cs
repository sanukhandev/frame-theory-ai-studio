namespace FrameTheory.Domain;

public sealed record TimelineAudioSegment
{
    public TimelineAudioSegment(
        TimelineClip clip,
        double gainDb = 0,
        bool muted = false,
        long fadeInFrames = 0,
        long fadeOutFrames = 0)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (!clip.IsAudio)
        {
            throw new ArgumentException("An audio segment must contain an audio clip.", nameof(clip));
        }

        if (double.IsNaN(gainDb) || double.IsInfinity(gainDb) || gainDb is < -60 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(gainDb));
        }

        if (fadeInFrames < 0 || fadeOutFrames < 0 || fadeInFrames >= clip.TimelineRange.Length || fadeOutFrames >= clip.TimelineRange.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(fadeInFrames));
        }

        Clip = clip;
        GainDb = gainDb;
        Muted = muted;
        FadeInFrames = fadeInFrames;
        FadeOutFrames = fadeOutFrames;
    }

    public TimelineClip Clip { get; }

    public double GainDb { get; }

    public bool Muted { get; }

    public long FadeInFrames { get; }

    public long FadeOutFrames { get; }
}

public sealed record TimelineAudioLayer
{
    public TimelineAudioLayer(string id, int order, IEnumerable<TimelineAudioSegment> segments)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("An audio layer ID is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(segments);
        var orderedSegments = segments.OrderBy(segment => segment.Clip.TimelineRange.StartFrame).ToArray();
        for (var index = 1; index < orderedSegments.Length; index++)
        {
            if (orderedSegments[index - 1].Clip.TimelineRange.EndFrameExclusive > orderedSegments[index].Clip.TimelineRange.StartFrame)
            {
                throw new ArgumentException("Audio segments on one layer cannot overlap.", nameof(segments));
            }
        }

        Id = id.Trim();
        Order = order;
        Segments = orderedSegments;
    }

    public string Id { get; }

    public int Order { get; }

    public IReadOnlyList<TimelineAudioSegment> Segments { get; }
}
