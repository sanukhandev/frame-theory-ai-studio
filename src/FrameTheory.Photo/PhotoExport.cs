using System.Globalization;
using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.Photo;

public enum PhotoExportFormat
{
    Png,
    Jpeg
}

public sealed record SocialPhotoExportPreset
{
    public SocialPhotoExportPreset(SocialAspectRatio aspectRatio, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        AspectRatio = aspectRatio;
        Width = width;
        Height = height;
    }

    public SocialAspectRatio AspectRatio { get; }

    public int Width { get; }

    public int Height { get; }

    public static SocialPhotoExportPreset Create(SocialAspectRatio aspectRatio, int longEdge = 1080)
    {
        var videoPreset = SocialVideoExportPreset.Create(aspectRatio, longEdge);
        return new SocialPhotoExportPreset(aspectRatio, videoPreset.Width, videoPreset.Height);
    }
}

public sealed class PhotoExportOptions
{
    public PhotoExportOptions(PhotoExportFormat format, int quality = 95, SocialPhotoExportPreset? preset = null)
    {
        if (format is not PhotoExportFormat.Png and not PhotoExportFormat.Jpeg)
        {
            throw new ArgumentOutOfRangeException(nameof(format));
        }

        if (quality is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 1 and 100.");
        }

        Format = format;
        Quality = quality;
        Preset = preset;
    }

    public PhotoExportFormat Format { get; }

    public int Quality { get; }

    public SocialPhotoExportPreset? Preset { get; }

    public string Extension => Format == PhotoExportFormat.Png ? ".png" : ".jpg";
}

public sealed record PhotoExportReceipt(
    Guid JobId,
    string OutputPath,
    long OutputBytes,
    MediaSourceMetadata VerifiedMetadata);

public sealed class PhotoExportException : Exception
{
    public PhotoExportException(string message) : base(message)
    {
    }
}

public sealed class PhotoExporter
{
    private readonly IMediaProcessRunner _processRunner;
    private readonly IMediaProbe _probe;

    public PhotoExporter(IMediaProcessRunner processRunner, IMediaProbe probe)
    {
        _processRunner = processRunner;
        _probe = probe;
    }

    public async Task<PhotoExportReceipt> ExportAsync(
        PhotoEditState state,
        string destinationPath,
        PhotoExportOptions options,
        IProgress<ExportProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(options);
        var sourcePath = GetLocalPath(state.Source.SourceUri, "Photo source");
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The source photo was not found.", sourcePath);
        }

        EnsureNotReparsePoint(sourcePath, "Photo source");
        var outputPath = Path.GetFullPath(destinationPath);
        var outputDirectory = Path.GetDirectoryName(outputPath)
            ?? throw new ArgumentException("An output directory is required.", nameof(destinationPath));
        if (!Directory.Exists(outputDirectory))
        {
            throw new DirectoryNotFoundException(outputDirectory);
        }

        EnsureNotReparsePoint(outputDirectory, "Photo export directory");

        if (string.Equals(sourcePath, outputPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new PhotoExportException("The export destination cannot replace the source photo.");
        }

        if (File.Exists(outputPath))
        {
            EnsureNotReparsePoint(outputPath, "Photo export destination");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var jobId = Guid.NewGuid();
        var exportImage = state.RenderExportImage();
        var inputExtension = exportImage.Length > 1 && exportImage[1] == (byte)'7' ? ".pam" : ".ppm";
        var temporaryPpm = Path.Combine(outputDirectory, $".{Path.GetFileName(outputPath)}.{jobId:N}{inputExtension}");
        var temporaryOutput = Path.Combine(outputDirectory, $".{Path.GetFileName(outputPath)}.{jobId:N}.tmp{options.Extension}");
        try
        {
            progress?.Report(new ExportProgress("Rendering", 0));
            await File.WriteAllBytesAsync(temporaryPpm, exportImage, cancellationToken);
            progress?.Report(new ExportProgress("Encoding", 0.25));
            var result = await _processRunner.RunAsync(
                "ffmpeg",
                BuildArguments(state, sourcePath, options, temporaryPpm, temporaryOutput, inputExtension == ".pam"),
                cancellationToken);
            if (result.ExitCode != 0 || !File.Exists(temporaryOutput))
            {
                throw new PhotoExportException("FFmpeg could not complete the photo export.");
            }

            progress?.Report(new ExportProgress("Verifying", 0.9));
            var verifiedMetadata = await _probe.ProbeAsync(new Uri(temporaryOutput), cancellationToken);
            var rendered = PpmRaster.Parse(state.RenderPreview());
            var expectedWidth = options.Preset?.Width ?? rendered.Width;
            var expectedHeight = options.Preset?.Height ?? rendered.Height;
            if (verifiedMetadata.Kind != MediaKind.Image ||
                verifiedMetadata.Width != expectedWidth ||
                verifiedMetadata.Height != expectedHeight ||
                string.IsNullOrWhiteSpace(verifiedMetadata.ColorProfile))
            {
                throw new PhotoExportException("The exported image dimensions or media kind did not match the edit.");
            }

            var outputBytes = new FileInfo(temporaryOutput).Length;
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryOutput, outputPath, overwrite: true);
            progress?.Report(new ExportProgress("Completed", 1));
            return new PhotoExportReceipt(jobId, outputPath, outputBytes, verifiedMetadata);
        }
        finally
        {
            DeleteIfPresent(temporaryPpm);
            DeleteIfPresent(temporaryOutput);
        }
    }

