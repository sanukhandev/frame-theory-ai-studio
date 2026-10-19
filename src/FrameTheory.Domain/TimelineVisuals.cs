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
        long transitionDurationFrames = 0,
        TimelineTransformKeyframes? animation = null,
        TimelineSpeedRamp? speedRamp = null,
        TimelineStabilizationSettings? stabilization = null)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (!clip.IsVideo)
        {
            throw new ArgumentException("A visual segment must contain a video clip.", nameof(clip));
        }

        if (transition is { } transitionValue && !Enum.IsDefined(transitionValue))
        {
            throw new ArgumentOutOfRangeException(nameof(transition));
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

        if (speedRamp is not null && (speedRamp.Points[0].Frame != clip.TimelineRange.StartFrame ||
            speedRamp.Points[^1].Frame != clip.TimelineRange.EndFrameExclusive))
        {
            throw new ArgumentException("Speed-ramp points must start at the clip and end at its exclusive boundary.", nameof(speedRamp));
        }

        Clip = clip;
        Transform = transform ?? new TimelineVideoTransform();
        Transition = transition;
        TransitionDurationFrames = transitionDurationFrames;
        if (animation is not null && (animation.Keyframes[0].Frame != clip.TimelineRange.StartFrame || animation.Keyframes[^1].Frame >= clip.TimelineRange.EndFrameExclusive))
        {
            throw new ArgumentException("Transform keyframes must fit within the segment timeline range.", nameof(animation));
        }

        Animation = animation;
        SpeedRamp = speedRamp;
        Stabilization = stabilization;
    }

    public TimelineClip Clip { get; }

    public TimelineVideoTransform Transform { get; }

    public TimelineTransitionKind? Transition { get; }

    public long TransitionDurationFrames { get; }

    public TimelineTransformKeyframes? Animation { get; }

    public TimelineSpeedRamp? SpeedRamp { get; }

    public TimelineStabilizationSettings? Stabilization { get; }
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
        var source = segments.ToArray();
        if (source.Any(segment => segment is null))
        {
            throw new ArgumentException("Visual segments cannot contain null entries.", nameof(segments));
        }

        var orderedSegments = source.OrderBy(segment => segment.Clip.TimelineRange.StartFrame).ToArray();
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
        Segments = Array.AsReadOnly(orderedSegments);
    }

    public string Id { get; }

    public int Order { get; }

    public IReadOnlyList<TimelineVideoSegment> Segments { get; }
}
