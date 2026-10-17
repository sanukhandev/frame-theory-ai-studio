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

    private static byte[] Ppm(byte red, byte green, byte blue) =>
        [.. "P6\n1 1\n255\n"u8, red, green, blue];
}
