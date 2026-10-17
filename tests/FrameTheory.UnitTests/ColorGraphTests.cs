using FrameTheory.Color;

namespace FrameTheory.UnitTests;

public sealed class ColorGraphTests
{
    [Xunit.Fact]
    public void SrgbaPipelinePreservesColorAndAlpha()
    {
        var color = new RgbColor(0.1f, 0.5f, 0.9f, 0.25f);
        var transformed = new ColorPipeline(ColorGraph.ForSrgb()).Transform(color);

        Xunit.Assert.InRange(transformed.Red, color.Red - 0.00001f, color.Red + 0.00001f);
        Xunit.Assert.InRange(transformed.Green, color.Green - 0.00001f, color.Green + 0.00001f);
        Xunit.Assert.InRange(transformed.Blue, color.Blue - 0.00001f, color.Blue + 0.00001f);
        Xunit.Assert.Equal(color.Alpha, transformed.Alpha);
    }

    [Xunit.Fact]
    public void Rec709AndSrgbUseExplicitTransferCurves()
    {
        var color = new RgbColor(0.5f, 0.5f, 0.5f);
        var srgb = new ColorPipeline(new ColorGraph(ColorProfiles.SRgb, ColorProfiles.LinearRec709, ColorProfiles.Rec709)).Transform(color);
        var rec709 = new ColorPipeline(ColorGraph.ForRec709Video()).Transform(color);

        Xunit.Assert.NotEqual(srgb.Red, rec709.Red);
        Xunit.Assert.InRange(srgb.Red, 0, 1);
        Xunit.Assert.InRange(rec709.Red, 0, 1);
    }

    [Xunit.Fact]
    public void GraphRequiresLinearWorkingProfileAndBoundedChannels()
    {
        Xunit.Assert.Throws<ArgumentException>(() => new ColorGraph(ColorProfiles.SRgb, ColorProfiles.SRgb, ColorProfiles.SRgb));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new RgbColor(-0.01f, 0, 0));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new RgbColor(0, 0, float.NaN));
    }

    [Xunit.Fact]
    public void GradeAppliesExposureContrastSaturationAndWhiteBalanceInLinearSpace()
    {
        var pipeline = new ColorPipeline(ColorGraph.ForSrgb());
        var input = new RgbColor(0.4f, 0.2f, 0.1f);
        var graded = pipeline.Transform(input, new ColorGrade(
            exposureEV: 1,
            contrast: 0.25f,
            saturation: -0.5f,
            temperature: 0.5f,
            tint: -0.5f));

        Xunit.Assert.NotEqual(input, graded);
        Xunit.Assert.InRange(graded.Red, 0, 1);
        Xunit.Assert.InRange(graded.Green, 0, 1);
        Xunit.Assert.InRange(graded.Blue, 0, 1);
    }

    [Xunit.Fact]
    public void GradeClipsHighlightsAndRejectsOutOfRangeValues()
    {
        var pipeline = new ColorPipeline(ColorGraph.ForSrgb());
        var clipped = pipeline.Transform(new RgbColor(1, 1, 1), new ColorGrade(exposureEV: 8));

        Xunit.Assert.Equal(1, clipped.Red);
        Xunit.Assert.Equal(1, clipped.Green);
        Xunit.Assert.Equal(1, clipped.Blue);
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new ColorGrade(exposureEV: 9));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new ColorGrade(temperature: -2));
    }

    [Xunit.Fact]
    public void ReferenceMatcherReturnsNeutralGradeForMatchingSamples()
    {
        var grade = ColorReferenceMatcher.Match([64, 96, 128, 192, 160, 128], [64, 96, 128, 192, 160, 128]);

        Xunit.Assert.InRange(grade.ExposureEV, -0.001f, 0.001f);
        Xunit.Assert.InRange(grade.Saturation, -0.001f, 0.001f);
        Xunit.Assert.InRange(grade.Temperature, -0.001f, 0.001f);
        Xunit.Assert.InRange(grade.Tint, -0.001f, 0.001f);
    }

    [Xunit.Fact]
    public void ReferenceMatcherDetectsBoundedBrightnessAndWhiteBalanceDifferences()
    {
        var grade = ColorReferenceMatcher.Match([32, 32, 32, 48, 48, 48], [160, 120, 80, 160, 120, 80]);

        Xunit.Assert.True(grade.ExposureEV > 0);
        Xunit.Assert.True(grade.Temperature > 0);
        Xunit.Assert.InRange(grade.ExposureEV, -4, 4);
        Xunit.Assert.InRange(grade.Saturation, -1, 1);
        Xunit.Assert.InRange(grade.Temperature, -1, 1);
        Xunit.Assert.InRange(grade.Tint, -1, 1);
    }

    [Xunit.Fact]
    public void ReferenceMatcherRejectsIncompleteRgbData()
    {
        Xunit.Assert.Throws<ArgumentException>(() => ColorReferenceMatcher.Match([1, 2], [1, 2, 3]));
        Xunit.Assert.Throws<ArgumentException>(() => ColorReferenceMatcher.Match([1, 2, 3], []));
    }
}
