namespace Engine.Observability;

using Engine.Eventing;

internal sealed partial class TelemetryEventProjection
{
    private void AddSelectedPlannerStepCandidate(PlannerStepRow step)
    {
        var entity = GetOrCreateEntity(step.EntityId);
        _batch.PlannerGoals.Add(new PlannerGoalRow(
            _runId,
            step.Tick,
            step.EntityId,
            entity.KindId,
            step.PlanId,
            step.GoalId,
            EffectScopeCode: 0,
            EffectValueId: 0,
            EffectKindId: 0,
            step.TargetEntityId,
            Urgency: 0f,
            ParentGoalId: 0,
            Depth: 0,
            IsActionable: true,
            BlockerRef: 0,
            SupportRuleSourceId: 0));
        _batch.PlannerCandidates.Add(new PlannerCandidateRow(
            _runId,
            step.Tick,
            step.EntityId,
            entity.KindId,
            step.PlanId,
            step.GoalId,
            step.StepIndex,
            ParentCandidateIndex: 0,
            ActionId: step.ActionId,
            ProviderEntityId: step.EntityId,
            step.TargetEntityId,
            EffectScopeCode: 0,
            EffectValueId: 0,
            EffectKindId: 0,
            RawScore: 0f,
            AdjustedScore: 0f,
            RewardScore: 0f,
            PreconditionStatusCode: TraceStatusCodes.None,
            DiagnosticCode: 0,
            CooldownStateCode: 0,
            LocalityId: LocalityIdFor(step.EntityId, step.PlanId),
            Selected: true));
    }

    private void ProjectSemanticPlannerTrace(in TraceKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
        if (evt.TraceSubjectCode is TraceSubjectCodes.MovementIntent
            or TraceSubjectCodes.InfluenceIntent
            or TraceSubjectCodes.RelationshipIntent)
        {
            AddMovementIntentRows(evt, entity);
            return;
        }

        if (evt.TraceSubjectCode == TraceSubjectCodes.Goal)
        {
            AddPlannerGoal(evt, entity);
            return;
        }

        if (evt.TraceSubjectCode == TraceSubjectCodes.Locality)
        {
            AddPlannerLocality(evt, entity);
            return;
        }

        if (PlannerCandidateTrace(evt.TraceSubjectCode))
        {
            AddPlannerCandidate(evt, entity);
        }
    }

