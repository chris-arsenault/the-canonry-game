namespace Engine.Kernel;

internal sealed class TickDeltaBuffer
{
    private readonly MovementDelta[] _movements;
    private readonly AttributeDelta[] _attributes;
    private readonly CultureDelta[] _culture;
    private readonly CultureReductionDelta[] _cultureReductions;
    private readonly RelationshipDelta[] _relationships;
    private readonly RelationshipEndDelta[] _relationshipEnds;
    private readonly TagDelta[] _tags;
    private readonly CellStateDelta[] _cellStates;
    private readonly LifecycleDelta[] _lifecycles;
    private int _movementCount;
    private int _attributeCount;
    private int _cultureCount;
    private int _cultureReductionCount;
    private int _relationshipCount;
    private int _relationshipEndCount;
    private int _tagCount;
    private int _cellStateCount;
    private int _lifecycleCount;

    public TickDeltaBuffer(SimulationOptions options)
    {
        _movements = new MovementDelta[checked(options.EntityCapacity * 2)];
        _attributes = new AttributeDelta[options.AttributeCapacity];
        _culture = new CultureDelta[options.MapInfluenceCapacity];
        _cultureReductions = new CultureReductionDelta[options.MapInfluenceCapacity];
        _relationships = new RelationshipDelta[options.RelationshipCapacity];
        _relationshipEnds = new RelationshipEndDelta[options.RelationshipCapacity];
        _tags = new TagDelta[options.AttributeCapacity];
        _cellStates = new CellStateDelta[options.MapCellCapacity];
        _lifecycles = new LifecycleDelta[options.EntityCapacity];
    }

    public int Count =>
        _movementCount +
        _attributeCount +
        _cultureCount +
        _cultureReductionCount +
        _relationshipCount +
        _relationshipEndCount +
        _tagCount +
        _cellStateCount +
        _lifecycleCount;

    public void Clear()
    {
        _movementCount = 0;
        _attributeCount = 0;
        _cultureCount = 0;
        _cultureReductionCount = 0;
        _relationshipCount = 0;
        _relationshipEndCount = 0;
        _tagCount = 0;
        _cellStateCount = 0;
        _lifecycleCount = 0;
    }

    public void AddMovement(in MovementDelta delta) => Add(_movements, ref _movementCount, delta);
    public void AddAttribute(in AttributeDelta delta) => Add(_attributes, ref _attributeCount, delta);
    public void AddCulture(in CultureDelta delta) => Add(_culture, ref _cultureCount, delta);
    public void AddCultureReduction(in CultureReductionDelta delta) =>
        Add(_cultureReductions, ref _cultureReductionCount, delta);
    public void AddRelationship(in RelationshipDelta delta) =>
        Add(_relationships, ref _relationshipCount, delta);
    public void AddRelationshipEnd(in RelationshipEndDelta delta) =>
        Add(_relationshipEnds, ref _relationshipEndCount, delta);
    public void AddTag(in TagDelta delta) => Add(_tags, ref _tagCount, delta);
    public void AddCellState(in CellStateDelta delta) =>
        Add(_cellStates, ref _cellStateCount, delta);
    public void AddLifecycle(in LifecycleDelta delta) =>
        Add(_lifecycles, ref _lifecycleCount, delta);

