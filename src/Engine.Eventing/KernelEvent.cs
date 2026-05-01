namespace Engine.Eventing;

public enum KernelEventType
{
    None = 0,
    EntityCreated = 1,
    EntityStatusChanged = 2,
    EntityMoved = 3,
    AttributeChanged = 4,
    ResourceChanged = 5,
    RelationshipChanged = 6,
    GoalGenerated = 9,
    DegeneracyDetected = 10,
    GeneratorEvaluated = 11,
    GeneratorFired = 12,
    GambitExecuted = 13,
    CommandApplied = 14,
    CommandRejected = 15,
    TickCompleted = 16,
    GeneratorEnabledChanged = 17,
    GambitEvaluated = 18,
    PressureChanged = 19,
    CellCreated = 20,
    CellChanged = 21,
    CellInfluenceChanged = 22,
    PlanStarted = 100,
    GoalConsidered = 101,
    GoalScored = 102,
    GoalSelected = 103,
    PlanStepEmitted = 104,
    PlanCommitted = 105,
    PlanAbandoned = 106,
    Trace = 120,
}

[Flags]
public enum KernelEventState
{
    None = 0,
    Succeeded = 1 << 0,
    Stale = 1 << 1,
    Truncated = 1 << 2,
    Overflowed = 1 << 3,
    Failed = 1 << 4,
}

[KernelEventLayout(KernelEventType.EntityCreated, schemaMinor: 1)]
public readonly record struct EntityCreatedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(1, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventIntSlot(2, "kind_id", KernelEventSemanticKind.KindId)]
    int KindId,
    [KernelEventIntSlot(3, "subtype_id", KernelEventSemanticKind.SubtypeId)]
    int SubtypeId,
    [KernelEventIntSlot(4, "culture_id", KernelEventSemanticKind.CultureId)]
    int CultureId,
    [KernelEventIntSlot(5, "physical_q", KernelEventSemanticKind.Coordinate)]
    int PhysicalQ,
    [KernelEventIntSlot(6, "physical_r", KernelEventSemanticKind.Coordinate)]
    int PhysicalR,
    [KernelEventIntSlot(7, "semantic_q", KernelEventSemanticKind.Coordinate)]
    int SemanticQ,
    [KernelEventIntSlot(8, "semantic_r", KernelEventSemanticKind.Coordinate)]
    int SemanticR,
    [KernelEventIntSlot(9, "semantic_s", KernelEventSemanticKind.Coordinate)]
    int SemanticS);

[KernelEventLayout(KernelEventType.EntityStatusChanged, schemaMinor: 1)]
public readonly record struct EntityStatusChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(1, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventIntSlot(2, "kind_id", KernelEventSemanticKind.KindId)]
    int KindId,
    [KernelEventIntSlot(3, "subtype_id", KernelEventSemanticKind.SubtypeId)]
    int SubtypeId,
    [KernelEventIntSlot(4, "previous_status_code", KernelEventSemanticKind.StatusCode)]
    int PreviousStatusCode,
    [KernelEventIntSlot(5, "current_status_code", KernelEventSemanticKind.StatusCode)]
    int CurrentStatusCode);

[KernelEventLayout(KernelEventType.EntityMoved, schemaMinor: 1)]
public readonly record struct EntityMovedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(1, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventIntSlot(2, "plane_code", KernelEventSemanticKind.PlaneCode)]
    int PlaneCode,
    [KernelEventIntSlot(3, "from_q", KernelEventSemanticKind.Coordinate)]
    int FromQ,
    [KernelEventIntSlot(4, "from_r", KernelEventSemanticKind.Coordinate)]
    int FromR,
    [KernelEventIntSlot(5, "from_s", KernelEventSemanticKind.Coordinate)]
    int FromS,
    [KernelEventIntSlot(6, "to_q", KernelEventSemanticKind.Coordinate)]
    int ToQ,
    [KernelEventIntSlot(7, "to_r", KernelEventSemanticKind.Coordinate)]
    int ToR,
    [KernelEventIntSlot(8, "to_s", KernelEventSemanticKind.Coordinate)]
    int ToS);

