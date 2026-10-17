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
