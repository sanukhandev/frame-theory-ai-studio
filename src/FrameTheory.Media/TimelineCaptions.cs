using FrameTheory.Captions;

namespace FrameTheory.Media;

public sealed record TimelineCaptionStyle
{
    public TimelineCaptionStyle(
        string fontPath,
        int fontSize = 36,
        uint colorRgb = 0xFFFFFF,
        int marginBottom = 32,
        int maximumCharactersPerLine = 42,
        bool kineticWords = false)
    {
        if (string.IsNullOrWhiteSpace(fontPath))
        {
            throw new ArgumentException("A local caption font path is required.", nameof(fontPath));
        }

        if (fontSize <= 0 || fontSize > 4096 || marginBottom < 0 || maximumCharactersPerLine is < 1 or > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize));
        }

        FontPath = fontPath;
        FontSize = fontSize;
        ColorRgb = colorRgb;
        MarginBottom = marginBottom;
        MaximumCharactersPerLine = maximumCharactersPerLine;
        KineticWords = kineticWords;
    }

    public string FontPath { get; }

    public int FontSize { get; }

    public uint ColorRgb { get; }

    public int MarginBottom { get; }

    public int MaximumCharactersPerLine { get; }

    public bool KineticWords { get; }
}
