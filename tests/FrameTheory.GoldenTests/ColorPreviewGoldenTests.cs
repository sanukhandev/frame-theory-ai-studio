using FrameTheory.Color;
using FrameTheory.Fixtures;
using FrameTheory.Photo;
using System.Security.Cryptography;

namespace FrameTheory.GoldenTests;

public sealed class ColorPreviewGoldenTests
{
    [Xunit.Fact]
    public void SharedColorGradeProducesByteStablePreviewAndExportPixels()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-color-golden-");
        try
        {
            var fixture = FixtureGenerator.Generate(directory.FullName);
            var source = PpmRaster.Parse(File.ReadAllBytes(Path.Combine(fixture.DirectoryPath, "test_chart.ppm")));
            var grade = new ColorGrade(exposureEV: 0.75f, contrast: 0.2f, saturation: -0.15f, temperature: 0.1f);
            var actual = source.Apply(ColorGraph.ForSrgb(), grade).ToPpm();
            var actualHash = Convert.ToHexString(SHA256.HashData(actual)).ToLowerInvariant();

            Xunit.Assert.Equal("9895d4e1ae8babb59725375f4c01b98706193a85a2b75c6a9cacd661db4a90a5", actualHash);
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
