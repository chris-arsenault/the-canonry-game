namespace Engine.Eventing;

[KernelEventLayout(KernelEventType.MinorActorCreated, schemaMinor: 0)]
public readonly record struct MinorActorCreatedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "actor_occurrence", KernelEventSemanticKind.RawScalar)]
    int ActorOccurrence,
    [KernelEventIntSlot(1, "source_entity_id", KernelEventSemanticKind.EntityId)]
    int SourceEntityId,
    [KernelEventIntSlot(2, "source_rule_id", KernelEventSemanticKind.RawScalar)]
    int SourceRuleId,
    [KernelEventIntSlot(3, "binding_relationship_id", KernelEventSemanticKind.RelationshipId)]
    int BindingRelationshipId,
    [KernelEventIntSlot(4, "culture_id", KernelEventSemanticKind.CultureId)]
    int CultureId,
    [KernelEventIntSlot(5, "role_code", KernelEventSemanticKind.StatusCode)]
    int RoleCode,
    [KernelEventIntSlot(6, "plane_code", KernelEventSemanticKind.PlaneCode)]
    int PlaneCode,
    [KernelEventIntSlot(7, "q", KernelEventSemanticKind.Coordinate)]
    int Q,
    [KernelEventIntSlot(8, "r", KernelEventSemanticKind.Coordinate)]
    int R,
    [KernelEventIntSlot(9, "posture_code", KernelEventSemanticKind.StatusCode)]
    int PostureCode,
    [KernelEventLongSlot(0, "strength_units", KernelEventSemanticKind.RawScalar)]
    long StrengthUnits);

[KernelEventLayout(KernelEventType.MinorActorAssigned, schemaMinor: 0)]
public readonly record struct MinorActorAssignedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "actor_occurrence", KernelEventSemanticKind.RawScalar)]
    int ActorOccurrence,
    [KernelEventIntSlot(1, "source_entity_id", KernelEventSemanticKind.EntityId)]
    int SourceEntityId,
    [KernelEventIntSlot(2, "role_code", KernelEventSemanticKind.StatusCode)]
    int RoleCode,
    [KernelEventIntSlot(3, "plane_code", KernelEventSemanticKind.PlaneCode)]
    int PlaneCode,
    [KernelEventIntSlot(4, "previous_target_q", KernelEventSemanticKind.Coordinate)]
    int PreviousTargetQ,
    [KernelEventIntSlot(5, "previous_target_r", KernelEventSemanticKind.Coordinate)]
    int PreviousTargetR,
    [KernelEventIntSlot(6, "target_q", KernelEventSemanticKind.Coordinate)]
    int TargetQ,
    [KernelEventIntSlot(7, "target_r", KernelEventSemanticKind.Coordinate)]
    int TargetR,
    [KernelEventIntSlot(8, "targeting_code", KernelEventSemanticKind.StatusCode)]
    int TargetingCode,
    [KernelEventIntSlot(9, "posture_code", KernelEventSemanticKind.StatusCode)]
    int PostureCode,
    [KernelEventIntSlot(10, "next_replan_tick", KernelEventSemanticKind.RawScalar)]
    int NextReplanTick,
    [KernelEventIntSlot(11, "commitment_until_tick", KernelEventSemanticKind.RawScalar)]
    int CommitmentUntilTick,
    [KernelEventLongSlot(0, "strength_units", KernelEventSemanticKind.RawScalar)]
    long StrengthUnits);

[KernelEventLayout(KernelEventType.MinorActorMoved, schemaMinor: 0)]
public readonly record struct MinorActorMovedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "actor_occurrence", KernelEventSemanticKind.RawScalar)]
    int ActorOccurrence,
    [KernelEventIntSlot(1, "source_entity_id", KernelEventSemanticKind.EntityId)]
    int SourceEntityId,
    [KernelEventIntSlot(2, "culture_id", KernelEventSemanticKind.CultureId)]
    int CultureId,
    [KernelEventIntSlot(3, "role_code", KernelEventSemanticKind.StatusCode)]
    int RoleCode,
    [KernelEventIntSlot(4, "plane_code", KernelEventSemanticKind.PlaneCode)]
    int PlaneCode,
    [KernelEventIntSlot(5, "from_q", KernelEventSemanticKind.Coordinate)]
    int FromQ,
    [KernelEventIntSlot(6, "from_r", KernelEventSemanticKind.Coordinate)]
    int FromR,
    [KernelEventIntSlot(7, "to_q", KernelEventSemanticKind.Coordinate)]
    int ToQ,
    [KernelEventIntSlot(8, "to_r", KernelEventSemanticKind.Coordinate)]
    int ToR,
    [KernelEventIntSlot(9, "target_q", KernelEventSemanticKind.Coordinate)]
    int TargetQ,
    [KernelEventIntSlot(10, "target_r", KernelEventSemanticKind.Coordinate)]
    int TargetR,
    [KernelEventIntSlot(11, "posture_code", KernelEventSemanticKind.StatusCode)]
    int PostureCode,
    [KernelEventLongSlot(0, "strength_units", KernelEventSemanticKind.RawScalar)]
    long StrengthUnits);

