namespace Engine.Kernel;

using Engine.Constants;
using Engine.Kernel.Programs;

internal sealed partial class Simulation
{
    internal long PhaseRevision => _phaseRevision;
    internal int CurrentPhaseCode => _currentPhaseCode;
    internal bool IsCollectingDeltas => _collectingDeltas;

    internal int FrozenEntityCount =>
        _collectingDeltas ? _frozenTick.EntityCount : World.ActiveEntityCount;
    internal int FrozenRelationshipCount =>
        _collectingDeltas ? _frozenTick.RelationshipCount : World.ActiveRelationshipCount;

    internal int FrozenEntityAt(int index) =>
        _collectingDeltas
            ? _frozenTick.EntityAt(index, Options.Execution.ReverseEvaluationOrder)
            : World.ActiveEntityAt(index);

    internal int FrozenRelationshipAt(int index) =>
        _collectingDeltas
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
        if (!_collectingDeltas)
        {
            return false;
        }

        var occurrence = World.EntityOccurrenceHandle(entityId);
        _tickDeltas.AddMovement(new MovementDelta(
            occurrence,
            planeCode,
            q,
            r,
            z,
            causeCode,
            MovementPriority(causeCode),
            DeltaSourceKey(occurrence, planeCode),
            ExecutionContext));
        return true;
    }

    internal void QueueLifecycleRetirement(int entityId)
    {
        if (!_collectingDeltas)
        {
            SetEntityStatus(entityId, EntityStatusCodes.Historical, CurrentTick);
            return;
        }

        var occurrence = World.EntityOccurrenceHandle(entityId);
        _tickDeltas.AddLifecycle(new LifecycleDelta(
            occurrence,
            0,
            0,
            DeltaSourceKey(occurrence, 0),
            ExecutionContext));
    }

    internal void QueueLifecycleRetirementWhenAttributeAtMost(
        int entityId,
        int attributeId,
        FixedScalar threshold)
    {
        if (!_collectingDeltas)
        {
            if (World.GetAttribute(entityId, attributeId) <= threshold)
            {
                SetEntityStatus(entityId, EntityStatusCodes.Historical, CurrentTick);
            }

            return;
        }

        var occurrence = World.EntityOccurrenceHandle(entityId);
        _tickDeltas.AddLifecycle(new LifecycleDelta(
            occurrence,
            attributeId,
            threshold.Units,
            DeltaSourceKey(occurrence, attributeId),
            ExecutionContext));
    }

    internal void QueueRelationshipEndWhenAtMost(
        int relationshipId,
        FixedScalar threshold)
    {
        if (!_collectingDeltas)
        {
            if (World.RelationshipStrength(relationshipId) <= threshold)
            {
                CullRelationship(relationshipId, CurrentTick);
            }

            return;
        }

        var occurrence = World.RelationshipOccurrenceHandle(relationshipId);
        _tickDeltas.AddRelationshipEnd(new RelationshipEndDelta(
            occurrence,
            KernelFixedPoint.RelationshipStrengthUnits(threshold),
            Unconditional: false,
            DeltaSourceKey(occurrence, 0),
            ExecutionContext));
    }

    internal global::System.Span<int> DirtyCultureScratch =>
        _dirtyCultureScratchByWorker[
            _singleEvaluationWorker ? 0 : KernelWorkerThread.WorkerIndex];

    internal long DeltaSourceKey(int occurrenceHandle, int discriminator)
    {
        var context = ExecutionContext;
        unchecked
        {
            var high = (uint)occurrenceHandle;
            var low = (uint)context.ProgramId * 2654435761u;
            low ^= (uint)context.SourceCodeId * 2246822519u;
            low ^= (uint)context.ActionId * 3266489917u;
            low ^= (uint)context.ProvenanceInvocationId * 668265263u;
            low ^= (uint)discriminator * 374761393u;
            low ^= low >> 16;
            return ((long)high << 32) | low;
        }
    }

    private static int MovementPriority(int causeCode) => causeCode switch
    {
        MovementCauseCodes.Forced or MovementCauseCodes.Relocation => 3,
        MovementCauseCodes.Placement or MovementCauseCodes.Follow => 2,
        _ => 1,
    };

    private void AdvanceOneTick()
    {
        CurrentTick++;
        World.BeginTick(CurrentTick);

        RunAdmissionAndPreparePhases();
        RunLocalEpoch();
        RunStrategicAndPublishPhases();
    }

    private void RunAdmissionAndPreparePhases()
    {
        BeginPublishedPhase(TickPhaseCodes.CommandAdmission);
        DrainCommands();
        CompletePublishedPhase(TickPhaseCodes.CommandAdmission);

        BeginPublishedPhase(TickPhaseCodes.PrepareLocalView);
        TickSystems(TickPhaseCodes.PrepareLocalView);
        CompletePublishedPhase(TickPhaseCodes.PrepareLocalView);
    }

    private void RunLocalEpoch()
    {
        _frozenTick.Capture(World, CurrentTick);
        _currentPhaseCode = TickPhaseCodes.EvaluateLocal;
        _phaseRevision++;
        var evaluationPhaseRevision = _phaseRevision;
        _tickDeltas.Clear();
        _collectingDeltas = true;
        _eventPublisher.BeginPhase(TickPhaseCodes.EvaluateLocal);
        TickSystems(TickPhaseCodes.EvaluateLocal);
        _collectingDeltas = false;
        if (WorldVersion != _frozenTick.WorldVersion)
        {
            throw new global::System.InvalidOperationException();
        }

        _currentPhaseCode = TickPhaseCodes.ResolveLocal;
        _phaseRevision++;
        var resolutionPhaseRevision = _phaseRevision;
        var deltaCount = _tickDeltas.Count;
        _tickDeltas.Resolve(
            this,
            Options.Execution.ReverseDeltaInsertionOrder,
            Options.Execution.LogicalShardCount);
        _eventPublisher.PublishPhase();
        WritePhaseCompleted(
            TickPhaseCodes.EvaluateLocal,
            _frozenTick.EntityCount,
            deltaCount,
            evaluationPhaseRevision);
        WritePhaseCompleted(
            TickPhaseCodes.ResolveLocal,
            deltaCount,
            WorldVersion - _frozenTick.WorldVersion,
            resolutionPhaseRevision);
    }

    private void RunStrategicAndPublishPhases()
    {
        BeginPublishedPhase(TickPhaseCodes.Strategic);
        TickSystems(TickPhaseCodes.Strategic);
        CompletePublishedPhase(TickPhaseCodes.Strategic);

        BeginPublishedPhase(TickPhaseCodes.Publish);
        WriteTickCompleted();
        CompletePublishedPhase(TickPhaseCodes.Publish);
    }

    internal int RandomInt(
        int exclusiveMaximum,
        long logicalKey,
        int ruleId,
        int purposeCode,
        int attempt) =>
        _random.Int(
            exclusiveMaximum,
            CurrentTick,
            _currentPhaseCode,
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
            CurrentTick,
            _currentPhaseCode,
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
            CurrentTick,
            _currentPhaseCode,
            ExecutionContext.ProgramId,
            logicalKey,
            ruleId,
            purposeCode,
            attempt);

    private void TickSystems(int phaseCode)
    {
        for (var i = 0; i < _systemCount; i++)
        {
            if (!_systemActive[i] ||
                _systemPhaseCodes[i] != phaseCode ||
                CurrentTick % _systemCadences[i] != 0)
            {
                continue;
            }

            TickSystem(i);
        }
    }

    private void TickSystem(int systemIndex)
    {
        var system = _systems[systemIndex] ??
            throw new global::System.InvalidOperationException();
        if (_systemSourceCodeIds[systemIndex] <= 0)
        {
            TickSystemRuntime(system);
            return;
        }

        using var scope = EnterExecutionContext(
            _systemSourceCodeIds[systemIndex],
            _systemProgramIds[systemIndex]);
        TickSystemRuntime(system);
    }

    private void TickSystemRuntime(ISimulationSystem system)
    {
        if (_collectingDeltas &&
            system is ILocalEvaluationRangeProgram rangeProgram)
        {
            _workers.Execute(
                this,
                rangeProgram,
                _frozenTick,
                CurrentTick,
                Options.Execution.ReverseEvaluationOrder,
                ExecutionContext);
            return;
        }

        system.Tick(this, CurrentTick);
    }

    internal void ExecuteOwnerWork(IKernelOwnerWork work, int workItemCount)
    {
        _workers.ExecuteOwners(
            this,
            work,
            workItemCount,
            ExecutionContext);
    }

    private void BeginPublishedPhase(int phaseCode)
    {
        _currentPhaseCode = phaseCode;
        _phaseRevision++;
        _eventPublisher.BeginPhase(phaseCode);
    }

    private void CompletePublishedPhase(int phaseCode)
    {
        var eventCount = _eventPublisher.BufferedCount;
        _eventPublisher.PublishPhase();
        WritePhaseCompleted(phaseCode, eventCount, 0, _phaseRevision);
    }

    private static int PhaseCodeForProgram(int programId) => programId switch
    {
        KernelProgramCodes.Movement => TickPhaseCodes.PrepareLocalView,
        KernelProgramCodes.LowAltitudeVerbs or
        KernelProgramCodes.CultureFieldDecay or
        KernelProgramCodes.Relationships or
        KernelProgramCodes.Effects or
        KernelProgramCodes.MinorActors => TickPhaseCodes.EvaluateLocal,
        _ => TickPhaseCodes.Strategic,
    };

    private static void ValidateExecutionOptions(KernelExecutionOptions options)
    {
        global::System.ArgumentNullException.ThrowIfNull(options);
        NumericGuard.ThrowIfNegativeOrZero(options.WorkerCount);
        NumericGuard.ThrowIfNegativeOrZero(options.LogicalShardCount);
    }
}
