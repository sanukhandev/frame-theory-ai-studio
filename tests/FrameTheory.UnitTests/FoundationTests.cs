using System.Reflection;

namespace FrameTheory.UnitTests;

public sealed class FoundationTests
{
    [Xunit.Fact]
    public void FoundationAssembliesAreLoadable()
    {
        var expected = new[]
        {
            "FrameTheory.Application",
            "FrameTheory.AI",
            "FrameTheory.Captions",
            "FrameTheory.Color",
            "FrameTheory.Domain",
            "FrameTheory.Media",
            "FrameTheory.Photo",
            "FrameTheory.Platform",
            "FrameTheory.Storage"
        };

        var loaded = expected.Select(Assembly.Load).Select(assembly => assembly.GetName().Name).ToArray();

        Xunit.Assert.Equal(expected, loaded);
    }
}
