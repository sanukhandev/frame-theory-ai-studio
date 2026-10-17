using System.Text.Json;
using System.Text.Json.Serialization;
using FrameTheory.Color;
using FrameTheory.Domain;
using FrameTheory.Photo;

namespace FrameTheory.Desktop;

public static class PhotoProjectStateJson
{
    private const int MaximumBytes = 1024 * 1024;
    private const int MaximumEntries = 10_000;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(PhotoEditState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return JsonSerializer.Serialize(new PhotoProjectState
        {
            SourceMediaId = state.Source.Metadata.Id.Value,
            Operations = state.Operations.Select(operation => new PhotoOperationProjectState
            {
                Kind = operation.Kind,
                First = operation.First,
                Second = operation.Second,
                Third = operation.Third,
                Fourth = operation.Fourth
            }).ToList(),
            Layers = state.Layers.Layers.Select(layer => new PhotoAdjustmentLayerProjectState
            {
                Name = layer.Name,
                IsEnabled = layer.IsEnabled,
                ExposureEV = layer.Grade.ExposureEV,
                Contrast = layer.Grade.Contrast,
                Saturation = layer.Grade.Saturation,
                Temperature = layer.Grade.Temperature,
                Tint = layer.Grade.Tint
            }).ToList(),
            CompositeLayers = state.CompositeLayers.Select(layer => new PhotoCompositeLayerProjectState
            {
                SourceMediaId = layer.SourceMediaId.Value,
                OffsetX = layer.OffsetX,
                OffsetY = layer.OffsetY,
                Opacity = layer.Opacity,
                BlendMode = layer.BlendMode,
                MaskStrokes = layer.MaskStrokes.Select(stroke => new PhotoMaskStrokeProjectState
                {
                    CenterX = stroke.CenterX,
                    CenterY = stroke.CenterY,
                    Radius = stroke.Radius,
                    Strength = stroke.Strength
                }).ToList()
            }).ToList(),
            RetouchStrokes = state.RetouchStrokes.Select(stroke => new PhotoRetouchStrokeProjectState
            {
                CenterX = stroke.CenterX,
                CenterY = stroke.CenterY,
                Radius = stroke.Radius,
                SourceOffsetX = stroke.SourceOffsetX,
                SourceOffsetY = stroke.SourceOffsetY,
                Strength = stroke.Strength
            }).ToList()
        }, Options);
    }

