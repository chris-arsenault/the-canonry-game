namespace Engine.Simulation;

public sealed class SimulationDiagnosticsOptions
{
    public static SimulationDiagnosticsOptions Detailed { get; } = new();

    public static SimulationDiagnosticsOptions LeanPerformance { get; } = new()
    {
        CaptureEvaluationCandidates = false,
        PublishGoalDetails = false,
    };

    public bool CaptureEvaluationCandidates { get; init; } = true;
    public bool PublishGoalDetails { get; init; } = true;
}
