namespace Engine.Eventing;

public enum KernelEventType
{
    None = 0,
    EntityCreated = 1,
    EntityStatusChanged = 2,
    EntityMoved = 3,
    AttributeChanged = 4,
    EntityCultureChanged = 5,
    RelationshipChanged = 6,
    TraitAssigned = 7,
    GoalGenerated = 9,
    GeneratorEvaluated = 11,
    GeneratorFired = 12,
    GambitExecuted = 13,
    CommandApplied = 14,
    CommandRejected = 15,
    TickCompleted = 16,
    GeneratorEnabledChanged = 17,
    GambitEvaluated = 18,
    CellCreated = 20,
    CellChanged = 21,
    CellInfluenceChanged = 22,
    ProvenanceInvocationRecorded = 23,
    KernelStorageGrown = 24,
    MapCellStateChanged = 25,
    CellRoadChanged = 26,
    CellTravelStructureChanged = 27,
    CellCultureInfluenceChanged = 28,
    CultureSlotOccupancyChanged = 29,
    GeneratorTargetEvaluated = 30,
    MinorActorVerbExecuted = 31,
    GambitEnabledChanged = 32,
    EntityTagChanged = 33,
    StoryChanged = 34,
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

[KernelEventLayout(KernelEventType.EntityMoved, schemaMinor: 2)]
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
    int ToS,
    [KernelEventIntSlot(9, "from_z", KernelEventSemanticKind.Coordinate)]
    int FromZ,
    [KernelEventIntSlot(10, "to_z", KernelEventSemanticKind.Coordinate)]
    int ToZ);

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

[KernelEventLayout(KernelEventType.EntityCultureChanged, schemaMinor: 0)]
public readonly record struct EntityCultureChangedKernelEvent(
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
    [KernelEventIntSlot(4, "previous_culture_id", KernelEventSemanticKind.CultureId)]
    int PreviousCultureId,
    [KernelEventIntSlot(5, "current_culture_id", KernelEventSemanticKind.CultureId)]
    int CurrentCultureId);

[KernelEventLayout(KernelEventType.TraitAssigned, schemaMinor: 0)]
public readonly record struct TraitAssignedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(1, "trait_id", KernelEventSemanticKind.TraitId)]
    int TraitId,
    [KernelEventIntSlot(2, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId);

[KernelEventLayout(KernelEventType.EntityTagChanged, schemaMinor: 0)]
public readonly record struct EntityTagChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "entity_id", KernelEventSemanticKind.EntityId)]
    int EntityId,
    [KernelEventIntSlot(1, "tag_id", KernelEventSemanticKind.Index)]
    int TagId,
    [KernelEventIntSlot(2, "assigned", KernelEventSemanticKind.Enabled)]
    int Assigned,
    [KernelEventIntSlot(3, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId);

[KernelEventLayout(KernelEventType.StoryChanged, schemaMinor: 0)]
public readonly record struct StoryChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "story_sequence", KernelEventSemanticKind.Index)]
    int StorySequence,
    [KernelEventIntSlot(1, "archetype_id", KernelEventSemanticKind.GeneratorId)]
    int ArchetypeId,
    [KernelEventIntSlot(2, "event_type_code", KernelEventSemanticKind.StatusCode)]
    int EventTypeCode,
    [KernelEventIntSlot(3, "act_index", KernelEventSemanticKind.Index)]
    int ActIndex,
    [KernelEventIntSlot(4, "outcome_code_id", KernelEventSemanticKind.StatusCode)]
    int OutcomeCodeId,
    [KernelEventIntSlot(5, "role_entity_id_0", KernelEventSemanticKind.EntityId)]
    int RoleEntityId0,
    [KernelEventIntSlot(6, "role_entity_id_1", KernelEventSemanticKind.EntityId)]
    int RoleEntityId1,
    [KernelEventIntSlot(7, "role_entity_id_2", KernelEventSemanticKind.EntityId)]
    int RoleEntityId2,
    [KernelEventIntSlot(8, "role_entity_id_3", KernelEventSemanticKind.EntityId)]
    int RoleEntityId3);

[KernelEventLayout(KernelEventType.RelationshipChanged, schemaMinor: 3)]
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
    [KernelEventIntSlot(6, "relationship_id", KernelEventSemanticKind.RelationshipId)]
    int RelationshipId,
    [KernelEventFloatSlot(0, "previous_strength", KernelEventSemanticKind.Strength)]
    float PreviousStrength,
    [KernelEventFloatSlot(1, "new_strength", KernelEventSemanticKind.Strength)]
    float NewStrength,
    [KernelEventIntSlot(7, "action_id", KernelEventSemanticKind.ActionId)]
    int ActionId = 0);

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