[KernelEventLayout(KernelEventType.AttributeChanged, schemaMinor: 0)]
public readonly record struct AttributeChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(1, "attribute_id", KernelEventSemanticKind.AttributeId)]
    int AttributeId,
    [KernelEventIntSlot(2, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventFloatSlot(0, "old_value", KernelEventSemanticKind.RawScalar)]
    float OldValue,
    [KernelEventFloatSlot(1, "new_value", KernelEventSemanticKind.RawScalar)]
    float NewValue);

[KernelEventLayout(KernelEventType.ResourceChanged, schemaMinor: 0)]
public readonly record struct ResourceChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "resource_id", KernelEventSemanticKind.ResourceId)]
    int ResourceId,
    [KernelEventIntSlot(1, "q", KernelEventSemanticKind.Coordinate)]
    int Q,
    [KernelEventIntSlot(2, "r", KernelEventSemanticKind.Coordinate)]
    int R,
    [KernelEventIntSlot(3, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventFloatSlot(0, "old_value", KernelEventSemanticKind.RawScalar)]
    float OldValue,
    [KernelEventFloatSlot(1, "new_value", KernelEventSemanticKind.RawScalar)]
    float NewValue);

[KernelEventLayout(KernelEventType.PressureChanged, schemaMinor: 0)]
public readonly record struct PressureChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "pressure_id", KernelEventSemanticKind.PressureId)]
    int PressureId,
    [KernelEventIntSlot(1, "scope_code", KernelEventSemanticKind.PressureScopeCode)]
    int ScopeCode,
    [KernelEventIntSlot(2, "scope_entity_id", KernelEventSemanticKind.EntityId)]
    int ScopeEntityId,
    [KernelEventIntSlot(3, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventFloatSlot(0, "old_value", KernelEventSemanticKind.RawScalar)]
    float OldValue,
    [KernelEventFloatSlot(1, "new_value", KernelEventSemanticKind.RawScalar)]
    float NewValue);

[KernelEventLayout(KernelEventType.RelationshipChanged, schemaMinor: 1)]
public readonly record struct RelationshipChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "change_code_id", KernelEventSemanticKind.EventCodeId)]
    int ChangeCodeId,
    [KernelEventIntSlot(1, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventIntSlot(2, "relationship_kind_id", KernelEventSemanticKind.RelationshipKindId)]
    int RelationshipKindId,
    [KernelEventIntSlot(3, "polarity_code", KernelEventSemanticKind.PolarityCode)]
    int PolarityCode,
    [KernelEventIntSlot(4, "source_entity_id", KernelEventSemanticKind.EntityId)]
    int SourceEntityId,
    [KernelEventIntSlot(5, "target_entity_id", KernelEventSemanticKind.EntityId)]
    int TargetEntityId,
    [KernelEventIntSlot(6, "fact_id_0", KernelEventSemanticKind.RelationshipFactId)]
    int FactId0,
    [KernelEventIntSlot(7, "fact_id_1", KernelEventSemanticKind.RelationshipFactId)]
    int FactId1,
    [KernelEventIntSlot(8, "fact_id_2", KernelEventSemanticKind.RelationshipFactId)]
    int FactId2,
    [KernelEventIntSlot(9, "fact_id_3", KernelEventSemanticKind.RelationshipFactId)]
    int FactId3,
    [KernelEventIntSlot(10, "fact_count", KernelEventSemanticKind.Count)]
    int FactCount,
    [KernelEventFloatSlot(0, "previous_strength", KernelEventSemanticKind.Strength)]
    float PreviousStrength,
    [KernelEventFloatSlot(1, "new_strength", KernelEventSemanticKind.Strength)]
    float NewStrength);

[KernelEventLayout(KernelEventType.GoalGenerated, schemaMinor: 0)]
public readonly record struct GoalGeneratedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(1, "kind_id", KernelEventSemanticKind.KindId)]
    int KindId,
    [KernelEventIntSlot(2, "goal_count", KernelEventSemanticKind.Count)]
    int GoalCount,
    [KernelEventIntSlot(3, "actionable_goal_count", KernelEventSemanticKind.Count)]
    int ActionableGoalCount,
    [KernelEventIntSlot(4, "state", KernelEventSemanticKind.StatusCode)]
    KernelEventState State);

