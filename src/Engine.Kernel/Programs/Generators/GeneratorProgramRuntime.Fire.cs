namespace Engine.Kernel;

using Engine.Constants;
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
        var targetOccurrenceHandle = world.EntityOccurrenceHandle(targetId);
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

        _plannedCreateCount = 0;
        _plannedCoverageCellCount = 0;
        if (!PlanProducedEntities(
                simulation,
                world,
                generatorSlot,
                targetId,
                targetOriginQ,
                targetOriginR,
                refCodes,
                refEntityIds,
                ref refCount,
                out failureReasonCode))
        {
            _plannedCreateCount = 0;
            _plannedCoverageCellCount = 0;
            return false;
        }

        createdEntityCount = CommitPlannedEntities(simulation, refCodes, refEntityIds, ref refCount, createdEntityIds);

        var createdRelationshipCount = CreateRelationships(simulation, world, generatorSlot, refCodes, refEntityIds, refCount, tick);
        var relationshipSpendCount = ExecuteEffects(simulation, world, generatorSlot, targetId, refCodes, refEntityIds, refCount, tick, triggerKindCode, triggerId);
        _plannedCreateCount = 0;
        _plannedCoverageCellCount = 0;
        _runCounts[generatorSlot]++;
        WriteGeneratorFired(simulation, tick, generatorSlot, targetOccurrenceHandle, createdEntityCount, createdRelationshipCount, relationshipSpendCount);
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
        FixedScalar minimumStrength,
        bool outgoing,
        out int relationshipId) =>
        KernelQuery.TryFirstRelationshipOfKind(world, targetId, relationshipKindId, outgoing, minimumStrength, out relationshipId);

    private bool PlanProducedEntities(
        Simulation simulation,
        WorldState world,
        int generatorSlot,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        int[] refCodes,
        int[] refEntityIds,
        ref int refCount,
        out int failureReasonCode)
    {
        failureReasonCode = GeneratorTargetRejectionReasonCodes.None;
        var firstProduced = _producedStarts[generatorSlot];
        var endProduced = firstProduced + _producedCounts[generatorSlot];
        for (var produced = firstProduced; produced < endProduced; produced++)
        {
            if (!PlanProducedEntity(
                    simulation,
                    world,
                    produced,
                    targetId,
                    targetOriginQ,
                    targetOriginR,
                    refCodes,
                    refEntityIds,
                    ref refCount,
                    out failureReasonCode))
            {
                return false;
            }
        }

        return true;
    }

    private bool PlanProducedEntity(
        Simulation simulation,
        WorldState world,
        int produced,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        int[] refCodes,
        int[] refEntityIds,
        ref int refCount,
        out int failureReasonCode)
    {
        failureReasonCode = GeneratorTargetRejectionReasonCodes.None;
        // Kind-general generators carry one gated produce slot per actor
        // kind; only the slot matching the resolved target's kind fires.
        if (_producedOnlyWhenTargetKindIds[produced] != 0 &&
            (targetId <= 0 || world.EntityKindId(targetId) != _producedOnlyWhenTargetKindIds[produced]))
        {
            return true;
        }

        if (_producedCreateChances[produced] < 1 &&
            simulation.RandomScalar(
                targetId > 0 ? world.EntityOccurrenceHandle(targetId) : 0,
                produced,
                KernelRandomPurposeCodes.GeneratorCreateChance,
                attempt: _producedRefCodes[produced]) > _producedCreateChances[produced])
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

        return PlanMissingProducedEntity(
            simulation,
            world,
            produced,
            targetId,
            refCodes,
            refEntityIds,
            ref refCount,
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
            _producedQueryRequireTargetCultureRoute[produced],
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

    private bool PlanMissingProducedEntity(
        Simulation simulation,
        WorldState world,
        int produced,
        int targetId,
        int[] refCodes,
        int[] refEntityIds,
        ref int refCount,
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
            return false;
        }

        var planIndex = RecordPlannedProducedEntity(produced, subtypeId, producedCultureId, physicalQ, physicalR, semanticQ, semanticR);
        if (_producedCoverageModeCodes[produced] != GeneratorCoverageModeCodes.None)
        {
            if (!PlanCoverage(
                    simulation,
                    produced,
                    producedCultureId,
                    refCodes,
                    refEntityIds,
                    refCount,
                    physicalQ,
                    physicalR,
                    out var anchorQ,
                    out var anchorR,
                    out var coverageCellCount))
            {
                failureReasonCode = GeneratorTargetRejectionReasonCodes.Placement;
                return false;
            }

            _plannedCoverageCounts[planIndex] = coverageCellCount;
            _plannedFinalPhysicalQ[planIndex] = anchorQ;
            _plannedFinalPhysicalR[planIndex] = anchorR;
        }

        _plannedCreateCount++;
        SetRef(refCodes, refEntityIds, ref refCount, _producedRefCodes[produced], PendingRefId(planIndex));
        return true;
    }

    private int RecordPlannedProducedEntity(
        int produced,
        int subtypeId,
        int producedCultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR)
    {
        var planIndex = _plannedCreateCount;
        if (planIndex >= _plannedProducedSlots.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _plannedProducedSlots[planIndex] = produced;
        _plannedSubtypeIds[planIndex] = subtypeId;
        _plannedCultureIds[planIndex] = producedCultureId;
        _plannedSpawnPhysicalQ[planIndex] = physicalQ;
        _plannedSpawnPhysicalR[planIndex] = physicalR;
        _plannedFinalPhysicalQ[planIndex] = physicalQ;
        _plannedFinalPhysicalR[planIndex] = physicalR;
        _plannedSemanticQ[planIndex] = semanticQ;
        _plannedSemanticR[planIndex] = semanticR;
        _plannedCoverageStarts[planIndex] = _plannedCoverageCellCount;
        _plannedCoverageCounts[planIndex] = 0;
        return planIndex;
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
                case GeneratorEffectTypeCodes.DemoteBindingStage:
                    ApplyBindingEntityEffect(simulation, effectType, refCodes, refEntityIds, refCount, refOrKindId);
                    break;

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

    private static void ApplyBindingEntityEffect(
        Simulation simulation,
        int effectType,
        int[] refCodes,
        int[] refEntityIds,
        int refCount,
        int refOrKindId)
    {
        var bindingEntityId = ResolveRef(refCodes, refEntityIds, refCount, refOrKindId);
        if (bindingEntityId <= 0)
        {
            return;
        }

        if (effectType == GeneratorEffectTypeCodes.SetBindingHistorical)
        {
            KernelEffectExecutor.SetEntityStatus(simulation, bindingEntityId, EntityStatusCodes.Historical);
            return;
        }

        _ = simulation.DecreaseEntityWorkTier(bindingEntityId);
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
        if (entityId <= 0 ||
            (!TryPlannedEntity(entityId, out _) &&
             !world.EntityIsActive(entityId)))
        {
            return;
        }

        var cultureId = PlannedOrWorldCultureId(world, entityId);
        if (cultureId <= 0)
        {
            return;
        }

        var radius = _effectRadii[effectSlot];
        var amount = _effectValues[effectSlot];
        var centerQ = PlannedOrWorldQ(
            world,
            entityId,
            PlaneCodes.Physical);
        var centerR = PlannedOrWorldR(
            world,
            entityId,
            PlaneCodes.Physical);
        for (var dq = -radius; dq <= radius; dq++)
        {
            for (var dr = global::System.Math.Max(-radius, -dq - radius); dr <= global::System.Math.Min(radius, -dq + radius); dr++)
            {
                var distance = (global::System.Math.Abs(dq) + global::System.Math.Abs(dr) + global::System.Math.Abs(dq + dr)) / 2;
                var falloff = 1 - FixedScalar.FromRatio(distance, radius + 1);
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
        var centerQ = PlannedOrWorldQ(
            world,
            ownerId,
            PlaneCodes.Semantic);
        var centerR = PlannedOrWorldR(
            world,
            ownerId,
            PlaneCodes.Semantic);
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
        var collected = 0;
        var outgoing = world.OutgoingRelationshipsOfKind(
            targetId,
            relationshipKindId);
        while (world.MoveNextRelationship(
                   ref outgoing,
                   out var relationshipId))
        {
            KernelEffectExecutor.CullRelationship(simulation, relationshipId);
            collected++;
        }

        var incoming = world.IncomingRelationshipsOfKind(
            targetId,
            relationshipKindId);
        while (world.MoveNextRelationship(
                   ref incoming,
                   out var relationshipId))
        {
            if (world.RelationshipSourceId(relationshipId) == targetId)
            {
                continue;
            }

            KernelEffectExecutor.CullRelationship(simulation, relationshipId);
            collected++;
        }

        return collected;
    }

    private static int AdjustTargetRelationshipStrength(
        Simulation simulation,
        WorldState world,
        int targetId,
        int relationshipKindId,
        FixedScalar delta,
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
        FixedScalar delta,
        int causalSpendLimitMultiplier)
    {
        var spendCount = 0;
        KernelQuery.ForEachRelatedEndpoint(
            world,
            entityId,
            relationshipKindId,
            minimumStrength: 0,
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
            minimumStrength: 0,
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
        FixedScalar delta,
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
        FixedScalar delta,
        int causalSpendLimitMultiplier)
    {
        var previous = KernelQuery.RelationshipStrength(world, relationshipId);
        var adjustedDelta = AdjustedCausalSpendDelta(delta, causalSpendLimitMultiplier);
        KernelEffectExecutor.ModifyRelationshipStrengthUnits(
            simulation,
            relationshipId,
            KernelFixedPoint.RelationshipStrengthDeltaUnits(adjustedDelta));
        return adjustedDelta < 0 && previous > previous + adjustedDelta ? 1 : 0;
    }

    private static FixedScalar AdjustedCausalSpendDelta(FixedScalar delta, int causalSpendLimitMultiplier) =>
        delta < 0 && causalSpendLimitMultiplier > 1
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