    private static IReadOnlyList<string> BuildArguments(
        PhotoEditState state,
        string sourcePath,
        PhotoExportOptions options,
        string inputPath,
        string outputPath,
        bool hasAlpha)
    {
        var arguments = new List<string>
        {
            "-hide_banner", "-loglevel", "error", "-y",
            "-f", "image2", "-i", inputPath,
            "-i", sourcePath,
            "-map", "0:v:0", "-map_metadata", "1",
            "-frames:v", "1",
            "-vf", BuildVideoFilter(options, hasAlpha),
            "-c:v", options.Format == PhotoExportFormat.Png ? "png" : "mjpeg",
            "-colorspace", "bt709",
            "-color_primaries", "bt709",
            "-color_trc", "iec61966-2-1",
            "-metadata", "comment=Frame Theory Studio AI",
            "-metadata", $"source_color_profile={MetadataValue(state.Source.Metadata.ColorProfile ?? "sRGB")}",
            "-metadata", $"source_orientation_degrees={state.Source.Metadata.OrientationDegrees.ToString(CultureInfo.InvariantCulture)}"
        };
        if (options.Format == PhotoExportFormat.Png)
        {
            arguments.AddRange(["-pix_fmt", hasAlpha ? "rgba" : "rgb24", "-compression_level", PngCompressionLevel(options.Quality).ToString(CultureInfo.InvariantCulture)]);
        }
        else
        {
            arguments.AddRange(["-q:v", JpegQuantizer(options.Quality).ToString(CultureInfo.InvariantCulture)]);
        }

        arguments.AddRange(["-f", "image2", outputPath]);
        return arguments;
    }

    private static string BuildVideoFilter(PhotoExportOptions options, bool hasAlpha)
    {
        var filters = new List<string>();
        if (options.Preset is not null)
        {
            filters.Add($"scale={options.Preset.Width}:{options.Preset.Height}:force_original_aspect_ratio=increase");
            filters.Add($"crop={options.Preset.Width}:{options.Preset.Height}");
        }

        filters.Add($"format={(options.Format == PhotoExportFormat.Png && hasAlpha ? "rgba" : "rgb24")}");
        filters.Add("setparams=color_primaries=bt709:color_trc=iec61966-2-1:colorspace=bt709:range=pc");
        return string.Join(',', filters);
    }

    private static int PngCompressionLevel(int quality) => 9 - (quality - 1) * 9 / 99;

    private static int JpegQuantizer(int quality) => 31 - (quality - 1) * 30 / 99;

    private static string MetadataValue(string value) =>
        value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal)[..Math.Min(value.Length, 128)];

    private static string GetLocalPath(Uri uri, string description)
    {
        if (!uri.IsFile)
        {
            throw new PhotoExportException($"Only local {description.ToLowerInvariant()} URIs are supported.");
        }

        return Path.GetFullPath(uri.LocalPath);
    }

    private static void EnsureNotReparsePoint(string path, string description)
    {
        if (MediaPathSecurity.ContainsReparsePoint(path))
        {
            throw new PhotoExportException($"{description} symlink paths are not accepted.");
        }
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
