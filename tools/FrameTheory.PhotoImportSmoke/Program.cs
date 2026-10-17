using FrameTheory.Media;
using FrameTheory.Photo;

if (args.Length != 1)
{
    throw new ArgumentException("Usage: FrameTheory.PhotoImportSmoke <image-path>");
}

var path = Path.GetFullPath(args[0]);
var runner = new FfmpegProcessRunner();
var document = await new PhotoImporter(new FfprobeMediaProbe(runner), new FfmpegFrameDecoder(runner))
    .ImportAsync(new Uri(path), CancellationToken.None);
var canvas = new PhotoCanvasState(document);
canvas.Fit(new PhotoViewport(640, 480));
Console.WriteLine($"kind={document.Metadata.Kind}; size={document.Metadata.Width}x{document.Metadata.Height}; preview_bytes={document.PreviewPpm.Length}; zoom={canvas.Zoom:0.###}");
