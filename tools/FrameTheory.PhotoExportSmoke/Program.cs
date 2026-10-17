using FrameTheory.Media;
using FrameTheory.Photo;
using FrameTheory.Desktop;
using FrameTheory.Storage;

if (args.Length is < 2 or > 5)
{
    throw new ArgumentException("Usage: FrameTheory.PhotoExportSmoke <image-path> <destination-path> [png|jpeg] [overlay-path] [16:9|1:1|9:16]");
}

var inputPath = Path.GetFullPath(args[0]);
var destinationPath = Path.GetFullPath(args[1]);
var optionalPath = args.Length >= 4 ? args[3] : null;
var overlayPath = optionalPath is not null && !IsPreset(optionalPath) ? Path.GetFullPath(optionalPath) : null;
var presetArgument = overlayPath is null ? optionalPath : args.Length == 5 ? args[4] : null;
var format = args.Length >= 3 && args[2].Equals("jpeg", StringComparison.OrdinalIgnoreCase)
    ? PhotoExportFormat.Jpeg
    : PhotoExportFormat.Png;
var preset = presetArgument is not null
    ? presetArgument switch
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
var editor = new PhotoEditorViewModel(importer, new PhotoExporter(runner, probe), projectRepository: new JsonProjectRepository());
await editor.OpenAsync(inputPath, CancellationToken.None);
if (editor.Current is null)
{
    throw new InvalidOperationException(editor.StatusText);
}

if (overlayPath is not null)
{
    await editor.ImportOverlayAsync(overlayPath, 0, 0, 0.5f, PhotoBlendMode.Screen, CancellationToken.None);
    var overlayState = editor.Current;
    if (overlayState is null || overlayState.CompositeLayers.Count != 1)
    {
        throw new InvalidOperationException(editor.StatusText);
    }

    editor.MaskLastOverlay(0, 0, 1, 1);
    editor.ApplyRetouch(1, 0, 1, -1, 0, 1);
    overlayState = editor.Current;
    if (overlayState is null || overlayState.CompositeLayers[0].MaskStrokes.Count != 1 || overlayState.RetouchStrokes.Count != 1)
    {
        throw new InvalidOperationException(editor.StatusText);
    }
}

editor.RotateRight();
var editedState = editor.Current;
if (editedState is null)
{
    throw new InvalidOperationException(editor.StatusText);
}

var receipt = await new PhotoExporter(runner, new FfprobeMediaProbe(runner)).ExportAsync(
    editedState,
    destinationPath,
    new PhotoExportOptions(format, 90, preset),
    new Progress<ExportProgress>(progress => Console.WriteLine($"{progress.Stage} {progress.Fraction:P0}")),
    CancellationToken.None);
var projectPath = Path.Combine(Path.GetDirectoryName(destinationPath) ?? ".", "photo-project.ftstudio");
var repository = new JsonProjectRepository();
if (!await editor.SaveProjectAsync(projectPath, CancellationToken.None))
{
    throw new InvalidOperationException(editor.StatusText);
}

var reopened = new PhotoEditorViewModel(importer, new PhotoExporter(runner, probe), projectRepository: repository);
if (!await reopened.OpenProjectAsync(projectPath, CancellationToken.None) ||
    reopened.Current?.Operations.Count != editedState.Operations.Count ||
    reopened.Current?.CompositeLayers.Count != editedState.CompositeLayers.Count ||
    !editedState.RenderExportImage().SequenceEqual(reopened.Current!.RenderExportImage()))
{
    throw new InvalidOperationException(reopened.StatusText);
}

Console.WriteLine($"format={format}; overlay={(overlayPath is null ? "none" : Path.GetFileName(overlayPath))}; size={receipt.VerifiedMetadata.Width}x{receipt.VerifiedMetadata.Height}; color={receipt.VerifiedMetadata.ColorProfile}; bytes={receipt.OutputBytes}; path={receipt.OutputPath}");
Console.WriteLine($"photo-project-roundtrip={projectPath}; operations={reopened.Current.Operations.Count}; overlays={reopened.Current.CompositeLayers.Count}; masks={reopened.Current.CompositeLayers.Sum(layer => layer.MaskStrokes.Count)}; retouch={reopened.Current.RetouchStrokes.Count}");

static bool IsPreset(string value) => value is "16:9" or "1:1" or "9:16";
