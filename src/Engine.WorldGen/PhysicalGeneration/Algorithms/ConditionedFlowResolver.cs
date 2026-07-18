namespace Engine.WorldGen;

using Engine.Constants;

internal static class ConditionedFlowResolver
{
    public static int[] ResolveLandTargets(PhysicalGeographyField map)
    {
        ArgumentNullException.ThrowIfNull(map);
        var flatRanks = BuildFlatRanks(map);
        var targets = new int[map.Grid.TriangleCount];
        Array.Fill(targets, -1);
        for (var triangleIndex = 0; triangleIndex < targets.Length; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            targets[triangleIndex] = SelectTarget(map, flatRanks, triangleIndex);
        }

        return targets;
    }

    private static int[] BuildFlatRanks(PhysicalGeographyField map)
    {
        var ranks = new int[map.Grid.TriangleCount];
        Array.Fill(ranks, -1);
        var queue = new Queue<int>();
        for (var triangleIndex = 0; triangleIndex < ranks.Length; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean ||
                !HasLowerOutlet(map, triangleIndex))
            {
                continue;
            }

            ranks[triangleIndex] = 0;
            queue.Enqueue(triangleIndex);
        }

        while (queue.TryDequeue(out var triangleIndex))
        {
            var nextRank = ranks[triangleIndex] + 1;
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0 && ranks[neighbor] < 0 &&
                    map.WaterKindCodes[neighbor] != PhysicalWaterKindCodes.Ocean &&
                    map.DrainageElevations[neighbor] == map.DrainageElevations[triangleIndex])
                {
                    ranks[neighbor] = nextRank;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return ranks;
    }

    private static bool HasLowerOutlet(PhysicalGeographyField map, int triangleIndex)
    {
        var elevation = map.DrainageElevations[triangleIndex];
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 &&
                (map.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean ||
                    map.DrainageElevations[neighbor] < elevation))
            {
                return true;
            }
        }

        return false;
    }

    private static int SelectTarget(PhysicalGeographyField map, int[] flatRanks, int triangleIndex)
    {
        var best = FlowCandidate.None;
        var currentDrainage = map.DrainageElevations[triangleIndex];
        var currentRank = flatRanks[triangleIndex];
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor < 0 || !TryCreateCandidate(
                    map,
                    flatRanks,
                    neighbor,
                    currentDrainage,
                    currentRank,
                    out var candidate))
            {
                continue;
            }

            if (candidate.IsPreferredTo(best))
            {
                best = candidate;
            }
        }

        if (best.Index < 0)
        {
            throw new InvalidOperationException($"Conditioned triangle {triangleIndex} has no outlet.");
        }

        return best.Index;
    }

    private static bool TryCreateCandidate(
        PhysicalGeographyField map,
        int[] flatRanks,
        int neighbor,
        int currentDrainage,
        int currentRank,
        out FlowCandidate candidate)
    {
        var drainage = map.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean
            ? int.MinValue
            : map.DrainageElevations[neighbor];
        var rank = flatRanks[neighbor];
        var descends = drainage < currentDrainage;
        var resolvesFlat = drainage == currentDrainage &&
            rank >= 0 && currentRank >= 0 && rank < currentRank;
        candidate = new FlowCandidate(neighbor, drainage, rank);
        return descends || resolvesFlat;
    }

    private readonly record struct FlowCandidate(int Index, int Drainage, int Rank)
    {
        public static FlowCandidate None { get; } = new(-1, int.MaxValue, int.MaxValue);

        public bool IsPreferredTo(FlowCandidate other) =>
            Drainage < other.Drainage ||
            (Drainage == other.Drainage && Rank < other.Rank) ||
            (Drainage == other.Drainage && Rank == other.Rank && Index < other.Index);
    }
}
