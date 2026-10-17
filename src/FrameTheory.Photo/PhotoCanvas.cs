using FrameTheory.Color;
using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.Photo;

public sealed record PhotoDocument
{
    public PhotoDocument(Uri sourceUri, MediaSourceMetadata metadata, byte[] previewPpm)
    {
        ArgumentNullException.ThrowIfNull(sourceUri);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(previewPpm);
        if (metadata.Kind != MediaKind.Image)
        {
            throw new ArgumentException("A photo document requires image metadata.", nameof(metadata));
        }

        if (previewPpm.Length == 0)
        {
            throw new ArgumentException("A decoded photo preview is required.", nameof(previewPpm));
        }

        SourceUri = sourceUri;
        Metadata = metadata;
        PreviewPpm = previewPpm;
    }

    public Uri SourceUri { get; }

    public MediaSourceMetadata Metadata { get; }

    public byte[] PreviewPpm { get; }
}

public sealed class PhotoImporter
{
    private readonly IMediaProbe _probe;
    private readonly IPreviewFrameDecoder _decoder;

    public PhotoImporter(IMediaProbe probe, IPreviewFrameDecoder decoder)
    {
        _probe = probe;
        _decoder = decoder;
    }

    public async Task<PhotoDocument> ImportAsync(Uri file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        var metadata = await _probe.ProbeAsync(file, cancellationToken);
        if (metadata.Kind != MediaKind.Image)
        {
            throw new InvalidOperationException("The selected media is not a still image.");
        }

        var preview = _decoder is IRgbaPreviewFrameDecoder rgbaDecoder
            ? await rgbaDecoder.DecodeRgbaFrameAsync(file, new RationalFrameRate(1, 1), 0, cancellationToken)
            : await _decoder.DecodeFrameAsync(file, new RationalFrameRate(1, 1), 0, cancellationToken);
        return new PhotoDocument(file, metadata, preview);
    }
}

public readonly record struct PhotoViewport
{
    public PhotoViewport(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }
}

public sealed class PhotoCanvasState
{
    private const double MinimumZoom = 0.01;
    private const double MaximumZoom = 32;
    private readonly PhotoDocument _document;

    public PhotoCanvasState(PhotoDocument document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        Zoom = 1;
    }

    public double Zoom { get; private set; }

    public double OffsetX { get; private set; }

    public double OffsetY { get; private set; }

    public void Fit(PhotoViewport viewport)
    {
        Zoom = Math.Min(
            (double)viewport.Width / _document.Metadata.Width,
            (double)viewport.Height / _document.Metadata.Height);
        OffsetX = (viewport.Width - _document.Metadata.Width * Zoom) / 2;
        OffsetY = (viewport.Height - _document.Metadata.Height * Zoom) / 2;
    }

    public void Set100Percent(PhotoViewport viewport)
    {
        Zoom = 1;
        OffsetX = (viewport.Width - _document.Metadata.Width) / 2;
        OffsetY = (viewport.Height - _document.Metadata.Height) / 2;
    }

    public void SetZoom(double zoom, PhotoViewport viewport)
    {
        if (double.IsNaN(zoom) || double.IsInfinity(zoom))
        {
            throw new ArgumentOutOfRangeException(nameof(zoom));
        }

        Zoom = Math.Clamp(zoom, MinimumZoom, MaximumZoom);
        OffsetX = (viewport.Width - _document.Metadata.Width * Zoom) / 2;
        OffsetY = (viewport.Height - _document.Metadata.Height * Zoom) / 2;
    }

    public void Pan(double deltaX, double deltaY)
    {
        if (double.IsNaN(deltaX) || double.IsInfinity(deltaX) || double.IsNaN(deltaY) || double.IsInfinity(deltaY))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaX));
        }

        OffsetX += deltaX;
        OffsetY += deltaY;
    }
}

public enum PhotoEditOperationKind
{
    Crop,
    Rotate,
    Resize
}

public sealed record PhotoEditOperation(PhotoEditOperationKind Kind, int First, int Second, int Third, int Fourth)
{
    public static PhotoEditOperation Crop(int x, int y, int width, int height) => new(PhotoEditOperationKind.Crop, x, y, width, height);

