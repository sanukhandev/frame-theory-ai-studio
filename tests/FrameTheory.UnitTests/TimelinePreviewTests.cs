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
            var mediaId = MediaId.Create(new Uri(sourcePath), "preview-test");
            var plan = new TimelineVideoRenderPlan(
                new RationalFrameRate(30, 1),
                16,
                8,
                30,
                new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
                [new TimelineVideoLayer("video-1", 0, [new TimelineVideoSegment(
                    TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1)))])]);
            var runner = new StubProcessRunner();
            var renderer = new FfmpegTimelinePreviewRenderer(runner);

            var result = await renderer.RenderFrameAsync(plan, 15, CancellationToken.None);

            Xunit.Assert.Equal("P6\n1 1\n255\n\0\0\0"u8.ToArray(), result);
            Xunit.Assert.Contains("-filter_complex", runner.ArgumentsSeen);
            Xunit.Assert.Contains("-map [outv]", runner.ArgumentsSeen);
            Xunit.Assert.Contains("-ss 0.5", runner.ArgumentsSeen);
            Xunit.Assert.Contains("-frames:v 1", runner.ArgumentsSeen);
        }
        finally
        {
            directory.Delete(true);
        }
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
}
