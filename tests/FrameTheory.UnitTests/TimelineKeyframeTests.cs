using FrameTheory.Domain;

namespace FrameTheory.UnitTests;

public sealed class TimelineKeyframeTests
{
    [Xunit.Fact]
    public void KeyframesRejectUndefinedEasingValues()
    {
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineTransformKeyframe(
            0, new TimelineVideoTransform(), (TimelineEasing)99));
    }

    [Xunit.Fact]
    public void EvaluatesInterpolatedTransformsWithEasingAndClampsBounds()
    {
        var animation = new TimelineTransformKeyframes([
            new TimelineTransformKeyframe(10, new TimelineVideoTransform(opacity: 0, scale: 1, offsetX: 0, offsetY: 0), TimelineEasing.EaseIn),
            new TimelineTransformKeyframe(20, new TimelineVideoTransform(opacity: 1, scale: 2, offsetX: 10, offsetY: 6))
        ]);

        var middle = animation.Evaluate(15);

        Xunit.Assert.Equal(0.25, middle.Opacity, precision: 6);
        Xunit.Assert.Equal(1.25, middle.Scale, precision: 6);
        Xunit.Assert.Equal(3, middle.OffsetX);
        Xunit.Assert.Equal(2, middle.OffsetY);
        Xunit.Assert.Equal(0, animation.Evaluate(0).Opacity);
        Xunit.Assert.Equal(1, animation.Evaluate(100).Opacity);
    }

    [Xunit.Fact]
    public void PresetsProduceBoundedKeyframesAndFfmpegExpressions()
    {
        var baseTransform = new TimelineVideoTransform(opacity: 0.8, scale: 1.5, offsetX: 2, offsetY: 3);
        var animation = TimelineTransformKeyframes.FromPreset(TimelineAnimationPreset.FadeIn, 0, 30, baseTransform);

        Xunit.Assert.Equal(0, animation.Evaluate(0).Opacity);
        Xunit.Assert.Equal(0.8, animation.Evaluate(29).Opacity);
        Xunit.Assert.Contains("if(lt(n,29)", animation.ToFfmpegExpression(value => value.Opacity, 0));
        Xunit.Assert.Throws<ArgumentException>(() => new TimelineTransformKeyframes([
            new TimelineTransformKeyframe(1, baseTransform),
            new TimelineTransformKeyframe(1, baseTransform)]));
    }

    [Xunit.Fact]
    public void KeyframesExposeAnImmutableSnapshot()
    {
        var source = new List<TimelineTransformKeyframe> { new(2, new TimelineVideoTransform()) };
        var animation = new TimelineTransformKeyframes(source);

        source.Clear();

        Xunit.Assert.Single(animation.Keyframes);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<TimelineTransformKeyframe>>(animation.Keyframes).IsReadOnly);
    }

    [Xunit.Fact]
    public void KeyframesRejectNullEntries()
    {
        Xunit.Assert.Throws<ArgumentException>(() => new TimelineTransformKeyframes([
            null!,
            new TimelineTransformKeyframe(2, new TimelineVideoTransform())]));
    }
}