[KernelEventLayout(KernelEventType.DegeneracyDetected, schemaMinor: 0)]
public readonly record struct DegeneracyDetectedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "detector_id", KernelEventSemanticKind.DetectorId)]
    int DetectorId,
    [KernelEventIntSlot(1, "severity_code", KernelEventSemanticKind.StatusCode)]
    int SeverityCode,
    [KernelEventIntSlot(2, "reason_code_id", KernelEventSemanticKind.ReasonCodeId)]
    int ReasonCodeId);

[KernelEventLayout(KernelEventType.GeneratorEvaluated, schemaMinor: 0)]
public readonly record struct GeneratorEvaluatedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "generator_id", KernelEventSemanticKind.GeneratorId)]
    int GeneratorId,
    [KernelEventIntSlot(1, "status_code", KernelEventSemanticKind.StatusCode)]
    int StatusCode,
    [KernelEventIntSlot(2, "selected_target_entity_id", KernelEventSemanticKind.EntityId)]
    int SelectedTargetEntityId,
    [KernelEventIntSlot(3, "candidate_count", KernelEventSemanticKind.Count)]
    int CandidateCount,
    [KernelEventIntSlot(4, "failed_fire_count", KernelEventSemanticKind.Count)]
    int FailedFireCount);

[KernelEventLayout(KernelEventType.GeneratorFired, schemaMinor: 0)]
public readonly record struct GeneratorFiredKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "generator_id", KernelEventSemanticKind.GeneratorId)]
    int GeneratorId,
    [KernelEventIntSlot(1, "target_entity_id", KernelEventSemanticKind.EntityId)]
    int TargetEntityId,
    [KernelEventIntSlot(2, "created_entity_count", KernelEventSemanticKind.Count)]
    int CreatedEntityCount,
    [KernelEventIntSlot(3, "created_relationship_count", KernelEventSemanticKind.Count)]
    int CreatedRelationshipCount);

[KernelEventLayout(KernelEventType.GambitEvaluated, schemaMinor: 0)]
public readonly record struct GambitEvaluatedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(1, "kind_id", KernelEventSemanticKind.KindId)]
    int KindId,
    [KernelEventIntSlot(2, "executed_gambit_id", KernelEventSemanticKind.GambitId)]
    int ExecutedGambitId,
    [KernelEventIntSlot(3, "candidate_count", KernelEventSemanticKind.Count)]
    int CandidateCount,
    [KernelEventIntSlot(4, "state", KernelEventSemanticKind.StatusCode)]
    KernelEventState State);

[KernelEventLayout(KernelEventType.GambitExecuted, schemaMinor: 0)]
public readonly record struct GambitExecutedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "actor_id", KernelEventSemanticKind.EntityId)]
    int ActorId,
    [KernelEventIntSlot(1, "actor_kind_id", KernelEventSemanticKind.KindId)]
    int ActorKindId,
    [KernelEventIntSlot(2, "gambit_id", KernelEventSemanticKind.GambitId)]
    int GambitId,
    [KernelEventIntSlot(3, "affected_entity_id_0", KernelEventSemanticKind.EntityId)]
    int AffectedEntityId0,
    [KernelEventIntSlot(4, "affected_entity_id_1", KernelEventSemanticKind.EntityId)]
    int AffectedEntityId1,
    [KernelEventIntSlot(5, "affected_entity_count", KernelEventSemanticKind.Count)]
    int AffectedEntityCount,
    [KernelEventIntSlot(6, "state", KernelEventSemanticKind.StatusCode)]
    KernelEventState State);

[KernelEventLayout(KernelEventType.CommandApplied, schemaMinor: 0)]
public readonly record struct CommandAppliedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "command_id", KernelEventSemanticKind.CommandId)]
    int CommandId,
    [KernelEventIntSlot(1, "command_type", KernelEventSemanticKind.CommandType)]
    int CommandType,
    [KernelEventIntSlot(2, "issue_version", KernelEventSemanticKind.RawScalar)]
    int IssueVersion);

[KernelEventLayout(KernelEventType.CommandRejected, schemaMinor: 0)]
public readonly record struct CommandRejectedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "command_id", KernelEventSemanticKind.CommandId)]
    int CommandId,
    [KernelEventIntSlot(1, "command_type", KernelEventSemanticKind.CommandType)]
    int CommandType,
    [KernelEventIntSlot(2, "issue_version", KernelEventSemanticKind.RawScalar)]
    int IssueVersion,
    [KernelEventIntSlot(3, "reason_code_id", KernelEventSemanticKind.ReasonCodeId)]
    int ReasonCodeId);

