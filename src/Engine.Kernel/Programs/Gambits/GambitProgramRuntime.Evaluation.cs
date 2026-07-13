namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GambitProgramRuntime
{
    private sealed class TargetSelectionState(int candidateCount)
    {
        public int CandidateCount = candidateCount;
        public int SelectedTargetId;
    }

    private bool ProviderSelectorRequiresAttribute(int selector)
    {
        if (_providerSelectorValueIds[selector] == 0)
        {
            return false;
        }

        return _providerSelectorCodes[selector] is
            BehaviorProviderSelectorCodes.TargetAttribute or
            BehaviorProviderSelectorCodes.LocationAttribute or
            BehaviorProviderSelectorCodes.OutgoingRelationship or
            BehaviorProviderSelectorCodes.IncomingRelationship;
    }

    private bool TargetEligibleForGambit(
        WorldState world,
        int entityId,
        int gambitSlot,
        int forcedTargetId,
        int targetId)
    {
        if (!world.EntityIsActive(targetId))
        {
            return false;
        }

        if (forcedTargetId > 0)
        {
            return targetId == forcedTargetId;
        }

        var selectorStart = _gambitProviderSelectorStarts[gambitSlot];
        var selectorEnd = selectorStart + _gambitProviderSelectorCounts[gambitSlot];
        if (selectorEnd > selectorStart)
        {
            for (var selector = selectorStart; selector < selectorEnd; selector++)
            {
                if (ProviderSelectorMatchesTarget(world, entityId, targetId, selector))
                {
                    return true;
                }
            }

            return false;
        }

        return TargetModeMatchesGambit(world, entityId, gambitSlot, targetId);
    }

    private bool TargetModeMatchesGambit(WorldState world, int entityId, int gambitSlot, int targetId)
    {
        var targetMode = _gambitTargetModes[gambitSlot];
        if (targetMode == BehaviorTargetModeCodes.Self)
        {
            return targetId == entityId;
        }

        if (targetMode != BehaviorTargetModeCodes.KindOther)
        {
            return targetId != entityId;
        }

        return targetId != entityId &&
            _gambitTargetKindIds[gambitSlot] > 0 &&
            world.EntityKindId(targetId) == _gambitTargetKindIds[gambitSlot];
    }

    private bool ProviderSelectorMatchesTarget(WorldState world, int entityId, int targetId, int selector)
    {
        var selectorKindId = _providerSelectorKindIds[selector];
        if (selectorKindId > 0 && world.EntityKindId(targetId) != selectorKindId)
        {
            return false;
        }

        if (ProviderSelectorRequiresAttribute(selector) &&
            !world.HasAttribute(targetId, _providerSelectorValueIds[selector]))
        {
            return false;
        }

        return BasicProviderSelectorMatches(world, entityId, targetId, selector) ||
            RelationshipProviderSelectorMatches(world, entityId, targetId, selector);
    }

    private void ResolveGambitTriggerEvidence(
        WorldState world,
        int gambitSlot,
        int entityId,
        int targetId,
        out int triggerKindCode,
        out int triggerId,
        out int parentInvocationId)
    {
        if (TryResolveProviderTriggerEvidence(
                world,
                gambitSlot,
                entityId,
                targetId,
                out triggerKindCode,
                out triggerId,
                out parentInvocationId))
        {
            return;
        }

        triggerKindCode = ProvenanceTriggerKindCodes.Entity;
        triggerId = targetId;
        parentInvocationId = 0;
    }

    private bool TryResolveProviderTriggerEvidence(
        WorldState world,
        int gambitSlot,
        int entityId,
        int targetId,
        out int triggerKindCode,
        out int triggerId,
        out int parentInvocationId)
    {
        triggerKindCode = ProvenanceTriggerKindCodes.None;
        triggerId = 0;
        parentInvocationId = 0;
        var selectorStart = _gambitProviderSelectorStarts[gambitSlot];
        var selectorEnd = selectorStart + _gambitProviderSelectorCounts[gambitSlot];
        for (var selector = selectorStart; selector < selectorEnd; selector++)
        {
            if (TryResolveProviderSelectorTriggerEvidence(
                    world,
                    entityId,
                    targetId,
                    selector,
                    out triggerKindCode,
                    out triggerId,
                    out parentInvocationId))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryResolveProviderSelectorTriggerEvidence(
        WorldState world,
        int entityId,
        int targetId,
        int selector,
        out int triggerKindCode,
        out int triggerId,
        out int parentInvocationId)
    {
        triggerKindCode = ProvenanceTriggerKindCodes.None;
        triggerId = 0;
        parentInvocationId = 0;
        if (!ProviderSelectorMatchesTarget(world, entityId, targetId, selector))
        {
            return false;
        }

        return _providerSelectorCodes[selector] switch
        {
            BehaviorProviderSelectorCodes.OutgoingRelationship =>
                TryResolveProviderRelationshipTriggerEvidence(
                    world,
                    entityId,
                    targetId,
                    selector,
                    outgoing: true,
                    out triggerKindCode,
                    out triggerId,
                    out parentInvocationId),
            BehaviorProviderSelectorCodes.IncomingRelationship =>
                TryResolveProviderRelationshipTriggerEvidence(
                    world,
                    entityId,
                    targetId,
                    selector,
                    outgoing: false,
                    out triggerKindCode,
                    out triggerId,
                    out parentInvocationId),
            BehaviorProviderSelectorCodes.TargetAttribute or BehaviorProviderSelectorCodes.LocationAttribute =>
                ResolveProviderValueTrigger(
                    selector,
                    targetId,
                    ProvenanceTriggerKindCodes.Attribute,
                    out triggerKindCode,
                    out triggerId,
                    out parentInvocationId),
            _ => ResolveProviderEntityTrigger(
                targetId,
                out triggerKindCode,
                out triggerId,
                out parentInvocationId),
        };
    }

    private bool ResolveProviderValueTrigger(
        int selector,
        int targetId,
        int valueTriggerKindCode,
        out int triggerKindCode,
        out int triggerId,
        out int parentInvocationId)
    {
        var valueId = _providerSelectorValueIds[selector];
        triggerKindCode = valueId > 0 ? valueTriggerKindCode : ProvenanceTriggerKindCodes.Entity;
        triggerId = valueId > 0 ? valueId : targetId;
        parentInvocationId = 0;
        return true;
    }

    private static bool ResolveProviderEntityTrigger(
        int targetId,
        out int triggerKindCode,
        out int triggerId,
        out int parentInvocationId)
    {
        triggerKindCode = ProvenanceTriggerKindCodes.Entity;
        triggerId = targetId;
        parentInvocationId = 0;
        return true;
    }

    private bool TryResolveProviderRelationshipTriggerEvidence(
        WorldState world,
        int entityId,
        int targetId,
        int selector,
        bool outgoing,
        out int triggerKindCode,
        out int triggerId,
        out int parentInvocationId)
    {
        triggerKindCode = ProvenanceTriggerKindCodes.None;
        triggerId = 0;
        parentInvocationId = 0;
        var sourceId = outgoing ? entityId : targetId;
        var destinationId = outgoing ? targetId : entityId;
        if (!KernelQuery.TryFindRelationship(
                world,
                sourceId,
                destinationId,
                _providerSelectorRelationshipKindIds[selector],
                hasMinimumStrength: 1,
                _providerSelectorMinimumStrengths[selector],
                out var relationshipId))
        {
            return false;
        }

        triggerKindCode = ProvenanceTriggerKindCodes.Relationship;
        triggerId = relationshipId;
        parentInvocationId = world.RelationshipCreatedByInvocationId(relationshipId);
        return true;
    }

    private bool BasicProviderSelectorMatches(WorldState world, int entityId, int targetId, int selector)
    {
        var code = _providerSelectorCodes[selector];
        if (code == BehaviorProviderSelectorCodes.TargetAttribute ||
            code == BehaviorProviderSelectorCodes.Structural)
        {
            return true;
        }

        return code == BehaviorProviderSelectorCodes.LocationAttribute &&
            world.EntityPhysicalQ(entityId) == world.EntityPhysicalQ(targetId) &&
            world.EntityPhysicalR(entityId) == world.EntityPhysicalR(targetId);
    }

    private bool RelationshipProviderSelectorMatches(WorldState world, int entityId, int targetId, int selector)
    {
        var code = _providerSelectorCodes[selector];
        if (code == BehaviorProviderSelectorCodes.OutgoingRelationship)
        {
            return RelationshipSelectorMatchesTarget(world, entityId, targetId, selector);
        }

        return code == BehaviorProviderSelectorCodes.IncomingRelationship &&
            RelationshipSelectorMatchesTarget(world, targetId, entityId, selector);
    }

    private bool RelationshipSelectorMatchesTarget(WorldState world, int sourceId, int targetId, int selector)
    {
        var relationshipKindId = _providerSelectorRelationshipKindIds[selector];
        if (relationshipKindId <= 0)
        {
            return false;
        }

        return KernelQuery.RelationshipPasses(
            world,
            sourceId,
            targetId,
            relationshipKindId,
            hasMinimumStrength: 1,
            _providerSelectorMinimumStrengths[selector]);
    }

    private float EvaluateTargets(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int tick,
        ref int candidateCount,
        float selectRoll,
        out int selectedTargetId)
    {
        selectedTargetId = 0;
        var targetMode = _gambitTargetModes[gambitSlot];
        if (targetMode == BehaviorTargetModeCodes.Self)
        {
            return EvaluateSelfTarget(
                simulation,
                gambitSlot,
                entityId,
                tick,
                ref candidateCount,
                selectRoll,
                out selectedTargetId);
        }

        if (targetMode == BehaviorTargetModeCodes.KindOther && _gambitTargetKindIds[gambitSlot] > 0)
        {
            return EvaluateKindTargets(
                simulation,
                gambitSlot,
                entityId,
                tick,
                ref candidateCount,
                selectRoll,
                out selectedTargetId);
        }

        return EvaluateAllTargets(
            simulation,
            gambitSlot,
            entityId,
            tick,
            ref candidateCount,
            selectRoll,
            out selectedTargetId);
    }

    private float EvaluateSelfTarget(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int tick,
        ref int candidateCount,
        float selectRoll,
        out int selectedTargetId)
    {
        candidateCount++;
        return EvaluateCandidate(
            simulation,
            gambitSlot,
            entityId,
            entityId,
            tick,
            selectRoll,
            out selectedTargetId);
    }

    private float EvaluateKindTargets(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int tick,
        ref int candidateCount,
        float selectRoll,
        out int selectedTargetId)
    {
        selectedTargetId = 0;
        var total = 0f;
        var world = simulation.World;
        var state = new TargetSelectionState(candidateCount);
        KernelQuery.ForEachActiveEntityOfKind(
            world,
            _gambitTargetKindIds[gambitSlot],
            targetId =>
            {
                total += EvaluateTargetCandidate(
                    simulation,
                    gambitSlot,
                    entityId,
                    targetId,
                    tick,
                    state,
                    selectRoll,
                    total);
                return state.SelectedTargetId == 0;
            });

        candidateCount = state.CandidateCount;
        selectedTargetId = state.SelectedTargetId;
        return total;
    }

    private float EvaluateAllTargets(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int tick,
        ref int candidateCount,
        float selectRoll,
        out int selectedTargetId)
    {
        selectedTargetId = 0;
        var total = 0f;
        var world = simulation.World;
        var state = new TargetSelectionState(candidateCount);
        KernelQuery.ForEachActiveEntity(
            world,
            targetId =>
            {
                total += EvaluateTargetCandidate(
                    simulation,
                    gambitSlot,
                    entityId,
                    targetId,
                    tick,
                    state,
                    selectRoll,
                    total);
                return state.SelectedTargetId == 0;
            });

        candidateCount = state.CandidateCount;
        selectedTargetId = state.SelectedTargetId;
        return total;
    }

    private float EvaluateTargetCandidate(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int targetId,
        int tick,
        TargetSelectionState state,
        float selectRoll,
        float total)
    {
        var world = simulation.World;
        if (!world.EntityIsActive(targetId) ||
            targetId == entityId ||
            !TargetEligibleForGambit(world, entityId, gambitSlot, forcedTargetId: 0, targetId))
        {
            return 0f;
        }

        state.CandidateCount++;
        var score = EvaluateCandidate(
            simulation,
            gambitSlot,
            entityId,
            targetId,
            tick,
            selectRoll < 0f ? -1f : selectRoll - total,
            out var selectedTargetId);
        if (selectedTargetId > 0)
        {
            state.SelectedTargetId = selectedTargetId;
        }

        return score;
    }

    private float EvaluateCandidate(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int targetId,
        int tick,
        float selectRoll,
        out int selectedTargetId)
    {
        selectedTargetId = 0;
        var preconditionResult = PreconditionsResult(simulation, gambitSlot, entityId, targetId, tick);
        if (preconditionResult == PreconditionFailed)
        {
            return 0f;
        }

        var score = EvaluateScore(simulation.World, gambitSlot, entityId, targetId) +
            GambitScoreDecisionDelta(simulation.World, _gambitIds[gambitSlot], entityId, targetId) +
            simulation.Stories.ScoreBias(entityId, targetId, _gambitIds[gambitSlot]);
        var recentActionCount = CountRecentActions(
            entityId,
            _gambitIds[gambitSlot],
            tick - Tuning.ScaleMainTicks(RecencyWindow));
        for (var i = 0; i < recentActionCount; i++)
        {
            score *= RecencyDecayFactor;
        }

        if (score <= 0f)
        {
            return 0f;
        }

        if (selectRoll >= 0f && selectRoll <= score)
        {
            selectedTargetId = targetId;
        }

        return score;
    }

    private int PreconditionsResult(Simulation simulation, int gambitSlot, int entityId, int targetId, int tick)
    {
        return PreconditionsResult(
            simulation,
            gambitSlot,
            entityId,
            targetId,
            tick,
            out _,
            out _,
            out _,
            out _);
    }

    private int PreconditionsResult(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int targetId,
        int tick,
        out int blockerScopeCode,
        out int blockerValueId,
        out int blockerKindId,
        out int blockerTargetId)
    {
        var world = simulation.World;
        blockerScopeCode = 0;
        blockerValueId = 0;
        blockerKindId = 0;
        blockerTargetId = 0;
        var first = _gambitPreconditionStarts[gambitSlot];
        var end = first + _gambitPreconditionCounts[gambitSlot];
        for (var pre = first; pre < end; pre++)
        {
            if (KernelQuery.QueryPredicateIsComposition(_preconditionCodes[pre]))
            {
                return ComposedPreconditionsResult(
                    simulation,
                    gambitSlot,
                    entityId,
                    targetId,
                    tick,
                    first,
                    end,
                    out blockerScopeCode,
                    out blockerValueId,
                    out blockerKindId,
                    out blockerTargetId);
            }
        }

        for (var pre = first; pre < end; pre++)
        {
            if (!EvaluateGambitPredicate(simulation, gambitSlot, pre, entityId, targetId, tick))
            {
                ResolvePreconditionBlocker(
                    world,
                    pre,
                    entityId,
                    targetId,
                    out blockerScopeCode,
                    out blockerValueId,
                    out blockerKindId,
                    out blockerTargetId);
                return PreconditionFailed;
            }
        }

        return PreconditionPassed;
    }

    private int ComposedPreconditionsResult(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int targetId,
        int tick,
        int first,
        int end,
        out int blockerScopeCode,
        out int blockerValueId,
        out int blockerKindId,
        out int blockerTargetId)
    {
        blockerScopeCode = 0;
        blockerValueId = 0;
        blockerKindId = 0;
        blockerTargetId = 0;
        global::System.Span<bool> stack = _preconditionStack;
        var stackCount = 0;
        var failedBlockerPre = 0;
        for (var pre = first; pre < end; pre++)
        {
            if (KernelQuery.TryApplyCompositionPredicate(_preconditionCodes[pre], stack, ref stackCount))
            {
                continue;
            }

            var passes = EvaluateGambitPredicate(simulation, gambitSlot, pre, entityId, targetId, tick);
            if (!passes && failedBlockerPre == 0)
            {
                failedBlockerPre = pre;
            }

            PushBool(stack, ref stackCount, passes);
        }

        var final = true;
        while (stackCount > 0)
        {
            final = PopBool(stack, ref stackCount) && final;
        }

        if (final)
        {
            return PreconditionPassed;
        }

        if (failedBlockerPre != 0)
        {
            ResolvePreconditionBlocker(
                simulation.World,
                failedBlockerPre,
                entityId,
                targetId,
                out blockerScopeCode,
                out blockerValueId,
                out blockerKindId,
                out blockerTargetId);
            return PreconditionFailed;
        }

        return PreconditionFailed;
    }

    private bool EvaluateGambitPredicate(Simulation simulation, int gambitSlot, int pre, int entityId, int targetId, int tick)
    {
        var predicate = new QueryPredicate(
            _preconditionCodes[pre],
            _preA[pre],
            _preB[pre],
            _preC[pre],
            _preD[pre],
            _preX[pre],
            _preY[pre]);
        var binding = new QueryBinding(
            entityId,
            targetId,
            tick,
            _gambitIds[gambitSlot],
            recentActionCounter: CountRecentActions,
            entityWorkTierReader: simulation.MinorActors.WorkTier);
        return KernelQuery.EvaluatePredicate(simulation.World, predicate, binding);
    }

    private void ResolvePreconditionBlocker(
        WorldState world,
        int pre,
        int entityId,
        int targetId,
        out int blockerScopeCode,
        out int blockerValueId,
        out int blockerKindId,
        out int blockerTargetId)
    {
        blockerScopeCode = 0;
        blockerValueId = 0;
        blockerKindId = 0;
        blockerTargetId = 0;
        var code = _preconditionCodes[pre];
        _ = TryResolveAttributeBlocker(world, code, pre, entityId, targetId, ref blockerScopeCode, ref blockerValueId, ref blockerKindId, ref blockerTargetId) ||
            TryResolveContextBlocker(world, code, pre, entityId, targetId, ref blockerScopeCode, ref blockerValueId, ref blockerKindId, ref blockerTargetId);
    }

    private bool TryResolveAttributeBlocker(
        WorldState world,
        int code,
        int pre,
        int entityId,
        int targetId,
        ref int blockerScopeCode,
        ref int blockerValueId,
        ref int blockerKindId,
        ref int blockerTargetId)
    {
        if (code == BehaviorPreconditionCodes.SelfAttributeRange)
        {
            blockerScopeCode = BehaviorEffectScopeCodes.SelfAttribute;
            blockerValueId = _preA[pre];
            blockerKindId = world.EntityKindId(entityId);
            blockerTargetId = entityId;
            return true;
        }

        if (code != BehaviorPreconditionCodes.TargetAttributeRange)
        {
            return false;
        }

        blockerScopeCode = BehaviorEffectScopeCodes.TargetAttribute;
        blockerValueId = _preA[pre];
        blockerKindId = world.EntityKindId(targetId);
        blockerTargetId = targetId;
        return true;
    }

    private bool TryResolveContextBlocker(
        WorldState world,
        int code,
        int pre,
        int entityId,
        int targetId,
        ref int blockerScopeCode,
        ref int blockerValueId,
        ref int blockerKindId,
        ref int blockerTargetId)
    {
        if (code == BehaviorPreconditionCodes.PlaneDistance)
        {
            blockerScopeCode = BehaviorEffectScopeCodes.Structural;
            blockerValueId = _preC[pre];
            blockerKindId = world.EntityKindId(entityId);
            blockerTargetId = targetId != entityId ? targetId : entityId;
            return true;
        }

        if (code == BehaviorPreconditionCodes.ContestedInfluence)
        {
            blockerScopeCode = BehaviorEffectScopeCodes.Structural;
            blockerValueId = _preA[pre];
            blockerKindId = world.EntityKindId(targetId);
            blockerTargetId = targetId;
            return true;
        }

        if (code == BehaviorPreconditionCodes.SharedInfluence)
        {
            blockerScopeCode = BehaviorEffectScopeCodes.Structural;
            blockerValueId = _preA[pre];
            blockerKindId = world.EntityKindId(targetId);
            blockerTargetId = targetId;
            return true;
        }

        return TryResolveRelationshipBlocker(world, code, pre, targetId, ref blockerScopeCode, ref blockerValueId, ref blockerKindId, ref blockerTargetId);
    }

    private bool TryResolveRelationshipBlocker(
        WorldState world,
        int code,
        int pre,
        int targetId,
        ref int blockerScopeCode,
        ref int blockerValueId,
        ref int blockerKindId,
        ref int blockerTargetId)
    {
        if (code != BehaviorPreconditionCodes.SelfHasRelationshipToTarget &&
            code != BehaviorPreconditionCodes.TargetHasRelationshipToSelf &&
            code != BehaviorPreconditionCodes.SelfHasOutgoingRelationship &&
            code != BehaviorPreconditionCodes.SelfHasOutgoingRelationshipToKind &&
            code != BehaviorPreconditionCodes.EndpointRelationshipCountBelow &&
            code != BehaviorPreconditionCodes.RelatedEntityMatchesTarget &&
            code != BehaviorPreconditionCodes.RelationshipExists)
        {
            return false;
        }

        blockerScopeCode = BehaviorEffectScopeCodes.Relational;
        blockerValueId = _preA[pre];
        blockerKindId = world.EntityKindId(targetId);
        blockerTargetId = targetId;
        return true;
    }

}
