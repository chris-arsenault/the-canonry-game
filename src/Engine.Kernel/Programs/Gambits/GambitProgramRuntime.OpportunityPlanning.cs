namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GambitProgramRuntime
{
    private const float OpportunityBaseFit = 0.25f;
    private const float OpportunityNoObjectiveFit = 0.5f;
    private const float OpportunityPhysicalDistancePenalty = 0.05f;
    private const float OpportunitySemanticDistancePenalty = 0.03f;

    private struct OpportunityGambitCandidate
    {
        public float Weight;
        public int CandidateIndex;
        public int LeafGambitSlot;
        public int LeafTargetId;
        public int EffectScopeCode;
        public int EffectValueId;
        public int EffectKindId;
        public int EffectKeyValue;
    }

    internal PlanningDecision PlanOpportunity(Simulation simulation, in PlanningRequest request)
    {
        var candidateCount = 0;
        var totalWeight = EvaluateOpportunityGambitPlanTotal(simulation, request, ref candidateCount);
        if (candidateCount == 0)
        {
            return simulation.Planners.EmptyDecision(
                PlanningStatusCodes.NoCandidates,
                request.ActionFamilyCode,
                candidateCount,
                0f);
        }

        if (totalWeight <= 0f)
        {
            return simulation.Planners.EmptyDecision(
                PlanningStatusCodes.NoWeight,
                request.ActionFamilyCode,
                candidateCount,
                totalWeight);
        }

        return SelectOpportunityGambitPlan(simulation, request, totalWeight, candidateCount);
    }

    private float EvaluateOpportunityGambitPlanTotal(Simulation simulation, in PlanningRequest request, ref int candidateCount)
    {
        return EvaluateOpportunityGambitPlans(
            simulation,
            request.ActorEntityId,
            request.ActorKindId,
            request.Tick,
            selectRoll: -1f,
            ref candidateCount,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _);
    }

    private PlanningDecision SelectOpportunityGambitPlan(
        Simulation simulation,
        in PlanningRequest request,
        float totalWeight,
        int candidateCount)
    {
        var selectedCandidateCount = 0;
        _ = EvaluateOpportunityGambitPlans(
            simulation,
            request.ActorEntityId,
            request.ActorKindId,
            request.Tick,
            ClampSelectionRoll(request.SelectionRoll) * totalWeight,
            ref selectedCandidateCount,
            out var selectedGambitSlot,
            out var selectedTargetId,
            out var selectedRootGambitSlot,
            out var selectedRootTargetId,
            out var selectedEffectKey,
            out var selectedLocalityKey);
        if (selectedGambitSlot == 0 || selectedTargetId == 0)
        {
            return simulation.Planners.EmptyDecision(
                PlanningStatusCodes.NoWeight,
                request.ActionFamilyCode,
                candidateCount,
                totalWeight);
        }

        ReserveOpportunityGambitTarget(selectedRootTargetId, selectedRootGambitSlot, request.Tick);
        return simulation.Planners.WriteSelectedPlan(
            request,
            selectedEffectKey,
            selectedRootGambitSlot,
            selectedRootTargetId,
            selectedGambitSlot,
            selectedTargetId,
            selectedLocalityKey,
            candidateCount,
            totalWeight);
    }

    private float EvaluateOpportunityGambitPlans(
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
        var total = 0f;
        var traceEnabled = selectRoll < 0f;
        for (var gambitSlot = 1; gambitSlot <= _gambitCount; gambitSlot++)
        {
            if (!GambitCanRunForEntity(gambitSlot, kindId, entityId, tick) ||
                GambitIsPositioningOnly(gambitSlot))
            {
                continue;
            }

            total = EvaluateOpportunityGambitTargets(
                simulation,
                entityId,
                gambitSlot,
                tick,
                selectRoll,
                traceEnabled,
                total,
                ref candidateCount,
                ref selectedGambitSlot,
                ref selectedTargetId,
                ref selectedRootGambitSlot,
                ref selectedRootTargetId,
                ref selectedEffectKey,
                ref selectedLocalityKey);
            if (selectedGambitSlot > 0)
            {
                return total;
            }
        }

        return total;
    }

    private float EvaluateOpportunityGambitTargets(
        Simulation simulation,
        int entityId,
        int gambitSlot,
        int tick,
        float selectRoll,
        bool traceEnabled,
        float total,
        ref int candidateCount,
        ref int selectedGambitSlot,
        ref int selectedTargetId,
        ref int selectedRootGambitSlot,
        ref int selectedRootTargetId,
        ref int selectedEffectKey,
        ref int selectedLocalityKey)
    {
        var world = simulation.World;
        for (var targetId = 1; targetId <= world.EntityHighWatermark; targetId++)
        {
            var candidate = EvaluateOpportunityGambitTarget(simulation, entityId, gambitSlot, targetId, tick, traceEnabled, world, ref candidateCount);
            if (candidate.Weight <= 0f)
            {
                continue;
            }

            total += candidate.Weight;
            if (selectRoll < 0f || selectRoll > total)
            {
                continue;
            }

            SelectOpportunityGambitCandidate(
                simulation,
                world,
                tick,
                entityId,
                candidate,
                gambitSlot,
                targetId,
                ref selectedGambitSlot,
                ref selectedTargetId,
                ref selectedRootGambitSlot,
                ref selectedRootTargetId,
                ref selectedEffectKey,
                ref selectedLocalityKey);
            return total;
        }

        return total;
    }

    private OpportunityGambitCandidate EvaluateOpportunityGambitTarget(
        Simulation simulation,
        int entityId,
        int gambitSlot,
        int targetId,
        int tick,
        bool traceEnabled,
        WorldState world,
        ref int candidateCount)
    {
        if (!TargetEligibleForGambit(world, entityId, gambitSlot, forcedTargetId: 0, targetId))
        {
            return default;
        }

        candidateCount++;
        var candidateIndex = candidateCount;
        var weight = EvaluateOpportunityGambitCandidate(
            simulation,
            entityId,
            gambitSlot,
            targetId,
            tick,
            candidateIndex,
            traceEnabled,
            ref candidateCount,
            out var leafGambitSlot,
            out var leafTargetId,
            out var effectScopeCode,
            out var effectValueId,
            out var effectKindId,
            out var effectKey);
        return new OpportunityGambitCandidate
        {
            Weight = weight,
            CandidateIndex = candidateIndex,
            LeafGambitSlot = leafGambitSlot,
            LeafTargetId = leafTargetId,
            EffectScopeCode = effectScopeCode,
            EffectValueId = effectValueId,
            EffectKindId = effectKindId,
            EffectKeyValue = effectKey,
        };
    }

    private void SelectOpportunityGambitCandidate(
        Simulation simulation,
        WorldState world,
        int tick,
        int entityId,
        OpportunityGambitCandidate candidate,
        int gambitSlot,
        int targetId,
        ref int selectedGambitSlot,
        ref int selectedTargetId,
        ref int selectedRootGambitSlot,
        ref int selectedRootTargetId,
        ref int selectedEffectKey,
        ref int selectedLocalityKey)
    {
        selectedGambitSlot = candidate.LeafGambitSlot;
        selectedTargetId = candidate.LeafTargetId;
        selectedRootGambitSlot = gambitSlot;
        selectedRootTargetId = targetId;
        selectedEffectKey = candidate.EffectKeyValue;
        selectedLocalityKey = LocalityKey(world, targetId);
        EmitOpportunityGambitTrace(
            simulation,
            tick,
            entityId,
            candidate.CandidateIndex,
            gambitSlot,
            candidate.EffectScopeCode,
            candidate.EffectValueId,
            candidate.EffectKindId,
            targetId,
            TraceStatusCodes.Selected,
            0f,
            candidate.Weight,
            candidate.Weight);
    }

    private float EvaluateOpportunityGambitCandidate(
        Simulation simulation,
        int entityId,
        int gambitSlot,
        int targetId,
        int tick,
        int candidateIndex,
        bool traceEnabled,
        ref int candidateCount,
        out int selectedGambitSlot,
        out int selectedTargetId,
        out int effectScopeCode,
        out int effectValueId,
        out int effectKindId,
        out int effectKey)
    {
        OpportunityGambitEffect(gambitSlot, out effectScopeCode, out effectValueId, out effectKindId);
        effectKey = EffectKey(effectScopeCode, effectValueId, effectKindId);
        selectedGambitSlot = 0;
        selectedTargetId = 0;
        var reward = AdjustedGambitPlanReward(simulation.World, gambitSlot, entityId, targetId);
        if (reward <= 0f)
        {
            EmitOpportunityGambitTraceIfNeeded(simulation, tick, entityId, candidateIndex, gambitSlot, effectScopeCode, effectValueId, effectKindId, targetId, TraceStatusCodes.NoReward, 0f, reward, 0f, traceEnabled);
            return 0f;
        }

        if (OpportunityGambitTargetAtCapacity(gambitSlot, targetId, tick))
        {
            EmitOpportunityGambitTraceIfNeeded(simulation, tick, entityId, candidateIndex, gambitSlot, effectScopeCode, effectValueId, effectKindId, targetId, TraceStatusCodes.CapacityReached, 0f, reward, 0f, traceEnabled);
            return 0f;
        }

        var urgency = OpportunityGambitUrgency(simulation.World, entityId, gambitSlot, targetId);
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
            depth: 0,
            ref candidateCount,
            out selectedGambitSlot,
            out selectedTargetId);
        EmitOpportunityGambitTraceIfNeeded(
            simulation,
            tick,
            entityId,
            candidateIndex,
            gambitSlot,
            effectScopeCode,
            effectValueId,
            effectKindId,
            targetId,
            weight > 0f ? TraceStatusCodes.Offered : TraceStatusCodes.NoTarget,
            urgency,
            reward,
            weight,
            traceEnabled);
        return weight;
    }

    private float OpportunityGambitUrgency(WorldState world, int entityId, int gambitSlot, int targetId)
    {
        var fit = OpportunityNoObjectiveFit;
        var matchedObjective = false;
        var first = _gambitGoalEffectStarts[gambitSlot];
        var end = first + _gambitGoalEffectCounts[gambitSlot];
        for (var effect = first; effect < end; effect++)
        {
            var objectiveUrgency = BestObjectiveUrgencyForGambitEffect(
                world,
                entityId,
                _goalEffectScopeCodes[effect],
                _goalEffectValueIds[effect],
                _goalEffectKindIds[effect]);
            if (objectiveUrgency <= 0f)
            {
                continue;
            }

            matchedObjective = true;
            fit = Max(fit, OpportunityBaseFit + objectiveUrgency);
        }

        if (!matchedObjective && _gambitGoalEffectCounts[gambitSlot] == 0)
        {
            fit = OpportunityBaseFit;
        }

        var physicalDistance = PlaneDistance(world, entityId, targetId, PlaneCodes.Physical);
        var semanticDistance = PlaneDistance(world, entityId, targetId, PlaneCodes.Semantic);
        return fit / (1f +
            (physicalDistance * OpportunityPhysicalDistancePenalty) +
            (semanticDistance * OpportunitySemanticDistancePenalty));
    }

    private float BestObjectiveUrgencyForGambitEffect(
        WorldState world,
        int entityId,
        int effectScopeCode,
        int effectValueId,
        int effectKindId)
    {
        var kindId = world.EntityKindId(entityId);
        var best = 0f;
        for (var objective = 1; objective <= _objectiveCount; objective++)
        {
            if (_objectiveKindIds[objective] != kindId ||
                _objectiveEffectScopeCodes[objective] != effectScopeCode ||
                _objectiveEffectValueIds[objective] != effectValueId ||
                (effectKindId > 0 &&
                 _objectiveEffectKindIds[objective] > 0 &&
                 _objectiveEffectKindIds[objective] != effectKindId))
            {
                continue;
            }

            best = Max(best, ObjectiveUrgency(world, entityId, objective));
        }

        return best;
    }

    private void OpportunityGambitEffect(int gambitSlot, out int effectScopeCode, out int effectValueId, out int effectKindId)
    {
        var first = _gambitGoalEffectStarts[gambitSlot];
        if (_gambitGoalEffectCounts[gambitSlot] > 0)
        {
            effectScopeCode = _goalEffectScopeCodes[first];
            effectValueId = _goalEffectValueIds[first];
            effectKindId = _goalEffectKindIds[first];
            return;
        }

        effectScopeCode = ProcessEffectScopeCodes.Structural;
        effectValueId = _gambitIds[gambitSlot];
        effectKindId = _gambitKindIds[gambitSlot];
    }

    private bool OpportunityGambitTargetAtCapacity(int gambitSlot, int targetId, int tick)
    {
        var configured = Tuning.Planning.OpportunityTargetCapacity;
        var capacity = configured <= 0 ? 1 : configured;
        return OpportunityGambitReservationCount(gambitSlot, targetId, tick) >= capacity;
    }

    private int OpportunityGambitReservationCount(int gambitSlot, int targetId, int tick)
    {
        if (_opportunityReservationTicks.GetOrDefault(targetId, gambitSlot) != tick)
        {
            return 0;
        }

        return _opportunityReservationCounts.GetOrDefault(targetId, gambitSlot);
    }

    private void ReserveOpportunityGambitTarget(int targetId, int gambitSlot, int tick)
    {
        if (targetId <= 0 || gambitSlot <= 0)
        {
            return;
        }

        var current = OpportunityGambitReservationCount(gambitSlot, targetId, tick);
        _opportunityReservationTicks.Set(targetId, gambitSlot, tick);
        _opportunityReservationCounts.Set(targetId, gambitSlot, current + 1);
    }

    private bool GambitIsPositioningOnly(int gambitSlot)
    {
        var first = _gambitStepStarts[gambitSlot];
        var end = first + _gambitStepCounts[gambitSlot];
        if (end <= first)
        {
            return false;
        }

        for (var step = first; step < end; step++)
        {
            if (!GambitOperationIsPositioning(_stepOperationCodes[step]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool GambitOperationIsPositioning(int operationCode)
    {
        return operationCode is
            ProcessOperationCodes.MoveTowardTargetPhysical or
            ProcessOperationCodes.WanderPhysical or
            ProcessOperationCodes.MoveTowardTargetSemantic or
            ProcessOperationCodes.WanderSemantic;
    }

    private void EmitOpportunityGambitTraceIfNeeded(
        Simulation simulation,
        int tick,
        int entityId,
        int candidateIndex,
        int gambitSlot,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int targetId,
        int statusCode,
        float urgency,
        float reward,
        float weight,
        bool traceEnabled)
    {
        if (!traceEnabled)
        {
            return;
        }

        EmitOpportunityGambitTrace(simulation, tick, entityId, candidateIndex, gambitSlot, effectScopeCode, effectValueId, effectKindId, targetId, statusCode, urgency, reward, weight);
    }

    private void EmitOpportunityGambitTrace(
        Simulation simulation,
        int tick,
        int entityId,
        int candidateIndex,
        int gambitSlot,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int targetId,
        int statusCode,
        float urgency,
        float reward,
        float weight)
    {
        simulation.EmitTrace(
            tick,
            ActionFamilyCodes.Gambit,
            entityId,
            TraceSubjectCodes.ActionSummary,
            candidateIndex,
            0,
            ActionFamilyCodes.Gambit,
            _gambitIds[gambitSlot],
            effectScopeCode,
            effectValueId,
            effectKindId,
            targetId,
            statusCode,
            urgency,
            reward,
            weight,
            weight);
    }
}
