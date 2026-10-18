using System.Diagnostics;
using System.Runtime.InteropServices;
using FrameTheory.Application;

namespace FrameTheory.Platform;

public sealed class SystemCapabilityDetector : ICapabilityDetector
{
    private readonly string _ffmpegExecutable;
    private readonly string _ffprobeExecutable;
    private readonly string _probeArgument;
    private readonly string? _gpuExecutable;
    private readonly IReadOnlyList<string> _gpuArguments;

    public SystemCapabilityDetector(
        string ffmpegExecutable = "ffmpeg",
        string ffprobeExecutable = "ffprobe",
        string probeArgument = "-version",
        string? gpuExecutable = null,
        IReadOnlyList<string>? gpuArguments = null)
    {
        _ffmpegExecutable = RequireExecutable(ffmpegExecutable, nameof(ffmpegExecutable));
        _ffprobeExecutable = RequireExecutable(ffprobeExecutable, nameof(ffprobeExecutable));
        _probeArgument = RequireExecutable(probeArgument, nameof(probeArgument));
        var defaultGpuProbe = GetDefaultGpuProbe();
        _gpuExecutable = gpuExecutable ?? defaultGpuProbe.Executable;
        _gpuArguments = (gpuArguments ?? defaultGpuProbe.Arguments).ToArray();
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
            await ProbeGpuAsync(cancellationToken)
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

            try
            {
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
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new CapabilityStatus(displayName, CapabilityState.Missing, "Not found on PATH.");
        }
    }

    private async Task<CapabilityStatus> ProbeGpuAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_gpuExecutable))
        {
            return new CapabilityStatus("GPU", CapabilityState.Unknown, "No platform GPU adapter probe is available; CPU fallback remains active.");
        }

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _gpuExecutable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            foreach (var argument in _gpuArguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (!process.Start())
            {
                return new CapabilityStatus("GPU", CapabilityState.Unknown, "The platform GPU adapter probe could not be started; CPU fallback remains active.");
            }

            try
            {
                var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
                var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);
                var output = await Task.WhenAll(standardOutput, standardError);
                var detail = SelectGpuDescription(output);
                if (process.ExitCode != 0)
                {
                    var failure = FirstOutputLine(output) ?? $"Exited with code {process.ExitCode}.";
                    return new CapabilityStatus("GPU", CapabilityState.Unknown, $"GPU adapter probe failed: {LimitDetail(failure)} CPU fallback remains active.");
                }

                return detail is null
                    ? new CapabilityStatus("GPU", CapabilityState.Unknown, "The platform GPU adapter probe reported no adapter; CPU fallback remains active.")
                    : new CapabilityStatus("GPU", CapabilityState.Available, LimitDetail(detail));
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new CapabilityStatus("GPU", CapabilityState.Unknown, "The platform GPU adapter probe is unavailable; CPU fallback remains active.");
        }
    }

    private static GpuProbeConfiguration GetDefaultGpuProbe()
    {
        if (OperatingSystem.IsWindows())
        {
            return new GpuProbeConfiguration(
                "powershell",
                ["-NoProfile", "-NonInteractive", "-Command", "Get-CimInstance Win32_VideoController | ForEach-Object { \"GPU: \" + $_.Name }"]);
        }

        if (OperatingSystem.IsMacOS())
        {
            return new GpuProbeConfiguration("system_profiler", ["SPDisplaysDataType"]);
        }

        if (OperatingSystem.IsLinux())
        {
            return new GpuProbeConfiguration("lspci", ["-nn"]);
        }

        return new GpuProbeConfiguration(null, []);
    }

    private static string? SelectGpuDescription(IReadOnlyList<string> output)
    {
        var lines = output
            .SelectMany(value => value.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .ToArray();
        return lines.FirstOrDefault(line =>
            line.Contains("VGA compatible controller", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("3D controller", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Display controller", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Chipset Model", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Graphics/Displays", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("GPU: ", StringComparison.OrdinalIgnoreCase));
    }

    private static string? FirstOutputLine(IReadOnlyList<string> output) => output
        .SelectMany(value => value.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        .Select(value => value.Trim())
        .FirstOrDefault(value => value.Length > 0);

    private static string LimitDetail(string value) => value.Length <= 512 ? value : value[..512];

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
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

    private sealed record GpuProbeConfiguration(string? Executable, IReadOnlyList<string> Arguments);
}