[KernelEventLayout(KernelEventType.GeneratorFired, schemaMinor: 1)]
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
    int CreatedRelationshipCount,
    [KernelEventIntSlot(4, "relationship_spend_count", KernelEventSemanticKind.Count)]
    int RelationshipSpendCount = 0,
    [KernelEventIntSlot(5, "output_lock_relationship_created_count", KernelEventSemanticKind.Count)]
    int OutputLockRelationshipCreatedCount = 0);

[KernelEventLayout(KernelEventType.GeneratorTargetEvaluated, schemaMinor: 1)]
public readonly record struct GeneratorTargetEvaluatedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "generator_id", KernelEventSemanticKind.GeneratorId)]
    int GeneratorId,
    [KernelEventIntSlot(1, "target_entity_id", KernelEventSemanticKind.EntityId)]
    int TargetEntityId,
    [KernelEventIntSlot(2, "target_kind_id", KernelEventSemanticKind.KindId)]
    int TargetKindId,
    [KernelEventIntSlot(3, "target_subtype_id", KernelEventSemanticKind.SubtypeId)]
    int TargetSubtypeId,
    [KernelEventIntSlot(4, "target_culture_id", KernelEventSemanticKind.CultureId)]
    int TargetCultureId,
    [KernelEventIntSlot(5, "physical_q", KernelEventSemanticKind.Coordinate)]
    int PhysicalQ,
    [KernelEventIntSlot(6, "physical_r", KernelEventSemanticKind.Coordinate)]
    int PhysicalR,
    [KernelEventIntSlot(7, "semantic_q", KernelEventSemanticKind.Coordinate)]
    int SemanticQ,
    [KernelEventIntSlot(8, "semantic_r", KernelEventSemanticKind.Coordinate)]
    int SemanticR,
    [KernelEventIntSlot(9, "semantic_s", KernelEventSemanticKind.Coordinate)]
    int SemanticS,
    [KernelEventIntSlot(10, "status_code", KernelEventSemanticKind.StatusCode)]
    int StatusCode,
    [KernelEventIntSlot(11, "rejection_reason_code", KernelEventSemanticKind.StatusCode)]
    int RejectionReasonCode,
    [KernelEventIntSlot(12, "physical_field_band_code", KernelEventSemanticKind.StatusCode)]
    int PhysicalFieldBandCode,
    [KernelEventIntSlot(13, "physical_cell_kind_code", KernelEventSemanticKind.StatusCode)]
    int PhysicalCellKindCode,
    [KernelEventIntSlot(14, "semantic_field_band_code", KernelEventSemanticKind.StatusCode)]
    int SemanticFieldBandCode,
    [KernelEventIntSlot(15, "semantic_cell_kind_code", KernelEventSemanticKind.StatusCode)]
    int SemanticCellKindCode,
    [KernelEventFloatSlot(0, "physical_local_raw_mass", KernelEventSemanticKind.RawScalar)]
    float PhysicalLocalRawMass,
    [KernelEventFloatSlot(1, "physical_connected_raw_mass", KernelEventSemanticKind.RawScalar)]
    float PhysicalConnectedRawMass,
    [KernelEventFloatSlot(2, "semantic_local_raw_mass", KernelEventSemanticKind.RawScalar)]
    float SemanticLocalRawMass,
    [KernelEventFloatSlot(3, "semantic_connected_raw_mass", KernelEventSemanticKind.RawScalar)]
    float SemanticConnectedRawMass,
    [KernelEventIntSlot(16, "physical_component_id", KernelEventSemanticKind.RawScalar)]
    int PhysicalComponentId = 0,
    [KernelEventIntSlot(17, "physical_component_cell_count", KernelEventSemanticKind.Count)]
    int PhysicalComponentCellCount = 0,
    [KernelEventIntSlot(18, "semantic_component_id", KernelEventSemanticKind.RawScalar)]
    int SemanticComponentId = 0,
    [KernelEventIntSlot(19, "semantic_component_cell_count", KernelEventSemanticKind.Count)]
    int SemanticComponentCellCount = 0,
    [KernelEventFloatSlot(4, "physical_component_raw_mass", KernelEventSemanticKind.RawScalar)]
    float PhysicalComponentRawMass = 0f,
    [KernelEventFloatSlot(5, "semantic_component_raw_mass", KernelEventSemanticKind.RawScalar)]
    float SemanticComponentRawMass = 0f);

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

