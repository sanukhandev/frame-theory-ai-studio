using FrameTheory.Domain;

namespace FrameTheory.Media;

public enum SocialAspectRatio
{
    Landscape16By9,
    Square1By1,
    Portrait9By16
}

public sealed record SocialVideoExportPreset(SocialAspectRatio AspectRatio, int Width, int Height)
{
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
