namespace Engine.Domain.Graph;

using Engine.Kinds;

public static class SemanticStockGraphProjection
{
    public static IReadOnlyList<SemanticProjectedAttributeEdge> ProjectAttributeEdges(
        SemanticFlowGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var stockByAttribute = graph.Stocks
            .Where(stock => stock.Id is AttributeStockId)
            .ToDictionary(stock => ((AttributeStockId)stock.Id).Key, stock => stock.Id);
        var stockByResource = graph.Stocks
            .Where(stock => stock.Id is ResourceStockId)
            .ToDictionary(stock => ((ResourceStockId)stock.Id).ResourceId, stock => stock.Id);

        var grouped = new Dictionary<(SemanticStockId Source, SemanticStockId Target), List<SemanticFlowEdge>>();

        foreach (var flow in graph.Flows)
        {
            foreach (var sourceRef in flow.SourceRefs)
            {
                var referenceStockId = TryResolveStockId(sourceRef, stockByAttribute, stockByResource);
                if (referenceStockId is null)
                {
                    continue;
                }

                var targetStockId = flow.TargetStock.Id;
                var edgeKey = flow.Role == SemanticFlowRole.Inflow
                    ? (referenceStockId, targetStockId)
                    : (targetStockId, referenceStockId);

                if (!grouped.TryGetValue(edgeKey, out var flows))
                {
                    flows = [];
                    grouped[edgeKey] = flows;
                }

                flows.Add(flow);
            }
        }

        return grouped
            .OrderBy(entry => entry.Key.Source.Label, StringComparer.Ordinal)
            .ThenBy(entry => entry.Key.Target.Label, StringComparer.Ordinal)
            .Select(entry =>
            {
                var supporting = entry.Value
                    .OrderBy(flow => flow.Verb.Id, StringComparer.Ordinal)
                    .ThenBy(flow => flow.Role)
                    .ToArray();
                return new SemanticProjectedAttributeEdge
                {
                    SourceId = entry.Key.Source,
                    TargetId = entry.Key.Target,
                    SupportingFlows = supporting,
                    IsGateOnly = supporting.All(flow => flow.IsGate),
                };
            })
            .ToArray();
    }

    private static SemanticStockId? TryResolveStockId(
        TypedDefinitionRef sourceRef,
        Dictionary<DefinitionAttributeKey, SemanticStockId> stockByAttribute,
        Dictionary<DefinitionResourceId, SemanticStockId> stockByResource)
    {
        switch (sourceRef.ValueId)
        {
            case AttributeValueId attributeValue when sourceRef.Kind is EntityKind kind:
                {
                    var key = new DefinitionAttributeKey(kind, attributeValue.AttributeId);
                    return stockByAttribute.TryGetValue(key, out var attributeStockId) ? attributeStockId : null;
                }

            case ResourceValueId resourceValue:
                return stockByResource.TryGetValue(resourceValue.ResourceId, out var resourceStockId) ? resourceStockId : null;

            default:
                return null;
        }
    }
}

public sealed class SemanticProjectedAttributeEdge
{
    public required SemanticStockId SourceId { get; init; }
    public required SemanticStockId TargetId { get; init; }
    public required IReadOnlyList<SemanticFlowEdge> SupportingFlows { get; init; }
    public bool IsGateOnly { get; init; }

    public DefinitionAttributeKey? SourceKey => (SourceId as AttributeStockId)?.Key;
    public DefinitionAttributeKey? TargetKey => (TargetId as AttributeStockId)?.Key;
}