[KernelEventLayout(KernelEventType.GambitEnabledChanged, schemaMinor: 0)]
public readonly record struct GambitEnabledChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "gambit_id", KernelEventSemanticKind.GambitId)]
    int GambitId,
    [KernelEventIntSlot(1, "enabled", KernelEventSemanticKind.Enabled)]
    int Enabled);

[KernelEventLayout(KernelEventType.CellCreated, schemaMinor: 3)]
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
    int OwnerEntityId,
    [KernelEventIntSlot(5, "superregion_id", KernelEventSemanticKind.SuperregionId)]
    int SuperregionId,
    [KernelEventIntSlot(6, "continent_id", KernelEventSemanticKind.ContinentId)]
    int ContinentId,
    [KernelEventIntSlot(7, "z", KernelEventSemanticKind.Coordinate)]
    int Z);

[KernelEventLayout(KernelEventType.CellChanged, schemaMinor: 2)]
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
    int CurrentOwnerEntityId,
    [KernelEventIntSlot(7, "superregion_id", KernelEventSemanticKind.SuperregionId)]
    int SuperregionId,
    [KernelEventIntSlot(8, "continent_id", KernelEventSemanticKind.ContinentId)]
    int ContinentId,
    [KernelEventIntSlot(9, "z", KernelEventSemanticKind.Coordinate)]
    int Z);

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

[KernelEventLayout(KernelEventType.CellCultureInfluenceChanged, schemaMinor: 1)]
public readonly record struct CellCultureInfluenceChangedKernelEvent(
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
    [KernelEventIntSlot(4, "culture_id", KernelEventSemanticKind.CultureId)]
    int CultureId,
    [KernelEventIntSlot(5, "z", KernelEventSemanticKind.Coordinate)]
    int Z,
    [KernelEventFloatSlot(0, "previous_share", KernelEventSemanticKind.RawScalar)]
    float PreviousShare,
    [KernelEventFloatSlot(1, "current_share", KernelEventSemanticKind.RawScalar)]
    float CurrentShare,
    [KernelEventFloatSlot(2, "previous_raw_influence", KernelEventSemanticKind.RawScalar)]
    float PreviousRawInfluence = 0f,
    [KernelEventFloatSlot(3, "current_raw_influence", KernelEventSemanticKind.RawScalar)]
    float CurrentRawInfluence = 0f,
    [KernelEventFloatSlot(4, "current_total_raw_influence", KernelEventSemanticKind.RawScalar)]
    float CurrentTotalRawInfluence = 0f);

[KernelEventLayout(KernelEventType.CultureSlotOccupancyChanged, schemaMinor: 0)]
public readonly record struct CultureSlotOccupancyChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventIntSlot(1, "culture_id", KernelEventSemanticKind.CultureId)]
    int CultureId,
    [KernelEventIntSlot(2, "slot_id", KernelEventSemanticKind.RawScalar)]
    int SlotId,
    [KernelEventIntSlot(3, "polarity_code", KernelEventSemanticKind.PolarityCode)]
    int PolarityCode,
    [KernelEventIntSlot(4, "previous_entity_id", KernelEventSemanticKind.EntityId)]
    int PreviousEntityId,
    [KernelEventIntSlot(5, "current_entity_id", KernelEventSemanticKind.EntityId)]
    int CurrentEntityId,
    [KernelEventFloatSlot(0, "previous_score", KernelEventSemanticKind.RawScalar)]
    float PreviousScore,
    [KernelEventFloatSlot(1, "current_score", KernelEventSemanticKind.RawScalar)]
    float CurrentScore);

