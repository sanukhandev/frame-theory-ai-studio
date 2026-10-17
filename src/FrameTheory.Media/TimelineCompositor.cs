using System.Globalization;
using FrameTheory.Captions;
using FrameTheory.Color;
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
        IReadOnlyList<TimelineVideoLayer> layers,
        IReadOnlyList<TimelineAudioLayer>? audioLayers = null,
        IReadOnlyList<TimelineTitle>? titles = null,
        IReadOnlyList<TimedCaption>? captions = null,
        TimelineCaptionStyle? captionStyle = null,
        IReadOnlyDictionary<MediaId, ColorGrade>? colorGrades = null)
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
        AudioLayers = (audioLayers ?? []).OrderBy(layer => layer.Order).ToArray();
        Titles = (titles ?? []).OrderBy(title => title.Order).ToArray();
        Captions = (captions ?? []).OrderBy(caption => caption.Start).ToArray();
        CaptionStyle = captionStyle;
        ColorGrades = colorGrades is null
            ? new Dictionary<MediaId, ColorGrade>()
            : new Dictionary<MediaId, ColorGrade>(colorGrades);
    }

    public RationalFrameRate FrameRate { get; }

    public int Width { get; }

    public int Height { get; }

    public long DurationFrames { get; }

    public IReadOnlyDictionary<MediaId, Uri> Sources { get; }

    public IReadOnlyList<TimelineVideoLayer> Layers { get; }

    public IReadOnlyList<TimelineAudioLayer> AudioLayers { get; }

    public IReadOnlyList<TimelineTitle> Titles { get; }

    public IReadOnlyList<TimedCaption> Captions { get; }

    public TimelineCaptionStyle? CaptionStyle { get; }

    public IReadOnlyDictionary<MediaId, ColorGrade> ColorGrades { get; }
}

public sealed record TimelineFilterGraph(string FilterComplex, IReadOnlyList<Uri> Inputs, string OutputLabel, string? AudioOutputLabel);

public sealed class TimelineFilterGraphBuilder
{
    public TimelineFilterGraph Build(TimelineVideoRenderPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var usedMediaIds = plan.Layers.SelectMany(layer => layer.Segments).Select(segment => segment.Clip.MediaId)
            .Concat(plan.AudioLayers.SelectMany(layer => layer.Segments).Select(segment => segment.Clip.MediaId))
            .Distinct()
            .ToArray();
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

        var titleIndex = 0;
        foreach (var title in plan.Titles)
        {
            var fontPath = GetFontPath(title);
            var nextBase = $"title{titleIndex++}";
            var titleEndFrame = checked(title.TimelineRange.StartFrame + title.TimelineRange.Length);
            var animation = title.Animation;
            var scale = animation is null ? title.FontSize.ToString(CultureInfo.InvariantCulture) : $"{title.FontSize}*({animation.ToFfmpegExpression(value => value.Scale, 0)})";
            var offsetX = animation?.ToFfmpegExpression(value => value.OffsetX, 0);
            var offsetY = animation?.ToFfmpegExpression(value => value.OffsetY, 0);
            var alpha = animation?.ToFfmpegExpression(value => value.Opacity, 0) ?? "1";
            var x = TitleX(title.Placement, offsetX);
            var y = TitleY(title.Placement, offsetY);
            filters.Add($"[{currentBase}]drawtext=fontfile='{EscapeDrawText(fontPath)}':text='{EscapeDrawText(title.Text)}':fontsize={EscapeFilterExpression(scale)}:fontcolor=0x{title.ColorRgb:X6}:alpha={EscapeFilterExpression(alpha)}:x={EscapeFilterExpression(x)}:y={EscapeFilterExpression(y)}:enable='between(t,{FormatSeconds(title.TimelineRange.StartFrame, plan.FrameRate)},{FormatSeconds(titleEndFrame, plan.FrameRate)})'[{nextBase}]");
            currentBase = nextBase;
        }

        if (plan.Captions.Count > 0)
        {
            if (plan.CaptionStyle is null)
            {
                throw new MediaExportException("Caption rendering requires a validated caption font style.");
            }

            var captionFontPath = GetFontPath(plan.CaptionStyle.FontPath);
            for (var index = 0; index < plan.Captions.Count; index++)
            {
                var caption = plan.Captions[index];
                var nextBase = $"caption{index}";
                var end = caption.End.TotalSeconds.ToString("0.######", CultureInfo.InvariantCulture);
                var start = caption.Start.TotalSeconds.ToString("0.######", CultureInfo.InvariantCulture);
                var text = EscapeDrawText(WrapCaptionText(caption.Text, plan.CaptionStyle.MaximumCharactersPerLine));
                filters.Add($"[{currentBase}]drawtext=fontfile='{EscapeDrawText(captionFontPath)}':text='{text}':fontsize={plan.CaptionStyle.FontSize}:fontcolor=0x{plan.CaptionStyle.ColorRgb:X6}:x=(w-text_w)/2:y=h-text_h-{plan.CaptionStyle.MarginBottom}:enable='between(t,{start},{end})'[{nextBase}]");
                currentBase = nextBase;
            }
        }

        filters.Add($"[{currentBase}]format=yuv420p,setparams=color_primaries=bt709:color_trc=bt709:colorspace=bt709:range=tv[outv]");
        var audioOutput = BuildAudio(plan, inputIndexes, filters);
        return new TimelineFilterGraph(string.Join(';', filters), inputs, "outv", audioOutput);
    }

