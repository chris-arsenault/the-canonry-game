namespace Engine.Kernel;

using Engine.Constants;
using Engine.Eventing;

internal sealed partial class GambitProgramRuntime
{
    private sealed class TargetSelectionState(int candidateCount)
    {
        public int CandidateCount = candidateCount;
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

    private FixedScalar EvaluateTargets(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int tick,
        ref int candidateCount)
    {
        if (!ActorPreconditionsPass(simulation, gambitSlot, entityId, tick))
        {
            return 0;
        }

        var targetMode = _gambitTargetModes[gambitSlot];
        if (targetMode == BehaviorTargetModeCodes.Self)
        {
            return EvaluateSelfTarget(
                simulation,
                gambitSlot,
                entityId,
                tick,
                ref candidateCount);
        }

        return EvaluateOtherTargets(
            simulation,
            gambitSlot,
            entityId,
            tick,
            ref candidateCount);
    }

    private FixedScalar EvaluateOtherTargets(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int tick,
        ref int candidateCount)
    {
        if (_gambitRelationshipTargetDirectionCodes[gambitSlot] != 0)
        {
            return EvaluateRelationshipTargets(
                simulation,
                gambitSlot,
                entityId,
                tick,
                ref candidateCount);
        }

        var targetMode = _gambitTargetModes[gambitSlot];
        if (targetMode == BehaviorTargetModeCodes.KindOther && _gambitTargetKindIds[gambitSlot] > 0)
        {
            return EvaluateKindTargets(
                simulation,
                gambitSlot,
                entityId,
                tick,
                ref candidateCount);
        }

        return EvaluateAllTargets(
            simulation,
            gambitSlot,
            entityId,
            tick,
            ref candidateCount);
    }

    private FixedScalar EvaluateSelfTarget(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int tick,
        ref int candidateCount)
    {
        candidateCount++;
        return EvaluateCandidate(
            simulation,
            gambitSlot,
            entityId,
            entityId,
            tick);
    }

    private FixedScalar EvaluateKindTargets(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int tick,
        ref int candidateCount)
    {
        var total = FixedScalar.Zero;
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
                    state);
                return true;
            });

        candidateCount = state.CandidateCount;
        return total;
    }

    private FixedScalar EvaluateAllTargets(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int tick,
        ref int candidateCount)
    {
        var total = FixedScalar.Zero;
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
                    state);
                return true;
            });

        candidateCount = state.CandidateCount;
        return total;
    }

    private FixedScalar EvaluateTargetCandidate(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int targetId,
        int tick,
        TargetSelectionState state,
        bool relationshipTargetSourceSatisfied = false)
    {
        var world = simulation.World;
        if (!world.EntityIsActive(targetId) ||
            targetId == entityId ||
            !TargetEligibleForGambit(world, entityId, gambitSlot, forcedTargetId: 0, targetId))
        {
            return 0;
        }

        state.CandidateCount++;
        return EvaluateCandidate(
            simulation,
            gambitSlot,
            entityId,
            targetId,
            tick,
            relationshipTargetSourceSatisfied);
    }

    private FixedScalar EvaluateCandidate(
        Simulation simulation,
        int gambitSlot,
        int entityId,
        int targetId,
        int tick,
        bool relationshipTargetSourceSatisfied = false)
    {
        if (!CandidatePreconditionsPass(
                simulation,
                gambitSlot,
                entityId,
                targetId,
                tick,
                relationshipTargetSourceSatisfied))
        {
            return 0;
        }

        var score = (EvaluateScore(simulation.World, gambitSlot, entityId, targetId) +
            GambitScoreDecisionDelta(simulation.World, _gambitIds[gambitSlot], entityId, targetId) +
            simulation.Chronicles.ScoreBias(entityId, targetId, _gambitIds[gambitSlot])) *
            Data.AgeScoreMultipliers[gambitSlot] *
            simulation.Knowledge.Modifier(
                simulation.World.EntityCultureId(entityId),
                KnowledgeGrantDomainCodes.GambitScore,
                _gambitIds[gambitSlot]);
        if (score <= 0)
        {
            return 0;
        }

        _scratch.AddCandidate(gambitSlot, targetId, score);
        return score;
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
            entityWorkTierReader: simulation.World.EntityWorkTier,
            travelCostReader: simulation.TravelReachCost,
            knowledgeClaimReader: simulation.Knowledge.CultureClaimsTech);
        return KernelQuery.EvaluatePredicate(simulation.World, predicate, binding);
    }

}
