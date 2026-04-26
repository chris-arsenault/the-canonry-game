namespace Engine.Observability;

using System.Collections.ObjectModel;

/// <summary>
/// In-memory columnar telemetry rows for a Monte Carlo run or batch.
/// Writers project these rows to Parquet/DuckDB-compatible tables.
/// </summary>
public sealed class TelemetryBatch
{
    public Collection<RunRow> Runs { get; } = [];
    public Collection<TickSnapshotRow> TickSnapshots { get; } = [];
    public Collection<KindSnapshotRow> KindSnapshots { get; } = [];
    public Collection<EntitySnapshotRow> EntitySnapshots { get; } = [];
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

public sealed record RunRow(
    string RunId,
    int? Seed,
    string Domain,
    string StartedUtc);

public sealed record TickSnapshotRow(
    string RunId,
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
    string RunId,
    int Tick,
    string Kind,
    string Subtype,
    int ActiveCount,
    int HistoricalCount,
    float AverageHealth,
    float MinimumHealth,
    float MaximumHealth);

public sealed record EntitySnapshotRow(
    string RunId,
    int Tick,
    int EntityId,
    string Kind,
    string Subtype,
    string Culture,
    string Status,
    int PhysicalQ,
    int PhysicalR,
    int SemanticQ,
    int SemanticR,
    int SemanticS,
    string Attributes);

public sealed record EntityEventRow(
    string RunId,
    int Tick,
    string EventType,
    int EntityId,
    string Kind,
    string Subtype,
    string Name,
    string Culture,
    string? PreviousStatus,
    string? CurrentStatus,
    int PhysicalQ,
    int PhysicalR,
    int SemanticQ,
    int SemanticR,
    int SemanticS);

public sealed record PlannerGoalRow(
    string RunId,
    int Tick,
    int EntityId,
    string Kind,
    string GoalId,
    string DesiredEffect,
    int TargetEntityId,
    float Urgency,
    string? PlannedProcessId,
    string? ParentGoalId,
    bool IsActionable,
    int Depth);

public sealed record PlannerTraceRow(
    string RunId,
    int StartTick,
    int EndTick,
    int EntityId,
    int PlanId,
    string Status,
    int CandidateCount,
    int StepCount,
    int ReasonCodeId,
    string Reason,
    float? TotalScore,
    long StartSequence,
    long EndSequence,
    long DroppedEventCount);

public sealed record PlannerTraceGoalRow(
    string RunId,
    int Tick,
    int EntityId,
    int PlanId,
    int GoalId,
    string Goal,
    int CandidateIndex,
    float PreliminaryScore,
    bool WasSelected,
    float? FinalScore);

public sealed record PlannerTraceScoreRow(
    string RunId,
    int Tick,
    int EntityId,
    int PlanId,
    int GoalId,
    string Goal,
    int ReasonCodeId,
    string Reason,
    int ScoringPassIndex,
    float ScoreDelta,
    float RunningTotal);

public sealed record PlannerTraceStepRow(
    string RunId,
    int Tick,
    int EntityId,
    int PlanId,
    int GoalId,
    string Goal,
    int StepIndex,
    int ProcessId,
    string Process,
    int TargetEntityId,
    int RelationshipKindId,
    string RelationshipKind);

public sealed record ProcessEventRow(
    string RunId,
    int Tick,
    int EntityId,
    string Kind,
    string? SelectedGoalId,
    string? ExecutedProcessName,
    bool IsStale,
    string? StatusNote,
    int CandidateCount,
    string? VisitId,
    string? LocalityKey,
    string? LocalityPlane,
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
    string? LocalitySelectionReason);

public sealed record ProcessCandidateRow(
    string RunId,
    int Tick,
    int EntityId,
    string Kind,
    string ProcessName,
    string GoalId,
    string ProviderName,
    float Score,
    bool PreconditionPassed,
    string PreconditionDiagnostic,
    bool OnCooldown,
    bool WasSelected);

public sealed record LocalityOpportunityRow(
    string RunId,
    int Tick,
    int EntityId,
    string Kind,
    string? VisitId,
    string LocalityKey,
    string Plane,
    int Q,
    int R,
    int? S,
    int AnchorEntityId,
    string AnchorName,
    float BundleScore,
    int GoalCount,
    int DistinctTargetCount,
    int DistinctEffectCount,
    int Distance,
    bool IsCurrentLocality,
    bool IsCommitted,
    bool WasSelected,
    string SelectionReason);

public sealed record AttributeEventRow(
    string RunId,
    int Tick,
    int EntityId,
    string AttributeName,
    float OldValue,
    float NewValue,
    float Delta,
    string Source);

public sealed record MovementEventRow(
    string RunId,
    int Tick,
    int EntityId,
    string Plane,
    int? FromQ,
    int? FromR,
    int? FromS,
    int? ToQ,
    int? ToR,
    int? ToS,
    int Distance);

public sealed record ResourceEventRow(
    string RunId,
    int Tick,
    string ResourceType,
    int Q,
    int R,
    float OldValue,
    float NewValue,
    float Delta,
    string Source);

public sealed record ResourceSnapshotRow(
    string RunId,
    int Tick,
    string ResourceType,
    float Total,
    float Average,
    float Minimum,
    float Maximum);

public sealed record RelationshipEventRow(
    string RunId,
    int Tick,
    string ChangeType,
    string RelationshipKind,
    string Polarity,
    int SourceId,
    int TargetId,
    float PreviousStrength,
    float NewStrength,
    string Facts);

public sealed record RelationshipSnapshotRow(
    string RunId,
    int Tick,
    string RelationshipKind,
    int ActiveCount,
    float AverageStrength,
    float MinimumStrength,
    float MaximumStrength);

public sealed record GeneratorEventRow(
    string RunId,
    int Tick,
    string EventType,
    string GeneratorId,
    string Status,
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
    string RunId,
    int Tick,
    string GeneratorId,
    int EntityId,
    string Kind,
    string Subtype,
    string Culture,
    int PhysicalQ,
    int PhysicalR,
    int SemanticQ,
    int SemanticR,
    int SemanticS);

public sealed record LifecycleEventRow(
    string RunId,
    int Tick,
    string EventType,
    int EntityId,
    string Kind,
    string Name);

public sealed record MapGenerationEventRow(
    string RunId,
    int Tick,
    int BaseSeed,
    int Attempt,
    int TerrainSeed,
    int EntitySeed,
    string Phase,
    string Description,
    string Reasons,
    bool WillRetry);

public sealed record MapSnapshotRow(
    string RunId,
    int Tick,
    string Terrain,
    int CellCount,
    int CoveredCellCount,
    int ActiveEntityCount);
