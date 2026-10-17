using System.Threading.Channels;

namespace FrameTheory.Media;

public sealed class ExportQueueJob
{
    private readonly CancellationTokenSource _cancellation;

    internal ExportQueueJob(Guid id, CancellationTokenSource cancellation, Task<ExportReceipt> completion)
    {
        Id = id;
        _cancellation = cancellation;
        Completion = completion;
    }

    public Guid Id { get; }

    public Task<ExportReceipt> Completion { get; }

    public void Cancel() => _cancellation.Cancel();
}

public sealed class ExportQueueService : IAsyncDisposable
{
    private readonly Channel<WorkItem> _queue = Channel.CreateUnbounded<WorkItem>();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _worker;

    public ExportQueueService() => _worker = Task.Run(ProcessAsync);

    public ExportQueueJob Enqueue(Func<CancellationToken, Task<ExportReceipt>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<ExportReceipt>(TaskCreationOptions.RunContinuationsAsynchronously);
        var item = new WorkItem(Guid.NewGuid(), operation, cancellation, completion);
        if (!_queue.Writer.TryWrite(item))
        {
            cancellation.Dispose();
            throw new InvalidOperationException("The export queue is closed.");
        }

        return new ExportQueueJob(item.Id, cancellation, completion.Task);
    }

    public async ValueTask DisposeAsync()
    {
        _queue.Writer.TryComplete();
        _shutdown.Cancel();
        try
        {
            await _worker.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        _shutdown.Dispose();
    }

    private async Task ProcessAsync()
    {
        try
        {
            await foreach (var item in _queue.Reader.ReadAllAsync(_shutdown.Token))
            {
                try
                {
                    item.Completion.TrySetResult(await item.Operation(item.Cancellation.Token).ConfigureAwait(false));
                }
                catch (OperationCanceledException exception)
                {
                    item.Completion.TrySetCanceled(exception.CancellationToken);
                }
                catch (Exception exception)
                {
                    item.Completion.TrySetException(exception);
                }
                finally
                {
                    item.Cancellation.Dispose();
                }
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
    }

    private sealed record WorkItem(
        Guid Id,
        Func<CancellationToken, Task<ExportReceipt>> Operation,
        CancellationTokenSource Cancellation,
        TaskCompletionSource<ExportReceipt> Completion);
}
