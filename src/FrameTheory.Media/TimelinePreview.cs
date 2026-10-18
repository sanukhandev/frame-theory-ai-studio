using System.Globalization;

namespace FrameTheory.Media;

public interface ITimelinePreviewRenderer
{
    Task<byte[]> RenderFrameAsync(
        TimelineVideoRenderPlan plan,
        long frameIndex,
        CancellationToken cancellationToken);
}

public sealed class FfmpegTimelinePreviewRenderer : ITimelinePreviewRenderer
{
    private const int MaximumFrameBytes = 20 * 1024 * 1024;
    private readonly IMediaProcessRunner _processRunner;
    private readonly TimelineFilterGraphBuilder _graphBuilder;

    public FfmpegTimelinePreviewRenderer(
        IMediaProcessRunner processRunner,
        TimelineFilterGraphBuilder? graphBuilder = null)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _graphBuilder = graphBuilder ?? new TimelineFilterGraphBuilder();
    }

    public async Task<byte[]> RenderFrameAsync(
        TimelineVideoRenderPlan plan,
        long frameIndex,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (frameIndex < 0 || frameIndex >= plan.DurationFrames)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        }

        var graph = _graphBuilder.Build(plan);
        var filterComplex = graph.AudioOutputLabel is null
            ? graph.FilterComplex
            : $"{graph.FilterComplex};[{graph.AudioOutputLabel}]anullsink";
        var outputDirectory = Path.GetTempPath();
        var outputPath = Path.Combine(outputDirectory, $"frame-theory-timeline-preview-{Guid.NewGuid():N}.ppm");
        try
        {
            var arguments = new List<string>();
            foreach (var input in graph.Inputs)
            {
                arguments.AddRange(["-hide_banner", "-loglevel", "error", "-y", "-i", input.LocalPath]);
            }

            arguments.AddRange([
                "-filter_complex", filterComplex,
                "-map", $"[{graph.OutputLabel}]",
                "-ss", FormatSeconds(frameIndex, plan.FrameRate),
                "-frames:v", "1",
                "-an",
                "-f", "image2",
                "-c:v", "ppm",
                outputPath
            ]);

            cancellationToken.ThrowIfCancellationRequested();
            var result = await _processRunner.RunAsync("ffmpeg", arguments, cancellationToken);
            if (result.ExitCode != 0 || !File.Exists(outputPath))
            {
                throw new MediaDecodeException($"FFmpeg could not render timeline frame: {result.StandardError.Trim()}");
            }

            var bytes = await File.ReadAllBytesAsync(outputPath, cancellationToken);
            if (bytes.Length == 0 || bytes.Length > MaximumFrameBytes)
            {
                throw new MediaDecodeException("The rendered timeline frame is empty or exceeds the supported size.");
            }

            return bytes;
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    private static string FormatSeconds(long frameIndex, Domain.RationalFrameRate frameRate) =>
        ((double)frameIndex * frameRate.Denominator / frameRate.Numerator).ToString("0.######", CultureInfo.InvariantCulture);
}
