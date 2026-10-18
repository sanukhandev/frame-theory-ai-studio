using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class TimelinePreviewTests
{
    [Xunit.Fact]
    public async Task RendererUsesTimelineFilterGraphAndExactTimelineTime()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-timeline-preview-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            await File.WriteAllTextAsync(sourcePath, "source");
            var plan = CreatePlan(sourcePath);
            var runner = new StubProcessRunner();
            var renderer = new FfmpegTimelinePreviewRenderer(runner);

            var result = await renderer.RenderFrameAsync(plan, 15, CancellationToken.None);

            Xunit.Assert.Equal("P6\n1 1\n255\n\0\0\0"u8.ToArray(), result);
            Xunit.Assert.Contains("-filter_complex", runner.ArgumentsSeen);
            Xunit.Assert.Contains("anullsink", runner.ArgumentsSeen);
            Xunit.Assert.Contains("-map [outv]", runner.ArgumentsSeen);
            Xunit.Assert.Contains("-ss 0.5", runner.ArgumentsSeen);
            Xunit.Assert.Contains("-frames:v 1", runner.ArgumentsSeen);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task RendererRejectsOutOfRangeAndCanceledFrames()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-timeline-preview-invalid-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            await File.WriteAllTextAsync(sourcePath, "source");
            var plan = CreatePlan(sourcePath);
            var runner = new StubProcessRunner();
            var renderer = new FfmpegTimelinePreviewRenderer(runner);

            await Xunit.Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => renderer.RenderFrameAsync(plan, plan.DurationFrames, CancellationToken.None));
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Xunit.Assert.ThrowsAnyAsync<OperationCanceledException>(() => renderer.RenderFrameAsync(plan, 0, cancellation.Token));
            Xunit.Assert.Empty(runner.ArgumentsSeen);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task RendererReportsProcessFailureAndCleansTemporaryOutput()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-timeline-preview-failure-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            await File.WriteAllTextAsync(sourcePath, "source");
            var runner = new FailingProcessRunner();

            var exception = await Xunit.Assert.ThrowsAsync<MediaDecodeException>(() => new FfmpegTimelinePreviewRenderer(runner)
                .RenderFrameAsync(CreatePlan(sourcePath), 0, CancellationToken.None));

            Xunit.Assert.Contains("no diagnostic output", exception.Message, StringComparison.Ordinal);
            Xunit.Assert.NotNull(runner.OutputPath);
            Xunit.Assert.False(File.Exists(runner.OutputPath));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private static TimelineVideoRenderPlan CreatePlan(string sourcePath)
    {
        var mediaId = MediaId.Create(new Uri(sourcePath), "preview-test");
        return new TimelineVideoRenderPlan(
            new RationalFrameRate(30, 1),
            16,
            8,
            30,
            new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
            [new TimelineVideoLayer("video-1", 0, [new TimelineVideoSegment(
                TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1)))])],
            [new TimelineAudioLayer("audio-1", 0, [new TimelineAudioSegment(
                TimelineClip.Audio(mediaId, 0, 48000, 0, 30, 48000))])]);
    }

    private sealed class StubProcessRunner : IMediaProcessRunner
    {
        public string ArgumentsSeen { get; private set; } = string.Empty;

        public Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentsSeen = string.Join(' ', arguments);
            File.WriteAllBytes(arguments[^1], "P6\n1 1\n255\n\0\0\0"u8.ToArray());
            return Task.FromResult(new MediaProcessResult(0, string.Empty, string.Empty));
        }
    }

    private sealed class FailingProcessRunner : IMediaProcessRunner
    {
        public string? OutputPath { get; private set; }

        public Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OutputPath = arguments[^1];
            return Task.FromResult(new MediaProcessResult(1, string.Empty, string.Empty));
        }
    }
}
