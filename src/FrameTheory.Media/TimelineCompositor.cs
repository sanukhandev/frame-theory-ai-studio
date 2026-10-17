using System.Globalization;
using FrameTheory.Domain;

namespace FrameTheory.Media;

public sealed record TimelineVideoRenderPlan
{
    public TimelineVideoRenderPlan(
        RationalFrameRate frameRate,
        int width,
        int height,
        long durationFrames,
        IReadOnlyDictionary<MediaId, Uri> sources,
        IReadOnlyList<TimelineVideoLayer> layers)
    {
        if (width <= 0 || height <= 0 || durationFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(layers);
        FrameRate = frameRate;
        Width = width;
        Height = height;
        DurationFrames = durationFrames;
        Sources = sources;
        Layers = layers.OrderBy(layer => layer.Order).ToArray();
    }

    public RationalFrameRate FrameRate { get; }

    public int Width { get; }

    public int Height { get; }

    public long DurationFrames { get; }

    public IReadOnlyDictionary<MediaId, Uri> Sources { get; }

    public IReadOnlyList<TimelineVideoLayer> Layers { get; }
}

public sealed record TimelineFilterGraph(string FilterComplex, IReadOnlyList<Uri> Inputs, string OutputLabel);

public sealed class TimelineFilterGraphBuilder
{
    public TimelineFilterGraph Build(TimelineVideoRenderPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var usedMediaIds = plan.Layers.SelectMany(layer => layer.Segments).Select(segment => segment.Clip.MediaId).Distinct().ToArray();
        var inputs = usedMediaIds.Select(mediaId => GetSource(plan, mediaId)).ToArray();
        var inputIndexes = usedMediaIds.Select((mediaId, index) => (mediaId, index)).ToDictionary(item => item.mediaId, item => item.index);
        var filters = new List<string>();
        var baseLabel = "base0";
        var durationSeconds = FormatSeconds(plan.DurationFrames, plan.FrameRate);
        filters.Add($"color=c=black@0.0:s={plan.Width}x{plan.Height}:r={Rate(plan.FrameRate)}:d={durationSeconds},format=yuva420p[{baseLabel}]");
        var currentBase = baseLabel;
        var layerIndex = 0;
        foreach (var layer in plan.Layers)
        {
            if (layer.Segments.Count == 0)
            {
                continue;
            }

            var layerStream = BuildLayer(plan, layer, inputIndexes, filters, layerIndex++);
            var nextBase = $"base{layerIndex}";
            filters.Add($"[{currentBase}][{layerStream}]overlay=x=0:y=0:eof_action=pass:shortest=0:format=auto[{nextBase}]");
            currentBase = nextBase;
        }

        filters.Add($"[{currentBase}]format=yuv420p,setparams=color_primaries=bt709:color_trc=bt709:colorspace=bt709:range=tv[outv]");
        return new TimelineFilterGraph(string.Join(';', filters), inputs, "outv");
    }

