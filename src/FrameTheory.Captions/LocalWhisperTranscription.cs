using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using FrameTheory.Domain;

namespace FrameTheory.Captions;

public sealed record LocalWhisperOptions
{
    public LocalWhisperOptions(string executablePath, string modelPath)
    {
        ExecutablePath = ValidatePath(executablePath, "The local Whisper executable was not found.");
        ModelPath = ValidatePath(modelPath, "The local Whisper model was not found.");
    }

    public string ExecutablePath { get; }

    public string ModelPath { get; }

    private static string ValidatePath(string path, string missingMessage)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A local Whisper path is required.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(missingMessage, fullPath);
        }

        if (LocalPathSecurity.ContainsReparsePoint(fullPath))
        {
            throw new InvalidDataException("Local Whisper executable and model files must not be reparse points.");
        }

        return fullPath;
    }
}

public sealed class WhisperCliTranscriptionProvider : IProgressiveTranscriptionProvider
{
    private const int MaximumJsonBytes = 10 * 1024 * 1024;
    private const int MaximumDiagnosticCharacters = 64 * 1024;
    private readonly LocalWhisperOptions _options;

    public WhisperCliTranscriptionProvider(LocalWhisperOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<TranscriptResult> TranscribeAsync(Uri source, CancellationToken cancellationToken)
        => TranscribeAsync(source, progress: null, cancellationToken);

    public async Task<TranscriptResult> TranscribeAsync(
        Uri source,
        IProgress<TranscriptionProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();
        if (!source.IsFile)
        {
            throw new InvalidDataException("Local Whisper sources must be local files.");
        }

        var sourcePath = Path.GetFullPath(source.LocalPath);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The local Whisper source was not found.", sourcePath);
        }

        if (LocalPathSecurity.ContainsReparsePoint(sourcePath))
        {
            throw new InvalidDataException("Local Whisper sources must not be reparse points.");
        }

        var sourceFingerprint = await TranscriptSourceFingerprint.ComputeAsync(source, cancellationToken);
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"frame-theory-whisper-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        try
        {
            progress?.Report(new TranscriptionProgress("Running local Whisper", 0));
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _options.ExecutablePath,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.StartInfo.ArgumentList.Add("--model");
            process.StartInfo.ArgumentList.Add(_options.ModelPath);
            process.StartInfo.ArgumentList.Add("--output_format");
            process.StartInfo.ArgumentList.Add("json");
            process.StartInfo.ArgumentList.Add("--word_timestamps");
            process.StartInfo.ArgumentList.Add("True");
            process.StartInfo.ArgumentList.Add("--output_dir");
            process.StartInfo.ArgumentList.Add(outputDirectory);
            process.StartInfo.ArgumentList.Add("--task");
            process.StartInfo.ArgumentList.Add("transcribe");
            process.StartInfo.ArgumentList.Add(sourcePath);

            try
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException("The local Whisper process could not be started.");
                }
            }
            catch (Win32Exception exception)
            {
                throw new InvalidOperationException("The local Whisper process could not be started.", exception);
            }

            var errorTask = ReadBoundedDiagnosticsAsync(process.StandardError, cancellationToken);
            try
            {
                await process.WaitForExitAsync(cancellationToken);
                var error = await errorTask;
                if (process.ExitCode != 0)
                {
                    throw new InvalidDataException($"Local Whisper failed with exit code {process.ExitCode}: {TrimError(error)}");
                }
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }

            var outputPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(sourcePath)}.json");
            if (!File.Exists(outputPath))
            {
                throw new InvalidDataException("Local Whisper completed without producing a JSON word-timing file.");
            }

            if (new FileInfo(outputPath).Length > MaximumJsonBytes)
            {
                throw new InvalidDataException("The local Whisper JSON output exceeds the supported size.");
            }

            progress?.Report(new TranscriptionProgress("Parsing local word timings", 0.9));
            var json = await File.ReadAllTextAsync(outputPath, cancellationToken);
            var currentFingerprint = await TranscriptSourceFingerprint.ComputeAsync(source, cancellationToken);
            if (!string.Equals(sourceFingerprint, currentFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The selected media changed while local Whisper was processing it.");
            }

            var result = WhisperCliJsonParser.Parse(source, sourceFingerprint, json);
            progress?.Report(new TranscriptionProgress("Completed", 1));
            return result;
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static string TrimError(string error) => string.IsNullOrWhiteSpace(error)
        ? "no diagnostic output"
        : error.Trim().Length <= 1000 ? error.Trim() : $"{error.Trim()[..1000]}…";

    private static async Task<string> ReadBoundedDiagnosticsAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var diagnostics = new StringBuilder();
        var buffer = new char[4096];
        int read;
        while ((read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            var remaining = MaximumDiagnosticCharacters - diagnostics.Length;
            if (remaining > 0)
            {
                diagnostics.Append(buffer, 0, Math.Min(read, remaining));
            }
        }

        return diagnostics.ToString();
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
    }
}

public static class WhisperCliJsonParser
{
    public static TranscriptResult Parse(Uri source, string sourceFingerprint, string json)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("The local Whisper JSON output is empty.");
        }

        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("segments", out var segments) ||
                segments.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("The local Whisper JSON output has no segments array.");
            }

            var words = new List<TranscriptWord>();
            foreach (var segment in segments.EnumerateArray())
            {
                if (!segment.TryGetProperty("words", out var segmentWords) || segmentWords.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var word in segmentWords.EnumerateArray())
                {
                    var text = word.TryGetProperty("word", out var wordText) && wordText.ValueKind == JsonValueKind.String
                        ? wordText.GetString()
                        : null;
                    if (string.IsNullOrWhiteSpace(text) ||
                        !TryReadSeconds(word, "start", out var start) ||
                        !TryReadSeconds(word, "end", out var end))
                    {
                        throw new InvalidDataException("The local Whisper JSON output contains an invalid word timing.");
                    }

                    words.Add(new TranscriptWord(text, start, end));
                }
            }

            return new TranscriptResult(source, sourceFingerprint, words);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The local Whisper JSON output is malformed.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("The local Whisper JSON output contains invalid word timings.", exception);
        }
    }

    private static bool TryReadSeconds(JsonElement word, string propertyName, out TimeSpan value)
    {
        value = default;
        if (!word.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Number ||
            !property.TryGetDouble(out var seconds) ||
            double.IsNaN(seconds) ||
            double.IsInfinity(seconds) ||
            seconds < 0 ||
            seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            return false;
        }

        value = TimeSpan.FromSeconds(seconds);
        return true;
    }
}
