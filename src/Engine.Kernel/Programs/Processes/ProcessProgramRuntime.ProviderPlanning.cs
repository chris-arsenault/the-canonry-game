namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class ProcessProgramRuntime
{
    private float EvaluateProviderSelectorTargets(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selectorStart,
        int selectorCount,
        bool traceEnabled,
        int parentCandidateIndex,
        ref int candidateCount,
        out int selectedRootProcessSlot,
        out int selectedRootTargetId,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        selectedRootProcessSlot = 0;
        selectedRootTargetId = 0;
        selectedProcessSlot = 0;
        selectedTargetId = 0;
        var bestWeight = 0f;
        _processVisited.BeginEpoch();
        var selectorEnd = selectorStart + selectorCount;
        for (var selector = selectorStart; selector < selectorEnd; selector++)
        {
            bestWeight = EvaluateProviderSelectorTargets(
                simulation,
                entityId,
                processSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selector,
                bestWeight,
                traceEnabled,
                parentCandidateIndex,
                ref candidateCount,
                ref selectedRootProcessSlot,
                ref selectedRootTargetId,
                ref selectedProcessSlot,
                ref selectedTargetId);
        }

        return bestWeight;
    }

    private float EvaluateProviderSelectorTargets(Simulation simulation, int entityId, int processSlot, int tick, int effectScopeCode, int effectValueId, int effectKindId, float urgency, int depth, int selector, float bestWeight, bool traceEnabled, int parentCandidateIndex, ref int candidateCount, ref int selectedRootProcessSlot, ref int selectedRootTargetId, ref int selectedProcessSlot, ref int selectedTargetId)
    {
        var code = _providerSelectorCodes[selector];
        if (code == ProcessProviderSelectorCodes.TargetAttribute ||
            code == ProcessProviderSelectorCodes.Structural ||
            code == ProcessProviderSelectorCodes.Resource)
        {
            return EvaluateKindProviderSelector(
                simulation, entityId, processSlot, tick, effectScopeCode, effectValueId, effectKindId,
                urgency, depth, selector, bestWeight,
                traceEnabled, parentCandidateIndex,
                ref candidateCount, ref selectedRootProcessSlot, ref selectedRootTargetId, ref selectedProcessSlot, ref selectedTargetId);
        }

        if (code == ProcessProviderSelectorCodes.LocationAttribute)
        {
            return EvaluateLocationProviderSelector(
                simulation, entityId, processSlot, tick, effectScopeCode, effectValueId, effectKindId,
                urgency, depth, selector, bestWeight,
                traceEnabled, parentCandidateIndex,
                ref candidateCount, ref selectedRootProcessSlot, ref selectedRootTargetId, ref selectedProcessSlot, ref selectedTargetId);
        }

        if (code == ProcessProviderSelectorCodes.OutgoingRelationship ||
            code == ProcessProviderSelectorCodes.IncomingRelationship)
        {
            return EvaluateRelationshipProviderSelector(
                simulation, entityId, processSlot, tick, effectScopeCode, effectValueId, effectKindId,
                urgency, depth, selector, code == ProcessProviderSelectorCodes.OutgoingRelationship, bestWeight,
                traceEnabled, parentCandidateIndex,
                ref candidateCount, ref selectedRootProcessSlot, ref selectedRootTargetId, ref selectedProcessSlot, ref selectedTargetId);
        }

        throw new global::System.InvalidOperationException();
    }

    private float EvaluateLocationProviderSelector(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selector,
        float bestWeight,
        bool traceEnabled,
        int parentCandidateIndex,
        ref int candidateCount,
        ref int selectedRootProcessSlot,
        ref int selectedRootTargetId,
        ref int selectedProcessSlot,
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
                processSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selector,
                bestWeight,
                traceEnabled,
                parentCandidateIndex,
                ref candidateCount,
                ref selectedRootProcessSlot,
                ref selectedRootTargetId,
                ref selectedProcessSlot,
                ref selectedTargetId);
        }

        return bestWeight;
    }

    private float EvaluateKindProviderSelector(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selector,
        float bestWeight,
        bool traceEnabled,
        int parentCandidateIndex,
        ref int candidateCount,
        ref int selectedRootProcessSlot,
        ref int selectedRootTargetId,
        ref int selectedProcessSlot,
        ref int selectedTargetId)
    {
        var world = simulation.World;
        var kindId = EffectiveProviderSelectorKindId(processSlot, selector);
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
                    processSlot,
                    tick,
                    effectScopeCode,
                    effectValueId,
                    effectKindId,
                    urgency,
                    depth,
                    selector,
                    bestWeight,
                    traceEnabled,
                    parentCandidateIndex,
                    ref candidateCount,
                    ref selectedRootProcessSlot,
                    ref selectedRootTargetId,
                    ref selectedProcessSlot,
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
                processSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selector,
                bestWeight,
                traceEnabled,
                parentCandidateIndex,
                ref candidateCount,
                ref selectedRootProcessSlot,
                ref selectedRootTargetId,
                ref selectedProcessSlot,
                ref selectedTargetId);
        }

        return bestWeight;
    }

    private float EvaluateRelationshipProviderSelector(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selector,
        bool outgoing,
        float bestWeight,
        bool traceEnabled,
        int parentCandidateIndex,
        ref int candidateCount,
        ref int selectedRootProcessSlot,
        ref int selectedRootTargetId,
        ref int selectedProcessSlot,
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
                processSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selector,
                bestWeight,
                traceEnabled,
                parentCandidateIndex,
                ref candidateCount,
                ref selectedRootProcessSlot,
                ref selectedRootTargetId,
                ref selectedProcessSlot,
                ref selectedTargetId);
        }

        return bestWeight;
    }

    private float EvaluateProviderTargetIfMatched(
        Simulation simulation,
        int entityId,
        int targetId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selector,
        float bestWeight,
        bool traceEnabled,
        int parentCandidateIndex,
        ref int candidateCount,
        ref int selectedRootProcessSlot,
        ref int selectedRootTargetId,
        ref int selectedProcessSlot,
        ref int selectedTargetId)
    {
        var world = simulation.World;
        if (!world.EntityIsActive(targetId) || _processVisited.Contains(targetId))
        {
            return bestWeight;
        }

        if (!ProviderTargetMatchesSelector(world, targetId, processSlot, selector))
        {
            return bestWeight;
        }

        _processVisited.Add(targetId);
        candidateCount++;
        var candidateIndex = candidateCount;
        var weight = EvaluatePlanCandidate(
            simulation,
            entityId,
            targetId,
            processSlot,
            tick,
            effectScopeCode,
            effectValueId,
            effectKindId,
            urgency,
            depth,
            candidateIndex,
            parentCandidateIndex,
            traceEnabled,
            ref candidateCount,
            out var planProcessSlot,
            out var planTargetId);
        if (weight > bestWeight)
        {
            selectedRootProcessSlot = processSlot;
            selectedRootTargetId = targetId;
            selectedProcessSlot = planProcessSlot;
            selectedTargetId = planTargetId;
            return weight;
        }

        return bestWeight;
    }

    private bool ProviderTargetMatchesSelector(WorldState world, int targetId, int processSlot, int selector)
    {
        if (NamedProviderRecoveryActive(processSlot))
        {
            return NamedProviderTargetMatches(world, targetId, processSlot);
        }

        var selectorKindId = EffectiveProviderSelectorKindId(processSlot, selector);
        if (selectorKindId > 0 && world.EntityKindId(targetId) != selectorKindId)
        {
            return false;
        }

        if (ProviderSelectorRequiresAttribute(selector) &&
            !world.HasAttribute(targetId, _providerSelectorValueIds[selector]))
        {
            return false;
        }

        return _providerSelectorCodes[selector] != ProcessProviderSelectorCodes.Resource ||
               world.GetResource(
                   _providerSelectorValueIds[selector],
                   world.EntityPhysicalQ(targetId),
                   world.EntityPhysicalR(targetId)) > 0f;
    }

    private int EffectiveProviderSelectorKindId(int processSlot, int selector)
    {
        return NamedProviderRecoveryActive(processSlot)
            ? _processNamedProviderKindIds[processSlot]
            : _providerSelectorKindIds[selector];
    }

}
