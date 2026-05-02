namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GambitProgramRuntime
{
    private struct GambitPlanChoice
    {
        public float Weight;
        public int RootGambitSlot;
        public int RootTargetId;
        public int GambitSlot;
        public int TargetId;
    }

    private sealed class GambitTargetPlanState(int candidateCount)
    {
        public int CandidateCount = candidateCount;
        public GambitPlanChoice Best;
    }

    private float EvaluatePlannedCandidates(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        float selectRoll,
        ref int candidateCount,
        out int selectedGambitSlot,
        out int selectedTargetId,
        out int selectedRootGambitSlot,
        out int selectedRootTargetId,
        out int selectedEffectKey,
        out int selectedLocalityKey)
    {
        selectedGambitSlot = 0;
        selectedTargetId = 0;
        selectedRootGambitSlot = 0;
        selectedRootTargetId = 0;
        selectedEffectKey = 0;
        selectedLocalityKey = 0;
        var planCount = BuildGambitPlans(simulation, entityId, kindId, tick, ref candidateCount);
        if (planCount == 0)
        {
            return 0f;
        }

        ApplyLocalityBundling(simulation.World, entityId, planCount);
        var total = GambitPlanAdjustedWeightTotal(planCount);
        return selectRoll < 0f
            ? total
            : SelectGambitPlanCandidate(entityId, tick, planCount, selectRoll, total, out selectedGambitSlot, out selectedTargetId, out selectedRootGambitSlot, out selectedRootTargetId, out selectedEffectKey, out selectedLocalityKey);
    }

    private int BuildGambitPlans(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        ref int candidateCount)
    {
        var planCount = 0;
        for (var objective = 1; objective <= _objectiveCount; objective++)
        {
            TryAddObjectiveGambitPlan(simulation, entityId, kindId, tick, objective, ref candidateCount, ref planCount);
        }

        return planCount;
    }

    private void TryAddObjectiveGambitPlan(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        int objective,
        ref int candidateCount,
        ref int planCount)
    {
        if (_objectiveKindIds[objective] != kindId)
        {
            return;
        }

        var urgency = ObjectiveUrgency(simulation.World, entityId, objective);
        if (urgency < MinimumObjectiveUrgency)
        {
            return;
        }

        var weight = EvaluateBestPlanForEffect(
            simulation,
            entityId,
            kindId,
            tick,
            _objectiveEffectScopeCodes[objective],
            _objectiveEffectValueIds[objective],
            _objectiveEffectKindIds[objective],
            forcedTargetId: 0,
            urgency,
            depth: 0,
            selectRoll: -1f,
            ref candidateCount,
            out var rootGambitSlot,
            out var rootTargetId,
            out var gambitSlot,
            out var targetId);
        if (weight <= 0f || rootGambitSlot == 0 || rootTargetId == 0 || gambitSlot == 0 || targetId == 0)
        {
            return;
        }

        if (GoapGambitTargetReservationsEnabled() &&
            GambitActionTargetAtCapacity(rootGambitSlot, rootTargetId, tick, capacity: 1))
        {
            return;
        }

        planCount++;
        _gambitPlanEffectKeys[planCount] = EffectKey(
            _objectiveEffectScopeCodes[objective],
            _objectiveEffectValueIds[objective],
            _objectiveEffectKindIds[objective]);
        _gambitPlanRootGambitSlots[planCount] = rootGambitSlot;
        _gambitPlanRootTargetIds[planCount] = rootTargetId;
        _gambitPlanGambitSlots[planCount] = gambitSlot;
        _gambitPlanTargetIds[planCount] = targetId;
        _gambitPlanLocalityKeys[planCount] = LocalityKey(simulation.World, rootTargetId);
        _gambitPlanWeights[planCount] = weight;
    }

    private float GambitPlanAdjustedWeightTotal(int planCount)
    {
        var total = 0f;
        for (var plan = 1; plan <= planCount; plan++)
        {
            total += _gambitPlanAdjustedWeights[plan];
        }

        return total;
    }

    private float SelectGambitPlanCandidate(
        int entityId,
        int tick,
        int planCount,
        float selectRoll,
        float total,
        out int selectedGambitSlot,
        out int selectedTargetId,
        out int selectedRootGambitSlot,
        out int selectedRootTargetId,
        out int selectedEffectKey,
        out int selectedLocalityKey)
    {
        selectedGambitSlot = 0;
        selectedTargetId = 0;
        selectedRootGambitSlot = 0;
        selectedRootTargetId = 0;
        selectedEffectKey = 0;
        selectedLocalityKey = 0;
        var cumulative = 0f;
        for (var plan = 1; plan <= planCount; plan++)
        {
            cumulative += _gambitPlanAdjustedWeights[plan];
            if (selectRoll > cumulative)
            {
                continue;
            }

            selectedGambitSlot = _gambitPlanGambitSlots[plan];
            selectedTargetId = _gambitPlanTargetIds[plan];
            selectedRootGambitSlot = _gambitPlanRootGambitSlots[plan];
            selectedRootTargetId = _gambitPlanRootTargetIds[plan];
            selectedEffectKey = _gambitPlanEffectKeys[plan];
            selectedLocalityKey = _gambitPlanLocalityKeys[plan];
            _committedPlanTargets.Set(entityId, _gambitPlanEffectKeys[plan], _gambitPlanRootTargetIds[plan]);
            _committedPlanGambitSlots.Set(entityId, _gambitPlanEffectKeys[plan], _gambitPlanRootGambitSlots[plan]);
            _committedPlanLocalities.Set(entityId, GambitLocalityCommitmentKey, _gambitPlanLocalityKeys[plan]);
            if (GoapGambitTargetReservationsEnabled())
            {
                ReserveGambitActionTarget(_gambitPlanRootTargetIds[plan], _gambitPlanRootGambitSlots[plan], tick);
            }

            return total;
        }

        return total;
    }

    private void ApplyLocalityBundling(WorldState world, int entityId, int planCount)
    {
        var selectedLocalityKey = SelectLocalityBundle(world, entityId, planCount);
        for (var plan = 1; plan <= planCount; plan++)
        {
            _gambitPlanAdjustedWeights[plan] = _gambitPlanWeights[plan] *
                (_gambitPlanLocalityKeys[plan] == selectedLocalityKey ? LocalityGoalSelectionBoost : 1f);
        }
    }

    private int SelectLocalityBundle(WorldState world, int entityId, int planCount)
    {
        var bestKey = 0;
        var bestScore = float.NegativeInfinity;
        var committedKey = _committedPlanLocalities.GetOrDefault(entityId, GambitLocalityCommitmentKey);
        var committedScore = 0f;
        for (var plan = 1; plan <= planCount; plan++)
        {
            var key = _gambitPlanLocalityKeys[plan];
            if (AlreadyScoredLocality(planCount, plan, key))
            {
                continue;
            }

            var score = LocalityBundleScore(world, entityId, planCount, key);
            if (key == committedKey)
            {
                committedScore = score;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestKey = key;
            }
        }

        if (committedKey != 0 &&
            committedScore > 0f &&
            bestScore <= committedScore * (1f + TargetSwitchHysteresisRatio))
        {
            return committedKey;
        }

        return bestKey;
    }

    private bool AlreadyScoredLocality(int planCount, int plan, int localityKey)
    {
        for (var previous = 1; previous < plan && previous <= planCount; previous++)
        {
            if (_gambitPlanLocalityKeys[previous] == localityKey)
            {
                return true;
            }
        }

        return false;
    }

    private float LocalityBundleScore(WorldState world, int entityId, int planCount, int localityKey)
    {
        var count = 0;
        var distinctEffects = 0;
        var totalWeight = 0f;
        var maxWeight = 0f;
        for (var plan = 1; plan <= planCount; plan++)
        {
            if (_gambitPlanLocalityKeys[plan] != localityKey)
            {
                continue;
            }

            count++;
            totalWeight += _gambitPlanWeights[plan];
            if (_gambitPlanWeights[plan] > maxWeight)
            {
                maxWeight = _gambitPlanWeights[plan];
            }

            if (FirstPlanForEffectAtLocality(plan, localityKey, _gambitPlanEffectKeys[plan]))
            {
                distinctEffects++;
            }
        }

        if (count == 0)
        {
            return 0f;
        }

        var rootTargetId = FirstTargetAtLocality(planCount, localityKey);
        var distance = rootTargetId > 0
            ? KernelQuery.HexDistance(
                world.EntityPhysicalQ(entityId),
                world.EntityPhysicalR(entityId),
                world.EntityPhysicalQ(rootTargetId),
                world.EntityPhysicalR(rootTargetId))
            : 0;
        var committedKey = _committedPlanLocalities.GetOrDefault(entityId, GambitLocalityCommitmentKey);
        return maxWeight +
               ((totalWeight - maxWeight) * LocalityAdditionalPlanWeight) +
               ((count - 1) * LocalityDensityBonus) +
               ((distinctEffects - 1) * LocalityDistinctEffectBonus) +
               (committedKey == localityKey ? LocalityCommitmentBonus : 0f) +
               (distance == 0 ? LocalityAlreadyThereBonus : 0f) -
               (distance * LocalityDistancePenaltyPerStep);
    }

    private bool FirstPlanForEffectAtLocality(int plan, int localityKey, int effectKey)
    {
        for (var previous = 1; previous < plan; previous++)
        {
            if (_gambitPlanLocalityKeys[previous] == localityKey &&
                _gambitPlanEffectKeys[previous] == effectKey)
            {
                return false;
            }
        }

        return true;
    }

    private int FirstTargetAtLocality(int planCount, int localityKey)
    {
        for (var plan = 1; plan <= planCount; plan++)
        {
            if (_gambitPlanLocalityKeys[plan] == localityKey)
            {
                return _gambitPlanRootTargetIds[plan];
            }
        }

        return 0;
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
        if (depth > MaxPlanningDepth)
        {
            return 0f;
        }

        var useCommitment = depth == 0;
        var effectKey = useCommitment ? EffectKey(effectScopeCode, effectValueId, effectKindId) : 0;
        var best = EvaluateGambitPlanChoices(
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
            ref candidateCount,
            out var committed);
        best = ChooseCommittedGambitPlanChoice(best, committed, useCommitment);
        SelectGambitPlanChoice(
            entityId,
            effectKey,
            useCommitment,
            selectRoll,
            best,
            out selectedRootGambitSlot,
            out selectedRootTargetId,
            out selectedGambitSlot,
            out selectedTargetId);
        return best.Weight;
    }

    private GambitPlanChoice EvaluateGambitPlanChoices(
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
        ref int candidateCount,
        out GambitPlanChoice committed)
    {
        var best = default(GambitPlanChoice);
        committed = default;
        var previousTargetId = useCommitment ? _committedPlanTargets.GetOrDefault(entityId, effectKey) : 0;
        var previousGambitSlot = useCommitment ? _committedPlanGambitSlots.GetOrDefault(entityId, effectKey) : 0;
        for (var gambitSlot = 1; gambitSlot <= _gambitCount; gambitSlot++)
        {
            if (!GambitCanPlanForEffect(gambitSlot, kindId, entityId, tick, effectScopeCode, effectValueId, effectKindId))
            {
                continue;
            }

            var candidate = EvaluateGambitPlanChoice(simulation, entityId, gambitSlot, tick, effectScopeCode, effectValueId, effectKindId, forcedTargetId, urgency, depth, ref candidateCount);
            TrackGambitPlanChoice(candidate, previousGambitSlot, previousTargetId, ref best, ref committed);
            if (TryEvaluatePreviousGambitPlanChoice(simulation, entityId, gambitSlot, tick, effectScopeCode, effectValueId, effectKindId, forcedTargetId, urgency, depth, useCommitment, previousGambitSlot, previousTargetId, ref candidateCount, out var previous))
            {
                committed = previous;
            }
        }

        return best;
    }

    private bool GambitCanPlanForEffect(
        int gambitSlot,
        int kindId,
        int entityId,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId)
    {
        return GambitCanRunForEntity(gambitSlot, kindId, entityId, tick) &&
            GambitServesEffect(gambitSlot, effectScopeCode, effectValueId, effectKindId);
    }

    private GambitPlanChoice EvaluateGambitPlanChoice(
        Simulation simulation,
        int entityId,
        int gambitSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int forcedTargetId,
        float urgency,
        int depth,
        ref int candidateCount)
    {
        var weight = EvaluateBestTargetsForGambit(
            simulation,
            entityId,
            gambitSlot,
            tick,
            effectScopeCode,
            effectValueId,
            effectKindId,
            forcedTargetId,
            urgency,
            depth,
            ref candidateCount,
            out var rootGambitSlot,
            out var rootTargetId,
            out var leafGambitSlot,
            out var leafTargetId);
        return new GambitPlanChoice
        {
            Weight = weight,
            RootGambitSlot = rootGambitSlot,
            RootTargetId = rootTargetId,
            GambitSlot = leafGambitSlot,
            TargetId = leafTargetId,
        };
    }

    private static void TrackGambitPlanChoice(
        GambitPlanChoice candidate,
        int previousGambitSlot,
        int previousTargetId,
        ref GambitPlanChoice best,
        ref GambitPlanChoice committed)
    {
        if (candidate.RootGambitSlot == previousGambitSlot && candidate.RootTargetId == previousTargetId)
        {
            committed = candidate;
        }

        if (candidate.Weight > best.Weight)
        {
            best = candidate;
        }
    }

    private bool TryEvaluatePreviousGambitPlanChoice(
        Simulation simulation,
        int entityId,
        int gambitSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int forcedTargetId,
        float urgency,
        int depth,
        bool useCommitment,
        int previousGambitSlot,
        int previousTargetId,
        ref int candidateCount,
        out GambitPlanChoice previous)
    {
        previous = default;
        if (!useCommitment ||
            previousGambitSlot != gambitSlot ||
            previousTargetId <= 0 ||
            !TargetEligibleForGambit(simulation.World, entityId, gambitSlot, forcedTargetId, previousTargetId))
        {
            return false;
        }

        candidateCount++;
        var weight = EvaluatePlanCandidate(
            simulation,
            entityId,
            previousTargetId,
            gambitSlot,
            tick,
            effectScopeCode,
            effectValueId,
            effectKindId,
            urgency,
            depth,
            ref candidateCount,
            out var leafGambitSlot,
            out var leafTargetId);
        if (weight <= 0f)
        {
            return false;
        }

        previous = new GambitPlanChoice
        {
            Weight = weight,
            RootGambitSlot = gambitSlot,
            RootTargetId = previousTargetId,
            GambitSlot = leafGambitSlot,
            TargetId = leafTargetId,
        };
        return true;
    }

    private static GambitPlanChoice ChooseCommittedGambitPlanChoice(
        GambitPlanChoice best,
        GambitPlanChoice committed,
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

    private void SelectGambitPlanChoice(
        int entityId,
        int effectKey,
        bool useCommitment,
        float selectRoll,
        GambitPlanChoice best,
        out int selectedRootGambitSlot,
        out int selectedRootTargetId,
        out int selectedGambitSlot,
        out int selectedTargetId)
    {
        selectedRootGambitSlot = 0;
        selectedRootTargetId = 0;
        selectedGambitSlot = 0;
        selectedTargetId = 0;
        if ((selectRoll >= 0f && selectRoll > best.Weight) || best.GambitSlot == 0)
        {
            return;
        }

        selectedRootGambitSlot = best.RootGambitSlot;
        selectedRootTargetId = best.RootTargetId;
        selectedGambitSlot = best.GambitSlot;
        selectedTargetId = best.TargetId;
        if (useCommitment && selectRoll >= 0f)
        {
            _committedPlanTargets.Set(entityId, effectKey, best.RootTargetId);
            _committedPlanGambitSlots.Set(entityId, effectKey, best.RootGambitSlot);
        }
    }

    private float EvaluateBestTargetsForGambit(
        Simulation simulation,
        int entityId,
        int gambitSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int forcedTargetId,
        float urgency,
        int depth,
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
        if (forcedTargetId > 0)
        {
            var forced = simulation.World.EntityIsActive(forcedTargetId)
                ? EvaluateGambitTargetPlanChoice(simulation, entityId, forcedTargetId, gambitSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, ref candidateCount)
                : default;
            return SelectBestGambitTargetChoice(forced, out selectedRootGambitSlot, out selectedRootTargetId, out selectedGambitSlot, out selectedTargetId);
        }

        var selectorStart = _gambitProviderSelectorStarts[gambitSlot];
        var selectorCount = _gambitProviderSelectorCounts[gambitSlot];
        if (selectorCount > 0)
        {
            return EvaluateProviderSelectorTargets(
                simulation,
                entityId,
                gambitSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selectorStart,
                selectorCount,
                ref candidateCount,
                out selectedRootGambitSlot,
                out selectedRootTargetId,
                out selectedGambitSlot,
                out selectedTargetId);
        }

        if (_gambitTargetModes[gambitSlot] == BehaviorTargetModeCodes.Self)
        {
            var self = EvaluateGambitTargetPlanChoice(simulation, entityId, entityId, gambitSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, ref candidateCount);
            return SelectBestGambitTargetChoice(self, out selectedRootGambitSlot, out selectedRootTargetId, out selectedGambitSlot, out selectedTargetId);
        }

        if (_gambitTargetModes[gambitSlot] == BehaviorTargetModeCodes.KindOther &&
            _gambitTargetKindIds[gambitSlot] > 0)
        {
            var kind = EvaluateKindGambitTargetChoices(simulation, entityId, gambitSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, ref candidateCount);
            return SelectBestGambitTargetChoice(kind, out selectedRootGambitSlot, out selectedRootTargetId, out selectedGambitSlot, out selectedTargetId);
        }

        var any = EvaluateAllGambitTargetChoices(simulation, entityId, gambitSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, ref candidateCount);
        return SelectBestGambitTargetChoice(any, out selectedRootGambitSlot, out selectedRootTargetId, out selectedGambitSlot, out selectedTargetId);
    }

    private GambitPlanChoice EvaluateKindGambitTargetChoices(
        Simulation simulation,
        int entityId,
        int gambitSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        ref int candidateCount)
    {
        var state = new GambitTargetPlanState(candidateCount);
        var world = simulation.World;
        KernelQuery.ForEachEntityOfKind(
            world,
            _gambitTargetKindIds[gambitSlot],
            targetId =>
            {
                EvaluateGambitTargetIfActive(simulation, entityId, targetId, gambitSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, state);
                return true;
            });

        candidateCount = state.CandidateCount;
        return state.Best;
    }

    private GambitPlanChoice EvaluateAllGambitTargetChoices(
        Simulation simulation,
        int entityId,
        int gambitSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        ref int candidateCount)
    {
        var state = new GambitTargetPlanState(candidateCount);
        var world = simulation.World;
        KernelQuery.ForEachEntity(
            world,
            targetId =>
            {
                EvaluateGambitTargetIfActive(simulation, entityId, targetId, gambitSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, state);
                return true;
            });

        candidateCount = state.CandidateCount;
        return state.Best;
    }

    private void EvaluateGambitTargetIfActive(
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
        GambitTargetPlanState state)
    {
        var world = simulation.World;
        if (!world.EntityIsActive(targetId) || targetId == entityId)
        {
            return;
        }

        var candidate = EvaluateGambitTargetPlanChoice(simulation, entityId, targetId, gambitSlot, tick, effectScopeCode, effectValueId, effectKindId, urgency, depth, ref state.CandidateCount);
        if (candidate.Weight > state.Best.Weight)
        {
            state.Best = candidate;
        }
    }

    private GambitPlanChoice EvaluateGambitTargetPlanChoice(
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
        ref int candidateCount)
    {
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
        return new GambitPlanChoice
        {
            Weight = weight,
            RootGambitSlot = planGambitSlot > 0 ? gambitSlot : 0,
            RootTargetId = planGambitSlot > 0 ? targetId : 0,
            GambitSlot = planGambitSlot,
            TargetId = planTargetId,
        };
    }

    private static float SelectBestGambitTargetChoice(
        GambitPlanChoice best,
        out int selectedRootGambitSlot,
        out int selectedRootTargetId,
        out int selectedGambitSlot,
        out int selectedTargetId)
    {
        selectedRootGambitSlot = best.RootGambitSlot;
        selectedRootTargetId = best.RootTargetId;
        selectedGambitSlot = best.GambitSlot;
        selectedTargetId = best.TargetId;
        return best.Weight;
    }

}