    public static PhotoEditOperation Rotate(int degrees) => new(PhotoEditOperationKind.Rotate, degrees, 0, 0, 0);

    public static PhotoEditOperation Resize(int width, int height) => new(PhotoEditOperationKind.Resize, width, height, 0, 0);
}

public sealed class PhotoEditState
{
    private readonly IReadOnlyList<PhotoEditOperation> _operations;

    public PhotoEditState(
        PhotoDocument source,
        IEnumerable<PhotoEditOperation>? operations = null,
        PhotoLayerStack? layers = null)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        _operations = operations?.ToArray() ?? [];
        Layers = layers ?? new PhotoLayerStack();
        RenderRaster();
    }

    public PhotoDocument Source { get; }

    public IReadOnlyList<PhotoEditOperation> Operations => _operations;

    public PhotoLayerStack Layers { get; }

    public PhotoEditState Crop(int x, int y, int width, int height)
    {
        var operation = PhotoEditOperation.Crop(x, y, width, height);
        ApplyOperation(RenderRaster(), operation);
        return WithOperation(operation);
    }

    public PhotoEditState Rotate(int degrees)
    {
        if (degrees % 90 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(degrees));
        }

        var operation = PhotoEditOperation.Rotate(degrees);
        ApplyOperation(RenderRaster(), operation);
        return WithOperation(operation);
    }

    public PhotoEditState Resize(int width, int height)
    {
        var operation = PhotoEditOperation.Resize(width, height);
        ApplyOperation(RenderRaster(), operation);
        return WithOperation(operation);
    }

    public PhotoEditState UndoLast()
    {
        if (_operations.Count == 0)
        {
            return this;
        }

        return new PhotoEditState(Source, _operations.Take(_operations.Count - 1), Layers);
    }

    public byte[] RenderPreview() => RenderRaster().ToPpm();

    public byte[] RenderExportImage() => RenderRaster().ToExportImage();

    public PhotoEditState WithLayers(PhotoLayerStack layers) =>
        new(Source, _operations, layers);

    private PhotoEditState WithOperation(PhotoEditOperation operation) =>
        new(Source, _operations.Append(operation), Layers);

    private PpmRaster RenderRaster()
    {
        var raster = PpmRaster.Parse(Source.PreviewPpm);
        foreach (var operation in _operations)
        {
            raster = ApplyOperation(raster, operation);
        }

        return Layers.Apply(raster);
    }

    private static PpmRaster ApplyOperation(PpmRaster raster, PhotoEditOperation operation) => operation.Kind switch
    {
        PhotoEditOperationKind.Crop => raster.Crop(operation.First, operation.Second, operation.Third, operation.Fourth),
        PhotoEditOperationKind.Rotate => raster.Rotate(operation.First),
        PhotoEditOperationKind.Resize => raster.Resize(operation.First, operation.Second),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
}

public sealed class PhotoAdjustmentLayer
{
    public PhotoAdjustmentLayer(string name, ColorGrade grade, bool isEnabled = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An adjustment layer name is required.", nameof(name));
        }

        Name = name.Trim();
        Grade = grade ?? throw new ArgumentNullException(nameof(grade));
        IsEnabled = isEnabled;
    }

    public string Name { get; }

    public ColorGrade Grade { get; }

    public bool IsEnabled { get; }

    public PhotoAdjustmentLayer WithEnabled(bool isEnabled) => new(Name, Grade, isEnabled);
}

public sealed class PhotoLayerStack
{
    private readonly IReadOnlyList<PhotoAdjustmentLayer> _layers;

