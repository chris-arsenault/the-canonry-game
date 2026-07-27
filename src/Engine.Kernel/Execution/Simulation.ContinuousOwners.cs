namespace Engine.Kernel;

internal sealed partial class Simulation
{
    internal int ExecutionTick =>
        KernelWorkerThread.OwnerIndex >= 0
            ? KernelWorkerThread.OwnerTick
            : CurrentTick;

    internal bool TryQueueContinuousRoadWork(
        int planeCode,
        int q,
        int r,
        FixedScalar amount,
        int tick)
    {
        if (!_continuousOwners.IsEvaluatingCurrentThread)
        {
            return false;
        }

        _continuousOwners.EnqueueRoadWork(
            tick,
            planeCode,
            q,
            r,
            amount,
            ExecutionContext);
        return true;
    }

    private int ExecutionPhaseCode =>
        KernelWorkerThread.OwnerIndex >= 0
            ? TickPhaseCodes.EvaluateLocal
            : _currentPhaseCode;

    internal void EvaluateContinuousLocalOwner(
        int ownerIndex,
        int tick)
    {
        for (var systemIndex = 0;
             systemIndex < _systemCount;
             systemIndex++)
        {
            if (!_systemActive[systemIndex] ||
                _systemPhaseCodes[systemIndex] !=
                    TickPhaseCodes.EvaluateLocal ||
                tick % _systemCadences[systemIndex] != 0)
            {
                continue;
            }

            var system = _systems[systemIndex] ??
                throw new global::System.InvalidOperationException();
            var programId = _systemProgramIds[systemIndex] > 0
                ? _systemProgramIds[systemIndex]
                : checked(_systems.Length + systemIndex + 1);
            if (_systemSourceCodeIds[systemIndex] <= 0)
            {
                EvaluateContinuousSystem(
                    system,
                    programId,
                    ownerIndex,
                    tick);
                continue;
            }

            using var scope = EnterExecutionContext(
                _systemSourceCodeIds[systemIndex],
                _systemProgramIds[systemIndex]);
            EvaluateContinuousSystem(
                system,
                programId,
                ownerIndex,
                tick);
        }
    }

    internal int FirstPublishedEntityInPhysicalSuperregion(
        int superregionId) =>
        _frozenTick.FirstEntityInPhysicalSuperregion(superregionId);

    internal int NextPublishedEntityInPhysicalSuperregion(
        int entityId) =>
        _frozenTick.NextEntityInPhysicalSuperregion(entityId);

    internal int FirstPublishedEntityOfKind(int kindId) =>
        _frozenTick.FirstEntityOfKind(kindId);

    internal int NextPublishedEntityOfKind(int entityId) =>
        _frozenTick.NextEntityOfKind(entityId);

    internal bool PublishedCellIsOccupied(
        int planeCode,
        int q,
        int r) =>
        _frozenTick.CellIsOccupied(World, planeCode, q, r);

    private void EvaluateContinuousSystem(
        ISimulationSystem system,
        int programId,
        int ownerIndex,
        int tick)
    {
        var programOwner = _ownerTopology.ProgramOwner(
            programId > 0 ? programId : ownerIndex + 1);
        if (EvaluateDedicatedContinuousSystem(
                system,
                programOwner,
                ownerIndex,
                tick))
        {
            return;
        }

        if (system is ILocalEvaluationRangeProgram rangeProgram)
        {
            EvaluateContinuousRange(
                system,
                rangeProgram,
                programOwner,
                ownerIndex,
                tick);
            return;
        }

        if (ownerIndex == programOwner)
        {
            system.Tick(this, tick);
        }
    }

    private bool EvaluateDedicatedContinuousSystem(
        ISimulationSystem system,
        int programOwner,
        int ownerIndex,
        int tick)
    {
        if (global::System.Object.ReferenceEquals(system, MinorActors))
        {
            if (ownerIndex == programOwner)
            {
                MinorActors.Tick(this, tick);
            }

            return true;
        }

        if (system is not IContinuousCellProgram cellProgram)
        {
            return false;
        }

        cellProgram.EvaluateOwner(
            this,
            tick,
            _frozenTick.CultureOwnerRange(ownerIndex));
        return true;
    }

    private void EvaluateContinuousRange(
        ISimulationSystem system,
        ILocalEvaluationRangeProgram rangeProgram,
        int programOwner,
        int ownerIndex,
        int tick)
    {
        if (rangeProgram.SupportsConcurrentRanges(this))
        {
            rangeProgram.EvaluateRange(
                this,
                tick,
                _frozenTick.OwnerRange(
                    rangeProgram.RangeKindCode,
                    ownerIndex,
                    Options.Execution.ReverseEvaluationOrder));
        }
        else if (ownerIndex == programOwner)
        {
            rangeProgram.EvaluateRange(
                this,
                tick,
                _frozenTick.WorkerRange(
                    rangeProgram.RangeKindCode,
                    workerIndex: 0,
                    workerCount: 1,
                    Options.Execution.ReverseEvaluationOrder));
        }

        if (ownerIndex == programOwner &&
            global::System.Object.ReferenceEquals(system, Effects))
        {
            rangeProgram.CompleteRanges(this, tick);
        }
    }

