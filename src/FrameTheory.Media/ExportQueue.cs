using System.Threading.Channels;

namespace FrameTheory.Media;

public enum ExportQueueJobState
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Canceled
}

public sealed record ExportQueueHistoryEntry(
    Guid Id,
    ExportQueueJobState State,
    int Attempts,
    string? ErrorMessage);

public sealed class ExportQueueJob
{
    private readonly object _sync = new();
    private readonly CancellationTokenSource _cancellation;
    private readonly TaskCompletionSource<ExportReceipt> _completion;
    private readonly Func<CancellationToken, Task<ExportReceipt>> _operation;
    private ExportQueueJobState _state = ExportQueueJobState.Queued;
    private int _attempts;
    private string? _errorMessage;

    internal ExportQueueJob(
        Guid id,
        CancellationTokenSource cancellation,
        TaskCompletionSource<ExportReceipt> completion,
        Func<CancellationToken, Task<ExportReceipt>> operation)
    {
        Id = id;
        _cancellation = cancellation;
        _completion = completion;
        _operation = operation;
        Completion = completion.Task;
    }

    public Guid Id { get; }

    public Task<ExportReceipt> Completion { get; }

    public ExportQueueJobState State
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    public int Attempts
    {
        get
        {
            lock (_sync)
            {
                return _attempts;
            }
        }
    }

    public string? ErrorMessage
    {
        get
        {
            lock (_sync)
            {
                return _errorMessage;
            }
        }
    }

    public void Cancel() => _ = TryCancel();

    internal bool TryCancel()
    {
        lock (_sync)
        {
            if (_state is ExportQueueJobState.Succeeded or ExportQueueJobState.Failed or ExportQueueJobState.Canceled)
            {
                return false;
            }

            _state = ExportQueueJobState.Canceled;
        }

        try
        {
            _cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        _completion.TrySetCanceled();
        return true;
    }

    internal Func<CancellationToken, Task<ExportReceipt>> Operation => _operation;

    internal void MarkRunning()
    {
        lock (_sync)
        {
            _state = ExportQueueJobState.Running;
        }
    }

    internal bool TryMarkSucceeded()
    {
        lock (_sync)
        {
            if (_state != ExportQueueJobState.Running)
            {
                return false;
            }

            _state = ExportQueueJobState.Succeeded;
            return true;
        }
    }

    internal bool TryMarkFailed(Exception exception)
    {
        lock (_sync)
        {
            if (_state != ExportQueueJobState.Running)
            {
                return false;
            }

            _errorMessage = exception.Message;
            _state = ExportQueueJobState.Failed;
            return true;
        }
    }

    internal void MarkAttempt()
    {
        lock (_sync)
        {
            _attempts++;
        }
    }
}

public sealed class ExportQueueService : IAsyncDisposable
{
    private readonly Channel<WorkItem> _queue = Channel.CreateUnbounded<WorkItem>();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _worker;

    public ExportQueueService() => _worker = Task.Run(ProcessAsync);

    public IReadOnlyList<ExportQueueHistoryEntry> History
    {
        get
        {
            lock (_history)
            {
                return Array.AsReadOnly(_history.Select(job => new ExportQueueHistoryEntry(job.Id, job.State, job.Attempts, job.ErrorMessage)).ToArray());
            }
        }
    }

    private readonly List<ExportQueueJob> _history = [];

    public ExportQueueJob Enqueue(Func<CancellationToken, Task<ExportReceipt>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<ExportReceipt>(TaskCreationOptions.RunContinuationsAsynchronously);
        var job = new ExportQueueJob(Guid.NewGuid(), cancellation, completion, operation);
        var item = new WorkItem(job.Id, operation, cancellation, completion, job);
        if (!_queue.Writer.TryWrite(item))
        {
            cancellation.Dispose();
            throw new InvalidOperationException("The export queue is closed.");
        }

        lock (_history)
        {
            _history.Add(job);
        }

        return job;
    }

    public ExportQueueJob Retry(ExportQueueJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (job.State is not (ExportQueueJobState.Failed or ExportQueueJobState.Canceled))
        {
            throw new InvalidOperationException("Only failed or canceled export jobs can be retried.");
        }

        return Enqueue(job.Operation);
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
                    if (item.Cancellation.IsCancellationRequested)
                    {
                        item.Completion.TrySetCanceled();
                        continue;
                    }

                    item.Job.MarkRunning();
                    item.Job.MarkAttempt();
                    var receipt = await item.Operation(item.Cancellation.Token).ConfigureAwait(false);
                    if (item.Cancellation.IsCancellationRequested)
                    {
                        item.Job.TryCancel();
                    }
                    else if (item.Job.TryMarkSucceeded())
                    {
                        item.Completion.TrySetResult(receipt);
                    }
                }
                catch (OperationCanceledException exception)
                {
                    item.Job.TryCancel();
                    item.Completion.TrySetCanceled(exception.CancellationToken);
                }
                catch (Exception exception)
                {
                    if (item.Job.TryMarkFailed(exception))
                    {
                        item.Completion.TrySetException(exception);
                    }
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
        TaskCompletionSource<ExportReceipt> Completion,
        ExportQueueJob Job);
}
