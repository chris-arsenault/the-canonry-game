namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GeneratorProgramRuntime
{
    private static bool IsFreePhysical(WorldState world, int q, int r)
    {
        return world.CellIsKnown(q, r) && !IsPhysicalOccupied(world, q, r);
    }

    private static bool IsPhysicalOccupied(WorldState world, int q, int r)
    {
        for (var entityId = world.FirstPhysicalEntityAt(q, r);
             entityId > 0;
             entityId = world.NextPhysicalEntityAt(entityId))
        {
            if (world.EntityIsActive(entityId) &&
                world.EntityPhysicalQ(entityId) == q &&
                world.EntityPhysicalR(entityId) == r)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSemanticOccupied(WorldState world, int q, int r)
    {
        for (var entityId = world.FirstSemanticEntityAt(q, r);
             entityId > 0;
             entityId = world.NextSemanticEntityAt(entityId))
        {
            if (world.EntityIsActive(entityId) &&
                world.EntitySemanticQ(entityId) == q &&
                world.EntitySemanticR(entityId) == r)
            {
                return true;
            }
        }

        return false;
    }

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var aS = -aq - ar;
        var bS = -bq - br;
        return (Abs(aq - bq) + Abs(ar - br) + Abs(aS - bS)) / 2;
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

    private static void SetRef(global::System.Span<int> refCodes, global::System.Span<int> refEntityIds, ref int refCount, int refCode, int entityId)
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

    private static int ResolveRef(global::System.Span<int> refCodes, global::System.Span<int> refEntityIds, int refCount, int refCode)
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

    private static bool RefAlreadyBound(global::System.Span<int> refEntityIds, int refCount, int entityId)
    {
        for (var i = 0; i < refCount; i++)
        {
            if (refEntityIds[i] == entityId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ScratchContains(global::System.Span<int> qs, global::System.Span<int> rs, int count, int q, int r)
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
        global::System.Span<int> createdEntityIds,
        int createdEntityCount,
        int tick)
    {
        for (var i = 0; i < createdEntityCount; i++)
        {
            if (createdEntityIds[i] > 0 && world.EntityIsActive(createdEntityIds[i]))
            {
                simulation.SetEntityStatus(createdEntityIds[i], EntityStatusCodes.Historical, tick);
            }
        }
    }

    private void PublishDueGeneratorStats(Simulation simulation, int tick)
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

    private void AddRelationshipKindFilterId(int relationshipKindId)
    {
        ValidatePositive(relationshipKindId);
        if (_relationshipKindFilterCount + 1 >= _relationshipKindFilterIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _relationshipKindFilterCount++;
        _relationshipKindFilterIds[_relationshipKindFilterCount] = relationshipKindId;
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

    private void AddAttributeCondition(int attributeId, int directionCode, float threshold)
    {
        ValidatePositive(attributeId);
        ValidatePositive(directionCode);
        if (_attributeConditionCount + 1 >= _attributeConditionAttributeIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _attributeConditionCount++;
        _attributeConditionAttributeIds[_attributeConditionCount] = attributeId;
        _attributeConditionDirectionCodes[_attributeConditionCount] = directionCode;
        _attributeConditionThresholds[_attributeConditionCount] = threshold;
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

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }

    private static void ValidateFactCount(int factCount)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(factCount);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThan(factCount, 4);
    }
}