    bool IKernelContinuousOwnerWork.ExecuteOwner(
        Simulation simulation,
        int ownerIndex)
    {
        if (!global::System.Object.ReferenceEquals(simulation, this))
        {
            throw new global::System.InvalidOperationException();
        }

        return _continuousOwners.ExecuteOwner(ownerIndex);
    }

    bool IKernelContinuousOwnerWork.WindowQuiescent(
        Simulation simulation)
    {
        if (!global::System.Object.ReferenceEquals(simulation, this))
        {
            throw new global::System.InvalidOperationException();
        }

        return _continuousOwners.WindowQuiescent();
    }

    bool IKernelContinuousOwnerWork.HasRunnableWork(
        Simulation simulation,
        int firstOwner,
        int ownerLimit)
    {
        if (!global::System.Object.ReferenceEquals(simulation, this))
        {
            throw new global::System.InvalidOperationException();
        }

        return _continuousOwners.HasRunnableWork(
            firstOwner,
            ownerLimit);
    }

    private void AdvanceContinuousWindow(
        int windowTicks)
    {
        NumericGuard.ThrowIfNegativeOrZero(windowTicks);
        var startTick = checked(CurrentTick + 1);
        var targetTick = checked(CurrentTick + windowTicks);

        PrepareContinuousLocalWindow(startTick, targetTick);
        Gambits.CapturePublishedView();
        CultureSlots.CapturePublishedView();
        World.BeginPublishedRelationshipView();
        try
        {
            _frozenTick.Capture(World, CurrentTick);
            _continuousOwners.BeginWindow(CurrentTick, targetTick);
            RouteCommandsToOwners();
            _eventPublisher.BeginContinuous();
            try
            {
                _workers.ExecuteContinuousOwners(
                    this,
                    this,
                    _ownerTopology.PartitionCount,
                    ExecutionContext);
                _continuousOwners.CompleteWindow();
            }
            finally
            {
                _eventPublisher.EndContinuous();
            }
        }
        finally
        {
            World.EndPublishedRelationshipView();
        }

        PublishContinuousProgress(startTick, targetTick);
        WriteWatermarkAdvanced(
            targetTick,
            PublishedHighWatermark,
            _ownerTopology.PartitionCount,
            _phaseRevision);
        global::System.Threading.Volatile.Write(
            ref _publishedLowWatermark,
            targetTick);
    }

    private void PrepareContinuousLocalWindow(
        int startTick,
        int targetTick)
    {
        for (var tick = startTick; tick <= targetTick; tick++)
        {
            for (var systemIndex = 0;
                 systemIndex < _systemCount;
                 systemIndex++)
            {
                if (!_systemActive[systemIndex] ||
                    _systemPhaseCodes[systemIndex] !=
                        TickPhaseCodes.PrepareLocalView ||
                    tick % _systemCadences[systemIndex] != 0)
                {
                    continue;
                }

                using var scope = EnterExecutionContext(
                    _systemSourceCodeIds[systemIndex],
                    _systemProgramIds[systemIndex]);
                (_systems[systemIndex] ??
                    throw new global::System.InvalidOperationException())
                    .Tick(this, tick);
            }
        }
    }

    private void PublishContinuousProgress(
        int startTick,
        int targetTick)
    {
        for (var tick = startTick; tick <= targetTick; tick++)
        {
            CurrentTick = tick;
            World.BeginTick(tick);
            WriteContinuousLocalPhases();
            _currentPhaseCode = TickPhaseCodes.Publish;
            WriteTickCompleted();
            WritePhaseCompleted(
                TickPhaseCodes.Publish,
                1,
                0,
                ++_phaseRevision);
        }
    }

    private void WriteContinuousLocalPhases()
    {
        _currentPhaseCode = TickPhaseCodes.CommandAdmission;
        WritePhaseCompleted(
            TickPhaseCodes.CommandAdmission,
            0,
            0,
            ++_phaseRevision);
        _currentPhaseCode = TickPhaseCodes.PrepareLocalView;
        WritePhaseCompleted(
            TickPhaseCodes.PrepareLocalView,
            0,
            0,
            ++_phaseRevision);
        _currentPhaseCode = TickPhaseCodes.EvaluateLocal;
        WritePhaseCompleted(
            TickPhaseCodes.EvaluateLocal,
            _frozenTick.EntityCount +
            _frozenTick.RelationshipCount +
            _frozenTick.CultureInfluenceCount,
            0,
            ++_phaseRevision);
        _currentPhaseCode = TickPhaseCodes.ResolveLocal;
        WritePhaseCompleted(
            TickPhaseCodes.ResolveLocal,
            0,
            0,
            ++_phaseRevision);
    }
}
