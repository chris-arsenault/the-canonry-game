namespace Engine.Domain.Graph;

public static class SemanticCoverageAnalyzer
{
    public static SemanticCoverageSummary Summarize(
        SemanticStockNode stock,
        SemanticFlowGraph graph)
    {
        ArgumentNullException.ThrowIfNull(stock);
        ArgumentNullException.ThrowIfNull(graph);

        var relatedFlows = graph.Flows
            .Where(flow => flow.TargetStock == stock)
            .ToList();

        return new SemanticCoverageSummary(
            Count(relatedFlows, SemanticFlowRole.Inflow, SemanticPlaneAxis.None),
            Count(relatedFlows, SemanticFlowRole.Outflow, SemanticPlaneAxis.None),
            Count(relatedFlows, SemanticFlowRole.Inflow, SemanticPlaneAxis.X),
            Count(relatedFlows, SemanticFlowRole.Outflow, SemanticPlaneAxis.X),
            Count(relatedFlows, SemanticFlowRole.Inflow, SemanticPlaneAxis.Y),
            Count(relatedFlows, SemanticFlowRole.Outflow, SemanticPlaneAxis.Y));
    }

    private static int Count(
        IReadOnlyCollection<SemanticFlowEdge> flows,
        SemanticFlowRole role,
        SemanticPlaneAxis axis) =>
        flows.Count(flow => flow.Role == role && flow.PlaneAxis == axis);
}

public readonly record struct SemanticCoverageSummary(
    int ScalarInflows,
    int ScalarOutflows,
    int XInflows,
    int XOutflows,
    int YInflows,
    int YOutflows);
