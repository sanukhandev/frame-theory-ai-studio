using FrameTheory.Media;

if (args.Length != 1)
{
    throw new ArgumentException("Usage: FrameTheory.MediaImportSmoke <media-path>");
}

var path = Path.GetFullPath(args[0]);
var runner = new FfmpegProcessRunner();
var imported = await new FfmpegMediaImporter(
    new FfprobeMediaProbe(runner),
    new FfmpegFrameDecoder(runner),
    runner).ImportAsync(new Uri(path), CancellationToken.None);
Console.WriteLine($"kind={imported.Metadata.Kind}; thumbnail_bytes={imported.ThumbnailPpm?.Length ?? 0}; waveform_peaks={imported.WaveformPeaks.Count}; peak={imported.WaveformPeaks.DefaultIfEmpty().Max():0.000}");
