namespace Engine.Kernel;

internal interface IKernelOwnerWork
{
    void ExecuteOwner(Simulation simulation, int ownerIndex, int ownerCount);
}

internal sealed class KernelWorkerSet
{
    private readonly KernelWorker[] _workers;
    private readonly object _completionGate = new();
    private int _remaining;
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

    internal void Execute(
        Simulation simulation,
        ILocalEvaluationRangeProgram program,
        FrozenTickView frozenTick,
        int tick,
        bool reverse,
        in KernelExecutionContext context)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        global::System.ArgumentNullException.ThrowIfNull(program);
        global::System.ArgumentNullException.ThrowIfNull(frozenTick);
        global::System.ObjectDisposedException.ThrowIf(_shutdown, this);

        var totalCount = frozenTick.CountFor(program.RangeKindCode);
        if (totalCount == 0)
        {
            program.CompleteRanges(simulation, tick);
            return;
        }

        if (WorkerCount == 1)
        {
            program.EvaluateRange(
                simulation,
                tick,
                frozenTick.Range(
                    program.RangeKindCode,
                    start: 0,
                    totalCount,
                    reverse));
            program.CompleteRanges(simulation, tick);
            return;
        }

        var activeWorkerCount = program.SupportsConcurrentRanges(simulation)
            ? global::System.Math.Min(WorkerCount, totalCount)
            : 1;
        EnsureStarted(activeWorkerCount);
        ConfigureWorkers(
            simulation,
            program,
            frozenTick,
            tick,
            reverse,
            context,
            totalCount,
            activeWorkerCount);
        ExecuteWorkers(activeWorkerCount);
        program.CompleteRanges(simulation, tick);
    }

    internal void ExecuteOwners(
        Simulation simulation,
        IKernelOwnerWork work,
        int workItemCount,
        in KernelExecutionContext context)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        global::System.ArgumentNullException.ThrowIfNull(work);
        global::System.ObjectDisposedException.ThrowIf(_shutdown, this);
        if (workItemCount <= 0)
        {
            return;
        }

        var activeWorkerCount = global::System.Math.Min(
            WorkerCount,
            workItemCount);
        if (WorkerCount == 1)
        {
            work.ExecuteOwner(simulation, ownerIndex: 0, ownerCount: 1);
            return;
        }

        EnsureStarted(activeWorkerCount);
        global::System.Threading.Volatile.Write(
            ref _remaining,
            activeWorkerCount - 1);
        for (var workerIndex = 0; workerIndex < activeWorkerCount; workerIndex++)
        {
            _workers[workerIndex].ConfigureOwner(
                simulation,
                work,
                workerIndex,
                activeWorkerCount,
                context);
        }

        ExecuteWorkers(activeWorkerCount);
    }

    private void ConfigureWorkers(
        Simulation simulation,
        ILocalEvaluationRangeProgram program,
        FrozenTickView frozenTick,
        int tick,
        bool reverse,
        in KernelExecutionContext context,
        int totalCount,
        int activeWorkerCount)
    {
        global::System.Threading.Volatile.Write(
            ref _remaining,
            activeWorkerCount - 1);
        for (var workerIndex = 0; workerIndex < activeWorkerCount; workerIndex++)
        {
            var start = (int)((long)totalCount * workerIndex / activeWorkerCount);
            var end = (int)((long)totalCount * (workerIndex + 1) / activeWorkerCount);
            _workers[workerIndex].Configure(
                simulation,
                program,
                frozenTick.Range(
                    program.RangeKindCode,
                    start,
                    end - start,
                    reverse),
                tick,
                context);
        }
    }

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
    private ILocalEvaluationRangeProgram? _program;
    private IKernelOwnerWork? _ownerWork;
    private FrozenTickRange _range;
    private int _ownerIndex;
    private int _ownerCount;
    private int _tick;
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

    internal Simulation? Simulation => _simulation;

    internal void Configure(
        Simulation simulation,
        ILocalEvaluationRangeProgram program,
        in FrozenTickRange range,
        int tick,
        in KernelExecutionContext context)
    {
        _simulation = simulation;
        _program = program;
        _ownerWork = null;
        _range = range;
        _ownerIndex = 0;
        _ownerCount = 0;
        _tick = tick;
        ExecutionContext = context;
        ExecutionContextDepth = 0;
        _failure = null;
    }

    internal void ConfigureOwner(
        Simulation simulation,
        IKernelOwnerWork work,
        int ownerIndex,
        int ownerCount,
        in KernelExecutionContext context)
    {
        _simulation = simulation;
        _program = null;
        _ownerWork = work;
        _range = default;
        _ownerIndex = ownerIndex;
        _ownerCount = ownerCount;
        _tick = 0;
        ExecutionContext = context;
        ExecutionContextDepth = 0;
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
            if (_program is not null)
            {
                _program.EvaluateRange(simulation, _tick, _range);
            }
            else if (_ownerWork is not null)
            {
                _ownerWork.ExecuteOwner(simulation, _ownerIndex, _ownerCount);
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
        _program = null;
        _ownerWork = null;
        _range = default;
        _tick = 0;
        _ownerIndex = 0;
        _ownerCount = 0;
        ExecutionContext = default;
        ExecutionContextDepth = 0;
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
