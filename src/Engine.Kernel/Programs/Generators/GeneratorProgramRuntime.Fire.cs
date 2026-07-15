namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GeneratorProgramRuntime
{
    private bool Fire(
        Simulation simulation,
        int generatorSlot,
        int targetId,
        int tick,
        out int failureReasonCode)
    {
        failureReasonCode = GeneratorTargetRejectionReasonCodes.None;
        var world = simulation.World;
        var targetOriginQ = world.EntityPhysicalQ(targetId);
        var targetOriginR = world.EntityPhysicalR(targetId);

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
            failureReasonCode = GeneratorTargetRejectionReasonCodes.SecondaryBinding;
            return false;
        }

        using var provenanceScope = EnterFireProvenance(simulation, generatorSlot, targetId, out var triggerKindCode, out var triggerId);

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
                tick,
                out failureReasonCode))
        {
            return false;
        }

        var createdRelationshipCount = CreateRelationships(simulation, world, generatorSlot, refCodes, refEntityIds, refCount, tick);
        var relationshipSpendCount = ExecuteEffects(simulation, world, generatorSlot, targetId, refCodes, refEntityIds, refCount, tick, triggerKindCode, triggerId);
        _runCounts[generatorSlot]++;
        WriteGeneratorFired(simulation, tick, generatorSlot, targetId, createdEntityCount, createdRelationshipCount, relationshipSpendCount);
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
        int tick,
        out int failureReasonCode)
    {
        failureReasonCode = GeneratorTargetRejectionReasonCodes.None;
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
                    tick,
                    out failureReasonCode))
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
        int tick,
        out int failureReasonCode)
    {
        failureReasonCode = GeneratorTargetRejectionReasonCodes.None;
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
            tick,
            out failureReasonCode);
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
            true,
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
            _producedQuerySamePhysicalCultureComponentAsTarget[produced],
            _producedQueryWithoutOutgoingStarts[produced],
            _producedQueryWithoutOutgoingCounts[produced],
            _producedQueryWithoutIncomingStarts[produced],
            _producedQueryWithoutIncomingCounts[produced],
            _producedQueryRequiredTagBits[produced],
            _producedQueryWithoutTagBits[produced],
            _producedQueryCultureIds[produced],
            _producedQueryRelatedToTargetKindIds[produced],
            _producedQueryRelatedToTargetIncoming[produced],
            _producedQueryRelatedToTargetMinStrengths[produced]);
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
        int tick,
        out int failureReasonCode)
    {
        failureReasonCode = GeneratorTargetRejectionReasonCodes.None;
        if (!TryResolveMissingProducedPlacement(
                simulation,
                world,
                produced,
                targetId,
                refCodes,
                refEntityIds,
                refCount,
                out var subtypeId,
                out var producedCultureId,
                out var physicalQ,
                out var physicalR,
                out var semanticQ,
                out var semanticR,
                out failureReasonCode))
        {
            MarkCreatedEntitiesHistorical(simulation, world, createdEntityIds, createdEntityCount, tick);
            return false;
        }

        var createdEntityId = CreateProducedEntity(
            simulation,
            produced,
            subtypeId,
            producedCultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR);
        TrackCreatedProducedEntity(
            produced,
            createdEntityId,
            createdEntityIds,
            ref refCount,
            ref createdEntityCount,
            refCodes,
            refEntityIds);
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
            tick,
            out failureReasonCode);
    }

    private bool TryResolveMissingProducedPlacement(
        Simulation simulation,
        WorldState world,
        int produced,
        int targetId,
        int[] refCodes,
        int[] refEntityIds,
        int refCount,
        out int subtypeId,
        out int producedCultureId,
        out int physicalQ,
        out int physicalR,
        out int semanticQ,
        out int semanticR,
        out int failureReasonCode)
    {
        if (!TryResolveProducedCreationInputs(
                simulation,
                world,
                produced,
                targetId,
                refCodes,
                refEntityIds,
                refCount,
                out subtypeId,
                out producedCultureId,
                out var placementAnchorId))
        {
            physicalQ = 0;
            physicalR = 0;
            semanticQ = 0;
            semanticR = 0;
            failureReasonCode = GeneratorTargetRejectionReasonCodes.SecondaryBinding;
            return false;
        }

        failureReasonCode = GeneratorTargetRejectionReasonCodes.Placement;
        return ResolveProducedPlacement(
            simulation,
            placementAnchorId,
            producedCultureId,
            produced,
            subtypeId,
            out physicalQ,
            out physicalR,
            out semanticQ,
            out semanticR);
    }

    private int CreateProducedEntity(
        Simulation simulation,
        int produced,
        int subtypeId,
        int producedCultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR) =>
        KernelEffectExecutor.CreateEntity(
            simulation,
            _producedKindIds[produced],
            subtypeId,
            producedCultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            -semanticQ - semanticR);

    private void TrackCreatedProducedEntity(
        int produced,
        int createdEntityId,
        int[] createdEntityIds,
        ref int refCount,
        ref int createdEntityCount,
        int[] refCodes,
        int[] refEntityIds)
    {
        createdEntityIds[createdEntityCount] = createdEntityId;
        createdEntityCount++;
        SetRef(refCodes, refEntityIds, ref refCount, _producedRefCodes[produced], createdEntityId);
    }

    private bool TryResolveProducedCreationInputs(
        Simulation simulation,
        WorldState world,
        int produced,
        int targetId,
        int[] refCodes,
        int[] refEntityIds,
        int refCount,
        out int subtypeId,
        out int producedCultureId,
        out int placementAnchorId)
    {
        subtypeId = ResolveProducedSubtype(simulation, produced);
        producedCultureId = ResolveProducedCultureId(world, refCodes, refEntityIds, refCount, produced);
        placementAnchorId = ResolveRef(refCodes, refEntityIds, refCount, _producedPlacementRefCodes[produced]);
        if (placementAnchorId == 0)
        {
            placementAnchorId = targetId;
        }

        return subtypeId > 0 && producedCultureId > 0;
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
        int tick,
        out int failureReasonCode)
    {
        failureReasonCode = GeneratorTargetRejectionReasonCodes.None;
        if (_producedCoverageModeCodes[produced] == GeneratorCoverageModeCodes.None)
        {
            return true;
        }

        if (!CreateCoverage(simulation, produced, createdEntityId, refCodes, refEntityIds, refCount, physicalQ, physicalR, out var anchorQ, out var anchorR))
        {
            MarkCreatedEntitiesHistorical(simulation, world, createdEntityIds, createdEntityCount, tick);
            failureReasonCode = GeneratorTargetRejectionReasonCodes.Placement;
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

    private int ExecuteEffects(
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
        var relationshipSpendCount = 0;
        var targetCausalSpendLimitMultiplier = simulation.CultureSlots.CausalSpendLimitMultiplier(targetId);
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
                    relationshipSpendCount += RemoveTargetRelationships(
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
                    relationshipSpendCount += AdjustTargetRelationshipStrength(
                        simulation,
                        world,
                        targetId,
                        refOrKindId,
                        _effectValues[e],
                        _effectInstanceSelectionCodes[e],
                        _effectOptionCodes[e],
                        targetCausalSpendLimitMultiplier,
                        triggerKindCode,
                        triggerId);
                    break;
                default:
                    ApplyMapEffect(simulation, world, e, effectType, refOrKindId, refCodes, refEntityIds, refCount, tick);
                    break;
            }
        }

        return relationshipSpendCount;
    }

    private void ApplyMapEffect(
        Simulation simulation,
        WorldState world,
        int effectSlot,
        int effectType,
        int refOrKindId,
        int[] refCodes,
        int[] refEntityIds,
        int refCount,
        int tick)
    {
        switch (effectType)
        {
            case GeneratorEffectTypeCodes.SetTerrainPatch:
                ExecuteSetTerrainPatch(simulation, world, effectSlot, refCodes, refEntityIds, refCount);
                break;
            case GeneratorEffectTypeCodes.SetBindingTag:
                ExecuteSetBindingTag(simulation, effectSlot, refOrKindId, refCodes, refEntityIds, refCount, tick);
                break;
            case GeneratorEffectTypeCodes.ClaimSemanticJurisdiction:
                ExecuteClaimSemanticJurisdiction(simulation, effectSlot, refOrKindId, refCodes, refEntityIds, refCount);
                break;
            case GeneratorEffectTypeCodes.ErodeCultureAtTarget:
                ExecuteErodeCultureAtTarget(simulation, world, effectSlot, refOrKindId, refCodes, refEntityIds, refCount, tick);
                break;
        }
    }

    /// <summary>
    /// War leaves marks on the land: erode the resolved entity's culture
    /// influence around its physical position, strongest at the center.
    /// </summary>
    private void ExecuteErodeCultureAtTarget(
        Simulation simulation,
        WorldState world,
        int effectSlot,
        int refOrKindId,
        int[] refCodes,
        int[] refEntityIds,
        int refCount,
        int tick)
    {
        var entityId = ResolveRef(refCodes, refEntityIds, refCount, refOrKindId);
        if (entityId <= 0 || !world.EntityIsActive(entityId))
        {
            return;
        }

        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return;
        }

        var radius = _effectRadii[effectSlot];
        var amount = _effectValues[effectSlot];
        var centerQ = world.EntityPhysicalQ(entityId);
        var centerR = world.EntityPhysicalR(entityId);
        for (var dq = -radius; dq <= radius; dq++)
        {
            for (var dr = global::System.Math.Max(-radius, -dq - radius); dr <= global::System.Math.Min(radius, -dq + radius); dr++)
            {
                var distance = (global::System.Math.Abs(dq) + global::System.Math.Abs(dr) + global::System.Math.Abs(dq + dr)) / 2;
                var falloff = 1f - (distance / (radius + 1f));
                _ = simulation.ModifyCellCultureInfluenceRaw(
                    cultureId,
                    PlaneCodes.Physical,
                    centerQ + dq,
                    centerR + dr,
                    -amount * falloff,
                    tick);
            }
        }
    }

    // Claims unowned semantic cells within the radius of the ref entity's
    // semantic position — generator-born canons taking jurisdiction.
    private void ExecuteClaimSemanticJurisdiction(
        Simulation simulation,
        int effectSlot,
        int refOrKindId,
        int[] refCodes,
        int[] refEntityIds,
        int refCount)
    {
        var ownerId = ResolveRef(refCodes, refEntityIds, refCount, refOrKindId);
        if (ownerId <= 0)
        {
            return;
        }

        var world = simulation.World;
        var radius = _effectInstanceSelectionCodes[effectSlot];
        var centerQ = world.EntitySemanticQ(ownerId);
        var centerR = world.EntitySemanticR(ownerId);
        for (var dq = -radius; dq <= radius; dq++)
        {
            for (var dr = global::System.Math.Max(-radius, -dq - radius); dr <= global::System.Math.Min(radius, -dq + radius); dr++)
            {
                var q = centerQ + dq;
                var r = centerR + dr;
                if (world.CellIsKnown(PlaneCodes.Semantic, q, r) &&
                    world.CoverageOwnerAt(PlaneCodes.Semantic, q, r) == 0)
                {
                    KernelEffectExecutor.SetCoverageOwner(simulation, PlaneCodes.Semantic, q, r, ownerId);
                }
            }
        }
    }

    private void ExecuteSetBindingTag(
        Simulation simulation,
        int effectSlot,
        int refOrKindId,
        int[] refCodes,
        int[] refEntityIds,
        int refCount,
        int tick)
    {
        var taggedEntityId = ResolveRef(refCodes, refEntityIds, refCount, refOrKindId);
        if (taggedEntityId > 0)
        {
            _ = simulation.SetEntityTag(
                taggedEntityId,
                _effectInstanceSelectionCodes[effectSlot],
                _effectOptionCodes[effectSlot] != 0,
                tick);
        }
    }

    private static int RemoveTargetRelationships(
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
            return CullAllTargetRelationshipsOfKind(simulation, world, targetId, relationshipKindId);
        }

        return RemoveTriggerRelationshipInstance(
            simulation,
            world,
            relationshipKindId,
            optionCode,
            triggerKindCode,
            triggerId);
    }

    private static int RemoveTriggerRelationshipInstance(
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
            return 0;
        }

        var sourceId = world.RelationshipSourceId(triggerId);
        var targetEntityId = world.RelationshipTargetId(triggerId);
        var reciprocalSourceEntityId = targetEntityId;
        var reciprocalTargetEntityId = sourceId;
        var reciprocalId = optionCode == 0
            ? 0
            : KernelQuery.FindRelationship(world, reciprocalSourceEntityId, reciprocalTargetEntityId, relationshipKindId);

        var removed = 1;
        KernelEffectExecutor.CullRelationship(simulation, triggerId);
        if (reciprocalId > 0 && reciprocalId != triggerId && world.RelationshipIsActive(reciprocalId))
        {
            KernelEffectExecutor.CullRelationship(simulation, reciprocalId);
            removed++;
        }

        return removed;
    }

    private static int CullAllTargetRelationshipsOfKind(
        Simulation simulation,
        WorldState world,
        int targetId,
        int relationshipKindId)
    {
        var removed = 0;
        while (KernelQuery.TryFirstRelationshipOfKind(
                   world,
                   targetId,
                   relationshipKindId,
                   outgoing: true,
                   minimumStrength: 0f,
                   out var relationshipId))
        {
            KernelEffectExecutor.CullRelationship(simulation, relationshipId);
            removed++;
        }

        while (KernelQuery.TryFirstRelationshipOfKind(
                   world,
                   targetId,
                   relationshipKindId,
                   outgoing: false,
                   minimumStrength: 0f,
                   out var relationshipId))
        {
            KernelEffectExecutor.CullRelationship(simulation, relationshipId);
            removed++;
        }

        return removed;
    }

    private static int AdjustTargetRelationshipStrength(
        Simulation simulation,
        WorldState world,
        int targetId,
        int relationshipKindId,
        float delta,
        int instanceSelectionCode,
        int optionCode,
        int causalSpendLimitMultiplier,
        int triggerKindCode,
        int triggerId)
    {
        if (instanceSelectionCode == GeneratorRelationshipInstanceSelectionCodes.AllTargetRelationships)
        {
            return AdjustAllRelationshipsOfKind(
                simulation,
                world,
                targetId,
                relationshipKindId,
                delta,
                causalSpendLimitMultiplier);
        }

        return AdjustTriggerRelationshipInstance(
            simulation,
            world,
            relationshipKindId,
            delta,
            optionCode,
            causalSpendLimitMultiplier,
            triggerKindCode,
            triggerId);
    }

    private static int AdjustAllRelationshipsOfKind(
        Simulation simulation,
        WorldState world,
        int entityId,
        int relationshipKindId,
        float delta,
        int causalSpendLimitMultiplier)
    {
        var spendCount = 0;
        KernelQuery.ForEachRelatedEndpoint(
            world,
            entityId,
            relationshipKindId,
            minimumStrength: 0f,
            outgoing: true,
            (relationshipId, _) =>
            {
                spendCount += AdjustRelationshipStrength(
                    simulation,
                    world,
                    relationshipId,
                    delta,
                    causalSpendLimitMultiplier);
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
                    spendCount += AdjustRelationshipStrength(
                        simulation,
                        world,
                        relationshipId,
                        delta,
                        causalSpendLimitMultiplier);
                }

                return true;
            });
        return spendCount;
    }

    private static int AdjustTriggerRelationshipInstance(
        Simulation simulation,
        WorldState world,
        int relationshipKindId,
        float delta,
        int optionCode,
        int causalSpendLimitMultiplier,
        int triggerKindCode,
        int triggerId)
    {
        if (triggerKindCode != ProvenanceTriggerKindCodes.Relationship ||
            triggerId <= 0 ||
            !world.RelationshipIsActive(triggerId) ||
            world.RelationshipKindId(triggerId) != relationshipKindId)
        {
            return 0;
        }

        var sourceId = world.RelationshipSourceId(triggerId);
        var targetEntityId = world.RelationshipTargetId(triggerId);
        var reciprocalId = optionCode == 0
            ? 0
            : KernelQuery.FindRelationship(world, targetEntityId, sourceId, relationshipKindId);

        var spendCount = AdjustRelationshipStrength(
            simulation,
            world,
            triggerId,
            delta,
            causalSpendLimitMultiplier);
        if (reciprocalId > 0 && reciprocalId != triggerId && world.RelationshipIsActive(reciprocalId))
        {
            spendCount += AdjustRelationshipStrength(
                simulation,
                world,
                reciprocalId,
                delta,
                causalSpendLimitMultiplier);
        }

        return spendCount;
    }

    private static int AdjustRelationshipStrength(
        Simulation simulation,
        WorldState world,
        int relationshipId,
        float delta,
        int causalSpendLimitMultiplier)
    {
        var previous = KernelQuery.RelationshipStrength(world, relationshipId);
        var adjustedDelta = AdjustedCausalSpendDelta(delta, causalSpendLimitMultiplier);
        KernelEffectExecutor.SetRelationshipStrength(
            simulation,
            relationshipId,
            previous + adjustedDelta);
        return adjustedDelta < 0f && previous > previous + adjustedDelta ? 1 : 0;
    }

    private static float AdjustedCausalSpendDelta(float delta, int causalSpendLimitMultiplier) =>
        delta < 0f && causalSpendLimitMultiplier > 1
            ? delta / causalSpendLimitMultiplier
            : delta;

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
