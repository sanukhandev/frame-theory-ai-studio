using FrameTheory.Domain;

namespace FrameTheory.Media;

public sealed class ExportPreset
{
    public ExportPreset(int width, int height, RationalFrameRate frameRate, bool includeAudio = true)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
        FrameRate = frameRate;
        IncludeAudio = includeAudio;
    }

    public int Width { get; }

    public int Height { get; }

    public RationalFrameRate FrameRate { get; }

    public bool IncludeAudio { get; }
}

public sealed record ExportProgress(string Stage, double Fraction);

public sealed record ExportReceipt(
    Guid JobId,
    string OutputPath,
    long OutputBytes,
    MediaSourceMetadata VerifiedMetadata);

public interface IMediaExporter
{
    Task<ExportReceipt> ExportAsync(
        Uri source,
        string destinationPath,
        ExportPreset preset,
        IProgress<ExportProgress>? progress,
        CancellationToken cancellationToken);
}

public sealed class MediaExportException : Exception
{
    public MediaExportException(string message) : base(message)
    {
    }
}

public sealed class FfmpegMediaExporter : IMediaExporter
{
    private readonly IMediaProcessRunner _processRunner;
    private readonly IMediaProbe _probe;

    public FfmpegMediaExporter(IMediaProcessRunner processRunner, IMediaProbe probe)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
    }

    public async Task<ExportReceipt> ExportAsync(
        Uri source,
        string destinationPath,
        ExportPreset preset,
        IProgress<ExportProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(preset);
        if (!source.IsFile)
        {
            throw new MediaExportException("Only local file URIs can be exported.");
        }

        var sourcePath = Path.GetFullPath(source.LocalPath);
        EnsureNotReparsePoint(sourcePath, "Source media");
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The source media file was not found.", sourcePath);
        }
        var outputPath = Path.GetFullPath(destinationPath);
        var outputDirectory = Path.GetDirectoryName(outputPath)
            ?? throw new ArgumentException("An output directory is required.", nameof(destinationPath));
        if (!Directory.Exists(outputDirectory))
        {
            throw new DirectoryNotFoundException(outputDirectory);
        }

        EnsureNotReparsePoint(outputDirectory, "Export directory");

        var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (string.Equals(sourcePath, outputPath, pathComparison))
        {
            throw new MediaExportException("The export destination cannot replace the source media.");
        }

        EnsureNotReparsePoint(outputPath, "Export destination");

        cancellationToken.ThrowIfCancellationRequested();
        var jobId = Guid.NewGuid();
        var temporaryPath = Path.Combine(outputDirectory, $".{Path.GetFileName(outputPath)}.{jobId:N}.tmp");
        EnsureNotReparsePoint(temporaryPath, "Temporary export");
        try
        {
            progress?.Report(new ExportProgress("Encoding", 0));
            var arguments = new List<string>
            {
                "-hide_banner", "-loglevel", "error", "-y",
                "-i", sourcePath,
                "-map", "0:v:0",
                "-c:v", "libx264",
                "-preset", "medium",
                "-pix_fmt", "yuv420p",
                "-colorspace", "bt709",
                "-color_primaries", "bt709",
                "-color_trc", "bt709",
                "-color_range", "tv",
                "-vf", $"scale={preset.Width}:{preset.Height},setparams=color_primaries=bt709:color_trc=bt709:colorspace=bt709:range=tv",
                "-r", preset.FrameRate.ToString()
            };
            if (preset.IncludeAudio)
            {
                arguments.AddRange(["-map", "0:a:0?", "-c:a", "aac", "-b:a", "192k"]);
            }
            else
            {
                arguments.Add("-an");
            }

            arguments.AddRange(["-movflags", "+faststart", "-shortest", "-f", "mp4", temporaryPath]);
            var result = await _processRunner.RunAsync("ffmpeg", arguments, cancellationToken);
            if (result.ExitCode != 0 || LocalPathSecurity.ContainsReparsePoint(temporaryPath) || !File.Exists(temporaryPath))
            {
                throw new MediaExportException("FFmpeg could not complete the export.");
            }

            progress?.Report(new ExportProgress("Verifying", 0.9));
            var verifiedMetadata = await _probe.ProbeAsync(new Uri(temporaryPath), cancellationToken);
            if (verifiedMetadata.Kind != MediaKind.Video ||
                verifiedMetadata.Width != preset.Width ||
                verifiedMetadata.Height != preset.Height ||
                (preset.IncludeAudio && !verifiedMetadata.HasAudio) ||
                !string.Equals(verifiedMetadata.ColorProfile, "bt709/bt709/bt709/tv", StringComparison.OrdinalIgnoreCase))
            {
                throw new MediaExportException("The exported stream did not match the requested preset.");
            }

            var outputBytes = new FileInfo(temporaryPath).Length;
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, outputPath, overwrite: true);
            progress?.Report(new ExportProgress("Completed", 1));
            return new ExportReceipt(jobId, outputPath, outputBytes, verifiedMetadata);
        }
        finally
        {
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
        }
    }

    private static void EnsureNotReparsePoint(string path, string description)
    {
        if (LocalPathSecurity.ContainsReparsePoint(path))
        {
            throw new MediaExportException($"{description} symlink paths are not accepted.");
        }
    }
}
