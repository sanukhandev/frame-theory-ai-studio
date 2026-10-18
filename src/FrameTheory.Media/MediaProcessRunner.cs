using System.Diagnostics;

namespace FrameTheory.Media;

public sealed record MediaProcessResult(int ExitCode, string StandardOutput, string StandardError);

public interface IMediaProcessRunner
{
    Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken);
}

public sealed class FfmpegProcessRunner : IMediaProcessRunner
{
    public async Task<MediaProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        executable = FfmpegExecutableLocator.Resolve(executable);
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
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Could not start media process '{executable}'.");
            }
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new InvalidOperationException($"Could not start media process '{executable}'.", exception);
        }

        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
            await Task.WhenAll(standardOutput, standardError);
        }
        catch (OperationCanceledException)
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

            throw;
        }

        return new MediaProcessResult(process.ExitCode, standardOutput.Result, standardError.Result);
    }
}
