using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class MotionAnalysisTests
{
    [Xunit.Fact]
    public void TrackerFindsDeterministicTranslationAndConfidence()
    {
        var previous = Frame(0, 12, 12, 0);
        var current = Frame(1, 12, 12, 2);
        var vectors = new TranslationMotionTracker(searchRadius: 3, sampleStride: 2).Track([previous, current]);

        Xunit.Assert.Single(vectors);
        Xunit.Assert.Equal(2, vectors[0].OffsetX);
        Xunit.Assert.Equal(0, vectors[0].OffsetY);
        Xunit.Assert.True(vectors[0].Confidence > 0.99f);
    }

    [Xunit.Fact]
    public void StabilizationPlannerSmoothsAndBoundsCompensation()
    {
        var plan = StabilizationPlanner.Plan(
            [new MotionVector(1, 10, 0, 1), new MotionVector(2, 10, 0, 1)],
            smoothingRadius: 1,
            maximumCompensation: 3);

        Xunit.Assert.Equal(2, plan.Transforms.Count);
        Xunit.Assert.Equal(3, plan.Transforms[0].OffsetX);
        Xunit.Assert.Equal(-3, plan.Transforms[1].OffsetX);
        Xunit.Assert.All(plan.Transforms, transform => Xunit.Assert.InRange(transform.OffsetX, -3, 3));
    }

    [Xunit.Fact]
    public void MotionAnalysisRejectsInsufficientOrMismatchedFrames()
    {
        var frame = Frame(0, 8, 8, 0);
        Xunit.Assert.Throws<ArgumentException>(() => new TranslationMotionTracker().Track([frame]));
        Xunit.Assert.Throws<ArgumentException>(() => new TranslationMotionTracker().Track([frame, Frame(1, 7, 8, 0)]));
    }

    [Xunit.Fact]
    public void TrackerFindsMotionWhenFramesAreSmallerThanTheDefaultSearchRadius()
    {
        var vectors = new TranslationMotionTracker().Track([Frame(0, 8, 8, 0), Frame(1, 8, 8, 1)]);

        Xunit.Assert.Single(vectors);
        Xunit.Assert.Equal(1, vectors[0].OffsetX);
        Xunit.Assert.Equal(0, vectors[0].OffsetY);
        Xunit.Assert.True(vectors[0].Confidence > 0.99f);
    }

    [Xunit.Fact]
    public void MotionAnalysisValidationIdentifiesInvalidArguments()
    {
        Xunit.Assert.Equal("confidence", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new MotionVector(0, 0, 0, 2)).ParamName);
        Xunit.Assert.Equal("sampleStride", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new TranslationMotionTracker(sampleStride: 33)).ParamName);
        Xunit.Assert.Equal("maximumCompensation", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => StabilizationPlanner.Plan([], maximumCompensation: 4097)).ParamName);
        Xunit.Assert.Equal("width", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new MotionFrame(0, 0, 1, new byte[] { 0, 0, 0 })).ParamName);
        Xunit.Assert.Equal("height", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new MotionFrame(0, 1, 0, new byte[] { 0, 0, 0 })).ParamName);
    }

    [Xunit.Fact]
    public void StabilizationPlanSnapshotsItsTransforms()
    {
        var transforms = new List<StabilizationTransform> { new(1, 2, 3) };
        var plan = new StabilizationPlan(transforms);

        transforms.Clear();

        Xunit.Assert.Single(plan.Transforms);
        Xunit.Assert.Equal(2, plan.Transforms[0].OffsetX);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<StabilizationTransform>>(plan.Transforms).IsReadOnly);
    }

    private static MotionFrame Frame(int index, int width, int height, int shiftX)
    {
        var pixels = new byte[width * height * 3];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sourceX = Math.Clamp(x - shiftX, 0, width - 1);
                var value = (byte)((sourceX * 17 + y * 11) % 256);
                var offset = (y * width + x) * 3;
                pixels[offset] = value;
                pixels[offset + 1] = value;
                pixels[offset + 2] = value;
            }
        }

        return new MotionFrame(index, width, height, pixels);
    }
}
