namespace Engine.Simulation;

public sealed class SimulationDiagnosticsOptions
{
    public static SimulationDiagnosticsOptions Detailed { get; } = new();

    public static SimulationDiagnosticsOptions LeanPerformance { get; } = new()
    {
        CapturePlanningTraces = false,
        CaptureEvaluationTraces = false,
        CaptureEvaluationCandidates = false,
        PublishGoalDetails = false,
    };

    public bool CapturePlanningTraces { get; init; } = true;
    public bool CaptureEvaluationTraces { get; init; } = true;
    public bool CaptureEvaluationCandidates { get; init; } = true;
    public bool PublishGoalDetails { get; init; } = true;
}
