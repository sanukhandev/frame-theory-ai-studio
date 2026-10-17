using FrameTheory.Color;
using FrameTheory.Domain;
using FrameTheory.Photo;

namespace FrameTheory.UnitTests;

public sealed class PhotoLayerTests
{
    [Xunit.Fact]
    public void LayersHaveInspectableOrderEnableStateAndUndoableHistory()
    {
        var metadata = new MediaSourceMetadata(
            MediaId.Parse(new string('3', 64)), new Uri("file:///photo.png"), MediaKind.Image, 1, 1, null, 0);
        var document = new PhotoDocument(metadata.SourceUri, metadata, Ppm(128, 128, 128));
        var baseState = new PhotoEditState(document);
        var stack = new PhotoLayerStack()
            .Add(new PhotoAdjustmentLayer("Exposure", new ColorGrade(exposureEV: 1)))
            .Add(new PhotoAdjustmentLayer("Mono", new ColorGrade(saturation: -1)));
        var reordered = stack.Move(1, 0).SetEnabled("Mono", false);
        var edited = baseState.WithLayers(reordered);
        var history = new PhotoEditHistory(baseState);
        history.Execute(edited);

        Xunit.Assert.Equal(["Off: Mono", "On: Exposure"], reordered.InspectorRows);
        Xunit.Assert.NotEqual(baseState.RenderPreview(), edited.RenderPreview());
        Xunit.Assert.True(history.Undo());
        Xunit.Assert.Equal(baseState.RenderPreview(), history.Current.RenderPreview());
        Xunit.Assert.True(history.Redo());
        Xunit.Assert.Equal(edited.Layers.InspectorRows, history.Current.Layers.InspectorRows);
    }

    [Xunit.Fact]
    public void CompositeLayerBlendsPixelsWithOpacityAndSupportsClipping()
    {
        var baseDocument = Document(new Uri("file:///base.png"), [100, 100, 100, 20, 40, 60]);
        var overlayDocument = Document(new Uri("file:///overlay.png"), [200, 0, 50, 255, 255, 0]);
        var state = new PhotoEditState(baseDocument).AddCompositeLayer(
            new PhotoCompositeLayer(overlayDocument, offsetX: 1, opacity: 0.5f));

        var rendered = PpmRaster.Parse(state.RenderPreview());

        Xunit.Assert.Equal([100, 100, 100, 110, 20, 55], rendered.RgbBytes);
        Xunit.Assert.Single(state.CompositeLayers);
    }

    [Xunit.Fact]
    public void CompositeLayerSupportsMultiplyAndScreenModes()
    {
        var baseDocument = Document(new Uri("file:///base.png"), [100, 100, 100, 20, 40, 60]);
        var overlayDocument = Document(new Uri("file:///overlay.png"), [200, 0, 50, 255, 255, 0]);

        var multiply = new PhotoEditState(baseDocument).AddCompositeLayer(
            new PhotoCompositeLayer(overlayDocument, blendMode: PhotoBlendMode.Multiply));
        var screen = new PhotoEditState(baseDocument).AddCompositeLayer(
            new PhotoCompositeLayer(overlayDocument, blendMode: PhotoBlendMode.Screen));

        Xunit.Assert.Equal([78, 0, 19, 20, 40, 0], PpmRaster.Parse(multiply.RenderPreview()).RgbBytes);
        Xunit.Assert.Equal([222, 100, 131, 255, 255, 60], PpmRaster.Parse(screen.RenderPreview()).RgbBytes);
    }

    [Xunit.Fact]
    public void CompositeLayerSurvivesSubsequentEditOperations()
    {
        var baseDocument = Document(new Uri("file:///base.png"), [100, 100, 100, 20, 40, 60]);
        var overlayDocument = Document(new Uri("file:///overlay.png"), [200, 0, 50, 255, 255, 0]);
        var edited = new PhotoEditState(baseDocument)
            .AddCompositeLayer(new PhotoCompositeLayer(overlayDocument, blendMode: PhotoBlendMode.Screen))
            .Rotate(90);

        Xunit.Assert.Single(edited.CompositeLayers);
        Xunit.Assert.Equal(PhotoBlendMode.Screen, edited.CompositeLayers[0].BlendMode);
    }

    [Xunit.Fact]
    public void BrushMaskHidesOverlayAndRetouchCopiesSourcePixels()
    {
        var baseDocument = Document(new Uri("file:///base.png"), [100, 0, 0, 0, 100, 0]);
        var overlayDocument = Document(new Uri("file:///overlay.png"), [200, 200, 200, 200, 200, 200]);
        var edited = new PhotoEditState(baseDocument)
            .AddCompositeLayer(new PhotoCompositeLayer(overlayDocument))
            .MaskLastCompositeLayer(new PhotoMaskStroke(0, 0, 1, 1));
        var retouched = new PhotoEditState(baseDocument)
            .AddRetouchStroke(new PhotoRetouchStroke(1, 0, 1, -1, 0, 1));

        var rendered = PpmRaster.Parse(edited.RenderPreview());
        Xunit.Assert.Equal([100, 0, 0, 200, 200, 200], rendered.RgbBytes);
        Xunit.Assert.Equal([100, 0, 0, 100, 0, 0], PpmRaster.Parse(retouched.RenderPreview()).RgbBytes);
        Xunit.Assert.Single(edited.CompositeLayers[0].MaskStrokes);
        Xunit.Assert.Single(retouched.RetouchStrokes);
    }

    private static PhotoDocument Document(Uri source, byte[] pixels) => new(
        source,
        new MediaSourceMetadata(MediaId.Create(source, "photo-layer-test"), source, MediaKind.Image, 2, 1, null, 0),
        [.. "P6\n2 1\n255\n"u8, .. pixels]);

    private static byte[] Ppm(byte red, byte green, byte blue) =>
        [.. "P6\n1 1\n255\n"u8, red, green, blue];
}
