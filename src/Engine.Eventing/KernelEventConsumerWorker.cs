namespace Engine.Eventing;

public interface IKernelEventConsumer
{
    ValueTask ConsumeAsync(SequencedKernelEvent kernelEvent, CancellationToken cancellationToken);
}

public sealed class KernelEventConsumerWorker : IAsyncDisposable
{
    private readonly KernelEventLog _eventLog;
    private readonly IKernelEventConsumer _consumer;
    private readonly KernelEventCursor _cursor;
    private readonly SequencedKernelEvent[] _buffer;
    private readonly TimeSpan _idleDelay;
    private readonly CancellationTokenSource _stop = new();
    private Task? _completion;
    private int _started;

    public KernelEventConsumerWorker(
        KernelEventLog eventLog,
        IKernelEventConsumer consumer,
        KernelEventCursorStart cursorStart = KernelEventCursorStart.Latest,
        int batchSize = 256,
        TimeSpan? idleDelay = null)
    {
        ArgumentNullException.ThrowIfNull(eventLog);
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        _eventLog = eventLog;
        _consumer = consumer;
        _cursor = eventLog.CreateCursor(cursorStart);
        _buffer = new SequencedKernelEvent[batchSize];
        _idleDelay = idleDelay ?? TimeSpan.FromMilliseconds(1);
    }

    public Task Completion => _completion ?? Task.CompletedTask;

    public Exception? Fault { get; private set; }

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException("Kernel event consumer worker has already been started.");
        }

        _completion = Task.Run(RunAsync);
    }

    public async ValueTask StopAsync()
    {
        await _stop.CancelAsync().ConfigureAwait(false);
        if (_completion is null)
        {
            return;
        }

        try
        {
            await _completion.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _cursor.Dispose();
        _stop.Dispose();
    }

    private async Task RunAsync()
    {
        var cancellationToken = _stop.Token;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = _eventLog.Read(_cursor, _buffer);
                if (read == 0)
                {
                    await Task.Delay(_idleDelay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                for (var i = 0; i < read; i++)
                {
                    await _consumer.ConsumeAsync(_buffer[i], cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Fault = ex;
            throw;
        }
    }
}
