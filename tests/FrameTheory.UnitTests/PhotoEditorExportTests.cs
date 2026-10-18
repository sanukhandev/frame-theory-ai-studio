using FrameTheory.Desktop;
using FrameTheory.Domain;
using FrameTheory.Media;
using FrameTheory.Photo;
using FrameTheory.Storage;
using System.Text;

namespace FrameTheory.UnitTests;

public sealed class PhotoEditorExportTests
{
    [Xunit.Fact]
    public async Task QueuedPhotoExportUsesStateCapturedBeforeLaterEdits()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-photo-export-queue-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.png");
            var outputPath = Path.Combine(directory.FullName, "output.png");
            await File.WriteAllTextAsync(sourcePath, "source");
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<ExportReceipt>(TaskCreationOptions.RunContinuationsAsynchronously);
            var processRunner = new RecordingProcessRunner();
            await using var queue = new ExportQueueService();
            var blocker = queue.Enqueue(async cancellationToken =>
            {
                started.SetResult();
                return await release.Task.WaitAsync(cancellationToken);
            });
            var editor = new PhotoEditorViewModel(
                new PhotoImporter(new StubProbe(), new StubDecoder()),
                new PhotoExporter(processRunner, new StubProbe()),
                queue,
                new JsonProjectRepository());

            await editor.OpenAsync(sourcePath, CancellationToken.None);
            var export = editor.ExportAsync(outputPath, PhotoExportFormat.Png, 95, CancellationToken.None);
            await started.Task;
            editor.RotateRight();
            release.SetResult(Receipt("blocker"));

            await export;

            Xunit.Assert.Contains("Exported", editor.StatusText, StringComparison.Ordinal);
            Xunit.Assert.NotNull(processRunner.CapturedInput);
            Xunit.Assert.StartsWith("P6\n2 1", Encoding.ASCII.GetString(processRunner.CapturedInput!));
            await blocker.Completion;
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private static ExportReceipt Receipt(string name) => new(
        Guid.NewGuid(),
        Path.Combine(Path.GetTempPath(), name + ".mp4"),
        1,
        new MediaSourceMetadata(
            MediaId.Create(new Uri("file:///photo-queue.mp4"), name),
            new Uri("file:///photo-queue.mp4"),
            MediaKind.Video,
            1,
            1,
            new RationalFrameRate(1, 1),
            1,
            TimeSpan.FromSeconds(1)));

    private sealed class StubProbe : IMediaProbe
    {
        public Task<MediaSourceMetadata> ProbeAsync(Uri file, CancellationToken cancellationToken) => Task.FromResult(
            new MediaSourceMetadata(
                MediaId.Create(file, "photo-export-queue"),
                file,
                MediaKind.Image,
                2,
                1,
                null,
                0,
                TimeSpan.Zero,
                colorProfile: "sRGB"));
    }

    private sealed class StubDecoder : IPreviewFrameDecoder
    {
        public Task<byte[]> DecodeFrameAsync(Uri file, RationalFrameRate frameRate, long frameIndex, CancellationToken cancellationToken) =>
            Task.FromResult<byte[]>([.. "P6\n2 1\n255\n"u8, 255, 0, 0, 0, 0, 255]);
    }

    private sealed class RecordingProcessRunner : IMediaProcessRunner
    {
        public byte[]? CapturedInput { get; private set; }

        public async Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            var inputIndex = arguments.ToList().IndexOf("-i");
            CapturedInput = await File.ReadAllBytesAsync(arguments[inputIndex + 1], cancellationToken);
            await File.WriteAllTextAsync(arguments[^1], "encoded", cancellationToken);
            return new MediaProcessResult(0, string.Empty, string.Empty);
        }
    }
}
