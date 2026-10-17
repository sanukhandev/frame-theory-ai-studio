using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class ExportQueueTests
{
    [Xunit.Fact]
    public async Task QueueRunsJobsInOrderAndSupportsCancellation()
    {
        await using var queue = new ExportQueueService();
        var order = new List<int>();
        var first = queue.Enqueue(async cancellationToken =>
        {
            order.Add(1);
            await Task.Delay(10, cancellationToken);
            return Receipt("first");
        });
        var second = queue.Enqueue(async cancellationToken =>
        {
            order.Add(2);
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            return Receipt("second");
        });

        await first.Completion;
        second.Cancel();
        await Xunit.Assert.ThrowsAnyAsync<OperationCanceledException>(() => second.Completion);

        Xunit.Assert.Equal([1, 2], order);
    }

    private static ExportReceipt Receipt(string name) => new(
        Guid.NewGuid(),
        Path.Combine(Path.GetTempPath(), name + ".mp4"),
        1,
        new MediaSourceMetadata(
            MediaId.Create(new Uri("file:///queue.mp4"), name),
            new Uri("file:///queue.mp4"),
            MediaKind.Video,
            16,
            9,
            new RationalFrameRate(30, 1),
            1));
}
