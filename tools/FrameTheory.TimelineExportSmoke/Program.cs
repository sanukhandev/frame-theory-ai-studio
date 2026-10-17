using FrameTheory.Captions;
using FrameTheory.Domain;
using FrameTheory.AI;
using FrameTheory.Desktop;
using FrameTheory.Media;
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
    ? new[] { new TimedCaption(1, TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.8), "caption smoke test words") }
    : Array.Empty<TimedCaption>();
var captionStyle = args.Length == 4
    ? new TimelineCaptionStyle(Path.GetFullPath(args[3]), maximumCharactersPerLine: 12)
    : null;
var plan = new TimelineVideoRenderPlan(
    frameRate,
    16,
    8,
    60,
    new Dictionary<MediaId, Uri> { [idA] = sourceA, [idB] = sourceB },
    [
        new TimelineVideoLayer("base", 0, [
            new TimelineVideoSegment(first, baseTransform),
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
    captionStyle
    );
var exporter = new FfmpegTimelineVideoExporter(runner, new FfprobeMediaProbe(runner));
var receipt = await exporter.ExportAsync(
    plan,
    destination,
    new ExportPreset(16, 8, frameRate),
    new Progress<ExportProgress>(update => Console.WriteLine($"stage={update.Stage}; fraction={update.Fraction:0.0}")),
    CancellationToken.None);
Console.WriteLine($"output={receipt.OutputPath}; bytes={receipt.OutputBytes}; kind={receipt.VerifiedMetadata.Kind}; size={receipt.VerifiedMetadata.Width}x{receipt.VerifiedMetadata.Height}; color={receipt.VerifiedMetadata.ColorProfile}");

var aiEditor = new TimelineEditorViewModel(frameRate);
aiEditor.AddMedia(importedSourceA.Metadata);
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
    [aiAction, reelAction],
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
    new Dictionary<MediaId, Uri> { [importedSourceA.Metadata.Id] = sourceA });
var aiReceipt = await exporter.ExportAsync(
    aiPlan,
    aiOutput,
    new ExportPreset(importedSourceA.Metadata.Width, importedSourceA.Metadata.Height, frameRate, importedSourceA.Metadata.HasAudio),
    new Progress<ExportProgress>(update => Console.WriteLine($"ai-stage={update.Stage}; fraction={update.Fraction:0.0}")),
    CancellationToken.None);
Console.WriteLine($"ai-output={aiReceipt.OutputPath}; bytes={aiReceipt.OutputBytes}; color={aiReceipt.VerifiedMetadata.ColorProfile}");
