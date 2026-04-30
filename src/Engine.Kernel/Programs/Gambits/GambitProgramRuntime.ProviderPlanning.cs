namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GambitProgramRuntime
{
    private float EvaluateProviderSelectorTargets(
        Simulation simulation,
        int entityId,
        int gambitSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selectorStart,
        int selectorCount,
        ref int candidateCount,
        out int selectedRootGambitSlot,
        out int selectedRootTargetId,
        out int selectedGambitSlot,
        out int selectedTargetId)
    {
        selectedRootGambitSlot = 0;
        selectedRootTargetId = 0;
        selectedGambitSlot = 0;
        selectedTargetId = 0;
        var bestWeight = 0f;
        _gambitVisited.BeginEpoch();
        var selectorEnd = selectorStart + selectorCount;
        for (var selector = selectorStart; selector < selectorEnd; selector++)
        {
            bestWeight = EvaluateProviderSelectorTargets(
                simulation,
                entityId,
                gambitSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selector,
                bestWeight,
                ref candidateCount,
                ref selectedRootGambitSlot,
                ref selectedRootTargetId,
                ref selectedGambitSlot,
                ref selectedTargetId);
        }

        return bestWeight;
    }

    private float EvaluateProviderSelectorTargets(Simulation simulation, int entityId, int gambitSlot, int tick, int effectScopeCode, int effectValueId, int effectKindId, float urgency, int depth, int selector, float bestWeight, ref int candidateCount, ref int selectedRootGambitSlot, ref int selectedRootTargetId, ref int selectedGambitSlot, ref int selectedTargetId)
    {
        var code = _providerSelectorCodes[selector];
        if (code == BehaviorProviderSelectorCodes.TargetAttribute ||
            code == BehaviorProviderSelectorCodes.Structural ||
            code == BehaviorProviderSelectorCodes.Resource)
        {
            return EvaluateKindProviderSelector(
                simulation, entityId, gambitSlot, tick, effectScopeCode, effectValueId, effectKindId,
                urgency, depth, selector, bestWeight,
                ref candidateCount, ref selectedRootGambitSlot, ref selectedRootTargetId, ref selectedGambitSlot, ref selectedTargetId);
        }

        if (code == BehaviorProviderSelectorCodes.LocationAttribute)
        {
            return EvaluateLocationProviderSelector(
                simulation, entityId, gambitSlot, tick, effectScopeCode, effectValueId, effectKindId,
                urgency, depth, selector, bestWeight,
                ref candidateCount, ref selectedRootGambitSlot, ref selectedRootTargetId, ref selectedGambitSlot, ref selectedTargetId);
        }

        if (code == BehaviorProviderSelectorCodes.OutgoingRelationship ||
            code == BehaviorProviderSelectorCodes.IncomingRelationship)
        {
            return EvaluateRelationshipProviderSelector(
                simulation, entityId, gambitSlot, tick, effectScopeCode, effectValueId, effectKindId,
                urgency, depth, selector, code == BehaviorProviderSelectorCodes.OutgoingRelationship, bestWeight,
                ref candidateCount, ref selectedRootGambitSlot, ref selectedRootTargetId, ref selectedGambitSlot, ref selectedTargetId);
        }

        throw new global::System.InvalidOperationException();
    }

    private float EvaluateLocationProviderSelector(
        Simulation simulation,
        int entityId,
        int gambitSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selector,
        float bestWeight,
        ref int candidateCount,
        ref int selectedRootGambitSlot,
        ref int selectedRootTargetId,
        ref int selectedGambitSlot,
        ref int selectedTargetId)
    {
        var world = simulation.World;
        for (var targetId = world.FirstPhysicalEntityAt(world.EntityPhysicalQ(entityId), world.EntityPhysicalR(entityId));
             targetId > 0;
             targetId = world.NextPhysicalEntityAt(targetId))
        {
            bestWeight = EvaluateProviderTargetIfMatched(
                simulation,
                entityId,
                targetId,
                gambitSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selector,
                bestWeight,
                ref candidateCount,
                ref selectedRootGambitSlot,
                ref selectedRootTargetId,
                ref selectedGambitSlot,
                ref selectedTargetId);
        }

        return bestWeight;
    }

    private float EvaluateKindProviderSelector(
        Simulation simulation,
        int entityId,
        int gambitSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selector,
        float bestWeight,
        ref int candidateCount,
        ref int selectedRootGambitSlot,
        ref int selectedRootTargetId,
        ref int selectedGambitSlot,
        ref int selectedTargetId)
    {
        var world = simulation.World;
        var kindId = _providerSelectorKindIds[selector];
        if (kindId > 0)
        {
            for (var targetId = world.FirstEntityOfKind(kindId);
                 targetId > 0;
                 targetId = world.NextEntityOfKind(targetId))
            {
                bestWeight = EvaluateProviderTargetIfMatched(
                    simulation,
                    entityId,
                    targetId,
                    gambitSlot,
                    tick,
                    effectScopeCode,
                    effectValueId,
                    effectKindId,
                    urgency,
                    depth,
                    selector,
                    bestWeight,
                    ref candidateCount,
                    ref selectedRootGambitSlot,
                    ref selectedRootTargetId,
                    ref selectedGambitSlot,
                    ref selectedTargetId);
            }

            return bestWeight;
        }

        for (var targetId = 1; targetId <= world.EntityHighWatermark; targetId++)
        {
            bestWeight = EvaluateProviderTargetIfMatched(
                simulation,
                entityId,
                targetId,
                gambitSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selector,
                bestWeight,
                ref candidateCount,
                ref selectedRootGambitSlot,
                ref selectedRootTargetId,
                ref selectedGambitSlot,
                ref selectedTargetId);
        }

        return bestWeight;
    }

    private float EvaluateRelationshipProviderSelector(
        Simulation simulation,
        int entityId,
        int gambitSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selector,
        bool outgoing,
        float bestWeight,
        ref int candidateCount,
        ref int selectedRootGambitSlot,
        ref int selectedRootTargetId,
        ref int selectedGambitSlot,
        ref int selectedTargetId)
    {
        var world = simulation.World;
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKindAbove(
                entityId,
                _providerSelectorRelationshipKindIds[selector],
                _providerSelectorMinimumStrengths[selector])
            : world.IncomingRelationshipsOfKindAbove(
                entityId,
                _providerSelectorRelationshipKindIds[selector],
                _providerSelectorMinimumStrengths[selector]);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            if (!world.RelationshipHasFact(relationshipId, _providerSelectorFactIds[selector]))
            {
                continue;
            }

            var targetId = outgoing
                ? world.RelationshipTargetId(relationshipId)
                : world.RelationshipSourceId(relationshipId);
            bestWeight = EvaluateProviderTargetIfMatched(
                simulation,
                entityId,
                targetId,
                gambitSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selector,
                bestWeight,
                ref candidateCount,
                ref selectedRootGambitSlot,
                ref selectedRootTargetId,
                ref selectedGambitSlot,
                ref selectedTargetId);
        }

        return bestWeight;
    }

    private float EvaluateProviderTargetIfMatched(
        Simulation simulation,
        int entityId,
        int targetId,
        int gambitSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selector,
        float bestWeight,
        ref int candidateCount,
        ref int selectedRootGambitSlot,
        ref int selectedRootTargetId,
        ref int selectedGambitSlot,
        ref int selectedTargetId)
    {
        var world = simulation.World;
        if (!world.EntityIsActive(targetId) || _gambitVisited.Contains(targetId))
        {
            return bestWeight;
        }

        var selectorKindId = _providerSelectorKindIds[selector];
        if (selectorKindId > 0 && world.EntityKindId(targetId) != selectorKindId)
        {
            return bestWeight;
        }

        if (ProviderSelectorRequiresAttribute(selector) &&
            !world.HasAttribute(targetId, _providerSelectorValueIds[selector]))
        {
            return bestWeight;
        }

        if (_providerSelectorCodes[selector] == BehaviorProviderSelectorCodes.Resource &&
            world.GetResource(
                _providerSelectorValueIds[selector],
                world.EntityPhysicalQ(targetId),
                world.EntityPhysicalR(targetId)) <= 0f)
        {
            return bestWeight;
        }

        _gambitVisited.Add(targetId);
        candidateCount++;
        var weight = EvaluatePlanCandidate(
            simulation,
            entityId,
            targetId,
            gambitSlot,
            tick,
            effectScopeCode,
            effectValueId,
            effectKindId,
            urgency,
            depth,
            ref candidateCount,
            out var planGambitSlot,
            out var planTargetId);
        if (weight > bestWeight)
        {
            selectedRootGambitSlot = gambitSlot;
            selectedRootTargetId = targetId;
            selectedGambitSlot = planGambitSlot;
            selectedTargetId = planTargetId;
            return weight;
        }

        return bestWeight;
    }

    private float EvaluatePlanCandidate(
        Simulation simulation,
        int entityId,
        int targetId,
        int gambitSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        ref int candidateCount,
        out int selectedGambitSlot,
        out int selectedTargetId)
    {
        selectedGambitSlot = 0;
        selectedTargetId = 0;
        var world = simulation.World;
        var reward = AdjustedGambitPlanReward(world, gambitSlot, entityId, targetId);
        if (reward <= 0f)
        {
            return 0f;
        }

        var planningWeight = 1f + global::System.MathF.Log(1f + reward);
        var weightedUrgency = urgency * planningWeight * global::System.MathF.Pow(DepthPenalty, depth);
        var preconditionResult = PreconditionsResult(
            simulation,
            gambitSlot,
            entityId,
            targetId,
            tick,
            out var blockerScopeCode,
            out var blockerValueId,
            out var blockerKindId,
            out var blockerTargetId);
        if (preconditionResult == PreconditionFailed)
        {
            return EvaluateBlockedGambitPlanCandidate(
                simulation,
                entityId,
                tick,
                weightedUrgency,
                depth,
                blockerScopeCode,
                blockerValueId,
                blockerKindId,
                blockerTargetId,
                ref candidateCount,
                out selectedGambitSlot,
                out selectedTargetId);
        }

        return EvaluateActionableGambitPlanCandidate(
            simulation,
            entityId,
            targetId,
            gambitSlot,
            tick,
            weightedUrgency,
            depth,
            ref candidateCount,
            out selectedGambitSlot,
            out selectedTargetId);
    }

    private float AdjustedGambitPlanReward(WorldState world, int gambitSlot, int entityId, int targetId)
    {
        var reward = EvaluateReward(world, gambitSlot, entityId, targetId);
        if (_gambitTargetModes[gambitSlot] == BehaviorTargetModeCodes.Self && reward < MinimumSelfDirectedReward)
        {
            return MinimumSelfDirectedReward;
        }

        return _gambitTargetModes[gambitSlot] == BehaviorTargetModeCodes.Self || reward > 0f ? reward : 0f;
    }

    private float EvaluateBlockedGambitPlanCandidate(
        Simulation simulation,
        int entityId,
        int tick,
        float weightedUrgency,
        int depth,
        int blockerScopeCode,
        int blockerValueId,
        int blockerKindId,
        int blockerTargetId,
        ref int candidateCount,
        out int selectedGambitSlot,
        out int selectedTargetId)
    {
        selectedGambitSlot = 0;
        selectedTargetId = 0;
        if (depth >= MaxPlanningDepth || blockerScopeCode == 0 || blockerTargetId == 0)
        {
            return 0f;
        }

        return EvaluateBestPlanForEffect(
            simulation,
            entityId,
            simulation.World.EntityKindId(entityId),
            tick,
            blockerScopeCode,
            blockerValueId,
            blockerKindId,
            blockerTargetId,
            weightedUrgency * SubGoalUrgencyScale,
            depth + 1,
            selectRoll: -1f,
            ref candidateCount,
            out _,
            out _,
            out selectedGambitSlot,
            out selectedTargetId);
    }

    private float EvaluateActionableGambitPlanCandidate(
        Simulation simulation,
        int entityId,
        int targetId,
        int gambitSlot,
        int tick,
        float weightedUrgency,
        int depth,
        ref int candidateCount,
        out int selectedGambitSlot,
        out int selectedTargetId)
    {
        var world = simulation.World;
        var score = EvaluateScore(world, gambitSlot, entityId, targetId);
        var recentActionCount = CountRecentActions(entityId, _gambitIds[gambitSlot], tick - RecencyWindow);
        for (var i = 0; i < recentActionCount; i++)
        {
            score *= RecencyDecayFactor;
        }

        var immediateWeight = weightedUrgency * Max(score, MinimumActionableScoreWeight);
        var bestWeight = immediateWeight;
        selectedGambitSlot = gambitSlot;
        selectedTargetId = targetId;

        if (depth < MaxPlanningDepth)
        {
            var supportWeight = EvaluateBestSupportPlan(
                simulation,
                entityId,
                targetId,
                gambitSlot,
                tick,
                weightedUrgency,
                score,
                depth + 1,
                ref candidateCount,
                out var supportGambitSlot,
                out var supportTargetId);
            if (supportWeight > bestWeight)
            {
                bestWeight = supportWeight;
                selectedGambitSlot = supportGambitSlot;
                selectedTargetId = supportTargetId;
            }
        }

        return bestWeight <= 0f ? 0f : bestWeight;
    }

    private float EvaluateBestSupportPlan(
        Simulation simulation,
        int entityId,
        int targetId,
        int gambitSlot,
        int tick,
        float weightedUrgency,
        float gambitScore,
        int depth,
        ref int candidateCount,
        out int selectedGambitSlot,
        out int selectedTargetId)
    {
        selectedGambitSlot = 0;
        selectedTargetId = 0;
        var bestWeight = 0f;
        var first = _gambitSupportRuleStarts[gambitSlot];
        var end = first + _gambitSupportRuleCounts[gambitSlot];
        var scoreHeadroom = 1f - Clamp01(gambitScore);
        for (var support = first; support < end; support++)
        {
            var currentValue = EvaluateSupportValue(simulation.World, support, entityId, targetId);
            var desiredMinimum = _supportDesiredMinimums[support];
            var deficitRatio = Clamp01((desiredMinimum - currentValue) / desiredMinimum);
            if (deficitRatio <= 0f)
            {
                continue;
            }

            var childBaseUrgency = weightedUrgency *
                SubGoalUrgencyScale *
                _supportUrgencyScales[support] *
                Max(deficitRatio, scoreHeadroom);
            if (childBaseUrgency <= 0f)
            {
                continue;
            }

            var forcedTargetId = SupportTargetId(entityId, targetId, _supportEffectScopeCodes[support]);
            var supportWeight = EvaluateBestPlanForEffect(
                simulation,
                entityId,
                simulation.World.EntityKindId(entityId),
                tick,
                _supportEffectScopeCodes[support],
                _supportEffectValueIds[support],
                _supportEffectKindIds[support],
                forcedTargetId,
                childBaseUrgency,
                depth,
                selectRoll: -1f,
                ref candidateCount,
                out _,
                out _,
                out var supportGambitSlot,
                out var supportTargetId);
            if (supportWeight > bestWeight)
            {
                bestWeight = supportWeight;
                selectedGambitSlot = supportGambitSlot;
                selectedTargetId = supportTargetId;
            }
        }

        return bestWeight;
    }

}
