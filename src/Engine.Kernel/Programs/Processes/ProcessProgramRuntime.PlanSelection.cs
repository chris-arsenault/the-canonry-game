namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class ProcessProgramRuntime
{
    internal PlanningDecision PlanGoap(Simulation simulation, in PlanningRequest request)
    {
        var candidateCount = 0;
        var totalWeight = EvaluateProcessPlanTotal(simulation, request, ref candidateCount);
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

        SelectProcessPlan(
            simulation,
            request,
            totalWeight,
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

    private float EvaluateProcessPlanTotal(Simulation simulation, in PlanningRequest request, ref int candidateCount)
    {
        return EvaluatePlannedCandidates(
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

    private void SelectProcessPlan(
        Simulation simulation,
        in PlanningRequest request,
        float totalWeight,
        out int selectedProcessSlot,
        out int selectedTargetId,
        out int selectedRootProcessSlot,
        out int selectedRootTargetId,
        out int selectedEffectKey,
        out int selectedLocalityKey)
    {
        var selectedCandidateCount = 0;
        _ = EvaluatePlannedCandidates(
            simulation,
            request.ActorEntityId,
            request.ActorKindId,
            request.Tick,
            ClampSelectionRoll(request.SelectionRoll) * totalWeight,
            ref selectedCandidateCount,
            out selectedProcessSlot,
            out selectedTargetId,
            out selectedRootProcessSlot,
            out selectedRootTargetId,
            out selectedEffectKey,
            out selectedLocalityKey);
    }

    internal int ProcessIdForSlot(int processSlot)
    {
        EnsureProcess(processSlot);
        return _processIds[processSlot];
    }

    private float EvaluatePlannedCandidates(
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
        var planCount = BuildProcessPlans(simulation, entityId, kindId, tick, ref candidateCount);
        if (planCount == 0)
        {
            return 0f;
        }

        ApplyLocalityBundling(simulation.World, entityId, planCount);
        var total = ProcessPlanAdjustedWeightTotal(planCount);
        return selectRoll < 0f
            ? total
            : SelectProcessPlanCandidate(entityId, planCount, selectRoll, total, out selectedProcessSlot, out selectedTargetId, out selectedRootProcessSlot, out selectedRootTargetId, out selectedEffectKey, out selectedLocalityKey);
    }

    private int BuildProcessPlans(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        ref int candidateCount)
    {
        var planCount = 0;
        for (var objective = 1; objective <= _objectiveCount; objective++)
        {
            TryAddObjectiveProcessPlan(simulation, entityId, kindId, tick, objective, ref candidateCount, ref planCount);
        }

        return planCount;
    }

    private void TryAddObjectiveProcessPlan(
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
            out var rootProcessSlot,
            out var rootTargetId,
            out var processSlot,
            out var targetId);
        if (weight <= 0f || rootProcessSlot == 0 || rootTargetId == 0 || processSlot == 0 || targetId == 0)
        {
            return;
        }

        planCount++;
        _processPlanEffectKeys[planCount] = EffectKey(
            _objectiveEffectScopeCodes[objective],
            _objectiveEffectValueIds[objective],
            _objectiveEffectKindIds[objective]);
        _processPlanRootProcessSlots[planCount] = rootProcessSlot;
        _processPlanRootTargetIds[planCount] = rootTargetId;
        _processPlanProcessSlots[planCount] = processSlot;
        _processPlanTargetIds[planCount] = targetId;
        _processPlanLocalityKeys[planCount] = LocalityKey(simulation.World, rootTargetId);
        _processPlanWeights[planCount] = weight;
    }

    private float ProcessPlanAdjustedWeightTotal(int planCount)
    {
        var total = 0f;
        for (var plan = 1; plan <= planCount; plan++)
        {
            total += _processPlanAdjustedWeights[plan];
        }

        return total;
    }

    private float SelectProcessPlanCandidate(
        int entityId,
        int planCount,
        float selectRoll,
        float total,
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
        var cumulative = 0f;
        for (var plan = 1; plan <= planCount; plan++)
        {
            cumulative += _processPlanAdjustedWeights[plan];
            if (selectRoll > cumulative)
            {
                continue;
            }

            selectedProcessSlot = _processPlanProcessSlots[plan];
            selectedTargetId = _processPlanTargetIds[plan];
            selectedRootProcessSlot = _processPlanRootProcessSlots[plan];
            selectedRootTargetId = _processPlanRootTargetIds[plan];
            selectedEffectKey = _processPlanEffectKeys[plan];
            selectedLocalityKey = _processPlanLocalityKeys[plan];
            _committedPlanTargets.Set(entityId, _processPlanEffectKeys[plan], _processPlanRootTargetIds[plan]);
            _committedPlanProcessSlots.Set(entityId, _processPlanEffectKeys[plan], _processPlanRootProcessSlots[plan]);
            _committedPlanLocalities.Set(entityId, ProcessLocalityCommitmentKey, _processPlanLocalityKeys[plan]);
            return total;
        }

        return total;
    }

}
