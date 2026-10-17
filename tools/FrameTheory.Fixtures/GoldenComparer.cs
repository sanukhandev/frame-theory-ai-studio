namespace FrameTheory.Fixtures;

public sealed record GoldenComparison(bool IsMatch, int? FirstDifferenceIndex, byte? Expected, byte? Actual);

public static class GoldenComparer
{
    public static GoldenComparison Compare(string expectedPath, string actualPath)
    {
        var expected = File.ReadAllBytes(expectedPath);
        var actual = File.ReadAllBytes(actualPath);
        var sharedLength = Math.Min(expected.Length, actual.Length);
        for (var index = 0; index < sharedLength; index++)
        {
            if (expected[index] != actual[index])
            {
                return new GoldenComparison(false, index, expected[index], actual[index]);
            }
        }

        return expected.Length == actual.Length
            ? new GoldenComparison(true, null, null, null)
            : new GoldenComparison(false, sharedLength,
                expected.Length > sharedLength ? expected[sharedLength] : null,
                actual.Length > sharedLength ? actual[sharedLength] : null);
    }
}
