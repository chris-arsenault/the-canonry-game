namespace Engine.Kernel;

internal interface IKernelContinuousOwnerWork
{
    bool ExecuteOwner(Simulation simulation, int ownerIndex);

    bool WindowQuiescent(Simulation simulation);

    bool HasRunnableWork(
        Simulation simulation,
        int firstOwner,
        int ownerLimit);
}

internal sealed class KernelWorkerSet
{
    private readonly KernelWorker[] _workers;
    private readonly object _completionGate = new();
    private int _remaining;
    private int _continuousStop;
    private bool _started;
    private bool _shutdown;

    internal KernelWorkerSet(int workerCount)
    {
        NumericGuard.ThrowIfNegativeOrZero(workerCount);
        _workers = new KernelWorker[workerCount];
        for (var workerIndex = 0; workerIndex < workerCount; workerIndex++)
        {
            _workers[workerIndex] = new KernelWorker(
                this,
                workerIndex,
                background: workerIndex > 0);
        }

    }

    internal int WorkerCount => _workers.Length;

    internal void ExecuteContinuousOwners(
        Simulation simulation,
        IKernelContinuousOwnerWork work,
        int logicalOwnerCount,
        in KernelExecutionContext context)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        global::System.ArgumentNullException.ThrowIfNull(work);
        global::System.ObjectDisposedException.ThrowIf(_shutdown, this);
        NumericGuard.ThrowIfNegativeOrZero(logicalOwnerCount);

        var activeWorkerCount = global::System.Math.Min(
            WorkerCount,
            logicalOwnerCount);
        if (activeWorkerCount == 1)
        {
            global::System.Threading.Volatile.Write(
                ref _continuousStop,
                0);
            _workers[0].ConfigureContinuousOwners(
                simulation,
                work,
                firstOwner: 0,
                ownerLimit: logicalOwnerCount,
                context);
            try
            {
                _workers[0].ExecuteConfigured();
            }
            finally
            {
                _workers[0].ReleaseConfigured();
            }

            return;
        }

        EnsureStarted(activeWorkerCount);
        while (true)
        {
            global::System.Threading.Volatile.Write(
                ref _continuousStop,
                0);
            global::System.Threading.Volatile.Write(
                ref _remaining,
                activeWorkerCount - 1);
            ConfigureContinuousWorkers(
                simulation,
                work,
                logicalOwnerCount,
                activeWorkerCount,
                context);
            ExecuteWorkers(activeWorkerCount);
            if (work.WindowQuiescent(simulation))
            {
                return;
            }
        }
    }

    private void ConfigureContinuousWorkers(
        Simulation simulation,
        IKernelContinuousOwnerWork work,
        int logicalOwnerCount,
        int activeWorkerCount,
        in KernelExecutionContext context)
    {
        for (var workerIndex = 0;
             workerIndex < activeWorkerCount;
             workerIndex++)
        {
            _workers[workerIndex].ConfigureContinuousOwners(
                simulation,
                work,
                (int)((long)logicalOwnerCount *
                    workerIndex / activeWorkerCount),
                (int)((long)logicalOwnerCount *
                    (workerIndex + 1) / activeWorkerCount),
                context);
        }
    }

    internal bool ContinuousStop =>
        global::System.Threading.Volatile.Read(
            ref _continuousStop) != 0;

    internal void RequestContinuousStop() =>
        global::System.Threading.Volatile.Write(
            ref _continuousStop,
            1);

    private void ExecuteWorkers(int activeWorkerCount)
    {
        for (var workerIndex = 1; workerIndex < activeWorkerCount; workerIndex++)
        {
            _workers[workerIndex].Start();
        }

        global::System.Exception? coordinatorException = null;
        try
        {
            _workers[0].ExecuteConfigured();
        }
#pragma warning disable CA1031 // Coordinator must still join background workers.
        catch (global::System.Exception exception)
        {
            coordinatorException = exception;
        }
#pragma warning restore CA1031

        if (activeWorkerCount > 1)
        {
            WaitForWorkers();
        }

        try
        {
            for (var workerIndex = 1; workerIndex < activeWorkerCount; workerIndex++)
            {
                _workers[workerIndex].ThrowIfFailed();
            }

            if (coordinatorException is not null)
            {
                global::System.Runtime.ExceptionServices.ExceptionDispatchInfo
                    .Capture(coordinatorException)
                    .Throw();
            }
        }
        finally
        {
            for (var workerIndex = 0; workerIndex < activeWorkerCount; workerIndex++)
            {
                _workers[workerIndex].ReleaseConfigured();
            }
        }
    }

    internal void SignalWorkerCompleted()
    {
        if (global::System.Threading.Interlocked.Decrement(ref _remaining) == 0)
        {
            lock (_completionGate)
            {
                global::System.Threading.Monitor.PulseAll(_completionGate);
            }
        }
    }

    private void WaitForWorkers()
    {
        lock (_completionGate)
        {
            while (global::System.Threading.Volatile.Read(ref _remaining) > 0)
            {
                global::System.Threading.Monitor.Wait(_completionGate);
            }
        }
    }

    private void EnsureStarted(int activeWorkerCount)
    {
        if (_started || activeWorkerCount <= 1)
        {
            return;
        }

        for (var workerIndex = 1; workerIndex < _workers.Length; workerIndex++)
        {
            _workers[workerIndex].StartThread();
        }

        _started = true;
    }

    internal void Shutdown()
    {
        if (_shutdown)
        {
            return;
        }

        _shutdown = true;
        if (_started)
        {
            for (var workerIndex = 1; workerIndex < _workers.Length; workerIndex++)
            {
                _workers[workerIndex].Stop();
            }

            for (var workerIndex = 1; workerIndex < _workers.Length; workerIndex++)
            {
                _workers[workerIndex].Join();
            }
        }
    }
}

