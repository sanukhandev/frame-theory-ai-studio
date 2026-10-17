using FrameTheory.Domain;
using FrameTheory.Media;

if (args.Length != 2 || !long.TryParse(args[1], out var frameIndex) || frameIndex < 0)
{
    throw new ArgumentException("Usage: FrameTheory.FrameDecodeSmoke <media-path> <frame-index>");
}

var path = Path.GetFullPath(args[0]);
var bytes = await new FfmpegFrameDecoder(new FfmpegProcessRunner())
    .DecodeFrameAsync(new Uri(path), new RationalFrameRate(30, 1), frameIndex, CancellationToken.None);
Console.WriteLine($"decoded_frame={frameIndex}; bytes={bytes.Length}; signature={System.Text.Encoding.ASCII.GetString(bytes, 0, Math.Min(2, bytes.Length))}");