    private static string? BuildAudio(
        TimelineVideoRenderPlan plan,
        IReadOnlyDictionary<MediaId, int> inputIndexes,
        ICollection<string> filters)
    {
        var segments = plan.AudioLayers.SelectMany(layer => layer.Segments).ToArray();
        if (segments.Length == 0)
        {
            return null;
        }

        var labels = new List<string> { "silence" };
        var duration = FormatSeconds(plan.DurationFrames, plan.FrameRate);
        filters.Add($"anullsrc=r=48000:cl=stereo:d={duration}[silence]");
        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            var clip = segment.Clip;
            var input = inputIndexes[clip.MediaId];
            var sourceEnd = checked(clip.SourceStartUnit + clip.SourceUnitCount);
            var label = $"audio{index}";
            var gain = segment.Muted ? "-60" : Format(segment.GainDb);
            var filter = $"[{input}:a:0]atrim=start_sample={clip.SourceStartUnit}:end_sample={sourceEnd},asetpts=PTS-STARTPTS,aresample=48000,aformat=sample_rates=48000:channel_layouts=stereo,volume={gain}dB";
            if (segment.FadeInFrames > 0)
            {
                filter += $",afade=t=in:st=0:d={FormatSeconds(segment.FadeInFrames, plan.FrameRate)}";
            }

            if (segment.FadeOutFrames > 0)
            {
                var start = clip.TimelineRange.Length - segment.FadeOutFrames;
                filter += $",afade=t=out:st={FormatSeconds(start, plan.FrameRate)}:d={FormatSeconds(segment.FadeOutFrames, plan.FrameRate)}";
            }

            if (segment.DuckingDb > 0)
            {
                for (var otherIndex = 0; otherIndex < segments.Length; otherIndex++)
                {
                    if (otherIndex == index)
                    {
                        continue;
                    }

                    var otherRange = segments[otherIndex].Clip.TimelineRange;
                    var overlapStart = Math.Max(segment.Clip.TimelineRange.StartFrame, otherRange.StartFrame);
                    var overlapEnd = Math.Min(segment.Clip.TimelineRange.EndFrameExclusive, otherRange.EndFrameExclusive);
                    if (overlapEnd <= overlapStart)
                    {
                        continue;
                    }

                    var relativeStart = overlapStart - segment.Clip.TimelineRange.StartFrame;
                    var relativeEnd = overlapEnd - segment.Clip.TimelineRange.StartFrame;
                    filter += $",volume={Format(-segment.DuckingDb)}dB:enable='between(t,{FormatSeconds(relativeStart, plan.FrameRate)},{FormatSeconds(relativeEnd, plan.FrameRate)})'";
                }
            }

            filter += $",asetpts=PTS-STARTPTS+{FormatSeconds(clip.TimelineRange.StartFrame, plan.FrameRate)}/TB[{label}]";
            filters.Add(filter);
            labels.Add(label);
        }

