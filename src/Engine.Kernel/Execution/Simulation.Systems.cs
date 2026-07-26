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
        _systemPhaseCodes[slot] = TickPhaseCodes.Strategic;
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
        var previous = _executionContext;
        var previousDepth = _executionContextDepth;
        _executionContext = new KernelExecutionContext(programId, sourceCodeId, actionId);
        _executionContextDepth = previousDepth + 1;
        return new KernelExecutionScope(this, previous, previousDepth);
    }

    internal KernelExecutionScope EnterExecutionContext(in KernelExecutionContext context)
    {
        var previous = _executionContext;
        var previousDepth = _executionContextDepth;
        _executionContext = context;
        _executionContextDepth = previousDepth + 1;
        return new KernelExecutionScope(this, previous, previousDepth);
    }

    internal KernelExecutionScope EnterActionExecutionContext(int actionId)
    {
        NumericGuard.ThrowIfNegativeOrZero(actionId);
        var previous = _executionContext;
        var previousDepth = _executionContextDepth;
        _executionContext = previous with { ActionId = actionId };
        _executionContextDepth = previousDepth + 1;
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

        var previous = _executionContext;
        var previousDepth = _executionContextDepth;
        var resolvedParentInvocationId = parentInvocationId >= 0
            ? parentInvocationId
            : previous.ProvenanceInvocationId;
        var invocationId = World.BeginProvenanceInvocation(
            CurrentTick,
            producerKindCode,
            producerId,
            primaryEntityId,
            secondaryEntityId,
            triggerKindCode,
            triggerId,
            resolvedParentInvocationId,
            previous);
        _executionContext = previous with { ProvenanceInvocationId = invocationId };
        _executionContextDepth = previousDepth + 1;
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
        _executionContext = context;
        _executionContextDepth = depth;
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
