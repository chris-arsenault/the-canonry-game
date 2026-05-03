namespace Engine.Observability;

using System.Collections.ObjectModel;

/// <summary>
/// In-memory primitive telemetry rows for a Monte Carlo run or batch.
/// Writers project these rows to Parquet/DuckDB-compatible tables.
/// </summary>
public sealed class TelemetryBatch
{
    public Collection<SymbolRow> Symbols { get; } = [];
    public Collection<EntityNameSymbolRow> EntityNameSymbols { get; } = [];
    public Collection<RunRow> Runs { get; } = [];
    public Collection<TickSnapshotRow> TickSnapshots { get; } = [];
    public Collection<KindSnapshotRow> KindSnapshots { get; } = [];
    public Collection<EntitySnapshotRow> EntitySnapshots { get; } = [];
    public Collection<EntityAttributeSnapshotRow> EntityAttributeSnapshots { get; } = [];
    public Collection<EntityEventRow> EntityEvents { get; } = [];
    public Collection<ExperimentVariantRow> ExperimentVariants { get; } = [];
    public Collection<PlannerGoalRow> PlannerGoals { get; } = [];
    public Collection<PlannerTraceRow> PlannerTraces { get; } = [];
    public Collection<PlannerTraceGoalRow> PlannerTraceGoals { get; } = [];
    public Collection<PlannerTraceScoreRow> PlannerTraceScores { get; } = [];
    public Collection<PlannerTraceStepRow> PlannerTraceSteps { get; } = [];
    public Collection<PlannerCandidateRow> PlannerCandidates { get; } = [];
    public Collection<PlannerScoreRow> PlannerScores { get; } = [];
    public Collection<PlannerStepRow> PlannerSteps { get; } = [];
    public Collection<PlannerLocalityRow> PlannerLocalities { get; } = [];
    public Collection<TraceRow> TraceEvents { get; } = [];
    public Collection<ActionExecutionRow> ActionExecutions { get; } = [];
    public Collection<LocalityOpportunityRow> LocalityOpportunities { get; } = [];
    public Collection<AttributeEventRow> AttributeEvents { get; } = [];
    public Collection<MovementEventRow> MovementEvents { get; } = [];
    public Collection<RelationshipEventRow> RelationshipEvents { get; } = [];
    public Collection<RelationshipSnapshotRow> RelationshipSnapshots { get; } = [];
    public Collection<GambitEventRow> GambitEvents { get; } = [];
    public Collection<GeneratorEventRow> GeneratorEvents { get; } = [];
    public Collection<GeneratorOutputRow> GeneratorOutputs { get; } = [];
    public Collection<GeneratorStateSnapshotRow> GeneratorStateSnapshots { get; } = [];
    public Collection<GeneratedEntityRow> GeneratedEntities { get; } = [];
    public Collection<LifecycleEventRow> LifecycleEvents { get; } = [];
    public Collection<LifecycleReasonEventRow> LifecycleReasonEvents { get; } = [];
    public Collection<MapGenerationEventRow> MapGenerationEvents { get; } = [];
    public Collection<MapCellEventRow> MapCellEvents { get; } = [];
    public Collection<CellInfluenceEventRow> CellInfluenceEvents { get; } = [];
    public Collection<MapSnapshotRow> MapSnapshots { get; } = [];
    public Collection<CoverageSnapshotRow> CoverageSnapshots { get; } = [];
    public Collection<CoverageTerrainSnapshotRow> CoverageTerrainSnapshots { get; } = [];
    public Collection<PlacementEventRow> PlacementEvents { get; } = [];
}

public sealed record SymbolRow(
    int SymbolKind,
    int Id,
    string StableName,
    string DisplayName,
    string? Description = null,
    string? IconKey = null,
    string? ColorKey = null);

public sealed record EntityNameSymbolRow(
    int RunId,
    int EntityId,
    int KindId,
    int SubtypeId,
    int CultureId,
    int NameSeed,
    string DisplayName);

public sealed record RunRow(
    int RunId,
    int? Seed,
    int DomainId,
    long StartedUnixMilliseconds);

public sealed record ExperimentVariantRow(
    int RunId,
    int ExperimentId,
    int VariantId,
    int ConfigHashId,
    int GitShaId,
    bool TraceEnabled,
    int EntitySnapshotCadence,
    int MapSnapshotCadence,
    int GeneratorSnapshotCadence);

public sealed record TickSnapshotRow(
    int RunId,
    int Tick,
    int ActiveEntities,
    int HistoricalEntities,
    int TotalGeneratorFires,
    int ActiveRelationships,
    int CoverageCount,
    int CoveredCells,
    int UncoveredCells,
    float PhysicalSpread,
    float SemanticSpread);

public sealed record KindSnapshotRow(
    int RunId,
    int Tick,
    int KindId,
    int SubtypeId,
    int ActiveCount,
    int HistoricalCount,
    float AverageHealth,
    float MinimumHealth,
    float MaximumHealth);

