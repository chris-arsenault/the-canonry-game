namespace Engine.Domain.Graph;

using Engine.Kinds;

public static class SemanticLoopAnalyzer
{
    public static IReadOnlyList<SemanticClosedLoop> FindClosedLoops(
        DefinitionGraph graph,
        int minDistinctKinds = 2,
        int maxLength = 8)
    {
        ArgumentNullException.ThrowIfNull(graph);

        ArgumentOutOfRangeException.ThrowIfLessThan(minDistinctKinds, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 2);

        var adjacencies = BuildAdjacencies(graph.SemanticFlows);
        var stocks = graph.SemanticFlows.Stocks
            .OrderBy(stock => StockLabel(stock))
            .ToList();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var loops = new List<SemanticClosedLoop>();

        foreach (var start in stocks)
        {
            var path = new List<SemanticLoopEdge>();
            var visited = new HashSet<DefinitionAttributeKey> { start.AttributeKey };
            Explore(start.AttributeKey, start.AttributeKey, adjacencies, visited, path, seen, loops, minDistinctKinds, maxLength);
        }

        return loops;
    }

    private static Dictionary<DefinitionAttributeKey, List<SemanticLoopEdge>> BuildAdjacencies(
        SemanticFlowGraph graph)
    {
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

                var sourceKey = new DefinitionAttributeKey(kind, attributeId);
                var targetKey = flow.TargetStock.AttributeKey;
                var key = (sourceKey, targetKey);

                if (!grouped.TryGetValue(key, out var flows))
                {
                    flows = [];
                    grouped[key] = flows;
                }

                flows.Add(flow);
            }
        }

        var adjacency = new Dictionary<DefinitionAttributeKey, List<SemanticLoopEdge>>();
        foreach (var ((sourceKey, targetKey), flows) in grouped)
        {
            var edge = new SemanticLoopEdge
            {
                SourceKey = sourceKey,
                TargetKey = targetKey,
                SupportingFlows = flows
                    .OrderBy(flow => flow.Verb.Id, StringComparer.Ordinal)
                    .ToArray(),
            };

            if (!adjacency.TryGetValue(sourceKey, out var edges))
            {
                edges = [];
                adjacency[sourceKey] = edges;
            }

            edges.Add(edge);
        }

        foreach (var (_, edges) in adjacency)
        {
            edges.Sort((left, right) => string.CompareOrdinal(
                $"{left.TargetKey.Kind}.{left.TargetKey.AttributeId.Value}",
                $"{right.TargetKey.Kind}.{right.TargetKey.AttributeId.Value}"));
        }

        return adjacency;
    }

    private static void Explore(
        DefinitionAttributeKey start,
        DefinitionAttributeKey current,
        IReadOnlyDictionary<DefinitionAttributeKey, List<SemanticLoopEdge>> adjacencies,
        HashSet<DefinitionAttributeKey> visited,
        List<SemanticLoopEdge> path,
        HashSet<string> seen,
        List<SemanticClosedLoop> loops,
        int minDistinctKinds,
        int maxLength)
    {
        if (!adjacencies.TryGetValue(current, out var nextEdges))
        {
            return;
        }

        foreach (var edge in nextEdges)
        {
            if (edge.TargetKey == start)
            {
                if (path.Count + 1 < 2)
                {
                    continue;
                }

                var cycleEdges = path.Append(edge).ToArray();
                var kindCount = cycleEdges
                    .Select(loopEdge => loopEdge.SourceKey.Kind)
                    .Append(start.Kind)
                    .Distinct()
                    .Count();

                if (kindCount < minDistinctKinds)
                {
                    continue;
                }

                var signature = Canonicalize(cycleEdges);
                if (seen.Add(signature))
                {
                    loops.Add(new SemanticClosedLoop
                    {
                        Signature = signature,
                        DistinctKinds = kindCount,
                        Edges = cycleEdges,
                    });
                }

                continue;
            }

            if (path.Count + 1 >= maxLength || !visited.Add(edge.TargetKey))
            {
                continue;
            }

            path.Add(edge);
            Explore(start, edge.TargetKey, adjacencies, visited, path, seen, loops, minDistinctKinds, maxLength);
            path.RemoveAt(path.Count - 1);
            visited.Remove(edge.TargetKey);
        }
    }

    private static string Canonicalize(IReadOnlyList<SemanticLoopEdge> cycleEdges)
    {
        var nodes = cycleEdges
            .Select(edge => $"{edge.SourceKey.Kind}.{edge.SourceKey.AttributeId.Value}")
            .ToArray();

        var rotations = Enumerable.Range(0, nodes.Length)
            .Select(offset =>
                string.Join(" -> ",
                    Enumerable.Range(0, nodes.Length)
                        .Select(index => nodes[(index + offset) % nodes.Length])))
            .ToArray();

        return rotations.Min(StringComparer.Ordinal)!;
    }

    private static string StockLabel(SemanticStockNode stock) =>
        $"{stock.AttributeKey.Kind}.{stock.AttributeKey.AttributeId.Value}";
}

public sealed class SemanticClosedLoop
{
    public required string Signature { get; init; }
    public required int DistinctKinds { get; init; }
    public required IReadOnlyList<SemanticLoopEdge> Edges { get; init; }
}

public sealed class SemanticLoopEdge
{
    public required DefinitionAttributeKey SourceKey { get; init; }
    public required DefinitionAttributeKey TargetKey { get; init; }
    public required IReadOnlyList<SemanticFlowEdge> SupportingFlows { get; init; }
}
