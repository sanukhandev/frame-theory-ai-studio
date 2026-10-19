using FrameTheory.Domain;

namespace FrameTheory.Media;

public enum SocialAspectRatio
{
    Landscape16By9,
    Square1By1,
    Portrait9By16
}

public sealed record SocialVideoExportPreset
{
    public SocialVideoExportPreset(SocialAspectRatio aspectRatio, int width, int height)
    {
        if (!Enum.IsDefined(aspectRatio))
        {
            throw new ArgumentOutOfRangeException(nameof(aspectRatio));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        AspectRatio = aspectRatio;
        Width = width;
        Height = height;
    }

    public SocialAspectRatio AspectRatio { get; }

    public int Width { get; }

    public int Height { get; }

    public static SocialVideoExportPreset Create(SocialAspectRatio aspectRatio, int longEdge = 1080)
    {
        if (longEdge <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(longEdge));
        }

        return aspectRatio switch
        {
            SocialAspectRatio.Landscape16By9 => new(aspectRatio, checked(longEdge * 16 / 9), longEdge),
            SocialAspectRatio.Square1By1 => new(aspectRatio, longEdge, longEdge),
            SocialAspectRatio.Portrait9By16 => new(aspectRatio, longEdge, checked(longEdge * 16 / 9)),
            _ => throw new ArgumentOutOfRangeException(nameof(aspectRatio))
        };
    }

    public ExportPreset ToExportPreset(RationalFrameRate frameRate, bool includeAudio) => new(Width, Height, frameRate, includeAudio);
}