public sealed record EntitySnapshotRow(
    int RunId,
    int Tick,
    int EntityId,
    int KindId,
    int SubtypeId,
    int CultureId,
    int StatusCode,
    int PhysicalQ,
    int PhysicalR,
    int SemanticQ,
    int SemanticR,
    int SemanticS);

public sealed record EntityAttributeSnapshotRow(
    int RunId,
    int Tick,
    int EntityId,
    int AttributeId,
    float Value);

public sealed record EntityEventRow(
    int RunId,
    int Tick,
    int EventTypeCode,
    int SourceCodeId,
    int EntityId,
    int KindId,
    int SubtypeId,
    int CultureId,
    int? PreviousStatusCode,
    int? CurrentStatusCode,
    int PhysicalQ,
    int PhysicalR,
    int SemanticQ,
    int SemanticR,
    int SemanticS);

public sealed record PlannerGoalRow(
    int RunId,
    int Tick,
    int EntityId,
    int KindId,
    int PlanId,
    int GoalId,
    int EffectScopeCode,
    int EffectValueId,
    int EffectKindId,
    int TargetEntityId,
    float Urgency,
    int ParentGoalId,
    int Depth,
    bool IsActionable,
    int BlockerRef,
    int SupportRuleSourceId);

public sealed record PlannerTraceRow(
    int RunId,
    int StartTick,
    int EndTick,
    int EntityId,
    int PlanId,
    int StatusCode,
    int CandidateCount,
    int StepCount,
    int ReasonCodeId,
    float? TotalScore,
    long StartSequence,
    long EndSequence,
    long DroppedEventCount);

public sealed record PlannerTraceGoalRow(
    int RunId,
    int Tick,
    int EntityId,
    int PlanId,
    int GoalId,
    int CandidateIndex,
    float PreliminaryScore,
    bool WasSelected,
    float? FinalScore);

public sealed record PlannerTraceScoreRow(
    int RunId,
    int Tick,
    int EntityId,
    int PlanId,
    int GoalId,
    int ReasonCodeId,
    int ScoringPassIndex,
    float ScoreDelta,
    float RunningTotal);

public sealed record PlannerTraceStepRow(
    int RunId,
    int Tick,
    int EntityId,
    int PlanId,
    int GoalId,
    int StepIndex,
    int ActionId,
    int TargetEntityId,
    int RelationshipKindId);

public sealed record PlannerCandidateRow(
    int RunId,
    int Tick,
    int EntityId,
    int KindId,
    int PlanId,
    int GoalId,
    int CandidateIndex,
    int ParentCandidateIndex,
    int ActionId,
    int ProviderEntityId,
    int TargetEntityId,
    int EffectScopeCode,
    int EffectValueId,
    int EffectKindId,
    float RawScore,
    float AdjustedScore,
    float RewardScore,
    int PreconditionStatusCode,
    int DiagnosticCode,
    int CooldownStateCode,
    int LocalityId,
    bool Selected);

public sealed record PlannerScoreRow(
    int RunId,
    int Tick,
    int EntityId,
    int PlanId,
    int GoalId,
    int CandidateIndex,
    int ReasonCodeId,
    float ScoreDelta,
    float RunningTotal);

public sealed record PlannerStepRow(
    int RunId,
    int Tick,
    int EntityId,
    int PlanId,
    int GoalId,
    int StepIndex,
    int ActionId,
    int TargetEntityId,
    int RelationshipKindId,
    int StepRoleCode);

public sealed record PlannerLocalityRow(
    int RunId,
    int Tick,
    int EntityId,
    int KindId,
    int PlanId,
    int LocalityId,
    int PlaneCode,
    int Q,
    int R,
    int S,
    int AnchorEntityId,
    float BundleScore,
    int GoalCount,
    int DistinctTargetCount,
    int DistinctEffectCount,
    int Distance,
    bool IsCurrent,
    bool IsCommitted,
    bool Selected);

public sealed record TraceRow(
    int RunId,
    int Tick,
    int PlanId,
    int EntityId,
    int TraceSubjectCode,
    int CandidateIndex,
    int ParentCandidateIndex,
    int ActionFamilyCode,
    int ActionId,
    int EffectScopeCode,
    int EffectValueId,
    int EffectKindId,
    int TargetEntityId,
    int StatusCode,
    float Urgency,
    float RewardOrBaseScore,
    float RawWeight,
    float AdjustedWeight);

public sealed record ActionExecutionRow(
    int RunId,
    int Tick,
    int EntityId,
    int KindId,
    int PlanId,
    int GoalId,
    int StepIndex,
    int ActionId,
    int TargetEntityId,
    int StateCode,
    int AffectedEntityId,
    int LocalityId);

public sealed record LocalityOpportunityRow(
    int RunId,
    int Tick,
    int EntityId,
    int KindId,
    int? VisitId,
    int LocalityId,
    int PlaneCode,
    int Q,
    int R,
    int? S,
    int AnchorEntityId,
    float BundleScore,
    int GoalCount,
    int DistinctTargetCount,
    int DistinctEffectCount,
    int Distance,
    bool IsCurrentLocality,
    bool IsCommitted,
    bool WasSelected,
    int SelectionReasonCode);

