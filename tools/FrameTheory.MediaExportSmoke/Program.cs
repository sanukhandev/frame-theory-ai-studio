using FrameTheory.Domain;
using FrameTheory.Media;

if (args.Length != 2)
{
    throw new ArgumentException("Usage: FrameTheory.MediaExportSmoke <source-path> <destination-path>");
}

var sourcePath = Path.GetFullPath(args[0]);
var destinationPath = Path.GetFullPath(args[1]);
var runner = new FfmpegProcessRunner();
var exporter = new FfmpegMediaExporter(runner, new FfprobeMediaProbe(runner));
var receipt = await exporter.ExportAsync(
    new Uri(sourcePath),
    destinationPath,
    new ExportPreset(8, 4, new RationalFrameRate(30, 1)),
    new Progress<ExportProgress>(update => Console.WriteLine($"stage={update.Stage}; fraction={update.Fraction:0.0}")),
    CancellationToken.None);
Console.WriteLine($"output={receipt.OutputPath}; bytes={receipt.OutputBytes}; kind={receipt.VerifiedMetadata.Kind}; size={receipt.VerifiedMetadata.Width}x{receipt.VerifiedMetadata.Height}; audio={receipt.VerifiedMetadata.HasAudio}; color={receipt.VerifiedMetadata.ColorProfile}");
