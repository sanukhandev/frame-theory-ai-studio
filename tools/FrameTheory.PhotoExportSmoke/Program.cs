using FrameTheory.Media;
using FrameTheory.Photo;
using FrameTheory.Desktop;
using FrameTheory.Storage;

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
var probe = new FfprobeMediaProbe(runner);
var decoder = new FfmpegFrameDecoder(runner);
var importer = new PhotoImporter(probe, decoder);
var document = await importer
    .ImportAsync(new Uri(inputPath), CancellationToken.None);
var state = new PhotoEditState(document).WithLayers(
    new PhotoLayerStack().Add(new PhotoAdjustmentLayer("Exposure", new FrameTheory.Color.ColorGrade(exposureEV: 0.25f))));
var receipt = await new PhotoExporter(runner, new FfprobeMediaProbe(runner)).ExportAsync(
    state,
    destinationPath,
    new PhotoExportOptions(format, 90, preset),
    new Progress<ExportProgress>(progress => Console.WriteLine($"{progress.Stage} {progress.Fraction:P0}")),
    CancellationToken.None);
var projectPath = Path.Combine(Path.GetDirectoryName(destinationPath) ?? ".", "photo-project.ftstudio");
var repository = new JsonProjectRepository();
var editor = new PhotoEditorViewModel(importer, new PhotoExporter(runner, probe), projectRepository: repository);
await editor.OpenAsync(inputPath, CancellationToken.None);
editor.RotateRight();
if (!await editor.SaveProjectAsync(projectPath, CancellationToken.None))
{
    throw new InvalidOperationException(editor.StatusText);
}

var reopened = new PhotoEditorViewModel(importer, new PhotoExporter(runner, probe), projectRepository: repository);
if (!await reopened.OpenProjectAsync(projectPath, CancellationToken.None) ||
    reopened.Current?.Operations.Count != editor.Current?.Operations.Count ||
    !editor.Current!.RenderExportImage().SequenceEqual(reopened.Current!.RenderExportImage()))
{
    throw new InvalidOperationException(reopened.StatusText);
}

Console.WriteLine($"format={format}; size={receipt.VerifiedMetadata.Width}x{receipt.VerifiedMetadata.Height}; color={receipt.VerifiedMetadata.ColorProfile}; bytes={receipt.OutputBytes}; path={receipt.OutputPath}");
Console.WriteLine($"photo-project-roundtrip={projectPath}; operations={reopened.Current.Operations.Count}");
