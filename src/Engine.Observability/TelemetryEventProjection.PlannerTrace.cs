namespace Engine.Observability;

using Engine.Eventing;

internal sealed partial class TelemetryEventProjection
{
    private void RecordProcessExecution(in ProcessExecutedKernelEvent evt)
    {
        var matched = _lastSelectedStepByEntityProcess.GetValueOrDefault((evt.ActorId, evt.ProcessId));
        RecordMatchedPlannerStep(matched);
        _batch.ProcessExecutions.Add(ProcessExecutionRowFrom(evt, matched));
    }

    private void RecordMatchedPlannerStep(PlannerStepRow? matched)
    {
        if (matched is null || !_recordedExecutedPlannerSteps.Add(PlannerStepKey.From(matched)))
        {
            return;
        }

        _batch.PlannerSteps.Add(matched);
        AddSelectedPlannerStepCandidate(matched);
    }

    private ProcessExecutionRow ProcessExecutionRowFrom(in ProcessExecutedKernelEvent evt, PlannerStepRow? matched) =>
        new(
            _runId,
            evt.Tick,
            evt.ActorId,
            evt.ActorKindId,
            matched?.PlanId ?? 0,
            matched?.GoalId ?? 0,
            matched?.StepIndex ?? 0,
            evt.ProcessId,
            evt.AffectedEntityId1 > 0 ? evt.AffectedEntityId1 : evt.ActorId,
            (int)evt.State,
            evt.AffectedEntityId0,
            matched?.StepIndex ?? 0);

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
            step.ProcessId,
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
            LocalityId: 0,
            Selected: true));
    }

    private void ProjectSemanticPlannerTrace(in TraceKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
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
            IsCommitted: IsSelectedTraceStatus(evt.StatusCode),
            Selected: false));
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
            ProcessIdForTrace(evt),
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

    private static int ProcessIdForTrace(in TraceKernelEvent evt) =>
        evt.ActionFamilyCode == ActionFamilyCodes.Process ? evt.ActionId : 0;

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