    public static PhotoEditState Deserialize(
        string json,
        PhotoDocument source,
        IReadOnlyDictionary<MediaId, PhotoDocument>? compositeSources = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(source);
        if (json.Length > MaximumBytes)
        {
            throw new InvalidDataException("The photo edit state exceeds the supported size limit.");
        }

        try
        {
            var state = JsonSerializer.Deserialize<PhotoProjectState>(json, Options)
                ?? throw new InvalidDataException("The photo edit state is empty.");
            if (state.SchemaVersion != 1 || state.Operations is null || state.Layers is null || state.CompositeLayers is null || state.RetouchStrokes is null ||
                state.Operations.Count > MaximumEntries || state.Layers.Count > MaximumEntries || state.CompositeLayers.Count > MaximumEntries || state.RetouchStrokes.Count > MaximumEntries)
            {
                throw new InvalidDataException("The photo edit state schema or bounds are invalid.");
            }

            var operations = state.Operations.Select(operation => operation is null
                ? throw new InvalidDataException("The photo edit state contains a null operation.")
                : new PhotoEditOperation(operation.Kind, operation.First, operation.Second, operation.Third, operation.Fourth));
            var layers = state.Layers.Select(layer => layer is null
                ? throw new InvalidDataException("The photo edit state contains a null layer.")
                : new PhotoAdjustmentLayer(
                    layer.Name ?? throw new InvalidDataException("A photo layer name is required."),
                    new ColorGrade(layer.ExposureEV, layer.Contrast, layer.Saturation, layer.Temperature, layer.Tint),
                    layer.IsEnabled));
            var compositeLayers = state.CompositeLayers.Select(layer =>
            {
                if (layer is null || string.IsNullOrWhiteSpace(layer.SourceMediaId))
                {
                    throw new InvalidDataException("The photo state contains an invalid composite layer.");
                }

                var mediaId = MediaId.Parse(layer.SourceMediaId);
                if (compositeSources is null || !compositeSources.TryGetValue(mediaId, out var compositeSource))
                {
                    throw new InvalidDataException($"The composite layer source '{mediaId.Value}' is missing from the project.");
                }

                if (layer.MaskStrokes is null || layer.MaskStrokes.Count > MaximumEntries)
                {
                    throw new InvalidDataException("The photo state contains invalid overlay mask strokes.");
                }

                var maskStrokes = layer.MaskStrokes.Select(stroke => stroke is null
                    ? throw new InvalidDataException("The photo state contains a null overlay mask stroke.")
                    : new PhotoMaskStroke(stroke.CenterX, stroke.CenterY, stroke.Radius, stroke.Strength));
                return new PhotoCompositeLayer(compositeSource, layer.OffsetX, layer.OffsetY, layer.Opacity, layer.BlendMode, maskStrokes);
            });
            var retouchStrokes = state.RetouchStrokes.Select(stroke => stroke is null
                ? throw new InvalidDataException("The photo state contains a null retouch stroke.")
                : new PhotoRetouchStroke(stroke.CenterX, stroke.CenterY, stroke.Radius, stroke.SourceOffsetX, stroke.SourceOffsetY, stroke.Strength));
            return new PhotoEditState(source, operations, new PhotoLayerStack(layers), compositeLayers, retouchStrokes);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The photo edit state JSON is malformed.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("The photo edit state contains invalid values.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidDataException("The photo edit state contains invalid operations.", exception);
        }
    }

    private sealed class PhotoProjectState
    {
        public int SchemaVersion { get; set; } = 1;

        public string? SourceMediaId { get; set; }

        public List<PhotoOperationProjectState>? Operations { get; set; } = [];

        public List<PhotoAdjustmentLayerProjectState>? Layers { get; set; } = [];

        public List<PhotoCompositeLayerProjectState>? CompositeLayers { get; set; } = [];

        public List<PhotoRetouchStrokeProjectState>? RetouchStrokes { get; set; } = [];
    }

    private sealed class PhotoOperationProjectState
    {
        public PhotoEditOperationKind Kind { get; set; }

        public int First { get; set; }

        public int Second { get; set; }

        public int Third { get; set; }

        public int Fourth { get; set; }
    }

    private sealed class PhotoAdjustmentLayerProjectState
    {
        public string? Name { get; set; }

        public bool IsEnabled { get; set; }

        public float ExposureEV { get; set; }

        public float Contrast { get; set; }

        public float Saturation { get; set; }

        public float Temperature { get; set; }

        public float Tint { get; set; }
    }

    private sealed class PhotoCompositeLayerProjectState
    {
        public string? SourceMediaId { get; set; }

        public int OffsetX { get; set; }

        public int OffsetY { get; set; }

        public float Opacity { get; set; }

        public PhotoBlendMode BlendMode { get; set; }

        public List<PhotoMaskStrokeProjectState>? MaskStrokes { get; set; } = [];
    }

    private sealed class PhotoMaskStrokeProjectState
    {
        public int CenterX { get; set; }

        public int CenterY { get; set; }

        public int Radius { get; set; }

        public float Strength { get; set; }
    }

    private sealed class PhotoRetouchStrokeProjectState
    {
        public int CenterX { get; set; }

        public int CenterY { get; set; }

        public int Radius { get; set; }

        public int SourceOffsetX { get; set; }

        public int SourceOffsetY { get; set; }

        public float Strength { get; set; }
    }
}
