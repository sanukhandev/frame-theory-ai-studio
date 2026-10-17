using System.Diagnostics;
using System.Runtime.InteropServices;
using FrameTheory.Application;

namespace FrameTheory.Platform;

public sealed class SystemCapabilityDetector : ICapabilityDetector
{
    public async Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entries = new List<CapabilityStatus>
        {
            new("Operating system", CapabilityState.Available, RuntimeInformation.OSDescription),
            await ProbeStorageAsync(cancellationToken),
            await ProbeExecutableAsync("ffmpeg", cancellationToken),
            await ProbeExecutableAsync("ffprobe", cancellationToken),
            new("GPU", CapabilityState.Unknown, "Native backend detection is not configured yet.")
        };

        return new CapabilityReport(
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            entries);
    }

    private static async Task<CapabilityStatus> ProbeExecutableAsync(string executable, CancellationToken cancellationToken)
    {
        var displayName = executable.Equals("ffmpeg", StringComparison.OrdinalIgnoreCase) ? "FFmpeg" : "ffprobe";
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.StartInfo.ArgumentList.Add("-version");

            if (!process.Start())
            {
                return new CapabilityStatus(displayName, CapabilityState.Missing, "The process could not be started.");
            }

            var output = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var version = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            return new CapabilityStatus(displayName, CapabilityState.Available, version ?? "Executable responded.");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new CapabilityStatus(displayName, CapabilityState.Missing, "Not found on PATH.");
        }
    }

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
