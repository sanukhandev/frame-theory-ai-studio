using FrameTheory.Color;

namespace FrameTheory.UnitTests;

public sealed class CubeLutTests
{
    [Xunit.Fact]
    public void IdentityLutRoundTripsAndInterpolates()
    {
        var lut = CubeLut.Identity(2);
        var color = new RgbColor(0.2f, 0.5f, 0.8f);
        var transformed = lut.Apply(color);
        var roundTripped = CubeLut.Parse(lut.ToCube()).Apply(color);

        Xunit.Assert.InRange(transformed.Red, color.Red - 0.00001f, color.Red + 0.00001f);
        Xunit.Assert.InRange(transformed.Green, color.Green - 0.00001f, color.Green + 0.00001f);
        Xunit.Assert.InRange(transformed.Blue, color.Blue - 0.00001f, color.Blue + 0.00001f);
        Xunit.Assert.InRange(roundTripped.Red, color.Red - 0.00001f, color.Red + 0.00001f);
    }

    [Xunit.Fact]
    public void KnownTwoPointLutProducesTrilinearResult()
    {
        var values = new List<RgbColor>();
        for (var red = 0; red < 2; red++)
        {
            for (var green = 0; green < 2; green++)
            {
                for (var blue = 0; blue < 2; blue++)
                {
                    values.Add(new RgbColor(red, green, blue));
                }
            }
        }

        var lut = new CubeLut(2, values);
        var transformed = lut.Apply(new RgbColor(0.25f, 0.5f, 0.75f));

        Xunit.Assert.InRange(transformed.Red, 0.24999f, 0.25001f);
        Xunit.Assert.InRange(transformed.Green, 0.49999f, 0.50001f);
        Xunit.Assert.InRange(transformed.Blue, 0.74999f, 0.75001f);
    }

    [Xunit.Fact]
    public void ParserRejectsUnsupportedOrOversizedLuts()
    {
        Xunit.Assert.Throws<FormatException>(() => CubeLut.Parse("LUT_1D_SIZE 2\n0 0 0\n0 0 0"));
        Xunit.Assert.Throws<FormatException>(() => CubeLut.Parse("LUT_3D_SIZE 2\n0 0 0"));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => CubeLut.Identity(66));
        Xunit.Assert.Throws<FormatException>(() => CubeLut.Parse("LUT_3D_SIZE 2\nDOMAIN_MIN 0 0 1\nDOMAIN_MAX 1 1 0" + Environment.NewLine + string.Join(Environment.NewLine, Enumerable.Repeat("0 0 0", 8))));
    }

    [Xunit.Fact]
    public void ParserRejectsDuplicateStructuralDirectives()
    {
        Xunit.Assert.Throws<FormatException>(() => CubeLut.Parse("LUT_3D_SIZE 2\nLUT_3D_SIZE 2"));
        Xunit.Assert.Throws<FormatException>(() => CubeLut.Parse("LUT_3D_SIZE 2\nDOMAIN_MIN 0 0 0\nDOMAIN_MIN 0 0 0"));
    }
}
