namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class ProcessProgramRuntime
{
    private const float OpportunityBaseFit = 0.25f;
    private const float OpportunityNoObjectiveFit = 0.5f;
    private const float OpportunityPhysicalDistancePenalty = 0.05f;
    private const float OpportunitySemanticDistancePenalty = 0.03f;

    private struct OpportunityProcessCandidate
    {
        public float Weight;
        public int CandidateIndex;
        public int LeafProcessSlot;
        public int LeafTargetId;
        public int EffectScopeCode;
        public int EffectValueId;
        public int EffectKindId;
        public int EffectKeyValue;
    }

    internal PlanningDecision PlanOpportunity(Simulation simulation, in PlanningRequest request)
    {
        var candidateCount = 0;
        var totalWeight = EvaluateOpportunityProcessPlanTotal(simulation, request, ref candidateCount);
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

        return SelectOpportunityProcessPlan(simulation, request, totalWeight, candidateCount);
    }

    private float EvaluateOpportunityProcessPlanTotal(Simulation simulation, in PlanningRequest request, ref int candidateCount)
    {
        return EvaluateOpportunityProcessPlans(
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

    private PlanningDecision SelectOpportunityProcessPlan(
        Simulation simulation,
        in PlanningRequest request,
        float totalWeight,
        int candidateCount)
    {
        var selectedCandidateCount = 0;
        _ = EvaluateOpportunityProcessPlans(
            simulation,
            request.ActorEntityId,
            request.ActorKindId,
            request.Tick,
            ClampSelectionRoll(request.SelectionRoll) * totalWeight,
            ref selectedCandidateCount,
            out var selectedProcessSlot,
            out var selectedTargetId,
            out var selectedRootProcessSlot,
            out var selectedRootTargetId,
            out var selectedEffectKey,
            out var selectedLocalityKey);
        if (selectedProcessSlot == 0 || selectedTargetId == 0)
        {
            return simulation.Planners.EmptyDecision(
                PlanningStatusCodes.NoWeight,
                request.ActionFamilyCode,
                candidateCount,
                totalWeight);
        }

        ReserveOpportunityProcessTarget(selectedRootTargetId, selectedRootProcessSlot, request.Tick);
        return simulation.Planners.WriteSelectedPlan(
            request,
            selectedEffectKey,
            selectedRootProcessSlot,
            selectedRootTargetId,
            selectedProcessSlot,
            selectedTargetId,
            selectedLocalityKey,
            candidateCount,
            totalWeight);
    }

    private float EvaluateOpportunityProcessPlans(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        float selectRoll,
        ref int candidateCount,
        out int selectedProcessSlot,
        out int selectedTargetId,
        out int selectedRootProcessSlot,
        out int selectedRootTargetId,
        out int selectedEffectKey,
        out int selectedLocalityKey)
    {
        selectedProcessSlot = 0;
        selectedTargetId = 0;
        selectedRootProcessSlot = 0;
        selectedRootTargetId = 0;
        selectedEffectKey = 0;
        selectedLocalityKey = 0;
        var total = 0f;
        var traceEnabled = selectRoll < 0f;
        for (var processSlot = 1; processSlot <= _processCount; processSlot++)
        {
            if (!ProcessCanRunForEntity(processSlot, kindId, entityId, tick) ||
                ProcessIsPositioningOnly(processSlot))
            {
                continue;
            }

            total = EvaluateOpportunityProcessTargets(
                simulation,
                entityId,
                processSlot,
                tick,
                selectRoll,
                traceEnabled,
                total,
                ref candidateCount,
                ref selectedProcessSlot,
                ref selectedTargetId,
                ref selectedRootProcessSlot,
                ref selectedRootTargetId,
                ref selectedEffectKey,
                ref selectedLocalityKey);
            if (selectedProcessSlot > 0)
            {
                return total;
            }
        }

        return total;
    }

    private float EvaluateOpportunityProcessTargets(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        float selectRoll,
        bool traceEnabled,
        float total,
        ref int candidateCount,
        ref int selectedProcessSlot,
        ref int selectedTargetId,
        ref int selectedRootProcessSlot,
        ref int selectedRootTargetId,
        ref int selectedEffectKey,
        ref int selectedLocalityKey)
    {
        var world = simulation.World;
        for (var targetId = 1; targetId <= world.EntityHighWatermark; targetId++)
        {
            var candidate = EvaluateOpportunityProcessTarget(
                simulation,
                entityId,
                processSlot,
                targetId,
                tick,
                traceEnabled,
                world,
                ref candidateCount);
            if (candidate.Weight <= 0f)
            {
                continue;
            }

            total += candidate.Weight;
            if (selectRoll < 0f || selectRoll > total)
            {
                continue;
            }

            SelectOpportunityProcessCandidate(
                simulation,
                world,
                tick,
                entityId,
                candidate,
                processSlot,
                targetId,
                ref selectedProcessSlot,
                ref selectedTargetId,
                ref selectedRootProcessSlot,
                ref selectedRootTargetId,
                ref selectedEffectKey,
                ref selectedLocalityKey);
            return total;
        }

        return total;
    }

    private OpportunityProcessCandidate EvaluateOpportunityProcessTarget(
        Simulation simulation,
        int entityId,
        int processSlot,
        int targetId,
        int tick,
        bool traceEnabled,
        WorldState world,
        ref int candidateCount)
    {
        if (!TargetEligibleForProcess(world, entityId, processSlot, forcedTargetId: 0, targetId))
        {
            return default;
        }

        candidateCount++;
        var candidateIndex = candidateCount;
        var weight = EvaluateOpportunityProcessCandidate(
            simulation,
            entityId,
            processSlot,
            targetId,
            tick,
            candidateIndex,
            traceEnabled,
            ref candidateCount,
            out var leafProcessSlot,
            out var leafTargetId,
            out var effectScopeCode,
            out var effectValueId,
            out var effectKindId,
            out var effectKey);
        return new OpportunityProcessCandidate
        {
            Weight = weight,
            CandidateIndex = candidateIndex,
            LeafProcessSlot = leafProcessSlot,
            LeafTargetId = leafTargetId,
            EffectScopeCode = effectScopeCode,
            EffectValueId = effectValueId,
            EffectKindId = effectKindId,
            EffectKeyValue = effectKey,
        };
    }

    private void SelectOpportunityProcessCandidate(
        Simulation simulation,
        WorldState world,
        int tick,
        int entityId,
        OpportunityProcessCandidate candidate,
        int processSlot,
        int targetId,
        ref int selectedProcessSlot,
        ref int selectedTargetId,
        ref int selectedRootProcessSlot,
        ref int selectedRootTargetId,
        ref int selectedEffectKey,
        ref int selectedLocalityKey)
    {
        selectedProcessSlot = candidate.LeafProcessSlot;
        selectedTargetId = candidate.LeafTargetId;
        selectedRootProcessSlot = processSlot;
        selectedRootTargetId = targetId;
        selectedEffectKey = candidate.EffectKeyValue;
        selectedLocalityKey = LocalityKey(world, targetId);
        EmitActionSummaryTrace(
            simulation,
            tick,
            entityId,
            candidate.CandidateIndex,
            0,
            processSlot,
            candidate.EffectScopeCode,
            candidate.EffectValueId,
            candidate.EffectKindId,
            targetId,
            TraceStatusCodes.Selected,
            0f,
            candidate.Weight,
            candidate.Weight);
    }

    private float EvaluateOpportunityProcessCandidate(
        Simulation simulation,
        int entityId,
        int processSlot,
        int targetId,
        int tick,
        int candidateIndex,
        bool traceEnabled,
        ref int candidateCount,
        out int selectedProcessSlot,
        out int selectedTargetId,
        out int effectScopeCode,
        out int effectValueId,
        out int effectKindId,
        out int effectKey)
    {
        OpportunityProcessEffect(processSlot, out effectScopeCode, out effectValueId, out effectKindId);
        effectKey = EffectKey(effectScopeCode, effectValueId, effectKindId);
        selectedProcessSlot = 0;
        selectedTargetId = 0;
        var reward = AdjustedProcessPlanReward(simulation.World, processSlot, entityId, targetId);
        if (reward <= 0f)
        {
            EmitActionSummaryIfNeeded(simulation, tick, entityId, candidateIndex, processSlot, effectScopeCode, effectValueId, effectKindId, targetId, TraceStatusCodes.NoReward, 0f, 0f, 0f, traceEnabled);
            return 0f;
        }

        if (OpportunityProcessTargetAtCapacity(processSlot, targetId, tick))
        {
            EmitActionSummaryIfNeeded(simulation, tick, entityId, candidateIndex, processSlot, effectScopeCode, effectValueId, effectKindId, targetId, TraceStatusCodes.CapacityReached, 0f, reward, 0f, traceEnabled);
            return 0f;
        }

        var urgency = OpportunityProcessUrgency(simulation.World, entityId, processSlot, targetId);
        var weight = EvaluatePlanCandidateWithReward(
            simulation,
            entityId,
            targetId,
            processSlot,
            tick,
            effectScopeCode,
            effectValueId,
            effectKindId,
            depth: 0,
            candidateIndex,
            parentCandidateIndex: 0,
            traceEnabled,
            reward,
            urgency,
            ref candidateCount,
            out selectedProcessSlot,
            out selectedTargetId);
        EmitActionSummaryIfNeeded(
            simulation,
            tick,
            entityId,
            candidateIndex,
            processSlot,
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

    private float OpportunityProcessUrgency(WorldState world, int entityId, int processSlot, int targetId)
    {
        var fit = OpportunityNoObjectiveFit;
        var matchedObjective = false;
        var first = _processGoalEffectStarts[processSlot];
        var end = first + _processGoalEffectCounts[processSlot];
        for (var effect = first; effect < end; effect++)
        {
            var objectiveUrgency = BestObjectiveUrgencyForProcessEffect(
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

        if (!matchedObjective && _processGoalEffectCounts[processSlot] == 0)
        {
            fit = OpportunityBaseFit;
        }

        var physicalDistance = PlaneDistance(world, entityId, targetId, PlaneCodes.Physical);
        var semanticDistance = PlaneDistance(world, entityId, targetId, PlaneCodes.Semantic);
        return fit / (1f +
            (physicalDistance * OpportunityPhysicalDistancePenalty) +
            (semanticDistance * OpportunitySemanticDistancePenalty));
    }

    private float BestObjectiveUrgencyForProcessEffect(
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

    private void OpportunityProcessEffect(int processSlot, out int effectScopeCode, out int effectValueId, out int effectKindId)
    {
        var first = _processGoalEffectStarts[processSlot];
        if (_processGoalEffectCounts[processSlot] > 0)
        {
            effectScopeCode = _goalEffectScopeCodes[first];
            effectValueId = _goalEffectValueIds[first];
            effectKindId = _goalEffectKindIds[first];
            return;
        }

        effectScopeCode = ProcessEffectScopeCodes.Structural;
        effectValueId = _processIds[processSlot];
        effectKindId = _processKindIds[processSlot];
    }

    private bool OpportunityProcessTargetAtCapacity(int processSlot, int targetId, int tick)
    {
        var capacity = OpportunityTargetCapacity();
        return OpportunityProcessReservationCount(processSlot, targetId, tick) >= capacity;
    }

    private int OpportunityProcessReservationCount(int processSlot, int targetId, int tick)
    {
        if (_opportunityReservationTicks.GetOrDefault(targetId, processSlot) != tick)
        {
            return 0;
        }

        return _opportunityReservationCounts.GetOrDefault(targetId, processSlot);
    }

    private void ReserveOpportunityProcessTarget(int targetId, int processSlot, int tick)
    {
        if (targetId <= 0 || processSlot <= 0)
        {
            return;
        }

        var current = OpportunityProcessReservationCount(processSlot, targetId, tick);
        _opportunityReservationTicks.Set(targetId, processSlot, tick);
        _opportunityReservationCounts.Set(targetId, processSlot, current + 1);
    }

    private int OpportunityTargetCapacity()
    {
        var configured = Tuning.Planning.OpportunityTargetCapacity;
        return configured <= 0 ? 1 : configured;
    }

    private bool ProcessIsPositioningOnly(int processSlot)
    {
        var first = _processStepStarts[processSlot];
        var end = first + _processStepCounts[processSlot];
        if (end <= first)
        {
            return false;
        }

        for (var step = first; step < end; step++)
        {
            if (!ProcessOperationIsPositioning(_stepOperationCodes[step]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ProcessOperationIsPositioning(int operationCode)
    {
        return operationCode is
            ProcessOperationCodes.MoveTowardTargetPhysical or
            ProcessOperationCodes.WanderPhysical or
            ProcessOperationCodes.MoveTowardTargetSemantic or
            ProcessOperationCodes.WanderSemantic;
    }

    private void EmitActionSummaryIfNeeded(
        Simulation simulation,
        int tick,
        int entityId,
        int candidateIndex,
        int processSlot,
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

        EmitActionSummaryTrace(
            simulation,
            tick,
            entityId,
            candidateIndex,
            0,
            processSlot,
            effectScopeCode,
            effectValueId,
            effectKindId,
            targetId,
            statusCode,
            urgency,
            reward,
            weight);
    }
}
