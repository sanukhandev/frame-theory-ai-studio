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

        var preview = await _decoder.DecodeFrameAsync(
            file,
            new RationalFrameRate(1, 1),
            0,
            cancellationToken);
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

    public PhotoEditState(PhotoDocument source, IEnumerable<PhotoEditOperation>? operations = null)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        _operations = operations?.ToArray() ?? [];
        RenderRaster();
    }

    public PhotoDocument Source { get; }

    public IReadOnlyList<PhotoEditOperation> Operations => _operations;

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

        return new PhotoEditState(Source, _operations.Take(_operations.Count - 1));
    }

    public byte[] RenderPreview() => RenderRaster().ToPpm();

    private PhotoEditState WithOperation(PhotoEditOperation operation) =>
        new(Source, _operations.Append(operation));

    private PpmRaster RenderRaster()
    {
        var raster = PpmRaster.Parse(Source.PreviewPpm);
        foreach (var operation in _operations)
        {
            raster = ApplyOperation(raster, operation);
        }

        return raster;
    }

    private static PpmRaster ApplyOperation(PpmRaster raster, PhotoEditOperation operation) => operation.Kind switch
    {
        PhotoEditOperationKind.Crop => raster.Crop(operation.First, operation.Second, operation.Third, operation.Fourth),
        PhotoEditOperationKind.Rotate => raster.Rotate(operation.First),
        PhotoEditOperationKind.Resize => raster.Resize(operation.First, operation.Second),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
}

public sealed class PpmRaster
{
    private const int MaximumPixels = 64 * 1024 * 1024;

    private PpmRaster(int width, int height, byte[] rgbBytes)
    {
        Width = width;
        Height = height;
        RgbBytes = rgbBytes;
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] RgbBytes { get; }

    public static PpmRaster Parse(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var index = 0;
        var magic = ReadToken(bytes, ref index);
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

        while (index < bytes.Length && bytes[index] is 9 or 10 or 13 or 32)
        {
            index++;
        }

        var expectedBytes = checked(width * height * 3);
        if (bytes.Length - index != expectedBytes)
        {
            throw new InvalidDataException("The PPM pixel data length is invalid.");
        }

        return new PpmRaster(width, height, bytes[index..].ToArray());
    }

    public PpmRaster Crop(int x, int y, int width, int height)
    {
        if (x < 0 || y < 0 || width <= 0 || height <= 0 || x > Width - width || y > Height - height)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "The crop rectangle must remain inside the image.");
        }

        var output = new byte[checked(width * height * 3)];
        for (var row = 0; row < height; row++)
        {
            var sourceOffset = ((y + row) * Width + x) * 3;
            var targetOffset = row * width * 3;
            RgbBytes.AsSpan(sourceOffset, width * 3).CopyTo(output.AsSpan(targetOffset, width * 3));
        }

        return new PpmRaster(width, height, output);
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
        for (var targetY = 0; targetY < height; targetY++)
        {
            var sourceY = targetY * Height / height;
            for (var targetX = 0; targetX < width; targetX++)
            {
                var sourceX = targetX * Width / width;
                var sourceOffset = (sourceY * Width + sourceX) * 3;
                var targetOffset = (targetY * width + targetX) * 3;
                RgbBytes.AsSpan(sourceOffset, 3).CopyTo(output.AsSpan(targetOffset, 3));
            }
        }

        return new PpmRaster(width, height, output);
    }

    public byte[] ToPpm()
    {
        var header = System.Text.Encoding.ASCII.GetBytes($"P6\n{Width} {Height}\n255\n");
        var output = new byte[header.Length + RgbBytes.Length];
        header.CopyTo(output, 0);
        RgbBytes.CopyTo(output, header.Length);
        return output;
    }

    private PpmRaster RotateClockwise()
    {
        var output = new byte[checked(RgbBytes.Length)];
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
            }
        }

        return new PpmRaster(Height, Width, output);
    }

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
}