[KernelEventLayout(KernelEventType.MinorActorWorked, schemaMinor: 0)]
public readonly record struct MinorActorWorkedKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "actor_occurrence", KernelEventSemanticKind.RawScalar)]
    int ActorOccurrence,
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
    [KernelEventIntSlot(6, "role_code", KernelEventSemanticKind.StatusCode)]
    int RoleCode,
    [KernelEventIntSlot(7, "action_code", KernelEventSemanticKind.ActionId)]
    int ActionCode,
    [KernelEventIntSlot(8, "targeting_code", KernelEventSemanticKind.StatusCode)]
    int TargetingCode,
    [KernelEventIntSlot(9, "plane_code", KernelEventSemanticKind.PlaneCode)]
    int PlaneCode,
    [KernelEventIntSlot(10, "q", KernelEventSemanticKind.Coordinate)]
    int Q,
    [KernelEventIntSlot(11, "r", KernelEventSemanticKind.Coordinate)]
    int R,
    [KernelEventIntSlot(12, "cell_kind_code", KernelEventSemanticKind.StatusCode)]
    int CellKindCode,
    [KernelEventLongSlot(0, "local_raw_mass_units", KernelEventSemanticKind.RawScalar)]
    long LocalRawMassUnits,
    [KernelEventLongSlot(1, "connected_raw_mass_units", KernelEventSemanticKind.RawScalar)]
    long ConnectedRawMassUnits,
    [KernelEventLongSlot(2, "strength_units", KernelEventSemanticKind.RawScalar)]
    long StrengthUnits,
    [KernelEventLongSlot(3, "delta_units", KernelEventSemanticKind.RawScalar)]
    long DeltaUnits);

[KernelEventLayout(KernelEventType.MinorActorEncountered, schemaMinor: 0)]
public readonly record struct MinorActorEncounteredKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "actor_occurrence", KernelEventSemanticKind.RawScalar)]
    int ActorOccurrence,
    [KernelEventIntSlot(1, "other_actor_occurrence", KernelEventSemanticKind.RawScalar)]
    int OtherActorOccurrence,
    [KernelEventIntSlot(2, "actor_culture_id", KernelEventSemanticKind.CultureId)]
    int ActorCultureId,
    [KernelEventIntSlot(3, "other_culture_id", KernelEventSemanticKind.CultureId)]
    int OtherCultureId,
    [KernelEventIntSlot(4, "actor_role_code", KernelEventSemanticKind.StatusCode)]
    int ActorRoleCode,
    [KernelEventIntSlot(5, "other_role_code", KernelEventSemanticKind.StatusCode)]
    int OtherRoleCode,
    [KernelEventIntSlot(6, "plane_code", KernelEventSemanticKind.PlaneCode)]
    int PlaneCode,
    [KernelEventIntSlot(7, "q", KernelEventSemanticKind.Coordinate)]
    int Q,
    [KernelEventIntSlot(8, "r", KernelEventSemanticKind.Coordinate)]
    int R,
    [KernelEventIntSlot(9, "encounter_code", KernelEventSemanticKind.StatusCode)]
    int EncounterCode,
    [KernelEventIntSlot(10, "actor_posture_code", KernelEventSemanticKind.StatusCode)]
    int ActorPostureCode,
    [KernelEventIntSlot(11, "other_posture_code", KernelEventSemanticKind.StatusCode)]
    int OtherPostureCode,
    [KernelEventLongSlot(0, "actor_strength_units", KernelEventSemanticKind.RawScalar)]
    long ActorStrengthUnits,
    [KernelEventLongSlot(1, "other_strength_units", KernelEventSemanticKind.RawScalar)]
    long OtherStrengthUnits,
    [KernelEventLongSlot(2, "effect_units", KernelEventSemanticKind.RawScalar)]
    long EffectUnits);

[KernelEventLayout(KernelEventType.MinorActorRetired, schemaMinor: 0)]
public readonly record struct MinorActorRetiredKernelEvent(
    int Tick,
    long WorldVersion,
    [KernelEventIntSlot(0, "actor_occurrence", KernelEventSemanticKind.RawScalar)]
    int ActorOccurrence,
    [KernelEventIntSlot(1, "source_entity_id", KernelEventSemanticKind.EntityId)]
    int SourceEntityId,
    [KernelEventIntSlot(2, "source_rule_id", KernelEventSemanticKind.RawScalar)]
    int SourceRuleId,
    [KernelEventIntSlot(3, "binding_relationship_id", KernelEventSemanticKind.RelationshipId)]
    int BindingRelationshipId,
    [KernelEventIntSlot(4, "culture_id", KernelEventSemanticKind.CultureId)]
    int CultureId,
    [KernelEventIntSlot(5, "role_code", KernelEventSemanticKind.StatusCode)]
    int RoleCode,
    [KernelEventIntSlot(6, "plane_code", KernelEventSemanticKind.PlaneCode)]
    int PlaneCode,
    [KernelEventIntSlot(7, "q", KernelEventSemanticKind.Coordinate)]
    int Q,
    [KernelEventIntSlot(8, "r", KernelEventSemanticKind.Coordinate)]
    int R,
    [KernelEventIntSlot(9, "reason_code", KernelEventSemanticKind.StatusCode)]
    int ReasonCode,
    [KernelEventIntSlot(10, "posture_code", KernelEventSemanticKind.StatusCode)]
    int PostureCode,
    [KernelEventLongSlot(0, "strength_units", KernelEventSemanticKind.RawScalar)]
    long StrengthUnits);
