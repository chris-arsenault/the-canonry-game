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
        var traceEnabled = simulation.TraceEnabled();
        var planCount = BuildOpportunityProcessPlans(
            simulation,
            request.ActorEntityId,
            request.ActorKindId,
            request.Tick,
            traceEnabled,
            ref candidateCount);
        if (candidateCount == 0)
        {
            return simulation.Planners.EmptyDecision(
                PlanningStatusCodes.NoCandidates,
                request.ActionFamilyCode,
                candidateCount,
                0f);
        }

        if (planCount == 0)
        {
            return simulation.Planners.EmptyDecision(
                PlanningStatusCodes.NoWeight,
                request.ActionFamilyCode,
                candidateCount,
                0f);
        }

        ApplyLocalityBundling(simulation, request.ActorEntityId, request.Tick, planCount, traceEnabled);
        var totalWeight = ProcessPlanAdjustedWeightTotal(planCount);
        if (totalWeight <= 0f)
        {
            return simulation.Planners.EmptyDecision(
                PlanningStatusCodes.NoWeight,
                request.ActionFamilyCode,
                candidateCount,
                totalWeight);
        }

        return WriteSelectedOpportunityProcessDecision(simulation, request, planCount, candidateCount, totalWeight);
    }

    private PlanningDecision WriteSelectedOpportunityProcessDecision(
        Simulation simulation,
        in PlanningRequest request,
        int planCount,
        int candidateCount,
        float totalWeight)
    {
        SelectOpportunityProcessPlan(
            simulation,
            request,
            planCount,
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

        ReserveProcessActionTarget(selectedRootTargetId, selectedRootProcessSlot, request.Tick);
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

    private int BuildOpportunityProcessPlans(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        bool traceEnabled,
        ref int candidateCount)
    {
        var planCount = 0;
        for (var processSlot = 1; processSlot <= _processCount; processSlot++)
        {
            if (!ProcessCanRunForEntity(processSlot, kindId, entityId, tick) ||
                ProcessIsPositioningOnly(processSlot))
            {
                continue;
            }

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

                if (planCount >= _processPlanWeights.Length - 1)
                {
                    EmitActionSummaryIfNeeded(
                        simulation,
                        tick,
                        entityId,
                        candidate.CandidateIndex,
                        processSlot,
                        candidate.EffectScopeCode,
                        candidate.EffectValueId,
                        candidate.EffectKindId,
                        targetId,
                        TraceStatusCodes.CapacityReached,
                        0f,
                        candidate.Weight,
                        0f,
                        traceEnabled);
                    continue;
                }

                planCount++;
                StoreOpportunityProcessPlan(simulation, planCount, processSlot, targetId, candidate);
            }
        }

        return planCount;
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

    private void StoreOpportunityProcessPlan(
        Simulation simulation,
        int planCount,
        int processSlot,
        int targetId,
        OpportunityProcessCandidate candidate)
    {
        _processPlanEffectKeys[planCount] = candidate.EffectKeyValue;
        _processPlanEffectScopeCodes[planCount] = candidate.EffectScopeCode;
        _processPlanEffectValueIds[planCount] = candidate.EffectValueId;
        _processPlanEffectKindIds[planCount] = candidate.EffectKindId;
        _processPlanGoalCandidateIndexes[planCount] = candidate.CandidateIndex;
        _processPlanRootProcessSlots[planCount] = processSlot;
        _processPlanRootTargetIds[planCount] = targetId;
        _processPlanProcessSlots[planCount] = candidate.LeafProcessSlot;
        _processPlanTargetIds[planCount] = candidate.LeafTargetId;
        _processPlanLocalityKeys[planCount] = LocalityKey(simulation.World, targetId);
        _processPlanWeights[planCount] = candidate.Weight;
        _processPlanAdjustedWeights[planCount] = candidate.Weight;
    }

    private void SelectOpportunityProcessPlan(
        Simulation simulation,
        in PlanningRequest request,
        int planCount,
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
        var selectRoll = ClampSelectionRoll(request.SelectionRoll) * total;
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
            EmitActionSummaryTrace(
                simulation,
                request.Tick,
                request.ActorEntityId,
                _processPlanGoalCandidateIndexes[plan],
                0,
                _processPlanRootProcessSlots[plan],
                _processPlanEffectScopeCodes[plan],
                _processPlanEffectValueIds[plan],
                _processPlanEffectKindIds[plan],
                _processPlanRootTargetIds[plan],
                TraceStatusCodes.Selected,
                0f,
                _processPlanWeights[plan],
                _processPlanAdjustedWeights[plan]);
            _committedPlanTargets.Set(request.ActorEntityId, _processPlanEffectKeys[plan], _processPlanRootTargetIds[plan]);
            _committedPlanProcessSlots.Set(request.ActorEntityId, _processPlanEffectKeys[plan], _processPlanRootProcessSlots[plan]);
            _committedPlanLocalities.Set(request.ActorEntityId, ProcessLocalityCommitmentKey, _processPlanLocalityKeys[plan]);
            return;
        }
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

        if (ProcessActionTargetAtCapacity(processSlot, targetId, tick, OpportunityTargetCapacity()))
        {
            EmitActionSummaryIfNeeded(simulation, tick, entityId, candidateIndex, processSlot, effectScopeCode, effectValueId, effectKindId, targetId, TraceStatusCodes.CapacityReached, 0f, reward, 0f, traceEnabled);
            return 0f;
        }

        var urgency = OpportunityProcessUrgency(simulation.World, entityId, processSlot, targetId);
        if (urgency <= 0f)
        {
            EmitActionSummaryIfNeeded(simulation, tick, entityId, candidateIndex, processSlot, effectScopeCode, effectValueId, effectKindId, targetId, TraceStatusCodes.BelowUrgency, 0f, reward, 0f, traceEnabled);
            return 0f;
        }

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

    private bool GoapProcessTargetReservationsEnabled()
    {
        return Tuning.Planning.GoapTargetReservationsEnabled;
    }

    private bool ProcessActionTargetAtCapacity(int processSlot, int targetId, int tick, int capacity)
    {
        return ProcessActionTargetReservationCount(processSlot, targetId, tick) >= capacity;
    }

    private int ProcessActionTargetReservationCount(int processSlot, int targetId, int tick)
    {
        if (_actionTargetReservationTicks.GetOrDefault(targetId, processSlot) != tick)
        {
            return 0;
        }

        return _actionTargetReservationCounts.GetOrDefault(targetId, processSlot);
    }

    private void ReserveProcessActionTarget(int targetId, int processSlot, int tick)
    {
        if (targetId <= 0 || processSlot <= 0)
        {
            return;
        }

        var current = ProcessActionTargetReservationCount(processSlot, targetId, tick);
        _actionTargetReservationTicks.Set(targetId, processSlot, tick);
        _actionTargetReservationCounts.Set(targetId, processSlot, current + 1);
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
