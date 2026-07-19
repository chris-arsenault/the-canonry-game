namespace Engine.WorldGen;

using Engine.Constants;

internal static class ConditionedFlowResolver
{
    public static int[] ResolveLandTargets(PhysicalGeographyField map)
    {
        ArgumentNullException.ThrowIfNull(map);
        var flatRouting = BuildFlatRouting(map);
        var targets = new int[map.Grid.TriangleCount];
        Array.Fill(targets, -1);
        for (var triangleIndex = 0; triangleIndex < targets.Length; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            targets[triangleIndex] = SelectTarget(map, flatRouting, triangleIndex);
        }

        return targets;
    }

    private static FlatRouting BuildFlatRouting(PhysicalGeographyField map)
    {
        var ranks = new int[map.Grid.TriangleCount];
        var outletX = new double[map.Grid.TriangleCount];
        var outletY = new double[map.Grid.TriangleCount];
        Array.Fill(ranks, -1);
        var queue = new Queue<int>();
        var order = new int[map.Grid.TriangleCount];
        SeedFlatOutlets(map, ranks, outletX, outletY, queue);
        var orderCount = RankFlatInterior(map, ranks, queue, order);
        AssignFlatOutletCentroids(map, ranks, outletX, outletY, order, orderCount);
        return new FlatRouting(ranks, outletX, outletY);
    }