[KernelEventLayout(KernelEventType.MinorActorVerbExecuted, schemaMinor: 1)]
public readonly record struct MinorActorVerbExecutedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "actor_id", KernelEventSemanticKind.RawScalar)]
    int ActorId,
    [KernelEventIntSlot(1, "source_entity_id", KernelEventSemanticKind.EntityId)]
    int SourceEntityId,
    [KernelEventIntSlot(2, "source_rule_id", KernelEventSemanticKind.RawScalar)]
    int SourceRuleId,
    [KernelEventIntSlot(3, "source_work_tier", KernelEventSemanticKind.RawScalar)]
    int SourceWorkTier,
    [KernelEventIntSlot(4, "effective_radius", KernelEventSemanticKind.RawScalar)]
    int EffectiveRadius,
    [KernelEventIntSlot(5, "culture_id", KernelEventSemanticKind.CultureId)]
    int CultureId,
    [KernelEventIntSlot(6, "verb_id", KernelEventSemanticKind.ActionId)]
    int VerbId,
    [KernelEventIntSlot(7, "purpose_code", KernelEventSemanticKind.RawScalar)]
    int PurposeCode,
    [KernelEventIntSlot(8, "action_code", KernelEventSemanticKind.ActionId)]
    int ActionCode,
    [KernelEventIntSlot(9, "targeting_code", KernelEventSemanticKind.RawScalar)]
    int TargetingCode,
    [KernelEventIntSlot(10, "plane_code", KernelEventSemanticKind.PlaneCode)]
    int PlaneCode,
    [KernelEventIntSlot(11, "q", KernelEventSemanticKind.Coordinate)]
    int Q,
    [KernelEventIntSlot(12, "r", KernelEventSemanticKind.Coordinate)]
    int R,
    [KernelEventIntSlot(13, "status_code", KernelEventSemanticKind.StatusCode)]
    int StatusCode,
    [KernelEventIntSlot(14, "cell_kind_code", KernelEventSemanticKind.RawScalar)]
    int CellKindCode,
    [KernelEventFloatSlot(0, "local_raw_mass", KernelEventSemanticKind.RawScalar)]
    float LocalRawMass,
    [KernelEventFloatSlot(1, "connected_raw_mass", KernelEventSemanticKind.RawScalar)]
    float ConnectedRawMass,
    [KernelEventFloatSlot(2, "score", KernelEventSemanticKind.RawScalar)]
    float Score,
    [KernelEventFloatSlot(3, "delta", KernelEventSemanticKind.RawScalar)]
    float Delta);

[KernelEventLayout(KernelEventType.MapCellStateChanged, schemaMinor: 1)]
public readonly record struct MapCellStateChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "plane_code", KernelEventSemanticKind.PlaneCode)]
    int PlaneCode,
    [KernelEventIntSlot(1, "q", KernelEventSemanticKind.Coordinate)]
    int Q,
    [KernelEventIntSlot(2, "r", KernelEventSemanticKind.Coordinate)]
    int R,
    [KernelEventIntSlot(3, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventIntSlot(4, "previous_terrain_id", KernelEventSemanticKind.TerrainId)]
    int PreviousTerrainId,
    [KernelEventIntSlot(5, "current_terrain_id", KernelEventSemanticKind.TerrainId)]
    int CurrentTerrainId,
    [KernelEventIntSlot(6, "previous_owner_entity_id", KernelEventSemanticKind.EntityId)]
    int PreviousOwnerEntityId,
    [KernelEventIntSlot(7, "current_owner_entity_id", KernelEventSemanticKind.EntityId)]
    int CurrentOwnerEntityId,
    [KernelEventIntSlot(8, "superregion_id", KernelEventSemanticKind.SuperregionId)]
    int SuperregionId,
    [KernelEventIntSlot(9, "continent_id", KernelEventSemanticKind.ContinentId)]
    int ContinentId,
    [KernelEventIntSlot(10, "z", KernelEventSemanticKind.Coordinate)]
    int Z);

[KernelEventLayout(KernelEventType.CellRoadChanged, schemaMinor: 0)]
public readonly record struct CellRoadChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "plane_code", KernelEventSemanticKind.PlaneCode)]
    int PlaneCode,
    [KernelEventIntSlot(1, "q", KernelEventSemanticKind.Coordinate)]
    int Q,
    [KernelEventIntSlot(2, "r", KernelEventSemanticKind.Coordinate)]
    int R,
    [KernelEventIntSlot(3, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventIntSlot(4, "travel_count", KernelEventSemanticKind.Count)]
    int TravelCount,
    [KernelEventFloatSlot(0, "previous_quality", KernelEventSemanticKind.RawScalar)]
    float PreviousQuality,
    [KernelEventFloatSlot(1, "current_quality", KernelEventSemanticKind.RawScalar)]
    float CurrentQuality);

