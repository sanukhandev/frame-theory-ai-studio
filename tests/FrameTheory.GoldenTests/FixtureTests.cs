using FrameTheory.Fixtures;

namespace FrameTheory.GoldenTests;

public sealed class FixtureTests
{
    [Xunit.Fact]
    public void GeneratedFixturesAreByteStableAndDetectChanges()
    {
        var firstDirectory = Directory.CreateTempSubdirectory("frame-theory-fixtures-");
        var secondDirectory = Directory.CreateTempSubdirectory("frame-theory-fixtures-");
        try
        {
            var first = FixtureGenerator.Generate(firstDirectory.FullName);
            var second = FixtureGenerator.Generate(secondDirectory.FullName);

            foreach (var fileName in first.Files.Select(Path.GetFileName))
            {
                var comparison = GoldenComparer.Compare(
                    Path.Combine(first.DirectoryPath, fileName!),
                    Path.Combine(second.DirectoryPath, fileName!));
                Xunit.Assert.True(comparison.IsMatch, $"Fixture mismatch in {fileName}.");
            }

            var changedPath = Path.Combine(second.DirectoryPath, "test_chart.ppm");
            var changed = File.ReadAllBytes(changedPath);
            changed[^1]++;
            File.WriteAllBytes(changedPath, changed);

            var mismatch = GoldenComparer.Compare(
                Path.Combine(first.DirectoryPath, "test_chart.ppm"),
                changedPath);
            Xunit.Assert.False(mismatch.IsMatch);
            Xunit.Assert.NotNull(mismatch.FirstDifferenceIndex);
        }
        finally
        {
            firstDirectory.Delete(true);
            secondDirectory.Delete(true);
        }
    }
}
