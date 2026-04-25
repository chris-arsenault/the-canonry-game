namespace Engine.Graph;

/// <summary>
/// Engine-produced record of the evaluator's per-tick evaluation.
/// The UI reads this — never computes its own.
/// </summary>
public sealed class EvaluationReport
{
    public int Tick { get; init; }
    public int SourceTick { get; init; }
    public string? SelectedGoalId { get; init; }
    public string? ExecutedProcessName { get; init; }
    public bool IsStale { get; init; }
    public string? StatusNote { get; init; }
    public int CandidateCount { get; init; }
    public IReadOnlyList<ProcessCandidate> Candidates { get; init; } = [];
}

/// <summary>
/// One process evaluated against one goal during the evaluation tick.
/// </summary>
public sealed class ProcessCandidate
{
    public required string ProcessName { get; init; }
    public required string GoalId { get; init; }
    public required string ProviderName { get; init; }
    public required float Score { get; init; }
    public required bool PreconditionPassed { get; init; }
    public required string PreconditionDiagnostic { get; init; }
    public required bool OnCooldown { get; init; }
    public bool WasSelected { get; set; }
}