public sealed record AttributeEventRow(
    int RunId,
    int Tick,
    int EntityId,
    int AttributeId,
    float OldValue,
    float NewValue,
    float Delta,
    int SourceCodeId);

public sealed record MovementEventRow(
    int RunId,
    int Tick,
    int EntityId,
    int PlaneCode,
    int SourceCodeId,
    int FromQ,
    int FromR,
    int FromS,
    int ToQ,
    int ToR,
    int ToS,
    int Distance);

public sealed record RelationshipEventRow(
    int RunId,
    int Tick,
    int ChangeCodeId,
    int SourceCodeId,
    int RelationshipKindId,
    int PolarityCode,
    int SourceId,
    int TargetId,
    float PreviousStrength,
    float NewStrength,
    int FactId0,
    int FactId1,
    int FactId2,
    int FactId3,
    int FactCount,
    int CauseId = 0);

public sealed record RelationshipSnapshotRow(
    int RunId,
    int Tick,
    int RelationshipKindId,
    int ActiveCount,
    float AverageStrength,
    float MinimumStrength,
    float MaximumStrength);

public sealed record GambitEventRow(
    int RunId,
    int Tick,
    int EventTypeCode,
    int GambitId,
    int ActorId,
    int ActorKindId,
    int TargetEntityId,
    int StateCode,
    bool Succeeded,
    int AffectedEntityCount);

public sealed record GeneratorEventRow(
    int RunId,
    int Tick,
    int EventTypeCode,
    int GeneratorId,
    int StatusCode,
    int TargetPoolCount,
    int InactiveRejectedCount,
    int SubtypeRejectedCount,
    int SaturationRejectedCount,
    int SecondaryTargetRejectedCount,
    int WildernessRejectedCount,
    int TriggerRejectedCount,
    int CandidateCount,
    int FailedFireCount,
    int? SelectedTargetEntityId,
    int CreatedEntityCount,
    int CreatedRelationshipCount,
    int CauseId = 0,
    int CreatedMapCellCount = 0);

public sealed record GeneratorOutputRow(
    int RunId,
    int Tick,
    int CauseId,
    int GeneratorId,
    int OutputDomainCode,
    int OutputEntityId,
    int OutputRelationshipKindId,
    int OutputSourceEntityId,
    int OutputTargetEntityId,
    int OutputQ,
    int OutputR,
    int OutputS,
    int PlacementEventId);

public sealed record GeneratorStateSnapshotRow(
    int RunId,
    int Tick,
    int GeneratorId,
    bool Enabled,
    int Cadence,
    int CooldownRemaining,
    int RunCount,
    int LastRunTick);

public sealed record GeneratedEntityRow(
    int RunId,
    int Tick,
    int GeneratorId,
    int EntityId,
    int KindId,
    int SubtypeId,
    int CultureId,
    int PhysicalQ,
    int PhysicalR,
    int SemanticQ,
    int SemanticR,
    int SemanticS);

public sealed record LifecycleEventRow(
    int RunId,
    int Tick,
    int EventTypeCode,
    int EntityId,
    int KindId,
    int StatusCode);

public sealed record LifecycleReasonEventRow(
    int RunId,
    int Tick,
    int EntityId,
    int ReasonCodeId,
    int CauseId,
    int SourceCodeId);

public sealed record MapGenerationEventRow(
    int RunId,
    int Tick,
    int BaseSeed,
    int Attempt,
    int TerrainSeed,
    int EntitySeed,
    int PhaseId,
    int DescriptionId,
    int ReasonId,
    bool WillRetry);

public sealed record MapCellEventRow(
    int RunId,
    int Tick,
    int EventTypeCode,
    int SourceCodeId,
    int Q,
    int R,
    int SuperregionId,
    int ContinentId,
    int PreviousTerrainId,
    int CurrentTerrainId,
    int PreviousOwnerEntityId,
    int CurrentOwnerEntityId,
    int CauseId = 0);

public sealed record CellInfluenceEventRow(
    int RunId,
    int Tick,
    int SourceCodeId,
    int ActionId,
    int EntityId,
    int PlaneCode,
    int Q,
    int R,
    float PreviousInfluence,
    float CurrentInfluence,
    float Delta);

public sealed record MapSnapshotRow(
    int RunId,
    int Tick,
    int TerrainId,
    int CellCount,
    int CoveredCellCount,
    int ActiveEntityCount);

public sealed record CoverageSnapshotRow(
    int RunId,
    int Tick,
    int OwnerEntityId,
    int CoverageKindCode,
    int CellCount,
    int MinQ,
    int MaxQ,
    int MinR,
    int MaxR,
    int ActiveEntityCount);

public sealed record CoverageTerrainSnapshotRow(
    int RunId,
    int Tick,
    int OwnerEntityId,
    int TerrainId,
    int CellCount);

public sealed record PlacementEventRow(
    int RunId,
    int Tick,
    int CauseId,
    int AnchorEntityId,
    int PlacementModeCode,
    int PlaneCode,
    int Q,
    int R,
    int S,
    int TerrainId,
    int CoverageOwnerEntityId,
    int RejectionReasonCode,
    bool Selected);
