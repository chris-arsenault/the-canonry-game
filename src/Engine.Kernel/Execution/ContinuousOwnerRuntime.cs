namespace Engine.Kernel;

/// <summary>
/// Runs fixed logical owners through boundedly-ahead local ticks. Mutable
/// values are consumed by their destination owner. Structural projections are
/// ordinary messages consumed by the structural-index owner.
/// </summary>
internal sealed partial class ContinuousOwnerRuntime
{
    private readonly Simulation _simulation;
    private readonly KernelOwnerTopology _topology;
    private readonly int[] _localTicks;
    private readonly int[] _publishedTicks;
    private readonly long[] _publishedRevisions;
    private readonly ContinuousOwnerInbox<MovementDelta> _movements;
    private readonly ContinuousOwnerInbox<AttributeDelta> _attributes;
    private readonly ContinuousOwnerInbox<CultureDelta> _culture;
    private readonly ContinuousOwnerInbox<CultureReductionDelta>
        _cultureReductions;
    private readonly ContinuousOwnerInbox<RelationshipDelta> _relationships;
    private readonly ContinuousOwnerInbox<RelationshipEndDelta>
        _relationshipEnds;
    private readonly ContinuousOwnerInbox<TagDelta> _tags;
    private readonly ContinuousOwnerInbox<CellStateDelta> _cellStates;
    private readonly ContinuousOwnerInbox<CellStateDelta>
        _cellStatePreparations;
    private readonly ContinuousOwnerInbox<CultureDelta>
        _culturePreparations;
    private readonly ContinuousOwnerInbox<LifecycleDelta> _lifecycles;
    private readonly ContinuousOwnerInbox<InfluenceCreditDelta>
        _influenceCredits;
    private readonly ContinuousOwnerInbox<ContinuousRoadWork> _roadWork;
    private readonly ContinuousOwnerInbox<KernelCommand> _commands;
    private readonly ContinuousOwnerInbox<ContinuousResolvedMovement>
        _movementStructures;
    private readonly ContinuousOwnerInbox<ContinuousCellReconciliation>
        _cellStructures;
    private readonly ContinuousOwnerInbox<CultureStorageKey>
        _cultureStructures;
    private readonly ContinuousOwnerInbox<RelationshipStructuralProjection>
        _relationshipRetirements;
    private readonly ContinuousOwnerInbox<EntityStructuralProjection>
        _entityStructures;
    private readonly ContinuousOwnerInbox<ContinuousRoadWork>
        _roadStructures;
    private readonly ContinuousOwnerInbox<RelationshipFollowProjection>
        _relationshipFollowProjections;
    private readonly ContinuousOwnerInbox<EntityRetirementCleanup>
        _entityRetirementCleanups;
    private readonly ContinuousOwnerInbox<AttributeSetIntent> _attributeSets;
    private readonly ContinuousOwnerInbox<EntityStateIntent> _entityStates;
    private readonly ContinuousOwnerInbox<EntityCreationIntent>
        _entityCreationRequests;
    private readonly ContinuousOwnerInbox<PreparedEntityCreation>
        _entityCreations;
    private readonly ContinuousOwnerInbox<PreparedEntityCreation>
        _entityCreationStructures;
    private readonly ContinuousOwnerInbox<RelationshipCreationIntent>
        _relationshipCreationRequests;
    private readonly ContinuousOwnerInbox<PreparedRelationshipCreation>
        _relationshipCreations;
    private readonly ContinuousOwnerInbox<RelationshipCreationIntent>
        _relationshipCreationStructures;
    private readonly ContinuousOwnerInbox<RelationshipSetIntent>
        _relationshipSets;
    private readonly ContinuousOwnerInbox<TravelStructureIntent>
        _travelStructures;
    private readonly ContinuousOwnerInbox<ForgeCellIntent> _forgeCells;
    private readonly ContinuousOwnerInbox<DistrictIntent> _districts;
    private readonly ContinuousOwnerInbox<CellMaterializationIntent>
        _cellMaterializations;
    private readonly ContinuousOwnerInbox<TerrainCoverIntent> _terrainCovers;
    private readonly ContinuousOwnerInbox<ProgramSignal> _programSignals;
    private readonly ContinuousOwnerInbox<EntityInfluenceMultiplierIntent>
        _entityInfluenceMultipliers;
    private readonly ContinuousOwnerInbox<CultureInfluenceMultiplierIntent>
        _cultureInfluenceMultipliers;
    private readonly ContinuousOwnerActivity _inboxActivity;
    private readonly int _leadTicks;
    private readonly int[] _retiringEntityOccurrences;
    private readonly int[] _retiringRelationshipOccurrences;
    private readonly CultureStorageKey[] _cultureStorageReconciliations;
    private int _retiringEntityCount;
    private int _retiringRelationshipCount;
    private int _cultureStorageReconciliationCount;
    private int _cellStateDeferrals;
    private int _relationshipCreationDeferrals;
    private int _terrainCoverDeferrals;
    private int _targetDrainAttempts;
    private int _activeDrains;
    private int _targetTick;
    private long _commandAdmissionVersion;
    private bool _windowActive;
    internal ContinuousOwnerRuntime(
        Simulation simulation,
        SimulationOptions options,
        KernelOwnerTopology topology)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        global::System.ArgumentNullException.ThrowIfNull(options);
        global::System.ArgumentNullException.ThrowIfNull(topology);
        _simulation = simulation;
        _topology = topology;
        _leadTicks = options.Execution.OwnerLeadTicks;
        var ownerCount = topology.PartitionCount;
        _inboxActivity = new ContinuousOwnerActivity(ownerCount);
        _localTicks = new int[ownerCount];
        _publishedTicks = new int[ownerCount];
        _publishedRevisions = new long[ownerCount];
        _retiringEntityOccurrences = new int[options.EntityCapacity];
        _retiringRelationshipOccurrences = new int[options.RelationshipCapacity];
        _cultureStorageReconciliations = new CultureStorageKey[LeadCapacity(options.MapInfluenceCapacity)];
        (_movements,
         _attributes,
         _culture,
         _cultureReductions,
         _relationships,
         _relationshipEnds,
         _tags,
         _cellStates,
         _lifecycles,
         _influenceCredits,
         _roadWork) = CreateInboxes(options, ownerCount);
        _cellStatePreparations = new(LeadCapacity(options.MapCellCapacity), ownerCount);
        _culturePreparations = new(LeadCapacity(options.MapInfluenceCapacity), ownerCount);
        _commands = new ContinuousOwnerInbox<KernelCommand>(
            options.CommandCapacity,
            ownerCount);
        (_movementStructures,
         _cellStructures,
         _cultureStructures,
         _relationshipRetirements,
         _entityStructures,
         _roadStructures,
         _relationshipFollowProjections,
         _entityRetirementCleanups) =
            CreateStructuralInboxes(options, ownerCount);
        (_attributeSets,
         _entityStates,
         _entityCreationRequests,
         _entityCreations,
         _entityCreationStructures,
         _relationshipCreationRequests,
         _relationshipCreations,
         _relationshipCreationStructures,
         _relationshipSets,
         _travelStructures,
         _forgeCells,
         _districts,
         _cellMaterializations,
         _terrainCovers) =
            CreateStrategicInboxes(options, ownerCount);
        (_programSignals,
         _entityInfluenceMultipliers,
         _cultureInfluenceMultipliers) =
            CreateProgramInboxes(options, ownerCount);
        AttachInboxActivity();
    }

    private (
        ContinuousOwnerInbox<MovementDelta>,
        ContinuousOwnerInbox<AttributeDelta>,
        ContinuousOwnerInbox<CultureDelta>,
        ContinuousOwnerInbox<CultureReductionDelta>,
        ContinuousOwnerInbox<RelationshipDelta>,
        ContinuousOwnerInbox<RelationshipEndDelta>,
        ContinuousOwnerInbox<TagDelta>,
        ContinuousOwnerInbox<CellStateDelta>,
        ContinuousOwnerInbox<LifecycleDelta>,
        ContinuousOwnerInbox<InfluenceCreditDelta>,
        ContinuousOwnerInbox<ContinuousRoadWork>)
        CreateInboxes(
            SimulationOptions options,
            int ownerCount) =>
        (
            new(LeadCapacity(checked(options.EntityCapacity * 2)), ownerCount),
            new(LeadCapacity(options.AttributeCapacity), ownerCount),
            new(LeadCapacity(options.MapInfluenceCapacity), ownerCount),
            new(LeadCapacity(options.MapInfluenceCapacity), ownerCount),
            new(LeadCapacity(options.RelationshipCapacity), ownerCount),
            new(LeadCapacity(options.RelationshipCapacity), ownerCount),
            new(LeadCapacity(options.AttributeCapacity), ownerCount),
            new(LeadCapacity(options.MapCellCapacity), ownerCount),
            new(LeadCapacity(options.EntityCapacity), ownerCount),
            new(LeadCapacity(options.MapInfluenceCapacity), ownerCount),
            new(
                LeadCapacity(checked(
                    (options.EntityCapacity * 2) +
                    options.MinorActorCapacity)),
                ownerCount)
        );

    private static (
        ContinuousOwnerInbox<ProgramSignal>,
        ContinuousOwnerInbox<EntityInfluenceMultiplierIntent>,
        ContinuousOwnerInbox<CultureInfluenceMultiplierIntent>)
        CreateProgramInboxes(
            SimulationOptions options,
            int ownerCount) =>
        (
            new(options.EntityCapacity, ownerCount),
            new(options.EntityCapacity, ownerCount),
            new(options.CultureIdCapacity, ownerCount)
        );

    private (
        ContinuousOwnerInbox<ContinuousResolvedMovement>,
        ContinuousOwnerInbox<ContinuousCellReconciliation>,
        ContinuousOwnerInbox<CultureStorageKey>,
        ContinuousOwnerInbox<RelationshipStructuralProjection>,
        ContinuousOwnerInbox<EntityStructuralProjection>,
        ContinuousOwnerInbox<ContinuousRoadWork>,
        ContinuousOwnerInbox<RelationshipFollowProjection>,
        ContinuousOwnerInbox<EntityRetirementCleanup>)
        CreateStructuralInboxes(
            SimulationOptions options,
            int ownerCount) =>
        (
            new(LeadCapacity(checked(options.EntityCapacity * 2)), ownerCount),
            new(LeadCapacity(options.MapCellCapacity), ownerCount),
            new(LeadCapacity(checked(options.MapInfluenceCapacity * 2)), ownerCount),
            new(LeadCapacity(options.RelationshipCapacity), ownerCount),
            new(LeadCapacity(options.EntityCapacity), ownerCount),
            new(
                LeadCapacity(checked(
                    (options.EntityCapacity * 2) +
                    options.MinorActorCapacity)),
                ownerCount),
            new(LeadCapacity(options.RelationshipCapacity), ownerCount),
            new(LeadCapacity(checked(options.EntityCapacity * 3)), ownerCount)
        );

    private (
        ContinuousOwnerInbox<AttributeSetIntent>,
        ContinuousOwnerInbox<EntityStateIntent>,
        ContinuousOwnerInbox<EntityCreationIntent>,
        ContinuousOwnerInbox<PreparedEntityCreation>,
        ContinuousOwnerInbox<PreparedEntityCreation>,
        ContinuousOwnerInbox<RelationshipCreationIntent>,
        ContinuousOwnerInbox<PreparedRelationshipCreation>,
        ContinuousOwnerInbox<RelationshipCreationIntent>,
        ContinuousOwnerInbox<RelationshipSetIntent>,
        ContinuousOwnerInbox<TravelStructureIntent>,
        ContinuousOwnerInbox<ForgeCellIntent>,
        ContinuousOwnerInbox<DistrictIntent>,
        ContinuousOwnerInbox<CellMaterializationIntent>,
        ContinuousOwnerInbox<TerrainCoverIntent>)
        CreateStrategicInboxes(
            SimulationOptions options,
            int ownerCount) =>
        (
            new(LeadCapacity(options.AttributeCapacity), ownerCount),
            new(LeadCapacity(options.EntityCapacity), ownerCount),
            new(LeadCapacity(options.EntityCapacity), ownerCount),
            new(LeadCapacity(options.EntityCapacity), ownerCount),
            new(LeadCapacity(options.EntityCapacity), ownerCount),
            new(LeadCapacity(options.RelationshipCapacity), ownerCount),
            new(LeadCapacity(options.RelationshipCapacity), ownerCount),
            new(LeadCapacity(options.RelationshipCapacity), ownerCount),
            new(LeadCapacity(options.RelationshipCapacity), ownerCount),
            new(LeadCapacity(options.MapCellCapacity), ownerCount),
            new(LeadCapacity(options.MapCellCapacity), ownerCount),
            new(LeadCapacity(options.EntityCapacity), ownerCount),
            new(LeadCapacity(options.MapCellCapacity), ownerCount),
            new(LeadCapacity(options.MapCellCapacity), ownerCount)
        );

    internal bool WindowActive =>
        global::System.Threading.Volatile.Read(ref _windowActive);

    internal bool IsEvaluatingCurrentThread =>
        KernelWorkerThread.Current is { IsContinuousOwnerEvaluation: true };

    internal int PublishedLowWatermark => ReadLowWatermark();

    internal int PublishedHighWatermark => ReadHighWatermark();

    internal int PendingMessageCount => PendingCount();

    internal int PublishedTick(int ownerIndex)
    {
        ValidateOwner(ownerIndex);
        return global::System.Threading.Volatile.Read(
            ref _publishedTicks[ownerIndex]);
    }

    internal long PublishedRevision(int ownerIndex)
    {
        ValidateOwner(ownerIndex);
        return global::System.Threading.Volatile.Read(
            ref _publishedRevisions[ownerIndex]);
    }

    internal void BeginWindow(int currentTick, int targetTick)
    {
        NumericGuard.ThrowIfNegative(currentTick);
        if (targetTick <= currentTick ||
            targetTick - currentTick > _leadTicks ||
            _windowActive ||
            PendingCount() != 0)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var owner = 0; owner < _localTicks.Length; owner++)
        {
            if (_localTicks[owner] == 0 && currentTick > 0)
            {
                _localTicks[owner] = currentTick;
                _publishedTicks[owner] = currentTick;
            }
            else if (_localTicks[owner] != currentTick)
            {
                throw new global::System.InvalidOperationException();
            }
        }

        _targetTick = targetTick;
        _commandAdmissionVersion = _simulation.CurrentVersion;
        global::System.Threading.Volatile.Write(ref _windowActive, true);
    }

    internal bool ExecuteOwner(int ownerIndex)
    {
        ValidateOwner(ownerIndex);
        global::System.Threading.Interlocked.Increment(
            ref _activeDrains);
        try
        {
            var current = global::System.Threading.Volatile.Read(
                ref _localTicks[ownerIndex]);
            var next = current + 1;
            KernelWorkerThread.SetOwnerContext(
                ownerIndex,
                global::System.Math.Min(next, _targetTick));
            var progressed = DrainOwnerIfPending(ownerIndex);
            if (current >= _targetTick)
            {
                NoteTargetDrainAttempt();
                return progressed;
            }

            var lowWatermark = ReadLowWatermark();
            if (next > lowWatermark + _leadTicks)
            {
                return progressed;
            }

            EvaluateOwnerPrograms(ownerIndex, next);
            _ = DrainOwnerIfPending(ownerIndex);
            global::System.Threading.Volatile.Write(
                ref _localTicks[ownerIndex],
                next);
            global::System.Threading.Volatile.Write(
                ref _publishedRevisions[ownerIndex],
                _publishedRevisions[ownerIndex] + 1);
            global::System.Threading.Volatile.Write(
                ref _publishedTicks[ownerIndex],
                next);
            return true;
        }
        finally
        {
            global::System.Threading.Interlocked.Decrement(
                ref _activeDrains);
        }
    }

    private void EvaluateOwnerPrograms(int ownerIndex, int tick)
    {
        var worker = KernelWorkerThread.Current ??
            throw new global::System.InvalidOperationException();
        worker.IsContinuousOwnerEvaluation = true;
        try
        {
            _simulation.EvaluateContinuousLocalOwner(ownerIndex, tick);
        }
        finally
        {
            worker.IsContinuousOwnerEvaluation = false;
        }
    }

    internal bool WindowQuiescent()
    {
        if (!_windowActive)
        {
            return false;
        }

        if (global::System.Threading.Volatile.Read(
                ref _activeDrains) != 0 ||
            PendingCount() != 0)
        {
            return false;
        }

        for (var owner = 0; owner < _localTicks.Length; owner++)
        {
            if (global::System.Threading.Volatile.Read(
                    ref _localTicks[owner]) < _targetTick)
            {
                return false;
            }
        }

        global::System.Threading.Thread.MemoryBarrier();
        return global::System.Threading.Volatile.Read(
                   ref _activeDrains) == 0 &&
            PendingCount() == 0;
    }

    internal bool HasRunnableWork(
        int firstOwner,
        int ownerLimit)
    {
        if ((uint)firstOwner >= (uint)_localTicks.Length ||
            ownerLimit <= firstOwner ||
            ownerLimit > _localTicks.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(firstOwner));
        }

        if (HasPendingOwnerWork(firstOwner, ownerLimit))
        {
            return true;
        }

        var lowWatermark = ReadLowWatermark();
        for (var owner = firstOwner; owner < ownerLimit; owner++)
        {
            var current = global::System.Threading.Volatile.Read(
                ref _localTicks[owner]);
            if (current < _targetTick &&
                current + 1 <= lowWatermark + _leadTicks)
            {
                return true;
            }
        }

        return false;
    }

    internal void CompleteWindow()
    {
        if (!WindowQuiescent())
        {
            throw new global::System.InvalidOperationException();
        }

        FinalizeCultureStorage();
        FinalizeRetiredSlots();
        global::System.Threading.Volatile.Write(ref _windowActive, false);
    }

    internal void Enqueue(in MovementDelta delta) =>
        _movements.Add(
            _topology.EntityOccurrenceOwner(
                delta.EntityOccurrenceHandle),
            delta);

    internal void Enqueue(in AttributeDelta delta) =>
        _attributes.Add(
            _topology.EntityOccurrenceOwner(
                delta.EntityOccurrenceHandle),
            delta);

    internal void Enqueue(in CultureDelta delta) =>
        _culture.Add(
            _topology.CellOwner(
                delta.PlaneCode,
                delta.Q,
                delta.R,
                delta.Z),
            delta);

    internal void Enqueue(in CultureReductionDelta delta) =>
        _cultureReductions.Add(
            _topology.CellOwner(
                delta.PlaneCode,
                delta.Q,
                delta.R),
            delta);

    internal void Enqueue(in RelationshipDelta delta) =>
        _relationships.Add(
            _topology.RelationshipOccurrenceOwner(
                delta.RelationshipOccurrenceHandle),
            delta);

    internal void Enqueue(in RelationshipEndDelta delta) =>
        _relationshipEnds.Add(
            _topology.RelationshipOccurrenceOwner(
                delta.RelationshipOccurrenceHandle),
            delta);

    internal void Enqueue(in TagDelta delta) =>
        _tags.Add(
            _topology.EntityOccurrenceOwner(
                delta.EntityOccurrenceHandle),
            delta);

    internal void Enqueue(in CellStateDelta delta) =>
        _cellStates.Add(
            _topology.CellOwner(
                delta.PlaneCode,
                delta.Q,
                delta.R),
            delta);

    internal void Enqueue(in LifecycleDelta delta) =>
        _lifecycles.Add(
            _topology.EntityOccurrenceOwner(
                delta.EntityOccurrenceHandle),
            delta);

    internal void EnqueueRoadWork(
        int tick,
        int planeCode,
        int q,
        int r,
        FixedScalar amount,
        in KernelExecutionContext context) =>
        _roadWork.Add(
            _topology.CellOwner(planeCode, q, r),
            new ContinuousRoadWork(
                tick,
                planeCode,
                q,
                r,
                amount,
                context));

    internal void EnqueueCommand(
        int ownerIndex,
        in KernelCommand command) =>
        _commands.Add(ownerIndex, command);

    internal void Enqueue(in AttributeSetIntent intent) =>
        _attributeSets.Add(
            _topology.EntityOccurrenceOwner(
                intent.EntityOccurrenceHandle),
            intent);

    internal void Enqueue(in EntityStateIntent intent) =>
        _entityStates.Add(
            _topology.EntityOccurrenceOwner(
                intent.EntityOccurrenceHandle),
            intent);

    internal void Enqueue(in EntityCreationIntent intent) =>
        _entityCreationRequests.Add(
            _topology.StructuralOwner,
            intent);

    internal void Enqueue(in RelationshipCreationIntent intent) =>
        _relationshipCreationRequests.Add(
            _topology.StructuralOwner,
            intent);

    internal void Enqueue(in RelationshipSetIntent intent) =>
        _relationshipSets.Add(
            _topology.RelationshipOccurrenceOwner(
                intent.RelationshipOccurrenceHandle),
            intent);

    internal void Enqueue(in TravelStructureIntent intent) =>
        _travelStructures.Add(
            _topology.CellOwner(
                intent.PlaneCode,
                intent.Q,
                intent.R),
            intent);

    internal void Enqueue(in ForgeCellIntent intent) =>
        _forgeCells.Add(
            _topology.StructuralOwner,
            intent);

    internal void Enqueue(in DistrictIntent intent) =>
        _districts.Add(
            _topology.EntityOccurrenceOwner(
                intent.OperationCode == DistrictIntentCodes.Install
                    ? intent.TargetOccurrenceHandle
                    : intent.EntityOccurrenceHandle),
            intent);

    internal void Enqueue(in CellMaterializationIntent intent) =>
        _cellMaterializations.Add(
            _topology.StructuralOwner,
            intent);

    internal void Enqueue(in TerrainCoverIntent intent) =>
        _terrainCovers.Add(
            _topology.CellOwner(
                PlaneCodes.Physical,
                intent.Q,
                intent.R),
            intent);

    internal void Enqueue(in ProgramSignal signal) =>
        _programSignals.Add(
            _topology.ProgramOwner(signal.ProgramId),
            signal);

    internal void Enqueue(in EntityInfluenceMultiplierIntent intent) =>
        _entityInfluenceMultipliers.Add(
            _topology.EntityOccurrenceOwner(
                intent.EntityOccurrenceHandle),
            intent);

    internal void Enqueue(in CultureInfluenceMultiplierIntent intent) =>
        _cultureInfluenceMultipliers.Add(
            _topology.CultureOwner(intent.CultureId),
            intent);

    private bool DrainOwner(int ownerIndex)
    {
        using var producer = EnterProducer();
        var progressed = false;
        progressed |= DrainCommands(ownerIndex);
        progressed |= DrainMovements(ownerIndex);
        progressed |= DrainCellStates(ownerIndex);
        progressed |= DrainCulture(ownerIndex);
        progressed |= DrainCultureReductions(ownerIndex);
        progressed |= DrainRelationships(ownerIndex);
        progressed |= DrainAttributes(ownerIndex);
        progressed |= DrainTags(ownerIndex);
        progressed |= DrainInfluenceCredits(ownerIndex);
        progressed |= DrainRoadWork(ownerIndex);
        progressed |= DrainRelationshipEnds(ownerIndex);
        progressed |= DrainLifecycles(ownerIndex);
        progressed |= DrainAttributeSets(ownerIndex);
        progressed |= DrainEntityStates(ownerIndex);
        progressed |= DrainEntityCreationRequests(ownerIndex);
        progressed |= DrainEntityCreations(ownerIndex);
        progressed |= DrainEntityCreationStructures(ownerIndex);
        progressed |= DrainRelationshipCreationRequests(ownerIndex);
        progressed |= DrainRelationshipCreations(ownerIndex);
        progressed |= DrainRelationshipCreationStructures(ownerIndex);
        progressed |= DrainRelationshipSets(ownerIndex);
        progressed |= DrainTravelStructures(ownerIndex);
        progressed |= DrainForgeCells(ownerIndex);
        progressed |= DrainDistricts(ownerIndex);
        progressed |= DrainCellMaterializations(ownerIndex);
        progressed |= DrainTerrainCovers(ownerIndex);
        progressed |= DrainRelationshipFollowProjections(ownerIndex);
        progressed |= DrainEntityRetirementCleanups(ownerIndex);
        progressed |= DrainProgramSignals(ownerIndex);
        progressed |= DrainEntityInfluenceMultipliers(ownerIndex);
        progressed |= DrainCultureInfluenceMultipliers(ownerIndex);
        progressed |= DrainCellStatePreparations(ownerIndex);
        progressed |= DrainCulturePreparations(ownerIndex);
        progressed |= DrainStructuralProjections(ownerIndex);
        return progressed;
    }

    private bool DrainOwnerIfPending(int ownerIndex) =>
        _inboxActivity.HasPending(ownerIndex, ownerIndex + 1) &&
        DrainOwner(ownerIndex);

    private bool DrainCommands(int ownerIndex)
    {
        if (_commands.PendingCount == 0)
        {
            return false;
        }

        var cursor = _commands.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_commands.MoveNext(ref cursor, out var command))
        {
            var result = _simulation.ApplyOwnedCommand(
                command,
                KernelWorkerThread.OwnerTick,
                _commandAdmissionVersion);

            if (result.Applied)
            {
                _simulation.WriteCommandApplied(
                    command,
                    KernelWorkerThread.OwnerTick);
            }
            else
            {
                _simulation.WriteCommandRejected(
                    command,
                    result.ReasonCode,
                    KernelWorkerThread.OwnerTick);
            }
        }

        return progressed;
    }

    private bool DrainMovements(int ownerIndex)
    {
        var cursor = _movements.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_movements.MoveNext(ref cursor, out var delta))
        {
            if (!_simulation.World.TryResolveEntityOccurrence(
                    delta.EntityOccurrenceHandle,
                    out var entityId))
            {
                continue;
            }

            var world = _simulation.World;
            var fromQ = delta.PlaneCode == PlaneCodes.Physical
                ? world.EntityPhysicalQ(entityId)
                : world.EntitySemanticQ(entityId);
            var fromR = delta.PlaneCode == PlaneCodes.Physical
                ? world.EntityPhysicalR(entityId)
                : world.EntitySemanticR(entityId);
            var fromZ = delta.PlaneCode == PlaneCodes.Physical
                ? world.EntityPhysicalZ(entityId)
                : 0;
            var tick = KernelWorkerThread.OwnerTick;
            using var scope = _simulation.EnterExecutionContext(delta.Context);
            world.ApplyEntityResolvedMove(
                entityId,
                delta.PlaneCode,
                fromQ,
                fromR,
                fromZ,
                delta.Q,
                delta.R,
                delta.Z,
                tick,
                delta.Context);
            _movementStructures.Add(
                _topology.StructuralOwner,
                new ContinuousResolvedMovement(
                    tick,
                    entityId,
                    delta.PlaneCode,
                    fromQ,
                    fromR,
                    fromZ,
                    delta.Q,
                    delta.R,
                    delta.Z,
                    delta.CauseCode,
                    delta.Context));
        }

        return progressed;
    }

    private bool DrainTags(int ownerIndex)
    {
        var cursor = _tags.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_tags.MoveNext(ref cursor, out var delta))
        {
            _simulation.ApplyContinuousOwnerTag(
                delta,
                KernelWorkerThread.OwnerTick);
        }

        return progressed;
    }

    private bool DrainCellStates(int ownerIndex)
    {
        var cursor = _cellStates.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_cellStates.MoveNext(ref cursor, out var delta))
        {
            if (delta.OwnerEntityId > 0 &&
                !_simulation.World.EntityIsActive(
                    delta.OwnerEntityId) &&
                _simulation.World.EntityIsReserved(
                    delta.OwnerEntityId))
            {
                NoteCellStateDeferral();
                _cellStates.Add(ownerIndex, delta);
                continue;
            }

            if (!_simulation.World.TryGetCellId(
                    delta.PlaneCode,
                    delta.Q,
                    delta.R,
                    z: 0,
                    out var cellId))
            {
                _cellStatePreparations.Add(
                    _topology.StructuralOwner,
                    delta);
                continue;
            }

            if (!CellStateStillApplies(delta))
            {
                continue;
            }

            using var scope =
                _simulation.EnterExecutionContext(delta.Context);
            if (_simulation.World.ApplyOwnedCellState(
                    cellId,
                    delta.PlaneCode,
                    delta.Q,
                    delta.R,
                    delta.TerrainId,
                    delta.OwnerEntityId,
                    KernelWorkerThread.OwnerTick,
                    delta.Context,
                    out var previousOwner,
                    out var terrainChanged))
            {
                _cellStructures.Add(
                    _topology.StructuralOwner,
                    new ContinuousCellReconciliation(
                        cellId,
                        previousOwner,
                        terrainChanged,
                        delta));
            }
        }

        return progressed;
    }

    private bool CellStateStillApplies(in CellStateDelta delta) =>
        delta.ExpectedOwnerEntityId < 0 ||
        _simulation.World.CoverageOwnerAt(
            delta.PlaneCode,
            delta.Q,
            delta.R) == delta.ExpectedOwnerEntityId;

    private bool DrainRoadWork(int ownerIndex)
    {
        var cursor = _roadWork.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_roadWork.MoveNext(ref cursor, out var delta))
        {
            _roadStructures.Add(_topology.StructuralOwner, delta);
        }

        return progressed;
    }

    private bool DrainRelationshipEnds(int ownerIndex)
    {
        var cursor = _relationshipEnds.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_relationshipEnds.MoveNext(ref cursor, out var delta))
        {
            if (!_simulation.World.TryResolveRelationshipOccurrence(
                    delta.RelationshipOccurrenceHandle,
                    out var relationshipId) ||
                (!delta.Unconditional &&
                 _simulation.World.RelationshipStrengthUnits(
                     relationshipId) > delta.ThresholdUnits))
            {
                continue;
            }

            var relationshipKindId =
                _simulation.World.RelationshipKindId(relationshipId);
            var targetEntityId =
                _simulation.World.RelationshipTargetId(relationshipId);
            using var scope =
                _simulation.EnterExecutionContext(delta.Context);
            _simulation.Relationships.NoteRelationshipRecordRetired(
                relationshipId);
            if (_simulation.World.BeginRelationshipRetirementOwned(
                    relationshipId,
                    KernelWorkerThread.OwnerTick,
                    delta.Context))
            {
                _relationshipRetirements.Add(
                    _topology.StructuralOwner,
                    new RelationshipStructuralProjection(
                        delta.RelationshipOccurrenceHandle,
                        relationshipKindId,
                        targetEntityId,
                        KernelWorkerThread.OwnerTick,
                        delta.Context));
            }
        }

        return progressed;
    }

    private bool DrainLifecycles(int ownerIndex)
    {
        var cursor = _lifecycles.BeginDrain(ownerIndex);
        var progressed = cursor.Remaining > 0;
        while (_lifecycles.MoveNext(ref cursor, out var delta))
        {
            if (!_simulation.World.TryResolveEntityOccurrence(
                    delta.EntityOccurrenceHandle,
                    out var entityId) ||
                (delta.AttributeId != 0 &&
                 _simulation.World.GetAttribute(
                     entityId,
                     delta.AttributeId) >
                 FixedScalar.FromUnits(delta.ThresholdUnits)))
            {
                continue;
            }

            BeginEntityRetirement(
                entityId,
                delta.EntityOccurrenceHandle,
                delta.Context);
        }

        return progressed;
    }

    private void BeginEntityRetirement(
        int entityId,
        int entityOccurrenceHandle,
        in KernelExecutionContext context)
    {
        using var scope = _simulation.EnterExecutionContext(context);
        _simulation.LowAltitudeVerbs.NoteEntityRetired(entityId);
        _simulation.Gambits.NoteEntityRetired(entityId);
        _simulation.Relationships.NoteEntityRetired(entityId);
        if (_simulation.World.BeginEntityRetirementOwned(
                entityId,
                EntityStatusCodes.Historical,
                KernelWorkerThread.OwnerTick,
                context))
        {
            _entityStructures.Add(
                _topology.StructuralOwner,
                new EntityStructuralProjection(
                    entityOccurrenceHandle,
                    EntityStructuralProjectionCodes.Retired,
                    PreviousCultureId: 0,
                    CurrentCultureId: 0,
                    KernelWorkerThread.OwnerTick,
                    context));
        }
    }

    private void RecordCultureStorage(in CultureDelta delta)
    {
        _cultureStructures.Add(
            _topology.StructuralOwner,
            new CultureStorageKey(
                delta.CultureId,
                delta.PlaneCode,
                delta.Q,
                delta.R,
                delta.Z));
    }

    private void TrackCultureStorageReconciliation(
        in CultureStorageKey key)
    {
        if (_cultureStorageReconciliationCount >=
            _cultureStorageReconciliations.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _cultureStorageReconciliations[
            _cultureStorageReconciliationCount++] = key;
    }

    private void FinalizeCultureStorage()
    {
        for (var index = 0;
             index < _cultureStorageReconciliationCount;
             index++)
        {
            var key = _cultureStorageReconciliations[index];
            _simulation.World.ReconcileOwnedCultureInfluence(
                key.CultureId,
                key.PlaneCode,
                key.Q,
                key.R,
                key.Z);
            _cultureStorageReconciliations[index] = default;
        }

        _cultureStorageReconciliationCount = 0;
    }

    private int ReadLowWatermark()
    {
        var minimum = int.MaxValue;
        for (var owner = 0; owner < _publishedTicks.Length; owner++)
        {
            minimum = global::System.Math.Min(
                minimum,
                global::System.Threading.Volatile.Read(
                    ref _publishedTicks[owner]));
        }

        return minimum == int.MaxValue ? 0 : minimum;
    }

    private int ReadHighWatermark()
    {
        var maximum = 0;
        for (var owner = 0; owner < _publishedTicks.Length; owner++)
        {
            maximum = global::System.Math.Max(
                maximum,
                global::System.Threading.Volatile.Read(
                    ref _publishedTicks[owner]));
        }

        return maximum;
    }

    private int LeadCapacity(int perTickCapacity)
    {
        const int maximumWindowBurstAllowance = 65536;
        var additional = global::System.Math.Min(
            checked(perTickCapacity * (_leadTicks - 1)),
            maximumWindowBurstAllowance);
        return checked(perTickCapacity + additional);
    }

    private void ValidateOwner(int ownerIndex)
    {
        if ((uint)ownerIndex >= (uint)_localTicks.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(ownerIndex));
        }
    }

}
