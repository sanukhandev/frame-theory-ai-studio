using FrameTheory.Media;

if (args.Length != 1)
{
    throw new ArgumentException("Usage: FrameTheory.MediaProbeSmoke <media-path>");
}

var path = Path.GetFullPath(args[0]);
var metadata = await new FfprobeMediaProbe(new FfmpegProcessRunner())
    .ProbeAsync(new Uri(path), CancellationToken.None);
Console.WriteLine($"kind={metadata.Kind}; size={metadata.Width}x{metadata.Height}; fps={metadata.FrameRate}; duration={metadata.Duration}; frames={metadata.DurationFrames}; audio={metadata.HasAudio}; color={metadata.ColorProfile}; orientation={metadata.OrientationDegrees}");
