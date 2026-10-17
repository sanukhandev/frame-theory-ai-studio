using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class MediaImportTests
{
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

    private sealed class StubProbe(MediaSourceMetadata metadata) : IMediaProbe
    {
        public Task<MediaSourceMetadata> ProbeAsync(Uri file, CancellationToken cancellationToken) => Task.FromResult(metadata);
    }

    private sealed class StubDecoder : IPreviewFrameDecoder
    {
        public Task<byte[]> DecodeFrameAsync(Uri file, RationalFrameRate frameRate, long frameIndex, CancellationToken cancellationToken) =>
            Task.FromResult("P6\n1 1\n255\n\0\0\0"u8.ToArray());
    }

    private sealed class StubProcessRunner(bool writeOutput = true) : IMediaProcessRunner
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

            return Task.FromResult(new MediaProcessResult(0, string.Empty, string.Empty));
        }
    }
}
