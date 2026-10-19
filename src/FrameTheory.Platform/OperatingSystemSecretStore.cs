using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FrameTheory.Application;

namespace FrameTheory.Platform;

public sealed class OperatingSystemSecretStore : ISecretStore
{
    private const string TargetPrefix = "FrameTheoryStudioAI/";

    public Task StoreAsync(string name, string secret, CancellationToken cancellationToken = default)
    {
        var target = NormalizeName(name);
        if (string.IsNullOrWhiteSpace(secret) || secret.Length > 4096)
        {
            throw new ArgumentException("A bounded non-empty secret is required.", nameof(secret));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (OperatingSystem.IsWindows())
        {
            StoreWindows(target, secret);
            return Task.CompletedTask;
        }

        return StoreUnixAsync(target, secret, cancellationToken);
    }

    public Task<string?> ReadAsync(string name, CancellationToken cancellationToken = default)
    {
        var target = NormalizeName(name);
        cancellationToken.ThrowIfCancellationRequested();
        if (OperatingSystem.IsWindows())
        {
            return Task.FromResult(ReadWindows(target));
        }

        return ReadUnixAsync(target, cancellationToken);
    }

    public Task DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        var target = NormalizeName(name);
        cancellationToken.ThrowIfCancellationRequested();
        if (OperatingSystem.IsWindows())
        {
            DeleteWindows(target);
            return Task.CompletedTask;
        }

        return DeleteUnixAsync(target, cancellationToken);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || name.Any(character => !char.IsLetterOrDigit(character) && character is not '.' and not '-' and not '_'))
        {
            throw new ArgumentException("Secret names may contain only letters, digits, '.', '-' and '_'.", nameof(name));
        }

        return TargetPrefix + name;
    }

    private static void StoreWindows(string target, string secret)
    {
        var bytes = Encoding.Unicode.GetBytes(secret);
        if (bytes.Length > 512)
        {
            throw new ArgumentException("Windows Credential Manager limits this secret to 512 bytes.", nameof(secret));
        }

        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            var credential = new NativeCredential
            {
                Type = 1,
                TargetName = target,
                CredentialBlobSize = (uint)bytes.Length,
                CredentialBlob = handle.AddrOfPinnedObject(),
                Persist = 2,
                UserName = "Frame Theory Studio AI"
            };
            if (!CredWrite(ref credential, 0))
            {
                throw new UnauthorizedAccessException("Windows Credential Manager rejected the Gemini API key.");
            }
        }
        finally
        {
            handle.Free();
        }
    }

    private static string? ReadWindows(string target)
    {
        if (!CredRead(target, 1, 0, out var credentialPointer))
        {
            var error = Marshal.GetLastWin32Error();
            return error == 1168 ? null : throw new UnauthorizedAccessException("Windows Credential Manager could not read the Gemini API key.");
        }

        try
        {
            var credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
            return credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0
                ? null
                : Marshal.PtrToStringUni(credential.CredentialBlob, checked((int)credential.CredentialBlobSize / 2));
        }
        finally
        {
            CredFree(credentialPointer);
        }
    }

    private static void DeleteWindows(string target)
    {
        if (!CredDelete(target, 1, 0))
        {
            var error = Marshal.GetLastWin32Error();
            if (error != 1168)
            {
                throw new UnauthorizedAccessException("Windows Credential Manager could not delete the Gemini API key.");
            }
        }
    }

    private static Task StoreUnixAsync(string target, string secret, CancellationToken cancellationToken) =>
        OperatingSystem.IsMacOS()
            ? RunUnixAsync("security", ["add-generic-password", "-a", "FrameTheoryStudioAI", "-s", target, "-w", secret, "-U"], null, cancellationToken, allowMissing: false)
            : OperatingSystem.IsLinux()
                ? RunUnixAsync("secret-tool", ["store", "--label=Frame Theory Studio AI", "service", target, "account", "FrameTheoryStudioAI"], secret, cancellationToken, allowMissing: false)
                : throw new PlatformNotSupportedException("No supported operating-system credential store is available.");

    private static async Task<string?> ReadUnixAsync(string target, CancellationToken cancellationToken)
    {
        var result = OperatingSystem.IsMacOS()
            ? await RunUnixResultAsync("security", ["find-generic-password", "-a", "FrameTheoryStudioAI", "-s", target, "-w"], null, cancellationToken)
            : OperatingSystem.IsLinux()
                ? await RunUnixResultAsync("secret-tool", ["lookup", "service", target, "account", "FrameTheoryStudioAI"], null, cancellationToken)
                : throw new PlatformNotSupportedException("No supported operating-system credential store is available.");
        return result.ExitCode == 0 ? result.Output.TrimEnd('\r', '\n') : null;
    }

    private static Task DeleteUnixAsync(string target, CancellationToken cancellationToken) =>
        OperatingSystem.IsMacOS()
            ? RunUnixAsync("security", ["delete-generic-password", "-a", "FrameTheoryStudioAI", "-s", target], null, cancellationToken, allowMissing: true)
            : OperatingSystem.IsLinux()
                ? RunUnixAsync("secret-tool", ["clear", "service", target, "account", "FrameTheoryStudioAI"], null, cancellationToken, allowMissing: true)
                : throw new PlatformNotSupportedException("No supported operating-system credential store is available.");

    private static async Task RunUnixAsync(string executable, IReadOnlyList<string> arguments, string? standardInput, CancellationToken cancellationToken, bool allowMissing)
    {
        var result = await RunUnixResultAsync(executable, arguments, standardInput, cancellationToken);
        if (result.ExitCode != 0 && !allowMissing)
        {
            throw new InvalidOperationException($"The operating-system credential store rejected the {executable} request.");
        }
    }

    private static async Task<UnixResult> RunUnixResultAsync(string executable, IReadOnlyList<string> arguments, string? standardInput, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardInput = standardInput is not null,
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
            process.Start();
        }
        catch (Win32Exception exception)
        {
            throw new PlatformNotSupportedException($"The operating-system credential store '{executable}' is unavailable.", exception);
        }

        try
        {
            if (standardInput is not null)
            {
                await process.StandardInput.WriteAsync(standardInput.AsMemory(), cancellationToken);
                await process.StandardInput.FlushAsync(cancellationToken);
                process.StandardInput.Close();
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            await Task.WhenAll(outputTask, errorTask);
            return new UnixResult(process.ExitCode, outputTask.Result);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }
    }

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
        catch (Win32Exception)
        {
        }
        catch (NotSupportedException)
        {
        }
    }

    private sealed record UnixResult(int ExitCode, string Output);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        [MarshalAs(UnmanagedType.LPWStr)] public string? TargetName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        [MarshalAs(UnmanagedType.LPWStr)] public string? TargetAlias;
        [MarshalAs(UnmanagedType.LPWStr)] public string? UserName;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref NativeCredential userCredential, uint flags);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CredFree(IntPtr credential);
}
