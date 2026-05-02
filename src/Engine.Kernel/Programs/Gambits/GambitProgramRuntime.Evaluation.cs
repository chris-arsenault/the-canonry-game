namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GambitProgramRuntime
{
    private sealed class TargetSelectionState(int candidateCount)
    {
        public int CandidateCount = candidateCount;
        public int SelectedTargetId;
    }

    private bool GambitServesEffect(int gambitSlot, int effectScopeCode, int effectValueId, int effectKindId)
    {
        var first = _gambitGoalEffectStarts[gambitSlot];
        var end = first + _gambitGoalEffectCounts[gambitSlot];
        for (var effect = first; effect < end; effect++)
        {
            if (_goalEffectScopeCodes[effect] == effectScopeCode &&
                _goalEffectValueIds[effect] == effectValueId &&
                (effectKindId == 0 ||
                 _goalEffectKindIds[effect] == 0 ||
                 _goalEffectKindIds[effect] == effectKindId))
            {
                return true;
            }
        }

        return false;
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

    private bool BasicProviderSelectorMatches(WorldState world, int entityId, int targetId, int selector)
    {
        var code = _providerSelectorCodes[selector];
        if (code == BehaviorProviderSelectorCodes.TargetAttribute ||
            code == BehaviorProviderSelectorCodes.Structural)
        {
            return true;
        }

        if (code == BehaviorProviderSelectorCodes.Resource)
        {
            return world.GetResource(
                _providerSelectorValueIds[selector],
                world.EntityPhysicalQ(targetId),
                world.EntityPhysicalR(targetId)) > 0f;
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
            _providerSelectorMinimumStrengths[selector],
            _providerSelectorFactIds[selector]);
    }

    private float EvaluateSupportValue(WorldState world, int supportRuleSlot, int entityId, int targetId)
    {
        return EvaluateExpression(
            world,
            entityId,
            targetId,
            _supportExpressionStarts[supportRuleSlot],
            _supportExpressionCounts[supportRuleSlot],
            _supportExpressionCodes,
            _supportExpressionA,
            _supportExpressionX,
            defaultValue: 0f);
    }

    private static int SupportTargetId(int entityId, int targetId, int effectScopeCode)
    {
        return effectScopeCode switch
        {
            BehaviorEffectScopeCodes.TargetAttribute or BehaviorEffectScopeCodes.Relational => targetId,
            BehaviorEffectScopeCodes.Structural when targetId != entityId => targetId,
            _ => entityId,
        };
    }

    private static int EffectKey(int effectScopeCode, int effectValueId, int effectKindId)
    {
        unchecked
        {
            var hash = (uint)effectScopeCode * 73856093u;
            hash ^= (uint)effectValueId * 19349663u;
            hash ^= (uint)effectKindId * 83492791u;
            var key = (int)(hash & 2147483647u);
            return key == 0 ? 1 : key;
        }
    }

    private static int LocalityKey(WorldState world, int targetId)
    {
        unchecked
        {
            var q = world.EntityPhysicalQ(targetId) + 32768;
            var r = world.EntityPhysicalR(targetId) + 32768;
            var key = (q << 16) ^ (r & 65535);
            return key == 0 ? 1 : key;
        }
    }

    private float ObjectiveUrgency(WorldState world, int entityId, int objective)
    {
        return _objectiveCodes[objective] switch
        {
            BehaviorObjectiveUrgencyCodes.Baseline => BaselineUrgency(world.GetAttribute(entityId, _objectiveAttributeIds[objective]), _objectiveBaselines[objective]),
            BehaviorObjectiveUrgencyCodes.RangeDefault => RangeUrgency(
                world.GetAttribute(entityId, _objectiveAttributeIds[objective]),
                _objectiveMinimums[objective],
                _objectiveMaximums[objective],
                _objectiveDefaults[objective]),
            BehaviorObjectiveUrgencyCodes.Fixed => _objectiveBaseUrgencies[objective],
            _ => 0f,
        };
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
        KernelQuery.ForEachEntityOfKind(
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
        KernelQuery.ForEachEntity(
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
        if (!world.EntityIsActive(targetId) || targetId == entityId)
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
            TraitGambitScoreDelta(simulation.World, _gambitIds[gambitSlot], entityId, targetId);
        var recentActionCount = CountRecentActions(entityId, _gambitIds[gambitSlot], tick - RecencyWindow);
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
            if (PreconditionIsCompositionOp(_preconditionCodes[pre]))
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
            if (!PreconditionPasses(simulation, gambitSlot, pre, entityId, targetId, tick))
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
            if (ApplyCompositionPreconditionOp(pre, stack, ref stackCount))
            {
                continue;
            }

            var passes = PreconditionPasses(simulation, gambitSlot, pre, entityId, targetId, tick);
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

    private bool ApplyCompositionPreconditionOp(
        int pre,
        global::System.Span<bool> stack,
        ref int stackCount)
    {
        var code = _preconditionCodes[pre];
        if (code == BehaviorPreconditionCodes.And)
        {
            var right = PopBool(stack, ref stackCount);
            var left = PopBool(stack, ref stackCount);
            PushBool(stack, ref stackCount, left && right);
            return true;
        }

        if (code == BehaviorPreconditionCodes.Or)
        {
            var right = PopBool(stack, ref stackCount);
            var left = PopBool(stack, ref stackCount);
            PushBool(stack, ref stackCount, left || right);
            return true;
        }

        if (code != BehaviorPreconditionCodes.Not)
        {
            return false;
        }

        PushBool(stack, ref stackCount, !PopBool(stack, ref stackCount));
        return true;
    }

    private bool PreconditionPasses(Simulation simulation, int gambitSlot, int pre, int entityId, int targetId, int tick)
    {
        var code = _preconditionCodes[pre];
        if (code == BehaviorPreconditionCodes.Always)
        {
            return true;
        }

        if (IsSimplePrecondition(code))
        {
            return SimplePreconditionPasses(simulation.World, code, entityId, targetId);
        }

        if (IsRangePrecondition(code))
        {
            return RangePreconditionPasses(simulation, gambitSlot, code, pre, entityId, targetId, tick);
        }

        if (IsRelationshipPrecondition(code))
        {
            return RelationshipPreconditionPasses(simulation.World, code, pre, entityId, targetId);
        }

        throw new global::System.InvalidOperationException();
    }

    private static bool IsSimplePrecondition(int code)
    {
        return code is BehaviorPreconditionCodes.TargetIsSelf or
            BehaviorPreconditionCodes.TargetIsNotSelf or
            BehaviorPreconditionCodes.HasColocatedEntity;
    }

    private static bool IsRangePrecondition(int code)
    {
        return code is BehaviorPreconditionCodes.SelfAttributeRange or
            BehaviorPreconditionCodes.TargetAttributeRange or
            BehaviorPreconditionCodes.PlaneDistance or
            BehaviorPreconditionCodes.LocationResourceRange or
            BehaviorPreconditionCodes.CooldownElapsed or
            BehaviorPreconditionCodes.EntityCountAtLeast or
            BehaviorPreconditionCodes.ContestedInfluence or
            BehaviorPreconditionCodes.SharedInfluence;
    }

    private static bool IsRelationshipPrecondition(int code)
    {
        return code is BehaviorPreconditionCodes.SelfHasRelationshipToTarget or
            BehaviorPreconditionCodes.TargetHasRelationshipToSelf or
            BehaviorPreconditionCodes.SelfHasOutgoingRelationship or
            BehaviorPreconditionCodes.EndpointRelationshipCountBelow or
            BehaviorPreconditionCodes.RelatedEntityMatchesTarget or
            BehaviorPreconditionCodes.RelationshipExists;
    }

    private static bool SimplePreconditionPasses(
        WorldState world,
        int code,
        int entityId,
        int targetId)
    {
        if (code == BehaviorPreconditionCodes.TargetIsSelf)
        {
            return targetId == entityId;
        }

        if (code == BehaviorPreconditionCodes.TargetIsNotSelf)
        {
            return targetId != entityId;
        }

        return code == BehaviorPreconditionCodes.HasColocatedEntity &&
            KernelQuery.HasColocatedEntity(world, entityId);
    }

    private bool RangePreconditionPasses(
        Simulation simulation,
        int gambitSlot,
        int code,
        int pre,
        int entityId,
        int targetId,
        int tick)
    {
        var world = simulation.World;
        if (code == BehaviorPreconditionCodes.SelfAttributeRange)
        {
            return KernelQuery.AttributeInRange(world, entityId, _preA[pre], _preB[pre], _preC[pre], _preX[pre], _preY[pre]);
        }

        if (code == BehaviorPreconditionCodes.TargetAttributeRange)
        {
            return KernelQuery.AttributeInRange(world, targetId, _preA[pre], _preB[pre], _preC[pre], _preX[pre], _preY[pre]);
        }

        return OtherRangePreconditionPasses(simulation, gambitSlot, code, pre, entityId, targetId, tick);
    }

    private bool OtherRangePreconditionPasses(
        Simulation simulation,
        int gambitSlot,
        int code,
        int pre,
        int entityId,
        int targetId,
        int tick)
    {
        var world = simulation.World;
        if (code == BehaviorPreconditionCodes.PlaneDistance)
        {
            return KernelQuery.PlaneDistance(world, entityId, targetId, _preB[pre]) <= _preA[pre];
        }

        if (code == BehaviorPreconditionCodes.LocationResourceRange)
        {
            return LocationResourceRangePasses(world, pre, entityId, targetId);
        }

        if (code == BehaviorPreconditionCodes.CooldownElapsed)
        {
            return CountRecentActions(entityId, _gambitIds[gambitSlot], tick - _preA[pre] + 1) == 0;
        }

        if (code == BehaviorPreconditionCodes.ContestedInfluence)
        {
            return KernelQuery.InfluenceOverlapPasses(world, entityId, targetId, _preA[pre], _preX[pre], _preY[pre]);
        }

        if (code == BehaviorPreconditionCodes.SharedInfluence)
        {
            return KernelQuery.InfluenceOverlapPasses(world, entityId, targetId, _preA[pre], _preX[pre], _preY[pre]);
        }

        return code == BehaviorPreconditionCodes.EntityCountAtLeast &&
            KernelQuery.ActiveEntityCountOfKind(world, _preA[pre]) >= _preB[pre];
    }

    private bool RelationshipPreconditionPasses(
        WorldState world,
        int code,
        int pre,
        int entityId,
        int targetId)
    {
        if (code == BehaviorPreconditionCodes.SelfHasRelationshipToTarget ||
            code == BehaviorPreconditionCodes.RelatedEntityMatchesTarget)
        {
            return KernelQuery.RelationshipPasses(world, entityId, targetId, _preA[pre], _preB[pre], _preX[pre]);
        }

        if (code == BehaviorPreconditionCodes.TargetHasRelationshipToSelf)
        {
            return KernelQuery.RelationshipPasses(world, targetId, entityId, _preA[pre], _preB[pre], _preX[pre]);
        }

        if (code == BehaviorPreconditionCodes.SelfHasOutgoingRelationship)
        {
            return KernelQuery.SelfOutgoingRelationshipPasses(world, entityId, _preA[pre], _preB[pre], _preX[pre]);
        }

        if (code == BehaviorPreconditionCodes.EndpointRelationshipCountBelow)
        {
            return EndpointRelationshipCountBelowPasses(world, entityId, targetId, pre);
        }

        return code == BehaviorPreconditionCodes.RelationshipExists &&
            KernelQuery.RelationshipPasses(world, _preC[pre], _preD[pre], _preA[pre], _preB[pre], _preX[pre]);
    }

    private static bool PreconditionIsCompositionOp(int code)
    {
        return code is BehaviorPreconditionCodes.And or
            BehaviorPreconditionCodes.Or or
            BehaviorPreconditionCodes.Not;
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
        if (code == BehaviorPreconditionCodes.LocationResourceRange)
        {
            blockerScopeCode = BehaviorEffectScopeCodes.Resource;
            blockerValueId = _preA[pre];
            blockerTargetId = _preB[pre] == 0 ? entityId : targetId;
            return true;
        }

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

    private bool LocationResourceRangePasses(WorldState world, int pre, int entityId, int targetId)
    {
        var locationEntityId = _preB[pre] == 0 ? entityId : targetId;
        var value = world.GetResource(
            _preA[pre],
            world.EntityPhysicalQ(locationEntityId),
            world.EntityPhysicalR(locationEntityId));
        return KernelQuery.ValueInRange(value, _preC[pre], _preD[pre], _preX[pre], _preY[pre]);
    }

    private bool EndpointRelationshipCountBelowPasses(WorldState world, int entityId, int targetId, int pre)
    {
        var endpointId = _preB[pre] == BehaviorEndpointCodes.Target ? targetId : entityId;
        var count = KernelQuery.DistinctRelationshipEndpointCount(world, endpointId, _preA[pre], _preD[pre], _preX[pre]);
        return count < _preC[pre];
    }

}
