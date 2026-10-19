using FrameTheory.Domain;
using FrameTheory.Media;
using FrameTheory.Photo;

namespace FrameTheory.UnitTests;

public sealed class PhotoImporterTests
{
    [Xunit.Fact]
    public void PhotoImporterRequiresMediaServices()
    {
        Xunit.Assert.Throws<ArgumentNullException>(() => new PhotoImporter(null!, new StubDecoder()));
        Xunit.Assert.Throws<ArgumentNullException>(() => new PhotoImporter(new StubProbe(null!), null!));
    }

    [Xunit.Fact]
    public async Task ImporterProbesImageAndDecodesRealPreviewContract()
    {
        var id = MediaId.Parse(new string('1', 64));
        var metadata = new MediaSourceMetadata(id, new Uri("file:///photo.png"), MediaKind.Image, 8, 4, null, 0);
        var document = await new PhotoImporter(new StubProbe(metadata), new StubDecoder())
            .ImportAsync(metadata.SourceUri, CancellationToken.None);

        Xunit.Assert.Equal(MediaKind.Image, document.Metadata.Kind);
        Xunit.Assert.Equal("P6", System.Text.Encoding.ASCII.GetString(document.PreviewPpm, 0, 2));
    }

    private sealed class StubProbe(MediaSourceMetadata metadata) : IMediaProbe
    {
        public Task<MediaSourceMetadata> ProbeAsync(Uri file, CancellationToken cancellationToken) => Task.FromResult(metadata);
    }

    private sealed class StubDecoder : IPreviewFrameDecoder
    {
        public Task<byte[]> DecodeFrameAsync(Uri file, RationalFrameRate frameRate, long frameIndex, CancellationToken cancellationToken) =>
            Task.FromResult("P6\n1 1\n255\n\0\0\0"u8.ToArray());
    }
}
