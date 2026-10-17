using System.Diagnostics;
using System.Runtime.InteropServices;
using FrameTheory.Application;

namespace FrameTheory.Platform;

public sealed class SystemCapabilityDetector : ICapabilityDetector
{
    private readonly string _ffmpegExecutable;
    private readonly string _ffprobeExecutable;
    private readonly string _probeArgument;

    public SystemCapabilityDetector(
        string ffmpegExecutable = "ffmpeg",
        string ffprobeExecutable = "ffprobe",
        string probeArgument = "-version")
    {
        _ffmpegExecutable = RequireExecutable(ffmpegExecutable, nameof(ffmpegExecutable));
        _ffprobeExecutable = RequireExecutable(ffprobeExecutable, nameof(ffprobeExecutable));
        _probeArgument = RequireExecutable(probeArgument, nameof(probeArgument));
    }

    public async Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entries = new List<CapabilityStatus>
        {
            new("Operating system", CapabilityState.Available, RuntimeInformation.OSDescription),
            await ProbeStorageAsync(cancellationToken),
            await ProbeExecutableAsync(_ffmpegExecutable, "FFmpeg", cancellationToken),
            await ProbeExecutableAsync(_ffprobeExecutable, "ffprobe", cancellationToken),
            new("GPU", CapabilityState.Unknown, "Native backend detection is not configured yet.")
        };

        return new CapabilityReport(
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            entries);
    }

    private async Task<CapabilityStatus> ProbeExecutableAsync(string executable, string displayName, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.StartInfo.ArgumentList.Add(_probeArgument);

            if (!process.Start())
            {
                return new CapabilityStatus(displayName, CapabilityState.Missing, "The process could not be started.");
            }

            var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var output = await Task.WhenAll(standardOutput, standardError);
            if (process.ExitCode != 0)
            {
                var detail = output.SelectMany(value => value.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    .Select(value => value.Trim())
                    .FirstOrDefault(value => value.Length > 0);
                return new CapabilityStatus(
                    displayName,
                    CapabilityState.Unknown,
                    $"Exited with code {process.ExitCode}." + (detail is null ? string.Empty : $" {detail}"));
            }

            var version = output.SelectMany(value => value.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                .Select(value => value.Trim())
                .FirstOrDefault(value => value.Length > 0);
            return new CapabilityStatus(displayName, CapabilityState.Available, version ?? "Executable responded.");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new CapabilityStatus(displayName, CapabilityState.Missing, "Not found on PATH.");
        }
    }

    private static string RequireExecutable(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("An executable or probe argument is required.", parameterName) : value.Trim();

    private static async Task<CapabilityStatus> ProbeStorageAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetTempPath();
        var path = Path.Combine(directory, $"frame-theory-probe-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(path, "probe", cancellationToken);
            return new CapabilityStatus("Storage", CapabilityState.Available, directory);
        }
        catch (IOException exception)
        {
            return new CapabilityStatus("Storage", CapabilityState.Missing, exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return new CapabilityStatus("Storage", CapabilityState.Missing, exception.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