    public PhotoLayerStack(IEnumerable<PhotoAdjustmentLayer>? layers = null)
    {
        _layers = layers?.ToArray() ?? [];
        if (_layers.GroupBy(layer => layer.Name, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Adjustment layer names must be unique.", nameof(layers));
        }
    }

    public IReadOnlyList<PhotoAdjustmentLayer> Layers => _layers;

    public IReadOnlyList<string> InspectorRows =>
        _layers.Select(layer => $"{(layer.IsEnabled ? "On" : "Off")}: {layer.Name}").ToArray();

    public PhotoLayerStack Add(PhotoAdjustmentLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (_layers.Any(existing => string.Equals(existing.Name, layer.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("An adjustment layer with that name already exists.", nameof(layer));
        }

        return new PhotoLayerStack(_layers.Append(layer));
    }

    public PhotoLayerStack Remove(string name) =>
        new PhotoLayerStack(_layers.Where(layer => !string.Equals(layer.Name, name, StringComparison.OrdinalIgnoreCase)));

    public PhotoLayerStack Move(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _layers.Count || toIndex < 0 || toIndex >= _layers.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(fromIndex));
        }

        var reordered = _layers.ToList();
        var layer = reordered[fromIndex];
        reordered.RemoveAt(fromIndex);
        reordered.Insert(toIndex, layer);
        return new PhotoLayerStack(reordered);
    }

    public PhotoLayerStack SetEnabled(string name, bool isEnabled)
    {
        var found = false;
        var updated = _layers.Select(layer =>
        {
            if (!string.Equals(layer.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return layer;
            }

            found = true;
            return layer.WithEnabled(isEnabled);
        }).ToArray();
        if (!found)
        {
            throw new KeyNotFoundException($"Adjustment layer '{name}' was not found.");
        }

        return new PhotoLayerStack(updated);
    }

    public PpmRaster Apply(PpmRaster raster)
    {
        var result = raster;
        foreach (var layer in _layers.Where(layer => layer.IsEnabled))
        {
            result = result.Apply(ColorGraph.ForSrgb(), layer.Grade);
        }

        return result;
    }
}

public sealed class PhotoEditHistory
{
    private readonly List<PhotoEditState> _undo = [];
    private readonly List<PhotoEditState> _redo = [];

    public PhotoEditHistory(PhotoEditState initialState) => Current = initialState ?? throw new ArgumentNullException(nameof(initialState));

    public PhotoEditState Current { get; private set; }

    public void Execute(PhotoEditState nextState)
    {
        ArgumentNullException.ThrowIfNull(nextState);
        _undo.Add(Current);
        _redo.Clear();
        Current = nextState;
    }

    public bool Undo()
    {
        if (_undo.Count == 0)
        {
            return false;
        }

        _redo.Add(Current);
        Current = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        return true;
    }

    public bool Redo()
    {
        if (_redo.Count == 0)
        {
            return false;
        }

        _undo.Add(Current);
        Current = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        return true;
    }
}

public sealed class PpmRaster
{
    private const int MaximumPixels = 64 * 1024 * 1024;

    private PpmRaster(int width, int height, byte[] rgbBytes, byte[]? alphaBytes = null)
    {
        Width = width;
        Height = height;
        RgbBytes = rgbBytes;
        AlphaBytes = alphaBytes;
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] RgbBytes { get; }

    public byte[]? AlphaBytes { get; }

    public bool HasAlpha => AlphaBytes is not null;

    public static PpmRaster Parse(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var index = 0;
        var magic = ReadToken(bytes, ref index);
        if (magic == "P7")
        {
            return ParsePam(bytes);
        }

        if (magic != "P6" || !int.TryParse(ReadToken(bytes, ref index), out var width) ||
            !int.TryParse(ReadToken(bytes, ref index), out var height) ||
            !int.TryParse(ReadToken(bytes, ref index), out var maximumValue) || maximumValue != 255)
        {
            throw new InvalidDataException("Only 8-bit binary PPM (P6) previews are supported.");
        }

        if (width <= 0 || height <= 0 || (long)width * height > MaximumPixels)
        {
            throw new InvalidDataException("The PPM dimensions are invalid or exceed the supported size.");
        }

        ConsumeBinarySeparator(bytes, ref index);

        var expectedBytes = checked(width * height * 3);
        if (bytes.Length - index != expectedBytes)
        {
            throw new InvalidDataException("The PPM pixel data length is invalid.");
        }

        return new PpmRaster(width, height, bytes[index..].ToArray());
    }

    public byte[] ToExportImage() => HasAlpha ? ToPam() : ToPpm();

    public PpmRaster Crop(int x, int y, int width, int height)
    {
        if (x < 0 || y < 0 || width <= 0 || height <= 0 || x > Width - width || y > Height - height)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "The crop rectangle must remain inside the image.");
        }

        var output = new byte[checked(width * height * 3)];
        var alpha = AlphaBytes is null ? null : new byte[checked(width * height)];
        for (var row = 0; row < height; row++)
        {
            var sourceOffset = ((y + row) * Width + x) * 3;
            var targetOffset = row * width * 3;
            RgbBytes.AsSpan(sourceOffset, width * 3).CopyTo(output.AsSpan(targetOffset, width * 3));
            AlphaBytes?.AsSpan((y + row) * Width + x, width).CopyTo(alpha!.AsSpan(row * width, width));
        }

        return new PpmRaster(width, height, output, alpha);
    }

