using FrameTheory.Domain;
using FrameTheory.Media;
using System.Security.Cryptography;

namespace FrameTheory.UnitTests;

public sealed class MediaExportTests
{
    [Xunit.Fact]
    public void MediaExporterRequiresMediaServices()
    {
        Xunit.Assert.Throws<ArgumentNullException>(() => new FfmpegMediaExporter(null!, new StubProbe(null!)));
        Xunit.Assert.Throws<ArgumentNullException>(() => new FfmpegMediaExporter(new StubProcessRunner(), null!));
    }

    [Xunit.Fact]
    public void ExportPresetReportsTheInvalidDimension()
    {
        var frameRate = new RationalFrameRate(30, 1);

        Xunit.Assert.Equal("width", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new ExportPreset(0, 4, frameRate)).ParamName);
        Xunit.Assert.Equal("height", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new ExportPreset(8, 0, frameRate)).ParamName);
        Xunit.Assert.Throws<ArgumentException>(() => new ExportPreset(8, 4, default));
    }

    [Xunit.Fact]
    public async Task ExportRejectsBlankDestinationPath()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-export-path-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            await File.WriteAllTextAsync(sourcePath, "source");
            var exporter = new FfmpegMediaExporter(new StubProcessRunner(), new StubProbe(Metadata(new Uri(sourcePath), true)));

            await Xunit.Assert.ThrowsAsync<ArgumentException>(() => exporter.ExportAsync(
                new Uri(sourcePath), " ", new ExportPreset(8, 4, new RationalFrameRate(30, 1)), null, CancellationToken.None));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task ExportVerifiesStreamsAndAtomicallyReplacesDestination()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-export-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            var destinationPath = Path.Combine(directory.FullName, "output.mp4");
            await File.WriteAllTextAsync(sourcePath, "source");
            var sourceChecksum = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(sourcePath)));
            await File.WriteAllTextAsync(destinationPath, "old");
            var metadata = Metadata(new Uri(destinationPath), hasAudio: true);
            var runner = new StubProcessRunner();
            var progress = new List<ExportProgress>();
            var exporter = new FfmpegMediaExporter(runner, new StubProbe(metadata));

            var receipt = await exporter.ExportAsync(
                new Uri(sourcePath),
                destinationPath,
                new ExportPreset(8, 4, new RationalFrameRate(30, 1)),
                new InlineProgress(progress.Add),
                CancellationToken.None);

            Xunit.Assert.Equal(destinationPath, receipt.OutputPath);
            Xunit.Assert.True(receipt.OutputBytes > 0);
            Xunit.Assert.Equal("encoded", await File.ReadAllTextAsync(destinationPath));
            Xunit.Assert.Equal(["Encoding", "Verifying", "Completed"], progress.Select(item => item.Stage));
            Xunit.Assert.Contains("-i " + sourcePath, runner.ArgumentsSeen.Single());
            Xunit.Assert.Contains("-f mp4", runner.ArgumentsSeen.Single());
            Xunit.Assert.Contains("-color_primaries bt709", runner.ArgumentsSeen.Single());
            Xunit.Assert.Contains("setparams=color_primaries=bt709", runner.ArgumentsSeen.Single());
            Xunit.Assert.Equal("source", await File.ReadAllTextAsync(sourcePath));
            Xunit.Assert.Equal(sourceChecksum, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(sourcePath))));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task FailedOrCanceledExportLeavesExistingDestinationIntact()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-export-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            var destinationPath = Path.Combine(directory.FullName, "output.mp4");
            await File.WriteAllTextAsync(sourcePath, "source");
            await File.WriteAllTextAsync(destinationPath, "old");
            var preset = new ExportPreset(8, 4, new RationalFrameRate(30, 1));
            var failing = new FfmpegMediaExporter(new StubProcessRunner(exitCode: 1), new StubProbe(Metadata(new Uri(destinationPath), true)));
            await Xunit.Assert.ThrowsAsync<MediaExportException>(() => failing.ExportAsync(new Uri(sourcePath), destinationPath, preset, null, CancellationToken.None));
            Xunit.Assert.Equal("old", await File.ReadAllTextAsync(destinationPath));

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var canceled = new FfmpegMediaExporter(new StubProcessRunner(), new StubProbe(Metadata(new Uri(destinationPath), true)));
            await Xunit.Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled.ExportAsync(new Uri(sourcePath), destinationPath, preset, null, cancellation.Token));
            Xunit.Assert.Equal("old", await File.ReadAllTextAsync(destinationPath));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task LinuxExportAllowsCaseVariantOutputName()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var directory = Directory.CreateTempSubdirectory("frame-theory-export-case-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            var destinationPath = Path.Combine(directory.FullName, "SOURCE.mp4");
            await File.WriteAllTextAsync(sourcePath, "source");
            var exporter = new FfmpegMediaExporter(
                new StubProcessRunner(),
                new StubProbe(Metadata(new Uri(destinationPath), hasAudio: false)));

            var receipt = await exporter.ExportAsync(
                new Uri(sourcePath),
                destinationPath,
                new ExportPreset(8, 4, new RationalFrameRate(30, 1), includeAudio: false),
                null,
                CancellationToken.None);

            Xunit.Assert.Equal(destinationPath, receipt.OutputPath);
            Xunit.Assert.Equal("source", await File.ReadAllTextAsync(sourcePath));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task ExportRejectsSymlinkedDestinationDirectory()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-export-boundary-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-export-external-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            await File.WriteAllTextAsync(sourcePath, "source");
            var destinationDirectory = Path.Combine(directory.FullName, "exports");
            Directory.CreateSymbolicLink(destinationDirectory, externalDirectory.FullName);
            var destinationPath = Path.Combine(destinationDirectory, "output.mp4");
            var exporter = new FfmpegMediaExporter(
                new StubProcessRunner(),
                new StubProbe(Metadata(new Uri(destinationPath), hasAudio: true)));

            await Xunit.Assert.ThrowsAsync<MediaExportException>(() => exporter.ExportAsync(
                new Uri(sourcePath),
                destinationPath,
                new ExportPreset(8, 4, new RationalFrameRate(30, 1)),
                null,
                CancellationToken.None));
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

    private static MediaSourceMetadata Metadata(Uri uri, bool hasAudio) => new(
        MediaId.Create(uri, "export-test"), uri, MediaKind.Video, 8, 4,
        new RationalFrameRate(30, 1), 30, TimeSpan.FromSeconds(1), colorProfile: "bt709/bt709/bt709/tv", hasAudio: hasAudio);

    private sealed class StubProbe(MediaSourceMetadata metadata) : IMediaProbe
    {
        public Task<MediaSourceMetadata> ProbeAsync(Uri file, CancellationToken cancellationToken) => Task.FromResult(metadata);
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
