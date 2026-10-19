using FrameTheory.Domain;
using FrameTheory.Media;
using FrameTheory.Photo;

namespace FrameTheory.UnitTests;

public sealed class PhotoCanvasTests
{
    [Xunit.Fact]
    public void ViewportReportsTheInvalidDimension()
    {
        Xunit.Assert.Equal("height", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new PhotoViewport(400, 0)).ParamName);
    }

    [Xunit.Fact]
    public void ImportRequiresAnImageAndCanvasSupportsFit100ZoomAndPan()
    {
        var imageId = MediaId.Parse(new string('f', 64));
        var metadata = new MediaSourceMetadata(imageId, new Uri("file:///photo.png"), MediaKind.Image, 800, 400, null, 0);
        var document = new PhotoDocument(metadata.SourceUri, metadata, "P6\n1 1\n255\n\0\0\0"u8.ToArray());
        var canvas = new PhotoCanvasState(document);
        var viewport = new PhotoViewport(400, 400);

        canvas.Fit(viewport);
        Xunit.Assert.Equal(0.5, canvas.Zoom, 6);
        Xunit.Assert.Equal(0, canvas.OffsetX, 6);
        Xunit.Assert.Equal(100, canvas.OffsetY, 6);
        canvas.Set100Percent(viewport);
        Xunit.Assert.Equal(1, canvas.Zoom);
        canvas.SetZoom(100, viewport);
        Xunit.Assert.Equal(32, canvas.Zoom);
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => canvas.Fit(default));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => canvas.Set100Percent(default));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => canvas.SetZoom(1, default));
        canvas.Pan(12, -8);
        Xunit.Assert.Equal(-12588, canvas.OffsetX, 6);
        Xunit.Assert.Equal(-6208, canvas.OffsetY, 6);
        Xunit.Assert.Throws<ArgumentException>(() => new PhotoDocument(
            new Uri("file:///video.mp4"),
            new MediaSourceMetadata(imageId, new Uri("file:///video.mp4"), MediaKind.Video, 1, 1, new RationalFrameRate(30, 1), 1),
            [1]));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new PhotoDocument(
            new Uri("file:///empty.png"),
            new MediaSourceMetadata(imageId, new Uri("file:///empty.png"), MediaKind.Image, 0, 1, null, 0),
            [1]));
    }
}