    private void AddPlannerGoal(in TraceKernelEvent evt, ObservedEntityProjection entity)
    {
        var actionable = IsActionableTraceStatus(evt.StatusCode);
        _batch.PlannerGoals.Add(new PlannerGoalRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            entity.KindId,
            evt.PlanId,
            evt.CandidateIndex,
            evt.EffectScopeCode,
            evt.EffectValueId,
            evt.EffectKindId,
            evt.TargetEntityId,
            evt.Urgency,
            ParentGoalId: 0,
            Depth: 0,
            actionable,
            BlockerRef: actionable ? 0 : evt.StatusCode,
            SupportRuleSourceId: 0));
    }

    private void AddPlannerLocality(in TraceKernelEvent evt, ObservedEntityProjection entity)
    {
        var selected = IsSelectedTraceStatus(evt.StatusCode);
        if (selected)
        {
            _selectedLocalityByEntityPlan[(evt.EntityId, evt.PlanId)] = evt.CandidateIndex;
        }

        _batch.PlannerLocalities.Add(new PlannerLocalityRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            entity.KindId,
            evt.PlanId,
            LocalityId: evt.CandidateIndex,
            PlaneCodes.Physical,
            Q: 0,
            R: 0,
            S: 0,
            evt.TargetEntityId,
            evt.AdjustedWeight,
            GoalCount: evt.EffectValueId,
            DistinctTargetCount: evt.EffectKindId,
            DistinctEffectCount: evt.EffectScopeCode,
            Distance: (int)MathF.Max(0f, evt.RawWeight),
            IsCurrent: evt.StatusCode == TraceStatusCodes.CommittedByHysteresis,
            IsCommitted: selected,
            Selected: selected));
    }

    private void AddPlannerCandidate(in TraceKernelEvent evt, ObservedEntityProjection entity)
    {
        var goalId = evt.ParentCandidateIndex > 0 ? evt.ParentCandidateIndex : evt.CandidateIndex;
        _batch.PlannerCandidates.Add(new PlannerCandidateRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            entity.KindId,
            evt.PlanId,
            goalId,
            evt.CandidateIndex,
            evt.ParentCandidateIndex,
            evt.ActionId,
            evt.EntityId,
            evt.TargetEntityId,
            evt.EffectScopeCode,
            evt.EffectValueId,
            evt.EffectKindId,
            evt.RawWeight,
            evt.AdjustedWeight,
            evt.RewardOrBaseScore,
            PreconditionStatusCode: evt.StatusCode == TraceStatusCodes.PreconditionFailed ? evt.StatusCode : 0,
            DiagnosticCode: IsActionableTraceStatus(evt.StatusCode) ? 0 : evt.StatusCode,
            CooldownStateCode: evt.StatusCode == TraceStatusCodes.OnCooldown ? evt.StatusCode : 0,
            LocalityId: 0,
            Selected: false));
        _batch.PlannerScores.Add(new PlannerScoreRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            evt.PlanId,
            goalId,
            evt.CandidateIndex,
            evt.StatusCode,
            evt.RawWeight,
            evt.AdjustedWeight));
    }

    private void AddMovementIntentRows(in TraceKernelEvent evt, ObservedEntityProjection entity)
    {
        AddMovementIntentDecisionRows(evt, entity);
        if (IsSelectedTraceStatus(evt.StatusCode))
        {
            AddSelectedMovementIntentRows(evt, entity);
        }
    }

    private void AddMovementIntentDecisionRows(in TraceKernelEvent evt, ObservedEntityProjection entity)
    {
        var diagnosticCode = MovementIntentDiagnosticCode(evt.StatusCode);
        _batch.PlannerGoals.Add(new PlannerGoalRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            entity.KindId,
            evt.PlanId,
            evt.CandidateIndex,
            evt.EffectScopeCode,
            evt.EffectValueId,
            evt.EffectKindId,
            evt.TargetEntityId,
            evt.Urgency,
            ParentGoalId: 0,
            Depth: 0,
            IsActionable: diagnosticCode == 0,
            BlockerRef: diagnosticCode,
            SupportRuleSourceId: 0));
        _batch.PlannerCandidates.Add(new PlannerCandidateRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            entity.KindId,
            evt.PlanId,
            evt.CandidateIndex,
            evt.CandidateIndex,
            evt.ParentCandidateIndex,
            evt.ActionId,
            ProviderEntityId: evt.EntityId,
            evt.TargetEntityId,
            evt.EffectScopeCode,
            evt.EffectValueId,
            evt.EffectKindId,
            evt.RawWeight,
            evt.AdjustedWeight,
            evt.RewardOrBaseScore,
            PreconditionStatusCode: evt.StatusCode == TraceStatusCodes.NoTarget ? evt.StatusCode : 0,
            DiagnosticCode: diagnosticCode,
            CooldownStateCode: 0,
            LocalityId: evt.PlanId,
            IsSelectedTraceStatus(evt.StatusCode)));
        _batch.PlannerScores.Add(new PlannerScoreRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            evt.PlanId,
            evt.CandidateIndex,
            evt.CandidateIndex,
            evt.StatusCode,
            evt.RawWeight,
            evt.AdjustedWeight));
    }

    private void AddSelectedMovementIntentRows(in TraceKernelEvent evt, ObservedEntityProjection entity)
    {
        AddSelectedMovementIntentTrace(evt);
        AddSelectedMovementIntentStep(evt);
        AddSelectedMovementIntentLocality(evt, entity);
        AddSelectedMovementIntentExecution(evt, entity);
    }

    private void AddSelectedMovementIntentTrace(in TraceKernelEvent evt) =>
        _batch.PlannerTraces.Add(new PlannerTraceRow(
            _runId,
            evt.Tick,
            evt.Tick,
            evt.EntityId,
            evt.PlanId,
            PlannerTraceStatusCodes.Committed,
            CandidateCount: evt.CandidateIndex,
            StepCount: 1,
            ReasonCodeId: evt.StatusCode,
            TotalScore: evt.AdjustedWeight,
            StartSequence: 0,
            EndSequence: 0,
            DroppedEventCount: 0));

    private void AddSelectedMovementIntentStep(in TraceKernelEvent evt) =>
        _batch.PlannerSteps.Add(new PlannerStepRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            evt.PlanId,
            evt.CandidateIndex,
            StepIndex: 1,
            evt.ActionId,
            evt.TargetEntityId,
            RelationshipKindId: 0,
            PlanningStepRoleCodes.Immediate));

    private void AddSelectedMovementIntentLocality(in TraceKernelEvent evt, ObservedEntityProjection entity) =>
        _batch.PlannerLocalities.Add(new PlannerLocalityRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            entity.KindId,
            evt.PlanId,
            LocalityId: evt.PlanId,
            PlaneCode: evt.EffectScopeCode,
            Q: 0,
            R: 0,
            S: 0,
            evt.TargetEntityId,
            evt.AdjustedWeight,
            GoalCount: 1,
            DistinctTargetCount: evt.TargetEntityId > 0 ? 1 : 0,
            DistinctEffectCount: 1,
            Distance: 0,
            IsCurrent: false,
            IsCommitted: true,
            Selected: true));

    private void AddSelectedMovementIntentExecution(in TraceKernelEvent evt, ObservedEntityProjection entity) =>
        _batch.ActionExecutions.Add(new ActionExecutionRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            entity.KindId,
            evt.PlanId,
            evt.CandidateIndex,
            StepIndex: 1,
            ActionId: evt.ActionId,
            evt.TargetEntityId,
            StateCode: (int)KernelEventState.Succeeded,
            AffectedEntityId: evt.EntityId,
            LocalityId: evt.PlanId));

    private static int MovementIntentDiagnosticCode(int statusCode) =>
        statusCode is TraceStatusCodes.Offered or TraceStatusCodes.Selected ? 0 : statusCode;

    private int LocalityIdFor(int entityId, int planId) =>
        _selectedLocalityByEntityPlan.GetValueOrDefault((entityId, planId));

    private static bool PlannerCandidateTrace(int traceSubjectCode) =>
        traceSubjectCode is TraceSubjectCodes.ActionSummary or
            TraceSubjectCodes.TargetDetail or
            TraceSubjectCodes.Support;

    private static bool IsActionableTraceStatus(int statusCode) =>
        statusCode is TraceStatusCodes.Actionable or
            TraceStatusCodes.SupportSelected or
            TraceStatusCodes.Selected or
            TraceStatusCodes.CommittedByHysteresis;

    private static bool IsSelectedTraceStatus(int statusCode) =>
        statusCode is TraceStatusCodes.Selected or
            TraceStatusCodes.CommittedByHysteresis or
            TraceStatusCodes.SupportSelected;
}
