namespace Engine.Kernel;

internal sealed class PlanningProgramRuntime : IKernelProgramRuntime<PlanningProgramData, PlanningProgramState>
{
    private readonly PlanningProgramScratch _scratch;

    internal PlanningProgramRuntime(
        PlanningProgramData data,
        PlanningProgramState state,
        PlanningProgramScratch scratch)
        : this(data, state, scratch, new KernelProgramTuningOptions())
    {
    }

    internal PlanningProgramRuntime(
        PlanningProgramData data,
        PlanningProgramState state,
        PlanningProgramScratch scratch,
        KernelProgramTuningOptions tuning)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(scratch);
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        Data = data;
        State = state;
        _scratch = scratch;
        Tuning = tuning;
        ApplyConfiguredOverrides(tuning.Planning);
    }

    public PlanningProgramData Data { get; }

    public PlanningProgramState State { get; }

    public KernelProgramTuningOptions Tuning { get; }

    public int PlannerKindCode => Data.DefaultPlannerKindCode;

    public PlanningPlanHandle LastPlan { get; private set; }

    public void SetPlannerKind(int plannerKindCode)
    {
        PlanningProgramData.ValidatePlannerKind(plannerKindCode);
        Data.DefaultPlannerKindCode = plannerKindCode;
    }

    public void SetKindPlannerKind(int kindId, int plannerKindCode)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kindId);
        PlanningProgramData.ValidatePlannerKind(plannerKindCode);
        State.KindPlannerKindCodes.Set(kindId, State.PlannerOverrideSecondKey, plannerKindCode);
    }

    public void SetEntityPlannerKind(int entityId, int plannerKindCode)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityId);
        PlanningProgramData.ValidatePlannerKind(plannerKindCode);
        State.EntityPlannerKindCodes.Set(entityId, State.PlannerOverrideSecondKey, plannerKindCode);
    }

    public int PlannerKindFor(int actorKindId, int actorEntityId)
    {
        if (actorEntityId > 0 &&
            State.EntityPlannerKindCodes.TryGet(actorEntityId, State.PlannerOverrideSecondKey, out var entityPlannerKindCode))
        {
            return entityPlannerKindCode;
        }

        if (actorKindId > 0 &&
            State.KindPlannerKindCodes.TryGet(actorKindId, State.PlannerOverrideSecondKey, out var kindPlannerKindCode))
        {
            return kindPlannerKindCode;
        }

        return Data.DefaultPlannerKindCode;
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
    }

    public PlanningDecision Plan(Simulation simulation, in PlanningRequest request)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var plannerKindCode = request.PlannerKindCode == 0 ? Data.DefaultPlannerKindCode : request.PlannerKindCode;
        var plannedRequest = new PlanningRequest(
            plannerKindCode,
            request.ActionFamilyCode,
            request.ActorEntityId,
            request.ActorKindId,
            request.Tick,
            request.SelectionRoll);
        PlanningDecision decision;
        decision = plannerKindCode switch
        {
            PlannerKindCodes.Goap => PlanGoap(simulation, plannedRequest),
            PlannerKindCodes.Opportunity => PlanOpportunity(simulation, plannedRequest),
            _ => EmptyDecision(PlanningStatusCodes.UnsupportedPlanner, request.ActionFamilyCode, 0, 0f),
        };
        EmitPlanEvents(simulation, plannedRequest, decision);
        return decision;
    }

    private PlanningDecision PlanGoap(Simulation simulation, in PlanningRequest request)
    {
        return request.ActionFamilyCode switch
        {
            ActionFamilyCodes.Gambit => simulation.Gambits.PlanGoap(simulation, request),
            _ => EmptyDecision(PlanningStatusCodes.UnsupportedActionFamily, request.ActionFamilyCode, 0, 0f),
        };
    }

    private PlanningDecision PlanOpportunity(Simulation simulation, in PlanningRequest request)
    {
        return request.ActionFamilyCode switch
        {
            ActionFamilyCodes.Gambit => simulation.Gambits.PlanOpportunity(simulation, request),
            _ => EmptyDecision(PlanningStatusCodes.UnsupportedActionFamily, request.ActionFamilyCode, 0, 0f),
        };
    }

    public PlanningPlanHeader PlanHeader(PlanningPlanHandle handle)
    {
        EnsurePlan(handle);
        var slot = handle.Slot;
        return new PlanningPlanHeader(
            _scratch.HeaderPlannerKindCodes[slot],
            _scratch.HeaderActionFamilyCodes[slot],
            _scratch.HeaderActorEntityIds[slot],
            _scratch.HeaderActorKindIds[slot],
            _scratch.HeaderObjectiveEffectKeys[slot],
            _scratch.HeaderStepStarts[slot],
            _scratch.HeaderStepCounts[slot],
            _scratch.HeaderImmediateStepIndexes[slot],
            _scratch.HeaderRootStepIndexes[slot],
            _scratch.HeaderRootTargetEntityIds[slot],
            _scratch.HeaderLocalityKeys[slot],
            _scratch.HeaderCandidateCounts[slot],
            _scratch.HeaderScores[slot],
            _scratch.HeaderStatusCodes[slot]);
    }

    public PlanningPlanStep PlanStep(PlanningPlanHandle handle, int stepIndex)
    {
        var header = PlanHeader(handle);
        if ((uint)stepIndex >= (uint)header.StepCount)
        {
            throw new global::System.InvalidOperationException();
        }

        var slot = header.StepStart + stepIndex;
        return new PlanningPlanStep(
            new PlanningActionHandle(_scratch.StepActionFamilyCodes[slot], _scratch.StepActionSlots[slot]),
            _scratch.StepTargetEntityIds[slot],
            _scratch.StepSatisfiesEffectKeys[slot],
            _scratch.StepParentStepIndexes[slot],
            _scratch.StepRoleCodes[slot],
            _scratch.StepScores[slot]);
    }

    internal PlanningDecision EmptyDecision(
        int statusCode,
        int actionFamilyCode,
        int candidateCount,
        float score)
    {
        return new PlanningDecision(
            statusCode,
            default,
            new PlanningActionHandle(actionFamilyCode, 0),
            0,
            candidateCount,
            score);
    }

    internal PlanningDecision WriteSelectedPlan(
        in PlanningRequest request,
        int objectiveEffectKey,
        int rootActionSlot,
        int rootTargetEntityId,
        int immediateActionSlot,
        int immediateTargetEntityId,
        int localityKey,
        int candidateCount,
        float score)
    {
        _scratch.Reset();
        var headerSlot = AllocateHeader();
        var stepStart = _scratch.StepCount + 1;
        var rootStepIndex = 0;
        AddStep(
            request.ActionFamilyCode,
            immediateActionSlot,
            immediateTargetEntityId,
            0,
            rootActionSlot == immediateActionSlot && rootTargetEntityId == immediateTargetEntityId ? -1 : 1,
            PlanningStepRoleCodes.Immediate,
            score);
        if (rootActionSlot != immediateActionSlot || rootTargetEntityId != immediateTargetEntityId)
        {
            rootStepIndex = 1;
            AddStep(
                request.ActionFamilyCode,
                rootActionSlot,
                rootTargetEntityId,
                objectiveEffectKey,
                -1,
                PlanningStepRoleCodes.Root,
                score);
        }

        var stepCount = _scratch.StepCount - stepStart + 1;
        _scratch.HeaderPlannerKindCodes[headerSlot] = request.PlannerKindCode;
        _scratch.HeaderActionFamilyCodes[headerSlot] = request.ActionFamilyCode;
        _scratch.HeaderActorEntityIds[headerSlot] = request.ActorEntityId;
        _scratch.HeaderActorKindIds[headerSlot] = request.ActorKindId;
        _scratch.HeaderObjectiveEffectKeys[headerSlot] = objectiveEffectKey;
        _scratch.HeaderStepStarts[headerSlot] = stepStart;
        _scratch.HeaderStepCounts[headerSlot] = stepCount;
        _scratch.HeaderImmediateStepIndexes[headerSlot] = 0;
        _scratch.HeaderRootStepIndexes[headerSlot] = rootStepIndex;
        _scratch.HeaderRootTargetEntityIds[headerSlot] = rootTargetEntityId;
        _scratch.HeaderLocalityKeys[headerSlot] = localityKey;
        _scratch.HeaderCandidateCounts[headerSlot] = candidateCount;
        _scratch.HeaderScores[headerSlot] = score;
        _scratch.HeaderStatusCodes[headerSlot] = PlanningStatusCodes.Succeeded;
        LastPlan = new PlanningPlanHandle(headerSlot);
        return new PlanningDecision(
            PlanningStatusCodes.Succeeded,
            LastPlan,
            new PlanningActionHandle(request.ActionFamilyCode, immediateActionSlot),
            immediateTargetEntityId,
            candidateCount,
            score);
    }

    private int AllocateHeader()
    {
        if (_scratch.HeaderCount + 1 >= _scratch.HeaderPlannerKindCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _scratch.HeaderCount++;
        return _scratch.HeaderCount;
    }

    private void AddStep(
        int actionFamilyCode,
        int actionSlot,
        int targetEntityId,
        int satisfiesEffectKey,
        int parentStepIndex,
        int stepRoleCode,
        float score)
    {
        if (_scratch.StepCount + 1 >= _scratch.StepActionFamilyCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _scratch.StepCount++;
        var slot = _scratch.StepCount;
        _scratch.StepActionFamilyCodes[slot] = actionFamilyCode;
        _scratch.StepActionSlots[slot] = actionSlot;
        _scratch.StepTargetEntityIds[slot] = targetEntityId;
        _scratch.StepSatisfiesEffectKeys[slot] = satisfiesEffectKey;
        _scratch.StepParentStepIndexes[slot] = parentStepIndex;
        _scratch.StepRoleCodes[slot] = stepRoleCode;
        _scratch.StepScores[slot] = score;
    }

    private void EmitPlanEvents(Simulation simulation, in PlanningRequest request, in PlanningDecision decision)
    {
        var planId = PlanId(request.ActionFamilyCode);
        simulation.EmitPlanStarted(
            request.Tick,
            planId,
            request.ActorEntityId,
            decision.StatusCode,
            decision.CandidateCount);
        if (decision.StatusCode != PlanningStatusCodes.Succeeded || decision.Plan.Slot == 0)
        {
            simulation.EmitPlanAbandoned(
                request.Tick,
                planId,
                request.ActorEntityId,
                decision.StatusCode);
            return;
        }

        var header = PlanHeader(decision.Plan);
        var goalId = header.ObjectiveEffectKey;
        EmitGoalEvents(simulation, request, planId, goalId, header.Score);
        EmitPlanStepEvents(simulation, request, decision, planId, goalId, header.StepCount);
        simulation.EmitPlanCommitted(
            request.Tick,
            planId,
            request.ActorEntityId,
            header.StepCount,
            PlanningStatusCodes.Succeeded,
            header.Score);
    }

    private static void EmitGoalEvents(
        Simulation simulation,
        in PlanningRequest request,
        int planId,
        int goalId,
        float score)
    {
        simulation.EmitGoalConsidered(
            request.Tick,
            planId,
            request.ActorEntityId,
            goalId,
            candidateIndex: 0,
            preliminaryScore: score);
        simulation.EmitGoalScored(
            request.Tick,
            planId,
            request.ActorEntityId,
            goalId,
            PlanningStatusCodes.Succeeded,
            scoringPassIndex: 0,
            scoreDelta: score,
            runningTotal: score);
        simulation.EmitGoalSelected(
            request.Tick,
            planId,
            request.ActorEntityId,
            goalId,
            score);
    }

    private void EmitPlanStepEvents(
        Simulation simulation,
        in PlanningRequest request,
        in PlanningDecision decision,
        int planId,
        int goalId,
        int stepCount)
    {
        for (var stepIndex = 0; stepIndex < stepCount; stepIndex++)
        {
            var step = PlanStep(decision.Plan, stepIndex);
            simulation.EmitPlanStepEmitted(
                request.Tick,
                planId,
                request.ActorEntityId,
                goalId,
                stepIndex,
                ActionId(simulation, step.Action),
                step.TargetEntityId,
                relationshipKindId: 0);
        }
    }

    private static int PlanId(int actionFamilyCode)
    {
        return actionFamilyCode;
    }

    private static int ActionId(Simulation simulation, PlanningActionHandle action)
    {
        return action.ActionFamilyCode switch
        {
            ActionFamilyCodes.Gambit => simulation.Gambits.GambitIdForSlot(action.ActionSlot),
            _ => action.ActionSlot,
        };
    }

    private void ApplyConfiguredOverrides(PlanningProgramTuningOptions planning)
    {
        SetPlannerKind(planning.DefaultPlannerKindCode);
        for (var i = 0; i < planning.KindPlannerOverrides.Length; i++)
        {
            var plannerOverride = planning.KindPlannerOverrides[i];
            SetKindPlannerKind(plannerOverride.KindId, plannerOverride.PlannerKindCode);
        }

        for (var i = 0; i < planning.EntityPlannerOverrides.Length; i++)
        {
            var plannerOverride = planning.EntityPlannerOverrides[i];
            SetEntityPlannerKind(plannerOverride.EntityId, plannerOverride.PlannerKindCode);
        }
    }

    private void EnsurePlan(PlanningPlanHandle handle)
    {
        if ((uint)handle.Slot == 0 || (uint)handle.Slot > (uint)_scratch.HeaderCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }
}
