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

        int[] refCodes = _refCodes;
        int[] refEntityIds = _refEntityIds;
        int[] createdEntityIds = _createdEntityIds;
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

        using var provenanceScope = EnterFireProvenance(simulation, generatorSlot, targetId);

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
        ExecuteEffects(simulation, world, generatorSlot, targetId, refCodes, refEntityIds, refCount, tick);
        _runCounts[generatorSlot]++;
        WriteGeneratorFired(simulation, tick, generatorSlot, targetId, createdEntityCount, createdRelationshipCount);
        return true;
    }

    private KernelProvenanceScope EnterFireProvenance(Simulation simulation, int generatorSlot, int targetId) =>
        simulation.EnterProvenanceInvocation(
            ProvenanceProducerKindCodes.Generator,
            _generatorIds[generatorSlot],
            targetId);

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
        int[] refCodes,
        int[] refEntityIds,
        int[] createdEntityIds,
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
        int[] refCodes,
        int[] refEntityIds,
        int[] createdEntityIds,
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
        int[] refCodes,
        int[] refEntityIds,
        int refCount)
    {
        if (_producedHasQuery[produced] == 0)
        {
            return 0;
        }

        return SelectSharedEntity(
            world,
            simulation,
            _producedKindIds[produced],
            targetId,
            targetOriginQ,
            targetOriginR,
            refEntityIds,
            refCount,
            _producedQuerySubtypeStarts[produced],
            _producedQuerySubtypeCounts[produced],
            _producedQueryAdjacentToTarget[produced],
            _producedQueryCultureMatchCodes[produced],
            _producedQueryWithoutOutgoingStarts[produced],
            _producedQueryWithoutOutgoingCounts[produced],
            _producedQueryWithoutIncomingStarts[produced],
            _producedQueryWithoutIncomingCounts[produced]);
    }

    private bool CreateMissingProducedEntity(
        Simulation simulation,
        WorldState world,
        int produced,
        int targetId,
        int[] refCodes,
        int[] refEntityIds,
        int[] createdEntityIds,
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

        var createdEntityId = KernelEffectExecutor.CreateEntity(
            simulation,
            _producedKindIds[produced],
            subtypeId,
            world.EntityCultureId(cultureSourceId),
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            -semanticQ - semanticR);
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
        int[] refCodes,
        int[] refEntityIds,
        int[] createdEntityIds,
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
            KernelEffectExecutor.MovePhysical(simulation, createdEntityId, anchorQ, anchorR);
        }

        return true;
    }

    private int CreateRelationships(
        Simulation simulation,
        WorldState world,
        int generatorSlot,
        int[] refCodes,
        int[] refEntityIds,
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
                KernelEffectExecutor.CullOutgoingRelationshipsOfKind(simulation, sourceId, _relationshipKindIds[relationship]);
            }

            KernelEffectExecutor.CreateRelationship(
                simulation,
                _relationshipKindIds[relationship],
                0,
                sourceId,
                destinationId,
                _relationshipStrengths[relationship],
                _relationshipFactId0[relationship],
                _relationshipFactId1[relationship],
                _relationshipFactId2[relationship],
                _relationshipFactId3[relationship],
                _relationshipFactCounts[relationship]);
            created++;
        }

        return created;
    }

    private void ExecuteEffects(
        Simulation simulation,
        WorldState world,
        int generatorSlot,
        int targetId,
        int[] refCodes,
        int[] refEntityIds,
        int refCount,
        int tick)
    {
        var effectStart = _effectStarts[generatorSlot];
        var effectCount = _effectCounts[generatorSlot];
        for (var e = effectStart; e < effectStart + effectCount; e++)
        {
            var effectType = _effectTypeCodes[e];
            var refOrKindId = _effectRefOrKindIds[e];
            switch (effectType)
            {
                case GeneratorEffectTypeCodes.SetBindingHistorical:
                    {
                        var bindingEntityId = ResolveRef(refCodes, refEntityIds, refCount, refOrKindId);
                        if (bindingEntityId > 0)
                        {
                            KernelEffectExecutor.SetEntityStatus(simulation, bindingEntityId, EntityStatusCodes.Historical);
                        }

                        break;
                    }

                case GeneratorEffectTypeCodes.RemoveTargetRelationships:
                    KernelEffectExecutor.CullAllRelationshipsOfKind(simulation, world, targetId, refOrKindId);
                    break;
            }
        }
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
            return KernelQuery.RelationshipEndpointCount(
                    world,
                    targetId,
                    _saturationRelationshipKindIds[generatorSlot],
                    _saturationFactIds[generatorSlot],
                    outgoing: false) >
                _saturationMaxExisting[generatorSlot];
        }

        var count = KernelQuery.RelationshipEndpointCount(
            world,
            targetId,
            _saturationRelationshipKindIds[generatorSlot],
            _saturationFactIds[generatorSlot],
            outgoing: true);
        if (direction == GeneratorSaturationDirectionCodes.Symmetric)
        {
            count += KernelQuery.RelationshipEndpointCount(
                world,
                targetId,
                _saturationRelationshipKindIds[generatorSlot],
                _saturationFactIds[generatorSlot],
                outgoing: false);
        }

        return count > _saturationMaxExisting[generatorSlot];
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
        return KernelQuery.EntityKindMatches(world, targetId, _triggerKindIds[trigger]);
    }

}
