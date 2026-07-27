namespace Engine.Kernel;

internal sealed partial class TickDeltaBuffer : IKernelOwnerWork
{
    private const int NoOwnerStage = 0;
    private const int AttributeOwnerStage = 1;
    private const int RelationshipOwnerStage = 2;
    private const int TagOwnerStage = 3;
    private const int MovementOwnerStage = 4;
    private const int CellOwnerStage = 5;
    private const int CultureOwnerStage = 6;
    private const int CultureReductionOwnerStage = 7;
    private const int InfluenceCreditOwnerStage = 8;
    private const int AttributeSetOwnerStage = 9;
    private const int RelationshipSetOwnerStage = 10;
    private const int StrategicCellOwnerStage = 11;
    private const int EntityWorkTierOwnerStage = 12;
    private const int EntityCreationOwnerStage = 13;
    private const int RelationshipCreationOwnerStage = 14;
    private const int TravelStructureOwnerStage = 15;
    private const int NoOwnerBatch = 0;
    private const int EntityCreationOwnerBatch = 1;
    private const int SpatialOwnerBatch = 2;
    private const int PrimaryStateOwnerBatch = 3;
    private const int SecondaryStateOwnerBatch = 4;
    private const int FinalStateOwnerBatch = 5;

    private readonly MovementDelta[] _movements;
    private readonly AttributeDelta[] _attributes;
    private readonly CultureDelta[] _culture;
    private readonly CultureReductionDelta[] _cultureReductions;
    private readonly RelationshipDelta[] _relationships;
    private readonly RelationshipEndDelta[] _relationshipEnds;
    private readonly TagDelta[] _tags;
    private readonly CellStateDelta[] _cellStates;
    private readonly LifecycleDelta[] _lifecycles;
    private readonly ResolvedMovement[] _resolvedMovements;
    private readonly ResolvedCellState[] _resolvedCellStates;
    private readonly int[] _resolvedCellPreviousOwners;
    private readonly bool[] _resolvedCellChanged;
    private readonly bool[] _resolvedCellTerrainChanged;
    private readonly ResolvedCultureGroup[] _resolvedCultureGroups;
    private readonly ResolvedCultureReductionGroup[] _resolvedCultureReductionGroups;
    private readonly int[] _cultureAppliedUnits;
    private readonly bool[] _cultureReductionChanged;
    private readonly InfluenceCreditDelta[] _influenceCredits;
    private readonly DeltaAppendArena<CultureStorageKey> _cultureStorageArena;
    private readonly CultureStorageKey[] _cultureStorageKeys;
    private readonly DeltaAppendArena<MovementDelta> _movementArena;
    private readonly DeltaAppendArena<AttributeDelta> _attributeArena;
    private readonly DeltaAppendArena<CultureDelta> _cultureArena;
    private readonly DeltaAppendArena<CultureReductionDelta> _cultureReductionArena;
    private readonly DeltaAppendArena<RelationshipDelta> _relationshipArena;
    private readonly DeltaAppendArena<RelationshipEndDelta> _relationshipEndArena;
    private readonly DeltaAppendArena<TagDelta> _tagArena;
    private readonly DeltaAppendArena<CellStateDelta> _cellStateArena;
    private readonly DeltaAppendArena<LifecycleDelta> _lifecycleArena;
    private readonly bool _singleWorker;
    private int _movementCount;
    private int _attributeCount;
    private int _cultureCount;
    private int _cultureReductionCount;
    private int _relationshipCount;
    private int _relationshipEndCount;
    private int _tagCount;
    private int _cellStateCount;
    private int _lifecycleCount;
    private int _resolvedMovementCount;
    private int _resolvedCellStateCount;
    private int _resolvedCultureGroupCount;
    private int _resolvedCultureReductionGroupCount;
    private int _influenceCreditCount;
    private int _ownerStage;
    private int _ownerBatch;