[KernelEventLayout(KernelEventType.TickCompleted, schemaMinor: 0)]
public readonly record struct TickCompletedKernelEvent(
    int Tick,
    long WorldVersion);

[KernelEventLayout(KernelEventType.GeneratorEnabledChanged, schemaMinor: 0)]
public readonly record struct GeneratorEnabledChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "generator_id", KernelEventSemanticKind.GeneratorId)]
    int GeneratorId,
    [KernelEventIntSlot(1, "enabled", KernelEventSemanticKind.Enabled)]
    int Enabled);

[KernelEventLayout(KernelEventType.CellCreated, schemaMinor: 1)]
public readonly record struct CellCreatedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "q", KernelEventSemanticKind.Coordinate)]
    int Q,
    [KernelEventIntSlot(1, "r", KernelEventSemanticKind.Coordinate)]
    int R,
    [KernelEventIntSlot(2, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventIntSlot(3, "terrain_id", KernelEventSemanticKind.TerrainId)]
    int TerrainId,
    [KernelEventIntSlot(4, "owner_entity_id", KernelEventSemanticKind.EntityId)]
    int OwnerEntityId);

[KernelEventLayout(KernelEventType.CellChanged, schemaMinor: 0)]
public readonly record struct CellChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "q", KernelEventSemanticKind.Coordinate)]
    int Q,
    [KernelEventIntSlot(1, "r", KernelEventSemanticKind.Coordinate)]
    int R,
    [KernelEventIntSlot(2, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventIntSlot(3, "previous_terrain_id", KernelEventSemanticKind.TerrainId)]
    int PreviousTerrainId,
    [KernelEventIntSlot(4, "current_terrain_id", KernelEventSemanticKind.TerrainId)]
    int CurrentTerrainId,
    [KernelEventIntSlot(5, "previous_owner_entity_id", KernelEventSemanticKind.EntityId)]
    int PreviousOwnerEntityId,
    [KernelEventIntSlot(6, "current_owner_entity_id", KernelEventSemanticKind.EntityId)]
    int CurrentOwnerEntityId);

[KernelEventLayout(KernelEventType.CellInfluenceChanged, schemaMinor: 2)]
public readonly record struct CellInfluenceChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "q", KernelEventSemanticKind.Coordinate)]
    int Q,
    [KernelEventIntSlot(1, "r", KernelEventSemanticKind.Coordinate)]
    int R,
    [KernelEventIntSlot(2, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventIntSlot(3, "plane_code", KernelEventSemanticKind.PlaneCode)]
    int PlaneCode,
    [KernelEventIntSlot(4, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(5, "action_id", KernelEventSemanticKind.ActionId)]
    int ActionId,
    [KernelEventFloatSlot(0, "previous_influence", KernelEventSemanticKind.RawScalar)]
    float PreviousInfluence,
    [KernelEventFloatSlot(1, "current_influence", KernelEventSemanticKind.RawScalar)]
    float CurrentInfluence);

[KernelEventLayout(KernelEventType.PlanStarted, schemaMinor: 0)]
public readonly record struct PlanStartedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "plan_id", KernelEventSemanticKind.PlanId)]
    int PlanId,
    [KernelEventIntSlot(1, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(2, "reason_code_id", KernelEventSemanticKind.ReasonCodeId)]
    int ReasonCodeId,
    [KernelEventIntSlot(3, "candidate_count", KernelEventSemanticKind.Count)]
    int CandidateCount);

[KernelEventLayout(KernelEventType.GoalConsidered, schemaMinor: 0)]
public readonly record struct GoalConsideredKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "plan_id", KernelEventSemanticKind.PlanId)]
    int PlanId,
    [KernelEventIntSlot(1, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(2, "goal_id", KernelEventSemanticKind.GoalId)]
    int GoalId,
    [KernelEventIntSlot(3, "candidate_index", KernelEventSemanticKind.Index)]
    int CandidateIndex,
    [KernelEventFloatSlot(0, "preliminary_score", KernelEventSemanticKind.Score)]
    float PreliminaryScore);

