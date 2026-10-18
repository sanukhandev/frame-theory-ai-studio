namespace FrameTheory.Media;

public static class FfmpegExecutableLocator
{
    public static string Resolve(string executable, string? bundledDirectory = null, string? baseDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        if (Path.IsPathRooted(executable) || !IsMediaExecutable(executable))
        {
            return executable;
        }

        var fileName = OperatingSystem.IsWindows() && !executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? executable + ".exe"
            : executable;
        var directories = new[]
        {
            bundledDirectory,
            Environment.GetEnvironmentVariable("FRAME_THEORY_FFMPEG_DIR"),
            baseDirectory ?? AppContext.BaseDirectory
        };
        foreach (var directory in directories.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var candidate = Path.Combine(directory!, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return executable;
    }

    private static bool IsMediaExecutable(string executable) =>
        string.Equals(executable, "ffmpeg", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(executable, "ffmpeg.exe", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(executable, "ffprobe", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(executable, "ffprobe.exe", StringComparison.OrdinalIgnoreCase);
}
