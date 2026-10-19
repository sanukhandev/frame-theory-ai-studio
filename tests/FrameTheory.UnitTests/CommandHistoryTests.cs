using FrameTheory.Domain;

namespace FrameTheory.UnitTests;

public sealed class CommandHistoryTests
{
    [Xunit.Fact]
    public void MediaReferenceCommandsRejectNullReferences()
    {
        Xunit.Assert.Throws<ArgumentNullException>(() => new AddMediaReferenceCommand(null!));
        Xunit.Assert.Throws<ArgumentNullException>(() => new RemoveMediaReferenceCommand(null!));
    }

    [Xunit.Fact]
    public void NamedGroupUndoRedoRestoresExactProjectSnapshots()
    {
        var sourceUri = new Uri("file:///media/clip.mov");
        var mediaReference = new MediaReference(
            MediaId.Create(sourceUri, "sha256:abc"),
            "media/clip.mov",
            "sha256:abc");
        var initial = new ProjectSnapshot("project", new RationalFrameRate(30, 1), 1920, 1080, "sRGB");
        var history = new EditHistory(initial);
        var group = new EditCommandGroup("Prepare clip", [
            new SetCanvasCommand(1080, 1920),
            new AddMediaReferenceCommand(mediaReference)
        ]);

        history.Execute(group);

        Xunit.Assert.Equal("Prepare clip", history.UndoDescription);
        Xunit.Assert.Equal(1080, history.Current.CanvasWidth);
        Xunit.Assert.Single(history.Current.MediaReferences);
        Xunit.Assert.True(history.Undo());
        Xunit.Assert.Equal(initial.CanvasWidth, history.Current.CanvasWidth);
        Xunit.Assert.Equal(initial.CanvasHeight, history.Current.CanvasHeight);
        Xunit.Assert.Empty(history.Current.MediaReferences);
        Xunit.Assert.True(history.Redo());
        Xunit.Assert.Equal(1080, history.Current.CanvasWidth);
        Xunit.Assert.Single(history.Current.MediaReferences);
    }

    [Xunit.Fact]
    public void FailedTransactionalGroupLeavesHistoryAndStateUnchanged()
    {
        var initial = new ProjectSnapshot("project", new RationalFrameRate(30, 1), 1920, 1080, "sRGB");
        var history = new EditHistory(initial);
        var missing = new MediaReference(
            MediaId.Create(new Uri("file:///media/missing.mov"), "sha256:missing"),
            "media/missing.mov",
            "sha256:missing");
        var group = new EditCommandGroup("Failing group", [
            new SetCanvasCommand(1080, 1920),
            new RemoveMediaReferenceCommand(missing)
        ]);

        Xunit.Assert.Throws<InvalidOperationException>(() => history.Execute(group));
        Xunit.Assert.Equal(initial.CanvasWidth, history.Current.CanvasWidth);
        Xunit.Assert.Equal(initial.CanvasHeight, history.Current.CanvasHeight);
        Xunit.Assert.Equal(0, history.UndoCount);
        Xunit.Assert.False(history.Undo());
    }

    [Xunit.Fact]
    public void HistoryIsBoundedAndNewEditsDiscardRedo()
    {
        var initial = new ProjectSnapshot("project", new RationalFrameRate(30, 1), 1920, 1080, "sRGB");
        var history = new EditHistory(initial, maximumDepth: 1);

        history.Execute(new SetCanvasCommand(1280, 720));
        history.Execute(new SetCanvasCommand(1080, 1920));
        Xunit.Assert.Equal(1, history.UndoCount);
        Xunit.Assert.True(history.Undo());
        Xunit.Assert.Equal(1280, history.Current.CanvasWidth);
        Xunit.Assert.True(history.Redo());
        history.Execute(new SetCanvasCommand(640, 480));
        Xunit.Assert.Equal(0, history.RedoCount);
        Xunit.Assert.Equal(640, history.Current.CanvasWidth);
    }
}
