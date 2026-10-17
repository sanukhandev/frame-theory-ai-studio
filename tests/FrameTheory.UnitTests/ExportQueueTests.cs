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

        Xunit.Assert.InRange(order.Count, 1, 2);
        Xunit.Assert.Equal(1, order[0]);
        Xunit.Assert.Equal(ExportQueueJobState.Canceled, second.State);
        Xunit.Assert.Contains(queue.History, item => item.Id == second.Id && item.State == ExportQueueJobState.Canceled);
    }

    [Xunit.Fact]
    public async Task QueueRetriesFailedJobsAndRecordsAttempts()
    {
        await using var queue = new ExportQueueService();
        var attempts = 0;
        var job = queue.Enqueue(_ =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                throw new InvalidOperationException("transient export failure");
            }

            return Task.FromResult(Receipt("retry"));
        });

        await Xunit.Assert.ThrowsAsync<InvalidOperationException>(() => job.Completion);
        var retry = queue.Retry(job);
        await retry.Completion;

        Xunit.Assert.Equal(2, attempts);
        Xunit.Assert.Equal(ExportQueueJobState.Failed, job.State);
        Xunit.Assert.Equal(ExportQueueJobState.Succeeded, retry.State);
        Xunit.Assert.Equal(1, job.Attempts);
        Xunit.Assert.Equal(1, retry.Attempts);
        Xunit.Assert.Contains(queue.History, item => item.Id == job.Id && item.ErrorMessage == "transient export failure");
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