    public PpmRaster Rotate(int degrees)
    {
        var normalized = ((degrees % 360) + 360) % 360;
        return normalized switch
        {
            0 => this,
            90 => RotateClockwise(),
            180 => RotateClockwise().RotateClockwise(),
            270 => RotateClockwise().RotateClockwise().RotateClockwise(),
            _ => throw new ArgumentOutOfRangeException(nameof(degrees))
        };
    }

    public PpmRaster Resize(int width, int height)
    {
        if (width <= 0 || height <= 0 || (long)width * height > MaximumPixels)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        var output = new byte[checked(width * height * 3)];
        var alpha = AlphaBytes is null ? null : new byte[checked(width * height)];
        for (var targetY = 0; targetY < height; targetY++)
        {
            var sourceY = targetY * Height / height;
            for (var targetX = 0; targetX < width; targetX++)
            {
                var sourceX = targetX * Width / width;
                var sourceOffset = (sourceY * Width + sourceX) * 3;
                var targetOffset = (targetY * width + targetX) * 3;
                RgbBytes.AsSpan(sourceOffset, 3).CopyTo(output.AsSpan(targetOffset, 3));
                if (alpha is not null)
                {
                    alpha[targetY * width + targetX] = AlphaBytes![sourceY * Width + sourceX];
                }
            }
        }

        return new PpmRaster(width, height, output, alpha);
    }

    public PpmRaster Apply(ColorGraph graph, ColorGrade grade)
    {
        var pipeline = new ColorPipeline(graph);
        var output = RgbBytes.ToArray();
        for (var index = 0; index < output.Length; index += 3)
        {
            var color = pipeline.Transform(
                new RgbColor(output[index] / 255f, output[index + 1] / 255f, output[index + 2] / 255f),
                grade);
            output[index] = ToByte(color.Red);
            output[index + 1] = ToByte(color.Green);
            output[index + 2] = ToByte(color.Blue);
        }

        return new PpmRaster(Width, Height, output, AlphaBytes?.ToArray());
    }

    public byte[] ToPpm()
    {
        var header = System.Text.Encoding.ASCII.GetBytes($"P6\n{Width} {Height}\n255\n");
        var output = new byte[header.Length + RgbBytes.Length];
        header.CopyTo(output, 0);
        RgbBytes.CopyTo(output, header.Length);
        return output;
    }

    public byte[] ToPam()
    {
        if (AlphaBytes is null)
        {
            throw new InvalidOperationException("The raster has no alpha channel.");
        }

        var header = System.Text.Encoding.ASCII.GetBytes($"P7\nWIDTH {Width}\nHEIGHT {Height}\nDEPTH 4\nMAXVAL 255\nTUPLTYPE RGB_ALPHA\nENDHDR\n");
        var output = new byte[checked(header.Length + RgbBytes.Length + AlphaBytes.Length)];
        header.CopyTo(output, 0);
        var offset = header.Length;
        for (var index = 0; index < AlphaBytes.Length; index++)
        {
            var rgbOffset = index * 3;
            output[offset++] = RgbBytes[rgbOffset];
            output[offset++] = RgbBytes[rgbOffset + 1];
            output[offset++] = RgbBytes[rgbOffset + 2];
            output[offset++] = AlphaBytes[index];
        }

        return output;
    }

