namespace Engine.Graph;

using Engine.Domain.Config;
using Engine.Processes;

/// <summary>
/// Engine-produced record of the evaluator's per-tick evaluation.
/// The UI reads this — never computes its own.
/// </summary>
public sealed class EvaluationReport
{
    public int Tick { get; init; }
    public int SourceTick { get; init; }
    public GoalRuntimeId? SelectedGoalId { get; init; }
    public ProcessRuntimeId? ExecutedProcessId { get; init; }
    public bool IsStale { get; init; }
    public string? StatusNote { get; init; }
    public int CandidateCount { get; init; }
    public IReadOnlyList<ProcessCandidate> Candidates { get; init; } = [];
    public LocalitySelectionReport? SelectedLocality { get; init; }
    public IReadOnlyList<LocalityOpportunityReport> LocalityOpportunities { get; init; } = [];
}

/// <summary>
/// One process evaluated against one goal during the evaluation tick.
/// </summary>
public sealed class ProcessCandidate
{
    public required ProcessRuntimeId ProcessId { get; init; }
    public required GoalRuntimeId? GoalId { get; init; }
    public required EntityId ProviderId { get; init; }
    public required float Score { get; init; }
    public required bool PreconditionPassed { get; init; }
    public required string PreconditionDiagnostic { get; init; }
    public required bool OnCooldown { get; init; }
    public bool WasSelected { get; set; }
}

public sealed class LocalitySelectionReport
{
    public required LocalityVisitId VisitId { get; init; }
    public required LocalityRuntimeKey LocalityKey { get; init; }
    public required PrimaryPlane Plane { get; init; }
    public required int Q { get; init; }
    public required int R { get; init; }
    public int? S { get; init; }
    public required int AnchorEntityId { get; init; }
    public required string AnchorName { get; init; }
    public required float BundleScore { get; init; }
    public required int GoalCount { get; init; }
    public required int DistinctTargetCount { get; init; }
    public required int DistinctEffectCount { get; init; }
    public required int Distance { get; init; }
    public required bool IsCurrentLocality { get; init; }
    public required bool IsCommitted { get; init; }
    public required bool Arrived { get; init; }
    public required int ActionsRemaining { get; init; }
    public required int RealizedActions { get; init; }
    public required LocalitySelectionReason SelectionReason { get; init; }
}

public sealed class LocalityOpportunityReport
{
    public required LocalityRuntimeKey LocalityKey { get; init; }
    public required PrimaryPlane Plane { get; init; }
    public required int Q { get; init; }
    public required int R { get; init; }
    public int? S { get; init; }
    public required int AnchorEntityId { get; init; }
    public required string AnchorName { get; init; }
    public required float BundleScore { get; init; }
    public required int GoalCount { get; init; }
    public required int DistinctTargetCount { get; init; }
    public required int DistinctEffectCount { get; init; }
    public required int Distance { get; init; }
    public required bool IsCurrentLocality { get; init; }
    public required bool IsCommitted { get; init; }
    public required bool WasSelected { get; init; }
    public required LocalitySelectionReason SelectionReason { get; init; }
}
