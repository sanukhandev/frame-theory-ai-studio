using FrameTheory.Captions;
using FrameTheory.Domain;
using FrameTheory.AI;
using FrameTheory.Application;
using FrameTheory.Desktop;
using FrameTheory.Media;
using System.Security.Cryptography;
using System.Text.Json;

if (args.Length is < 3 or > 4)
{
    throw new ArgumentException("Usage: FrameTheory.TimelineExportSmoke <source-a> <source-b> <destination> [font-path]");
}

var sourceA = new Uri(Path.GetFullPath(args[0]));
var sourceB = new Uri(Path.GetFullPath(args[1]));
var destination = Path.GetFullPath(args[2]);
var runner = new FfmpegProcessRunner();
var decoder = new FfmpegFrameDecoder(runner);
var importer = new FfmpegMediaImporter(new FfprobeMediaProbe(runner), decoder, runner);
var importedSourceA = await importer.ImportAsync(sourceA, CancellationToken.None);
var importedSourceB = await importer.ImportAsync(sourceB, CancellationToken.None);
var projectPath = Path.Combine(Path.GetDirectoryName(destination) ?? ".", "timeline-smoke.ftstudio");
var approvedWatermarkPath = Path.Combine(AppContext.BaseDirectory, "assets", "brand", "frame-theory-original.png");
var approvedWatermark = File.Exists(approvedWatermarkPath)
    ? new TimelineWatermark(approvedWatermarkPath, opacity: 0.85f, margin: 1)
    : null;
var projectViewModel = new MainWindowViewModel(
    new WorkspaceCatalog(),
    new SmokeCapabilityDetector(),
    importer,
    timelinePreviewRenderer: new FfmpegTimelinePreviewRenderer(runner));
await projectViewModel.ImportFilesAsync([sourceA.LocalPath, sourceB.LocalPath], CancellationToken.None);
if (!projectViewModel.TimelineEditor.SelectFirst(TimelineTrackKind.Video))
{
    throw new InvalidOperationException("The timeline smoke project did not select its first video clip.");
}

projectViewModel.TimelineEditor.SetApprovedWatermark();
projectViewModel.TimelineEditor.ApplyStabilizationToSelected(5);
if (!await projectViewModel.PreviewCurrentTimelineFrameAsync(CancellationToken.None) ||
    projectViewModel.TimelinePreviewPpm is not { Length: > 4 } previewPpm ||
    !previewPpm.AsSpan().StartsWith("P6\n"u8))
{
    throw new InvalidOperationException($"Timeline compositor preview failed: {projectViewModel.TimelinePreviewStatusText}");
}

Console.WriteLine($"timeline-preview=ok; bytes={projectViewModel.TimelinePreviewPpm.Length}; status={projectViewModel.TimelinePreviewStatusText}");
if (!await projectViewModel.SaveTimelineProjectAsync(projectPath, CancellationToken.None))
{
    throw new InvalidOperationException(projectViewModel.TimelineProjectStatusText);
}

var reopenedProjectViewModel = new MainWindowViewModel(new WorkspaceCatalog(), new SmokeCapabilityDetector(), importer);
if (!await reopenedProjectViewModel.OpenTimelineProjectAsync(projectPath, CancellationToken.None) ||
    reopenedProjectViewModel.ImportedMedia.Count != 2 ||
    reopenedProjectViewModel.TimelineEditor.TrackRows.All(row => row.Kind != TimelineTrackKind.Video) ||
    reopenedProjectViewModel.TimelineEditor.Watermark is null)
{
    throw new InvalidOperationException(reopenedProjectViewModel.TimelineProjectStatusText);
}

var reopenedStabilization = reopenedProjectViewModel.TimelineEditor.CaptureProjectState().VideoSettings
    .Select(settings => settings.Stabilization)
    .SingleOrDefault(settings => settings is not null);
if (reopenedStabilization?.SearchRadius != 5)
{
    throw new InvalidOperationException("Timeline project reopen did not preserve stabilization settings.");
}