    private static string BuildLayer(
        TimelineVideoRenderPlan plan,
        TimelineVideoLayer layer,
        IReadOnlyDictionary<MediaId, int> inputIndexes,
        ICollection<string> filters,
        int layerIndex)
    {
        var contiguous = layer.Segments.Zip(layer.Segments.Skip(1), (previous, current) =>
            previous.Clip.TimelineRange.EndFrameExclusive == current.Clip.TimelineRange.StartFrame).All(value => value);
        if (!contiguous && layer.Segments.Any(segment => segment.Transition is not null))
        {
            throw new MediaExportException("Transitions require contiguous visual segments on one layer.");
        }

        if (!contiguous)
        {
            var labels = new List<string>();
            for (var index = 0; index < layer.Segments.Count; index++)
            {
                var segmentLabel = $"layer{layerIndex}segment{index}";
                BuildSegment(plan, layer.Segments[index], inputIndexes, filters, segmentLabel, includeTimelineOffset: true);
                labels.Add(segmentLabel);
            }

            return OverlaySegments(filters, labels, layerIndex);
        }

        var sequenceLabel = $"layer{layerIndex}sequence";
        var previousLabel = string.Empty;
        var accumulatedFrames = 0L;
        for (var index = 0; index < layer.Segments.Count; index++)
        {
            var segment = layer.Segments[index];
            var segmentLabel = $"layer{layerIndex}segment{index}";
            BuildSegment(plan, segment, inputIndexes, filters, segmentLabel, includeTimelineOffset: false);
            if (index == 0)
            {
                previousLabel = segmentLabel;
                accumulatedFrames = segment.Clip.TimelineRange.Length;
                continue;
            }

            var nextLabel = index == layer.Segments.Count - 1 ? sequenceLabel : $"layer{layerIndex}sequence{index}";
            if (segment.Transition is null)
            {
                filters.Add($"[{previousLabel}][{segmentLabel}]concat=n=2:v=1:a=0:unsafe=1[{nextLabel}]");
                accumulatedFrames += segment.Clip.TimelineRange.Length;
            }
            else
            {
                var duration = FormatSeconds(segment.TransitionDurationFrames, plan.FrameRate);
                var offset = FormatSeconds(accumulatedFrames - segment.TransitionDurationFrames, plan.FrameRate);
                var transition = segment.Transition == TimelineTransitionKind.CrossDissolve ? "fade" : "wipeleft";
                filters.Add($"[{previousLabel}][{segmentLabel}]xfade=transition={transition}:duration={duration}:offset={offset}[{nextLabel}]");
                accumulatedFrames += segment.Clip.TimelineRange.Length - segment.TransitionDurationFrames;
            }

            previousLabel = nextLabel;
        }

        var timedLabel = $"layer{layerIndex}timed";
        var firstStart = layer.Segments[0].Clip.TimelineRange.StartFrame;
        filters.Add($"[{previousLabel}]setpts=PTS-STARTPTS+{FormatSeconds(firstStart, plan.FrameRate)}/TB[{timedLabel}]");
        return timedLabel;
    }

    private static void BuildSegment(
        TimelineVideoRenderPlan plan,
        TimelineVideoSegment segment,
        IReadOnlyDictionary<MediaId, int> inputIndexes,
        ICollection<string> filters,
        string label,
        bool includeTimelineOffset)
    {
        var transform = segment.Transform;
        var clip = segment.Clip;
        var input = inputIndexes[clip.MediaId];
        var sourceEnd = checked(clip.SourceStartUnit + clip.SourceUnitCount);
        var scale = Format(transform.Scale);
        var opacity = Format(transform.Opacity);
        var filter = $"[{input}:v:0]trim=start_frame={clip.SourceStartUnit}:end_frame={sourceEnd},setpts=PTS-STARTPTS,fps={Rate(plan.FrameRate)},scale=trunc(iw*{scale}/2)*2:trunc(ih*{scale}/2)*2,format=yuva420p,pad={plan.Width}:{plan.Height}:{transform.OffsetX}:{transform.OffsetY}:color=black@0,colorchannelmixer=aa={opacity}";
        if (includeTimelineOffset)
        {
            filter += $",setpts=PTS-STARTPTS+{FormatSeconds(clip.TimelineRange.StartFrame, plan.FrameRate)}/TB";
        }

        filters.Add($"{filter}[{label}]");
    }

    private static string OverlaySegments(ICollection<string> filters, IReadOnlyList<string> labels, int layerIndex)
    {
        var current = labels[0];
        for (var index = 1; index < labels.Count; index++)
        {
            var next = $"layer{layerIndex}overlay{index}";
            filters.Add($"[{current}][{labels[index]}]overlay=x=0:y=0:eof_action=pass:shortest=0:format=auto[{next}]");
            current = next;
        }

        return current;
    }

    private static Uri GetSource(TimelineVideoRenderPlan plan, MediaId mediaId)
    {
        if (!plan.Sources.TryGetValue(mediaId, out var source) || !source.IsFile)
        {
            throw new MediaExportException($"No local source is registered for media '{mediaId.Value}'.");
        }

        var path = Path.GetFullPath(source.LocalPath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("A timeline source file was not found.", path);
        }

        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new MediaExportException("Symlinked timeline sources are not accepted.");
        }

        return new Uri(path);
    }

    private static string Rate(RationalFrameRate frameRate) => $"{frameRate.Numerator}/{frameRate.Denominator}";

    private static string FormatSeconds(long frames, RationalFrameRate frameRate) =>
        Format((double)frames * frameRate.Denominator / frameRate.Numerator);

