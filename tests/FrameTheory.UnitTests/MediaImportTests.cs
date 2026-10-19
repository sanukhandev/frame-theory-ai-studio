using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class MediaImportTests
{
    [Xunit.Fact]
    public void MediaImporterRequiresMediaServices()
    {
        Xunit.Assert.Throws<ArgumentNullException>(() => new FfmpegMediaImporter(null!, new StubDecoder(), new StubProcessRunner()));
        Xunit.Assert.Throws<ArgumentNullException>(() => new FfmpegMediaImporter(new StubProbe(null!), null!, new StubProcessRunner()));
        Xunit.Assert.Throws<ArgumentNullException>(() => new FfmpegMediaImporter(new StubProbe(null!), new StubDecoder(), null!));
    }

    [Xunit.Fact]
    public void ImportedMediaRejectsInvalidWaveformPeaks()
    {
        var metadata = new MediaSourceMetadata(
            MediaId.Parse(new string('b', 64)), new Uri("file:///clip.wav"), MediaKind.Audio, 0, 0, null, 0);

        Xunit.Assert.Throws<ArgumentException>(() => new ImportedMedia(metadata, null, [float.NaN]));
        Xunit.Assert.Throws<ArgumentException>(() => new ImportedMedia(metadata, null, [1.1f]));
    }

    [Xunit.Fact]
    public void ImportedMediaExposesImmutableAnalysisSnapshots()
    {
        var metadata = new MediaSourceMetadata(
            MediaId.Parse(new string('c', 64)), new Uri("file:///clip.mp4"), MediaKind.Video, 1, 1,
            new RationalFrameRate(30, 1), 1);
        var thumbnail = new byte[] { 1, 2, 3 };
        var peaks = new List<float> { 0.5f };
        var imported = new ImportedMedia(metadata, thumbnail, peaks);

        thumbnail[0] = 9;
        peaks[0] = 0;
        var exposedThumbnail = imported.ThumbnailPpm!;
        exposedThumbnail[0] = 8;

        Xunit.Assert.Equal((byte)1, imported.ThumbnailPpm![0]);
        Xunit.Assert.Equal(0.5f, imported.WaveformPeaks[0]);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<float>>(imported.WaveformPeaks).IsReadOnly);
    }

    [Xunit.Fact]
    public async Task ImportBuildsThumbnailAndWaveformWithoutMutatingSource()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-import-");
        try
        {
            var path = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllTextAsync(path, "source");
            var before = await File.ReadAllBytesAsync(path);
            var id = MediaId.Parse(new string('d', 64));
            var metadata = new MediaSourceMetadata(id, new Uri(path), MediaKind.Video, 8, 4,
                new RationalFrameRate(30, 1), 30, TimeSpan.FromSeconds(1), hasAudio: true);
            var importer = new FfmpegMediaImporter(
                new StubProbe(metadata),
                new StubDecoder(),
                new StubProcessRunner());

            var imported = await importer.ImportAsync(new Uri(path), CancellationToken.None);

            Xunit.Assert.Equal(metadata, imported.Metadata);
            Xunit.Assert.Equal("P6", System.Text.Encoding.ASCII.GetString(imported.ThumbnailPpm!, 0, 2));
            Xunit.Assert.Equal(128, imported.WaveformPeaks.Count);
            Xunit.Assert.Contains(imported.WaveformPeaks, peak => peak > 0.9f);
            Xunit.Assert.Equal(before, await File.ReadAllBytesAsync(path));
            Xunit.Assert.Empty(Directory.EnumerateFiles(Path.GetTempPath(), "frame-theory-waveform-*.pcm"));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task ImportRejectsMissingAudioOutput()
    {
        var id = MediaId.Parse(new string('e', 64));
        var path = Path.Combine(Path.GetTempPath(), "frame-theory-import-missing.wav");
        await File.WriteAllTextAsync(path, "source");
        try
        {
            var metadata = new MediaSourceMetadata(id, new Uri(path), MediaKind.Audio, 0, 0, null, 0, hasAudio: true);
            var importer = new FfmpegMediaImporter(new StubProbe(metadata), new StubDecoder(), new StubProcessRunner(writeOutput: false));
            await Xunit.Assert.ThrowsAsync<MediaImportException>(() => importer.ImportAsync(new Uri(path), CancellationToken.None));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Xunit.Fact]
    public async Task ImportFailureCleansWaveformOutput()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-import-failure-");
        try
        {
            var path = Path.Combine(directory.FullName, "clip.wav");
            await File.WriteAllTextAsync(path, "source");
            var metadata = new MediaSourceMetadata(
                MediaId.Parse(new string('a', 64)),
                new Uri(path),
                MediaKind.Audio,
                0,
                0,
                null,
                0,
                hasAudio: true);
            var importer = new FfmpegMediaImporter(
                new StubProbe(metadata),
                new StubDecoder(),
                new StubProcessRunner(writeOutput: true, exitCode: 1));

            await Xunit.Assert.ThrowsAsync<MediaImportException>(() => importer.ImportAsync(new Uri(path), CancellationToken.None));
            Xunit.Assert.Empty(Directory.EnumerateFiles(Path.GetTempPath(), "frame-theory-waveform-*.pcm"));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task ImportRejectsLinkedWaveformSource()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-import-link-");
        var external = Directory.CreateTempSubdirectory("frame-theory-import-link-target-");
        try
        {
            var target = Path.Combine(external.FullName, "clip.mp4");
            await File.WriteAllTextAsync(target, "source");
            var linked = Path.Combine(directory.FullName, "clip.mp4");
            try
            {
                File.CreateSymbolicLink(linked, target);
            }
            catch (Exception linkException) when (linkException is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                return;
            }

            var metadata = new MediaSourceMetadata(
                MediaId.Parse(new string('f', 64)),
                new Uri(linked),
                MediaKind.Video,
                8,
                4,
                new RationalFrameRate(30, 1),
                30,
                TimeSpan.FromSeconds(1),
                hasAudio: true);
            var importer = new FfmpegMediaImporter(new StubProbe(metadata), new StubDecoder(), new StubProcessRunner());

            var exception = await Xunit.Assert.ThrowsAsync<MediaImportException>(() => importer.ImportAsync(new Uri(linked), CancellationToken.None));
            Xunit.Assert.Contains("Symlinked media paths", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            directory.Delete(true);
            external.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task ImportRejectsMediaChangedDuringAnalysis()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-import-change-");
        try
        {
            var path = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllTextAsync(path, "source");
            var metadata = new MediaSourceMetadata(
                MediaId.Parse(new string('b', 64)),
                new Uri(path),
                MediaKind.Video,
                8,
                4,
                new RationalFrameRate(30, 1),
                30,
                TimeSpan.FromSeconds(1));
            var importer = new FfmpegMediaImporter(
                new StubProbe(metadata),
                new MutatingDecoder(),
                new StubProcessRunner());

            var exception = await Xunit.Assert.ThrowsAsync<MediaImportException>(() =>
                importer.ImportAsync(new Uri(path), CancellationToken.None));

            Xunit.Assert.Contains("changed while import", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            directory.Delete(true);
        }
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

    private sealed class MutatingDecoder : IPreviewFrameDecoder
    {
        public async Task<byte[]> DecodeFrameAsync(Uri file, RationalFrameRate frameRate, long frameIndex, CancellationToken cancellationToken)
        {
            await File.WriteAllTextAsync(file.LocalPath, "replacement media with a different length", cancellationToken);
            return "P6\n1 1\n255\n\0\0\0"u8.ToArray();
        }
    }

    private sealed class StubProcessRunner(bool writeOutput = true, int exitCode = 0) : IMediaProcessRunner
    {
        public Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            if (writeOutput)
            {
                var samples = new short[] { 0, 32767, -32768, 16384 };
                var bytes = new byte[samples.Length * sizeof(short)];
                for (var index = 0; index < samples.Length; index++)
                {
                    BitConverter.TryWriteBytes(bytes.AsSpan(index * sizeof(short), sizeof(short)), samples[index]);
                }

                File.WriteAllBytes(arguments[^1], bytes);
            }

            return Task.FromResult(new MediaProcessResult(exitCode, string.Empty, string.Empty));
        }
    }
}
