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

    [Xunit.Fact]
    public void AnalysisRequestReportAndSampleProtectCallerCollectionsAndBytes()
    {
        var payload = new byte[] { 1, 2, 3 };
        var sample = new LocalAnalysisSample(new Uri("file:///sample.ppm"), MediaKind.Image, "photo-preview-ppm", null, null, payload);
        var samples = new List<LocalAnalysisSample> { sample };
        var report = new LocalMediaAnalysisReport(samples, GeminiAnalysisScope.PhotoPreview, false, payload.Length, "local");
        var requestAssets = new List<ImportedMedia> { Imported(new Uri("file:///sample.png"), MediaKind.Image, null, 0, TimeSpan.Zero) };
        var request = new GeminiAnalysisRequest(requestAssets, GeminiAnalysisScope.PhotoPreview, false);

        payload[0] = 9;
        samples.Clear();
        requestAssets.Clear();
        var exposed = sample.Payload;
        exposed[0] = 8;

        Xunit.Assert.Equal((byte)1, sample.Payload[0]);
        Xunit.Assert.Single(report.Samples);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<LocalAnalysisSample>>(report.Samples).IsReadOnly);
        Xunit.Assert.Single(request.Assets);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<ImportedMedia>>(request.Assets).IsReadOnly);
    }

    [Xunit.Fact]
    public void AnalysisContractsRejectMalformedSampleMetadata()
    {
        Xunit.Assert.Throws<ArgumentException>(() => new LocalAnalysisSample(
            new Uri("https://example.test/sample.ppm"), MediaKind.Image, "photo-preview-ppm", 0, TimeSpan.Zero, [1]));
        Xunit.Assert.Throws<ArgumentException>(() => new LocalAnalysisSample(
            new Uri("file:///sample.ppm"), MediaKind.Image, " ", 0, TimeSpan.Zero, [1]));
        var sample = new LocalAnalysisSample(new Uri("file:///sample.ppm"), MediaKind.Image, "photo-preview-ppm", 0, TimeSpan.Zero, [1]);
        Xunit.Assert.Throws<ArgumentException>(() => new LocalMediaAnalysisReport([null!], GeminiAnalysisScope.PhotoPreview, false, 1, "local"));
        Xunit.Assert.Throws<ArgumentException>(() => new LocalMediaAnalysisReport([sample], GeminiAnalysisScope.None, false, 1, "local"));
        Xunit.Assert.Throws<ArgumentException>(() => new LocalMediaAnalysisReport([sample], GeminiAnalysisScope.PhotoPreview, false, -1, "local"));
        Xunit.Assert.Throws<ArgumentException>(() => new GeminiAnalysisRequest([null!], GeminiAnalysisScope.PhotoPreview, false));
        Xunit.Assert.Throws<ArgumentException>(() => new GeminiAnalysisRequest([requestAsset()], (GeminiAnalysisScope)8, false));
    }

    [Xunit.Fact]
    public void AnalysisProgressRejectsInvalidStageAndFraction()
    {
        Xunit.Assert.Throws<ArgumentException>(() => new LocalAnalysisProgress(" ", 0));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new LocalAnalysisProgress("Preparing", double.NaN));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new LocalAnalysisProgress("Preparing", 1.1));

        var progress = new LocalAnalysisProgress(" Preparing ", 0.5);
        Xunit.Assert.Equal("Preparing", progress.Stage);
        Xunit.Assert.Equal(0.5, progress.Fraction);
    }

    private static ImportedMedia requestAsset() => Imported(new Uri("file:///sample.png"), MediaKind.Image, null, 0, TimeSpan.Zero);

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