    private static string Format(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}

public interface ITimelineVideoExporter
{
    Task<ExportReceipt> ExportAsync(
        TimelineVideoRenderPlan plan,
        string destinationPath,
        ExportPreset preset,
        IProgress<ExportProgress>? progress,
        CancellationToken cancellationToken);
}

public sealed class FfmpegTimelineVideoExporter : ITimelineVideoExporter
{
    private readonly IMediaProcessRunner _processRunner;
    private readonly IMediaProbe _probe;
    private readonly TimelineFilterGraphBuilder _graphBuilder;

    public FfmpegTimelineVideoExporter(IMediaProcessRunner processRunner, IMediaProbe probe, TimelineFilterGraphBuilder? graphBuilder = null)
    {
        _processRunner = processRunner;
        _probe = probe;
        _graphBuilder = graphBuilder ?? new TimelineFilterGraphBuilder();
    }

    public async Task<ExportReceipt> ExportAsync(
        TimelineVideoRenderPlan plan,
        string destinationPath,
        ExportPreset preset,
        IProgress<ExportProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(preset);
        if (preset.IncludeAudio)
        {
            throw new MediaExportException("Timeline audio export is not available until FT-505.");
        }

        if (preset.Width != plan.Width || preset.Height != plan.Height || preset.FrameRate != plan.FrameRate)
        {
            throw new MediaExportException("The timeline render plan and export preset must match.");
        }

        var outputPath = Path.GetFullPath(destinationPath);
        var outputDirectory = Path.GetDirectoryName(outputPath) ?? throw new ArgumentException("An output directory is required.", nameof(destinationPath));
        if (!Directory.Exists(outputDirectory))
        {
            throw new DirectoryNotFoundException(outputDirectory);
        }

        if (File.Exists(outputPath) && (File.GetAttributes(outputPath) & FileAttributes.ReparsePoint) != 0)
        {
            throw new MediaExportException("Export destinations that are symlinks are not accepted.");
        }

        var graph = _graphBuilder.Build(plan);
        var jobId = Guid.NewGuid();
        var temporaryPath = Path.Combine(outputDirectory, $".{Path.GetFileName(outputPath)}.{jobId:N}.tmp");
        try
        {
            progress?.Report(new ExportProgress("Encoding", 0));
            var arguments = new List<string>();
            foreach (var input in graph.Inputs)
            {
                arguments.AddRange(["-hide_banner", "-loglevel", "error", "-y", "-i", input.LocalPath]);
            }

            arguments.AddRange([
                "-filter_complex", graph.FilterComplex,
                "-map", $"[{graph.OutputLabel}]",
                "-an", "-c:v", "libx264", "-preset", "medium", "-pix_fmt", "yuv420p",
                "-colorspace", "bt709", "-color_primaries", "bt709", "-color_trc", "bt709", "-color_range", "tv",
                "-r", preset.FrameRate.ToString(), "-movflags", "+faststart", "-f", "mp4", temporaryPath
            ]);
            var result = await _processRunner.RunAsync("ffmpeg", arguments, cancellationToken);
            if (result.ExitCode != 0 || !File.Exists(temporaryPath))
            {
                throw new MediaExportException("FFmpeg could not complete the timeline export.");
            }

            progress?.Report(new ExportProgress("Verifying", 0.9));
            var verifiedMetadata = await _probe.ProbeAsync(new Uri(temporaryPath), cancellationToken);
            if (verifiedMetadata.Kind != MediaKind.Video ||
                verifiedMetadata.Width != plan.Width ||
                verifiedMetadata.Height != plan.Height ||
                !string.Equals(verifiedMetadata.ColorProfile, "bt709/bt709/bt709/tv", StringComparison.OrdinalIgnoreCase))
            {
                throw new MediaExportException($"The timeline export did not match the requested preset: {verifiedMetadata.Kind}, {verifiedMetadata.Width}x{verifiedMetadata.Height}, {verifiedMetadata.ColorProfile}.");
            }

            var outputBytes = new FileInfo(temporaryPath).Length;
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, outputPath, overwrite: true);
            progress?.Report(new ExportProgress("Completed", 1));
            return new ExportReceipt(jobId, outputPath, outputBytes, verifiedMetadata);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
