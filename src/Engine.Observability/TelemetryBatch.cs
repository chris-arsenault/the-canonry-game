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
    public Collection<PlannerGoalRow> PlannerGoals { get; } = [];
    public Collection<PlannerTraceRow> PlannerTraces { get; } = [];
    public Collection<PlannerTraceGoalRow> PlannerTraceGoals { get; } = [];
    public Collection<PlannerTraceScoreRow> PlannerTraceScores { get; } = [];
    public Collection<PlannerTraceStepRow> PlannerTraceSteps { get; } = [];
    public Collection<ProcessEventRow> ProcessEvents { get; } = [];
    public Collection<ProcessCandidateRow> ProcessCandidates { get; } = [];
    public Collection<LocalityOpportunityRow> LocalityOpportunities { get; } = [];
    public Collection<AttributeEventRow> AttributeEvents { get; } = [];
    public Collection<MovementEventRow> MovementEvents { get; } = [];
    public Collection<ResourceEventRow> ResourceEvents { get; } = [];
    public Collection<ResourceSnapshotRow> ResourceSnapshots { get; } = [];
    public Collection<RelationshipEventRow> RelationshipEvents { get; } = [];
    public Collection<RelationshipSnapshotRow> RelationshipSnapshots { get; } = [];
    public Collection<GeneratorEventRow> GeneratorEvents { get; } = [];
    public Collection<GeneratedEntityRow> GeneratedEntities { get; } = [];
    public Collection<LifecycleEventRow> LifecycleEvents { get; } = [];
    public Collection<MapGenerationEventRow> MapGenerationEvents { get; } = [];
    public Collection<MapSnapshotRow> MapSnapshots { get; } = [];
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
    int GoalId,
    int DesiredEffectCode,
    int TargetEntityId,
    float Urgency,
    int? PlannedProcessId,
    int? ParentGoalId,
    bool IsActionable,
    int Depth);

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
    int ProcessId,
    int TargetEntityId,
    int RelationshipKindId);

public sealed record ProcessEventRow(
    int RunId,
    int Tick,
    int EntityId,
    int KindId,
    int? SelectedGoalId,
    int? ExecutedProcessId,
    bool IsStale,
    int StateCode,
    int CandidateCount,
    int? VisitId,
    int? LocalityId,
    int? LocalityPlaneCode,
    int? LocalityQ,
    int? LocalityR,
    int? LocalityS,
    float? LocalityBundleScore,
    int? LocalityGoalCount,
    int? LocalityDistinctTargetCount,
    int? LocalityDistinctEffectCount,
    int? LocalityDistance,
    bool? LocalityIsCurrent,
    bool? LocalityIsCommitted,
    bool? LocalityArrived,
    int? LocalityActionsRemaining,
    int? LocalityRealizedActions,
    int? LocalitySelectionReasonCode);

public sealed record ProcessCandidateRow(
    int RunId,
    int Tick,
    int EntityId,
    int KindId,
    int ProcessId,
    int GoalId,
    int ProviderId,
    float Score,
    bool PreconditionPassed,
    int PreconditionDiagnosticCode,
    bool OnCooldown,
    bool WasSelected);

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

public sealed record ResourceEventRow(
    int RunId,
    int Tick,
    int ResourceId,
    int Q,
    int R,
    float OldValue,
    float NewValue,
    float Delta,
    int SourceCodeId);

public sealed record ResourceSnapshotRow(
    int RunId,
    int Tick,
    int ResourceId,
    float Total,
    float Average,
    float Minimum,
    float Maximum);

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
    int FactCount);

public sealed record RelationshipSnapshotRow(
    int RunId,
    int Tick,
    int RelationshipKindId,
    int ActiveCount,
    float AverageStrength,
    float MinimumStrength,
    float MaximumStrength);

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
    int CreatedRelationshipCount);

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

public sealed record MapSnapshotRow(
    int RunId,
    int Tick,
    int TerrainId,
    int CellCount,
    int CoveredCellCount,
    int ActiveEntityCount);
