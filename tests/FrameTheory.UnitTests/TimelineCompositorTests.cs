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
    public void SpeedRampGraphKeepsVideoAndAudioOnTheSameTimelineDuration()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-speed-ramp-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            File.WriteAllText(sourcePath, "source");
            var mediaId = MediaId.Create(new Uri(sourcePath), "speed-ramp-test");
            var frameRate = new RationalFrameRate(30, 1);
            var videoClip = TimelineClip.Video(mediaId, 0, 60, 0, 30, frameRate);
            var audioClip = TimelineClip.Audio(mediaId, 0, 96000, 0, 30, 48000);
            var speedRamp = new TimelineSpeedRamp([
                new TimelineSpeedPoint(0, 0.5),
                new TimelineSpeedPoint(15, 1),
                new TimelineSpeedPoint(30, 2)
            ]);
            var plan = new TimelineVideoRenderPlan(
                frameRate,
                16,
                8,
                30,
                new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
                [new TimelineVideoLayer("video", 0, [new TimelineVideoSegment(videoClip, speedRamp: speedRamp, stabilization: new TimelineStabilizationSettings(12))])],
                [new TimelineAudioLayer("audio", 0, [new TimelineAudioSegment(audioClip)])]);

            var graph = new TimelineFilterGraphBuilder().Build(plan);

            Xunit.Assert.Contains("setpts=(PTS-STARTPTS)/", graph.FilterComplex, StringComparison.Ordinal);
            Xunit.Assert.Contains("deshake=rx=16:ry=16:edge=mirror:search=exhaustive", graph.FilterComplex, StringComparison.Ordinal);
            Xunit.Assert.Contains("concat=n=2:v=1:a=0", graph.FilterComplex, StringComparison.Ordinal);
            Xunit.Assert.Contains("concat=n=2:v=0:a=1", graph.FilterComplex, StringComparison.Ordinal);
            Xunit.Assert.Contains("atempo=", graph.FilterComplex, StringComparison.Ordinal);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void StabilizationGraphUsesTheConfiguredSearchRadius()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-stabilization-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            File.WriteAllText(sourcePath, "source");
            var mediaId = MediaId.Create(new Uri(sourcePath), "stabilization-test");
            var clip = TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1));
            var plan = new TimelineVideoRenderPlan(
                new RationalFrameRate(30, 1),
                16,
                8,
                30,
                new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
                [new TimelineVideoLayer("video", 0, [new TimelineVideoSegment(clip, stabilization: new TimelineStabilizationSettings(5))])]);

            var graph = new TimelineFilterGraphBuilder().Build(plan);

            Xunit.Assert.Contains("deshake=rx=16:ry=16:edge=mirror:search=exhaustive", graph.FilterComplex, StringComparison.Ordinal);
        }
        finally
        {
            directory.Delete(true);
        }
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
            var animation = TimelineTransformKeyframes.FromPreset(
                TimelineAnimationPreset.FadeIn,
                0,
                30,
                new TimelineVideoTransform(opacity: 0.8, scale: 0.5, offsetX: 2, offsetY: 1));
            var plan = new TimelineVideoRenderPlan(
                new RationalFrameRate(30, 1),
                16,
                8,
                60,
                new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
                [
                    new TimelineVideoLayer(
                        "video-1",
                        0,
                        [
                            new TimelineVideoSegment(first, transform: new TimelineVideoTransform(opacity: 0.8, scale: 0.5, offsetX: 2, offsetY: 1)),
                            new TimelineVideoSegment(second, transition: TimelineTransitionKind.WipeLeft, transitionDurationFrames: 5)
                        ]),
                    new TimelineVideoLayer(
                        "animated-1",
                        1,
                        [new TimelineVideoSegment(first, animation: animation)])
                ]);

            var graph = new TimelineFilterGraphBuilder().Build(plan);

            Xunit.Assert.Single(graph.Inputs);
            Xunit.Assert.Contains("xfade=transition=wipeleft", graph.FilterComplex);
            Xunit.Assert.Contains("overlay=x=0:y=0", graph.FilterComplex);
            Xunit.Assert.Contains("scale=trunc(iw*min(if", graph.FilterComplex);
            Xunit.Assert.Contains("geq=r=", graph.FilterComplex);
            Xunit.Assert.Contains("if(lt(n\\,29)", graph.FilterComplex);
            Xunit.Assert.EndsWith("[outv]", graph.FilterComplex, StringComparison.Ordinal);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void FilterGraphBuildsEditableBeforeAfterForMultipleClips()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-before-after-multi-");
        try
        {
            var firstPath = Path.Combine(directory.FullName, "first.mp4");
            var secondPath = Path.Combine(directory.FullName, "second.mp4");
            File.WriteAllText(firstPath, "first");
            File.WriteAllText(secondPath, "second");
            var firstId = MediaId.Create(new Uri(firstPath), "before-after-first");
            var secondId = MediaId.Create(new Uri(secondPath), "before-after-second");
            var frameRate = new RationalFrameRate(30, 1);
            var first = TimelineClip.Video(firstId, 0, 30, 0, 30, frameRate);
            var second = TimelineClip.Video(secondId, 0, 30, 30, 30, frameRate);
            var plan = new TimelineVideoRenderPlan(
                frameRate,
                16,
                8,
                60,
                new Dictionary<MediaId, Uri>
                {
                    [firstId] = new Uri(firstPath),
                    [secondId] = new Uri(secondPath)
                },
                [new TimelineVideoLayer("video-1", 0, [new TimelineVideoSegment(first), new TimelineVideoSegment(second)])],
                colorGrades: new Dictionary<MediaId, FrameTheory.Color.ColorGrade>
                {
                    [secondId] = new(0.2f, 0.1f, 0.1f, 0, 0)
                },
                beforeAfterStyle: TimelineRevealStyle.Split,
                beforeAfterRevealFrame: 30);

            var graph = new TimelineFilterGraphBuilder().Build(plan);

            Xunit.Assert.Equal(2, graph.Inputs.Count);
            Xunit.Assert.Equal(2, graph.FilterComplex.Split("concat=n=2:v=1:a=0", StringSplitOptions.None).Length - 1);
            Xunit.Assert.Contains("beforeafter_result", graph.FilterComplex, StringComparison.Ordinal);
            Xunit.Assert.Contains("eq=brightness=", graph.FilterComplex, StringComparison.Ordinal);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void FilterGraphAddsOnlyExplicitValidatedWatermarkInput()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-watermark-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            var watermarkPath = Path.Combine(directory.FullName, "watermark.png");
            File.WriteAllText(sourcePath, "source");
            File.WriteAllText(watermarkPath, "watermark");
            var mediaId = MediaId.Create(new Uri(sourcePath), "watermark-source");
            var clip = TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1));
            var plan = new TimelineVideoRenderPlan(
                new RationalFrameRate(30, 1),
                16,
                8,
                30,
                new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
                [new TimelineVideoLayer("video-1", 0, [new TimelineVideoSegment(clip)])],
                watermark: new TimelineWatermark(watermarkPath, 0.5f, 12));

            var graph = new TimelineFilterGraphBuilder().Build(plan);

            Xunit.Assert.Equal(2, graph.Inputs.Count);
            Xunit.Assert.Contains("scale=w='min(iw,4)':h='min(ih,2)':force_original_aspect_ratio=decrease", graph.FilterComplex, StringComparison.Ordinal);
            Xunit.Assert.Contains("colorchannelmixer=aa=0.5,loop=loop=-1:size=1:start=0", graph.FilterComplex, StringComparison.Ordinal);
            Xunit.Assert.Contains("overlay=x=W-w-12:y=H-h-12", graph.FilterComplex, StringComparison.Ordinal);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task TimelineExportIsAtomicAndMapsTimelineAudio()
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
            var audioClip = TimelineClip.Audio(mediaId, 0, 48000, 0, 30, 48000);
            var plan = new TimelineVideoRenderPlan(
                new RationalFrameRate(30, 1),
                16,
                8,
                30,
                new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
                [new TimelineVideoLayer("video-1", 0, [new TimelineVideoSegment(clip)])],
                [new TimelineAudioLayer("audio-1", 0, [new TimelineAudioSegment(audioClip)])]);
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
                hasAudio: true);
            var runner = new StubProcessRunner();
            var exporter = new FfmpegTimelineVideoExporter(runner, new StubProbe(metadata));
            var receipt = await exporter.ExportAsync(
                plan,
                destinationPath,
                new ExportPreset(16, 8, new RationalFrameRate(30, 1)),
                null,
                CancellationToken.None);

            Xunit.Assert.Equal(destinationPath, receipt.OutputPath);
            Xunit.Assert.Equal("encoded", await File.ReadAllTextAsync(destinationPath));
            Xunit.Assert.Contains("-filter_complex", runner.ArgumentsSeen.Single());
            Xunit.Assert.Contains("-map [aout]", runner.ArgumentsSeen.Single());
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task TimelineExportRejectsSymlinkedDestinationDirectory()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-compositor-boundary-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-compositor-external-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            await File.WriteAllTextAsync(sourcePath, "source");
            var destinationDirectory = Path.Combine(directory.FullName, "exports");
            Directory.CreateSymbolicLink(destinationDirectory, externalDirectory.FullName);
            var destinationPath = Path.Combine(destinationDirectory, "output.mp4");
            var mediaId = MediaId.Create(new Uri(sourcePath), "compositor-boundary-test");
            var clip = TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1));
            var plan = new TimelineVideoRenderPlan(
                new RationalFrameRate(30, 1),
                16,
                8,
                30,
                new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
                [new TimelineVideoLayer("video-1", 0, [new TimelineVideoSegment(clip)])]);
            var exporter = new FfmpegTimelineVideoExporter(
                new StubProcessRunner(),
                new StubProbe(new MediaSourceMetadata(
                    MediaId.Create(new Uri(destinationPath), "verified"),
                    new Uri(destinationPath),
                    MediaKind.Video,
                    16,
                    8,
                    new RationalFrameRate(30, 1),
                    30,
                    TimeSpan.FromSeconds(1),
                    colorProfile: "bt709/bt709/bt709/tv")));

            await Xunit.Assert.ThrowsAsync<MediaExportException>(() => exporter.ExportAsync(
                plan,
                destinationPath,
                new ExportPreset(16, 8, new RationalFrameRate(30, 1), includeAudio: false),
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
