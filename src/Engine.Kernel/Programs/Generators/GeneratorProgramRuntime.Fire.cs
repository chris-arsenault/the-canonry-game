namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GeneratorProgramRuntime
{
    private bool Fire(Simulation simulation, int generatorSlot, int targetId, int tick)
    {
        var world = simulation.World;
        if (!TryResolveTargetOrigin(world, generatorSlot, targetId, out var targetOriginQ, out var targetOriginR))
        {
            return false;
        }

        global::System.Span<int> refCodes = _refCodes;
        global::System.Span<int> refEntityIds = _refEntityIds;
        global::System.Span<int> createdEntityIds = _createdEntityIds;
        var refCount = 0;
        var createdEntityCount = 0;
        SetRef(refCodes, refEntityIds, ref refCount, GeneratorProgramConstants.TargetReferenceCode, targetId);

        if (!ResolveSecondaryBindings(
                world,
                generatorSlot,
                targetId,
                targetOriginQ,
                targetOriginR,
                refCodes,
                refEntityIds,
                ref refCount,
                selectRandom: true,
                simulation))
        {
            return false;
        }

        if (!CreateProducedEntities(
                simulation,
                world,
                generatorSlot,
                targetId,
                targetOriginQ,
                targetOriginR,
                refCodes,
                refEntityIds,
                createdEntityIds,
                ref refCount,
                ref createdEntityCount,
                tick))
        {
            return false;
        }

        var createdRelationshipCount = CreateRelationships(
            simulation,
            world,
            generatorSlot,
            refCodes,
            refEntityIds,
            refCount,
            tick);
        _runCounts[generatorSlot]++;
        WriteGeneratorFired(simulation, tick, generatorSlot, targetId, createdEntityCount, createdRelationshipCount);
        return true;
    }

    private bool TryResolveTargetOrigin(
        WorldState world,
        int generatorSlot,
        int targetId,
        out int targetOriginQ,
        out int targetOriginR)
    {
        targetOriginQ = world.EntityPhysicalQ(targetId);
        targetOriginR = world.EntityPhysicalR(targetId);
        return _targetAllowWilderness[generatorSlot] == 0 ||
            FindFreePhysicalHex(world, targetOriginQ, targetOriginR, maxRadius: 5, out targetOriginQ, out targetOriginR);
    }

    private bool CreateProducedEntities(
        Simulation simulation,
        WorldState world,
        int generatorSlot,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        global::System.Span<int> createdEntityIds,
        ref int refCount,
        ref int createdEntityCount,
        int tick)
    {
        var firstProduced = _producedStarts[generatorSlot];
        var endProduced = firstProduced + _producedCounts[generatorSlot];
        for (var produced = firstProduced; produced < endProduced; produced++)
        {
            if (!CreateOrBindProducedEntity(
                    simulation,
                    world,
                    produced,
                    targetId,
                    targetOriginQ,
                    targetOriginR,
                    refCodes,
                    refEntityIds,
                    createdEntityIds,
                    ref refCount,
                    ref createdEntityCount,
                    tick))
            {
                return false;
            }
        }

        return true;
    }

    private bool CreateOrBindProducedEntity(
        Simulation simulation,
        WorldState world,
        int produced,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        global::System.Span<int> createdEntityIds,
        ref int refCount,
        ref int createdEntityCount,
        int tick)
    {
        if (_producedCreateChances[produced] < 1f &&
            simulation.NextFloat() > _producedCreateChances[produced])
        {
            SetRef(refCodes, refEntityIds, ref refCount, _producedRefCodes[produced], 0);
            return true;
        }

        var boundEntityId = SelectProducedQueryBinding(
            simulation,
            world,
            produced,
            targetId,
            targetOriginQ,
            targetOriginR,
            refCodes,
            refEntityIds,
            refCount);
        if (boundEntityId > 0)
        {
            SetRef(refCodes, refEntityIds, ref refCount, _producedRefCodes[produced], boundEntityId);
            return true;
        }

        if (_producedCreateIfMissing[produced] == 0)
        {
            SetRef(refCodes, refEntityIds, ref refCount, _producedRefCodes[produced], 0);
            return true;
        }

        return CreateMissingProducedEntity(
            simulation,
            world,
            produced,
            targetId,
            refCodes,
            refEntityIds,
            createdEntityIds,
            ref refCount,
            ref createdEntityCount,
            tick);
    }

    private int SelectProducedQueryBinding(
        Simulation simulation,
        WorldState world,
        int produced,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        int refCount)
    {
        if (_producedHasQuery[produced] == 0)
        {
            return 0;
        }

        return SelectEntityForQuery(
            world,
            simulation,
            _producedKindIds[produced],
            targetId,
            targetOriginQ,
            targetOriginR,
            refCodes,
            refEntityIds,
            refCount,
            _producedQuerySubtypeStarts[produced],
            _producedQuerySubtypeCounts[produced],
            _producedQueryAdjacentToTarget[produced],
            _producedQueryCultureMatchCodes[produced],
            _producedQueryWithoutOutgoingStarts[produced],
            _producedQueryWithoutOutgoingCounts[produced],
            _producedQueryWithoutIncomingStarts[produced],
            _producedQueryWithoutIncomingCounts[produced],
            _producedQueryAttributeConditionStarts[produced],
            _producedQueryAttributeConditionCounts[produced]);
    }

    private bool CreateMissingProducedEntity(
        Simulation simulation,
        WorldState world,
        int produced,
        int targetId,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        global::System.Span<int> createdEntityIds,
        ref int refCount,
        ref int createdEntityCount,
        int tick)
    {
        var subtypeId = ResolveProducedSubtype(simulation, produced);
        var cultureSourceId = ResolveRef(refCodes, refEntityIds, refCount, _producedCultureRefCodes[produced]);
        if (subtypeId == 0 || cultureSourceId == 0)
        {
            MarkCreatedEntitiesHistorical(simulation, world, createdEntityIds, createdEntityCount, tick);
            return false;
        }

        var placementAnchorId = ResolveRef(refCodes, refEntityIds, refCount, _producedPlacementRefCodes[produced]);
        if (placementAnchorId == 0)
        {
            placementAnchorId = targetId;
        }

        ResolvePhysicalPlacement(
            simulation,
            placementAnchorId,
            _producedPhysicalPlacementCodes[produced],
            out var physicalQ,
            out var physicalR);
        ResolveSemanticPlacement(
            simulation,
            placementAnchorId,
            _producedSemanticPlacementCodes[produced],
            out var semanticQ,
            out var semanticR);

        var createdEntityId = simulation.CreateEntity(
            _producedKindIds[produced],
            subtypeId,
            world.EntityCultureId(cultureSourceId),
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            -semanticQ - semanticR,
            tick);
        createdEntityIds[createdEntityCount] = createdEntityId;
        createdEntityCount++;
        SetRef(refCodes, refEntityIds, ref refCount, _producedRefCodes[produced], createdEntityId);
        return ApplyProducedCoverage(
            simulation,
            world,
            produced,
            createdEntityId,
            physicalQ,
            physicalR,
            refCodes,
            refEntityIds,
            createdEntityIds,
            refCount,
            createdEntityCount,
            tick);
    }

    private bool ApplyProducedCoverage(
        Simulation simulation,
        WorldState world,
        int produced,
        int createdEntityId,
        int physicalQ,
        int physicalR,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        global::System.Span<int> createdEntityIds,
        int refCount,
        int createdEntityCount,
        int tick)
    {
        if (_producedCoverageModeCodes[produced] == GeneratorCoverageModeCodes.None)
        {
            return true;
        }

        if (!CreateCoverage(simulation, produced, createdEntityId, refCodes, refEntityIds, refCount, physicalQ, physicalR, out var anchorQ, out var anchorR))
        {
            MarkCreatedEntitiesHistorical(simulation, world, createdEntityIds, createdEntityCount, tick);
            return false;
        }

        if (anchorQ != physicalQ || anchorR != physicalR)
        {
            simulation.MoveEntityPhysical(createdEntityId, anchorQ, anchorR, tick);
        }

        return true;
    }

    private int CreateRelationships(
        Simulation simulation,
        WorldState world,
        int generatorSlot,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        int refCount,
        int tick)
    {
        var created = 0;
        var first = _relationshipStarts[generatorSlot];
        var end = first + _relationshipCounts[generatorSlot];
        for (var relationship = first; relationship < end; relationship++)
        {
            var sourceId = ResolveRef(refCodes, refEntityIds, refCount, _relationshipSourceRefCodes[relationship]);
            var destinationId = ResolveRef(refCodes, refEntityIds, refCount, _relationshipDestinationRefCodes[relationship]);
            if (sourceId == 0 || destinationId == 0)
            {
                continue;
            }

            if (_relationshipReplaceExistingOutgoing[relationship] != 0)
            {
                CullOutgoingRelationshipsOfKind(simulation, sourceId, _relationshipKindIds[relationship], tick);
            }

            simulation.CreateRelationship(
                _relationshipKindIds[relationship],
                0,
                sourceId,
                destinationId,
                _relationshipStrengths[relationship],
                tick,
                _relationshipFactId0[relationship],
                _relationshipFactId1[relationship],
                _relationshipFactId2[relationship],
                _relationshipFactId3[relationship],
                _relationshipFactCounts[relationship]);
            created++;
        }

        return created;
    }

    private static void CullOutgoingRelationshipsOfKind(
        Simulation simulation,
        int sourceEntityId,
        int relationshipKindId,
        int tick)
    {
        simulation.CullOutgoingRelationshipsOfKind(sourceEntityId, relationshipKindId, tick);
    }

    private bool ExceedsTargetSaturation(WorldState world, int generatorSlot, int targetId)
    {
        if (_saturationRelationshipKindIds[generatorSlot] == 0)
        {
            return false;
        }

        var direction = _saturationDirectionCodes[generatorSlot];
        if (direction == GeneratorSaturationDirectionCodes.IncomingToTarget)
        {
            return CountTargetSaturationRelationships(world, generatorSlot, targetId, outgoing: false) >
                _saturationMaxExisting[generatorSlot];
        }

        var count = CountTargetSaturationRelationships(world, generatorSlot, targetId, outgoing: true);
        if (direction == GeneratorSaturationDirectionCodes.Symmetric)
        {
            count += CountTargetSaturationRelationships(world, generatorSlot, targetId, outgoing: false);
        }

        return count > _saturationMaxExisting[generatorSlot];
    }

    private int CountTargetSaturationRelationships(
        WorldState world,
        int generatorSlot,
        int targetId,
        bool outgoing)
    {
        var count = 0;
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKind(targetId, _saturationRelationshipKindIds[generatorSlot])
            : world.IncomingRelationshipsOfKind(targetId, _saturationRelationshipKindIds[generatorSlot]);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            if (TargetSaturationRelationshipMatches(world, generatorSlot, relationshipId))
            {
                count++;
            }
        }

        return count;
    }

    private bool TargetSaturationRelationshipMatches(WorldState world, int generatorSlot, int relationshipId)
    {
        return world.RelationshipHasFact(relationshipId, _saturationFactIds[generatorSlot]);
    }

    private bool TriggersPass(WorldState world, int generatorSlot, int targetId)
    {
        var first = _triggerStarts[generatorSlot];
        var end = first + _triggerCounts[generatorSlot];
        for (var trigger = first; trigger < end; trigger++)
        {
            if (TriggerPasses(world, trigger, targetId))
            {
                return true;
            }
        }

        return false;
    }

    private bool TriggerPasses(WorldState world, int trigger, int targetId)
    {
        var direction = _triggerDirectionCodes[trigger];
        if (direction == GeneratorTriggerDirectionCodes.Any)
        {
            return true;
        }

        if (_triggerAttributeIds[trigger] == 0)
        {
            return false;
        }

        return DirectionPasses(
            world.GetAttribute(targetId, _triggerAttributeIds[trigger]),
            direction,
            _triggerThresholds[trigger]);
    }

    private int SelectEntityForQuery(
        WorldState world,
        Simulation? simulation,
        int kindId,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        int refCount,
        int subtypeStart,
        int subtypeCount,
        int adjacentToTarget,
        int cultureMatchCode,
        int withoutOutgoingStart,
        int withoutOutgoingCount,
        int withoutIncomingStart,
        int withoutIncomingCount,
        int attributeConditionStart,
        int attributeConditionCount)
    {
        var candidateCount = CountMatchingEntitiesForQuery(
            world,
            kindId,
            targetId,
            targetOriginQ,
            targetOriginR,
            refCodes,
            refEntityIds,
            refCount,
            subtypeStart,
            subtypeCount,
            adjacentToTarget,
            cultureMatchCode,
            withoutOutgoingStart,
            withoutOutgoingCount,
            withoutIncomingStart,
            withoutIncomingCount,
            attributeConditionStart,
            attributeConditionCount);
        if (candidateCount == 0)
        {
            return 0;
        }

        return SelectMatchingEntityForQuery(
            world,
            simulation is null ? 0 : simulation.NextInt(candidateCount),
            kindId,
            targetId,
            targetOriginQ,
            targetOriginR,
            refCodes,
            refEntityIds,
            refCount,
            subtypeStart,
            subtypeCount,
            adjacentToTarget,
            cultureMatchCode,
            withoutOutgoingStart,
            withoutOutgoingCount,
            withoutIncomingStart,
            withoutIncomingCount,
            attributeConditionStart,
            attributeConditionCount);
    }

    private int SelectMatchingEntityForQuery(
        WorldState world,
        int selectedOrdinal,
        int kindId,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        int refCount,
        int subtypeStart,
        int subtypeCount,
        int adjacentToTarget,
        int cultureMatchCode,
        int withoutOutgoingStart,
        int withoutOutgoingCount,
        int withoutIncomingStart,
        int withoutIncomingCount,
        int attributeConditionStart,
        int attributeConditionCount)
    {
        var seen = 0;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!EntityMatchesQuery(
                    world,
                    entityId,
                    kindId,
                    targetId,
                    targetOriginQ,
                    targetOriginR,
                    refCodes,
                    refEntityIds,
                    refCount,
                    subtypeStart,
                    subtypeCount,
                    adjacentToTarget,
                    cultureMatchCode,
                    withoutOutgoingStart,
                    withoutOutgoingCount,
                    withoutIncomingStart,
                    withoutIncomingCount,
                    attributeConditionStart,
                    attributeConditionCount))
            {
                continue;
            }

            if (seen == selectedOrdinal)
            {
                return entityId;
            }

            seen++;
        }

        return 0;
    }

    private int CountMatchingEntitiesForQuery(
        WorldState world,
        int kindId,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        int refCount,
        int subtypeStart,
        int subtypeCount,
        int adjacentToTarget,
        int cultureMatchCode,
        int withoutOutgoingStart,
        int withoutOutgoingCount,
        int withoutIncomingStart,
        int withoutIncomingCount,
        int attributeConditionStart,
        int attributeConditionCount)
    {
        var count = 0;
        for (var entityId = world.FirstEntityOfKind(kindId);
             entityId > 0;
             entityId = world.NextEntityOfKind(entityId))
        {
            if (EntityMatchesQuery(
                    world,
                    entityId,
                    kindId,
                    targetId,
                    targetOriginQ,
                    targetOriginR,
                    refCodes,
                    refEntityIds,
                    refCount,
                    subtypeStart,
                    subtypeCount,
                    adjacentToTarget,
                    cultureMatchCode,
                    withoutOutgoingStart,
                    withoutOutgoingCount,
                    withoutIncomingStart,
                    withoutIncomingCount,
                    attributeConditionStart,
                    attributeConditionCount))
            {
                count++;
            }
        }

        return count;
    }

    private bool EntityMatchesQuery(
        WorldState world,
        int entityId,
        int kindId,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        int refCount,
        int subtypeStart,
        int subtypeCount,
        int adjacentToTarget,
        int cultureMatchCode,
        int withoutOutgoingStart,
        int withoutOutgoingCount,
        int withoutIncomingStart,
        int withoutIncomingCount,
        int attributeConditionStart,
        int attributeConditionCount)
    {
        return EntityBasicsMatchQuery(world, entityId, kindId, targetId, refEntityIds, refCount, subtypeStart, subtypeCount) &&
               EntityFiltersMatchQuery(world, entityId, targetId, cultureMatchCode, withoutOutgoingStart, withoutOutgoingCount, withoutIncomingStart, withoutIncomingCount, attributeConditionStart, attributeConditionCount) &&
               EntityAdjacencyMatchesQuery(world, entityId, targetOriginQ, targetOriginR, adjacentToTarget);
    }

    private bool EntityBasicsMatchQuery(
        WorldState world,
        int entityId,
        int kindId,
        int targetId,
        global::System.Span<int> refEntityIds,
        int refCount,
        int subtypeStart,
        int subtypeCount)
    {
        return world.EntityIsActive(entityId) &&
               entityId != targetId &&
               !RefAlreadyBound(refEntityIds, refCount, entityId) &&
               world.EntityKindId(entityId) == kindId &&
               SubtypeMatches(world.EntitySubtypeId(entityId), subtypeStart, subtypeCount);
    }

    private bool EntityFiltersMatchQuery(
        WorldState world,
        int entityId,
        int targetId,
        int cultureMatchCode,
        int withoutOutgoingStart,
        int withoutOutgoingCount,
        int withoutIncomingStart,
        int withoutIncomingCount,
        int attributeConditionStart,
        int attributeConditionCount)
    {
        return
               CultureMatches(world, entityId, targetId, cultureMatchCode) &&
               RelationshipFiltersPass(world, entityId, withoutOutgoingStart, withoutOutgoingCount, outgoing: true) &&
               RelationshipFiltersPass(world, entityId, withoutIncomingStart, withoutIncomingCount, outgoing: false) &&
               AttributeConditionsPass(world, entityId, attributeConditionStart, attributeConditionCount);
    }

    private static bool EntityAdjacencyMatchesQuery(
        WorldState world,
        int entityId,
        int targetOriginQ,
        int targetOriginR,
        int adjacentToTarget)
    {
        return adjacentToTarget == 0 ||
            HexDistance(world.EntityPhysicalQ(entityId), world.EntityPhysicalR(entityId), targetOriginQ, targetOriginR) == 1;
    }

    private bool SubtypeMatches(int subtypeId, int start, int count)
    {
        if (count == 0)
        {
            return true;
        }

        var end = start + count;
        for (var i = start; i < end; i++)
        {
            if (_subtypeIds[i] == subtypeId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool CultureMatches(WorldState world, int entityId, int targetId, int cultureMatchCode)
    {
        return cultureMatchCode switch
        {
            GeneratorCultureMatchCodes.SameAsTarget => world.EntityCultureId(entityId) == world.EntityCultureId(targetId),
            GeneratorCultureMatchCodes.DifferentFromTarget => world.EntityCultureId(entityId) != world.EntityCultureId(targetId),
            _ => true,
        };
    }

    private bool RelationshipFiltersPass(
        WorldState world,
        int entityId,
        int start,
        int count,
        bool outgoing)
    {
        var end = start + count;
        for (var filter = start; filter < end; filter++)
        {
            var cursor = outgoing
                ? world.OutgoingRelationshipsOfKind(entityId, _relationshipKindFilterIds[filter])
                : world.IncomingRelationshipsOfKind(entityId, _relationshipKindFilterIds[filter]);
            if (world.MoveNextRelationship(ref cursor, out _))
            {
                return false;
            }
        }

        return true;
    }

    private bool AttributeConditionsPass(WorldState world, int entityId, int start, int count)
    {
        var end = start + count;
        for (var condition = start; condition < end; condition++)
        {
            if (!DirectionPasses(
                    world.GetAttribute(entityId, _attributeConditionAttributeIds[condition]),
                    _attributeConditionDirectionCodes[condition],
                    _attributeConditionThresholds[condition]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool DirectionPasses(float value, int directionCode, float threshold)
    {
        return directionCode switch
        {
            GeneratorTriggerDirectionCodes.Low => value <= threshold,
            GeneratorTriggerDirectionCodes.Critical => value <= threshold,
            GeneratorTriggerDirectionCodes.High => value >= threshold,
            GeneratorTriggerDirectionCodes.Any => true,
            _ => false,
        };
    }

}
