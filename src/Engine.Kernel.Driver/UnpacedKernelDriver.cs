namespace Engine.Kernel.Driver;

using System.Threading;

public sealed class UnpacedKernelDriver : IKernelDriver
{
    private readonly IKernel _kernel;
    private readonly int _batchSize;
    private readonly StopPredicate? _stopAt;
    private Thread? _thread;
    private TaskCompletionSource? _completion;
    private int _state;
    private int _started;
    private int _stopRequested;
    private bool _disposed;

    public UnpacedKernelDriver(IKernel kernel, int batchSize = 4096, StopPredicate? stopAt = null)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        _kernel = kernel;
        _batchSize = batchSize;
        _stopAt = stopAt;
    }

    public DriverState State => (DriverState)Volatile.Read(ref _state);

    public Task RunAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException();
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _completion = completion;
        Volatile.Write(ref _state, (int)DriverState.Running);
        _thread = new Thread(() => RunLoop(completion, cancellationToken))
        {
            IsBackground = true,
        };
        _thread.Start();
        return completion.Task;
    }

    public void RequestStop()
    {
        Volatile.Write(ref _stopRequested, 1);
        if (State == DriverState.Running)
        {
            Volatile.Write(ref _state, (int)DriverState.Draining);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        RequestStop();
        _thread?.Join();
    }

    private void RunLoop(TaskCompletionSource completion, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && Volatile.Read(ref _stopRequested) == 0)
            {
                var options = new AdvanceOptions(_stopAt, cancellationToken);
                var advanced = _kernel.AdvanceTicks(_batchSize, in options);
                if (advanced < _batchSize)
                {
                    break;
                }
            }

            Volatile.Write(ref _state, (int)DriverState.Stopped);
            completion.SetResult();
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Volatile.Write(ref _state, (int)DriverState.Stopped);
            completion.SetException(ex);
        }
    }
}
