namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class Simulation
{
    internal int RegisterSystem(int cadence, ISimulationSystem system)
    {
        NumericGuard.ThrowIfNegativeOrZero(cadence);
        global::System.ArgumentNullException.ThrowIfNull(system);

        var slot = FindSystemSlot();
        _systemCadences[slot] = cadence;
        _systemProgramIds[slot] = 0;
        _systemSourceCodeIds[slot] = 0;
        _systemPhaseCodes[slot] = TickPhaseCodes.EvaluateLocal;
        _systems[slot] = system;
        _systemActive[slot] = true;
        if (slot >= _systemCount)
        {
            _systemCount = slot + 1;
        }

        return slot;
    }

    internal int RegisterProgram(
        int programId,
        int cadence,
        ISimulationSystem system)
    {
        NumericGuard.ThrowIfNegativeOrZero(programId);
        var sourceCodeId = SourceCodeIdForProgram(programId);
        if (sourceCodeId <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        var handle = RegisterSystem(cadence, system);
        _systemProgramIds[handle] = programId;
        _systemSourceCodeIds[handle] = sourceCodeId;
        _systemPhaseCodes[handle] = PhaseCodeForProgram(programId);
        _programs.Register(programId, handle);
        return handle;
    }

    internal KernelExecutionScope EnterExecutionContext(
        int sourceCodeId,
        int programId,
        int actionId = 0)
    {
        NumericGuard.ThrowIfNegative(sourceCodeId);
        NumericGuard.ThrowIfNegative(programId);
        NumericGuard.ThrowIfNegative(actionId);
        var previous = ExecutionContext;
        var previousDepth = ExecutionContextDepth();
        SetExecutionContext(
            new KernelExecutionContext(programId, sourceCodeId, actionId),
            previousDepth + 1);
        return new KernelExecutionScope(this, previous, previousDepth);
    }

    internal KernelExecutionScope EnterExecutionContext(in KernelExecutionContext context)
    {
        var previous = ExecutionContext;
        var previousDepth = ExecutionContextDepth();
        SetExecutionContext(context, previousDepth + 1);
        return new KernelExecutionScope(this, previous, previousDepth);
    }

    internal KernelExecutionScope EnterActionExecutionContext(int actionId)
    {
        NumericGuard.ThrowIfNegativeOrZero(actionId);
        var previous = ExecutionContext;
        var previousDepth = ExecutionContextDepth();
        SetExecutionContext(
            previous with { ActionId = actionId },
            previousDepth + 1);
        return new KernelExecutionScope(this, previous, previousDepth);
    }

    internal KernelProvenanceScope EnterProvenanceInvocation(
        int producerKindCode,
        int producerId,
        int primaryEntityId,
        int secondaryEntityId = 0,
        int triggerKindCode = ProvenanceTriggerKindCodes.None,
        int triggerId = 0,
        int parentInvocationId = -1)
    {
        NumericGuard.ThrowIfLessThan(parentInvocationId, -1);

        var previous = ExecutionContext;
        var previousDepth = ExecutionContextDepth();
        var resolvedParentInvocationId = parentInvocationId >= 0
            ? parentInvocationId
            : previous.ProvenanceInvocationId;
        var invocationId = World.BeginProvenanceInvocation(
            ExecutionTick,
            producerKindCode,
            producerId,
            primaryEntityId,
            secondaryEntityId,
            triggerKindCode,
            triggerId,
            resolvedParentInvocationId,
            previous);
        SetExecutionContext(
            previous with { ProvenanceInvocationId = invocationId },
            previousDepth + 1);
        return new KernelProvenanceScope(this, previous, previousDepth, invocationId);
    }

    internal void CompleteProvenanceInvocation(int invocationId)
    {
        var record = World.CompleteProvenanceInvocation(invocationId);
        EmitProvenanceInvocationRecorded(
            record.Tick,
            record.InvocationId,
            record.ProducerKindCode,
            record.ProducerId,
            record.PrimaryEntityId,
            record.SecondaryEntityId,
            record.ParentInvocationId,
            record.TriggerKindCode,
            record.TriggerId,
            record.OutputCount);
    }

    internal void RestoreExecutionContext(in KernelExecutionContext context, int depth)
    {
        SetExecutionContext(context, depth);
    }

    private int ExecutionContextDepth() =>
        CurrentKernelWorker()?.ExecutionContextDepth ?? _executionContextDepth;

    private void SetExecutionContext(
        in KernelExecutionContext context,
        int depth)
    {
        var worker = CurrentKernelWorker();
        if (worker is not null)
        {
            worker.ExecutionContext = context;
            worker.ExecutionContextDepth = depth;
            return;
        }

        _executionContext = context;
        _executionContextDepth = depth;
    }

    private KernelWorker? CurrentKernelWorker()
    {
        if (_singleEvaluationWorker)
        {
            return null;
        }

        var worker = KernelWorkerThread.Current;
        return global::System.Object.ReferenceEquals(worker?.Simulation, this)
            ? worker
            : null;
    }

    internal void SetSystemActive(int handle, bool active)
    {
        if ((uint)handle >= (uint)_systemActive.Length || _systems[handle] is null)
        {
            throw new global::System.InvalidOperationException();
        }

        _systemActive[handle] = active;
    }

    internal void SetProgramEnabled(int programId, bool enabled)
    {
        _programs.SetEnabled(this, programId, enabled);
    }

    internal bool ProgramIsEnabled(int programId)
    {
        return _programs.IsEnabled(programId);
    }

    internal void UnregisterSystem(int handle)
    {
        if ((uint)handle >= (uint)_systemActive.Length)
        {
            return;
        }

        _systemActive[handle] = false;
        _systems[handle] = null;
    }
}
