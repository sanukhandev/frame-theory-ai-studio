using FrameTheory.Color;
using FrameTheory.Application;
using FrameTheory.Domain;
using FrameTheory.Desktop;
using FrameTheory.Media;
using FrameTheory.Photo;
using FrameTheory.Storage;

namespace FrameTheory.UnitTests;

public sealed class PhotoProjectPersistenceTests
{
    [Xunit.Fact]
    public async Task PhotoHistoryCapabilitiesTrackUndoAndRedoAvailability()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-history-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "photo.png");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            var editor = CreateEditor(new JsonProjectRepository());

            Xunit.Assert.False(editor.CanUndo);
            Xunit.Assert.False(editor.CanRedo);
            await editor.OpenAsync(sourcePath, CancellationToken.None);
            Xunit.Assert.False(editor.CanUndo);
            Xunit.Assert.False(editor.CanRedo);

            editor.RotateRight();
            Xunit.Assert.True(editor.CanUndo);
            Xunit.Assert.False(editor.CanRedo);
            Xunit.Assert.True(editor.Undo());
            Xunit.Assert.False(editor.CanUndo);
            Xunit.Assert.True(editor.CanRedo);
            Xunit.Assert.True(editor.Redo());
            Xunit.Assert.True(editor.CanUndo);
            Xunit.Assert.False(editor.CanRedo);
            Xunit.Assert.True(editor.Undo());
            editor.RotateRight();
            Xunit.Assert.True(editor.CanUndo);
            Xunit.Assert.False(editor.CanRedo);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task PhotoProjectSaveAndOpenRestoresNonDestructiveOperations()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-project-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "photo.png");
            var projectPath = Path.Combine(directory.FullName, "photo.ftstudio");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            var repository = new JsonProjectRepository();
            var first = CreateEditor(repository);

            await first.OpenAsync(sourcePath, CancellationToken.None);
            first.RotateRight();
            first.Undo();
            first.Redo();
            Xunit.Assert.Single(first.Current!.Operations);
            Xunit.Assert.True(await first.SaveProjectAsync(projectPath, CancellationToken.None));

            var second = CreateEditor(repository);
            Xunit.Assert.True(await second.OpenProjectAsync(projectPath, CancellationToken.None));
            Xunit.Assert.Single(second.Current!.Operations);
            Xunit.Assert.Equal(PhotoEditOperationKind.Rotate, second.Current.Operations[0].Kind);
            Xunit.Assert.Equal(first.Current!.RenderExportImage(), second.Current!.RenderExportImage());
            Xunit.Assert.NotNull((await repository.LoadAsync(projectPath, CancellationToken.None)).PhotoStateJson);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void PhotoProjectStateRoundTripRestoresAdjustmentLayers()
    {
        var source = Document(new Uri("file:///photo.png"));
        var state = new PhotoEditState(source).WithLayers(
            new PhotoLayerStack()
                .Add(new PhotoAdjustmentLayer("Exposure", new ColorGrade(exposureEV: 1), maskStrokes: [new PhotoMaskStroke(0, 0, 1, 1)]))
                .Add(new PhotoAdjustmentLayer("Mono", new ColorGrade(saturation: -1), isEnabled: false)));

        var restored = PhotoProjectStateJson.Deserialize(PhotoProjectStateJson.Serialize(state), source);

        Xunit.Assert.Equal(state.Layers.InspectorRows, restored.Layers.InspectorRows);
        Xunit.Assert.Equal(state.RenderPreview(), restored.RenderPreview());
        Xunit.Assert.Single(restored.Layers.Layers[0].MaskStrokes);
    }

    [Xunit.Fact]
    public async Task PhotoProjectOpenFailureLeavesCurrentPhotoLoaded()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-project-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "photo.png");
            var projectPath = Path.Combine(directory.FullName, "photo.ftstudio");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            var repository = new JsonProjectRepository();
            var editor = CreateEditor(repository);
            await editor.OpenAsync(sourcePath, CancellationToken.None);
            editor.RotateRight();
            await File.WriteAllTextAsync(projectPath, "{not-json");

            Xunit.Assert.False(await editor.OpenProjectAsync(projectPath, CancellationToken.None));
            Xunit.Assert.Single(editor.Current!.Operations);
            Xunit.Assert.Contains("could not be opened", editor.StatusText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task PhotoProjectSaveAndOpenRestoresImportedCompositeLayer()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-composite-project-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "photo.png");
            var overlayPath = Path.Combine(directory.FullName, "overlay.png");
            var projectPath = Path.Combine(directory.FullName, "photo.ftstudio");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            await File.WriteAllBytesAsync(overlayPath, [4, 5, 6]);
            var repository = new JsonProjectRepository();
            var first = CreateEditor(repository);

            await first.OpenAsync(sourcePath, CancellationToken.None);
            await first.ImportOverlayAsync(overlayPath, 0, 0, 0.5f, PhotoBlendMode.Screen, CancellationToken.None);
            first.MaskLastOverlay(0, 0, 1, 1);
            first.ApplyRetouch(1, 0, 1, -1, 0, 1);
            Xunit.Assert.Single(first.Current!.CompositeLayers);
            Xunit.Assert.Single(first.Current.CompositeLayers[0].MaskStrokes);
            Xunit.Assert.Single(first.Current.RetouchStrokes);
            var expected = first.Current.RenderExportImage();
            Xunit.Assert.True(await first.SaveProjectAsync(projectPath, CancellationToken.None));

            var second = CreateEditor(repository);
            Xunit.Assert.True(await second.OpenProjectAsync(projectPath, CancellationToken.None));
            Xunit.Assert.Single(second.Current!.CompositeLayers);
            Xunit.Assert.Equal(PhotoBlendMode.Screen, second.Current.CompositeLayers[0].BlendMode);
            Xunit.Assert.Single(second.Current.CompositeLayers[0].MaskStrokes);
            Xunit.Assert.Single(second.Current.RetouchStrokes);
            Xunit.Assert.Equal(expected, second.Current.RenderExportImage());
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task PhotoProjectOpenRejectsChangedSourceBeforeImport()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-project-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "photo.png");
            var projectPath = Path.Combine(directory.FullName, "photo.ftstudio");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            var repository = new JsonProjectRepository();
            var editor = CreateEditor(repository);
            await editor.OpenAsync(sourcePath, CancellationToken.None);
            editor.RotateRight();
            Xunit.Assert.True(await editor.SaveProjectAsync(projectPath, CancellationToken.None));
            await File.WriteAllBytesAsync(sourcePath, [4, 5, 6]);

            Xunit.Assert.False(await editor.OpenProjectAsync(projectPath, CancellationToken.None));
            Xunit.Assert.Single(editor.Current!.Operations);
            Xunit.Assert.Contains("fingerprint", editor.StatusText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task PhotoProjectOpenRejectsSymlinkedMediaDirectory()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-project-boundary-");
        var projectDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "project"));
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-photo-project-external-");
        try
        {
            var mediaDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "media"));
            var sourcePath = Path.Combine(mediaDirectory.FullName, "photo.png");
            var externalSourcePath = Path.Combine(externalDirectory.FullName, "photo.png");
            var projectPath = Path.Combine(projectDirectory.FullName, "photo.ftstudio");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            await File.WriteAllBytesAsync(externalSourcePath, [1, 2, 3]);
            var repository = new JsonProjectRepository();
            var editor = CreateEditor(repository);
            await editor.OpenAsync(sourcePath, CancellationToken.None);
            editor.RotateRight();
            Xunit.Assert.True(await editor.SaveProjectAsync(projectPath, CancellationToken.None));

            Directory.Delete(mediaDirectory.FullName, recursive: true);
            Directory.CreateSymbolicLink(mediaDirectory.FullName, externalDirectory.FullName);

            Xunit.Assert.False(await editor.OpenProjectAsync(projectPath, CancellationToken.None));
            Xunit.Assert.Single(editor.Current!.Operations);
            Xunit.Assert.Contains("reparse", editor.StatusText, StringComparison.OrdinalIgnoreCase);
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (IOException)
        {
        }
        finally
        {
            directory.Delete(true);
            externalDirectory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task PhotoOpenRejectsSymlinkedSourceBeforeImport()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-import-boundary-");
        try
        {
            var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-photo-import-external-");
            try
            {
                var externalPath = Path.Combine(externalDirectory.FullName, "photo.png");
                var linkedPath = Path.Combine(directory.FullName, "photo.png");
                await File.WriteAllBytesAsync(externalPath, [1, 2, 3]);
                File.CreateSymbolicLink(linkedPath, externalPath);

                var editor = CreateEditor(new JsonProjectRepository());
                await editor.OpenAsync(linkedPath, CancellationToken.None);

                Xunit.Assert.Null(editor.Current);
                Xunit.Assert.Contains("symlink", editor.StatusText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                externalDirectory.Delete(true);
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (IOException)
        {
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task PhotoOverlayRejectsSymlinkedSourceBeforeImport()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-overlay-boundary-");
        try
        {
            var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-photo-overlay-external-");
            try
            {
                var sourcePath = Path.Combine(directory.FullName, "photo.png");
                var externalPath = Path.Combine(externalDirectory.FullName, "overlay.png");
                var linkedPath = Path.Combine(directory.FullName, "overlay.png");
                await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
                await File.WriteAllBytesAsync(externalPath, [4, 5, 6]);
                File.CreateSymbolicLink(linkedPath, externalPath);

                var editor = CreateEditor(new JsonProjectRepository());
                await editor.OpenAsync(sourcePath, CancellationToken.None);
                await editor.ImportOverlayAsync(linkedPath, 0, 0, 1, PhotoBlendMode.Normal, CancellationToken.None);

                Xunit.Assert.Empty(editor.Current!.CompositeLayers);
                Xunit.Assert.Contains("symlink", editor.StatusText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                externalDirectory.Delete(true);
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (IOException)
        {
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private static PhotoEditorViewModel CreateEditor(IProjectRepository repository) => new(
        new PhotoImporter(new StubProbe(), new StubDecoder()),
        new PhotoExporter(new StubProcessRunner(), new StubProbe()),
        projectRepository: repository);

    private static PhotoDocument Document(Uri source) => new(
        source,
        new MediaSourceMetadata(MediaId.Create(source, "photo-test"), source, MediaKind.Image, 2, 1, null, 0),
        Ppm());

    private sealed class StubProbe : IMediaProbe
    {
        public Task<MediaSourceMetadata> ProbeAsync(Uri file, CancellationToken cancellationToken) => Task.FromResult(
            new MediaSourceMetadata(MediaId.Create(file, "photo-test"), file, MediaKind.Image, 2, 1, null, 0));
    }

    private sealed class StubDecoder : IPreviewFrameDecoder
    {
        public Task<byte[]> DecodeFrameAsync(Uri file, RationalFrameRate frameRate, long frameIndex, CancellationToken cancellationToken) =>
            Task.FromResult(file.LocalPath.Contains("overlay", StringComparison.OrdinalIgnoreCase)
                ? Ppm(0, 255, 0, 0, 255, 0)
                : Ppm());
    }

    private sealed class StubProcessRunner : IMediaProcessRunner
    {
        public Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken) =>
            Task.FromResult(new MediaProcessResult(0, string.Empty, string.Empty));
    }

    private static byte[] Ppm() => Ppm(255, 0, 0, 0, 0, 255);

    private static byte[] Ppm(params byte[] pixels) => [.. "P6\n2 1\n255\n"u8, .. pixels];
}