    public void Resolve(Simulation simulation, bool reverseInsertionOrder, int logicalShardCount)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logicalShardCount);

        Prepare(_movements, _movementCount, reverseInsertionOrder);
        Prepare(_cellStates, _cellStateCount, reverseInsertionOrder);
        Prepare(_attributes, _attributeCount, reverseInsertionOrder);
        Prepare(_culture, _cultureCount, reverseInsertionOrder);
        Prepare(_cultureReductions, _cultureReductionCount, reverseInsertionOrder);
        Prepare(_relationships, _relationshipCount, reverseInsertionOrder);
        Prepare(_relationshipEnds, _relationshipEndCount, reverseInsertionOrder);
        Prepare(_tags, _tagCount, reverseInsertionOrder);
        Prepare(_lifecycles, _lifecycleCount, reverseInsertionOrder);

        ResolveMovements(simulation, logicalShardCount);
        ResolveCellStates(simulation, logicalShardCount);
        ResolveAttributes(simulation, logicalShardCount);
        ResolveCulture(simulation, logicalShardCount);
        ResolveCultureReductions(simulation, logicalShardCount);
        ResolveRelationships(simulation, logicalShardCount);
        ResolveRelationshipEnds(simulation, logicalShardCount);
        ResolveTags(simulation, logicalShardCount);
        ResolveLifecycles(simulation, logicalShardCount);
    }

    private void ResolveMovements(Simulation simulation, int shardCount)
    {
        for (var shard = 0; shard < shardCount; shard++)
        {
            var index = 0;
            while (index < _movementCount)
            {
                var first = _movements[index];
                var end = index + 1;
                while (end < _movementCount &&
                       _movements[end].EntityOccurrenceHandle == first.EntityOccurrenceHandle &&
                       _movements[end].PlaneCode == first.PlaneCode)
                {
                    end++;
                }

                if (Shard(first.EntityOccurrenceHandle, first.PlaneCode, 0, 0, shardCount) == shard &&
                    simulation.World.TryResolveEntityOccurrence(first.EntityOccurrenceHandle, out var entityId))
                {
                    var winner = _movements[end - 1];
                    simulation.Movement.ApplyResolvedMove(
                        simulation,
                        entityId,
                        winner.PlaneCode,
                        winner.Q,
                        winner.R,
                        winner.Z,
                        simulation.CurrentTick,
                        winner.CauseCode,
                        winner.Context);
                }

                index = end;
            }
        }
    }

    private void ResolveCellStates(Simulation simulation, int shardCount)
    {
        for (var shard = 0; shard < shardCount; shard++)
        {
            var index = 0;
            while (index < _cellStateCount)
            {
                var first = _cellStates[index];
                var end = index + 1;
                while (end < _cellStateCount && first.SameDestination(_cellStates[end]))
                {
                    end++;
                }

                if (Shard(first.PlaneCode, first.Q, first.R, 0, shardCount) == shard)
                {
                    var winner = _cellStates[end - 1];
                    using var scope = simulation.EnterExecutionContext(winner.Context);
                    simulation.SetCellState(
                        winner.PlaneCode,
                        winner.Q,
                        winner.R,
                        winner.TerrainId,
                        winner.OwnerEntityId,
                        simulation.CurrentTick);
                }

                index = end;
            }
        }
    }

    private void ResolveAttributes(Simulation simulation, int shardCount)
    {
        for (var shard = 0; shard < shardCount; shard++)
        {
            var index = 0;
            while (index < _attributeCount)
            {
                var first = _attributes[index];
                var end = index + 1;
                long sum = first.DeltaUnits;
                while (end < _attributeCount && first.SameDestination(_attributes[end]))
                {
                    sum = checked(sum + _attributes[end].DeltaUnits);
                    end++;
                }

                if (Shard(first.EntityOccurrenceHandle, first.AttributeId, 0, 0, shardCount) == shard &&
                    simulation.World.TryResolveEntityOccurrence(first.EntityOccurrenceHandle, out var entityId))
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
    }

    private void ResolveCulture(Simulation simulation, int shardCount)
    {
        for (var shard = 0; shard < shardCount; shard++)
        {
            var index = 0;
            while (index < _cultureCount)
            {
                var first = _culture[index];
                var end = index + 1;
                long sum = first.DeltaUnits;
                while (end < _cultureCount && first.SameDestination(_culture[end]))
                {
                    sum = checked(sum + _culture[end].DeltaUnits);
                    end++;
                }

                ResolveCultureGroup(simulation, shardCount, shard, index, end, sum);

                index = end;
            }
        }
    }

    private void ResolveCultureGroup(
        Simulation simulation,
        int shardCount,
        int shard,
        int start,
        int end,
        long sum)
    {
        var first = _culture[start];
        if (Shard(first.CultureId, first.PlaneCode, first.Q, first.R, shardCount) != shard)
        {
            return;
        }

        using var scope = simulation.EnterExecutionContext(first.Context);
        var applied = simulation.ApplyResolvedCultureDelta(
            first.CultureId,
            first.PlaneCode,
            first.Q,
            first.R,
            first.Z,
            ClampToInt(sum),
            simulation.CurrentTick);
        if (applied > 0)
        {
            CreditCultureSources(simulation, start, end, first.PlaneCode, applied);
        }
    }

    private void CreditCultureSources(
        Simulation simulation,
        int start,
        int end,
        int planeCode,
        int applied)
    {
        var remaining = applied;
        for (var contribution = start; contribution < end && remaining > 0; contribution++)
        {
            var source = _culture[contribution];
            if (source.SourceEntityOccurrenceHandle <= 0 ||
                source.DeltaUnits <= 0 ||
                !simulation.World.TryResolveEntityOccurrence(
                    source.SourceEntityOccurrenceHandle,
                    out var sourceEntityId))
            {
                continue;
            }

            var credited = global::System.Math.Min(remaining, source.DeltaUnits);
            simulation.World.NoteEntityInfluenceProduction(sourceEntityId, planeCode, credited);
            remaining -= credited;
        }
    }

    private void ResolveCultureReductions(Simulation simulation, int shardCount)
    {
        for (var shard = 0; shard < shardCount; shard++)
        {
            var index = 0;
            while (index < _cultureReductionCount)
            {
                var first = _cultureReductions[index];
                var end = index + 1;
                long sum = first.DeltaUnits;
                while (end < _cultureReductionCount && first.SameDestination(_cultureReductions[end]))
                {
                    sum = checked(sum + _cultureReductions[end].DeltaUnits);
                    end++;
                }

                if (Shard(first.OwnCultureId, first.PlaneCode, first.Q, first.R, shardCount) == shard)
                {
                    using var scope = simulation.EnterExecutionContext(first.Context);
                    simulation.ApplyResolvedCultureReduction(
                        first.OwnCultureId,
                        first.PlaneCode,
                        first.Q,
                        first.R,
                        ClampToInt(sum),
                        simulation.CurrentTick);
                }

                index = end;
            }
        }
    }

    private void ResolveRelationships(Simulation simulation, int shardCount)
    {
        for (var shard = 0; shard < shardCount; shard++)
        {
            var index = 0;
            while (index < _relationshipCount)
            {
                var first = _relationships[index];
                var end = index + 1;
                long sum = first.DeltaUnits;
                while (end < _relationshipCount && first.SameDestination(_relationships[end]))
                {
                    sum = checked(sum + _relationships[end].DeltaUnits);
                    end++;
                }

                if (Shard(first.RelationshipOccurrenceHandle, 0, 0, 0, shardCount) == shard &&
                    simulation.World.TryResolveRelationshipOccurrence(
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
    }

    private void ResolveRelationshipEnds(Simulation simulation, int shardCount)
    {
        for (var shard = 0; shard < shardCount; shard++)
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

                if (Shard(first.RelationshipOccurrenceHandle, 0, 0, 0, shardCount) == shard &&
                    simulation.World.TryResolveRelationshipOccurrence(
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
    }

    private void ResolveTags(Simulation simulation, int shardCount)
    {
        for (var shard = 0; shard < shardCount; shard++)
        {
            var index = 0;
            while (index < _tagCount)
            {
                var first = _tags[index];
                var end = index + 1;
                while (end < _tagCount && first.SameDestination(_tags[end]))
                {
                    end++;
                }

                if (Shard(first.EntityOccurrenceHandle, first.TagId, 0, 0, shardCount) == shard &&
                    simulation.World.TryResolveEntityOccurrence(first.EntityOccurrenceHandle, out var entityId))
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
    }

    private void ResolveLifecycles(Simulation simulation, int shardCount)
    {
        for (var shard = 0; shard < shardCount; shard++)
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

                if (Shard(first.EntityOccurrenceHandle, 0, 0, 0, shardCount) == shard &&
                    simulation.World.TryResolveEntityOccurrence(first.EntityOccurrenceHandle, out var entityId))
                {
                    ResolveLifecycleGroup(simulation, entityId, index, end);
                }

                index = end;
            }
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

    private static void Add<T>(T[] values, ref int count, in T value)
    {
        if (count >= values.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        values[count++] = value;
    }

    private static int ClampToInt(long value)
    {
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }

        return value < int.MinValue ? int.MinValue : (int)value;
    }

    private static int Shard(int a, int b, int c, int d, int shardCount)
    {
        unchecked
        {
            var hash = (uint)a * 2654435761u;
            hash ^= (uint)b * 2246822519u;
            hash ^= (uint)c * 3266489917u;
            hash ^= (uint)d * 668265263u;
            hash ^= hash >> 16;
            return (int)(hash % (uint)shardCount);
        }
    }
}

internal readonly record struct MovementDelta(
    int EntityOccurrenceHandle,
    int PlaneCode,
    int Q,
    int R,
    int Z,
    int CauseCode,
    int Priority,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<MovementDelta>
{
    public int CompareTo(MovementDelta other) =>
        Compare(
            EntityOccurrenceHandle, other.EntityOccurrenceHandle,
            PlaneCode, other.PlaneCode,
            Priority, other.Priority,
            SourceKey, other.SourceKey,
            Q, other.Q,
            R, other.R,
            Z, other.Z);

    private static int Compare(
        int a0, int b0,
        int a1, int b1,
        int a2, int b2,
        long a3, long b3,
        int a4, int b4,
        int a5, int b5,
        int a6, int b6)
    {
        var comparison = a0.CompareTo(b0);
        if (comparison != 0) return comparison;
        comparison = a1.CompareTo(b1);
        if (comparison != 0) return comparison;
        comparison = a2.CompareTo(b2);
        if (comparison != 0) return comparison;
        comparison = a3.CompareTo(b3);
        if (comparison != 0) return comparison;
        comparison = a4.CompareTo(b4);
        if (comparison != 0) return comparison;
        comparison = a5.CompareTo(b5);
        return comparison != 0 ? comparison : a6.CompareTo(b6);
    }
}

internal readonly record struct AttributeDelta(
    int EntityOccurrenceHandle,
    int AttributeId,
    int DeltaUnits,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<AttributeDelta>
{
    public bool SameDestination(AttributeDelta other) =>
        EntityOccurrenceHandle == other.EntityOccurrenceHandle &&
        AttributeId == other.AttributeId;

    public int CompareTo(AttributeDelta other)
    {
        var comparison = EntityOccurrenceHandle.CompareTo(other.EntityOccurrenceHandle);
        if (comparison != 0) return comparison;
        comparison = AttributeId.CompareTo(other.AttributeId);
        return comparison != 0 ? comparison : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct CultureDelta(
    int CultureId,
    int PlaneCode,
    int Q,
    int R,
    int Z,
    int SourceEntityOccurrenceHandle,
    int DeltaUnits,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<CultureDelta>
{
    public bool SameDestination(CultureDelta other) =>
        CultureId == other.CultureId &&
        PlaneCode == other.PlaneCode &&
        Q == other.Q &&
        R == other.R &&
        Z == other.Z;

    public int CompareTo(CultureDelta other)
    {
        var comparison = CultureId.CompareTo(other.CultureId);
        if (comparison != 0) return comparison;
        comparison = PlaneCode.CompareTo(other.PlaneCode);
        if (comparison != 0) return comparison;
        comparison = Q.CompareTo(other.Q);
        if (comparison != 0) return comparison;
        comparison = R.CompareTo(other.R);
        if (comparison != 0) return comparison;
        comparison = Z.CompareTo(other.Z);
        if (comparison != 0) return comparison;
        comparison = SourceEntityOccurrenceHandle.CompareTo(other.SourceEntityOccurrenceHandle);
        return comparison != 0 ? comparison : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct CultureReductionDelta(
    int OwnCultureId,
    int PlaneCode,
    int Q,
    int R,
    int DeltaUnits,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<CultureReductionDelta>
{
    public bool SameDestination(CultureReductionDelta other) =>
        OwnCultureId == other.OwnCultureId &&
        PlaneCode == other.PlaneCode &&
        Q == other.Q &&
        R == other.R;

    public int CompareTo(CultureReductionDelta other)
    {
        var comparison = OwnCultureId.CompareTo(other.OwnCultureId);
        if (comparison != 0) return comparison;
        comparison = PlaneCode.CompareTo(other.PlaneCode);
        if (comparison != 0) return comparison;
        comparison = Q.CompareTo(other.Q);
        if (comparison != 0) return comparison;
        comparison = R.CompareTo(other.R);
        return comparison != 0 ? comparison : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct RelationshipDelta(
    int RelationshipOccurrenceHandle,
    int DeltaUnits,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<RelationshipDelta>
{
    public bool SameDestination(RelationshipDelta other) =>
        RelationshipOccurrenceHandle == other.RelationshipOccurrenceHandle;

    public int CompareTo(RelationshipDelta other)
    {
        var comparison = RelationshipOccurrenceHandle.CompareTo(other.RelationshipOccurrenceHandle);
        return comparison != 0 ? comparison : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct RelationshipEndDelta(
    int RelationshipOccurrenceHandle,
    int ThresholdUnits,
    bool Unconditional,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<RelationshipEndDelta>
{
    public bool SameDestination(RelationshipEndDelta other) =>
        RelationshipOccurrenceHandle == other.RelationshipOccurrenceHandle;

    public int CompareTo(RelationshipEndDelta other)
    {
        var comparison = RelationshipOccurrenceHandle.CompareTo(other.RelationshipOccurrenceHandle);
        return comparison != 0 ? comparison : SourceKey.CompareTo(other.SourceKey);
    }
}

internal readonly record struct TagDelta(
    int EntityOccurrenceHandle,
    int TagId,
    bool Assigned,
    int Priority,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<TagDelta>
{
    public bool SameDestination(TagDelta other) =>
        EntityOccurrenceHandle == other.EntityOccurrenceHandle &&
        TagId == other.TagId;

    public int CompareTo(TagDelta other)
    {
        var comparison = EntityOccurrenceHandle.CompareTo(other.EntityOccurrenceHandle);
        if (comparison != 0) return comparison;
        comparison = TagId.CompareTo(other.TagId);
        if (comparison != 0) return comparison;
        comparison = Priority.CompareTo(other.Priority);
        if (comparison != 0) return comparison;
        comparison = SourceKey.CompareTo(other.SourceKey);
        return comparison != 0 ? comparison : Assigned.CompareTo(other.Assigned);
    }
}

internal readonly record struct CellStateDelta(
    int PlaneCode,
    int Q,
    int R,
    int TerrainId,
    int OwnerEntityId,
    int Priority,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<CellStateDelta>
{
    public bool SameDestination(CellStateDelta other) =>
        PlaneCode == other.PlaneCode && Q == other.Q && R == other.R;

    public int CompareTo(CellStateDelta other)
    {
        var comparison = PlaneCode.CompareTo(other.PlaneCode);
        if (comparison != 0) return comparison;
        comparison = Q.CompareTo(other.Q);
        if (comparison != 0) return comparison;
        comparison = R.CompareTo(other.R);
        if (comparison != 0) return comparison;
        comparison = Priority.CompareTo(other.Priority);
        if (comparison != 0) return comparison;
        comparison = SourceKey.CompareTo(other.SourceKey);
        if (comparison != 0) return comparison;
        comparison = TerrainId.CompareTo(other.TerrainId);
        return comparison != 0 ? comparison : OwnerEntityId.CompareTo(other.OwnerEntityId);
    }
}

internal readonly record struct LifecycleDelta(
    int EntityOccurrenceHandle,
    int AttributeId,
    long ThresholdUnits,
    long SourceKey,
    KernelExecutionContext Context) : IDeterministicOrder<LifecycleDelta>
{
    public int CompareTo(LifecycleDelta other)
    {
        var comparison = EntityOccurrenceHandle.CompareTo(other.EntityOccurrenceHandle);
        if (comparison != 0) return comparison;
        comparison = AttributeId.CompareTo(other.AttributeId);
        return comparison != 0 ? comparison : SourceKey.CompareTo(other.SourceKey);
    }
}
