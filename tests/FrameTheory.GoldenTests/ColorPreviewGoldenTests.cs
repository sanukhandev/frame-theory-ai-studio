using FrameTheory.Color;
using FrameTheory.Fixtures;
using FrameTheory.Photo;

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
            var expectedPath = Path.Combine(directory.FullName, "expected.ppm");
            var actualPath = Path.Combine(directory.FullName, "actual.ppm");
            File.WriteAllBytes(expectedPath, source.Apply(ColorGraph.ForSrgb(), grade).ToPpm());
            File.WriteAllBytes(actualPath, source.Apply(ColorGraph.ForSrgb(), grade).ToPpm());

            var comparison = GoldenComparer.Compare(expectedPath, actualPath);

            Xunit.Assert.True(comparison.IsMatch);
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