internal sealed class KernelWorker
{
    private readonly KernelWorkerSet _owner;
    private readonly object _startGate = new();
    private readonly global::System.Threading.Thread? _thread;
    private Simulation? _simulation;
    private IKernelContinuousOwnerWork? _continuousOwnerWork;
    private int _ownerIndex;
    private int _ownerLimit;
    private global::System.Exception? _failure;
    private bool _hasWork;
    private bool _stop;

    internal KernelWorker(
        KernelWorkerSet owner,
        int workerIndex,
        bool background)
    {
        _owner = owner;
        WorkerIndex = workerIndex;
        if (background)
        {
            _thread = new global::System.Threading.Thread(Run)
            {
                IsBackground = true,
            };
        }
    }

    internal int WorkerIndex { get; }

    internal KernelExecutionContext ExecutionContext { get; set; }

    internal int ExecutionContextDepth { get; set; }

    internal int OwnerIndex { get; set; } = -1;

    internal int OwnerTick { get; set; }

    internal bool IsContinuousOwnerEvaluation { get; set; }

    internal Simulation? Simulation => _simulation;

    internal void ConfigureContinuousOwners(
        Simulation simulation,
        IKernelContinuousOwnerWork work,
        int firstOwner,
        int ownerLimit,
        in KernelExecutionContext context)
    {
        _simulation = simulation;
        _continuousOwnerWork = work;
        _ownerIndex = firstOwner;
        _ownerLimit = ownerLimit;
        ExecutionContext = context;
        ExecutionContextDepth = 0;
        OwnerIndex = -1;
        OwnerTick = 0;
        IsContinuousOwnerEvaluation = false;
        _failure = null;
    }

    internal void StartThread() => _thread?.Start();

    internal void Start()
    {
        lock (_startGate)
        {
            _hasWork = true;
            global::System.Threading.Monitor.Pulse(_startGate);
        }
    }