    public TickDeltaBuffer(SimulationOptions options)
    {
        _singleWorker = options.Execution.WorkerCount == 1;
        _movementArena = new DeltaAppendArena<MovementDelta>(
            checked(options.EntityCapacity * 2),
            options.Execution.WorkerCount);
        _attributeArena = new DeltaAppendArena<AttributeDelta>(
            options.AttributeCapacity,
            options.Execution.WorkerCount);
        _cultureArena = new DeltaAppendArena<CultureDelta>(
            options.MapInfluenceCapacity,
            options.Execution.WorkerCount);
        _cultureReductionArena = new DeltaAppendArena<CultureReductionDelta>(
            options.MapInfluenceCapacity,
            options.Execution.WorkerCount);
        _relationshipArena = new DeltaAppendArena<RelationshipDelta>(
            options.RelationshipCapacity,
            options.Execution.WorkerCount);
        _relationshipEndArena = new DeltaAppendArena<RelationshipEndDelta>(
            options.RelationshipCapacity,
            options.Execution.WorkerCount);
        _tagArena = new DeltaAppendArena<TagDelta>(
            options.AttributeCapacity,
            options.Execution.WorkerCount);
        _cellStateArena = new DeltaAppendArena<CellStateDelta>(
            options.MapCellCapacity,
            options.Execution.WorkerCount);
        _lifecycleArena = new DeltaAppendArena<LifecycleDelta>(
            options.EntityCapacity,
            options.Execution.WorkerCount);
        InitializeStrategicStorage(options);
        _movements = _movementArena.Values;
        _attributes = _attributeArena.Values;
        _culture = _cultureArena.Values;
        _cultureReductions = _cultureReductionArena.Values;
        _relationships = _relationshipArena.Values;
        _relationshipEnds = _relationshipEndArena.Values;
        _tags = _tagArena.Values;
        _cellStates = _cellStateArena.Values;
        _lifecycles = _lifecycleArena.Values;
        _resolvedMovements = new ResolvedMovement[checked(options.EntityCapacity * 2)];
        _resolvedCellStates = new ResolvedCellState[options.MapCellCapacity];
        _resolvedCellPreviousOwners = new int[options.MapCellCapacity];
        _resolvedCellChanged = new bool[options.MapCellCapacity];
        _resolvedCellTerrainChanged = new bool[options.MapCellCapacity];
        _resolvedCultureGroups =
            new ResolvedCultureGroup[options.MapInfluenceCapacity];
        _resolvedCultureReductionGroups =
            new ResolvedCultureReductionGroup[options.MapInfluenceCapacity];
        _cultureAppliedUnits = new int[options.MapInfluenceCapacity];
        _cultureReductionChanged = new bool[options.MapInfluenceCapacity];
        _influenceCredits =
            new InfluenceCreditDelta[options.MapInfluenceCapacity];
        _cultureStorageArena = new DeltaAppendArena<CultureStorageKey>(
            checked(options.MapInfluenceCapacity * 2),
            options.Execution.WorkerCount);
        _cultureStorageKeys = _cultureStorageArena.Values;
    }

    public int Count =>
        _movementArena.Count +
        _attributeArena.Count +
        _cultureArena.Count +
        _cultureReductionArena.Count +
        _relationshipArena.Count +
        _relationshipEndArena.Count +
        _tagArena.Count +
        _cellStateArena.Count +
        _lifecycleArena.Count +
        StrategicCount;

    public void Clear()
    {
        _movementArena.Clear();
        _attributeArena.Clear();
        _cultureArena.Clear();
        _cultureReductionArena.Clear();
        _relationshipArena.Clear();
        _relationshipEndArena.Clear();
        _tagArena.Clear();
        _cellStateArena.Clear();
        _lifecycleArena.Clear();
        ClearStrategicStorage();
        _cultureStorageArena.Clear();
        _movementCount = 0;
        _attributeCount = 0;
        _cultureCount = 0;
        _cultureReductionCount = 0;
        _relationshipCount = 0;
        _relationshipEndCount = 0;
        _tagCount = 0;
        _cellStateCount = 0;
        _lifecycleCount = 0;
        _resolvedMovementCount = 0;
        _resolvedCellStateCount = 0;
        _resolvedCultureGroupCount = 0;
        _resolvedCultureReductionGroupCount = 0;
        _influenceCreditCount = 0;
    }