Console.WriteLine($"project-roundtrip={projectPath}; media={reopenedProjectViewModel.ImportedMedia.Count}; stabilization={reopenedStabilization.SearchRadius}; ffmpeg-stabilization={new TimelineStabilizationSettings(reopenedStabilization.SearchRadius).FfmpegSearchRadius}; watermark={(reopenedProjectViewModel.TimelineEditor.Watermark is null ? "none" : "approved")}; status={reopenedProjectViewModel.TimelineProjectStatusText}");
var proxyDirectory = Path.Combine(Path.GetDirectoryName(destination) ?? ".", "proxy-cache");
var proxyService = new FfmpegPreviewProxyService(runner, proxyDirectory, maximumCacheBytes: 64L * 1024 * 1024);
var sourceFingerprint = $"sha256:{Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(sourceA.LocalPath))).ToLowerInvariant()}";
var proxy = await proxyService.CreateAsync(sourceA, sourceFingerprint, 8, 8, CancellationToken.None);
var proxyMetadata = await new FfprobeMediaProbe(runner).ProbeAsync(new Uri(proxy.OutputPath), CancellationToken.None);
if (proxyMetadata.Kind != MediaKind.Video || proxyMetadata.Width > 8 || proxyMetadata.Height > 8)
{
    throw new InvalidOperationException($"Preview proxy dimensions were invalid: {proxyMetadata.Kind} {proxyMetadata.Width}x{proxyMetadata.Height}.");
}

var reusedProxy = await proxyService.CreateAsync(sourceA, sourceFingerprint, 8, 8, CancellationToken.None);
if (!reusedProxy.Reused)
{
    throw new InvalidOperationException("Preview proxy was not reused on the second request.");
}

Console.WriteLine($"proxy={proxy.OutputPath}; bytes={proxy.OutputBytes}; reused={reusedProxy.Reused}; size={proxyMetadata.Width}x{proxyMetadata.Height}");
var analysis = await new LocalAnalysisSampler(decoder).PrepareAsync(
    new GeminiAnalysisRequest([importedSourceA], GeminiAnalysisScope.VideoFrames | GeminiAnalysisScope.AudioWaveform, uploadConsent: false),
    new Progress<LocalAnalysisProgress>(update => Console.WriteLine($"analysis={update.Stage}; fraction={update.Fraction:0.0}")),
    CancellationToken.None);
Console.WriteLine($"analysis-samples={analysis.Samples.Count}; bytes={analysis.EstimatedUploadBytes}; upload-consent={analysis.UploadConsent}");
var frameRate = new RationalFrameRate(30, 1);
var idA = MediaId.Create(sourceA, "timeline-smoke-a");
var idB = MediaId.Create(sourceB, "timeline-smoke-b");
var first = TimelineClip.Video(idA, 0, 30, 0, 30, frameRate);
var second = TimelineClip.Video(idB, 0, 30, 30, 30, frameRate);
var overlay = TimelineClip.Video(idB, 0, 30, 15, 30, frameRate);
var audioA = TimelineClip.Audio(idA, 0, 48000, 0, 30, 48000);
var audioB = TimelineClip.Audio(idB, 0, 48000, 30, 30, 48000);
var musicA = TimelineClip.Audio(idA, 0, 48000, 0, 30, 48000);
var musicB = TimelineClip.Audio(idB, 0, 48000, 30, 30, 48000);
var baseTransform = new TimelineVideoTransform(opacity: 0.9, scale: 1, offsetX: 0, offsetY: 0);
var overlayTransform = new TimelineVideoTransform(opacity: 0.35, scale: 0.5, offsetX: 2, offsetY: 1);
var speedRamp = new TimelineSpeedRamp([
    new TimelineSpeedPoint(0, 0.5),
    new TimelineSpeedPoint(15, 0.75),
    new TimelineSpeedPoint(30, 1)
]);
var titles = args.Length == 4
    ? new[]
    {
        new TimelineTitle(
            "FRAME THEORY",
            Path.GetFullPath(args[3]),
            5,
            45,
            placement: TimelineTitlePlacement.TopCenter,
            animation: TimelineTransformKeyframes.FromPreset(
                TimelineAnimationPreset.PushIn,
                5,
                50,
                new TimelineVideoTransform()))
    }
    : Array.Empty<TimelineTitle>();
