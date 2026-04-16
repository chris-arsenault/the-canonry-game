namespace Engine.Domain.Graph;

using Engine.Kinds;

public static class SemanticStockGraphProjection
{
    public static IReadOnlyList<SemanticProjectedAttributeEdge> ProjectAttributeEdges(
        SemanticFlowGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var grouped = new Dictionary<(DefinitionAttributeKey Source, DefinitionAttributeKey Target), List<SemanticFlowEdge>>();

        foreach (var flow in graph.Flows)
        {
            foreach (var sourceRef in flow.SourceRefs)
            {
                if (sourceRef.ValueId is not AttributeValueId { AttributeId: var attributeId } ||
                    sourceRef.Kind is not EntityKind kind)
                {
                    continue;
                }

                var referenceKey = new DefinitionAttributeKey(kind, attributeId);
                var stockKey = flow.TargetStock.AttributeKey;
                var edgeKey = flow.Role == SemanticFlowRole.Inflow
                    ? (referenceKey, stockKey)
                    : (stockKey, referenceKey);

                if (!grouped.TryGetValue(edgeKey, out var flows))
                {
                    flows = [];
                    grouped[edgeKey] = flows;
                }

                flows.Add(flow);
            }
        }

        return grouped
            .OrderBy(entry => $"{entry.Key.Source.Kind}.{entry.Key.Source.AttributeId.Value}")
            .ThenBy(entry => $"{entry.Key.Target.Kind}.{entry.Key.Target.AttributeId.Value}")
            .Select(entry => new SemanticProjectedAttributeEdge
            {
                SourceKey = entry.Key.Source,
                TargetKey = entry.Key.Target,
                SupportingFlows = entry.Value
                    .OrderBy(flow => flow.Verb.Id, StringComparer.Ordinal)
                    .ThenBy(flow => flow.Role)
                    .ToArray(),
            })
            .ToArray();
    }
}

public sealed class SemanticProjectedAttributeEdge
{
    public required DefinitionAttributeKey SourceKey { get; init; }
    public required DefinitionAttributeKey TargetKey { get; init; }
    public required IReadOnlyList<SemanticFlowEdge> SupportingFlows { get; init; }
}
