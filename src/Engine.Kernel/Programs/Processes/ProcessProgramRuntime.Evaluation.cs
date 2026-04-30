namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class ProcessProgramRuntime
{
    private bool ProcessServesEffect(int processSlot, int effectScopeCode, int effectValueId, int effectKindId)
    {
        var first = _processGoalEffectStarts[processSlot];
        var end = first + _processGoalEffectCounts[processSlot];
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
            ProcessProviderSelectorCodes.TargetAttribute or
            ProcessProviderSelectorCodes.LocationAttribute or
            ProcessProviderSelectorCodes.OutgoingRelationship or
            ProcessProviderSelectorCodes.IncomingRelationship;
    }

    private bool TargetEligibleForProcess(
        WorldState world,
        int entityId,
        int processSlot,
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

        var selectorStart = _processProviderSelectorStarts[processSlot];
        var selectorEnd = selectorStart + _processProviderSelectorCounts[processSlot];
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

        return TargetModeMatchesProcess(world, entityId, processSlot, targetId);
    }

    private bool TargetModeMatchesProcess(WorldState world, int entityId, int processSlot, int targetId)
    {
        var targetMode = _processTargetModes[processSlot];
        if (targetMode == ProcessTargetModeCodes.Self)
        {
            return targetId == entityId;
        }

        if (targetMode != ProcessTargetModeCodes.KindOther)
        {
            return targetId != entityId;
        }

        return targetId != entityId &&
            EffectiveTargetKindId(processSlot) > 0 &&
            world.EntityKindId(targetId) == EffectiveTargetKindId(processSlot);
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
        if (code == ProcessProviderSelectorCodes.TargetAttribute ||
            code == ProcessProviderSelectorCodes.Structural)
        {
            return true;
        }

        if (code == ProcessProviderSelectorCodes.Resource)
        {
            return world.GetResource(
                _providerSelectorValueIds[selector],
                world.EntityPhysicalQ(targetId),
                world.EntityPhysicalR(targetId)) > 0f;
        }

        return code == ProcessProviderSelectorCodes.LocationAttribute &&
            world.EntityPhysicalQ(entityId) == world.EntityPhysicalQ(targetId) &&
            world.EntityPhysicalR(entityId) == world.EntityPhysicalR(targetId);
    }

    private bool RelationshipProviderSelectorMatches(WorldState world, int entityId, int targetId, int selector)
    {
        var code = _providerSelectorCodes[selector];
        if (code == ProcessProviderSelectorCodes.OutgoingRelationship)
        {
            return RelationshipSelectorMatchesTarget(world, entityId, targetId, selector);
        }

        return code == ProcessProviderSelectorCodes.IncomingRelationship &&
            RelationshipSelectorMatchesTarget(world, targetId, entityId, selector);
    }

    private bool RelationshipSelectorMatchesTarget(WorldState world, int sourceId, int targetId, int selector)
    {
        var relationshipKindId = _providerSelectorRelationshipKindIds[selector];
        if (relationshipKindId <= 0)
        {
            return false;
        }

        var relationshipId = world.FindRelationship(sourceId, targetId, relationshipKindId);
        return relationshipId > 0 &&
               world.RelationshipStrength(relationshipId) >= _providerSelectorMinimumStrengths[selector] &&
               world.RelationshipHasFact(relationshipId, _providerSelectorFactIds[selector]);
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
            ProcessEffectScopeCodes.TargetAttribute or ProcessEffectScopeCodes.Relational => targetId,
            ProcessEffectScopeCodes.Structural when targetId != entityId => targetId,
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
            ProcessObjectiveUrgencyCodes.StandardNeed => StandardNeedUrgency(world.GetAttribute(entityId, _objectiveAttributeIds[objective])),
            ProcessObjectiveUrgencyCodes.Baseline => BaselineUrgency(world.GetAttribute(entityId, _objectiveAttributeIds[objective]), _objectiveBaselines[objective]),
            ProcessObjectiveUrgencyCodes.RangeDefault => RangeUrgency(
                world.GetAttribute(entityId, _objectiveAttributeIds[objective]),
                _objectiveMinimums[objective],
                _objectiveMaximums[objective],
                _objectiveDefaults[objective]),
            ProcessObjectiveUrgencyCodes.Fixed => _objectiveBaseUrgencies[objective],
            _ => 0f,
        };
    }

    private float EvaluateTargets(
        Simulation simulation,
        int processSlot,
        int entityId,
        int tick,
        ref int candidateCount,
        float selectRoll,
        out int selectedTargetId)
    {
        selectedTargetId = 0;
        var targetMode = _processTargetModes[processSlot];
        if (targetMode == ProcessTargetModeCodes.Self)
        {
            return EvaluateSelfTarget(
                simulation,
                processSlot,
                entityId,
                tick,
                ref candidateCount,
                selectRoll,
                out selectedTargetId);
        }

        if (targetMode == ProcessTargetModeCodes.KindOther && EffectiveTargetKindId(processSlot) > 0)
        {
            return EvaluateKindTargets(
                simulation,
                processSlot,
                entityId,
                tick,
                ref candidateCount,
                selectRoll,
                out selectedTargetId);
        }

        return EvaluateAllTargets(
            simulation,
            processSlot,
            entityId,
            tick,
            ref candidateCount,
            selectRoll,
            out selectedTargetId);
    }

    private float EvaluateSelfTarget(
        Simulation simulation,
        int processSlot,
        int entityId,
        int tick,
        ref int candidateCount,
        float selectRoll,
        out int selectedTargetId)
    {
        candidateCount++;
        var candidateIndex = candidateCount;
        return EvaluateCandidate(
            simulation,
            processSlot,
            entityId,
            entityId,
            tick,
            candidateIndex,
            selectRoll,
            out selectedTargetId);
    }

    private float EvaluateKindTargets(
        Simulation simulation,
        int processSlot,
        int entityId,
        int tick,
        ref int candidateCount,
        float selectRoll,
        out int selectedTargetId)
    {
        selectedTargetId = 0;
        var total = 0f;
        var world = simulation.World;
        for (var targetId = world.FirstEntityOfKind(EffectiveTargetKindId(processSlot));
             targetId > 0;
             targetId = world.NextEntityOfKind(targetId))
        {
            total += EvaluateTargetCandidate(
                simulation,
                processSlot,
                entityId,
                targetId,
                tick,
                ref candidateCount,
                selectRoll,
                total,
                out selectedTargetId);
            if (selectedTargetId > 0)
            {
                return total;
            }
        }

        return total;
    }

    private float EvaluateAllTargets(
        Simulation simulation,
        int processSlot,
        int entityId,
        int tick,
        ref int candidateCount,
        float selectRoll,
        out int selectedTargetId)
    {
        selectedTargetId = 0;
        var total = 0f;
        var world = simulation.World;
        for (var targetId = 1; targetId <= world.EntityHighWatermark; targetId++)
        {
            total += EvaluateTargetCandidate(
                simulation,
                processSlot,
                entityId,
                targetId,
                tick,
                ref candidateCount,
                selectRoll,
                total,
                out selectedTargetId);
            if (selectedTargetId > 0)
            {
                return total;
            }
        }

        return total;
    }

    private float EvaluateTargetCandidate(
        Simulation simulation,
        int processSlot,
        int entityId,
        int targetId,
        int tick,
        ref int candidateCount,
        float selectRoll,
        float total,
        out int selectedTargetId)
    {
        selectedTargetId = 0;
        var world = simulation.World;
        if (!world.EntityIsActive(targetId) || targetId == entityId)
        {
            return 0f;
        }

        candidateCount++;
        var candidateIndex = candidateCount;
        return EvaluateCandidate(
            simulation,
            processSlot,
            entityId,
            targetId,
            tick,
            candidateIndex,
            selectRoll < 0f ? -1f : selectRoll - total,
            out selectedTargetId);
    }

    private float EvaluateCandidate(
        Simulation simulation,
        int processSlot,
        int entityId,
        int targetId,
        int tick,
        int candidateIndex,
        float selectRoll,
        out int selectedTargetId)
    {
        selectedTargetId = 0;
        var preconditionResult = PreconditionsResult(simulation, processSlot, entityId, targetId, tick);
        if (preconditionResult == PreconditionFailed)
        {
            EmitUnplannedTargetTraceIfNeeded(simulation, tick, entityId, candidateIndex, processSlot, targetId, TraceStatusCodes.PreconditionFailed, 0f, 0f, selectRoll);
            return 0f;
        }

        var score = EvaluateScore(simulation.World, processSlot, entityId, targetId);
        var recentActionCount = CountRecentActions(entityId, _processIds[processSlot], tick - RecencyWindow);
        for (var i = 0; i < recentActionCount; i++)
        {
            score *= RecencyDecayFactor;
        }

        if (score <= 0f)
        {
            EmitUnplannedTargetTraceIfNeeded(simulation, tick, entityId, candidateIndex, processSlot, targetId, TraceStatusCodes.NoReward, score, 0f, selectRoll);
            return 0f;
        }

        if (selectRoll >= 0f && selectRoll <= score)
        {
            selectedTargetId = targetId;
        }

        EmitUnplannedTargetTraceIfNeeded(simulation, tick, entityId, candidateIndex, processSlot, targetId, TraceStatusCodes.Actionable, score, score, selectRoll);
        return score;
    }

    private int PreconditionsResult(Simulation simulation, int processSlot, int entityId, int targetId, int tick)
    {
        return PreconditionsResult(
            simulation,
            processSlot,
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
        int processSlot,
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
        var first = _processPreconditionStarts[processSlot];
        var end = first + _processPreconditionCounts[processSlot];
        for (var pre = first; pre < end; pre++)
        {
            if (PreconditionIsCompositionOp(_preconditionCodes[pre]))
            {
                return ComposedPreconditionsResult(
                    simulation,
                    processSlot,
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
            if (!PreconditionPasses(simulation, processSlot, pre, entityId, targetId, tick))
            {
                ResolvePreconditionBlocker(
                    world,
                    processSlot,
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
        int processSlot,
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

            var passes = PreconditionPasses(simulation, processSlot, pre, entityId, targetId, tick);
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
                processSlot,
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
        if (code == ProcessPreconditionCodes.And)
        {
            var right = PopBool(stack, ref stackCount);
            var left = PopBool(stack, ref stackCount);
            PushBool(stack, ref stackCount, left && right);
            return true;
        }

        if (code == ProcessPreconditionCodes.Or)
        {
            var right = PopBool(stack, ref stackCount);
            var left = PopBool(stack, ref stackCount);
            PushBool(stack, ref stackCount, left || right);
            return true;
        }

        if (code != ProcessPreconditionCodes.Not)
        {
            return false;
        }

        PushBool(stack, ref stackCount, !PopBool(stack, ref stackCount));
        return true;
    }

    private bool PreconditionPasses(Simulation simulation, int processSlot, int pre, int entityId, int targetId, int tick)
    {
        var code = _preconditionCodes[pre];
        if (code == ProcessPreconditionCodes.Always)
        {
            return true;
        }

        if (IsSimplePrecondition(code))
        {
            return SimplePreconditionPasses(simulation.World, code, entityId, targetId);
        }

        if (IsRangePrecondition(code))
        {
            return RangePreconditionPasses(simulation, processSlot, code, pre, entityId, targetId, tick);
        }

        if (IsRelationshipPrecondition(code))
        {
            return RelationshipPreconditionPasses(simulation.World, code, pre, entityId, targetId);
        }

        throw new global::System.InvalidOperationException();
    }

    private static bool IsSimplePrecondition(int code)
    {
        return code is ProcessPreconditionCodes.TargetIsSelf or
            ProcessPreconditionCodes.TargetIsNotSelf or
            ProcessPreconditionCodes.HasColocatedEntity;
    }

    private static bool IsRangePrecondition(int code)
    {
        return code is ProcessPreconditionCodes.SelfAttributeRange or
            ProcessPreconditionCodes.TargetAttributeRange or
            ProcessPreconditionCodes.PlaneDistance or
            ProcessPreconditionCodes.LocationResourceRange or
            ProcessPreconditionCodes.PressureRange or
            ProcessPreconditionCodes.CooldownElapsed or
            ProcessPreconditionCodes.EntityCountAtLeast;
    }

    private static bool IsRelationshipPrecondition(int code)
    {
        return code is ProcessPreconditionCodes.SelfHasRelationshipToTarget or
            ProcessPreconditionCodes.TargetHasRelationshipToSelf or
            ProcessPreconditionCodes.RelatedEntityMatchesTarget or
            ProcessPreconditionCodes.RelationshipExists;
    }

    private static bool SimplePreconditionPasses(
        WorldState world,
        int code,
        int entityId,
        int targetId)
    {
        if (code == ProcessPreconditionCodes.TargetIsSelf)
        {
            return targetId == entityId;
        }

        if (code == ProcessPreconditionCodes.TargetIsNotSelf)
        {
            return targetId != entityId;
        }

        return code == ProcessPreconditionCodes.HasColocatedEntity &&
            HasColocatedEntity(world, entityId);
    }

    private bool RangePreconditionPasses(
        Simulation simulation,
        int processSlot,
        int code,
        int pre,
        int entityId,
        int targetId,
        int tick)
    {
        var world = simulation.World;
        if (code == ProcessPreconditionCodes.SelfAttributeRange)
        {
            return ValueInRange(world.GetAttribute(entityId, _preA[pre]), _preB[pre], _preC[pre], _preX[pre], _preY[pre]);
        }

        if (code == ProcessPreconditionCodes.TargetAttributeRange)
        {
            return ValueInRange(world.GetAttribute(targetId, _preA[pre]), _preB[pre], _preC[pre], _preX[pre], _preY[pre]);
        }

        return OtherRangePreconditionPasses(simulation, processSlot, code, pre, entityId, targetId, tick);
    }

    private bool OtherRangePreconditionPasses(
        Simulation simulation,
        int processSlot,
        int code,
        int pre,
        int entityId,
        int targetId,
        int tick)
    {
        var world = simulation.World;
        if (code == ProcessPreconditionCodes.PlaneDistance)
        {
            return PlaneDistance(world, entityId, targetId, _preB[pre]) <= _preA[pre];
        }

        if (code == ProcessPreconditionCodes.LocationResourceRange)
        {
            return LocationResourceRangePasses(world, pre, entityId, targetId);
        }

        if (code == ProcessPreconditionCodes.PressureRange)
        {
            return PressureRangePasses(simulation, pre, entityId);
        }

        if (code == ProcessPreconditionCodes.CooldownElapsed)
        {
            return CountRecentActions(entityId, _processIds[processSlot], tick - _preA[pre] + 1) == 0;
        }

        return code == ProcessPreconditionCodes.EntityCountAtLeast &&
            ActiveEntityCountOfKind(world, _preA[pre]) >= _preB[pre];
    }

    private bool RelationshipPreconditionPasses(
        WorldState world,
        int code,
        int pre,
        int entityId,
        int targetId)
    {
        if (code == ProcessPreconditionCodes.SelfHasRelationshipToTarget ||
            code == ProcessPreconditionCodes.RelatedEntityMatchesTarget)
        {
            return RelationshipPasses(world, entityId, targetId, pre);
        }

        if (code == ProcessPreconditionCodes.TargetHasRelationshipToSelf)
        {
            return RelationshipPasses(world, targetId, entityId, pre);
        }

        return code == ProcessPreconditionCodes.RelationshipExists &&
            RelationshipPasses(world, _preC[pre], _preD[pre], pre);
    }

    private static bool PreconditionIsCompositionOp(int code)
    {
        return code is ProcessPreconditionCodes.And or
            ProcessPreconditionCodes.Or or
            ProcessPreconditionCodes.Not;
    }

    private static int ActiveEntityCountOfKind(WorldState world, int kindId)
    {
        var count = 0;
        for (var entityId = world.FirstEntityOfKind(kindId);
             entityId > 0;
             entityId = world.NextEntityOfKind(entityId))
        {
            if (world.EntityIsActive(entityId))
            {
                count++;
            }
        }

        return count;
    }

    private static bool HasColocatedEntity(WorldState world, int entityId)
    {
        var q = world.EntityPhysicalQ(entityId);
        var r = world.EntityPhysicalR(entityId);
        for (var colocatedId = world.FirstPhysicalEntityAt(q, r);
             colocatedId > 0;
             colocatedId = world.NextPhysicalEntityAt(colocatedId))
        {
            if (colocatedId != entityId && world.EntityIsActive(colocatedId))
            {
                return true;
            }
        }

        return false;
    }

    private void ResolvePreconditionBlocker(
        WorldState world,
        int processSlot,
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
        if (code == ProcessPreconditionCodes.SelfAttributeRange)
        {
            blockerScopeCode = ProcessEffectScopeCodes.SelfAttribute;
            blockerValueId = _preA[pre];
            blockerKindId = world.EntityKindId(entityId);
            blockerTargetId = entityId;
            return true;
        }

        if (code != ProcessPreconditionCodes.TargetAttributeRange)
        {
            return false;
        }

        blockerScopeCode = ProcessEffectScopeCodes.TargetAttribute;
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
        if (code == ProcessPreconditionCodes.LocationResourceRange)
        {
            blockerScopeCode = ProcessEffectScopeCodes.Resource;
            blockerValueId = _preA[pre];
            blockerTargetId = _preB[pre] == 0 ? entityId : targetId;
            return true;
        }

        if (code == ProcessPreconditionCodes.PlaneDistance)
        {
            blockerScopeCode = ProcessEffectScopeCodes.Structural;
            blockerValueId = _preC[pre];
            blockerKindId = world.EntityKindId(entityId);
            blockerTargetId = targetId != entityId ? targetId : entityId;
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
        if (code != ProcessPreconditionCodes.SelfHasRelationshipToTarget &&
            code != ProcessPreconditionCodes.TargetHasRelationshipToSelf &&
            code != ProcessPreconditionCodes.RelatedEntityMatchesTarget &&
            code != ProcessPreconditionCodes.RelationshipExists)
        {
            return false;
        }

        blockerScopeCode = ProcessEffectScopeCodes.Relational;
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
        return ValueInRange(value, _preC[pre], _preD[pre], _preX[pre], _preY[pre]);
    }

    private bool RelationshipPasses(WorldState world, int sourceId, int targetId, int pre)
    {
        var relationshipId = world.FindRelationship(sourceId, targetId, _preA[pre]);
        return relationshipId > 0 &&
               (_preB[pre] == 0 || world.RelationshipStrength(relationshipId) >= _preX[pre]);
    }

    private bool PressureRangePasses(Simulation simulation, int pre, int entityId)
    {
        var scopeEntityId = _preB[pre] > 0 ? _preB[pre] : entityId;
        var value = simulation.Pressures.GetValue(_preA[pre], scopeEntityId);
        return ValueInRange(value, _preC[pre], _preD[pre], _preX[pre], _preY[pre]);
    }

}
