namespace FrameTheory.Media;

using FrameTheory.Domain;

public sealed class PreparedAudioSource : IAsyncDisposable
{
    private string? _temporaryPath;

    public PreparedAudioSource(Uri source, string? temporaryPath)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsFile)
        {
            throw new ArgumentException("Prepared audio sources must be local files.", nameof(source));
        }

        Source = source;
        _temporaryPath = temporaryPath;
    }

    public Uri Source { get; }

    public ValueTask DisposeAsync()
    {
        var temporaryPath = Interlocked.Exchange(ref _temporaryPath, null);
        if (temporaryPath is null)
        {
            return ValueTask.CompletedTask;
        }

        try
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return ValueTask.CompletedTask;
    }
}

public interface IAudioSourcePreparer
{
    Task<PreparedAudioSource> PrepareAsync(Uri source, CancellationToken cancellationToken);
}

public sealed class FfmpegAudioSourcePreparer : IAudioSourcePreparer
{
    private const long MaximumPreparedAudioBytes = 2L * 1024 * 1024 * 1024;
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".aac", ".aiff", ".aif", ".flac", ".m4a", ".mp3", ".ogg", ".opus", ".wav"
    };
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".avi", ".mkv", ".mov", ".mp4", ".mpeg", ".mpg", ".webm"
    };
    private readonly IMediaProcessRunner _processRunner;

    public FfmpegAudioSourcePreparer(IMediaProcessRunner processRunner) => _processRunner = processRunner;

    public async Task<PreparedAudioSource> PrepareAsync(Uri source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();
        if (!source.IsFile)
        {
            throw new InvalidDataException("Audio sources must be local files.");
        }

        var sourcePath = Path.GetFullPath(source.LocalPath);
        if (LocalPathSecurity.ContainsReparsePoint(sourcePath))
        {
            throw new InvalidDataException("Symlinked audio sources are not accepted.");
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The audio source was not found.", sourcePath);
        }

        var extension = Path.GetExtension(sourcePath);
        if (AudioExtensions.Contains(extension))
        {
            return new PreparedAudioSource(source, temporaryPath: null);
        }

        if (!VideoExtensions.Contains(extension))
        {
            throw new InvalidDataException("Transcription requires a supported audio or video file.");
        }

        var outputPath = Path.Combine(Path.GetTempPath(), $"frame-theory-transcription-{Guid.NewGuid():N}.wav");
        try
        {
            var result = await _processRunner.RunAsync(
                "ffmpeg",
                [
                    "-hide_banner", "-loglevel", "error", "-y",
                    "-i", sourcePath,
                    "-map", "0:a:0",
                    "-vn", "-ac", "1", "-ar", "48000", "-c:a", "pcm_s16le",
                    "-f", "wav", outputPath
                ],
                cancellationToken);
            if (result.ExitCode != 0 || !File.Exists(outputPath))
            {
                throw new InvalidDataException("FFmpeg could not extract an audio track from the selected video.");
            }

            var outputLength = new FileInfo(outputPath).Length;
            if (outputLength == 0 || outputLength > MaximumPreparedAudioBytes)
            {
                throw new InvalidDataException("The extracted audio exceeds the supported 2 GiB Gemini Files API limit; use a shorter audio source.");
            }

            return new PreparedAudioSource(new Uri(outputPath), outputPath);
        }
        catch
        {
            TryDelete(outputPath);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
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
