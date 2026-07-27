namespace Engine.Kernel.Driver;

internal sealed class SteppedKernelDriver : IKernelDriver, IDriverControl
{
    private readonly IKernel _kernel;
    private int _state;
    private bool _disposed;

    public SteppedKernelDriver(IKernel kernel)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        _kernel = kernel;
    }

    public DriverState State => (DriverState)Volatile.Read(ref _state);

    public int Step(int tickCount, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(tickCount);
        Volatile.Write(ref _state, (int)DriverState.Running);
        try
        {
            var start = _kernel.PublishedLowWatermark;
            var target = checked(start + tickCount);
            var converged = _kernel.ConvergeAt(
                target,
                cancellationToken);
            return Math.Max(0, converged - start);
        }
        finally
        {
            Volatile.Write(ref _state, (int)DriverState.Stopped);
        }
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Task.CompletedTask;
    }

    public void SetTickRate(double ticksPerSecond)
    {
        _ = ticksPerSecond;
        throw new NotSupportedException();
    }

    public void RequestPause()
    {
        Volatile.Write(ref _state, (int)DriverState.Stopped);
    }

    public void RequestResume()
    {
    }

    public void RequestSingleStep(int tickCount)
    {
        _ = Step(tickCount);
    }

    public void RequestStop()
    {
        Volatile.Write(ref _state, (int)DriverState.Stopped);
    }

    public void Dispose()
    {
        _disposed = true;
        RequestStop();
    }
}