var captions = args.Length == 4
    ? new[]
    {
        new TimedCaption(
            1,
            TimeSpan.FromSeconds(0.2),
            TimeSpan.FromSeconds(0.8),
            "caption smoke test words",
            CaptionProvenance.VerifiedTranscript,
            [
                new TimedCaptionWord("caption", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.35)),
                new TimedCaptionWord("smoke", TimeSpan.FromSeconds(0.4), TimeSpan.FromSeconds(0.55)),
                new TimedCaptionWord("test", TimeSpan.FromSeconds(0.6), TimeSpan.FromSeconds(0.7)),
                new TimedCaptionWord("words", TimeSpan.FromSeconds(0.7), TimeSpan.FromSeconds(0.8))
            ])
    }
    : Array.Empty<TimedCaption>();
var captionStyle = args.Length == 4
    ? new TimelineCaptionStyle(Path.GetFullPath(args[3]), maximumCharactersPerLine: 12, kineticWords: true)
    : null;
var plan = new TimelineVideoRenderPlan(
    frameRate,
    16,
    8,
    60,
    new Dictionary<MediaId, Uri> { [idA] = sourceA, [idB] = sourceB },
    [
        new TimelineVideoLayer("base", 0, [
            new TimelineVideoSegment(first, baseTransform, speedRamp: speedRamp),
            new TimelineVideoSegment(second, transition: TimelineTransitionKind.CrossDissolve, transitionDurationFrames: 5)
        ]),
        new TimelineVideoLayer("overlay", 1, [
            new TimelineVideoSegment(overlay, overlayTransform, animation: new TimelineTransformKeyframes([
                new TimelineTransformKeyframe(15, new TimelineVideoTransform(opacity: 0, scale: 0.4, offsetX: 0, offsetY: 1), TimelineEasing.EaseIn),
                new TimelineTransformKeyframe(44, overlayTransform)
            ]))
        ])
    ],
    [new TimelineAudioLayer("audio", 0, [
            new TimelineAudioSegment(audioA, fadeInFrames: 2, fadeOutFrames: 3),
            new TimelineAudioSegment(audioB, gainDb: -3, fadeInFrames: 2, fadeOutFrames: 3)
        ]),
        new TimelineAudioLayer("music", 1, [
            new TimelineAudioSegment(musicA, gainDb: -6, duckingDb: 12),
            new TimelineAudioSegment(musicB, gainDb: -6, duckingDb: 12)
        ])],
    titles,
    captions,
    captionStyle,
    watermark: approvedWatermark
    );
var exporter = new FfmpegTimelineVideoExporter(runner, new FfprobeMediaProbe(runner));
var receipt = await exporter.ExportAsync(
    plan,
    destination,
    new ExportPreset(16, 8, frameRate),
    new Progress<ExportProgress>(update => Console.WriteLine($"stage={update.Stage}; fraction={update.Fraction:0.0}")),
    CancellationToken.None);
Console.WriteLine($"output={receipt.OutputPath}; bytes={receipt.OutputBytes}; kind={receipt.VerifiedMetadata.Kind}; size={receipt.VerifiedMetadata.Width}x{receipt.VerifiedMetadata.Height}; color={receipt.VerifiedMetadata.ColorProfile}");
Console.WriteLine($"watermark={(approvedWatermark is null ? "none" : "approved")}");

