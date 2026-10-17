using System.Security.Cryptography;
using System.Text;

namespace FrameTheory.Domain;

public readonly record struct RationalFrameRate
{
    public RationalFrameRate(int numerator, int denominator)
    {
        if (numerator <= 0 || denominator <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numerator), "Frame-rate components must be positive.");
        }

        var greatestCommonDivisor = GreatestCommonDivisor(numerator, denominator);
        Numerator = numerator / greatestCommonDivisor;
        Denominator = denominator / greatestCommonDivisor;
    }

    public int Numerator { get; }

    public int Denominator { get; }

    public long FrameIndexAt(TimeSpan position)
    {
        if (position < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Frame positions cannot be negative.");
        }

        var frameNumerator = (Int128)position.Ticks * Numerator;
        var frameDenominator = (Int128)TimeSpan.TicksPerSecond * Denominator;
        var frameIndex = frameNumerator / frameDenominator;
        if (frameIndex > long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "The frame index exceeds the supported range.");
        }

        return (long)frameIndex;
    }

    public TimeSpan TimeAtFrame(long frameIndex)
    {
        if (frameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex), "Frame positions cannot be negative.");
        }

        var tickNumerator = (Int128)frameIndex * TimeSpan.TicksPerSecond * Denominator;
        var tickDenominator = (Int128)Numerator;
        var ticks = tickNumerator / tickDenominator;
        if (ticks > TimeSpan.MaxValue.Ticks)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex), "The frame time exceeds the supported range.");
        }

        return TimeSpan.FromTicks((long)ticks);
    }

    public override string ToString() => $"{Numerator}/{Denominator}";

    private static int GreatestCommonDivisor(int left, int right)
    {
        while (right != 0)
        {
            (left, right) = (right, left % right);
        }

        return left;
    }
}

public readonly record struct FrameRange
{
    public FrameRange(long startFrame, long length)
    {
        if (startFrame < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startFrame));
        }

        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (startFrame > long.MaxValue - length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Frame range exceeds the supported range.");
        }

        StartFrame = startFrame;
        Length = length;
    }

    public long StartFrame { get; }

    public long Length { get; }

    public long EndFrameExclusive => StartFrame + Length;

    public bool Contains(long frame) => frame >= StartFrame && frame < EndFrameExclusive;
}

public readonly record struct MediaId
{
    private MediaId(string value) => Value = value;

    public string Value { get; }

    public static MediaId Create(Uri sourceUri, string contentFingerprint)
    {
        ArgumentNullException.ThrowIfNull(sourceUri);
        if (!sourceUri.IsAbsoluteUri)
        {
            throw new ArgumentException("The source URI must be absolute.", nameof(sourceUri));
        }

        if (string.IsNullOrWhiteSpace(contentFingerprint))
        {
            throw new ArgumentException("A content fingerprint is required.", nameof(contentFingerprint));
        }

        var canonicalSource = sourceUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped);
        var identity = Encoding.UTF8.GetBytes($"{canonicalSource}\n{contentFingerprint.Trim()}");
        return new MediaId(Convert.ToHexString(SHA256.HashData(identity)).ToLowerInvariant());
    }

    public static MediaId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || !value.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("Media IDs must be 64 hexadecimal characters.", nameof(value));
        }

        return new MediaId(value.ToLowerInvariant());
    }

    public override string ToString() => Value;
}

public enum MediaKind
{
    Image,
    Video,
    Audio
}

public sealed record MediaSourceMetadata
{
    public MediaSourceMetadata(
        MediaId id,
        Uri sourceUri,
        MediaKind kind,
        int width,
        int height,
        RationalFrameRate? frameRate,
        long durationFrames)
    {
        ArgumentNullException.ThrowIfNull(sourceUri);
        if (!sourceUri.IsAbsoluteUri)
        {
            throw new ArgumentException("The source URI must be absolute.", nameof(sourceUri));
        }

        if (width < 0 || height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Media dimensions cannot be negative.");
        }

        if (durationFrames < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationFrames));
        }

        if (kind == MediaKind.Video && frameRate is null)
        {
            throw new ArgumentException("Video metadata requires a frame rate.", nameof(frameRate));
        }

        Id = id;
        SourceUri = sourceUri;
        Kind = kind;
        Width = width;
        Height = height;
        FrameRate = frameRate;
        DurationFrames = durationFrames;
    }

    public MediaId Id { get; }

    public Uri SourceUri { get; }

    public MediaKind Kind { get; }

    public int Width { get; }

    public int Height { get; }

    public RationalFrameRate? FrameRate { get; }

    public long DurationFrames { get; }
}