    public void AddMovement(in MovementDelta delta) =>
        _movementArena.Add(AppendWorkerIndex, delta);
    public void AddAttribute(in AttributeDelta delta) =>
        _attributeArena.Add(AppendWorkerIndex, delta);
    public void AddCulture(in CultureDelta delta) =>
        _cultureArena.Add(AppendWorkerIndex, delta);
    public void AddCultureReduction(in CultureReductionDelta delta) =>
        _cultureReductionArena.Add(AppendWorkerIndex, delta);
    public void AddRelationship(in RelationshipDelta delta) =>
        _relationshipArena.Add(AppendWorkerIndex, delta);
    public void AddRelationshipEnd(in RelationshipEndDelta delta) =>
        _relationshipEndArena.Add(AppendWorkerIndex, delta);
    public void AddTag(in TagDelta delta) =>
        _tagArena.Add(AppendWorkerIndex, delta);
    public void AddCellState(in CellStateDelta delta) =>
        _cellStateArena.Add(AppendWorkerIndex, delta);
    public void AddLifecycle(in LifecycleDelta delta) =>
        _lifecycleArena.Add(AppendWorkerIndex, delta);

    private int AppendWorkerIndex =>
        _singleWorker ? 0 : KernelWorkerThread.WorkerIndex;

    public void Resolve(Simulation simulation, bool reverseInsertionOrder, int logicalShardCount)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logicalShardCount);

        _movementCount = _movementArena.Compact();
        _attributeCount = _attributeArena.Compact();
        _cultureCount = _cultureArena.Compact();
        _cultureReductionCount = _cultureReductionArena.Compact();
        _relationshipCount = _relationshipArena.Compact();
        _relationshipEndCount = _relationshipEndArena.Compact();
        _tagCount = _tagArena.Compact();
        _cellStateCount = _cellStateArena.Compact();
        _lifecycleCount = _lifecycleArena.Compact();
        CompactStrategicStorage();

        Prepare(_movements, _movementCount, reverseInsertionOrder);
        Prepare(_cellStates, _cellStateCount, reverseInsertionOrder);
        Prepare(_attributes, _attributeCount, reverseInsertionOrder);
        Prepare(_culture, _cultureCount, reverseInsertionOrder);
        Prepare(_cultureReductions, _cultureReductionCount, reverseInsertionOrder);
        Prepare(_relationships, _relationshipCount, reverseInsertionOrder);
        Prepare(_relationshipEnds, _relationshipEndCount, reverseInsertionOrder);
        Prepare(_tags, _tagCount, reverseInsertionOrder);
        Prepare(_lifecycles, _lifecycleCount, reverseInsertionOrder);
        PrepareStrategicStorage(reverseInsertionOrder);

        ResolvePrepared(simulation);
    }

    private void ExecuteOwnerStage(Simulation simulation, int ownerStage, int count)
    {
        if (count == 0)
        {
            return;
        }

        if (_ownerStage != NoOwnerStage)
        {
            throw new global::System.InvalidOperationException();
        }

        _ownerStage = ownerStage;
        try
        {
            simulation.ExecuteOwnerWork(this, count);
        }
        finally
        {
            _ownerStage = NoOwnerStage;
        }
    }

    private void ResolveMovements(Simulation simulation)
    {
        if (_singleWorker)
        {
            ResolveMovementsSingleWorker(simulation);
            return;
        }

        PrepareResolvedMovements(simulation.World);
        DetachResolvedMovements(simulation.World);
        ExecuteOwnerStage(
            simulation,
            MovementOwnerStage,
            _resolvedMovementCount);
        AttachResolvedMovements(simulation.World);
        ApplyResolvedMovementConsequences(simulation);
    }

    private void ResolveMovementsSingleWorker(Simulation simulation)
    {
        _resolvedMovementCount = 0;
        var index = 0;
        while (index < _movementCount)
        {
            var end = MovementGroupEnd(index);
            var previousCount = _resolvedMovementCount;
            TryPrepareResolvedMovement(simulation.World, index, end);
            if (_resolvedMovementCount > previousCount)
            {
                ApplySingleResolvedMovement(
                    simulation,
                    _resolvedMovements[previousCount]);
            }

            index = end;
        }
    }

    private static void ApplySingleResolvedMovement(
        Simulation simulation,
        in ResolvedMovement movement)
    {
        var world = simulation.World;
        world.DetachEntityForResolvedMove(
            movement.EntityId,
            movement.PlaneCode,
            movement.FromQ,
            movement.FromR,
            movement.FromZ);
        using (simulation.EnterExecutionContext(movement.Context))
        {
            world.ApplyEntityResolvedMove(
                movement.EntityId,
                movement.PlaneCode,
                movement.FromQ,
                movement.FromR,
                movement.FromZ,
                movement.ToQ,
                movement.ToR,
                movement.ToZ,
                simulation.CurrentTick,
                movement.Context);
        }

        world.AttachEntityForResolvedMove(
            movement.EntityId,
            movement.PlaneCode,
            movement.ToQ,
            movement.ToR,
            movement.ToZ);
        if (movement.ToZ == 0)
        {
            using var scope =
                simulation.EnterExecutionContext(movement.Context);
            simulation.Movement.ApplyResolvedMoveSurfaceConsequences(
                simulation,
                movement.EntityId,
                movement.PlaneCode,
                movement.ToQ,
                movement.ToR,
                simulation.CurrentTick,
                movement.CauseCode);
        }
    }

    private void PrepareResolvedMovements(WorldState world)
    {
        _resolvedMovementCount = 0;
        var index = 0;
        while (index < _movementCount)
        {
            var end = MovementGroupEnd(index);
            TryPrepareResolvedMovement(world, index, end);
            index = end;
        }
    }

    private int MovementGroupEnd(int start)
    {
        var first = _movements[start];
        var end = start + 1;
        while (end < _movementCount &&
               _movements[end].EntityOccurrenceHandle == first.EntityOccurrenceHandle &&
               _movements[end].PlaneCode == first.PlaneCode)
        {
            end++;
        }

        return end;
    }

    private void TryPrepareResolvedMovement(
        WorldState world,
        int start,
        int end)
    {
        var first = _movements[start];
        if (!world.TryResolveEntityOccurrence(
                first.EntityOccurrenceHandle,
                out var entityId))
        {
            return;
        }

        var winner = _movements[end - 1];
        var semantic = winner.PlaneCode == PlaneCodes.Semantic;
        var fromQ = semantic
            ? world.EntitySemanticQ(entityId)
            : world.EntityPhysicalQ(entityId);
        var fromR = semantic
            ? world.EntitySemanticR(entityId)
            : world.EntityPhysicalR(entityId);
        var fromZ = semantic ? 0 : world.EntityPhysicalZ(entityId);
        if (fromQ == winner.Q && fromR == winner.R && fromZ == winner.Z)
        {
            return;
        }

        _resolvedMovements[_resolvedMovementCount++] = new ResolvedMovement(
            entityId,
            winner.PlaneCode,
            fromQ,
            fromR,
            fromZ,
            winner.Q,
            winner.R,
            winner.Z,
            winner.CauseCode,
            winner.Context);
    }

    private void DetachResolvedMovements(WorldState world)
    {
        for (var index = 0; index < _resolvedMovementCount; index++)
        {
            var movement = _resolvedMovements[index];
            world.DetachEntityForResolvedMove(
                movement.EntityId,
                movement.PlaneCode,
                movement.FromQ,
                movement.FromR,
                movement.FromZ);
        }
    }

    private void AttachResolvedMovements(WorldState world)
    {
        for (var index = 0; index < _resolvedMovementCount; index++)
        {
            var movement = _resolvedMovements[index];
            world.AttachEntityForResolvedMove(
                movement.EntityId,
                movement.PlaneCode,
                movement.ToQ,
                movement.ToR,
                movement.ToZ);
        }
    }

    private void ApplyResolvedMovementConsequences(Simulation simulation)
    {
        for (var index = 0; index < _resolvedMovementCount; index++)
        {
            var movement = _resolvedMovements[index];
            if (movement.ToZ != 0)
            {
                continue;
            }

            using var scope = simulation.EnterExecutionContext(movement.Context);
            simulation.Movement.ApplyResolvedMoveSurfaceConsequences(
                simulation,
                movement.EntityId,
                movement.PlaneCode,
                movement.ToQ,
                movement.ToR,
                simulation.CurrentTick,
                movement.CauseCode);
        }
    }

    private void ResolveMovementOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = OwnerStart(
            ownerIndex,
            ownerCount,
            _resolvedMovementCount,
            MovementOwnerStage);
        var limit = OwnerStart(
            ownerIndex + 1,
            ownerCount,
            _resolvedMovementCount,
            MovementOwnerStage);
        while (index < limit)
        {
            var movement = _resolvedMovements[index++];
            using var scope = simulation.EnterExecutionContext(movement.Context);
            simulation.World.ApplyEntityResolvedMove(
                movement.EntityId,
                movement.PlaneCode,
                movement.FromQ,
                movement.FromR,
                movement.FromZ,
                movement.ToQ,
                movement.ToR,
                movement.ToZ,
                simulation.CurrentTick,
                movement.Context);
        }
    }

    private void ResolveCellStates(Simulation simulation)
    {
        if (_singleWorker)
        {
            ResolveCellStatesSingleWorker(simulation);
            return;
        }

        PrepareResolvedCellStates(simulation.World);
        ExecuteOwnerStage(
            simulation,
            CellOwnerStage,
            _resolvedCellStateCount);
        ReconcileResolvedCellStates(simulation);
    }

    private void ResolveCellStatesSingleWorker(Simulation simulation)
    {
        var index = 0;
        while (index < _cellStateCount)
        {
            var first = _cellStates[index];
            var end = index + 1;
            while (end < _cellStateCount &&
                   first.SameDestination(_cellStates[end]))
            {
                end++;
            }

            ApplySingleCellState(
                simulation,
                _cellStates[end - 1]);
            index = end;
        }
    }

    private static void ApplySingleCellState(
        Simulation simulation,
        in CellStateDelta delta)
    {
        var world = simulation.World;
        var cellId = world.PrepareOwnedCellState(
            delta.PlaneCode,
            delta.Q,
            delta.R);
        using var scope = simulation.EnterExecutionContext(delta.Context);
        var changed = world.ApplyOwnedCellState(
            cellId,
            delta.PlaneCode,
            delta.Q,
            delta.R,
            delta.TerrainId,
            delta.OwnerEntityId,
            simulation.CurrentTick,
            delta.Context,
            out var previousOwner,
            out var terrainChanged);
        if (!changed)
        {
            return;
        }

        world.ReconcileOwnedCellState(
            cellId,
            delta.PlaneCode,
            delta.Q,
            delta.R,
            previousOwner,
            delta.OwnerEntityId);
        if (terrainChanged)
        {
            simulation.Movement.RecordTopologyChanged();
        }
    }

    private void PrepareResolvedCellStates(WorldState world)
    {
        _resolvedCellStateCount = 0;
        var index = 0;
        while (index < _cellStateCount)
        {
            var first = _cellStates[index];
            var end = index + 1;
            while (end < _cellStateCount && first.SameDestination(_cellStates[end]))
            {
                end++;
            }

            var winner = _cellStates[end - 1];
            var cellId = world.PrepareOwnedCellState(
                winner.PlaneCode,
                winner.Q,
                winner.R);
            _resolvedCellStates[_resolvedCellStateCount++] =
                new ResolvedCellState(cellId, winner);

            index = end;
        }
    }

    private void ResolveCellOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = (int)((long)_resolvedCellStateCount * ownerIndex / ownerCount);
        var limit = (int)((long)_resolvedCellStateCount * (ownerIndex + 1) / ownerCount);
        while (index < limit)
        {
            var resolved = _resolvedCellStates[index];
            var winner = resolved.Delta;
            using var scope = simulation.EnterExecutionContext(winner.Context);
            _resolvedCellChanged[index] = simulation.World.ApplyOwnedCellState(
                resolved.CellId,
                winner.PlaneCode,
                winner.Q,
                winner.R,
                winner.TerrainId,
                winner.OwnerEntityId,
                simulation.CurrentTick,
                winner.Context,
                out _resolvedCellPreviousOwners[index],
                out _resolvedCellTerrainChanged[index]);
            index++;
        }
    }

    private void ReconcileResolvedCellStates(Simulation simulation)
    {
        for (var index = 0; index < _resolvedCellStateCount; index++)
        {
            if (!_resolvedCellChanged[index])
            {
                continue;
            }

            var resolved = _resolvedCellStates[index];
            var delta = resolved.Delta;
            simulation.World.ReconcileOwnedCellState(
                resolved.CellId,
                delta.PlaneCode,
                delta.Q,
                delta.R,
                _resolvedCellPreviousOwners[index],
                delta.OwnerEntityId);
            if (_resolvedCellTerrainChanged[index])
            {
                simulation.Movement.RecordTopologyChanged();
            }
        }
    }

    private void ResolveAttributeOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = OwnerStart(
            ownerIndex,
            ownerCount,
            _attributeCount,
            AttributeOwnerStage);
        var limit = OwnerStart(
            ownerIndex + 1,
            ownerCount,
            _attributeCount,
            AttributeOwnerStage);
        while (index < limit)
        {
            var first = _attributes[index];
            var end = index + 1;
            long sum = first.DeltaUnits;
            while (end < limit && first.SameDestination(_attributes[end]))
            {
                sum = checked(sum + _attributes[end].DeltaUnits);
                end++;
            }

            if (simulation.World.TryResolveEntityOccurrence(first.EntityOccurrenceHandle, out var entityId))
            {
                using var scope = simulation.EnterExecutionContext(first.Context);
                simulation.ModifyAttributeUnits(
                    entityId,
                    first.AttributeId,
                    ClampToInt(sum),
                    simulation.CurrentTick);
            }

            index = end;
        }
    }

    private void ResolveRelationshipOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = OwnerStart(
            ownerIndex,
            ownerCount,
            _relationshipCount,
            RelationshipOwnerStage);
        var limit = OwnerStart(
            ownerIndex + 1,
            ownerCount,
            _relationshipCount,
            RelationshipOwnerStage);
        while (index < limit)
        {
            var first = _relationships[index];
            var end = index + 1;
            long sum = first.DeltaUnits;
            while (end < limit && first.SameDestination(_relationships[end]))
            {
                sum = checked(sum + _relationships[end].DeltaUnits);
                end++;
            }

            if (simulation.World.TryResolveRelationshipOccurrence(
                    first.RelationshipOccurrenceHandle,
                    out var relationshipId))
            {
                using var scope = simulation.EnterExecutionContext(first.Context);
                simulation.ModifyRelationshipStrengthUnits(
                    relationshipId,
                    ClampToInt(sum),
                    simulation.CurrentTick);
            }

            index = end;
        }
    }

    private void ResolveRelationshipEnds(Simulation simulation)
    {
        var index = 0;
        while (index < _relationshipEndCount)
        {
            var first = _relationshipEnds[index];
            var end = index + 1;
            var unconditional = first.Unconditional;
            var thresholdUnits = first.ThresholdUnits;
            while (end < _relationshipEndCount && first.SameDestination(_relationshipEnds[end]))
            {
                unconditional |= _relationshipEnds[end].Unconditional;
                thresholdUnits = global::System.Math.Max(
                    thresholdUnits,
                    _relationshipEnds[end].ThresholdUnits);
                end++;
            }

            if (simulation.World.TryResolveRelationshipOccurrence(
                    first.RelationshipOccurrenceHandle,
                    out var relationshipId) &&
                (unconditional ||
                 KernelFixedPoint.RelationshipStrengthUnits(
                     simulation.World.RelationshipStrength(relationshipId)) <= thresholdUnits))
            {
                using var scope = simulation.EnterExecutionContext(first.Context);
                simulation.CullRelationship(relationshipId, simulation.CurrentTick);
            }

            index = end;
        }
    }

    private void ResolveTagOwner(
        Simulation simulation,
        int ownerIndex,
        int ownerCount)
    {
        var index = OwnerStart(
            ownerIndex,
            ownerCount,
            _tagCount,
            TagOwnerStage);
        var limit = OwnerStart(
            ownerIndex + 1,
            ownerCount,
            _tagCount,
            TagOwnerStage);
        while (index < limit)
        {
            var first = _tags[index];
            var end = index + 1;
            while (end < limit && first.SameDestination(_tags[end]))
            {
                end++;
            }

            if (simulation.World.TryResolveEntityOccurrence(first.EntityOccurrenceHandle, out var entityId))
            {
                var winner = _tags[end - 1];
                using var scope = simulation.EnterExecutionContext(winner.Context);
                _ = simulation.SetEntityTag(
                    entityId,
                    winner.TagId,
                    winner.Assigned,
                    simulation.CurrentTick);
            }

            index = end;
        }
    }

    private void ResolveLifecycles(Simulation simulation)
    {
        var index = 0;
        while (index < _lifecycleCount)
        {
            var first = _lifecycles[index];
            var end = index + 1;
            while (end < _lifecycleCount &&
                   _lifecycles[end].EntityOccurrenceHandle == first.EntityOccurrenceHandle)
            {
                end++;
            }

            if (simulation.World.TryResolveEntityOccurrence(first.EntityOccurrenceHandle, out var entityId))
            {
                ResolveLifecycleGroup(simulation, entityId, index, end);
            }

            index = end;
        }
    }

    private void ResolveLifecycleGroup(
        Simulation simulation,
        int entityId,
        int start,
        int end)
    {
        if (!LifecycleGroupShouldRetire(simulation.World, entityId, start, end))
        {
            return;
        }

        using var scope = simulation.EnterExecutionContext(_lifecycles[start].Context);
        simulation.SetEntityStatus(
            entityId,
            EntityStatusCodes.Historical,
            simulation.CurrentTick);
    }

    private bool LifecycleGroupShouldRetire(
        WorldState world,
        int entityId,
        int start,
        int end)
    {
        for (var condition = start; condition < end; condition++)
        {
            var delta = _lifecycles[condition];
            if (delta.AttributeId == 0 ||
                world.GetAttribute(entityId, delta.AttributeId) <=
                FixedScalar.FromUnits(delta.ThresholdUnits))
            {
                return true;
            }
        }

        return false;
    }

    private static void Prepare<T>(T[] values, int count, bool reverse)
        where T : struct, IDeterministicOrder<T>
    {
        if (reverse)
        {
            DeterministicSort.Reverse(values, count);
        }

        DeterministicSort.Sort(values, count);
    }

    private static int ClampToInt(long value)
    {
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }

        return value < int.MinValue ? int.MinValue : (int)value;
    }

    private int OwnerStart(
        int ownerIndex,
        int ownerCount,
        int count,
        int ownerStage)
    {
        if (ownerIndex <= 0)
        {
            return 0;
        }

        if (ownerIndex >= ownerCount)
        {
            return count;
        }

        var start = (int)((long)count * ownerIndex / ownerCount);
        while (start < count && SameOwnerAtBoundary(start, ownerStage))
        {
            start++;
        }

        return start;
    }

    private bool SameOwnerAtBoundary(
        int index,
        int ownerStage)
    {
        if (ownerStage == AttributeOwnerStage)
        {
            return _attributes[index - 1].EntityOccurrenceHandle ==
                _attributes[index].EntityOccurrenceHandle;
        }

        if (ownerStage == RelationshipOwnerStage)
        {
            return _relationships[index - 1].RelationshipOccurrenceHandle ==
                _relationships[index].RelationshipOccurrenceHandle;
        }

        if (ownerStage == TagOwnerStage)
        {
            return _tags[index - 1].EntityOccurrenceHandle ==
                _tags[index].EntityOccurrenceHandle;
        }

        if (ownerStage == MovementOwnerStage)
        {
            return _resolvedMovements[index - 1].EntityId ==
                _resolvedMovements[index].EntityId;
        }

        if (ownerStage == CultureOwnerStage)
        {
            return _resolvedCultureGroups[index - 1].SameCell(
                _resolvedCultureGroups[index]);
        }

        if (ownerStage == CultureReductionOwnerStage)
        {
            return _resolvedCultureReductionGroups[index - 1].SameCell(
                _resolvedCultureReductionGroups[index]);
        }

        if (ownerStage == InfluenceCreditOwnerStage)
        {
            return _influenceCredits[index - 1].EntityId ==
                _influenceCredits[index].EntityId;
        }

        return SameStrategicOwnerAtBoundary(index, ownerStage);
    }
}