var queuedOutput = Path.Combine(Path.GetDirectoryName(destination) ?? ".", $"{Path.GetFileNameWithoutExtension(destination)}-queue-retry.mp4");
await using var exportQueue = new ExportQueueService();
var retryAttempts = 0;
var failedJob = exportQueue.Enqueue(async queueCancellation =>
{
    if (Interlocked.Increment(ref retryAttempts) == 1)
    {
        throw new MediaExportException("Synthetic transient queue failure.");
    }

    return await exporter.ExportAsync(
        plan,
        queuedOutput,
        new ExportPreset(16, 8, frameRate),
        new Progress<ExportProgress>(update => Console.WriteLine($"retry-stage={update.Stage}; fraction={update.Fraction:0.0}")),
        queueCancellation);
});
try
{
    await failedJob.Completion;
    throw new InvalidOperationException("The synthetic queue failure unexpectedly succeeded.");
}
catch (MediaExportException)
{
}

var retriedJob = exportQueue.Retry(failedJob);
var retriedReceipt = await retriedJob.Completion;
Console.WriteLine($"queue-retry-output={retriedReceipt.OutputPath}; bytes={retriedReceipt.OutputBytes}; attempts={retryAttempts}; history={exportQueue.History.Count}");

var aiEditor = new TimelineEditorViewModel(frameRate);
aiEditor.AddMedia(importedSourceA.Metadata);
aiEditor.AddMedia(importedSourceB.Metadata);
aiEditor.SelectFirst(TimelineTrackKind.Video);
aiEditor.ApplyStabilizationToSelected(5);
var aiAction = new AiEditAction(
    "ai-grade",
    "color.adjust",
    importedSourceA.Metadata.Id.Value,
    0,
    Math.Max(1, importedSourceA.Metadata.DurationFrames),
    new Dictionary<string, JsonElement>
    {
        ["exposureEV"] = JsonDocument.Parse("0.5").RootElement.Clone(),
        ["contrast"] = JsonDocument.Parse("0.1").RootElement.Clone(),
        ["saturation"] = JsonDocument.Parse("0").RootElement.Clone(),
        ["temperature"] = JsonDocument.Parse("0").RootElement.Clone(),
        ["tint"] = JsonDocument.Parse("0").RootElement.Clone()
    });
var aiSecondGradeAction = new AiEditAction(
    "ai-grade-second",
    "color.adjust",
    importedSourceB.Metadata.Id.Value,
    0,
    Math.Max(1, importedSourceB.Metadata.DurationFrames),
    new Dictionary<string, JsonElement>
    {
        ["exposureEV"] = JsonDocument.Parse("-0.2").RootElement.Clone(),
        ["contrast"] = JsonDocument.Parse("0.05").RootElement.Clone(),
        ["saturation"] = JsonDocument.Parse("0").RootElement.Clone(),
        ["temperature"] = JsonDocument.Parse("0").RootElement.Clone(),
        ["tint"] = JsonDocument.Parse("0").RootElement.Clone()
    });
var aiTransitionAction = new AiEditAction(
    "ai-transition",
    "transition.add",
    importedSourceB.Metadata.Id.Value,
    importedSourceA.Metadata.DurationFrames,
    Math.Min(5, Math.Max(1, importedSourceB.Metadata.DurationFrames - 1)),
    new Dictionary<string, JsonElement>
    {
        ["kind"] = JsonDocument.Parse("\"cross-dissolve\"").RootElement.Clone()
    });
var reelAction = new AiEditAction(
    "ai-reel",
    "reel.before_after",
    "active-project",
    0,
    Math.Max(1, importedSourceA.Metadata.DurationFrames),
    new Dictionary<string, JsonElement>
    {
        ["style"] = JsonDocument.Parse("\"split\"").RootElement.Clone()
    });
var aiResult = AiEditPlanExecutor.Execute(
    [aiAction, aiSecondGradeAction, aiTransitionAction],
    aiEditor,
    new Dictionary<string, string>(),
    new Dictionary<string, FrameTheory.Color.ColorGrade>());
if (!aiResult.IsApplied)
{
    throw new InvalidOperationException(aiResult.Message);
}

