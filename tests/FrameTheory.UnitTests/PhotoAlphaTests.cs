using FrameTheory.Domain;
using FrameTheory.Media;
using FrameTheory.Photo;

namespace FrameTheory.UnitTests;

public sealed class PhotoAlphaTests
{
    [Xunit.Fact]
    public async Task PhotoImportAndTransformsPreserveAlphaForPngExport()
    {
        var sourceUri = new Uri("file:///alpha.png");
        var metadata = new MediaSourceMetadata(
            MediaId.Parse(new string('4', 64)), sourceUri, MediaKind.Image, 2, 1, null, 0);
        var document = await new PhotoImporter(new StubProbe(metadata), new StubDecoder())
            .ImportAsync(sourceUri, CancellationToken.None);
        var raster = PpmRaster.Parse(new PhotoEditState(document).Resize(4, 2).RenderExportImage());

        Xunit.Assert.True(raster.HasAlpha);
        Xunit.Assert.Equal(4, raster.Width);
        Xunit.Assert.Equal(2, raster.Height);
        Xunit.Assert.Equal(0, raster.AlphaBytes![0]);
        Xunit.Assert.Equal(255, raster.AlphaBytes[^1]);
    }

    private sealed class StubProbe(MediaSourceMetadata metadata) : IMediaProbe
    {
        public Task<MediaSourceMetadata> ProbeAsync(Uri file, CancellationToken cancellationToken) => Task.FromResult(metadata);
    }

    private sealed class StubDecoder : IPreviewFrameDecoder, IRgbaPreviewFrameDecoder
    {
        public Task<byte[]> DecodeFrameAsync(Uri file, RationalFrameRate frameRate, long frameIndex, CancellationToken cancellationToken) =>
            Task.FromResult("P6\n1 1\n255\n\0\0\0"u8.ToArray());

        public Task<byte[]> DecodeRgbaFrameAsync(Uri file, RationalFrameRate frameRate, long frameIndex, CancellationToken cancellationToken) =>
            Task.FromResult<byte[]>([.. "P7\nWIDTH 2\nHEIGHT 1\nDEPTH 4\nMAXVAL 255\nTUPLTYPE RGB_ALPHA\nENDHDR\n"u8, 255, 0, 0, 0, 0, 255, 0, 255]);
    }
}