        var joined = string.Join("][", labels);
        filters.Add($"[{joined}]amix=inputs={labels.Count}:duration=first:dropout_transition=0:normalize=0[aout]");
        return "aout";
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
        if (layer.Segments.Any(segment => segment.Animation is not null))
        {
            if (layer.Segments.Any(segment => segment.Transition is not null))
            {
                throw new MediaExportException("Animated visual segments cannot currently be combined with transitions on the same layer.");
            }

            return BuildAnimatedLayer(plan, layer, inputIndexes, filters, layerIndex);
        }

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

    private static string BuildAnimatedLayer(
        TimelineVideoRenderPlan plan,
        TimelineVideoLayer layer,
        IReadOnlyDictionary<MediaId, int> inputIndexes,
        ICollection<string> filters,
        int layerIndex)
    {
        var current = $"layer{layerIndex}animated0";
        filters.Add($"color=c=black@0.0:s={plan.Width}x{plan.Height}:r={Rate(plan.FrameRate)}:d={FormatSeconds(plan.DurationFrames, plan.FrameRate)},format=yuva420p[{current}]");
        for (var index = 0; index < layer.Segments.Count; index++)
        {
            var segment = layer.Segments[index];
            var segmentLabel = $"layer{layerIndex}animatedSegment{index}";
            BuildSegment(plan, segment, inputIndexes, filters, segmentLabel, includeTimelineOffset: true, padToCanvas: false);
            var offsetX = segment.Animation?.ToFfmpegExpression(value => value.OffsetX, 0) ?? Format(segment.Transform.OffsetX);
            var offsetY = segment.Animation?.ToFfmpegExpression(value => value.OffsetY, 0) ?? Format(segment.Transform.OffsetY);
            var next = $"layer{layerIndex}animated{index + 1}";
            filters.Add($"[{current}][{segmentLabel}]overlay=x={EscapeFilterExpression(offsetX)}:y={EscapeFilterExpression(offsetY)}:eof_action=pass:shortest=0:format=auto:eval=frame[{next}]");
            current = next;
        }

        return current;
    }

    private static void BuildSegment(
        TimelineVideoRenderPlan plan,
        TimelineVideoSegment segment,
        IReadOnlyDictionary<MediaId, int> inputIndexes,
        ICollection<string> filters,
        string label,
        bool includeTimelineOffset,
        bool padToCanvas = true)
    {
        var transform = segment.Transform;
        var clip = segment.Clip;
        var input = inputIndexes[clip.MediaId];
        var sourceEnd = checked(clip.SourceStartUnit + clip.SourceUnitCount);
        var scale = segment.Animation?.ToFfmpegExpression(value => value.Scale, clip.TimelineRange.StartFrame) ?? Format(transform.Scale);
        var opacity = segment.Animation?.ToFfmpegExpression(value => value.Opacity, clip.TimelineRange.StartFrame, "N") ?? Format(transform.Opacity);
        scale = EscapeFilterExpression(scale);
        opacity = EscapeFilterExpression(opacity);
        var alpha = segment.Animation is null
            ? $"colorchannelmixer=aa={opacity}"
            : $"geq=r='r(X\\,Y)':g='g(X\\,Y)':b='b(X\\,Y)':a='alpha(X\\,Y)*({opacity})'";
        var filter = $"[{input}:v:0]trim=start_frame={clip.SourceStartUnit}:end_frame={sourceEnd},setpts=PTS-STARTPTS,fps={Rate(plan.FrameRate)},scale=trunc(iw*{scale}/2)*2:trunc(ih*{scale}/2)*2:eval=frame,format=yuva420p";
        if (plan.ColorGrades.TryGetValue(clip.MediaId, out var grade))
        {
            filter += $",eq=brightness={Format((Math.Pow(2, grade.ExposureEV) - 1) * 0.5)}:contrast={Format(1 + grade.Contrast)}:saturation={Format(1 + grade.Saturation)}";
            filter += $",colorbalance=rs={Format(grade.Temperature * 0.1)}:bs={Format(-grade.Temperature * 0.1)}:gs={Format(-grade.Tint * 0.1)}";
        }

        if (padToCanvas)
        {
            filter += $",pad={plan.Width}:{plan.Height}:{transform.OffsetX}:{transform.OffsetY}:color=black@0:eval=frame";
        }

        filter += $",{alpha}";
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

    private static string GetFontPath(TimelineTitle title) => GetFontPath(title.FontPath);

    private static string GetFontPath(string fontPath)
    {
        var path = Path.GetFullPath(fontPath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The selected title font was not found.", path);
        }

        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new MediaExportException("Symlinked title fonts are not accepted.");
        }

        return path;
    }