[KernelEventLayout(KernelEventType.GoalScored, schemaMinor: 0)]
public readonly record struct GoalScoredKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "plan_id", KernelEventSemanticKind.PlanId)]
    int PlanId,
    [KernelEventIntSlot(1, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(2, "goal_id", KernelEventSemanticKind.GoalId)]
    int GoalId,
    [KernelEventIntSlot(3, "reason_code_id", KernelEventSemanticKind.ReasonCodeId)]
    int ReasonCodeId,
    [KernelEventIntSlot(4, "scoring_pass_index", KernelEventSemanticKind.Index)]
    int ScoringPassIndex,
    [KernelEventFloatSlot(0, "score_delta", KernelEventSemanticKind.Delta)]
    float ScoreDelta,
    [KernelEventFloatSlot(1, "running_total", KernelEventSemanticKind.Score)]
    float RunningTotal);

[KernelEventLayout(KernelEventType.GoalSelected, schemaMinor: 0)]
public readonly record struct GoalSelectedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "plan_id", KernelEventSemanticKind.PlanId)]
    int PlanId,
    [KernelEventIntSlot(1, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(2, "goal_id", KernelEventSemanticKind.GoalId)]
    int GoalId,
    [KernelEventFloatSlot(0, "final_score", KernelEventSemanticKind.Score)]
    float FinalScore);

[KernelEventLayout(KernelEventType.PlanStepEmitted, schemaMinor: 0)]
public readonly record struct PlanStepEmittedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "plan_id", KernelEventSemanticKind.PlanId)]
    int PlanId,
    [KernelEventIntSlot(1, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(2, "goal_id", KernelEventSemanticKind.GoalId)]
    int GoalId,
    [KernelEventIntSlot(3, "step_index", KernelEventSemanticKind.Index)]
    int StepIndex,
    [KernelEventIntSlot(4, "action_id", KernelEventSemanticKind.ActionId)]
    int ActionId,
    [KernelEventIntSlot(5, "target_entity_id", KernelEventSemanticKind.EntityId)]
    int TargetEntityId,
    [KernelEventIntSlot(6, "relationship_kind_id", KernelEventSemanticKind.RelationshipKindId)]
    int RelationshipKindId);

[KernelEventLayout(KernelEventType.PlanCommitted, schemaMinor: 0)]
public readonly record struct PlanCommittedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "plan_id", KernelEventSemanticKind.PlanId)]
    int PlanId,
    [KernelEventIntSlot(1, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(2, "step_count", KernelEventSemanticKind.Count)]
    int StepCount,
    [KernelEventIntSlot(3, "reason_code_id", KernelEventSemanticKind.ReasonCodeId)]
    int ReasonCodeId,
    [KernelEventFloatSlot(0, "total_score", KernelEventSemanticKind.Score)]
    float TotalScore);

[KernelEventLayout(KernelEventType.PlanAbandoned, schemaMinor: 0)]
public readonly record struct PlanAbandonedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "plan_id", KernelEventSemanticKind.PlanId)]
    int PlanId,
    [KernelEventIntSlot(1, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(2, "reason_code_id", KernelEventSemanticKind.ReasonCodeId)]
    int ReasonCodeId);

[KernelEventLayout(KernelEventType.Trace, schemaMinor: 0)]
public readonly record struct TraceKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "plan_id", KernelEventSemanticKind.PlanId)]
    int PlanId,
    [KernelEventIntSlot(1, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(2, "trace_subject_code", KernelEventSemanticKind.TraceSubjectCode)]
    int TraceSubjectCode,
    [KernelEventIntSlot(3, "candidate_index", KernelEventSemanticKind.Index)]
    int CandidateIndex,
    [KernelEventIntSlot(4, "parent_candidate_index", KernelEventSemanticKind.Index)]
    int ParentCandidateIndex,
    [KernelEventIntSlot(5, "action_family_code", KernelEventSemanticKind.ActionFamilyCode)]
    int ActionFamilyCode,
    [KernelEventIntSlot(6, "action_id", KernelEventSemanticKind.ActionId)]
    int ActionId,
    [KernelEventIntSlot(7, "effect_scope_code", KernelEventSemanticKind.EffectScopeCode)]
    int EffectScopeCode,
    [KernelEventIntSlot(8, "effect_value_id", KernelEventSemanticKind.EffectValueId)]
    int EffectValueId,
    [KernelEventIntSlot(9, "effect_kind_id", KernelEventSemanticKind.EffectKindId)]
    int EffectKindId,
    [KernelEventIntSlot(10, "target_entity_id", KernelEventSemanticKind.EntityId)]
    int TargetEntityId,
    [KernelEventIntSlot(11, "status_code", KernelEventSemanticKind.StatusCode)]
    int StatusCode,
    [KernelEventFloatSlot(0, "urgency", KernelEventSemanticKind.Score)]
    float Urgency,
    [KernelEventFloatSlot(1, "reward_or_base_score", KernelEventSemanticKind.Score)]
    float RewardOrBaseScore,
    [KernelEventFloatSlot(2, "raw_weight", KernelEventSemanticKind.Score)]
    float RawWeight,
    [KernelEventFloatSlot(3, "adjusted_weight", KernelEventSemanticKind.Score)]
    float AdjustedWeight);

