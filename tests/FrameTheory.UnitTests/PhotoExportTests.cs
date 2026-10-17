using FrameTheory.Domain;
using FrameTheory.Media;
using FrameTheory.Photo;

namespace FrameTheory.UnitTests;

public sealed class PhotoExportTests
{
    [Xunit.Fact]
    public async Task ExportRendersEditsVerifiesImageAndAtomicallyReplacesDestination()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-export-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.png");
            var destinationPath = Path.Combine(directory.FullName, "output.jpg");
            await File.WriteAllTextAsync(sourcePath, "source");
            await File.WriteAllTextAsync(destinationPath, "old");
            var document = Document(new Uri(sourcePath));
            var runner = new StubProcessRunner();
            var progress = new List<ExportProgress>();
            var exporter = new PhotoExporter(runner, new StubProbe(new Uri(destinationPath)));

            var receipt = await exporter.ExportAsync(
                new PhotoEditState(document).Resize(2, 1),
                destinationPath,
                new PhotoExportOptions(PhotoExportFormat.Jpeg, 80),
                new InlineProgress(progress.Add),
                CancellationToken.None);

            Xunit.Assert.Equal(destinationPath, receipt.OutputPath);
            Xunit.Assert.True(receipt.OutputBytes > 0);
            Xunit.Assert.Equal("encoded", await File.ReadAllTextAsync(destinationPath));
            Xunit.Assert.Equal(["Rendering", "Encoding", "Verifying", "Completed"], progress.Select(item => item.Stage));
            Xunit.Assert.Contains("-c:v mjpeg", runner.ArgumentsSeen.Single());
            Xunit.Assert.Contains("setparams=color_primaries=bt709", runner.ArgumentsSeen.Single());
            Xunit.Assert.Contains("-q:v 8", runner.ArgumentsSeen.Single());
            Xunit.Assert.Contains("-map_metadata 1", runner.ArgumentsSeen.Single());
            Xunit.Assert.Contains("source_color_profile=sRGB", runner.ArgumentsSeen.Single());
            Xunit.Assert.Equal("source", await File.ReadAllTextAsync(sourcePath));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task FailedExportLeavesExistingDestinationIntactAndQualityIsBounded()
    {
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new PhotoExportOptions((PhotoExportFormat)99));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new PhotoExportOptions(PhotoExportFormat.Png, 0));
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-export-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.png");
            var destinationPath = Path.Combine(directory.FullName, "output.png");
            await File.WriteAllTextAsync(sourcePath, "source");
            await File.WriteAllTextAsync(destinationPath, "old");
            var exporter = new PhotoExporter(
                new StubProcessRunner(exitCode: 1),
                new StubProbe(new Uri(destinationPath)));

            await Xunit.Assert.ThrowsAsync<PhotoExportException>(() => exporter.ExportAsync(
                new PhotoEditState(Document(new Uri(sourcePath))),
                destinationPath,
                new PhotoExportOptions(PhotoExportFormat.Png),
                null,
                CancellationToken.None));
            Xunit.Assert.Equal("old", await File.ReadAllTextAsync(destinationPath));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task SocialPhotoExportCropsToExactPresetDimensions()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-social-export-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.png");
            var destinationPath = Path.Combine(directory.FullName, "output.jpg");
            await File.WriteAllTextAsync(sourcePath, "source");
            var runner = new StubProcessRunner();
            var exporter = new PhotoExporter(runner, new StubProbe(new Uri(destinationPath), 4, 4));

            var receipt = await exporter.ExportAsync(
                new PhotoEditState(Document(new Uri(sourcePath))),
                destinationPath,
                new PhotoExportOptions(PhotoExportFormat.Jpeg, 80, SocialPhotoExportPreset.Create(SocialAspectRatio.Square1By1, 4)),
                null,
                CancellationToken.None);

            Xunit.Assert.Equal((4, 4), (receipt.VerifiedMetadata.Width, receipt.VerifiedMetadata.Height));
            Xunit.Assert.Contains("scale=4:4:force_original_aspect_ratio=increase,crop=4:4", runner.ArgumentsSeen.Single());
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private static PhotoDocument Document(Uri sourceUri) => new(
        sourceUri,
        new MediaSourceMetadata(
            MediaId.Create(sourceUri, "photo-export-test"), sourceUri, MediaKind.Image, 1, 1, null, 0,
            colorProfile: "sRGB"),
        [.. "P6\n1 1\n255\n"u8, 128, 64, 32]);

    private sealed class StubProbe(Uri sourceUri, int width = 2, int height = 1) : IMediaProbe
    {
        public Task<MediaSourceMetadata> ProbeAsync(Uri file, CancellationToken cancellationToken) => Task.FromResult(
            new MediaSourceMetadata(
                MediaId.Create(file, "photo-export-output"), sourceUri, MediaKind.Image, width, height, null, 0,
                colorProfile: "bt709/iec61966-2-1/gbr/pc"));
    }

    private sealed class StubProcessRunner(int exitCode = 0) : IMediaProcessRunner
    {
        public List<string> ArgumentsSeen { get; } = [];

        public Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentsSeen.Add(string.Join(' ', arguments));
            if (exitCode == 0)
            {
                File.WriteAllText(arguments[^1], "encoded");
            }

            return Task.FromResult(new MediaProcessResult(exitCode, string.Empty, string.Empty));
        }
    }

    private sealed class InlineProgress(Action<ExportProgress> callback) : IProgress<ExportProgress>
    {
        public void Report(ExportProgress value) => callback(value);
    }
}
