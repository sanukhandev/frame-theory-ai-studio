using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class FfmpegExecutableLocatorTests
{
    [Xunit.Fact]
    public void ResolveUsesExplicitBundleDirectoryForMediaTools()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-ffmpeg-locator-");
        try
        {
            var executable = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
            var path = Path.Combine(directory.FullName, executable);
            File.WriteAllText(path, "bundle");

            Xunit.Assert.Equal(path, FfmpegExecutableLocator.Resolve("ffmpeg", directory.FullName, directory.FullName));
            Xunit.Assert.Equal("dotnet", FfmpegExecutableLocator.Resolve("dotnet", directory.FullName, directory.FullName));
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
