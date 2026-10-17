using System.Globalization;

namespace FrameTheory.Domain;

public enum TimelineEasing
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut
}

public enum TimelineAnimationPreset
{
    FadeIn,
    FadeOut,
    PushIn,
    PullOut
}

public sealed record TimelineTransformKeyframe
{
    public TimelineTransformKeyframe(long frame, TimelineVideoTransform value, TimelineEasing easing = TimelineEasing.Linear)
    {
        if (frame < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frame));
        }

        ArgumentNullException.ThrowIfNull(value);
        Frame = frame;
        Value = value;
        Easing = easing;
    }

    public long Frame { get; }

    public TimelineVideoTransform Value { get; }

    public TimelineEasing Easing { get; }
}

public sealed class TimelineTransformKeyframes
{
    public TimelineTransformKeyframes(IEnumerable<TimelineTransformKeyframe> keyframes)
    {
        ArgumentNullException.ThrowIfNull(keyframes);
        var ordered = keyframes.OrderBy(keyframe => keyframe.Frame).ToArray();
        if (ordered.Length == 0 || ordered.Select(keyframe => keyframe.Frame).Distinct().Count() != ordered.Length)
        {
            throw new ArgumentException("At least one keyframe with unique frame positions is required.", nameof(keyframes));
        }

        Keyframes = ordered;
    }

    public IReadOnlyList<TimelineTransformKeyframe> Keyframes { get; }

    public TimelineVideoTransform Evaluate(long frame)
    {
        if (frame <= Keyframes[0].Frame)
        {
            return Keyframes[0].Value;
        }

        if (frame >= Keyframes[^1].Frame)
        {
            return Keyframes[^1].Value;
        }

        var rightIndex = Enumerable.Range(1, Keyframes.Count - 1).First(index => Keyframes[index].Frame >= frame);
        var left = Keyframes[rightIndex - 1];
        var right = Keyframes[rightIndex];
        var progress = (double)(frame - left.Frame) / (right.Frame - left.Frame);
        var eased = ApplyEasing(progress, left.Easing);
        return Interpolate(left.Value, right.Value, eased);
    }

    public string ToFfmpegExpression(Func<TimelineVideoTransform, double> selector, long segmentStartFrame, string frameVariable = "n")
    {
        ArgumentNullException.ThrowIfNull(selector);
        if (segmentStartFrame < 0 || Keyframes[0].Frame < segmentStartFrame)
        {
            throw new ArgumentOutOfRangeException(nameof(segmentStartFrame));
        }

        var expression = Format(selector(Keyframes[^1].Value));
        for (var index = Keyframes.Count - 2; index >= 0; index--)
        {
            var left = Keyframes[index];
            var right = Keyframes[index + 1];
            var leftFrame = left.Frame - segmentStartFrame;
            var rightFrame = right.Frame - segmentStartFrame;
            var duration = rightFrame - leftFrame;
            var progress = $"(({frameVariable}-{leftFrame})/{duration})";
            var eased = EasingExpression(progress, left.Easing);
            var leftValue = Format(selector(left.Value));
            var rightValue = Format(selector(right.Value));
            var interpolated = $"({leftValue}+({rightValue}-{leftValue})*({eased}))";
            expression = $"if(lt({frameVariable},{rightFrame}),{interpolated},{expression})";
        }

        var firstFrame = Keyframes[0].Frame - segmentStartFrame;
        if (firstFrame > 0)
        {
            expression = $"if(lt({frameVariable},{firstFrame}),{Format(selector(Keyframes[0].Value))},{expression})";
        }

        return expression;
    }

    public static TimelineTransformKeyframes FromPreset(
        TimelineAnimationPreset preset,
        long startFrame,
        long endFrameExclusive,
        TimelineVideoTransform baseTransform)
    {
        if (startFrame < 0 || endFrameExclusive <= startFrame)
        {
            throw new ArgumentOutOfRangeException(nameof(startFrame));
        }

        ArgumentNullException.ThrowIfNull(baseTransform);
        var start = preset switch
        {
            TimelineAnimationPreset.FadeIn => new TimelineVideoTransform(0, baseTransform.Scale, baseTransform.OffsetX, baseTransform.OffsetY),
            TimelineAnimationPreset.FadeOut => baseTransform,
            TimelineAnimationPreset.PushIn => new TimelineVideoTransform(baseTransform.Opacity, baseTransform.Scale * 0.8, baseTransform.OffsetX, baseTransform.OffsetY),
            TimelineAnimationPreset.PullOut => baseTransform,
            _ => throw new ArgumentOutOfRangeException(nameof(preset))
        };
        var end = preset switch
        {
            TimelineAnimationPreset.FadeIn => baseTransform,
            TimelineAnimationPreset.FadeOut => new TimelineVideoTransform(0, baseTransform.Scale, baseTransform.OffsetX, baseTransform.OffsetY),
            TimelineAnimationPreset.PushIn => baseTransform,
            TimelineAnimationPreset.PullOut => new TimelineVideoTransform(baseTransform.Opacity, baseTransform.Scale * 0.8, baseTransform.OffsetX, baseTransform.OffsetY),
            _ => throw new ArgumentOutOfRangeException(nameof(preset))
        };
        return new TimelineTransformKeyframes([
            new TimelineTransformKeyframe(startFrame, start, TimelineEasing.EaseInOut),
            new TimelineTransformKeyframe(endFrameExclusive - 1, end, TimelineEasing.EaseInOut)
        ]);
    }

    private static TimelineVideoTransform Interpolate(TimelineVideoTransform left, TimelineVideoTransform right, double progress) =>
        new(
            left.Opacity + (right.Opacity - left.Opacity) * progress,
            left.Scale + (right.Scale - left.Scale) * progress,
            (int)Math.Round(left.OffsetX + (right.OffsetX - left.OffsetX) * progress, MidpointRounding.AwayFromZero),
            (int)Math.Round(left.OffsetY + (right.OffsetY - left.OffsetY) * progress, MidpointRounding.AwayFromZero));

    private static double ApplyEasing(double progress, TimelineEasing easing) => easing switch
    {
        TimelineEasing.Linear => progress,
        TimelineEasing.EaseIn => progress * progress,
        TimelineEasing.EaseOut => 1 - (1 - progress) * (1 - progress),
        TimelineEasing.EaseInOut => progress < 0.5 ? 2 * progress * progress : 1 - Math.Pow(-2 * progress + 2, 2) / 2,
        _ => throw new ArgumentOutOfRangeException(nameof(easing))
    };

    private static string EasingExpression(string progress, TimelineEasing easing) => easing switch
    {
        TimelineEasing.Linear => progress,
        TimelineEasing.EaseIn => $"pow({progress},2)",
        TimelineEasing.EaseOut => $"(1-pow(1-{progress},2))",
        TimelineEasing.EaseInOut => $"if(lt({progress},0.5),2*pow({progress},2),1-pow(-2*{progress}+2,2)/2)",
        _ => throw new ArgumentOutOfRangeException(nameof(easing))
    };

    private static string Format(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
