using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class TimelineCompositorTests
{
    [Xunit.Fact]
    public void VisualContractsValidateTransformsAndTransitions()
    {
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineVideoTransform(opacity: 2));
        var mediaId = MediaId.Parse(new string('a', 64));
        var first = TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1));
        var second = TimelineClip.Video(mediaId, 30, 30, 30, 30, new RationalFrameRate(30, 1));
        Xunit.Assert.Throws<ArgumentException>(() => new TimelineVideoLayer(
            "video-1",
            0,
            [new TimelineVideoSegment(first, transition: TimelineTransitionKind.CrossDissolve, transitionDurationFrames: 5)]));
        var layer = new TimelineVideoLayer(
            "video-1",
            0,
            [
                new TimelineVideoSegment(first, transform: new TimelineVideoTransform(opacity: 0.8, scale: 0.5, offsetX: 2, offsetY: 1)),
                new TimelineVideoSegment(second, transition: TimelineTransitionKind.CrossDissolve, transitionDurationFrames: 5)
            ]);
        Xunit.Assert.Equal(2, layer.Segments.Count);
    }

    [Xunit.Fact]
    public void FilterGraphBuildsOrderedOverlayAndTransitionStages()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-compositor-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            File.WriteAllText(sourcePath, "source");
            var mediaId = MediaId.Create(new Uri(sourcePath), "compositor-test");
            var first = TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1));
            var second = TimelineClip.Video(mediaId, 30, 30, 30, 30, new RationalFrameRate(30, 1));
            var plan = new TimelineVideoRenderPlan(
                new RationalFrameRate(30, 1),
                16,
                8,
                60,
                new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
                [new TimelineVideoLayer(
                    "video-1",
                    0,
                    [
                        new TimelineVideoSegment(first, transform: new TimelineVideoTransform(opacity: 0.8, scale: 0.5, offsetX: 2, offsetY: 1)),
                        new TimelineVideoSegment(second, transition: TimelineTransitionKind.WipeLeft, transitionDurationFrames: 5)
                    ])]);

            var graph = new TimelineFilterGraphBuilder().Build(plan);

            Xunit.Assert.Single(graph.Inputs);
            Xunit.Assert.Contains("xfade=transition=wipeleft", graph.FilterComplex);
            Xunit.Assert.Contains("overlay=x=0:y=0", graph.FilterComplex);
            Xunit.Assert.Contains("scale=trunc(iw*0.5/2)*2", graph.FilterComplex);
            Xunit.Assert.Contains("colorchannelmixer=aa=0.8", graph.FilterComplex);
            Xunit.Assert.EndsWith("[outv]", graph.FilterComplex, StringComparison.Ordinal);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task TimelineExportIsAtomicAndRejectsAudioUntilAudioTask()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-compositor-export-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            var destinationPath = Path.Combine(directory.FullName, "output.mp4");
            File.WriteAllText(sourcePath, "source");
            File.WriteAllText(destinationPath, "old");
            var mediaId = MediaId.Create(new Uri(sourcePath), "compositor-export-test");
            var clip = TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1));
            var plan = new TimelineVideoRenderPlan(
                new RationalFrameRate(30, 1),
                16,
                8,
                30,
                new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
                [new TimelineVideoLayer("video-1", 0, [new TimelineVideoSegment(clip)])]);
            var metadata = new MediaSourceMetadata(
                MediaId.Create(new Uri(destinationPath), "verified"),
                new Uri(destinationPath),
                MediaKind.Video,
                16,
                8,
                new RationalFrameRate(30, 1),
                30,
                TimeSpan.FromSeconds(1),
                colorProfile: "bt709/bt709/bt709/tv",
                hasAudio: false);
            var runner = new StubProcessRunner();
            var exporter = new FfmpegTimelineVideoExporter(runner, new StubProbe(metadata));
            var receipt = await exporter.ExportAsync(
                plan,
                destinationPath,
                new ExportPreset(16, 8, new RationalFrameRate(30, 1), includeAudio: false),
                null,
                CancellationToken.None);

            Xunit.Assert.Equal(destinationPath, receipt.OutputPath);
            Xunit.Assert.Equal("encoded", await File.ReadAllTextAsync(destinationPath));
            Xunit.Assert.Contains("-filter_complex", runner.ArgumentsSeen.Single());

            await Xunit.Assert.ThrowsAsync<MediaExportException>(() => exporter.ExportAsync(
                plan,
                destinationPath,
                new ExportPreset(16, 8, new RationalFrameRate(30, 1)),
                null,
                CancellationToken.None));
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

    private sealed class StubProcessRunner : IMediaProcessRunner
    {
        public List<string> ArgumentsSeen { get; } = [];

        public Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentsSeen.Add(string.Join(' ', arguments));
            File.WriteAllText(arguments[^1], "encoded");
            return Task.FromResult(new MediaProcessResult(0, string.Empty, string.Empty));
        }
    }
}
