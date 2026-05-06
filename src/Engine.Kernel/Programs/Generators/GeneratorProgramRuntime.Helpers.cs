namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GeneratorProgramRuntime
{
    private static bool IsFreePhysical(WorldState world, int q, int r)
    {
        return world.CellIsKnown(q, r) && !KernelQuery.PhysicalCellIsOccupied(world, q, r);
    }

    private static int DirectionQ(int direction)
    {
        return direction switch
        {
            0 => 1,
            1 => 1,
            2 => 0,
            3 => -1,
            4 => -1,
            _ => 0,
        };
    }

    private static int DirectionR(int direction)
    {
        return direction switch
        {
            0 => 0,
            1 => -1,
            2 => -1,
            3 => 0,
            4 => 1,
            _ => 1,
        };
    }

    private static void SetRef(int[] refCodes, int[] refEntityIds, ref int refCount, int refCode, int entityId)
    {
        for (var i = 0; i < refCount; i++)
        {
            if (refCodes[i] == refCode)
            {
                refEntityIds[i] = entityId;
                return;
            }
        }

        if (refCount >= refCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        refCodes[refCount] = refCode;
        refEntityIds[refCount] = entityId;
        refCount++;
    }

    private static int ResolveRef(int[] refCodes, int[] refEntityIds, int refCount, int refCode)
    {
        for (var i = 0; i < refCount; i++)
        {
            if (refCodes[i] == refCode)
            {
                return refEntityIds[i];
            }
        }

        return 0;
    }

    private static bool ScratchContains(int[] qs, int[] rs, int count, int q, int r)
    {
        for (var i = 0; i < count; i++)
        {
            if (qs[i] == q && rs[i] == r)
            {
                return true;
            }
        }

        return false;
    }

    private static void MarkCreatedEntitiesHistorical(
        Simulation simulation,
        WorldState world,
        int[] createdEntityIds,
        int createdEntityCount,
        int tick)
    {
        for (var i = 0; i < createdEntityCount; i++)
        {
            if (createdEntityIds[i] > 0 && world.EntityIsActive(createdEntityIds[i]))
            {
                KernelEffectExecutor.SetEntityStatus(simulation, createdEntityIds[i], EntityStatusCodes.Historical);
            }
        }
    }

    private int SelectSharedEntity(
        WorldState world,
        Simulation? simulation,
        int kindId,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        int[] refEntityIds,
        int refCount,
        int subtypeStart,
        int subtypeCount,
        int adjacentToTarget,
        int cultureMatchCode,
        int withoutOutgoingStart,
        int withoutOutgoingCount,
        int withoutIncomingStart,
        int withoutIncomingCount)
    {
        var query = new SharedQuery(
            kindId,
            _subtypeIds,
            subtypeStart,
            subtypeCount,
            adjacentToTarget,
            cultureMatchCode,
            _relationshipKindFilterIds,
            withoutOutgoingStart,
            withoutOutgoingCount,
            withoutIncomingStart,
            withoutIncomingCount);
        var binding = new QueryBinding(
            actorId: 0,
            targetId,
            targetOriginQ: targetOriginQ,
            targetOriginR: targetOriginR,
            refEntityIds: refEntityIds,
            refCount: refCount);
        var count = KernelQuery.EvaluateSharedQuery(world, query, binding, selectedOrdinal: -1).Count;
        if (count == 0)
        {
            return 0;
        }

        var selectedOrdinal = simulation is null ? 0 : simulation.NextInt(count);
        return KernelQuery.EvaluateSharedQuery(world, query, binding, selectedOrdinal).SelectedEntityId;
    }

    private void PublishGeneratorEvaluationStats(Simulation simulation, int tick)
    {
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            if (_pulseStatusCodes[slot] != GeneratorStatusCodes.NotEvaluated)
            {
                WriteGeneratorEvaluated(simulation, tick, slot);
            }
        }
    }

    private static void WriteGeneratorEvaluated(Simulation simulation, int tick, int slot)
    {
        simulation.EmitGeneratorEvaluated(
            tick,
            simulation.Generators._generatorIds[slot],
            simulation.Generators._pulseStatusCodes[slot],
            simulation.Generators._pulseSelectedTargetIds[slot],
            simulation.Generators._pulseCandidateCounts[slot],
            simulation.Generators._pulseFailedFireCounts[slot]);
    }

    private static void WriteGeneratorFired(
        Simulation simulation,
        int tick,
        int slot,
        int selectedTargetEntityId,
        int createdEntityCount,
        int createdRelationshipCount)
    {
        simulation.EmitGeneratorFired(
            tick,
            simulation.Generators._generatorIds[slot],
            selectedTargetEntityId,
            createdEntityCount,
            createdRelationshipCount);
    }

    private void AddSubtypeId(int subtypeId)
    {
        ValidatePositive(subtypeId);
        if (_subtypeCount + 1 >= _subtypeIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _subtypeCount++;
        _subtypeIds[_subtypeCount] = subtypeId;
    }

    private void AddRelationshipKindFilterId(int relationshipKindId) =>
        AddRelationshipKindFilter(relationshipKindId, minimumStrength: 0f);

    private void AddRelationshipKindFilter(int relationshipKindId, float minimumStrength)
    {
        ValidatePositive(relationshipKindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(minimumStrength);
        if (_relationshipKindFilterCount + 1 >= _relationshipKindFilterIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _relationshipKindFilterCount++;
        _relationshipKindFilterIds[_relationshipKindFilterCount] = relationshipKindId;
        _relationshipKindFilterMinimumStrengths[_relationshipKindFilterCount] = minimumStrength;
    }

    private void AddTerrainFilterId(int terrainId)
    {
        ValidatePositive(terrainId);
        if (_terrainFilterCount + 1 >= _terrainFilterIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _terrainFilterCount++;
        _terrainFilterIds[_terrainFilterCount] = terrainId;
    }

    private void AddTerrainFilterId(int terrainId, int[] starts, int[] counts, int slot)
    {
        if (counts[slot] == 0)
        {
            starts[slot] = _terrainFilterCount + 1;
        }

        AddTerrainFilterId(terrainId);
        counts[slot]++;
    }

    private void EnsureOpenGenerator(int generatorSlot)
    {
        EnsureGenerator(generatorSlot);
        if (generatorSlot != _openGeneratorSlot)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureOpenBinding(int bindingSlot)
    {
        if ((uint)bindingSlot == 0u || bindingSlot > _bindingCount || bindingSlot != _openBindingSlot)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureOpenProduced(int producedSlot)
    {
        if ((uint)producedSlot == 0u || producedSlot > _producedCount || producedSlot != _openProducedSlot)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureOpenEffect(int effectSlot)
    {
        if ((uint)effectSlot == 0u || effectSlot > _effectCount || effectSlot != _openEffectSlot)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureGenerator(int generatorSlot)
    {
        if ((uint)generatorSlot == 0u || generatorSlot > _generatorCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static int Abs(int value)
    {
        return value < 0 ? -value : value;
    }

    private static int MaxAbs(int a, int b, int c) =>
        global::System.Math.Max(Abs(a), global::System.Math.Max(Abs(b), Abs(c)));

    private static int CurrentQ(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticQ(entityId) : world.EntityPhysicalQ(entityId);

    private static int CurrentR(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticR(entityId) : world.EntityPhysicalR(entityId);

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }

}
