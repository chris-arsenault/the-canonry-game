namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class ProcessProgramRuntime
{
    private struct ProcessPlanChoice
    {
        public float Weight;
        public int RootProcessSlot;
        public int RootTargetId;
        public int ProcessSlot;
        public int TargetId;
    }

    private float EvaluateBestPlanForEffect(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int forcedTargetId,
        float urgency,
        int depth,
        float selectRoll,
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
        if (depth > MaxPlanningDepth)
        {
            return 0f;
        }

        var useCommitment = depth == 0;
        var effectKey = useCommitment ? EffectKey(effectScopeCode, effectValueId, effectKindId) : 0;
        var best = EvaluateProcessPlanChoices(
            simulation,
            entityId,
            kindId,
            tick,
            effectScopeCode,
            effectValueId,
            effectKindId,
            forcedTargetId,
            urgency,
            depth,
            useCommitment,
            effectKey,
            traceEnabled,
            parentCandidateIndex,
            ref candidateCount,
            out var committed);
        best = ChooseCommittedProcessPlanChoice(best, committed, useCommitment);
        SelectProcessPlanChoice(
            entityId,
            effectKey,
            useCommitment,
            selectRoll,
            best,
            out selectedRootProcessSlot,
            out selectedRootTargetId,
            out selectedProcessSlot,
            out selectedTargetId);
        return best.Weight;
    }

    private ProcessPlanChoice EvaluateProcessPlanChoices(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int forcedTargetId,
        float urgency,
        int depth,
        bool useCommitment,
        int effectKey,
        bool traceEnabled,
        int parentCandidateIndex,
        ref int candidateCount,
        out ProcessPlanChoice committed)
    {
        var best = default(ProcessPlanChoice);
        committed = default;
        var previousTargetId = useCommitment ? _committedPlanTargets.GetOrDefault(entityId, effectKey) : 0;
        var previousProcessSlot = useCommitment ? _committedPlanProcessSlots.GetOrDefault(entityId, effectKey) : 0;
        for (var processSlot = 1; processSlot <= _processCount; processSlot++)
        {
            if (ShouldSkipPlanProcess(
                simulation,
                entityId,
                kindId,
                tick,
                processSlot,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                traceEnabled,
                parentCandidateIndex))
            {
                continue;
            }

            var candidate = EvaluateProcessPlanChoice(simulation, entityId, processSlot, tick, effectScopeCode, effectValueId, effectKindId, forcedTargetId, urgency, depth, traceEnabled, parentCandidateIndex, ref candidateCount);
            TrackProcessPlanChoice(candidate, previousProcessSlot, previousTargetId, ref best, ref committed);
            if (TryEvaluatePreviousProcessPlanChoice(simulation, entityId, processSlot, tick, effectScopeCode, effectValueId, effectKindId, forcedTargetId, urgency, depth, useCommitment, previousProcessSlot, previousTargetId, traceEnabled, parentCandidateIndex, ref candidateCount, out var previous))
            {
                committed = previous;
            }
        }

        return best;
    }

    private ProcessPlanChoice EvaluateProcessPlanChoice(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int forcedTargetId,
        float urgency,
        int depth,
        bool traceEnabled,
        int parentCandidateIndex,
        ref int candidateCount)
    {
        var weight = EvaluateBestTargetsForProcess(
            simulation,
            entityId,
            processSlot,
            tick,
            effectScopeCode,
            effectValueId,
            effectKindId,
            forcedTargetId,
            urgency,
            depth,
            traceEnabled,
            parentCandidateIndex,
            ref candidateCount,
            out var rootProcessSlot,
            out var rootTargetId,
            out var leafProcessSlot,
            out var leafTargetId);
        return new ProcessPlanChoice
        {
            Weight = weight,
            RootProcessSlot = rootProcessSlot,
            RootTargetId = rootTargetId,
            ProcessSlot = leafProcessSlot,
            TargetId = leafTargetId,
        };
    }

    private static void TrackProcessPlanChoice(
        ProcessPlanChoice candidate,
        int previousProcessSlot,
        int previousTargetId,
        ref ProcessPlanChoice best,
        ref ProcessPlanChoice committed)
    {
        if (candidate.RootProcessSlot == previousProcessSlot && candidate.RootTargetId == previousTargetId)
        {
            committed = candidate;
        }

        if (candidate.Weight > best.Weight)
        {
            best = candidate;
        }
    }

    private bool TryEvaluatePreviousProcessPlanChoice(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int forcedTargetId,
        float urgency,
        int depth,
        bool useCommitment,
        int previousProcessSlot,
        int previousTargetId,
        bool traceEnabled,
        int parentCandidateIndex,
        ref int candidateCount,
        out ProcessPlanChoice previous)
    {
        previous = default;
        if (!useCommitment ||
            previousProcessSlot != processSlot ||
            previousTargetId <= 0 ||
            !TargetEligibleForProcess(simulation.World, entityId, processSlot, forcedTargetId, previousTargetId))
        {
            return false;
        }

        candidateCount++;
        var candidateIndex = candidateCount;
        var weight = EvaluatePlanCandidate(
            simulation,
            entityId,
            previousTargetId,
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
            out var leafProcessSlot,
            out var leafTargetId);
        if (weight <= 0f)
        {
            return false;
        }

        previous = new ProcessPlanChoice
        {
            Weight = weight,
            RootProcessSlot = processSlot,
            RootTargetId = previousTargetId,
            ProcessSlot = leafProcessSlot,
            TargetId = leafTargetId,
        };
        return true;
    }

    private static ProcessPlanChoice ChooseCommittedProcessPlanChoice(
        ProcessPlanChoice best,
        ProcessPlanChoice committed,
        bool useCommitment)
    {
        if (useCommitment &&
            committed.Weight > 0f &&
            best.Weight <= committed.Weight * (1f + TargetSwitchHysteresisRatio))
        {
            return committed;
        }

        return best;
    }

    private void SelectProcessPlanChoice(
        int entityId,
        int effectKey,
        bool useCommitment,
        float selectRoll,
        ProcessPlanChoice best,
        out int selectedRootProcessSlot,
        out int selectedRootTargetId,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        selectedRootProcessSlot = 0;
        selectedRootTargetId = 0;
        selectedProcessSlot = 0;
        selectedTargetId = 0;
        if ((selectRoll >= 0f && selectRoll > best.Weight) || best.ProcessSlot == 0)
        {
            return;
        }

        selectedRootProcessSlot = best.RootProcessSlot;
        selectedRootTargetId = best.RootTargetId;
        selectedProcessSlot = best.ProcessSlot;
        selectedTargetId = best.TargetId;
        if (useCommitment && selectRoll >= 0f)
        {
            _committedPlanTargets.Set(entityId, effectKey, best.RootTargetId);
            _committedPlanProcessSlots.Set(entityId, effectKey, best.RootProcessSlot);
        }
    }

    private float EvaluateBestTargetsForProcess(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int forcedTargetId,
        float urgency,
        int depth,
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
        if (forcedTargetId > 0)
        {
            var forced = simulation.World.EntityIsActive(forcedTargetId)
                ? EvaluateProcessTargetPlanChoice(simulation, entityId, forcedTargetId, processSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, traceEnabled, parentCandidateIndex, ref candidateCount)
                : default;
            return SelectBestProcessTargetChoice(forced, out selectedRootProcessSlot, out selectedRootTargetId, out selectedProcessSlot, out selectedTargetId);
        }

        var selectorStart = _processProviderSelectorStarts[processSlot];
        var selectorCount = _processProviderSelectorCounts[processSlot];
        if (selectorCount > 0)
        {
            return EvaluateProviderSelectorTargets(
                simulation,
                entityId,
                processSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selectorStart,
                selectorCount,
                traceEnabled,
                parentCandidateIndex,
                ref candidateCount,
                out selectedRootProcessSlot,
                out selectedRootTargetId,
                out selectedProcessSlot,
                out selectedTargetId);
        }

        if (NamedProviderRecoveryActive(processSlot))
        {
            var namedProvider = EvaluateKindProcessTargetChoices(simulation, entityId, processSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, traceEnabled, parentCandidateIndex, ref candidateCount);
            return SelectBestProcessTargetChoice(namedProvider, out selectedRootProcessSlot, out selectedRootTargetId, out selectedProcessSlot, out selectedTargetId);
        }

        if (_processTargetModes[processSlot] == ProcessTargetModeCodes.Self)
        {
            var self = EvaluateProcessTargetPlanChoice(simulation, entityId, entityId, processSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, traceEnabled, parentCandidateIndex, ref candidateCount);
            return SelectBestProcessTargetChoice(self, out selectedRootProcessSlot, out selectedRootTargetId, out selectedProcessSlot, out selectedTargetId);
        }

        if (_processTargetModes[processSlot] == ProcessTargetModeCodes.KindOther &&
            EffectiveTargetKindId(processSlot) > 0)
        {
            var kind = EvaluateKindProcessTargetChoices(simulation, entityId, processSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, traceEnabled, parentCandidateIndex, ref candidateCount);
            return SelectBestProcessTargetChoice(kind, out selectedRootProcessSlot, out selectedRootTargetId, out selectedProcessSlot, out selectedTargetId);
        }

        var any = EvaluateAllProcessTargetChoices(simulation, entityId, processSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, traceEnabled, parentCandidateIndex, ref candidateCount);
        return SelectBestProcessTargetChoice(any, out selectedRootProcessSlot, out selectedRootTargetId, out selectedProcessSlot, out selectedTargetId);
    }

    private ProcessPlanChoice EvaluateKindProcessTargetChoices(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        bool traceEnabled,
        int parentCandidateIndex,
        ref int candidateCount)
    {
        var best = default(ProcessPlanChoice);
        var world = simulation.World;
        for (var targetId = world.FirstEntityOfKind(EffectiveTargetKindId(processSlot));
             targetId > 0;
             targetId = world.NextEntityOfKind(targetId))
        {
            best = EvaluateProcessTargetIfActive(simulation, entityId, targetId, processSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, traceEnabled, parentCandidateIndex, best, ref candidateCount);
        }

        return best;
    }

    private ProcessPlanChoice EvaluateAllProcessTargetChoices(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        bool traceEnabled,
        int parentCandidateIndex,
        ref int candidateCount)
    {
        var best = default(ProcessPlanChoice);
        var world = simulation.World;
        for (var targetId = 1; targetId <= world.EntityHighWatermark; targetId++)
        {
            best = EvaluateProcessTargetIfActive(simulation, entityId, targetId, processSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, traceEnabled, parentCandidateIndex, best, ref candidateCount);
        }

        return best;
    }

    private ProcessPlanChoice EvaluateProcessTargetIfActive(
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
        bool traceEnabled,
        int parentCandidateIndex,
        ProcessPlanChoice best,
        ref int candidateCount)
    {
        var world = simulation.World;
        if (!world.EntityIsActive(targetId) || targetId == entityId)
        {
            return best;
        }

        var candidate = EvaluateProcessTargetPlanChoice(simulation, entityId, targetId, processSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, traceEnabled, parentCandidateIndex, ref candidateCount);
        return candidate.Weight > best.Weight ? candidate : best;
    }

    private ProcessPlanChoice EvaluateProcessTargetPlanChoice(
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
        bool traceEnabled,
        int parentCandidateIndex,
        ref int candidateCount)
    {
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
        return new ProcessPlanChoice
        {
            Weight = weight,
            RootProcessSlot = planProcessSlot > 0 ? processSlot : 0,
            RootTargetId = planProcessSlot > 0 ? targetId : 0,
            ProcessSlot = planProcessSlot,
            TargetId = planTargetId,
        };
    }

    private static float SelectBestProcessTargetChoice(
        ProcessPlanChoice best,
        out int selectedRootProcessSlot,
        out int selectedRootTargetId,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        selectedRootProcessSlot = best.RootProcessSlot;
        selectedRootTargetId = best.RootTargetId;
        selectedProcessSlot = best.ProcessSlot;
        selectedTargetId = best.TargetId;
        return best.Weight;
    }

    private float EvaluatePlanCandidate(
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
        int candidateIndex,
        int parentCandidateIndex,
        bool traceEnabled,
        ref int candidateCount,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        selectedProcessSlot = 0;
        selectedTargetId = 0;
        var world = simulation.World;
        var reward = AdjustedProcessPlanReward(world, processSlot, entityId, targetId);
        if (reward <= 0f)
        {
            EmitPlanTargetTraceIfNeeded(simulation, tick, entityId, candidateIndex, parentCandidateIndex, processSlot, effectScopeCode, effectValueId, effectKindId, targetId, TraceStatusCodes.NoReward, urgency, reward, 0f, 0f, traceEnabled);
            return 0f;
        }

        return EvaluatePlanCandidateWithReward(
            simulation,
            entityId,
            targetId,
            processSlot,
            tick,
            effectScopeCode,
            effectValueId,
            effectKindId,
            depth,
            candidateIndex,
            parentCandidateIndex,
            traceEnabled,
            reward,
            urgency,
            ref candidateCount,
            out selectedProcessSlot,
            out selectedTargetId);
    }

    private float EvaluatePlanCandidateWithReward(
        Simulation simulation,
        int entityId,
        int targetId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int depth,
        int candidateIndex,
        int parentCandidateIndex,
        bool traceEnabled,
        float reward,
        float urgency,
        ref int candidateCount,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        var planningWeight = 1f + global::System.MathF.Log(1f + reward);
        var weightedUrgency = urgency * planningWeight * global::System.MathF.Pow(DepthPenalty, depth);
        var preconditionResult = PreconditionsResult(
            simulation,
            processSlot,
            entityId,
            targetId,
            tick,
            out var blockerScopeCode,
            out var blockerValueId,
            out var blockerKindId,
            out var blockerTargetId);
        if (preconditionResult == PreconditionFailed)
        {
            return EvaluateFailedPreconditionPlanCandidate(
                simulation,
                entityId,
                targetId,
                processSlot,
                tick,
                weightedUrgency,
                depth,
                candidateIndex,
                parentCandidateIndex,
                traceEnabled,
                effectScopeCode,
                effectValueId,
                effectKindId,
                reward,
                blockerScopeCode,
                blockerValueId,
                blockerKindId,
                blockerTargetId,
                ref candidateCount,
                out selectedProcessSlot,
                out selectedTargetId);
        }

        return EvaluateActionableProcessPlanCandidate(
            simulation,
            entityId,
            targetId,
            processSlot,
            tick,
            weightedUrgency,
            depth,
            candidateIndex,
            parentCandidateIndex,
            traceEnabled,
            effectScopeCode,
            effectValueId,
            effectKindId,
            ref candidateCount,
            out selectedProcessSlot,
            out selectedTargetId);
    }

    private float EvaluateFailedPreconditionPlanCandidate(
        Simulation simulation,
        int entityId,
        int targetId,
        int processSlot,
        int tick,
        float weightedUrgency,
        int depth,
        int candidateIndex,
        int parentCandidateIndex,
        bool traceEnabled,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float reward,
        int blockerScopeCode,
        int blockerValueId,
        int blockerKindId,
        int blockerTargetId,
        ref int candidateCount,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        var blockedWeight = EvaluateBlockedProcessPlanCandidate(
            simulation,
            entityId,
            targetId,
            processSlot,
            tick,
            weightedUrgency,
            depth,
            candidateIndex,
            parentCandidateIndex,
            traceEnabled,
            effectScopeCode,
            effectValueId,
            effectKindId,
            reward,
            blockerScopeCode,
            blockerValueId,
            blockerKindId,
            blockerTargetId,
            ref candidateCount,
            out selectedProcessSlot,
            out selectedTargetId);
        if (traceEnabled)
        {
            EmitTargetTrace(
                simulation,
                tick,
                entityId,
                candidateIndex,
                parentCandidateIndex,
                processSlot,
                effectScopeCode,
                effectValueId,
                effectKindId,
                targetId,
                TraceStatusCodes.PreconditionFailed,
                weightedUrgency,
                reward,
                0f,
                blockedWeight);
        }

        return blockedWeight;
    }

    private float AdjustedProcessPlanReward(WorldState world, int processSlot, int entityId, int targetId)
    {
        var reward = EvaluateReward(world, processSlot, entityId, targetId);
        if (_processTargetModes[processSlot] == ProcessTargetModeCodes.Self && reward < MinimumSelfDirectedReward)
        {
            return MinimumSelfDirectedReward;
        }

        return _processTargetModes[processSlot] == ProcessTargetModeCodes.Self || reward > 0f ? reward : 0f;
    }

    private float EvaluateBlockedProcessPlanCandidate(
        Simulation simulation,
        int entityId, int targetId, int processSlot, int tick,
        float weightedUrgency,
        int depth, int candidateIndex, int parentCandidateIndex,
        bool traceEnabled,
        int effectScopeCode, int effectValueId, int effectKindId,
        float reward,
        int blockerScopeCode, int blockerValueId, int blockerKindId, int blockerTargetId,
        ref int candidateCount,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        selectedProcessSlot = 0;
        selectedTargetId = 0;
        if (depth >= MaxPlanningDepth || blockerScopeCode == 0 || blockerTargetId == 0)
        {
            EmitSupportTraceIfNeeded(simulation, tick, entityId, candidateIndex, parentCandidateIndex, processSlot, effectScopeCode, effectValueId, effectKindId, targetId, TraceStatusCodes.NoTarget, weightedUrgency, reward, 0f, traceEnabled);
            return 0f;
        }

        var supportWeight = EvaluateBlockerSupportPlan(
            simulation,
            entityId,
            tick,
            weightedUrgency,
            depth,
            blockerScopeCode,
            blockerValueId,
            blockerKindId,
            blockerTargetId,
            traceEnabled,
            candidateIndex,
            ref candidateCount,
            out selectedProcessSlot,
            out selectedTargetId);
        EmitSupportTraceIfNeeded(simulation, tick, entityId, candidateIndex, parentCandidateIndex, selectedProcessSlot, blockerScopeCode, blockerValueId, blockerKindId, blockerTargetId, supportWeight > 0f ? TraceStatusCodes.SupportSelected : TraceStatusCodes.NoTarget, weightedUrgency, reward, supportWeight, traceEnabled);
        return supportWeight;
    }

    private float EvaluateBlockerSupportPlan(
        Simulation simulation,
        int entityId,
        int tick,
        float weightedUrgency,
        int depth,
        int blockerScopeCode,
        int blockerValueId,
        int blockerKindId,
        int blockerTargetId,
        bool traceEnabled,
        int candidateIndex,
        ref int candidateCount,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
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
            traceEnabled,
            parentCandidateIndex: candidateIndex,
            ref candidateCount,
            out _,
            out _,
            out selectedProcessSlot,
            out selectedTargetId);
    }

    private float EvaluateActionableProcessPlanCandidate(
        Simulation simulation,
        int entityId,
        int targetId,
        int processSlot,
        int tick,
        float weightedUrgency,
        int depth,
        int candidateIndex,
        int parentCandidateIndex,
        bool traceEnabled,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        ref int candidateCount,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        var world = simulation.World;
        var score = EvaluateScore(world, processSlot, entityId, targetId);
        var recentActionCount = CountRecentActions(entityId, _processIds[processSlot], tick - RecencyWindow);
        for (var i = 0; i < recentActionCount; i++)
        {
            score *= RecencyDecayFactor;
        }

        var immediateWeight = weightedUrgency * Max(score, MinimumActionableScoreWeight);
        var bestWeight = immediateWeight;
        selectedProcessSlot = processSlot;
        selectedTargetId = targetId;

        if (depth < MaxPlanningDepth)
        {
            var supportWeight = EvaluateBestSupportPlan(
                simulation,
                entityId,
                targetId,
                processSlot,
                tick,
                weightedUrgency,
                score,
                depth + 1,
                candidateIndex,
                traceEnabled,
                ref candidateCount,
                out var supportProcessSlot,
                out var supportTargetId);
            if (supportWeight > bestWeight)
            {
                bestWeight = supportWeight;
                selectedProcessSlot = supportProcessSlot;
                selectedTargetId = supportTargetId;
            }
        }

        EmitPlanTargetTraceIfNeeded(
            simulation,
            tick,
            entityId,
            candidateIndex,
            parentCandidateIndex,
            selectedProcessSlot,
            effectScopeCode,
            effectValueId,
            effectKindId,
            selectedTargetId,
            selectedProcessSlot == processSlot && selectedTargetId == targetId
                ? TraceStatusCodes.Actionable
                : TraceStatusCodes.SupportSelected,
            weightedUrgency,
            score,
            immediateWeight,
            bestWeight,
            traceEnabled);

        return bestWeight <= 0f ? 0f : bestWeight;
    }

    private float EvaluateBestSupportPlan(
        Simulation simulation,
        int entityId,
        int targetId,
        int processSlot,
        int tick,
        float weightedUrgency,
        float processScore,
        int depth,
        int parentCandidateIndex,
        bool traceEnabled,
        ref int candidateCount,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        selectedProcessSlot = 0;
        selectedTargetId = 0;
        var bestWeight = 0f;
        var first = _processSupportRuleStarts[processSlot];
        var end = first + _processSupportRuleCounts[processSlot];
        var scoreHeadroom = 1f - Clamp01(processScore);
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
                traceEnabled,
                parentCandidateIndex,
                ref candidateCount,
                out _,
                out _,
                out var supportProcessSlot,
                out var supportTargetId);
            if (supportWeight > bestWeight)
            {
                bestWeight = supportWeight;
                selectedProcessSlot = supportProcessSlot;
                selectedTargetId = supportTargetId;
            }
        }

        return bestWeight;
    }

}
