using FrameTheory.AI;
using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class LocalAnalysisTests
{
    [Xunit.Fact]
    public async Task SamplerBuildsBoundedSamplesForSelectedScopesWithoutUploading()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-analysis-");
        try
        {
            var videoPath = Path.Combine(directory.FullName, "video.mp4");
            var imagePath = Path.Combine(directory.FullName, "photo.png");
            var audioPath = Path.Combine(directory.FullName, "audio.wav");
            await File.WriteAllTextAsync(videoPath, "video");
            await File.WriteAllTextAsync(imagePath, "image");
            await File.WriteAllTextAsync(audioPath, "audio");
            var video = Imported(new Uri(videoPath), MediaKind.Video, new RationalFrameRate(30, 1), 90, TimeSpan.FromSeconds(3));
            var image = Imported(new Uri(imagePath), MediaKind.Image, null, 0, TimeSpan.Zero, thumbnail: [1, 2, 3]);
            var audio = Imported(new Uri(audioPath), MediaKind.Audio, null, 0, TimeSpan.FromSeconds(1), waveform: [0.1f, 0.5f]);
            var decoder = new StubDecoder();

            var report = await new LocalAnalysisSampler(decoder).PrepareAsync(
                new GeminiAnalysisRequest([video, image, audio], GeminiAnalysisScope.VideoFrames | GeminiAnalysisScope.PhotoPreview | GeminiAnalysisScope.AudioWaveform, uploadConsent: false),
                null,
                CancellationToken.None);

            Xunit.Assert.Equal(5, report.Samples.Count);
            Xunit.Assert.Equal(3, decoder.FrameIndices.Count);
            Xunit.Assert.False(report.UploadConsent);
            Xunit.Assert.Contains("No upload consent was given", report.Disclosure);
            Xunit.Assert.True(report.EstimatedUploadBytes > 0);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task SamplerHonorsCancellationBeforeReadingMedia()
    {
        var source = new Uri(Path.Combine(Path.GetTempPath(), "analysis-cancel.mp4"));
        var asset = Imported(source, MediaKind.Video, new RationalFrameRate(30, 1), 30, TimeSpan.FromSeconds(1));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Xunit.Assert.ThrowsAsync<OperationCanceledException>(() => new LocalAnalysisSampler(new StubDecoder()).PrepareAsync(
            new GeminiAnalysisRequest([asset], GeminiAnalysisScope.VideoFrames, uploadConsent: true),
            null,
            cancellation.Token));
    }

    private static ImportedMedia Imported(
        Uri source,
        MediaKind kind,
        RationalFrameRate? frameRate,
        long durationFrames,
        TimeSpan duration,
        byte[]? thumbnail = null,
        IReadOnlyList<float>? waveform = null) =>
        new(
            new MediaSourceMetadata(
                MediaId.Create(source, source.LocalPath),
                source,
                kind,
                kind == MediaKind.Video ? 16 : 2,
                kind == MediaKind.Video ? 8 : 2,
                frameRate,
                durationFrames,
                duration,
                hasAudio: kind == MediaKind.Audio),
            thumbnail,
            waveform ?? []);

    private sealed class StubDecoder : IPreviewFrameDecoder
    {
        public List<long> FrameIndices { get; } = [];

        public Task<byte[]> DecodeFrameAsync(Uri file, RationalFrameRate frameRate, long frameIndex, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FrameIndices.Add(frameIndex);
            return Task.FromResult(new byte[] { 80, 54, 10, 10, 255, 255, 255, 255, 255, 255 });
        }
    }
}
