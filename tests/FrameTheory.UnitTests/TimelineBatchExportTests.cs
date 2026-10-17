using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class TimelineBatchExportTests
{
    [Xunit.Fact]
    public async Task BatchExportsTargetsInOrderAndReportsCompletion()
    {
        var exporter = new RecordingTimelineExporter();
        var progress = new List<TimelineBatchExportProgress>();
        var plan = Plan(16, 9);
        var targets = new[]
        {
            Target("landscape", plan, "landscape.mp4"),
            Target("square", Plan(9, 9), "square.mp4")
        };

        var receipts = await new TimelineBatchExporter(exporter).ExportAsync(
            targets,
            new InlineProgress<TimelineBatchExportProgress>(progress.Add),
            CancellationToken.None);

        Xunit.Assert.Equal(new[] { "landscape.mp4", "square.mp4" }, exporter.Outputs.Select(Path.GetFileName));
        Xunit.Assert.Equal(2, receipts.Count);
        Xunit.Assert.Equal(2, progress.Last().CompletedTargets);
        Xunit.Assert.Equal("square", progress.Last().TargetName);
    }

    [Xunit.Fact]
    public async Task BatchRejectsDuplicateDestinationsBeforeExport()
    {
        var exporter = new RecordingTimelineExporter();
        var plan = Plan(16, 9);
        var targets = new[]
        {
            Target("first", plan, "same.mp4"),
            Target("second", plan, "same.mp4")
        };

        await Xunit.Assert.ThrowsAsync<ArgumentException>(() => new TimelineBatchExporter(exporter).ExportAsync(targets, null, CancellationToken.None));
        Xunit.Assert.Empty(exporter.Outputs);
    }

    [Xunit.Fact]
    public async Task BatchHonorsCancellationBeforeStartingNextTarget()
    {
        var exporter = new RecordingTimelineExporter();
        using var cancellation = new CancellationTokenSource();
        var plan = Plan(16, 9);
        var targets = new[]
        {
            Target("first", plan, "first.mp4"),
            Target("second", plan, "second.mp4")
        };
        cancellation.Cancel();

        await Xunit.Assert.ThrowsAnyAsync<OperationCanceledException>(() => new TimelineBatchExporter(exporter).ExportAsync(targets, null, cancellation.Token));
        Xunit.Assert.Empty(exporter.Outputs);
    }

    private static TimelineBatchExportTarget Target(string name, TimelineVideoRenderPlan plan, string path) =>
        new(name, plan, path, new ExportPreset(plan.Width, plan.Height, plan.FrameRate, includeAudio: false));

    private static TimelineVideoRenderPlan Plan(int width, int height) => new(
        new RationalFrameRate(30, 1),
        width,
        height,
        30,
        new Dictionary<MediaId, Uri>(),
        []);

    private sealed class RecordingTimelineExporter : ITimelineVideoExporter
    {
        public List<string> Outputs { get; } = [];

        public Task<ExportReceipt> ExportAsync(
            TimelineVideoRenderPlan plan,
            string destinationPath,
            ExportPreset preset,
            IProgress<ExportProgress>? progress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Outputs.Add(destinationPath);
            progress?.Report(new ExportProgress("Completed", 1));
            return Task.FromResult(new ExportReceipt(
                Guid.NewGuid(),
                destinationPath,
                1,
                new MediaSourceMetadata(
                    MediaId.Create(new Uri("file:///batch.mp4"), Path.GetFileNameWithoutExtension(destinationPath)),
                    new Uri("file:///batch.mp4"),
                    MediaKind.Video,
                    plan.Width,
                    plan.Height,
                    plan.FrameRate,
                    1)));
        }
    }

    private sealed class InlineProgress<T>(Action<T> callback) : IProgress<T>
    {
        public void Report(T value) => callback(value);
    }
}
