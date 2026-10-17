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
}
