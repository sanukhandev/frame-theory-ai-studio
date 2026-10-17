using FrameTheory.Media;
using FrameTheory.Photo;

if (args.Length is < 2 or > 4)
{
    throw new ArgumentException("Usage: FrameTheory.PhotoExportSmoke <image-path> <destination-path> [png|jpeg] [16:9|1:1|9:16]");
}

var inputPath = Path.GetFullPath(args[0]);
var destinationPath = Path.GetFullPath(args[1]);
var format = args.Length >= 3 && args[2].Equals("jpeg", StringComparison.OrdinalIgnoreCase)
    ? PhotoExportFormat.Jpeg
    : PhotoExportFormat.Png;
var preset = args.Length == 4
    ? args[3] switch
    {
        "16:9" => SocialPhotoExportPreset.Create(SocialAspectRatio.Landscape16By9),
        "1:1" => SocialPhotoExportPreset.Create(SocialAspectRatio.Square1By1),
        "9:16" => SocialPhotoExportPreset.Create(SocialAspectRatio.Portrait9By16),
        _ => throw new ArgumentException("The photo social preset must be 16:9, 1:1 or 9:16.", nameof(args))
    }
    : null;
var runner = new FfmpegProcessRunner();
var document = await new PhotoImporter(new FfprobeMediaProbe(runner), new FfmpegFrameDecoder(runner))
    .ImportAsync(new Uri(inputPath), CancellationToken.None);
var state = new PhotoEditState(document).WithLayers(
    new PhotoLayerStack().Add(new PhotoAdjustmentLayer("Exposure", new FrameTheory.Color.ColorGrade(exposureEV: 0.25f))));
var receipt = await new PhotoExporter(runner, new FfprobeMediaProbe(runner)).ExportAsync(
    state,
    destinationPath,
    new PhotoExportOptions(format, 90, preset),
    new Progress<ExportProgress>(progress => Console.WriteLine($"{progress.Stage} {progress.Fraction:P0}")),
    CancellationToken.None);
Console.WriteLine($"format={format}; size={receipt.VerifiedMetadata.Width}x{receipt.VerifiedMetadata.Height}; color={receipt.VerifiedMetadata.ColorProfile}; bytes={receipt.OutputBytes}; path={receipt.OutputPath}");
