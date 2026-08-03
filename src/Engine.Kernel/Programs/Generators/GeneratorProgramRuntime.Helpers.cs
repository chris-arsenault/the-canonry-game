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

    private int ResolveProducedCultureId(
        WorldState world,
        int[] refCodes,
        int[] refEntityIds,
        int refCount,
        int produced)
    {
        if (_producedCultureModeCodes[produced] == GeneratorProducedCultureModeCodes.AnyParticipant)
        {
            return DrawParticipantCultureId(world, refCodes, refEntityIds, refCount, produced);
        }

        var fixedCultureId = _producedFixedCultureIds[produced];
        if (fixedCultureId > 0)
        {
            return fixedCultureId;
        }

        var cultureSourceId = ResolveRef(refCodes, refEntityIds, refCount, _producedCultureRefCodes[produced]);
        if (cultureSourceId == 0)
        {
            return 0;
        }

        return IsPendingRef(cultureSourceId)
            ? _plannedCultureIds[PendingPlanIndex(cultureSourceId)]
            : world.EntityCultureId(cultureSourceId);
    }

    // The draw is a pure function of the bound entities and the produced
    // slot, so every evaluation of the same firing resolves the same
    // culture without consuming the simulation's random stream. Distinct
    // cultures are walked in bound order twice (count, then select) - ref
    // counts are single digits, so the quadratic distinct check is free.
    private int DrawParticipantCultureId(
        WorldState world,
        int[] refCodes,
        int[] refEntityIds,
        int refCount,
        int produced)
    {
        var distinctCount = 0;
        var hash = 2166136261u;
        for (var i = 0; i < refCount; i++)
        {
            var cultureId = ParticipantCultureId(world, refCodes, refEntityIds, i, produced);
            if (cultureId <= 0)
            {
                continue;
            }

            unchecked
            {
                hash = (hash ^ (uint)refEntityIds[i]) * 16777619u;
            }

            if (IsFirstOccurrence(world, refCodes, refEntityIds, i, cultureId, produced))
            {
                distinctCount++;
            }
        }

        if (distinctCount == 0)
        {
            return 0;
        }

        unchecked
        {
            hash = (hash ^ (uint)produced) * 16777619u;
        }

        var pick = (int)(hash % (uint)distinctCount);
        for (var i = 0; i < refCount; i++)
        {
            var cultureId = ParticipantCultureId(world, refCodes, refEntityIds, i, produced);
            if (cultureId <= 0 || !IsFirstOccurrence(world, refCodes, refEntityIds, i, cultureId, produced))
            {
                continue;
            }

            if (pick == 0)
            {
                return cultureId;
            }

            pick--;
        }

        return 0;
    }

    // Participants are the entities that came to the event. The event's own
    // products - pending or already created earlier in the same firing -
    // are outputs, not participants, and must not pollute the draw pool.
    // The leader-culture sentinel resolves at query time: bindings that
    // hunt the current leading culture follow whoever leads now.
    private static int ResolveQueryCultureId(WorldState world, int queryCultureId) =>
        queryCultureId == GeneratorProgramConstants.LeaderCultureFilter
            ? world.LeaderCultureId()
            : queryCultureId;

    private int ParticipantCultureId(
        WorldState world,
        int[] refCodes,
        int[] refEntityIds,
        int index,
        int produced)
    {
        var entityId = refEntityIds[index];
        if (entityId == 0 || IsPendingRef(entityId) || IsCoProductRef(refCodes[index], produced))
        {
            return 0;
        }

        return world.EntityCultureId(entityId);
    }

    private bool IsCoProductRef(int refCode, int produced)
    {
        var owner = _producedOwnerSlots[produced];
        var start = _producedStarts[owner];
        var end = start + _producedCounts[owner];
        for (var q = start; q < end; q++)
        {
            if (_producedRefCodes[q] == refCode)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsFirstOccurrence(
        WorldState world,
        int[] refCodes,
        int[] refEntityIds,
        int index,
        int cultureId,
        int produced)
    {
        for (var earlier = 0; earlier < index; earlier++)
        {
            if (ParticipantCultureId(world, refCodes, refEntityIds, earlier, produced) == cultureId)
            {
                return false;
            }
        }

        return true;
    }

    private static int PendingRefId(int planIndex) => -(planIndex + 1);

    private static bool IsPendingRef(int refId) => refId < 0;

    private static int PendingPlanIndex(int refId) => -refId - 1;

    private int PlannedEntityQ(int planIndex, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? _plannedSemanticQ[planIndex] : _plannedFinalPhysicalQ[planIndex];

    private int PlannedEntityR(int planIndex, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? _plannedSemanticR[planIndex] : _plannedFinalPhysicalR[planIndex];

    private bool PendingPlacementOccupies(int planeCode, int q, int r)
    {
        for (var planIndex = 0; planIndex < _plannedCreateCount; planIndex++)
        {
            if (PlannedEntityQ(planIndex, planeCode) == q && PlannedEntityR(planIndex, planeCode) == r)
            {
                return true;
            }
        }

        return false;
    }

    private bool PendingCoverageClaims(int q, int r) =>
        ScratchContains(_plannedCoverageCellQ, _plannedCoverageCellR, _plannedCoverageCellCount, q, r);

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

    private int SelectSharedEntity(
        WorldState world,
        Simulation? simulation,
        bool selectRandom,
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
        int requireTargetCultureRoute,
        int withoutOutgoingStart,
        int withoutOutgoingCount,
        int withoutIncomingStart,
        int withoutIncomingCount,
        int requiredTagBits = 0,
        int withoutTagBits = 0,
        int queryCultureId = 0,
        int relatedToTargetKindId = 0,
        int relatedToTargetIncoming = 0,
        FixedScalar relatedToTargetMinStrength = default,
        int kindIdB = 0)
    {
        var query = BuildSharedQuery(
            world, kindId, subtypeStart, subtypeCount, adjacentToTarget,
            cultureMatchCode, withoutOutgoingStart, withoutOutgoingCount,
            withoutIncomingStart, withoutIncomingCount, requiredTagBits,
            withoutTagBits, queryCultureId, relatedToTargetKindId,
            relatedToTargetIncoming, relatedToTargetMinStrength);
        var binding = new QueryBinding(
            actorId: 0,
            targetId,
            targetOriginQ: targetOriginQ,
            targetOriginR: targetOriginR,
            refEntityIds: refEntityIds,
            refCount: refCount);
        // Dual-kind bindings sample uniformly across the joint pool: count
        // both kinds, draw one ordinal over the total, pick from whichever
        // pool it lands in.
        var queryB = kindIdB == 0
            ? query
            : BuildSharedQuery(
                world, kindIdB, subtypeStart, subtypeCount, adjacentToTarget,
                cultureMatchCode, withoutOutgoingStart, withoutOutgoingCount,
                withoutIncomingStart, withoutIncomingCount, requiredTagBits,
                withoutTagBits, queryCultureId, relatedToTargetKindId,
                relatedToTargetIncoming, relatedToTargetMinStrength);
        var count = EvaluateSharedEntityQuery(world, simulation, query, binding, requireTargetCultureRoute, selectedOrdinal: -1).Count;
        var countB = kindIdB == 0
            ? 0
            : EvaluateSharedEntityQuery(world, simulation, queryB, binding, requireTargetCultureRoute, selectedOrdinal: -1).Count;
        if (count + countB == 0)
        {
            return 0;
        }

        var selectedOrdinal = 0;
        if (selectRandom && simulation is not null)
        {
            selectedOrdinal = simulation.RandomInt(
                count + countB,
                targetId > 0 ? world.EntityOccurrenceHandle(targetId) : kindId,
                kindId,
                KernelRandomPurposeCodes.GeneratorSharedQuery,
                attempt: subtypeStart);
        }

        var pool = selectedOrdinal >= count ? queryB : query;
        var poolOrdinal = selectedOrdinal >= count ? selectedOrdinal - count : selectedOrdinal;
        return EvaluateSharedEntityQuery(world, simulation, pool, binding, requireTargetCultureRoute, poolOrdinal).SelectedEntityId;
    }

    private SharedQuery BuildSharedQuery(
        WorldState world,
        int kindId,
        int subtypeStart,
        int subtypeCount,
        int adjacentToTarget,
        int cultureMatchCode,
        int withoutOutgoingStart,
        int withoutOutgoingCount,
        int withoutIncomingStart,
        int withoutIncomingCount,
        int requiredTagBits,
        int withoutTagBits,
        int queryCultureId,
        int relatedToTargetKindId,
        int relatedToTargetIncoming,
        FixedScalar relatedToTargetMinStrength) =>
        new(
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
            withoutIncomingCount,
            requiredTagBits,
            withoutTagBits,
            ResolveQueryCultureId(world, queryCultureId),
            relatedToTargetKindId,
            relatedToTargetIncoming,
            relatedToTargetMinStrength);

    private QueryEvaluation EvaluateSharedEntityQuery(
        WorldState world,
        Simulation? simulation,
        in SharedQuery query,
        in QueryBinding binding,
        int requireTargetCultureRoute,
        int selectedOrdinal)
    {
        if (requireTargetCultureRoute == 0)
        {
            return KernelQuery.EvaluateSharedQuery(world, query, binding, selectedOrdinal);
        }

        if (simulation is null)
        {
            return new QueryEvaluation(0, 0);
        }

        var queryValue = query;
        var bindingValue = binding;
        var count = 0;
        var selectedEntityId = 0;
        KernelQuery.ForEachActiveEntity(
            world,
            entityId =>
            {
                if (!KernelQuery.SharedQueryMatchesCandidate(world, queryValue, bindingValue, entityId) ||
                    !SharedEntityMatchesTargetCultureRoute(simulation, bindingValue.TargetId, entityId))
                {
                    return true;
                }

                if (count == selectedOrdinal)
                {
                    selectedEntityId = entityId;
                }

                count++;
                return selectedEntityId == 0;
            });
        return new QueryEvaluation(count, selectedEntityId);
    }

    private bool SharedEntityMatchesTargetCultureRoute(
        Simulation simulation,
        int targetId,
        int entityId)
    {
        var world = simulation.World;
        var cultureId = world.EntityCultureId(targetId);
        if (cultureId <= 0 || world.EntityCultureId(entityId) != cultureId)
        {
            return false;
        }

        var targetLocality = simulation.CultureLocality.CellAt(
            world,
            cultureId,
            PlaneCodes.Physical,
            world.EntityPhysicalQ(targetId),
            world.EntityPhysicalR(targetId));
        if (targetLocality.ConnectedRawMass < WorldState.CultureClaimedRawMass)
        {
            return false;
        }

        var candidateLocality = simulation.CultureLocality.CellAt(
            world,
            cultureId,
            PlaneCodes.Physical,
            world.EntityPhysicalQ(entityId),
            world.EntityPhysicalR(entityId));
        return candidateLocality.ConnectedRawMass >= WorldState.CultureClaimedRawMass &&
            KernelQuery.HexDistance(
                world.EntityPhysicalQ(targetId),
                world.EntityPhysicalR(targetId),
                world.EntityPhysicalQ(entityId),
                world.EntityPhysicalR(entityId)) <= 12;
    }

    private void PublishGeneratorEvaluationStats(Simulation simulation, int tick)
    {
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            if (_fireStatusCodes[slot] != GeneratorStatusCodes.NotEvaluated)
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
            simulation.Generators._fireStatusCodes[slot],
            simulation.Generators._fireSelectedTargetOccurrenceHandles[slot],
            simulation.Generators._fireCandidateCounts[slot],
            simulation.Generators._fireFailedCounts[slot]);
    }

    private static void WriteGeneratorFired(
        Simulation simulation,
        int tick,
        int slot,
        int selectedTargetOccurrenceHandle,
        int createdEntityCount,
        int createdRelationshipCount,
        int relationshipSpendCount)
    {
        simulation.EmitGeneratorFired(
            tick,
            simulation.Generators._generatorIds[slot],
            selectedTargetOccurrenceHandle,
            createdEntityCount,
            createdRelationshipCount,
            relationshipSpendCount,
            createdRelationshipCount);
        if (selectedTargetOccurrenceHandle > 0)
        {
            simulation.Chronicles.NoteGeneratorFire(
                selectedTargetOccurrenceHandle,
                simulation.Generators._generatorIds[slot]);
        }
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

    private void AddRelationshipKindFilterId(
        int relationshipKindId,
        bool allowSlotCausalLimitMultiplier = false) =>
        AddRelationshipKindFilter(
            relationshipKindId,
            minimumStrength: 0,
            allowSlotCausalLimitMultiplier);

    private void AddRelationshipKindFilter(
        int relationshipKindId,
        FixedScalar minimumStrength,
        bool allowSlotCausalLimitMultiplier = false)
    {
        ValidatePositive(relationshipKindId);
        NumericGuard.ThrowIfNegative(minimumStrength);
        if (_relationshipKindFilterCount + 1 >= _relationshipKindFilterIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _relationshipKindFilterCount++;
        _relationshipKindFilterIds[_relationshipKindFilterCount] = relationshipKindId;
        _relationshipKindFilterMinimumStrengths[_relationshipKindFilterCount] = minimumStrength;
        _relationshipKindFilterAllowsSlotCausalLimitMultiplier[_relationshipKindFilterCount] =
            allowSlotCausalLimitMultiplier ? 1 : 0;
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

    private static int CurrentQ(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticQ(entityId) : world.EntityPhysicalQ(entityId);

    private static int CurrentR(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticR(entityId) : world.EntityPhysicalR(entityId);

    private static void ValidatePositive(int value)
    {
        NumericGuard.ThrowIfNegativeOrZero(value);
    }

}
