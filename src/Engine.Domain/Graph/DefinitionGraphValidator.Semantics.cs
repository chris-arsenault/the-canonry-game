namespace Engine.Domain.Graph;

using Engine.Behaviors;
using Engine.Kinds;
using Engine.Wiring;

public static partial class DefinitionGraphValidator
{
    private static void ValidateSemanticFlows(DefinitionGraph graph, List<string> errors)
    {
        foreach (var stock in graph.SemanticFlows.Stocks)
        {
            var coverage = SemanticCoverageAnalyzer.Summarize(stock, graph.SemanticFlows);
            var stockLabel = stock.Id.Label;

            ValidateScalarCoverage(stock, coverage, stockLabel, errors);
            ValidateAxisCoverage(stock, coverage, stockLabel, errors);
            ValidateStockDecayRules(stock, graph.SemanticFlows, stockLabel, errors);
            ValidatePlaneAxisBalance(stock, coverage, stockLabel, errors);
        }
    }

    private static void ValidateScalarCoverage(
        SemanticStockNode stock,
        SemanticCoverageSummary coverage,
        string stockLabel,
        List<string> errors)
    {
        if (stock.RequiredFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Inflow) &&
            coverage.ScalarInflows == 0)
        {
            errors.Add(
                $"Semantic stock '{stockLabel}' requires at least one inflow but none are declared.");
        }

        if (stock.RequiredFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Outflow) &&
            coverage.ScalarOutflows == 0)
        {
            errors.Add(
                $"Semantic stock '{stockLabel}' requires at least one outflow but none are declared.");
        }
    }

    private static void ValidateAxisCoverage(
        SemanticStockNode stock,
        SemanticCoverageSummary coverage,
        string stockLabel,
        List<string> errors)
    {
        ValidateCoverageRequirement(stock.RequiredXFlowCoverage, coverage.XInflows, stockLabel, "X-axis inflow", errors);
        ValidateCoverageRequirement(stock.RequiredXFlowCoverage, coverage.XOutflows, stockLabel, "X-axis outflow", errors);
        ValidateCoverageRequirement(stock.RequiredYFlowCoverage, coverage.YInflows, stockLabel, "Y-axis inflow", errors);
        ValidateCoverageRequirement(stock.RequiredYFlowCoverage, coverage.YOutflows, stockLabel, "Y-axis outflow", errors);
    }

    private static void ValidateCoverageRequirement(
        SemanticFlowCoverageRequirement requirement,
        int count,
        string stockLabel,
        string label,
        List<string> errors)
    {
        var needed = label.Contains("inflow", StringComparison.Ordinal)
            ? SemanticFlowCoverageRequirement.Inflow
            : SemanticFlowCoverageRequirement.Outflow;
        if (requirement.HasFlag(needed) && count == 0)
        {
            errors.Add(
                $"Semantic stock '{stockLabel}' requires at least one {label} but none are declared.");
        }
    }

    private static void ValidatePlaneAxisBalance(
        SemanticStockNode stock,
        SemanticCoverageSummary coverage,
        string stockLabel,
        List<string> errors)
    {
        if (stock.StockClass != SemanticStockClass.Plane)
        {
            return;
        }

        if (coverage.XOutflows > 0 && coverage.XInflows == 0)
        {
            errors.Add(
                $"Plane stock '{stockLabel}' has X-axis outflows but no X-axis inflows; planes with outflow on an axis must have inflow on that axis.");
        }

        if (coverage.YOutflows > 0 && coverage.YInflows == 0)
        {
            errors.Add(
                $"Plane stock '{stockLabel}' has Y-axis outflows but no Y-axis inflows; planes with outflow on an axis must have inflow on that axis.");
        }
    }

    private static void ValidateStockDecayRules(
        SemanticStockNode stock,
        SemanticFlowGraph graph,
        string stockLabel,
        List<string> errors)
    {
        var relatedFlows = graph.Flows.Where(flow => flow.TargetStock == stock).ToList();
        var decayFlows = relatedFlows
            .Where(flow => flow.Mechanism == SemanticMechanism.Decay)
            .ToList();

        if (stock.Id is ResourceStockId)
        {
            if (decayFlows.Count > 0)
            {
                errors.Add(
                    $"Semantic stock '{stockLabel}' is a resource; resource stocks must not declare engine_decay flows.");
            }

            return;
        }

        if (stock.StockClass == SemanticStockClass.Plane)
        {
            if (decayFlows.Count > 0)
            {
                errors.Add(
                    $"Semantic stock '{stockLabel}' is a plane; plane stocks must not declare engine_decay flows.");
            }

            return;
        }

        var hasDecayOutflow = decayFlows.Exists(flow => flow.Role == SemanticFlowRole.Outflow);
        if (!hasDecayOutflow)
        {
            errors.Add(
                $"Semantic stock '{stockLabel}' is scalar and must declare an engine_decay outflow.");
        }

        var nonOutflowDecay = decayFlows.Exists(flow => flow.Role != SemanticFlowRole.Outflow);
        if (nonOutflowDecay)
        {
            errors.Add(
                $"Semantic stock '{stockLabel}' declares a decay flow that is not an outflow; decay only applies as an outflow.");
        }
    }

    private static void ValidateProjectedSemanticEdges(DefinitionGraph graph, List<string> errors)
    {
        var projectedEdges = SemanticStockGraphProjection.ProjectAttributeEdges(graph.SemanticFlows);
        var massFlowPairs = projectedEdges
            .Where(edge => !edge.IsGateOnly)
            .Select(edge => (edge.SourceId, edge.TargetId))
            .ToHashSet();

        foreach (var edge in projectedEdges)
        {
            if (!TryGetReversalPair(edge, graph, massFlowPairs, out var pair))
            {
                continue;
            }

            AddProjectedReversalError(pair, errors);
        }
    }

    private static bool TryGetReversalPair(
        SemanticProjectedAttributeEdge edge,
        DefinitionGraph graph,
        HashSet<(SemanticStockId SourceId, SemanticStockId TargetId)> massFlowPairs,
        out string pair)
    {
        pair = string.Empty;
        if (edge.IsGateOnly || edge.SourceId == edge.TargetId)
        {
            return false;
        }

        if (edge.SourceId is not AttributeStockId sourceAttributeId ||
            edge.TargetId is not AttributeStockId targetAttributeId)
        {
            return false;
        }

        if (!IsScalarAttributePair(graph, sourceAttributeId, targetAttributeId) ||
            !massFlowPairs.Contains((edge.TargetId, edge.SourceId)))
        {
            return false;
        }

        pair = $"{sourceAttributeId.Label} <-> {targetAttributeId.Label}";
        return true;
    }

    private static bool IsScalarAttributePair(
        DefinitionGraph graph,
        AttributeStockId sourceAttributeId,
        AttributeStockId targetAttributeId) =>
        graph.Attributes.TryGetValue(sourceAttributeId.Key, out var sourceAttribute) &&
        graph.Attributes.TryGetValue(targetAttributeId.Key, out var targetAttribute) &&
        sourceAttribute.Shape == DefinitionAttributeShape.Scalar &&
        targetAttribute.Shape == DefinitionAttributeShape.Scalar;

    private static void AddProjectedReversalError(string pair, List<string> errors)
    {
        if (errors.Exists(error => error.Contains(pair, StringComparison.Ordinal)))
        {
            return;
        }

        errors.Add(
            $"Semantic stock graph contains a direct two-node reversal '{pair}'. Encode a longer path or an explicit modifier/coupling instead of opposite one-hop flows.");
    }
}
