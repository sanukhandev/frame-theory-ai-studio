using FrameTheory.Domain;
using FrameTheory.Media;

if (args.Length != 3)
{
    throw new ArgumentException("Usage: FrameTheory.TimelineExportSmoke <source-a> <source-b> <destination>");
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
var plan = new TimelineVideoRenderPlan(
    frameRate,
    16,
    8,
    60,
    new Dictionary<MediaId, Uri> { [idA] = sourceA, [idB] = sourceB },
    [
        new TimelineVideoLayer("base", 0, [
            new TimelineVideoSegment(first),
            new TimelineVideoSegment(second, transition: TimelineTransitionKind.CrossDissolve, transitionDurationFrames: 5)
        ]),
        new TimelineVideoLayer("overlay", 1, [
            new TimelineVideoSegment(overlay, new TimelineVideoTransform(opacity: 0.35, scale: 0.5, offsetX: 2, offsetY: 1))
        ])
    ]);
var runner = new FfmpegProcessRunner();
var exporter = new FfmpegTimelineVideoExporter(runner, new FfprobeMediaProbe(runner));
var receipt = await exporter.ExportAsync(
    plan,
    destination,
    new ExportPreset(16, 8, frameRate, includeAudio: false),
    new Progress<ExportProgress>(update => Console.WriteLine($"stage={update.Stage}; fraction={update.Fraction:0.0}")),
    CancellationToken.None);
Console.WriteLine($"output={receipt.OutputPath}; bytes={receipt.OutputBytes}; kind={receipt.VerifiedMetadata.Kind}; size={receipt.VerifiedMetadata.Width}x{receipt.VerifiedMetadata.Height}; color={receipt.VerifiedMetadata.ColorProfile}");