    private static string WrapCaptionText(string text, int maximumCharactersPerLine)
    {
        var lines = new List<string>();
        foreach (var paragraph in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = string.Empty;
            foreach (var word in words)
            {
                if (line.Length > 0 && line.Length + word.Length + 1 > maximumCharactersPerLine)
                {
                    lines.Add(line);
                    line = string.Empty;
                }

                line = line.Length == 0 ? word : $"{line} {word}";
            }

            if (line.Length > 0 || words.Length == 0)
            {
                lines.Add(line);
            }
        }

        return string.Join("\\n", lines);
    }

    private static string TitleX(TimelineTitlePlacement placement, string? offset) => AddOffset(placement switch
    {
        TimelineTitlePlacement.TopLeft => "w*0.1",
        TimelineTitlePlacement.TopCenter or TimelineTitlePlacement.Center or TimelineTitlePlacement.BottomCenter => "(w-text_w)/2",
        _ => throw new ArgumentOutOfRangeException(nameof(placement))
    }, offset);

    private static string TitleY(TimelineTitlePlacement placement, string? offset) => AddOffset(placement switch
    {
        TimelineTitlePlacement.TopLeft or TimelineTitlePlacement.TopCenter => "h*0.1",
        TimelineTitlePlacement.Center => "(h-text_h)/2",
        TimelineTitlePlacement.BottomCenter => "h*0.8",
        _ => throw new ArgumentOutOfRangeException(nameof(placement))
    }, offset);

    private static string AddOffset(string position, string? offset) => offset is null ? position : $"({position})+({offset})";

    private static string EscapeDrawText(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace(":", "\\:", StringComparison.Ordinal)
        .Replace(",", "\\,", StringComparison.Ordinal)
        .Replace("'", "\\'", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal);

    private static string Rate(RationalFrameRate frameRate) => $"{frameRate.Numerator}/{frameRate.Denominator}";

    private static string FormatSeconds(long frames, RationalFrameRate frameRate) =>
        Format((double)frames * frameRate.Denominator / frameRate.Numerator);

    private static string Format(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string EscapeFilterExpression(string expression) => expression.Replace(",", "\\,", StringComparison.Ordinal);
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
                "-c:v", "libx264", "-preset", "medium", "-pix_fmt", "yuv420p",
                "-colorspace", "bt709", "-color_primaries", "bt709", "-color_trc", "bt709", "-color_range", "tv",
                "-r", preset.FrameRate.ToString(), "-movflags", "+faststart", "-f", "mp4", temporaryPath
            ]);
            if (preset.IncludeAudio)
            {
                if (graph.AudioOutputLabel is null)
                {
                    throw new MediaExportException("No timeline audio clips are available for audio export.");
                }

                var mapIndex = arguments.IndexOf("-map");
                arguments.InsertRange(mapIndex + 2, ["-map", $"[{graph.AudioOutputLabel}]", "-c:a", "aac", "-b:a", "192k"]);
            }
            else
            {
                arguments.Insert(arguments.IndexOf("-c:v"), "-an");
            }
            var result = await _processRunner.RunAsync("ffmpeg", arguments, cancellationToken);
            if (result.ExitCode != 0 || !File.Exists(temporaryPath))
            {
                throw new MediaExportException($"FFmpeg could not complete the timeline export: {result.StandardError.Trim()}");
            }

            progress?.Report(new ExportProgress("Verifying", 0.9));
            var verifiedMetadata = await _probe.ProbeAsync(new Uri(temporaryPath), cancellationToken);
            if (verifiedMetadata.Kind != MediaKind.Video ||
                verifiedMetadata.Width != plan.Width ||
                verifiedMetadata.Height != plan.Height ||
                (preset.IncludeAudio && !verifiedMetadata.HasAudio) ||
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
