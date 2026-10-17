namespace FrameTheory.Color;

public sealed class ColorGrade
{
    public ColorGrade(float exposureEV = 0, float contrast = 0, float saturation = 0, float temperature = 0, float tint = 0)
    {
        Validate(exposureEV, -8, 8, nameof(exposureEV));
        Validate(contrast, -1, 1, nameof(contrast));
        Validate(saturation, -1, 1, nameof(saturation));
        Validate(temperature, -1, 1, nameof(temperature));
        Validate(tint, -1, 1, nameof(tint));
        ExposureEV = exposureEV;
        Contrast = contrast;
        Saturation = saturation;
        Temperature = temperature;
        Tint = tint;
    }

    public float ExposureEV { get; }

    public float Contrast { get; }

    public float Saturation { get; }

    public float Temperature { get; }

    public float Tint { get; }

    private static void Validate(float value, float minimum, float maximum, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"The grade value must be finite and in the range {minimum}..{maximum}.");
        }
    }
}
