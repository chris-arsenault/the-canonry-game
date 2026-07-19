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
        Span<GeometricFlowCandidate> candidates = stackalloc GeometricFlowCandidate[3];
        var candidateCount = 0;
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
                    out var drainage,
                    out var rank))
            {
                continue;
            }

            candidates[candidateCount++] = new GeometricFlowCandidate(
                neighbor,
                drainage,
                rank,
                map.Grid.TriangleX(neighbor) - map.Grid.TriangleX(triangleIndex),
                map.Grid.TriangleY(neighbor) - map.Grid.TriangleY(triangleIndex));
        }

        if (candidateCount == 0)
        {
            throw new InvalidOperationException($"Conditioned triangle {triangleIndex} has no outlet.");
        }

        return SelectByGeometry(
            candidates[..candidateCount],
            map.Grid.TriangleX(triangleIndex),
            map.Grid.TriangleY(triangleIndex),
            map.CrustMotionX[triangleIndex],
            map.CrustMotionY[triangleIndex]);
    }

    private static bool TryCreateCandidate(
        PhysicalGeographyField map,
        int[] flatRanks,
        int neighbor,
        int currentDrainage,
        int currentRank,
        out int drainage,
        out int rank)
    {
        drainage = map.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean
            ? int.MinValue
            : map.DrainageElevations[neighbor];
        rank = flatRanks[neighbor];
        var descends = drainage < currentDrainage;
        var resolvesFlat = drainage == currentDrainage &&
            rank >= 0 && currentRank >= 0 && rank < currentRank;
        return descends || resolvesFlat;
    }

    internal static int SelectByGeometry(
        ReadOnlySpan<GeometricFlowCandidate> candidates,
        double currentX,
        double currentY,
        double motionX,
        double motionY)
    {
        if (candidates.IsEmpty)
        {
            throw new ArgumentException("Flow selection requires at least one candidate.", nameof(candidates));
        }

        var (bestDrainage, bestRank) = FindPrimaryPreference(candidates);
        var (outletX, outletY) = SumOutletDirection(candidates, bestDrainage, bestRank);

        var tangentX = -currentY;
        var tangentY = currentX;
        var best = candidates[0];
        var hasBest = false;
        var bestOutlet = double.MinValue;
        var bestRadial = double.MinValue;
        var bestMotion = double.MinValue;
        foreach (var candidate in candidates)
        {
            if (candidate.Drainage != bestDrainage || candidate.Rank != bestRank)
            {
                continue;
            }

            Normalize(candidate.DirectionX, candidate.DirectionY, out var x, out var y);
            var outletScore = (x * outletX) + (y * outletY);
            var radialScore = (x * tangentX) + (y * tangentY);
            var motionScore = (x * motionX) + (y * motionY);
            if (IsBetterScore(
                hasBest,
                outletScore,
                radialScore,
                motionScore,
                bestOutlet,
                bestRadial,
                bestMotion))
            {
                best = candidate;
                hasBest = true;
                bestOutlet = outletScore;
                bestRadial = radialScore;
                bestMotion = motionScore;
            }
        }

        return best.Value;
    }

    private static (int Drainage, int Rank) FindPrimaryPreference(
        ReadOnlySpan<GeometricFlowCandidate> candidates)
    {
        var bestDrainage = int.MaxValue;
        var bestRank = int.MaxValue;
        foreach (var candidate in candidates)
        {
            if (candidate.Drainage < bestDrainage ||
                (candidate.Drainage == bestDrainage && candidate.Rank < bestRank))
            {
                bestDrainage = candidate.Drainage;
                bestRank = candidate.Rank;
            }
        }

        return (bestDrainage, bestRank);
    }

    private static (double X, double Y) SumOutletDirection(
        ReadOnlySpan<GeometricFlowCandidate> candidates,
        int drainage,
        int rank)
    {
        var outletX = 0d;
        var outletY = 0d;
        foreach (var candidate in candidates)
        {
            if (candidate.Drainage != drainage || candidate.Rank != rank)
            {
                continue;
            }

            Normalize(candidate.DirectionX, candidate.DirectionY, out var x, out var y);
            outletX += x;
            outletY += y;
        }

        return (outletX, outletY);
    }

    private static bool IsBetterScore(
        bool hasBest,
        double outlet,
        double radial,
        double motion,
        double bestOutlet,
        double bestRadial,
        double bestMotion) =>
        !hasBest || outlet > bestOutlet ||
        (NearlyEqual(outlet, bestOutlet) && radial > bestRadial) ||
        (NearlyEqual(outlet, bestOutlet) && NearlyEqual(radial, bestRadial) && motion > bestMotion);

    private static void Normalize(double x, double y, out double normalizedX, out double normalizedY)
    {
        var length = Math.Max(0.000001d, Math.Sqrt((x * x) + (y * y)));
        normalizedX = x / length;
        normalizedY = y / length;
    }

    private static bool NearlyEqual(double first, double second) =>
        Math.Abs(first - second) < 0.0000001d;

    internal readonly record struct GeometricFlowCandidate(
        int Value,
        int Drainage,
        int Rank,
        double DirectionX,
        double DirectionY);
}