[KernelEventLayout(KernelEventType.CellTravelStructureChanged, schemaMinor: 0)]
public readonly record struct CellTravelStructureChangedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "plane_code", KernelEventSemanticKind.PlaneCode)]
    int PlaneCode,
    [KernelEventIntSlot(1, "q", KernelEventSemanticKind.Coordinate)]
    int Q,
    [KernelEventIntSlot(2, "r", KernelEventSemanticKind.Coordinate)]
    int R,
    [KernelEventIntSlot(3, "source_code_id", KernelEventSemanticKind.SourceCodeId)]
    int SourceCodeId,
    [KernelEventIntSlot(4, "previous_travel_structure_id", KernelEventSemanticKind.RawScalar)]
    int PreviousTravelStructureId,
    [KernelEventIntSlot(5, "current_travel_structure_id", KernelEventSemanticKind.RawScalar)]
    int CurrentTravelStructureId);

[KernelEventLayout(KernelEventType.ProvenanceInvocationRecorded, schemaMinor: 0)]
public readonly record struct ProvenanceInvocationRecordedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "invocation_id", KernelEventSemanticKind.ProvenanceInvocationId)]
    int InvocationId,
    [KernelEventIntSlot(1, "producer_kind_code", KernelEventSemanticKind.ProvenanceProducerKindCode)]
    int ProducerKindCode,
    [KernelEventIntSlot(2, "producer_id", KernelEventSemanticKind.ProvenanceProducerId)]
    int ProducerId,
    [KernelEventIntSlot(3, "primary_entity_id", KernelEventSemanticKind.EntityId)]
    int PrimaryEntityId,
    [KernelEventIntSlot(4, "secondary_entity_id", KernelEventSemanticKind.EntityId)]
    int SecondaryEntityId,
    [KernelEventIntSlot(5, "parent_invocation_id", KernelEventSemanticKind.ProvenanceInvocationId)]
    int ParentInvocationId,
    [KernelEventIntSlot(6, "trigger_kind_code", KernelEventSemanticKind.ProvenanceTriggerKindCode)]
    int TriggerKindCode,
    [KernelEventIntSlot(7, "trigger_id", KernelEventSemanticKind.RawScalar)]
    int TriggerId,
    [KernelEventIntSlot(8, "output_count", KernelEventSemanticKind.Count)]
    int OutputCount);

[KernelEventLayout(KernelEventType.KernelStorageGrown, schemaMinor: 0)]
public readonly record struct KernelStorageGrownKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "storage_code", KernelEventSemanticKind.StorageCode)]
    int StorageCode,
    [KernelEventIntSlot(1, "reason_code", KernelEventSemanticKind.ReasonCodeId)]
    int ReasonCode,
    [KernelEventIntSlot(2, "high_watermark", KernelEventSemanticKind.Count)]
    int HighWatermark,
    [KernelEventIntSlot(3, "previous_capacity", KernelEventSemanticKind.Capacity)]
    int PreviousCapacity,
    [KernelEventIntSlot(4, "current_capacity", KernelEventSemanticKind.Capacity)]
    int CurrentCapacity,
    [KernelEventIntSlot(5, "threshold_permille", KernelEventSemanticKind.Threshold)]
    int ThresholdPermille);

