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
    public Collection<ProcessEventRow> ProcessEvents { get; } = [];
    public Collection<ProcessCandidateRow> ProcessCandidates { get; } = [];
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

public sealed record ProcessEventRow(
    string RunId,
    int Tick,
    int EntityId,
    string Kind,
    string? SelectedGoalId,
    string? ExecutedProcessName,
    bool IsStale,
    string? StatusNote,
    int CandidateCount);

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