    internal void ExecuteConfigured()
    {
        var simulation = _simulation ??
            throw new global::System.InvalidOperationException();
        var previousWorker = KernelWorkerThread.Enter(this);
        try
        {
            if (_continuousOwnerWork is not null)
            {
                ExecuteContinuousOwners(
                    simulation,
                    _continuousOwnerWork);
            }
            else
            {
                throw new global::System.InvalidOperationException();
            }
        }
        finally
        {
            KernelWorkerThread.Restore(previousWorker);
        }
    }

    private void ExecuteContinuousOwners(
        Simulation simulation,
        IKernelContinuousOwnerWork work)
    {
        var spin = new global::System.Threading.SpinWait();
        while (true)
        {
            if (_owner.ContinuousStop)
            {
                return;
            }

            if (work.WindowQuiescent(simulation))
            {
                _owner.RequestContinuousStop();
                return;
            }

            var progressed = false;
            for (var owner = _ownerIndex;
                 owner < _ownerLimit;
                 owner++)
            {
                progressed |= work.ExecuteOwner(
                    simulation,
                    owner);
            }

            if (progressed)
            {
                spin.Reset();
            }
            else
            {
                WaitForRunnableOwnerWork(
                    simulation,
                    work,
                    ref spin);
            }
        }
    }

    private void WaitForRunnableOwnerWork(
        Simulation simulation,
        IKernelContinuousOwnerWork work,
        ref global::System.Threading.SpinWait spin)
    {
        while (!work.WindowQuiescent(simulation) &&
               !_owner.ContinuousStop &&
               !work.HasRunnableWork(
                   simulation,
                   _ownerIndex,
                   _ownerLimit))
        {
            spin.SpinOnce();
        }

        if (work.WindowQuiescent(simulation))
        {
            _owner.RequestContinuousStop();
        }
    }

    internal void ThrowIfFailed()
    {
        if (_failure is null)
        {
            return;
        }

        global::System.Runtime.ExceptionServices.ExceptionDispatchInfo
            .Capture(_failure)
            .Throw();
    }

    internal void ReleaseConfigured()
    {
        _simulation = null;
        _continuousOwnerWork = null;
        _ownerIndex = 0;
        _ownerLimit = 0;
        ExecutionContext = default;
        ExecutionContextDepth = 0;
        OwnerIndex = -1;
        OwnerTick = 0;
        IsContinuousOwnerEvaluation = false;
    }

    internal void Stop()
    {
        lock (_startGate)
        {
            _stop = true;
            global::System.Threading.Monitor.Pulse(_startGate);
        }
    }

    internal void Join() => _thread?.Join();

    private void Run()
    {
        while (true)
        {
            lock (_startGate)
            {
                while (!_hasWork && !_stop)
                {
                    global::System.Threading.Monitor.Wait(_startGate);
                }

                if (_stop)
                {
                    return;
                }

                _hasWork = false;
            }

            try
            {
                ExecuteConfigured();
            }
#pragma warning disable CA1031 // Worker failures must cross the thread boundary.
            catch (global::System.Exception exception)
            {
                _failure = exception;
                _owner.RequestContinuousStop();
            }
#pragma warning restore CA1031
            finally
            {
                _owner.SignalWorkerCompleted();
            }
        }
    }
}

internal static class KernelWorkerThread
{
    [global::System.ThreadStatic]
    private static KernelWorker? _current;

    internal static KernelWorker? Current => _current;

    internal static int WorkerIndex => _current?.WorkerIndex ?? 0;

    internal static int OwnerIndex => _current?.OwnerIndex ?? -1;

    internal static int OwnerTick => _current?.OwnerTick ?? 0;

    internal static void SetOwnerContext(int ownerIndex, int tick)
    {
        var worker = _current ??
            throw new global::System.InvalidOperationException();
        worker.OwnerIndex = ownerIndex;
        worker.OwnerTick = tick;
    }

    internal static KernelWorker? Enter(KernelWorker worker)
    {
        var previous = _current;
        _current = worker;
        return previous;
    }

    internal static void Restore(KernelWorker? worker)
    {
        _current = worker;
    }
}
