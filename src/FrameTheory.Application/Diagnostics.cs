using System.Text.RegularExpressions;

namespace FrameTheory.Application;

public interface IDiagnosticsLog
{
    string LogPath { get; }

    void Record(string eventName, Exception? exception = null);
}

public sealed class FileDiagnosticsLog : IDiagnosticsLog
{
    private static readonly Regex SecretPattern = new("AIza[0-9A-Za-z_-]{20,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly object _sync = new();

    public FileDiagnosticsLog(string? logPath = null)
    {
        LogPath = string.IsNullOrWhiteSpace(logPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FrameTheory", "frame-theory.log")
            : Path.GetFullPath(logPath);
    }

    public string LogPath { get; }

    public void Record(string eventName, Exception? exception = null)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            var message = exception is null
                ? string.Empty
                : $" type={exception.GetType().Name} message={SecretPattern.Replace(exception.Message, "[REDACTED]").Replace(Environment.NewLine, " ")}";
            lock (_sync)
            {
                File.AppendAllText(LogPath, $"{DateTimeOffset.UtcNow:O} {eventName}{message}{Environment.NewLine}");
            }
        }
        catch
        {
        }
    }
}
