using System.Text.RegularExpressions;
using FrameTheory.Domain;

namespace FrameTheory.Application;

public interface IDiagnosticsLog
{
    string LogPath { get; }

    void Record(string eventName, Exception? exception = null);
}

public sealed class FileDiagnosticsLog : IDiagnosticsLog
{
    private static readonly Regex SecretPattern = new("AIza[0-9A-Za-z_-]{20,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex CredentialPattern = new(@"(?i)(?:api[-_ ]?key|authorization)\s*[:=]\s*(?:bearer\s+)?[^\s,;]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
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

            if (LocalPathSecurity.ContainsReparsePoint(directory) || LocalPathSecurity.ContainsReparsePoint(LogPath))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            var message = exception is null
                ? string.Empty
                : $" type={exception.GetType().Name} message={CredentialPattern.Replace(SecretPattern.Replace(exception.Message, "[REDACTED]"), "[REDACTED]").Replace(Environment.NewLine, " ")}";
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
