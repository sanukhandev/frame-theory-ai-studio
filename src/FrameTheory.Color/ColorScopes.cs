namespace FrameTheory.Color;

public sealed class ColorScopeData
{
    public ColorScopeData(
        int width,
        int height,
        IReadOnlyList<int> redHistogram,
        IReadOnlyList<int> greenHistogram,
        IReadOnlyList<int> blueHistogram,
        IReadOnlyList<int> waveform,
        IReadOnlyList<int> vectorscope)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        ValidateBins(redHistogram, 256, nameof(redHistogram));
        ValidateBins(greenHistogram, 256, nameof(greenHistogram));
        ValidateBins(blueHistogram, 256, nameof(blueHistogram));
        ValidateBins(waveform, 64 * 64, nameof(waveform));
        ValidateBins(vectorscope, 64 * 64, nameof(vectorscope));
        Width = width;
        Height = height;
        RedHistogram = Array.AsReadOnly(redHistogram.ToArray());
        GreenHistogram = Array.AsReadOnly(greenHistogram.ToArray());
        BlueHistogram = Array.AsReadOnly(blueHistogram.ToArray());
        Waveform = Array.AsReadOnly(waveform.ToArray());
        Vectorscope = Array.AsReadOnly(vectorscope.ToArray());
    }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<int> RedHistogram { get; }

    public IReadOnlyList<int> GreenHistogram { get; }

    public IReadOnlyList<int> BlueHistogram { get; }

    public IReadOnlyList<int> Waveform { get; }

    public IReadOnlyList<int> Vectorscope { get; }

    public int PixelCount => Width * Height;

    private static void ValidateBins(IReadOnlyList<int> bins, int expectedCount, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(bins);
        if (bins.Count != expectedCount || bins.Any(value => value < 0))
        {
            throw new ArgumentException("Scope bins must be non-negative and have the expected bounded size.", parameterName);
        }
    }
}

public static class ColorScopeAnalyzer
{
    public static ColorScopeData Analyze(int width, int height, ReadOnlySpan<byte> rgbBytes)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if ((long)width * height > 64 * 1024 * 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Scope dimensions exceed the bounded analysis size.");
        }

        if (rgbBytes.Length != checked(width * height * 3))
        {
            throw new ArgumentException("RGB bytes must contain exactly three channels per pixel.", nameof(rgbBytes));
        }

        var red = new int[256];
        var green = new int[256];
        var blue = new int[256];
        var waveform = new int[64 * 64];
        var vectorscope = new int[64 * 64];
        for (var pixel = 0; pixel < width * height; pixel++)
        {
            var offset = pixel * 3;
            var redValue = rgbBytes[offset];
            var greenValue = rgbBytes[offset + 1];
            var blueValue = rgbBytes[offset + 2];
            red[redValue]++;
            green[greenValue]++;
            blue[blueValue]++;

            var x = pixel % width * 64 / width;
            var luminance = (int)Math.Clamp(MathF.Round(0.2126f * redValue + 0.7152f * greenValue + 0.0722f * blueValue), 0, 255);
            var waveformY = luminance * 64 / 256;
            waveform[x * 64 + waveformY]++;

            var chromaX = Math.Clamp((int)MathF.Round((redValue - blueValue + 255) * 63 / 510f), 0, 63);
            var chromaY = Math.Clamp((int)MathF.Round((greenValue - blueValue + 255) * 63 / 510f), 0, 63);
            vectorscope[chromaY * 64 + chromaX]++;
        }

        return new ColorScopeData(width, height, red, green, blue, waveform, vectorscope);
    }
}
