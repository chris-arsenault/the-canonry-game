namespace Engine.Kernel;

using Engine.Constants;
using Engine.Kernel.Programs;

internal sealed partial class Simulation
{
    internal long PhaseRevision => _phaseRevision;
    internal int CurrentPhaseCode => _currentPhaseCode;
    internal bool IsCollectingDeltas =>
        _continuousOwners.IsEvaluatingCurrentThread;

    internal int FrozenEntityCount =>
        IsCollectingDeltas
            ? _frozenTick.EntityCount
            : World.ActiveEntityCount;
    internal int FrozenRelationshipCount =>
        IsCollectingDeltas
            ? _frozenTick.RelationshipCount
            : World.ActiveRelationshipCount;

    internal int FrozenEntityAt(int index) =>
        IsCollectingDeltas
            ? _frozenTick.EntityAt(index, Options.Execution.ReverseEvaluationOrder)
            : World.ActiveEntityAt(index);

    internal int FrozenRelationshipAt(int index) =>
        IsCollectingDeltas
            ? _frozenTick.RelationshipAt(index, Options.Execution.ReverseEvaluationOrder)
            : World.ActiveRelationshipAt(index);

    internal bool TryQueueMovement(
        int entityId,
        int planeCode,
        int q,
        int r,
        int z,
        int causeCode)
    {
        if (!IsCollectingDeltas)
        {
            return false;
        }

        var occurrence = World.EntityOccurrenceHandleForIntent(entityId);
        var delta = new MovementDelta(
            occurrence,
            planeCode,
            q,
            r,
            z,
            causeCode,
            ExecutionContext);
        _continuousOwners.Enqueue(delta);
        return true;
    }

    internal void QueueLifecycleRetirement(int entityId)
    {
        if (!IsCollectingDeltas)
        {
            SetEntityStatus(
                entityId,
                EntityStatusCodes.Historical,
                ExecutionTick);
            return;
        }

        var occurrence = World.EntityOccurrenceHandleForIntent(entityId);
        var delta = new LifecycleDelta(
            occurrence,
            0,
            0,
            ExecutionContext);
        _continuousOwners.Enqueue(delta);
    }

    internal void QueueLifecycleRetirementWhenAttributeAtMost(
        int entityId,
        int attributeId,
        FixedScalar threshold)
    {
        if (!IsCollectingDeltas)
        {
            if (World.GetAttribute(entityId, attributeId) <= threshold)
            {
                SetEntityStatus(
                    entityId,
                    EntityStatusCodes.Historical,
                    ExecutionTick);
            }

            return;
        }

        var occurrence = World.EntityOccurrenceHandleForIntent(entityId);
        var delta = new LifecycleDelta(
            occurrence,
            attributeId,
            threshold.Units,
            ExecutionContext);
        _continuousOwners.Enqueue(delta);
    }

    internal void QueueRelationshipEndWhenAtMost(
        int relationshipId,
        FixedScalar threshold)
    {
        if (!IsCollectingDeltas)
        {
            if (World.RelationshipStrength(relationshipId) <= threshold)
            {
                CullRelationship(relationshipId, ExecutionTick);
            }

            return;
        }

        var occurrence = World.RelationshipOccurrenceHandle(relationshipId);
        var delta = new RelationshipEndDelta(
            occurrence,
            KernelFixedPoint.RelationshipStrengthUnits(threshold),
            Unconditional: false,
            ExecutionContext);
        _continuousOwners.Enqueue(delta);
    }

    internal global::System.Span<int> DirtyCultureScratch =>
        _dirtyCultureScratchByWorker[
            _singleEvaluationWorker ? 0 : KernelWorkerThread.WorkerIndex];

    internal int RandomInt(
        int exclusiveMaximum,
        long logicalKey,
        int ruleId,
        int purposeCode,
        int attempt) =>
        _random.Int(
            exclusiveMaximum,
            ExecutionTick,
            ExecutionPhaseCode,
            ExecutionContext.ProgramId,
            logicalKey,
            ruleId,
            purposeCode,
            attempt);

    internal int RandomInclusive(
        int minimum,
        int maximum,
        long logicalKey,
        int ruleId,
        int purposeCode,
        int attempt) =>
        _random.Inclusive(
            minimum,
            maximum,
            ExecutionTick,
            ExecutionPhaseCode,
            ExecutionContext.ProgramId,
            logicalKey,
            ruleId,
            purposeCode,
            attempt);

    internal FixedScalar RandomScalar(
        long logicalKey,
        int ruleId,
        int purposeCode,
        int attempt) =>
        _random.Scalar(
            ExecutionTick,
            ExecutionPhaseCode,
            ExecutionContext.ProgramId,
            logicalKey,
            ruleId,
            purposeCode,
            attempt);

    private static int PhaseCodeForProgram(int programId) => programId switch
    {
        KernelProgramCodes.Movement => TickPhaseCodes.PrepareLocalView,
        _ => TickPhaseCodes.EvaluateLocal,
    };

    private static void ValidateExecutionOptions(KernelExecutionOptions options)
    {
        global::System.ArgumentNullException.ThrowIfNull(options);
        NumericGuard.ThrowIfNegativeOrZero(options.WorkerCount);
        NumericGuard.ThrowIfNegativeOrZero(options.OwnerPartitionCount);
        NumericGuard.ThrowIfNegativeOrZero(options.OwnerLeadTicks);
    }
}
