namespace Engine.Eventing;

public readonly partial record struct KernelEvent
{
    public static KernelEvent From(in EntityCreatedKernelEvent payload) =>
        new(
            KernelEventType.EntityCreated,
            payload.Tick,
            payload.WorldVersion,
            payload.EntityId,
            payload.SourceCodeId,
            payload.KindId,
            payload.SubtypeId,
            payload.CultureId,
            payload.PhysicalQ,
            payload.PhysicalR,
            payload.SemanticQ,
            payload.SemanticR,
            payload.SemanticS);

    public static KernelEvent From(in EntityStatusChangedKernelEvent payload) =>
        new(
            KernelEventType.EntityStatusChanged,
            payload.Tick,
            payload.WorldVersion,
            payload.EntityId,
            payload.SourceCodeId,
            payload.KindId,
            payload.SubtypeId,
            payload.PreviousStatusCode,
            payload.CurrentStatusCode);

    public static KernelEvent From(in EntityMovedKernelEvent payload) =>
        new(
            KernelEventType.EntityMoved,
            payload.Tick,
            payload.WorldVersion,
            payload.EntityId,
            payload.SourceCodeId,
            payload.PlaneCode,
            payload.FromQ,
            payload.FromR,
            payload.FromS,
            payload.ToQ,
            payload.ToR,
            payload.ToS);

    public static KernelEvent From(in AttributeChangedKernelEvent payload) =>
        new(
            KernelEventType.AttributeChanged,
            payload.Tick,
            payload.WorldVersion,
            payload.EntityId,
            payload.AttributeId,
            payload.SourceCodeId,
            x: payload.OldValue,
            y: payload.NewValue);

    public static KernelEvent From(in ResourceChangedKernelEvent payload) =>
        new(
            KernelEventType.ResourceChanged,
            payload.Tick,
            payload.WorldVersion,
            payload.ResourceId,
            payload.Q,
            payload.R,
            payload.SourceCodeId,
            x: payload.OldValue,
            y: payload.NewValue);

    public static KernelEvent From(in RelationshipChangedKernelEvent payload) =>
        new(
            KernelEventType.RelationshipChanged,
            payload.Tick,
            payload.WorldVersion,
            payload.ChangeCodeId,
            payload.SourceCodeId,
            payload.RelationshipKindId,
            payload.PolarityCode,
            payload.SourceEntityId,
            payload.TargetEntityId,
            payload.FactId0,
            payload.FactId1,
            payload.FactId2,
            payload.FactId3,
            payload.FactCount,
            x: payload.PreviousStrength,
            y: payload.NewStrength);

    public static KernelEvent From(in ProcessEvaluatedKernelEvent payload) =>
        new(
            KernelEventType.ProcessEvaluated,
            payload.Tick,
            payload.WorldVersion,
            payload.EntityId,
            payload.KindId,
            payload.SelectedGoalId,
            payload.ExecutedProcessId,
            payload.CandidateCount,
            (int)payload.State);

    public static KernelEvent From(in ProcessExecutedKernelEvent payload) =>
        new(
            KernelEventType.ProcessExecuted,
            payload.Tick,
            payload.WorldVersion,
            payload.ActorId,
            payload.ActorKindId,
            payload.ProcessId,
            payload.AffectedEntityId0,
            payload.AffectedEntityId1,
            payload.AffectedEntityCount,
            (int)payload.State);

    public static KernelEvent From(in GoalGeneratedKernelEvent payload) =>
        new(
            KernelEventType.GoalGenerated,
            payload.Tick,
            payload.WorldVersion,
            payload.EntityId,
            payload.KindId,
            payload.GoalCount,
            payload.ActionableGoalCount,
            (int)payload.State);

    public static KernelEvent From(in DegeneracyDetectedKernelEvent payload) =>
        new(
            KernelEventType.DegeneracyDetected,
            payload.Tick,
            payload.WorldVersion,
            payload.DetectorId,
            payload.SeverityCode,
            payload.ReasonCodeId);

    public static KernelEvent From(in GeneratorEvaluatedKernelEvent payload) =>
        new(
            KernelEventType.GeneratorEvaluated,
            payload.Tick,
            payload.WorldVersion,
            payload.GeneratorId,
            payload.StatusCode,
            payload.SelectedTargetEntityId,
            payload.CandidateCount,
            payload.FailedFireCount);

    public static KernelEvent From(in GeneratorFiredKernelEvent payload) =>
        new(
            KernelEventType.GeneratorFired,
            payload.Tick,
            payload.WorldVersion,
            payload.GeneratorId,
            payload.TargetEntityId,
            payload.CreatedEntityCount,
            payload.CreatedRelationshipCount);

    public static KernelEvent From(in GambitEvaluatedKernelEvent payload) =>
        new(
            KernelEventType.GambitEvaluated,
            payload.Tick,
            payload.WorldVersion,
            payload.EntityId,
            payload.KindId,
            payload.ExecutedGambitId,
            payload.CandidateCount,
            (int)payload.State);

    public static KernelEvent From(in GambitExecutedKernelEvent payload) =>
        new(
            KernelEventType.GambitExecuted,
            payload.Tick,
            payload.WorldVersion,
            payload.ActorId,
            payload.ActorKindId,
            payload.GambitId,
            payload.AffectedEntityId0,
            payload.AffectedEntityId1,
            payload.AffectedEntityCount,
            (int)payload.State);

    public static KernelEvent From(in CommandAppliedKernelEvent payload) =>
        new(
            KernelEventType.CommandApplied,
            payload.Tick,
            payload.WorldVersion,
            payload.CommandId,
            payload.CommandType,
            payload.IssueVersion);

    public static KernelEvent From(in CommandRejectedKernelEvent payload) =>
        new(
            KernelEventType.CommandRejected,
            payload.Tick,
            payload.WorldVersion,
            payload.CommandId,
            payload.CommandType,
            payload.IssueVersion,
            payload.ReasonCodeId);

    public static KernelEvent From(in TickCompletedKernelEvent payload) =>
        new(
            KernelEventType.TickCompleted,
            payload.Tick,
            payload.WorldVersion);

    public static KernelEvent From(in GeneratorEnabledChangedKernelEvent payload) =>
        new(
            KernelEventType.GeneratorEnabledChanged,
            payload.Tick,
            payload.WorldVersion,
            payload.GeneratorId,
            payload.Enabled);

    public static KernelEvent From(in CellCreatedKernelEvent payload) =>
        new(
            KernelEventType.CellCreated,
            payload.Tick,
            payload.WorldVersion,
            payload.Q,
            payload.R,
            payload.SourceCodeId,
            payload.TerrainId,
            payload.OwnerEntityId);

    public static KernelEvent From(in PlanStartedKernelEvent payload) =>
        new(
            KernelEventType.PlanStarted,
            payload.Tick,
            payload.WorldVersion,
            payload.PlanId,
            payload.EntityId,
            payload.ReasonCodeId,
            payload.CandidateCount);

    public static KernelEvent From(in GoalConsideredKernelEvent payload) =>
        new(
            KernelEventType.GoalConsidered,
            payload.Tick,
            payload.WorldVersion,
            payload.PlanId,
            payload.EntityId,
            payload.GoalId,
            payload.CandidateIndex,
            x: payload.PreliminaryScore);

    public static KernelEvent From(in GoalScoredKernelEvent payload) =>
        new(
            KernelEventType.GoalScored,
            payload.Tick,
            payload.WorldVersion,
            payload.PlanId,
            payload.EntityId,
            payload.GoalId,
            payload.ReasonCodeId,
            payload.ScoringPassIndex,
            x: payload.ScoreDelta,
            y: payload.RunningTotal);

    public static KernelEvent From(in GoalSelectedKernelEvent payload) =>
        new(
            KernelEventType.GoalSelected,
            payload.Tick,
            payload.WorldVersion,
            payload.PlanId,
            payload.EntityId,
            payload.GoalId,
            x: payload.FinalScore);

    public static KernelEvent From(in PlanStepEmittedKernelEvent payload) =>
        new(
            KernelEventType.PlanStepEmitted,
            payload.Tick,
            payload.WorldVersion,
            payload.PlanId,
            payload.EntityId,
            payload.GoalId,
            payload.StepIndex,
            payload.ProcessId,
            payload.TargetEntityId,
            payload.RelationshipKindId);

    public static KernelEvent From(in PlanCommittedKernelEvent payload) =>
        new(
            KernelEventType.PlanCommitted,
            payload.Tick,
            payload.WorldVersion,
            payload.PlanId,
            payload.EntityId,
            payload.StepCount,
            payload.ReasonCodeId,
            x: payload.TotalScore);

    public static KernelEvent From(in PlanAbandonedKernelEvent payload) =>
        new(
            KernelEventType.PlanAbandoned,
            payload.Tick,
            payload.WorldVersion,
            payload.PlanId,
            payload.EntityId,
            payload.ReasonCodeId);

    public EntityCreatedKernelEvent AsEntityCreated()
    {
        EnsureType(KernelEventType.EntityCreated);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, _f, _g, _h, _i, _j);
    }

    public EntityStatusChangedKernelEvent AsEntityStatusChanged()
    {
        EnsureType(KernelEventType.EntityStatusChanged);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, _f);
    }

    public EntityMovedKernelEvent AsEntityMoved()
    {
        EnsureType(KernelEventType.EntityMoved);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, _f, _g, _h, _i);
    }

    public AttributeChangedKernelEvent AsAttributeChanged()
    {
        EnsureType(KernelEventType.AttributeChanged);
        return new(Tick, WorldVersion, _a, _b, _c, _x, _y);
    }

    public ResourceChangedKernelEvent AsResourceChanged()
    {
        EnsureType(KernelEventType.ResourceChanged);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _x, _y);
    }

    public RelationshipChangedKernelEvent AsRelationshipChanged()
    {
        EnsureType(KernelEventType.RelationshipChanged);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, _f, _g, _h, _i, _j, _k, _x, _y);
    }

    public ProcessEvaluatedKernelEvent AsProcessEvaluated()
    {
        EnsureType(KernelEventType.ProcessEvaluated);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, (KernelEventState)_f);
    }

    public ProcessExecutedKernelEvent AsProcessExecuted()
    {
        EnsureType(KernelEventType.ProcessExecuted);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, _f, (KernelEventState)_g);
    }

    public GoalGeneratedKernelEvent AsGoalGenerated()
    {
        EnsureType(KernelEventType.GoalGenerated);
        return new(Tick, WorldVersion, _a, _b, _c, _d, (KernelEventState)_e);
    }

    public DegeneracyDetectedKernelEvent AsDegeneracyDetected()
    {
        EnsureType(KernelEventType.DegeneracyDetected);
        return new(Tick, WorldVersion, _a, _b, _c);
    }

    public GeneratorEvaluatedKernelEvent AsGeneratorEvaluated()
    {
        EnsureType(KernelEventType.GeneratorEvaluated);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e);
    }

    public GeneratorFiredKernelEvent AsGeneratorFired()
    {
        EnsureType(KernelEventType.GeneratorFired);
        return new(Tick, WorldVersion, _a, _b, _c, _d);
    }

    public GambitEvaluatedKernelEvent AsGambitEvaluated()
    {
        EnsureType(KernelEventType.GambitEvaluated);
        return new(Tick, WorldVersion, _a, _b, _c, _d, (KernelEventState)_e);
    }

    public GambitExecutedKernelEvent AsGambitExecuted()
    {
        EnsureType(KernelEventType.GambitExecuted);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, _f, (KernelEventState)_g);
    }

    public CommandAppliedKernelEvent AsCommandApplied()
    {
        EnsureType(KernelEventType.CommandApplied);
        return new(Tick, WorldVersion, _a, _b, _c);
    }

    public CommandRejectedKernelEvent AsCommandRejected()
    {
        EnsureType(KernelEventType.CommandRejected);
        return new(Tick, WorldVersion, _a, _b, _c, _d);
    }

    public TickCompletedKernelEvent AsTickCompleted()
    {
        EnsureType(KernelEventType.TickCompleted);
        return new(Tick, WorldVersion);
    }

    public GeneratorEnabledChangedKernelEvent AsGeneratorEnabledChanged()
    {
        EnsureType(KernelEventType.GeneratorEnabledChanged);
        return new(Tick, WorldVersion, _a, _b);
    }

    public CellCreatedKernelEvent AsCellCreated()
    {
        EnsureType(KernelEventType.CellCreated);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e);
    }

    public PlanStartedKernelEvent AsPlanStarted()
    {
        EnsureType(KernelEventType.PlanStarted);
        return new(Tick, WorldVersion, _a, _b, _c, _d);
    }

    public GoalConsideredKernelEvent AsGoalConsidered()
    {
        EnsureType(KernelEventType.GoalConsidered);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _x);
    }

    public GoalScoredKernelEvent AsGoalScored()
    {
        EnsureType(KernelEventType.GoalScored);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, _x, _y);
    }

    public GoalSelectedKernelEvent AsGoalSelected()
    {
        EnsureType(KernelEventType.GoalSelected);
        return new(Tick, WorldVersion, _a, _b, _c, _x);
    }

    public PlanStepEmittedKernelEvent AsPlanStepEmitted()
    {
        EnsureType(KernelEventType.PlanStepEmitted);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, _f, _g);
    }

    public PlanCommittedKernelEvent AsPlanCommitted()
    {
        EnsureType(KernelEventType.PlanCommitted);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _x);
    }

    public PlanAbandonedKernelEvent AsPlanAbandoned()
    {
        EnsureType(KernelEventType.PlanAbandoned);
        return new(Tick, WorldVersion, _a, _b, _c);
    }

    private void EnsureType(KernelEventType expected)
    {
        if (EventType != expected)
        {
            throw new InvalidOperationException($"Kernel event is {EventType}, not {expected}.");
        }
    }
}