[KernelEventLayout(KernelEventType.Trace, schemaMinor: 3)]
public readonly record struct TraceKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "intent_id", KernelEventSemanticKind.IntentId)]
    int IntentId,
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
    [KernelEventIntSlot(12, "affordance_source_kind_code", KernelEventSemanticKind.RawScalar)]
    int AffordanceSourceKindCode,
    [KernelEventIntSlot(13, "affordance_source_id", KernelEventSemanticKind.RawScalar)]
    int AffordanceSourceId,
    [KernelEventIntSlot(14, "affordance_shape_code", KernelEventSemanticKind.RawScalar)]
    int AffordanceShapeCode,
    [KernelEventIntSlot(15, "affordance_source_kind_mask", KernelEventSemanticKind.RawScalar)]
    int AffordanceSourceKindMask,
    [KernelEventIntSlot(16, "affordance_shape_mask", KernelEventSemanticKind.RawScalar)]
    int AffordanceShapeMask,
    [KernelEventIntSlot(17, "affordance_contribution_count", KernelEventSemanticKind.Count)]
    int AffordanceContributionCount,
    [KernelEventIntSlot(18, "generator_affordance_count", KernelEventSemanticKind.Count)]
    int GeneratorAffordanceCount,
    [KernelEventIntSlot(19, "gambit_affordance_count", KernelEventSemanticKind.Count)]
    int GambitAffordanceCount,
    [KernelEventIntSlot(20, "hidden_affordance_count", KernelEventSemanticKind.Count)]
    int HiddenAffordanceCount,
    [KernelEventIntSlot(21, "selection_bucket_code", KernelEventSemanticKind.RawScalar)]
    int SelectionBucketCode,
    [KernelEventFloatSlot(0, "urgency", KernelEventSemanticKind.Score)]
    float Urgency,
    [KernelEventFloatSlot(1, "reward_or_base_score", KernelEventSemanticKind.Score)]
    float RewardOrBaseScore,
    [KernelEventFloatSlot(2, "raw_weight", KernelEventSemanticKind.Score)]
    float RawWeight,
    [KernelEventFloatSlot(3, "adjusted_weight", KernelEventSemanticKind.Score)]
    float AdjustedWeight,
    [KernelEventFloatSlot(4, "affordance_score", KernelEventSemanticKind.Score)]
    float AffordanceScore,
    [KernelEventFloatSlot(5, "affordance_bonus", KernelEventSemanticKind.Score)]
    float AffordanceBonus);

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
    private readonly int _int12;
    private readonly int _int13;
    private readonly int _int14;
    private readonly int _int15;
    private readonly int _int16;
    private readonly int _int17;
    private readonly int _int18;
    private readonly int _int19;
    private readonly int _int20;
    private readonly int _int21;
    private readonly float _float0;
    private readonly float _float1;
    private readonly float _float2;
    private readonly float _float3;
    private readonly float _float4;
    private readonly float _float5;

    private KernelEvent(
        KernelEventType eventType,
        int tick,
        long worldVersion,
        int programId = 0,
        int sourceCodeId = 0,
        int provenanceInvocationId = 0,
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
        int int12 = 0,
        int int13 = 0,
        int int14 = 0,
        int int15 = 0,
        int int16 = 0,
        int int17 = 0,
        int int18 = 0,
        int int19 = 0,
        int int20 = 0,
        int int21 = 0,
        float float0 = 0f,
        float float1 = 0f,
        float float2 = 0f,
        float float3 = 0f,
        float float4 = 0f,
        float float5 = 0f)
    {
        EventType = eventType;
        Tick = tick;
        WorldVersion = worldVersion;
        ProgramId = programId;
        SourceCodeId = sourceCodeId;
        ProvenanceInvocationId = provenanceInvocationId;
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
        _int12 = int12;
        _int13 = int13;
        _int14 = int14;
        _int15 = int15;
        _int16 = int16;
        _int17 = int17;
        _int18 = int18;
        _int19 = int19;
        _int20 = int20;
        _int21 = int21;
        _float0 = float0;
        _float1 = float1;
        _float2 = float2;
        _float3 = float3;
        _float4 = float4;
        _float5 = float5;
    }

    public KernelEventType EventType { get; }
    public int Tick { get; }
    public long WorldVersion { get; }
    public int ProgramId { get; }
    public int SourceCodeId { get; }
    public int ProvenanceInvocationId { get; }

    public const int IntSlotCount = 22;
    public const int FloatSlotCount = 6;

    public KernelEvent WithExecutionContext(int programId, int sourceCodeId, int provenanceInvocationId = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(programId);
        ArgumentOutOfRangeException.ThrowIfNegative(sourceCodeId);
        ArgumentOutOfRangeException.ThrowIfNegative(provenanceInvocationId);
        return new KernelEvent(
            EventType,
            Tick,
            WorldVersion,
            programId,
            sourceCodeId,
            provenanceInvocationId,
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
            int12: _int12,
            int13: _int13,
            int14: _int14,
            int15: _int15,
            int16: _int16,
            int17: _int17,
            int18: _int18,
            int19: _int19,
            int20: _int20,
            int21: _int21,
            float0: _float0,
            float1: _float1,
            float2: _float2,
            float3: _float3,
            float4: _float4,
            float5: _float5);
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
        12 => _int12,
        13 => _int13,
        14 => _int14,
        15 => _int15,
        16 => _int16,
        17 => _int17,
        18 => _int18,
        19 => _int19,
        20 => _int20,
        21 => _int21,
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Kernel event int slots are numbered 0 through 21."),
    };
#pragma warning restore S1541

    public float GetFloatSlot(int slot) => slot switch
    {
        0 => _float0,
        1 => _float1,
        2 => _float2,
        3 => _float3,
        4 => _float4,
        5 => _float5,
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Kernel event float slots are numbered 0 through 5."),
    };
}

public readonly record struct SequencedKernelEvent(long Sequence, KernelEvent Event);