    private PpmRaster RotateClockwise()
    {
        var output = new byte[checked(RgbBytes.Length)];
        var alpha = AlphaBytes is null ? null : new byte[checked(Width * Height)];
        var outputWidth = Height;
        for (var sourceY = 0; sourceY < Height; sourceY++)
        {
            for (var sourceX = 0; sourceX < Width; sourceX++)
            {
                var targetX = Height - 1 - sourceY;
                var targetY = sourceX;
                var sourceOffset = (sourceY * Width + sourceX) * 3;
                var targetOffset = (targetY * outputWidth + targetX) * 3;
                RgbBytes.AsSpan(sourceOffset, 3).CopyTo(output.AsSpan(targetOffset, 3));
                if (alpha is not null)
                {
                    alpha[targetY * outputWidth + targetX] = AlphaBytes![sourceY * Width + sourceX];
                }
            }
        }

        return new PpmRaster(Height, Width, output, alpha);
    }

    private static byte ToByte(float value) => (byte)Math.Clamp((int)MathF.Round(value * 255), 0, 255);

    private static string ReadToken(byte[] bytes, ref int index)
    {
        while (index < bytes.Length && (bytes[index] is 9 or 10 or 13 or 32 || bytes[index] == (byte)'#'))
        {
            if (bytes[index] == (byte)'#')
            {
                while (index < bytes.Length && bytes[index] != 10)
                {
                    index++;
                }
            }
            else
            {
                index++;
            }
        }

        var start = index;
        while (index < bytes.Length && bytes[index] > 32)
        {
            index++;
        }

        return System.Text.Encoding.ASCII.GetString(bytes, start, index - start);
    }

    private static PpmRaster ParsePam(byte[] bytes)
    {
        var headerEnd = System.Text.Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 4096))
            .IndexOf("ENDHDR", StringComparison.Ordinal);
        if (headerEnd < 0)
        {
            throw new InvalidDataException("The PAM header is invalid.");
        }

        var dataStart = headerEnd + "ENDHDR".Length;
        ConsumeBinarySeparator(bytes, ref dataStart);

        var header = System.Text.Encoding.ASCII.GetString(bytes, 0, dataStart);
        var values = header.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
        if (!int.TryParse(values.GetValueOrDefault("WIDTH"), out var width) ||
            !int.TryParse(values.GetValueOrDefault("HEIGHT"), out var height) ||
            values.GetValueOrDefault("DEPTH") != "4" ||
            values.GetValueOrDefault("MAXVAL") != "255" ||
            values.GetValueOrDefault("TUPLTYPE") != "RGB_ALPHA" ||
            width <= 0 || height <= 0 || (long)width * height > MaximumPixels)
        {
            throw new InvalidDataException("Only 8-bit RGBA PAM previews are supported.");
        }

        var expectedBytes = checked(width * height * 4);
        if (bytes.Length - dataStart != expectedBytes)
        {
            throw new InvalidDataException("The PAM pixel data length is invalid.");
        }

        var rgb = new byte[checked(width * height * 3)];
        var alpha = new byte[checked(width * height)];
        for (var index = 0; index < alpha.Length; index++)
        {
            var sourceOffset = index * 4;
            var targetOffset = index * 3;
            rgb[targetOffset] = bytes[dataStart + sourceOffset];
            rgb[targetOffset + 1] = bytes[dataStart + sourceOffset + 1];
            rgb[targetOffset + 2] = bytes[dataStart + sourceOffset + 2];
            alpha[index] = bytes[dataStart + sourceOffset + 3];
        }

        return new PpmRaster(width, height, rgb, alpha);
    }

    private static void ConsumeBinarySeparator(byte[] bytes, ref int index)
    {
        if (index >= bytes.Length || bytes[index] is not (9 or 10 or 13 or 32))
        {
            throw new InvalidDataException("The binary image header is missing its data separator.");
        }

        var separator = bytes[index++];
        if (separator == 13 && index < bytes.Length && bytes[index] == 10)
        {
            index++;
        }
    }
}