var aiOutput = Path.Combine(Path.GetDirectoryName(destination) ?? ".", $"{Path.GetFileNameWithoutExtension(destination)}-ai.mp4");
var aiPlan = aiEditor.BuildRenderPlan(
    importedSourceA.Metadata.Width,
    importedSourceA.Metadata.Height,
    new Dictionary<MediaId, Uri>
    {
        [importedSourceA.Metadata.Id] = sourceA,
        [importedSourceB.Metadata.Id] = sourceB
    });
if (aiPlan.Layers.SelectMany(layer => layer.Segments).All(segment => segment.Stabilization?.SearchRadius != 5))
{
    throw new InvalidOperationException("Timeline render plan did not carry stabilization into native export.");
}
var aiReceipt = await exporter.ExportAsync(
    aiPlan,
    aiOutput,
    new ExportPreset(importedSourceA.Metadata.Width, importedSourceA.Metadata.Height, frameRate, importedSourceA.Metadata.HasAudio),
    new Progress<ExportProgress>(update => Console.WriteLine($"ai-stage={update.Stage}; fraction={update.Fraction:0.0}")),
    CancellationToken.None);
Console.WriteLine($"ai-output={aiReceipt.OutputPath}; bytes={aiReceipt.OutputBytes}; stabilization=5; ffmpeg-stabilization=16; color={aiReceipt.VerifiedMetadata.ColorProfile}");

var batchTargets = Enum.GetValues<SocialAspectRatio>().Select(aspectRatio =>
{
    var socialPreset = SocialVideoExportPreset.Create(aspectRatio, 16);
    var batchPlan = aiEditor.BuildRenderPlan(
        socialPreset.Width,
        socialPreset.Height,
        new Dictionary<MediaId, Uri>
        {
            [importedSourceA.Metadata.Id] = sourceA,
            [importedSourceB.Metadata.Id] = sourceB
        });
    var name = aspectRatio switch
    {
        SocialAspectRatio.Landscape16By9 => "16x9-landscape",
        SocialAspectRatio.Square1By1 => "1x1-square",
        SocialAspectRatio.Portrait9By16 => "9x16-portrait",
        _ => throw new ArgumentOutOfRangeException(nameof(aspectRatio))
    };
    return new TimelineBatchExportTarget(
        name,
        batchPlan,
        Path.Combine(Path.GetDirectoryName(destination) ?? ".", $"{Path.GetFileNameWithoutExtension(destination)}-{name}.mp4"),
        socialPreset.ToExportPreset(batchPlan.FrameRate, batchPlan.AudioLayers.Count > 0));
}).ToArray();
var batchReceipts = await new TimelineBatchExporter(exporter).ExportAsync(
    batchTargets,
    new Progress<TimelineBatchExportProgress>(update => Console.WriteLine($"batch-{update.TargetName}={update.Progress.Stage}; fraction={update.Progress.Fraction:0.0}")),
    CancellationToken.None);
if (batchReceipts.Count != 3 || batchReceipts.Any(receipt => receipt.VerifiedMetadata.Kind != MediaKind.Video))
{
    throw new InvalidOperationException("The multi-target batch export did not produce three verified videos.");
}

Console.WriteLine($"batch-output={batchReceipts.Count}; targets={string.Join(',', batchReceipts.Select(receipt => Path.GetFileName(receipt.OutputPath)))}");

