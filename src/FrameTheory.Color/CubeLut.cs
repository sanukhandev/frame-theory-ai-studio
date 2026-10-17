using System.Globalization;
using System.Text;

namespace FrameTheory.Color;

public sealed class CubeLut
{
    private const int MinimumSize = 2;
    private const int MaximumSize = 65;
    private const int MaximumTextBytes = 64 * 1024 * 1024;
    private readonly RgbColor[] _values;

    public CubeLut(int size, IReadOnlyList<RgbColor> values, RgbColor? domainMin = null, RgbColor? domainMax = null)
    {
        ValidateSize(size);
        ArgumentNullException.ThrowIfNull(values);
        var expectedValueCount = checked(size * size * size);
        if (values.Count != expectedValueCount)
        {
            throw new ArgumentException($"A {size} cubed LUT requires {expectedValueCount} values.", nameof(values));
        }

        Size = size;
        _values = values.ToArray();
        DomainMin = domainMin ?? new RgbColor(0, 0, 0);
        DomainMax = domainMax ?? new RgbColor(1, 1, 1);
        ValidateDomain(DomainMin, DomainMax);
    }

    public int Size { get; }

    public RgbColor DomainMin { get; }

    public RgbColor DomainMax { get; }

    public static CubeLut Identity(int size = 17)
    {
        ValidateSize(size);
        var values = new List<RgbColor>(checked(size * size * size));
        for (var red = 0; red < size; red++)
        {
            for (var green = 0; green < size; green++)
            {
                for (var blue = 0; blue < size; blue++)
                {
                    values.Add(new RgbColor(
                        red / (float)(size - 1),
                        green / (float)(size - 1),
                        blue / (float)(size - 1)));
                }
            }
        }

        return new CubeLut(size, values);
    }

    public static CubeLut Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > MaximumTextBytes)
        {
            throw new FormatException("The LUT text exceeds the supported size limit.");
        }

        int? size = null;
        var values = new List<RgbColor>();
        var domainMin = new RgbColor(0, 0, 0);
        var domainMax = new RgbColor(1, 1, 1);
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            switch (tokens[0].ToUpperInvariant())
            {
                case "TITLE":
                    continue;
                case "LUT_3D_SIZE":
                    if (tokens.Length != 2 || !int.TryParse(tokens[1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedSize))
                    {
                        throw new FormatException("LUT_3D_SIZE must contain one integer.");
                    }

                    ValidateSize(parsedSize);
                    size = parsedSize;
                    break;
                case "LUT_1D_SIZE":
                    throw new FormatException("One-dimensional LUTs are not supported.");
                case "DOMAIN_MIN":
                    domainMin = ParseColor(tokens, "DOMAIN_MIN");
                    break;
                case "DOMAIN_MAX":
                    domainMax = ParseColor(tokens, "DOMAIN_MAX");
                    break;
                default:
                    if (tokens.Length != 3 || size is null)
                    {
                        throw new FormatException("LUT data requires a preceding LUT_3D_SIZE and three channels.");
                    }

                    values.Add(ParseColor(["LUT", .. tokens], "LUT data"));
                    break;
            }
        }

        if (size is null)
        {
            throw new FormatException("LUT_3D_SIZE is required.");
        }

        try
        {
            return new CubeLut(size.Value, values, domainMin, domainMax);
        }
        catch (ArgumentException exception)
        {
            throw new FormatException("The LUT values are invalid.", exception);
        }
    }

    public RgbColor Apply(RgbColor color)
    {
        var red = Normalize(color.Red, DomainMin.Red, DomainMax.Red) * (Size - 1);
        var green = Normalize(color.Green, DomainMin.Green, DomainMax.Green) * (Size - 1);
        var blue = Normalize(color.Blue, DomainMin.Blue, DomainMax.Blue) * (Size - 1);
        var red0 = (int)MathF.Floor(red);
        var green0 = (int)MathF.Floor(green);
        var blue0 = (int)MathF.Floor(blue);
        var red1 = Math.Min(red0 + 1, Size - 1);
        var green1 = Math.Min(green0 + 1, Size - 1);
        var blue1 = Math.Min(blue0 + 1, Size - 1);
        var redFraction = red - red0;
        var greenFraction = green - green0;
        var blueFraction = blue - blue0;

        var c000 = At(red0, green0, blue0);
        var c100 = At(red1, green0, blue0);
        var c010 = At(red0, green1, blue0);
        var c110 = At(red1, green1, blue0);
        var c001 = At(red0, green0, blue1);
        var c101 = At(red1, green0, blue1);
        var c011 = At(red0, green1, blue1);
        var c111 = At(red1, green1, blue1);
        var result = Lerp(
            Lerp(Lerp(c000, c100, redFraction), Lerp(c010, c110, redFraction), greenFraction),
            Lerp(Lerp(c001, c101, redFraction), Lerp(c011, c111, redFraction), greenFraction),
            blueFraction);
        return result;
    }

    public string ToCube()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Frame Theory Studio AI 3D LUT");
        builder.AppendLine($"LUT_3D_SIZE {Size.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"DOMAIN_MIN {Format(DomainMin.Red)} {Format(DomainMin.Green)} {Format(DomainMin.Blue)}");
        builder.AppendLine($"DOMAIN_MAX {Format(DomainMax.Red)} {Format(DomainMax.Green)} {Format(DomainMax.Blue)}");
        foreach (var value in _values)
        {
            builder.AppendLine($"{Format(value.Red)} {Format(value.Green)} {Format(value.Blue)}");
        }

        return builder.ToString();
    }

    private RgbColor At(int red, int green, int blue) => _values[(red * Size + green) * Size + blue];

    private static RgbColor Lerp(RgbColor from, RgbColor to, float amount) => new(
        from.Red + (to.Red - from.Red) * amount,
        from.Green + (to.Green - from.Green) * amount,
        from.Blue + (to.Blue - from.Blue) * amount,
        from.Alpha + (to.Alpha - from.Alpha) * amount);

    private static float Normalize(float value, float minimum, float maximum) =>
        Math.Clamp((value - minimum) / (maximum - minimum), 0, 1);

    private static RgbColor ParseColor(string[] tokens, string directive)
    {
        if (tokens.Length != 4 ||
            !float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var red) ||
            !float.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var green) ||
            !float.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var blue))
        {
            throw new FormatException($"{directive} must contain three numeric channels.");
        }

        try
        {
            return new RgbColor(red, green, blue);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new FormatException($"{directive} channels must be finite values in the range 0..1.", exception);
        }
    }

    private static void ValidateSize(int size)
    {
        if (size < MinimumSize || size > MaximumSize)
        {
            throw new ArgumentOutOfRangeException(nameof(size), $"LUT size must be between {MinimumSize} and {MaximumSize}.");
        }
    }

    private static void ValidateDomain(RgbColor minimum, RgbColor maximum)
    {
        if (minimum.Red >= maximum.Red || minimum.Green >= maximum.Green || minimum.Blue >= maximum.Blue)
        {
            throw new ArgumentException("Each LUT domain minimum must be less than its maximum.");
        }
    }

    private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
