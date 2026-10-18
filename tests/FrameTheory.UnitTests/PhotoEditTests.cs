using FrameTheory.Domain;
using FrameTheory.Photo;

namespace FrameTheory.UnitTests;

public sealed class PhotoEditTests
{
    [Xunit.Fact]
    public void EditsRenderNonDestructivelyAndUndoLastOperation()
    {
        var sourcePpm = Ppm(2, 1, [255, 0, 0, 0, 255, 0]);
        var originalPpm = sourcePpm.ToArray();
        var metadata = new MediaSourceMetadata(
            MediaId.Parse(new string('2', 64)), new Uri("file:///photo.png"), MediaKind.Image, 2, 1, null, 0);
        var document = new PhotoDocument(metadata.SourceUri, metadata, sourcePpm);
        sourcePpm[^1] = 99;
        var exposedPreview = document.PreviewPpm;
        exposedPreview[^1] = 98;
        Xunit.Assert.Equal((byte)0, document.PreviewPpm[^1]);
        var edits = new PhotoEditState(document)
            .Crop(0, 0, 2, 1)
            .Rotate(90)
            .Resize(2, 2);
        var rendered = PpmRaster.Parse(edits.RenderPreview());
        var undone = PpmRaster.Parse(edits.UndoLast().RenderPreview());

        Xunit.Assert.Equal(2, rendered.Width);
        Xunit.Assert.Equal(2, rendered.Height);
        Xunit.Assert.Equal(1, undone.Width);
        Xunit.Assert.Equal(2, undone.Height);
        Xunit.Assert.Equal(originalPpm, document.PreviewPpm);
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new PhotoEditState(document).Crop(1, 0, 2, 1));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new PhotoEditState(document).Rotate(45));
    }

    private static byte[] Ppm(int width, int height, byte[] pixels)
    {
        using var stream = new MemoryStream();
        var header = System.Text.Encoding.ASCII.GetBytes($"P6\n{width} {height}\n255\n");
        stream.Write(header);
        stream.Write(pixels);
        return stream.ToArray();
    }
}
