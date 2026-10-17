namespace FrameTheory.Domain;

public enum TimelineTransitionKind
{
    CrossDissolve,
    WipeLeft
}

public sealed record TimelineVideoTransform
{
    public TimelineVideoTransform(double opacity = 1, double scale = 1, int offsetX = 0, int offsetY = 0)
    {
        if (double.IsNaN(opacity) || double.IsInfinity(opacity) || opacity is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }

        if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale));
        }

        if (offsetX < 0 || offsetY < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offsetX));
        }

        Opacity = opacity;
        Scale = scale;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }

    public double Opacity { get; }

    public double Scale { get; }

    public int OffsetX { get; }

    public int OffsetY { get; }
}

public sealed record TimelineVideoSegment
{
    public TimelineVideoSegment(
        TimelineClip clip,
        TimelineVideoTransform? transform = null,
        TimelineTransitionKind? transition = null,
        long transitionDurationFrames = 0)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (!clip.IsVideo)
        {
            throw new ArgumentException("A visual segment must contain a video clip.", nameof(clip));
        }

        if (transition is null && transitionDurationFrames != 0)
        {
            throw new ArgumentException("A transition duration requires a transition.", nameof(transitionDurationFrames));
        }

        if (transitionDurationFrames <= 0 || transitionDurationFrames >= clip.TimelineRange.Length)
        {
            if (transition is not null)
            {
                throw new ArgumentOutOfRangeException(nameof(transitionDurationFrames));
            }
        }

        Clip = clip;
        Transform = transform ?? new TimelineVideoTransform();
        Transition = transition;
        TransitionDurationFrames = transitionDurationFrames;
    }

    public TimelineClip Clip { get; }

    public TimelineVideoTransform Transform { get; }

    public TimelineTransitionKind? Transition { get; }

    public long TransitionDurationFrames { get; }
}

public sealed record TimelineVideoLayer
{
    public TimelineVideoLayer(string id, int order, IEnumerable<TimelineVideoSegment> segments)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A layer ID is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(segments);
        var orderedSegments = segments.OrderBy(segment => segment.Clip.TimelineRange.StartFrame).ToArray();
        for (var index = 0; index < orderedSegments.Length; index++)
        {
            if (index > 0 && orderedSegments[index - 1].Clip.TimelineRange.EndFrameExclusive > orderedSegments[index].Clip.TimelineRange.StartFrame)
            {
                throw new ArgumentException("Visual segments on one layer cannot overlap.", nameof(segments));
            }

            if (index == 0 && orderedSegments[index].Transition is not null)
            {
                throw new ArgumentException("The first visual segment cannot have an incoming transition.", nameof(segments));
            }
        }

        Id = id.Trim();
        Order = order;
        Segments = orderedSegments;
    }

    public string Id { get; }

    public int Order { get; }

    public IReadOnlyList<TimelineVideoSegment> Segments { get; }
}
