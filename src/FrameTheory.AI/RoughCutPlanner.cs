namespace FrameTheory.AI;

public sealed class RoughCutSource
{
    public RoughCutSource(string mediaId, long timelineStartFrame, long durationFrames, IReadOnlyList<float> waveformPeaks)
    {
        if (string.IsNullOrWhiteSpace(mediaId) || mediaId.Length > 128)
        {
            throw new ArgumentException("A bounded media ID is required.", nameof(mediaId));
        }

        if (timelineStartFrame < 0 || durationFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timelineStartFrame));
        }

        ArgumentNullException.ThrowIfNull(waveformPeaks);
        if (waveformPeaks.Any(peak => float.IsNaN(peak) || float.IsInfinity(peak) || peak < 0 || peak > 1))
        {
            throw new ArgumentException("Waveform peaks must be finite values from zero through one.", nameof(waveformPeaks));
        }

        MediaId = mediaId;
        TimelineStartFrame = timelineStartFrame;
        DurationFrames = durationFrames;
        WaveformPeaks = waveformPeaks.ToArray();
    }

    public string MediaId { get; }

    public long TimelineStartFrame { get; }

    public long DurationFrames { get; }

    public IReadOnlyList<float> WaveformPeaks { get; }
}

public sealed record RoughCutSegment(
    string MediaId,
    long OriginalStartFrame,
    long StartFrame,
    long DurationFrames,
    double ActivityScore);

public sealed class RoughCutPlan
{
    internal RoughCutPlan(IReadOnlyList<RoughCutSegment> segments)
    {
        Segments = segments;
        TotalFrames = segments.Sum(segment => segment.DurationFrames);
    }

    public IReadOnlyList<RoughCutSegment> Segments { get; }

    public long TotalFrames { get; }

    public string Summary => Segments.Count == 0
        ? "No clips selected for rough cut."
        : $"Rough cut ordered {Segments.Count} clip(s) by measured waveform activity; {TotalFrames} timeline frame(s) remain editable.";
}

public static class RoughCutPlanner
{
    private const int MaximumSources = 32;

    public static RoughCutPlan Create(IReadOnlyList<RoughCutSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        if (sources.Count is < 2 or > MaximumSources)
        {
            throw new ArgumentException($"A multi-clip rough cut requires two through {MaximumSources} sources.", nameof(sources));
        }

        if (sources.Select(source => source.MediaId).Distinct(StringComparer.Ordinal).Count() != sources.Count)
        {
            throw new ArgumentException("Rough-cut sources must have unique media IDs.", nameof(sources));
        }

        var ordered = sources
            .Select((source, index) => new
            {
                Source = source,
                Index = index,
                Score = ActivityScore(source.WaveformPeaks)
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Index)
            .ToArray();

        var timelineFrame = 0L;
        var segments = new List<RoughCutSegment>(ordered.Length);
        foreach (var item in ordered)
        {
            segments.Add(new RoughCutSegment(
                item.Source.MediaId,
                item.Source.TimelineStartFrame,
                timelineFrame,
                item.Source.DurationFrames,
                item.Score));
            timelineFrame = checked(timelineFrame + item.Source.DurationFrames);
        }

        return new RoughCutPlan(segments);
    }

    private static double ActivityScore(IReadOnlyList<float> peaks)
    {
        if (peaks.Count == 0)
        {
            return 0;
        }

        var energy = peaks.Average(peak => (double)peak);
        var onset = peaks.Count == 1
            ? 0
            : peaks.Zip(peaks.Skip(1), (previous, current) => Math.Max(0, current - previous)).Average();
        return energy * 0.75 + onset * 0.25;
    }
}
