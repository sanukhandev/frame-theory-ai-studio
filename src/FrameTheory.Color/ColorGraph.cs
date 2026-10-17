namespace FrameTheory.Color;

public enum ColorPrimaries
{
    Rec709
}

public enum ColorTransfer
{
    Linear,
    SRgb,
    Rec709
}

public sealed record ColorProfile
{
    public ColorProfile(string name, ColorPrimaries primaries, ColorTransfer transfer)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A color profile name is required.", nameof(name));
        }

        Name = name;
        Primaries = primaries;
        Transfer = transfer;
    }

    public string Name { get; }

    public ColorPrimaries Primaries { get; }

    public ColorTransfer Transfer { get; }
}

public static class ColorProfiles
{
    public static ColorProfile SRgb { get; } = new("sRGB", ColorPrimaries.Rec709, ColorTransfer.SRgb);

    public static ColorProfile Rec709 { get; } = new("Rec.709 SDR", ColorPrimaries.Rec709, ColorTransfer.Rec709);

    public static ColorProfile LinearRec709 { get; } = new("Linear Rec.709", ColorPrimaries.Rec709, ColorTransfer.Linear);
}

public readonly record struct RgbColor
{
    public RgbColor(float red, float green, float blue, float alpha = 1)
    {
        Validate(red, nameof(red));
        Validate(green, nameof(green));
        Validate(blue, nameof(blue));
        Validate(alpha, nameof(alpha));
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    public float Red { get; }

    public float Green { get; }

    public float Blue { get; }

    public float Alpha { get; }

    private static void Validate(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Color channels must be finite values in the range 0..1.");
        }
    }
}

public sealed class ColorGraph
{
    public const int CurrentVersion = 1;

    public ColorGraph(ColorProfile inputProfile, ColorProfile workingProfile, ColorProfile outputProfile)
    {
        InputProfile = inputProfile ?? throw new ArgumentNullException(nameof(inputProfile));
        WorkingProfile = workingProfile ?? throw new ArgumentNullException(nameof(workingProfile));
        OutputProfile = outputProfile ?? throw new ArgumentNullException(nameof(outputProfile));
        if (workingProfile.Transfer != ColorTransfer.Linear)
        {
            throw new ArgumentException("The working profile must be linear-light.", nameof(workingProfile));
        }
    }

    public int Version => CurrentVersion;

    public ColorProfile InputProfile { get; }

    public ColorProfile WorkingProfile { get; }

    public ColorProfile OutputProfile { get; }

    public static ColorGraph ForSrgb() => new(ColorProfiles.SRgb, ColorProfiles.LinearRec709, ColorProfiles.SRgb);

    public static ColorGraph ForRec709Video() => new(ColorProfiles.Rec709, ColorProfiles.LinearRec709, ColorProfiles.Rec709);
}

public sealed class ColorPipeline
{
    private readonly ColorGraph _graph;

    public ColorPipeline(ColorGraph graph) => _graph = graph ?? throw new ArgumentNullException(nameof(graph));

    public RgbColor Transform(RgbColor encodedColor)
    {
        var linearRed = Decode(_graph.InputProfile.Transfer, encodedColor.Red);
        var linearGreen = Decode(_graph.InputProfile.Transfer, encodedColor.Green);
        var linearBlue = Decode(_graph.InputProfile.Transfer, encodedColor.Blue);
        return new RgbColor(
            Encode(_graph.OutputProfile.Transfer, linearRed),
            Encode(_graph.OutputProfile.Transfer, linearGreen),
            Encode(_graph.OutputProfile.Transfer, linearBlue),
            encodedColor.Alpha);
    }

    private static float Decode(ColorTransfer transfer, float value) => transfer switch
    {
        ColorTransfer.Linear => value,
        ColorTransfer.SRgb => value <= 0.04045f
            ? value / 12.92f
            : MathF.Pow((value + 0.055f) / 1.055f, 2.4f),
        ColorTransfer.Rec709 => value < 0.081f
            ? value / 4.5f
            : MathF.Pow((value + 0.099f) / 1.099f, 1 / 0.45f),
        _ => throw new ArgumentOutOfRangeException(nameof(transfer))
    };

    private static float Encode(ColorTransfer transfer, float value) => transfer switch
    {
        ColorTransfer.Linear => value,
        ColorTransfer.SRgb => value <= 0.0031308f
            ? value * 12.92f
            : 1.055f * MathF.Pow(value, 1 / 2.4f) - 0.055f,
        ColorTransfer.Rec709 => value < 0.018f
            ? value * 4.5f
            : 1.099f * MathF.Pow(value, 0.45f) - 0.099f,
        _ => throw new ArgumentOutOfRangeException(nameof(transfer))
    };
}
