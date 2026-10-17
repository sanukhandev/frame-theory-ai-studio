namespace FrameTheory.Color;

public static class ColorReferenceMatcher
{
    public static ColorGrade Match(ReadOnlySpan<byte> sourceRgb, ReadOnlySpan<byte> referenceRgb)
    {
        var source = Mean(sourceRgb, nameof(sourceRgb));
        var reference = Mean(referenceRgb, nameof(referenceRgb));
        var sourceLuma = Luma(source);
        var referenceLuma = Luma(reference);
        var exposure = Clamp((float)Math.Log((referenceLuma + 0.01) / (sourceLuma + 0.01), 2), -4, 4);
        var saturation = Clamp((float)((Chroma(reference) - Chroma(source)) / Math.Max(0.25, Chroma(source))), -1, 1);
        var temperature = Clamp((float)((reference.Red - reference.Blue) - (source.Red - source.Blue)) * 2, -1, 1);
        var tint = Clamp((float)((reference.Green - (reference.Red + reference.Blue) / 2) -
            (source.Green - (source.Red + source.Blue) / 2)) * 2, -1, 1);
        return new ColorGrade(exposure, saturation: saturation, temperature: temperature, tint: tint);
    }

    private static (double Red, double Green, double Blue) Mean(ReadOnlySpan<byte> rgb, string parameterName)
    {
        if (rgb.Length == 0 || rgb.Length % 3 != 0)
        {
            throw new ArgumentException("RGB data must contain at least one complete pixel.", parameterName);
        }

        var red = 0L;
        var green = 0L;
        var blue = 0L;
        for (var index = 0; index < rgb.Length; index += 3)
        {
            red += rgb[index];
            green += rgb[index + 1];
            blue += rgb[index + 2];
        }

        var pixels = rgb.Length / 3d;
        return (red / pixels / 255, green / pixels / 255, blue / pixels / 255);
    }

    private static double Luma((double Red, double Green, double Blue) color) =>
        color.Red * 0.2126 + color.Green * 0.7152 + color.Blue * 0.0722;

    private static double Chroma((double Red, double Green, double Blue) color) =>
        Math.Max(color.Red, Math.Max(color.Green, color.Blue)) - Math.Min(color.Red, Math.Min(color.Green, color.Blue));

    private static float Clamp(float value, float minimum, float maximum) => Math.Clamp(value, minimum, maximum);
}
