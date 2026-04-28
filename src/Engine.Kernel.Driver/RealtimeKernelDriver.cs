namespace Engine.Kernel.Driver;

using System.Diagnostics;
using System.Threading;

internal sealed class RealtimeKernelDriver : IKernelDriver, IDriverControl
{
    private readonly IKernel _kernel;
    private readonly object _configGate = new();
    private readonly RealtimeTickAccumulator _tickAccumulator;
    private Thread? _thread;
    private RealtimeDriverConfig _config;
    private int _state;
    private int _started;
    private int _stopRequested;
    private bool _disposed;

    public RealtimeKernelDriver(IKernel kernel, RealtimeDriverConfig config)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        ValidateConfig(config);

        _kernel = kernel;
        _config = config;
        _tickAccumulator = new RealtimeTickAccumulator(config.MaxTicksPerSlice);
    }

    public DriverState State => (DriverState)Volatile.Read(ref _state);

    public int OverrunCount => _tickAccumulator.OverrunCount;

    public Task RunAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException();
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Volatile.Write(ref _state, (int)DriverState.Running);
        _thread = new Thread(() => RunLoop(completion, cancellationToken))
        {
            IsBackground = true,
        };
        _thread.Start();
        return completion.Task;
    }

    public void SetTickRate(double ticksPerSecond)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticksPerSecond);
        lock (_configGate)
        {
            _config = _config with { TicksPerSecond = ticksPerSecond };
        }
    }

    public void RequestPause() => RequestStop();

    public void RequestResume()
    {
    }

    public void RequestSingleStep(int tickCount)
    {
        _ = tickCount;
        throw new NotSupportedException();
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

    private static void ValidateConfig(RealtimeDriverConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.TicksPerSecond < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.TicksPerSecond, null);
        }

        if (config.MaxTicksPerSlice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.MaxTicksPerSlice, null);
        }

        if (config.SliceInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(config), config.SliceInterval, null);
        }
    }

    private RealtimeDriverConfig ReadConfig()
    {
        lock (_configGate)
        {
            return _config;
        }
    }

    private void RunLoop(TaskCompletionSource completion, CancellationToken cancellationToken)
    {
        try
        {
            var lastTimestamp = Stopwatch.GetTimestamp();
            while (!cancellationToken.IsCancellationRequested && Volatile.Read(ref _stopRequested) == 0)
            {
                var config = ReadConfig();
                var now = Stopwatch.GetTimestamp();
                var elapsed = Stopwatch.GetElapsedTime(lastTimestamp, now);
                lastTimestamp = now;
                var ticks = _tickAccumulator.ConsumeTicks(elapsed, config.TicksPerSecond);
                if (ticks > 0)
                {
                    var options = new AdvanceOptions(Cancel: cancellationToken);
                    _ = _kernel.AdvanceTicks(ticks, in options);
                }

                Thread.Sleep(config.SliceInterval);
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
