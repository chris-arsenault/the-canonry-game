namespace Engine.Kernel.Driver;

internal sealed class KernelDriverTimeController : IKernelDriverTimeControl
{
    private readonly IKernel _kernel;
    private readonly object _gate = new();
    private RealtimeDriverConfig _config;
    private RealtimeKernelDriver? _realtimeDriver;
    private double _lastNonZeroTicksPerSecond;
    private bool _disposed;

    public KernelDriverTimeController(IKernel kernel, RealtimeDriverConfig config)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        ValidateConfig(config);

        _kernel = kernel;
        _config = config;
        _lastNonZeroTicksPerSecond = config.TicksPerSecond > 0d ? config.TicksPerSecond : 1d;
    }

    public DriverState State
    {
        get
        {
            lock (_gate)
            {
                return _realtimeDriver?.State ?? DriverState.Stopped;
            }
        }
    }

    public double TicksPerSecond
    {
        get
        {
            lock (_gate)
            {
                return _config.TicksPerSecond;
            }
        }
    }

    public bool IsPaused
    {
        get
        {
            lock (_gate)
            {
                return _realtimeDriver is null || _realtimeDriver.State != DriverState.Running;
            }
        }
    }

    public int PublishedLowWatermark => _kernel.PublishedLowWatermark;

    public int PublishedHighWatermark => _kernel.PublishedHighWatermark;

    public void SetTickRate(double ticksPerSecond)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticksPerSecond);
        ThrowIfDisposed();

        if (ticksPerSecond == 0d)
        {
            RequestPause();
            lock (_gate)
            {
                _config = _config with { TicksPerSecond = 0d };
            }

            return;
        }

        lock (_gate)
        {
            ThrowIfDisposed();
            _lastNonZeroTicksPerSecond = ticksPerSecond;
            _config = _config with { TicksPerSecond = ticksPerSecond };
            if (_realtimeDriver is not null)
            {
                _realtimeDriver.SetTickRate(ticksPerSecond);
                return;
            }

            StartRealtimeDriverLocked();
        }
    }

    public void RequestPause() => StopRealtimeDriver();

    public void RequestResume()
    {
        ThrowIfDisposed();

        lock (_gate)
        {
            ThrowIfDisposed();
            var ticksPerSecond = _config.TicksPerSecond > 0d
                ? _config.TicksPerSecond
                : _lastNonZeroTicksPerSecond;
            _config = _config with { TicksPerSecond = ticksPerSecond };
            if (_realtimeDriver is null)
            {
                StartRealtimeDriverLocked();
            }
        }
    }

    public void RequestSingleStep(int tickCount)
    {
        _ = AdvanceSingleStep(tickCount);
    }

    public int AdvanceSingleStep(int tickCount, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        StopRealtimeDriver();
        using var steppedDriver = new SteppedKernelDriver(_kernel);
        return steppedDriver.Step(tickCount, cancellationToken);
    }

    public int ConvergeAt(
        int tick,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        StopRealtimeDriver();
        return _kernel.ConvergeAt(tick, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopRealtimeDriver(allowDuringDispose: true);
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

    private void StartRealtimeDriverLocked()
    {
        var driver = new RealtimeKernelDriver(_kernel, _config);
        _realtimeDriver = driver;
        _ = driver.RunAsync(CancellationToken.None).ContinueWith(
            task =>
            {
                _ = task;
                ClearStoppedRealtimeDriver(driver);
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private void ClearStoppedRealtimeDriver(RealtimeKernelDriver driver)
    {
        lock (_gate)
        {
            if (ReferenceEquals(_realtimeDriver, driver) && driver.State == DriverState.Stopped)
            {
                _realtimeDriver = null;
            }
        }
    }

    private void StopRealtimeDriver(bool allowDuringDispose = false)
    {
        RealtimeKernelDriver? driver;
        lock (_gate)
        {
            ThrowIfDisposed(allowDuringDispose);
            driver = _realtimeDriver;
            _realtimeDriver = null;
        }

        driver?.Dispose();
        if (driver is not null)
        {
            _ = _kernel.ConvergeAt(
                _kernel.PublishedHighWatermark);
        }
    }

    private void ThrowIfDisposed(bool allowDuringDispose = false)
    {
        if (allowDuringDispose && _disposed)
        {
            return;
        }

        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
