using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using FrameTheory.Domain;

namespace FrameTheory.Media;

public interface IMediaProbe
{
    Task<MediaSourceMetadata> ProbeAsync(Uri file, CancellationToken cancellationToken);
}

public sealed class MediaProbeException : Exception
{
    public MediaProbeException(string message) : base(message)
    {
    }

    public MediaProbeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed class FfprobeMediaProbe : IMediaProbe
{
    private readonly IMediaProcessRunner _processRunner;
    private readonly string _executable;

    public FfprobeMediaProbe(IMediaProcessRunner processRunner, string executable = "ffprobe")
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _executable = string.IsNullOrWhiteSpace(executable)
            ? throw new ArgumentException("The probe executable is required.", nameof(executable))
            : executable;
    }

    public async Task<MediaSourceMetadata> ProbeAsync(Uri file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (!file.IsFile)
        {
            throw new MediaProbeException("Only local file URIs can be probed.");
        }

        var path = Path.GetFullPath(file.LocalPath);
        if (LocalPathSecurity.ContainsReparsePoint(path))
        {
            throw new MediaProbeException("Symlinked media paths are not accepted by the probe.");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The media file was not found.", path);
        }

        var result = await _processRunner.RunAsync(_executable,
            ["-v", "error", "-show_streams", "-show_format", "-of", "json", "-i", path],
            cancellationToken);
        if (result.ExitCode != 0)
        {
            var details = result.StandardError.Trim();
            if (details.Length > 1024)
            {
                details = details[..1024];
            }

            throw new MediaProbeException(details.Length == 0
                ? "ffprobe could not read the media file."
                : $"ffprobe could not read the media file: {details}");
        }

        try
        {
            using var document = JsonDocument.Parse(result.StandardOutput);
            var fingerprint = await ComputeFingerprintAsync(path, cancellationToken);
            return ParseMetadata(file, document.RootElement, fingerprint);
        }
        catch (JsonException exception)
        {
            throw new MediaProbeException("ffprobe returned malformed JSON.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new MediaProbeException("ffprobe returned unsupported media metadata.", exception);
        }
    }

    private static MediaSourceMetadata ParseMetadata(Uri sourceUri, JsonElement root, string fingerprint)
    {
        var streams = root.TryGetProperty("streams", out var streamArray) && streamArray.ValueKind == JsonValueKind.Array
            ? streamArray.EnumerateArray().ToArray()
            : [];
        var video = streams.FirstOrDefault(stream => GetString(stream, "codec_type") == "video");
        var audio = streams.Any(stream => GetString(stream, "codec_type") == "audio");
        var hasVideo = video.ValueKind != JsonValueKind.Undefined;
        var isStillImage = hasVideo && IsStillImage(sourceUri);
        var kind = isStillImage ? MediaKind.Image : hasVideo ? MediaKind.Video : audio ? MediaKind.Audio : MediaKind.Image;
        var frameRate = hasVideo && !isStillImage ? ParseFrameRate(GetString(video, "r_frame_rate")) : null;
        var duration = ParseDuration(root, streams);
        var durationFrames = hasVideo && !isStillImage
            ? ParseFrameCount(video, frameRate, duration)
            : 0;
        var width = hasVideo ? GetInt(video, "width") : 0;
        var height = hasVideo ? GetInt(video, "height") : 0;
        var orientation = hasVideo ? ParseOrientation(video) : 0;
        var colorProfile = hasVideo ? ParseColorProfile(video) : null;
        var mediaId = MediaId.Create(sourceUri, fingerprint);

        return new MediaSourceMetadata(
            mediaId,
            sourceUri,
            kind,
            width,
            height,
            frameRate,
            durationFrames,
            duration,
            orientation,
            colorProfile,
            audio);
    }

    private static async Task<string> ComputeFingerprintAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Share = FileShare.Read,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static TimeSpan ParseDuration(JsonElement root, IReadOnlyList<JsonElement> streams)
    {
        var durationText = root.TryGetProperty("format", out var format) ? GetString(format, "duration") : null;
        durationText ??= streams.Select(stream => GetString(stream, "duration")).FirstOrDefault(value => value is not null);
        if (durationText is null || !decimal.TryParse(durationText, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return TimeSpan.Zero;
        }

        var ticks = decimal.Round(seconds * TimeSpan.TicksPerSecond, 0, MidpointRounding.AwayFromZero);
        if (ticks < 0 || ticks > TimeSpan.MaxValue.Ticks)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromTicks(decimal.ToInt64(ticks));
    }

    private static long ParseFrameCount(JsonElement video, RationalFrameRate? frameRate, TimeSpan duration)
    {
        var frameText = GetString(video, "nb_frames");
        if (long.TryParse(frameText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var frameCount) && frameCount > 0)
        {
            return frameCount;
        }

        return frameRate is null || duration == TimeSpan.Zero
            ? 0
            : Math.Max(1, frameRate.Value.FrameIndexAt(duration));
    }

    private static RationalFrameRate? ParseFrameRate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "0/0")
        {
            return null;
        }

        var parts = value.Split('/');
        return parts.Length == 2 &&
            int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var numerator) &&
            int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var denominator) &&
            numerator > 0 && denominator > 0
            ? new RationalFrameRate(numerator, denominator)
            : null;
    }

    private static int ParseOrientation(JsonElement stream)
    {
        if (!stream.TryGetProperty("tags", out var tags) || !tags.TryGetProperty("rotate", out var rotate) ||
            !int.TryParse(rotate.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var degrees))
        {
            return 0;
        }

        return degrees;
    }

    private static string? ParseColorProfile(JsonElement stream)
    {
        var values = new[]
        {
            GetString(stream, "color_primaries"),
            GetString(stream, "color_transfer"),
            GetString(stream, "color_space"),
            GetString(stream, "color_range")
        }.Where(value => !string.IsNullOrWhiteSpace(value));
        var profile = string.Join('/', values);
        return profile.Length == 0 ? null : profile;
    }

    private static bool IsStillImage(Uri sourceUri) =>
        sourceUri.AbsolutePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
        sourceUri.AbsolutePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
        sourceUri.AbsolutePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
        sourceUri.AbsolutePath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
        sourceUri.AbsolutePath.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
        sourceUri.AbsolutePath.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) ||
        sourceUri.AbsolutePath.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase);

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.GetRawText();
    }

    private static int GetInt(JsonElement element, string propertyName) =>
        int.TryParse(GetString(element, propertyName), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
}
