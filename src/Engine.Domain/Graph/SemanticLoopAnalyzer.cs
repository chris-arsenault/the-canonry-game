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
            .OrderBy(stock => stock.Id.Label, StringComparer.Ordinal)
            .ToList();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var loops = new List<SemanticClosedLoop>();

        foreach (var start in stocks)
        {
            var path = new List<SemanticLoopEdge>();
            var visited = new HashSet<SemanticStockId> { start.Id };
            Explore(start.Id, start.Id, adjacencies, visited, path, seen, loops, minDistinctKinds, maxLength);
        }

        return loops;
    }

    private static Dictionary<SemanticStockId, List<SemanticLoopEdge>> BuildAdjacencies(
        SemanticFlowGraph graph)
    {
        var grouped = new Dictionary<(SemanticStockId Source, SemanticStockId Target), List<SemanticFlowEdge>>();

        foreach (var edge in SemanticStockGraphProjection.ProjectAttributeEdges(graph))
        {
            grouped[(edge.SourceId, edge.TargetId)] = [.. edge.SupportingFlows];
        }

        var adjacency = new Dictionary<SemanticStockId, List<SemanticLoopEdge>>();
        foreach (var ((sourceId, targetId), flows) in grouped)
        {
            var edge = new SemanticLoopEdge
            {
                SourceId = sourceId,
                TargetId = targetId,
                SupportingFlows = flows
                    .OrderBy(flow => flow.Verb.Id, StringComparer.Ordinal)
                    .ToArray(),
            };

            if (!adjacency.TryGetValue(sourceId, out var edges))
            {
                edges = [];
                adjacency[sourceId] = edges;
            }

            edges.Add(edge);
        }

        foreach (var (_, edges) in adjacency)
        {
            edges.Sort((left, right) => string.CompareOrdinal(left.TargetId.Label, right.TargetId.Label));
        }

        return adjacency;
    }

    private static void Explore(
        SemanticStockId start,
        SemanticStockId current,
        IReadOnlyDictionary<SemanticStockId, List<SemanticLoopEdge>> adjacencies,
        HashSet<SemanticStockId> visited,
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
            if (edge.TargetId == start)
            {
                AddClosedLoop(start, edge, path, seen, loops, minDistinctKinds);
                continue;
            }

            ExploreEdge(start, edge, adjacencies, visited, path, seen, loops, minDistinctKinds, maxLength);
        }
    }

    private static void AddClosedLoop(
        SemanticStockId start,
        SemanticLoopEdge edge,
        List<SemanticLoopEdge> path,
        HashSet<string> seen,
        List<SemanticClosedLoop> loops,
        int minDistinctKinds)
    {
        if (path.Count + 1 < 2)
        {
            return;
        }

        var cycleEdges = path.Append(edge).ToArray();
        var kindCount = DistinctKindCount(start, cycleEdges);
        var signature = Canonicalize(cycleEdges);
        if (kindCount >= minDistinctKinds && seen.Add(signature))
        {
            loops.Add(new SemanticClosedLoop
            {
                Signature = signature,
                DistinctKinds = kindCount,
                Edges = cycleEdges,
            });
        }
    }

    private static void ExploreEdge(
        SemanticStockId start,
        SemanticLoopEdge edge,
        IReadOnlyDictionary<SemanticStockId, List<SemanticLoopEdge>> adjacencies,
        HashSet<SemanticStockId> visited,
        List<SemanticLoopEdge> path,
        HashSet<string> seen,
        List<SemanticClosedLoop> loops,
        int minDistinctKinds,
        int maxLength)
    {
        if (path.Count + 1 >= maxLength || !visited.Add(edge.TargetId))
        {
            return;
        }

        path.Add(edge);
        Explore(start, edge.TargetId, adjacencies, visited, path, seen, loops, minDistinctKinds, maxLength);
        path.RemoveAt(path.Count - 1);
        visited.Remove(edge.TargetId);
    }

    private static int DistinctKindCount(SemanticStockId start, IReadOnlyList<SemanticLoopEdge> cycleEdges) =>
        cycleEdges
            .Select(loopEdge => DistinctKindOf(loopEdge.SourceId))
            .Append(DistinctKindOf(start))
            .Distinct(StringComparer.Ordinal)
            .Count();

    private static string Canonicalize(IReadOnlyList<SemanticLoopEdge> cycleEdges)
    {
        var nodes = cycleEdges
            .Select(edge => edge.SourceId.Label)
            .ToArray();

        var rotations = Enumerable.Range(0, nodes.Length)
            .Select(offset =>
                string.Join(" -> ",
                    Enumerable.Range(0, nodes.Length)
                        .Select(index => nodes[(index + offset) % nodes.Length])))
            .ToArray();

        return rotations.Min(StringComparer.Ordinal)!;
    }

    private static string DistinctKindOf(SemanticStockId id) => id switch
    {
        AttributeStockId attribute => attribute.Key.Kind.ToString(),
        ResourceStockId => "hex",
        _ => id.Label,
    };
}

public sealed class SemanticClosedLoop
{
    public required string Signature { get; init; }
    public required int DistinctKinds { get; init; }
    public required IReadOnlyList<SemanticLoopEdge> Edges { get; init; }
}

public sealed class SemanticLoopEdge
{
    public required SemanticStockId SourceId { get; init; }
    public required SemanticStockId TargetId { get; init; }
    public required IReadOnlyList<SemanticFlowEdge> SupportingFlows { get; init; }

    public DefinitionAttributeKey? SourceKey => (SourceId as AttributeStockId)?.Key;
    public DefinitionAttributeKey? TargetKey => (TargetId as AttributeStockId)?.Key;
}