public readonly partial record struct KernelEvent
{
    private readonly int _int0;
    private readonly int _int1;
    private readonly int _int2;
    private readonly int _int3;
    private readonly int _int4;
    private readonly int _int5;
    private readonly int _int6;
    private readonly int _int7;
    private readonly int _int8;
    private readonly int _int9;
    private readonly int _int10;
    private readonly int _int11;
    private readonly float _float0;
    private readonly float _float1;
    private readonly float _float2;
    private readonly float _float3;

    private KernelEvent(
        KernelEventType eventType,
        int tick,
        long worldVersion,
        int programId = 0,
        int sourceCodeId = 0,
        int int0 = 0,
        int int1 = 0,
        int int2 = 0,
        int int3 = 0,
        int int4 = 0,
        int int5 = 0,
        int int6 = 0,
        int int7 = 0,
        int int8 = 0,
        int int9 = 0,
        int int10 = 0,
        int int11 = 0,
        float float0 = 0f,
        float float1 = 0f,
        float float2 = 0f,
        float float3 = 0f)
    {
        EventType = eventType;
        Tick = tick;
        WorldVersion = worldVersion;
        ProgramId = programId;
        SourceCodeId = sourceCodeId;
        _int0 = int0;
        _int1 = int1;
        _int2 = int2;
        _int3 = int3;
        _int4 = int4;
        _int5 = int5;
        _int6 = int6;
        _int7 = int7;
        _int8 = int8;
        _int9 = int9;
        _int10 = int10;
        _int11 = int11;
        _float0 = float0;
        _float1 = float1;
        _float2 = float2;
        _float3 = float3;
    }

    public KernelEventType EventType { get; }
    public int Tick { get; }
    public long WorldVersion { get; }
    public int ProgramId { get; }
    public int SourceCodeId { get; }

    public const int IntSlotCount = 12;
    public const int FloatSlotCount = 4;

    public KernelEvent WithExecutionContext(int programId, int sourceCodeId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(programId);
        ArgumentOutOfRangeException.ThrowIfNegative(sourceCodeId);
        return new KernelEvent(
            EventType,
            Tick,
            WorldVersion,
            programId,
            sourceCodeId,
            int0: _int0,
            int1: _int1,
            int2: _int2,
            int3: _int3,
            int4: _int4,
            int5: _int5,
            int6: _int6,
            int7: _int7,
            int8: _int8,
            int9: _int9,
            int10: _int10,
            int11: _int11,
            float0: _float0,
            float1: _float1,
            float2: _float2,
            float3: _float3);
    }

#pragma warning disable S1541 // Direct slot dispatch is intentionally allocation-free.
    public int GetIntSlot(int slot) => slot switch
    {
        0 => _int0,
        1 => _int1,
        2 => _int2,
        3 => _int3,
        4 => _int4,
        5 => _int5,
        6 => _int6,
        7 => _int7,
        8 => _int8,
        9 => _int9,
        10 => _int10,
        11 => _int11,
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Kernel event int slots are numbered 0 through 11."),
    };
#pragma warning restore S1541

    public float GetFloatSlot(int slot) => slot switch
    {
        0 => _float0,
        1 => _float1,
        2 => _float2,
        3 => _float3,
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Kernel event float slots are numbered 0 through 3."),
    };
}

public readonly record struct SequencedKernelEvent(long Sequence, KernelEvent Event);