var roughCutEditor = new TimelineEditorViewModel(frameRate);
roughCutEditor.AddMedia(importedSourceA.Metadata);
roughCutEditor.AddMedia(importedSourceB.Metadata);
var roughCutPlan = RoughCutPlanner.Create([
    new RoughCutSource(importedSourceA.Metadata.Id.Value, 0, importedSourceA.Metadata.DurationFrames, importedSourceA.WaveformPeaks),
    new RoughCutSource(importedSourceB.Metadata.Id.Value, importedSourceA.Metadata.DurationFrames, importedSourceB.Metadata.DurationFrames, importedSourceB.WaveformPeaks)
]);
roughCutEditor.ApplyRoughCut(roughCutPlan);
var roughCutState = TimelineProjectStateJson.Serialize(roughCutEditor.CaptureProjectState());
var reopenedRoughCutEditor = new TimelineEditorViewModel(frameRate);
reopenedRoughCutEditor.RestoreProjectState(TimelineProjectStateJson.Deserialize(roughCutState));
var restoredRoughCutIds = reopenedRoughCutEditor.TrackRows
    .Single(row => row.Kind == TimelineTrackKind.Video)
    .Clips
    .Select(clip => clip.MediaId)
    .ToArray();
if (!restoredRoughCutIds.SequenceEqual(roughCutPlan.Segments.Select(segment => segment.MediaId), StringComparer.Ordinal))
{
    throw new InvalidOperationException("Rough-cut project reopen did not preserve the planned clip order.");
}

var roughCutOutput = Path.Combine(Path.GetDirectoryName(destination) ?? ".", $"{Path.GetFileNameWithoutExtension(destination)}-rough-cut.mp4");
var roughCutReceipt = await exporter.ExportAsync(
    reopenedRoughCutEditor.BuildRenderPlan(
        importedSourceA.Metadata.Width,
        importedSourceA.Metadata.Height,
        new Dictionary<MediaId, Uri>
        {
            [importedSourceA.Metadata.Id] = sourceA,
            [importedSourceB.Metadata.Id] = sourceB
        }),
    roughCutOutput,
    new ExportPreset(importedSourceA.Metadata.Width, importedSourceA.Metadata.Height, frameRate, importedSourceA.Metadata.HasAudio),
    new Progress<ExportProgress>(update => Console.WriteLine($"rough-cut-stage={update.Stage}; fraction={update.Fraction:0.0}")),
    CancellationToken.None);
Console.WriteLine($"rough-cut-output={roughCutReceipt.OutputPath}; bytes={roughCutReceipt.OutputBytes}; color={roughCutReceipt.VerifiedMetadata.ColorProfile}");

var reelEditor = new TimelineEditorViewModel(frameRate);
reelEditor.AddMedia(importedSourceA.Metadata);
reelEditor.AddMedia(importedSourceB.Metadata);
var reelResult = AiEditPlanExecutor.Execute(
    [reelAction],
    reelEditor,
    new Dictionary<string, string>(),
    new Dictionary<string, FrameTheory.Color.ColorGrade>());
if (!reelResult.IsApplied)
{
    throw new InvalidOperationException(reelResult.Message);
}

var reelOutput = Path.Combine(Path.GetDirectoryName(destination) ?? ".", $"{Path.GetFileNameWithoutExtension(destination)}-ai-reel.mp4");
var reelPlan = reelEditor.BuildRenderPlan(
    importedSourceA.Metadata.Width,
    importedSourceA.Metadata.Height,
    new Dictionary<MediaId, Uri>
    {
        [importedSourceA.Metadata.Id] = sourceA,
        [importedSourceB.Metadata.Id] = sourceB
    });
var reelReceipt = await exporter.ExportAsync(
    reelPlan,
    reelOutput,
    new ExportPreset(importedSourceA.Metadata.Width, importedSourceA.Metadata.Height, frameRate, importedSourceA.Metadata.HasAudio),
    new Progress<ExportProgress>(update => Console.WriteLine($"reel-stage={update.Stage}; fraction={update.Fraction:0.0}")),
    CancellationToken.None);
Console.WriteLine($"reel-output={reelReceipt.OutputPath}; bytes={reelReceipt.OutputBytes}; color={reelReceipt.VerifiedMetadata.ColorProfile}");

file sealed class SmokeCapabilityDetector : ICapabilityDetector
{
    public Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken) => Task.FromResult(
        new CapabilityReport("Smoke", "x64", [new("Storage", CapabilityState.Available, "ready")]));
}
