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
                .Add(new PhotoAdjustmentLayer("Exposure", new ColorGrade(exposureEV: 1)))
                .Add(new PhotoAdjustmentLayer("Mono", new ColorGrade(saturation: -1), isEnabled: false)));

        var restored = PhotoProjectStateJson.Deserialize(PhotoProjectStateJson.Serialize(state), source);

        Xunit.Assert.Equal(state.Layers.InspectorRows, restored.Layers.InspectorRows);
        Xunit.Assert.Equal(state.RenderPreview(), restored.RenderPreview());
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
            Task.FromResult(Ppm());
    }

    private sealed class StubProcessRunner : IMediaProcessRunner
    {
        public Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken) =>
            Task.FromResult(new MediaProcessResult(0, string.Empty, string.Empty));
    }

    private static byte[] Ppm() => [.. "P6\n2 1\n255\n"u8, 255, 0, 0, 0, 0, 255];
}
