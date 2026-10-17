namespace FrameTheory.Domain;

public enum TimelineTitlePlacement
{
    TopLeft,
    TopCenter,
    Center,
    BottomCenter
}

public sealed record TimelineTitle
{
    public TimelineTitle(
        string text,
        string fontPath,
        long startFrame,
        long durationFrames,
        int fontSize = 48,
        uint colorRgb = 0xFFFFFF,
        TimelineTitlePlacement placement = TimelineTitlePlacement.Center,
        int order = 0)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 1000)
        {
            throw new ArgumentException("Title text must contain between one and 1000 characters.", nameof(text));
        }

        if (string.IsNullOrWhiteSpace(fontPath))
        {
            throw new ArgumentException("A licensed local font path is required.", nameof(fontPath));
        }

        if (startFrame < 0 || durationFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startFrame));
        }

        if (fontSize <= 0 || fontSize > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize));
        }

        Text = text;
        FontPath = fontPath;
        TimelineRange = new FrameRange(startFrame, durationFrames);
        FontSize = fontSize;
        ColorRgb = colorRgb;
        Placement = placement;
        Order = order;
    }

    public string Text { get; }

    public string FontPath { get; }

    public FrameRange TimelineRange { get; }

    public int FontSize { get; }

    public uint ColorRgb { get; }

    public TimelineTitlePlacement Placement { get; }

    public int Order { get; }
}