    private static void SeedFlatOutlets(
        PhysicalGeographyField map,
        int[] ranks,
        double[] outletX,
        double[] outletY,
        Queue<int> queue)
    {
        for (var triangleIndex = 0; triangleIndex < ranks.Length; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean ||
                !HasLowerOutlet(map, triangleIndex))
            {
                continue;
            }

            ranks[triangleIndex] = 0;
            AssignLowerOutletCentroid(map, triangleIndex, outletX, outletY);
            queue.Enqueue(triangleIndex);
        }
    }

    private static int RankFlatInterior(
        PhysicalGeographyField map,
        int[] ranks,
        Queue<int> queue,
        int[] order)
    {
        var orderCount = 0;
        while (queue.TryDequeue(out var triangleIndex))
        {
            order[orderCount++] = triangleIndex;
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

        return orderCount;
    }

    private static void AssignFlatOutletCentroids(
        PhysicalGeographyField map,
        int[] ranks,
        double[] outletX,
        double[] outletY,
        int[] order,
        int orderCount)
    {
        for (var orderIndex = 0; orderIndex < orderCount; orderIndex++)
        {
            var triangleIndex = order[orderIndex];
            if (ranks[triangleIndex] > 0)
            {
                AssignInheritedOutletCentroid(map, ranks, outletX, outletY, triangleIndex);
            }
        }
    }

    private static void AssignLowerOutletCentroid(
        PhysicalGeographyField map,
        int triangleIndex,
        double[] outletX,
        double[] outletY)
    {
        var bestDrainage = long.MaxValue;
        var count = 0;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor < 0)
            {
                continue;
            }

            var drainage = map.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean
                ? long.MinValue
                : map.DrainageElevations[neighbor];
            if (drainage >= map.DrainageElevations[triangleIndex])
            {
                continue;
            }

            if (drainage < bestDrainage)
            {
                bestDrainage = drainage;
                outletX[triangleIndex] = 0d;
                outletY[triangleIndex] = 0d;
                count = 0;
            }

            if (drainage == bestDrainage)
            {
                outletX[triangleIndex] += map.Grid.TriangleX(neighbor);
                outletY[triangleIndex] += map.Grid.TriangleY(neighbor);
                count++;
            }
        }

        outletX[triangleIndex] /= Math.Max(1, count);
        outletY[triangleIndex] /= Math.Max(1, count);
    }

    private static void AssignInheritedOutletCentroid(
        PhysicalGeographyField map,
        int[] ranks,
        double[] outletX,
        double[] outletY,
        int triangleIndex)
    {
        var count = 0;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 && ranks[neighbor] >= 0 && ranks[neighbor] < ranks[triangleIndex] &&
                map.DrainageElevations[neighbor] == map.DrainageElevations[triangleIndex])
            {
                outletX[triangleIndex] += outletX[neighbor];
                outletY[triangleIndex] += outletY[neighbor];
                count++;
            }
        }

        if (count == 0)
        {
            throw new InvalidOperationException("Conditioned flat lacks an outlet gradient.");
        }

        outletX[triangleIndex] /= count;
        outletY[triangleIndex] /= count;
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

    private static int SelectTarget(
        PhysicalGeographyField map,
        FlatRouting flatRouting,
        int triangleIndex)
    {
        Span<GeometricFlowCandidate> candidates = stackalloc GeometricFlowCandidate[3];
        var candidateCount = 0;
        var currentDrainage = map.DrainageElevations[triangleIndex];
        var currentRank = flatRouting.Ranks[triangleIndex];
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor < 0 || !TryCreateCandidate(
                    map,
                    flatRouting.Ranks,
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
            flatRouting.OutletX[triangleIndex] - map.Grid.TriangleX(triangleIndex),
            flatRouting.OutletY[triangleIndex] - map.Grid.TriangleY(triangleIndex),
            map.CrustMotionX[triangleIndex],
            map.CrustMotionY[triangleIndex]);
    }

    private static bool TryCreateCandidate(
        PhysicalGeographyField map,
        int[] flatRanks,
        int neighbor,
        long currentDrainage,
        int currentRank,
        out long drainage,
        out int rank)
    {
        drainage = map.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean
            ? long.MinValue
            : map.DrainageElevations[neighbor];
        rank = flatRanks[neighbor];
        var descends = drainage < currentDrainage;
        var resolvesFlat = drainage == currentDrainage &&
            rank >= 0 && currentRank >= 0 && rank < currentRank;
        return descends || resolvesFlat;
    }

    internal static int SelectByGeometry(
        ReadOnlySpan<GeometricFlowCandidate> candidates,
        double outletX,
        double outletY,
        double motionX,
        double motionY)
    {
        if (candidates.IsEmpty)
        {
            throw new ArgumentException("Flow selection requires at least one candidate.", nameof(candidates));
        }

        var (bestDrainage, bestRank) = FindPrimaryPreference(candidates);
        Normalize(outletX, outletY, out var desiredX, out var desiredY);
        var best = candidates[0];
        var hasBest = false;
        var bestOutlet = double.MinValue;
        var bestMotion = double.MinValue;
        var bestTurn = double.MinValue;
        foreach (var candidate in candidates)
        {
            if (candidate.Drainage != bestDrainage || candidate.Rank != bestRank)
            {
                continue;
            }

            Normalize(candidate.DirectionX, candidate.DirectionY, out var x, out var y);
            var outletScore = (x * desiredX) + (y * desiredY);
            var motionScore = (x * motionX) + (y * motionY);
            var turnScore = (desiredX * y) - (desiredY * x);
            if (IsBetterScore(
                hasBest,
                outletScore,
                motionScore,
                turnScore,
                bestOutlet,
                bestMotion,
                bestTurn))
            {
                best = candidate;
                hasBest = true;
                bestOutlet = outletScore;
                bestMotion = motionScore;
                bestTurn = turnScore;
            }
        }

        return best.Value;
    }

    private static (long Drainage, int Rank) FindPrimaryPreference(
        ReadOnlySpan<GeometricFlowCandidate> candidates)
    {
        var bestDrainage = long.MaxValue;
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

    private static bool IsBetterScore(
        bool hasBest,
        double outlet,
        double motion,
        double turn,
        double bestOutlet,
        double bestMotion,
        double bestTurn) =>
        !hasBest || outlet > bestOutlet ||
        (NearlyEqual(outlet, bestOutlet) && motion > bestMotion) ||
        (NearlyEqual(outlet, bestOutlet) && NearlyEqual(motion, bestMotion) && turn > bestTurn);

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
        long Drainage,
        int Rank,
        double DirectionX,
        double DirectionY);

    private sealed record FlatRouting(int[] Ranks, double[] OutletX, double[] OutletY);
}
