namespace FrameTheory.Fixtures;

public static class Program
{
    public static void Main(string[] args)
    {
        var outputDirectory = args.FirstOrDefault() ?? Path.Combine("tools", "fixtures", "generated");
        var fixtureSet = FixtureGenerator.Generate(outputDirectory);
        Console.WriteLine($"Generated {fixtureSet.Files.Count} deterministic fixtures in {fixtureSet.DirectoryPath}.");
    }
}
