using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FrameTheory.Domain;

namespace FrameTheory.Color;

public sealed class ColorPreset
{
    public ColorPreset(string name, ColorGrade grade, CubeLut? lut = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A preset name is required.", nameof(name));
        }

        Name = name.Trim();
        Grade = grade ?? throw new ArgumentNullException(nameof(grade));
        Lut = lut;
    }

    public string Name { get; }

    public ColorGrade Grade { get; }

    public CubeLut? Lut { get; }

    public override string ToString() => Name;

    public RgbColor Apply(ColorGraph graph, RgbColor source)
    {
        var graded = new ColorPipeline(graph).Transform(source, Grade);
        return Lut?.Apply(graded) ?? graded;
    }
}

public sealed class JsonColorPresetStore
{
    private const long MaximumPresetBytes = 5 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task SaveAtomicAsync(string path, IEnumerable<ColorPreset> presets, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(presets);
        var presetArray = presets.ToArray();
        if (presetArray.GroupBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Preset names must be unique.", nameof(presets));
        }

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("A preset directory is required.", nameof(path));
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(directory);
        }

        EnsureRegularPath(fullPath, directory);

        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporaryPath, new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.CreateNew,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous | FileOptions.WriteThrough
            }))
            {
                await JsonSerializer.SerializeAsync(stream, new PresetFile(presetArray.Select(PresetDto.FromPreset).ToArray()), JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public async Task<IReadOnlyList<ColorPreset>> LoadAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("The preset file was not found.", fullPath);
        }

        EnsureRegularPath(fullPath, fileInfo.DirectoryName
            ?? throw new ArgumentException("A preset directory is required.", nameof(path)));

        if (fileInfo.Length > MaximumPresetBytes)
        {
            throw new InvalidDataException("The preset file exceeds the supported size limit.");
        }

        await using var stream = new FileStream(fullPath, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Share = FileShare.Read,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });
        try
        {
            var file = await JsonSerializer.DeserializeAsync<PresetFile>(stream, JsonOptions, cancellationToken)
                ?? throw new InvalidDataException("The preset file is empty.");
            var presets = file.Presets?.Select(item => item.ToPreset()).ToArray() ?? [];
            if (presets.GroupBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            {
                throw new InvalidDataException("Preset names must be unique.");
            }

            return presets;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The preset JSON is malformed.", exception);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("The preset contains an invalid LUT.", exception);
        }
    }

    private sealed record PresetFile(PresetDto[] Presets);

    private static void EnsureRegularPath(string fullPath, string directory)
    {
        if (LocalPathSecurity.ContainsReparsePoint(directory))
        {
            throw new InvalidDataException("The preset directory must not be a reparse point.");
        }

        if (File.Exists(fullPath) && LocalPathSecurity.ContainsReparsePoint(fullPath))
        {
            throw new InvalidDataException("The preset file must not be a reparse point.");
        }
    }

    private sealed class PresetDto
    {
        public string? Name { get; set; }

        public float ExposureEV { get; set; }

        public float Contrast { get; set; }

        public float Saturation { get; set; }

        public float Temperature { get; set; }

        public float Tint { get; set; }

        public string? LutCube { get; set; }

        public static PresetDto FromPreset(ColorPreset preset) => new()
        {
            Name = preset.Name,
            ExposureEV = preset.Grade.ExposureEV,
            Contrast = preset.Grade.Contrast,
            Saturation = preset.Grade.Saturation,
            Temperature = preset.Grade.Temperature,
            Tint = preset.Grade.Tint,
            LutCube = preset.Lut?.ToCube()
        };

        public ColorPreset ToPreset()
        {
            try
            {
                return new ColorPreset(
                    Name ?? throw new InvalidDataException("Preset name is required."),
                    new ColorGrade(ExposureEV, Contrast, Saturation, Temperature, Tint),
                    string.IsNullOrWhiteSpace(LutCube) ? null : CubeLut.Parse(LutCube));
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Preset values are invalid.", exception);
            }
        }
    }
}
