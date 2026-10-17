using FrameTheory.Captions;
using FrameTheory.Domain;
using FrameTheory.Media;

if (args.Length is < 3 or > 4)
{
    throw new ArgumentException("Usage: FrameTheory.TimelineExportSmoke <source-a> <source-b> <destination> [font-path]");
}

var sourceA = new Uri(Path.GetFullPath(args[0]));
var sourceB = new Uri(Path.GetFullPath(args[1]));
var destination = Path.GetFullPath(args[2]);
var frameRate = new RationalFrameRate(30, 1);
var idA = MediaId.Create(sourceA, "timeline-smoke-a");
var idB = MediaId.Create(sourceB, "timeline-smoke-b");
var first = TimelineClip.Video(idA, 0, 30, 0, 30, frameRate);
var second = TimelineClip.Video(idB, 0, 30, 30, 30, frameRate);
var overlay = TimelineClip.Video(idB, 0, 30, 15, 30, frameRate);
var audioA = TimelineClip.Audio(idA, 0, 48000, 0, 30, 48000);
var audioB = TimelineClip.Audio(idB, 0, 48000, 30, 30, 48000);
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
        ])],
    titles,
    captions,
    captionStyle
    );
var runner = new FfmpegProcessRunner();
var exporter = new FfmpegTimelineVideoExporter(runner, new FfprobeMediaProbe(runner));
var receipt = await exporter.ExportAsync(
    plan,
    destination,
    new ExportPreset(16, 8, frameRate),
    new Progress<ExportProgress>(update => Console.WriteLine($"stage={update.Stage}; fraction={update.Fraction:0.0}")),
    CancellationToken.None);
Console.WriteLine($"output={receipt.OutputPath}; bytes={receipt.OutputBytes}; kind={receipt.VerifiedMetadata.Kind}; size={receipt.VerifiedMetadata.Width}x{receipt.VerifiedMetadata.Height}; color={receipt.VerifiedMetadata.ColorProfile}");
