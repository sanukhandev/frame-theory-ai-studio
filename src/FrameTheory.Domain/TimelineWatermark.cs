namespace FrameTheory.Domain;

public sealed record TimelineWatermark
{
    public TimelineWatermark(string sourcePath, float opacity = 0.85f, int margin = 24)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("A watermark source path is required.", nameof(sourcePath));
        }

        if (float.IsNaN(opacity) || float.IsInfinity(opacity) || opacity is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }

        if (margin < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(margin));
        }

        SourcePath = Path.GetFullPath(sourcePath);
        Opacity = opacity;
        Margin = margin;
    }

    public string SourcePath { get; }

    public float Opacity { get; }

    public int Margin { get; }
}
