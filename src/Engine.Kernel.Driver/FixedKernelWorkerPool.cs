namespace Engine.Kernel.Driver;

using Engine.Kernel;
using System.Runtime.ExceptionServices;

internal sealed class FixedKernelWorkerPool : IKernelWorkScheduler, IDisposable
{
    internal const int MaximumWorkerCount = 16;

    private readonly SemaphoreSlim[] _startSignals;
    private readonly SemaphoreSlim _finishSignal;
    private readonly KernelWorker[] _workers;
    private IKernelParallelJob? _job;
    private Exception? _failure;
    private int _activeWorkerCount;
    private int _startInclusive;
    private int _endExclusive;
    private int _runActive;
    private bool _stopping;
    private bool _disposed;

    internal FixedKernelWorkerPool(int workerCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workerCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(workerCount, MaximumWorkerCount);
        WorkerCount = workerCount;
        _finishSignal = new SemaphoreSlim(0, Math.Max(1, workerCount - 1));
        _workers = new KernelWorker[workerCount - 1];
        _startSignals = new SemaphoreSlim[workerCount - 1];
        for (var workerIndex = 1; workerIndex < workerCount; workerIndex++)
        {
            _startSignals[workerIndex - 1] = new SemaphoreSlim(0, 1);
            _workers[workerIndex - 1] = new KernelWorker(workerIndex, RunWorker);
        }
    }

    public int WorkerCount { get; }

    public void Run(
        IKernelParallelJob job,
        int startInclusive,
        int endExclusive,
        int minimumChunkSize)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentOutOfRangeException.ThrowIfNegative(startInclusive);
        ArgumentOutOfRangeException.ThrowIfLessThan(endExclusive, startInclusive);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumChunkSize);
        if (startInclusive >= endExclusive)
        {
            return;
        }

        if (WorkerCount == 1 || endExclusive - startInclusive <= minimumChunkSize)
        {
            job.Execute(0, startInclusive, endExclusive);
            return;
        }

        EnterRun();
        try
        {
            _job = job;
            _failure = null;
            _startInclusive = startInclusive;
            _endExclusive = endExclusive;
            _activeWorkerCount = Math.Min(
                WorkerCount,
                ((endExclusive - startInclusive) + minimumChunkSize - 1) / minimumChunkSize);
            for (var workerIndex = 1; workerIndex < _activeWorkerCount; workerIndex++)
            {
                _startSignals[workerIndex - 1].Release();
            }

            RunPartition(0);
            for (var workerIndex = 1; workerIndex < _activeWorkerCount; workerIndex++)
            {
                _finishSignal.Wait();
            }

            if (_failure is not null)
            {
                ExceptionDispatchInfo.Capture(_failure).Throw();
            }
        }
        finally
        {
            _job = null;
            Volatile.Write(ref _runActive, 0);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        var spin = new SpinWait();
        while (Interlocked.CompareExchange(ref _runActive, 1, 0) != 0)
        {
            spin.SpinOnce();
        }

        _disposed = true;
        _stopping = true;
        for (var i = 0; i < _startSignals.Length; i++)
        {
            _startSignals[i].Release();
        }

        for (var i = 0; i < _workers.Length; i++)
        {
            _workers[i].Join();
        }

        _finishSignal.Dispose();
        for (var i = 0; i < _startSignals.Length; i++)
        {
            _startSignals[i].Dispose();
        }
    }

    private void RunWorker(int workerIndex)
    {
        while (true)
        {
            _startSignals[workerIndex - 1].Wait();
            if (_stopping)
            {
                return;
            }

            RunPartition(workerIndex);
            _finishSignal.Release();
        }
    }

    private void RunPartition(int workerIndex)
    {
        if (workerIndex >= _activeWorkerCount)
        {
            return;
        }

        try
        {
            var itemCount = _endExclusive - _startInclusive;
            var start = _startInclusive + ((itemCount * workerIndex) / _activeWorkerCount);
            var end = _startInclusive + ((itemCount * (workerIndex + 1)) / _activeWorkerCount);
            _job!.Execute(workerIndex, start, end);
        }
#pragma warning disable CA1031
        catch (Exception exception)
#pragma warning restore CA1031
        {
            _ = Interlocked.CompareExchange(ref _failure, exception, null);
        }
    }

    private void EnterRun()
    {
        if (Interlocked.CompareExchange(ref _runActive, 1, 0) != 0)
        {
            throw new InvalidOperationException();
        }
    }
}
