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

        using var provenanceScope = EnterFireProvenance(simulation, generatorSlot, targetId, out var triggerKindCode, out var triggerId);

        if (!CreateProducedEntities(simulation, world, generatorSlot, targetId, targetOriginQ, targetOriginR, refCodes, refEntityIds, createdEntityIds, ref refCount, ref createdEntityCount, tick))
        {
            return false;
        }

        var createdRelationshipCount = CreateRelationships(simulation, world, generatorSlot, refCodes, refEntityIds, refCount, tick);
        ExecuteEffects(simulation, world, generatorSlot, targetId, refCodes, refEntityIds, refCount, tick, triggerKindCode, triggerId);
        _runCounts[generatorSlot]++;
        WriteGeneratorFired(simulation, tick, generatorSlot, targetId, createdEntityCount, createdRelationshipCount);
        return true;
    }

    private KernelProvenanceScope EnterFireProvenance(
        Simulation simulation,
        int generatorSlot,
        int targetId,
        out int triggerKindCode,
        out int triggerId)
    {
        ResolveGeneratorTriggerEvidence(
            simulation.World,
            generatorSlot,
            targetId,
            out triggerKindCode,
            out triggerId,
            out var parentInvocationId);
        return simulation.EnterProvenanceInvocation(
            ProvenanceProducerKindCodes.Generator,
            _generatorIds[generatorSlot],
            targetId,
            triggerKindCode: triggerKindCode,
            triggerId: triggerId,
            parentInvocationId: parentInvocationId);
    }

    private void ResolveGeneratorTriggerEvidence(
        WorldState world,
        int generatorSlot,
        int targetId,
        out int triggerKindCode,
        out int triggerId,
        out int parentInvocationId)
    {
        if (TryResolveTargetRelationshipTrigger(world, generatorSlot, targetId, out var relationshipId))
        {
            triggerKindCode = ProvenanceTriggerKindCodes.Relationship;
            triggerId = relationshipId;
            parentInvocationId = world.RelationshipCreatedByInvocationId(relationshipId);
            return;
        }

        triggerKindCode = ProvenanceTriggerKindCodes.Entity;
        triggerId = targetId;
        parentInvocationId = 0;
    }

    private bool TryResolveTargetRelationshipTrigger(
        WorldState world,
        int generatorSlot,
        int targetId,
        out int relationshipId)
    {
        return TryResolveRelationshipTrigger(
                world,
                targetId,
                _targetRequiredOutgoingStarts[generatorSlot],
                _targetRequiredOutgoingCounts[generatorSlot],
                outgoing: true,
                out relationshipId) ||
            TryResolveRelationshipTrigger(
                world,
                targetId,
                _targetRequiredIncomingStarts[generatorSlot],
                _targetRequiredIncomingCounts[generatorSlot],
                outgoing: false,
                out relationshipId) ||
            TryResolveAnyDirectionRelationshipTrigger(
                world,
                targetId,
                _targetRequiredAnyStarts[generatorSlot],
                _targetRequiredAnyCounts[generatorSlot],
                out relationshipId);
    }

    private bool TryResolveAnyDirectionRelationshipTrigger(
        WorldState world,
        int targetId,
        int start,
        int count,
        out int relationshipId)
    {
        relationshipId = 0;
        var end = start + count;
        for (var filter = start; filter < end; filter++)
        {
            var relationshipKindId = _relationshipKindFilterIds[filter];
            var minimumStrength = _relationshipKindFilterMinimumStrengths[filter];
            if (TryResolveRelationshipOfKind(world, targetId, relationshipKindId, minimumStrength, outgoing: true, out relationshipId) ||
                TryResolveRelationshipOfKind(world, targetId, relationshipKindId, minimumStrength, outgoing: false, out relationshipId))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryResolveRelationshipTrigger(
        WorldState world,
        int targetId,
        int start,
        int count,
        bool outgoing,
        out int relationshipId)
    {
        relationshipId = 0;
        var end = start + count;
        for (var filter = start; filter < end; filter++)
        {
            if (TryResolveRelationshipOfKind(
                    world,
                    targetId,
                    _relationshipKindFilterIds[filter],
                    _relationshipKindFilterMinimumStrengths[filter],
                    outgoing,
                    out relationshipId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveRelationshipOfKind(
        WorldState world,
        int targetId,
        int relationshipKindId,
        float minimumStrength,
        bool outgoing,
        out int relationshipId) =>
        KernelQuery.TryFirstRelationshipOfKind(world, targetId, relationshipKindId, outgoing, minimumStrength, out relationshipId);

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
                _relationshipStrengths[relationship]);
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
        int tick,
        int triggerKindCode,
        int triggerId)
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
                    RemoveTargetRelationships(
                        simulation,
                        world,
                        targetId,
                        refOrKindId,
                        _effectInstanceSelectionCodes[e],
                        _effectOptionCodes[e],
                        triggerKindCode,
                        triggerId);
                    break;
                case GeneratorEffectTypeCodes.AdjustTargetRelationshipStrength:
                    AdjustTargetRelationshipStrength(
                        simulation,
                        world,
                        targetId,
                        refOrKindId,
                        _effectValues[e],
                        _effectInstanceSelectionCodes[e],
                        _effectOptionCodes[e],
                        triggerKindCode,
                        triggerId);
                    break;
                case GeneratorEffectTypeCodes.SetTerrainPatch:
                    ExecuteSetTerrainPatch(
                        simulation,
                        world,
                        e,
                        refCodes,
                        refEntityIds,
                        refCount);
                    break;
            }
        }
    }

    private static void RemoveTargetRelationships(
        Simulation simulation,
        WorldState world,
        int targetId,
        int relationshipKindId,
        int instanceSelectionCode,
        int optionCode,
        int triggerKindCode,
        int triggerId)
    {
        if (instanceSelectionCode == GeneratorRelationshipInstanceSelectionCodes.AllTargetRelationships)
        {
            KernelEffectExecutor.CullAllRelationshipsOfKind(simulation, world, targetId, relationshipKindId);
            return;
        }

        RemoveTriggerRelationshipInstance(
            simulation,
            world,
            relationshipKindId,
            optionCode,
            triggerKindCode,
            triggerId);
    }

    private static void RemoveTriggerRelationshipInstance(
        Simulation simulation,
        WorldState world,
        int relationshipKindId,
        int optionCode,
        int triggerKindCode,
        int triggerId)
    {
        if (triggerKindCode != ProvenanceTriggerKindCodes.Relationship ||
            triggerId <= 0 ||
            !world.RelationshipIsActive(triggerId) ||
            world.RelationshipKindId(triggerId) != relationshipKindId)
        {
            return;
        }

        var sourceId = world.RelationshipSourceId(triggerId);
        var targetEntityId = world.RelationshipTargetId(triggerId);
        var reciprocalSourceEntityId = targetEntityId;
        var reciprocalTargetEntityId = sourceId;
        var reciprocalId = optionCode == 0
            ? 0
            : KernelQuery.FindRelationship(world, reciprocalSourceEntityId, reciprocalTargetEntityId, relationshipKindId);

        KernelEffectExecutor.CullRelationship(simulation, triggerId);
        if (reciprocalId > 0 && reciprocalId != triggerId && world.RelationshipIsActive(reciprocalId))
        {
            KernelEffectExecutor.CullRelationship(simulation, reciprocalId);
        }
    }

    private static void AdjustTargetRelationshipStrength(
        Simulation simulation,
        WorldState world,
        int targetId,
        int relationshipKindId,
        float delta,
        int instanceSelectionCode,
        int optionCode,
        int triggerKindCode,
        int triggerId)
    {
        if (instanceSelectionCode == GeneratorRelationshipInstanceSelectionCodes.AllTargetRelationships)
        {
            AdjustAllRelationshipsOfKind(simulation, world, targetId, relationshipKindId, delta);
            return;
        }

        AdjustTriggerRelationshipInstance(
            simulation,
            world,
            relationshipKindId,
            delta,
            optionCode,
            triggerKindCode,
            triggerId);
    }

    private static void AdjustAllRelationshipsOfKind(
        Simulation simulation,
        WorldState world,
        int entityId,
        int relationshipKindId,
        float delta)
    {
        KernelQuery.ForEachRelatedEndpoint(
            world,
            entityId,
            relationshipKindId,
            minimumStrength: 0f,
            outgoing: true,
            (relationshipId, _) =>
            {
                AdjustRelationshipStrength(simulation, world, relationshipId, delta);
                return true;
            });
        KernelQuery.ForEachRelatedEndpoint(
            world,
            entityId,
            relationshipKindId,
            minimumStrength: 0f,
            outgoing: false,
            (relationshipId, endpointId) =>
            {
                if (endpointId != entityId)
                {
                    AdjustRelationshipStrength(simulation, world, relationshipId, delta);
                }

                return true;
            });
    }

    private static void AdjustTriggerRelationshipInstance(
        Simulation simulation,
        WorldState world,
        int relationshipKindId,
        float delta,
        int optionCode,
        int triggerKindCode,
        int triggerId)
    {
        if (triggerKindCode != ProvenanceTriggerKindCodes.Relationship ||
            triggerId <= 0 ||
            !world.RelationshipIsActive(triggerId) ||
            world.RelationshipKindId(triggerId) != relationshipKindId)
        {
            return;
        }

        var sourceId = world.RelationshipSourceId(triggerId);
        var targetEntityId = world.RelationshipTargetId(triggerId);
        var reciprocalId = optionCode == 0
            ? 0
            : KernelQuery.FindRelationship(world, targetEntityId, sourceId, relationshipKindId);

        AdjustRelationshipStrength(simulation, world, triggerId, delta);
        if (reciprocalId > 0 && reciprocalId != triggerId && world.RelationshipIsActive(reciprocalId))
        {
            AdjustRelationshipStrength(simulation, world, reciprocalId, delta);
        }
    }

    private static void AdjustRelationshipStrength(
        Simulation simulation,
        WorldState world,
        int relationshipId,
        float delta)
    {
        KernelEffectExecutor.SetRelationshipStrength(
            simulation,
            relationshipId,
            KernelQuery.RelationshipStrength(world, relationshipId) + delta);
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
