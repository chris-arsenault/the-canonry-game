namespace Engine.WorldGen;

using Engine.Constants;

internal static class ConditionedFlowResolver
{
    private const double ScoreEpsilon = 0.0000001d;

    public static int[] ResolveLandTargets(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(profile);
        var targets = new int[map.Grid.TriangleCount];
        Array.Fill(targets, -1);
        for (var triangleIndex = 0; triangleIndex < targets.Length; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
            {
                targets[triangleIndex] = SelectTarget(map, profile, seed, triangleIndex);
            }
        }

        return targets;
    }

    internal static bool IsLegalTarget(
        PhysicalGeographyField map,
        int source,
        int target)
    {
        if (target < 0 || map.Grid.EdgeLeadingTo(source, target) < 0)
        {
            return false;
        }

        return IsLegalAdjacentTarget(map, source, target);
    }

    private static bool IsLegalAdjacentTarget(
        PhysicalGeographyField map,
        int source,
        int target)
    {
        if (map.WaterKindCodes[target] == PhysicalWaterKindCodes.Ocean)
        {
            return true;
        }

        var sourceDrainage = map.DrainageElevations[source];
        var targetDrainage = map.DrainageElevations[target];
        return targetDrainage < sourceDrainage ||
            targetDrainage == sourceDrainage &&
                map.DrainageRanks[target] < map.DrainageRanks[source];
    }

    internal static int SelectByOutletAlignment(
        PhysicalGeographyField map,
        ReadOnlySpan<int> candidates,
        int source,
        int outlet,
        int seed)
    {
        if (candidates.IsEmpty)
        {
            throw new ArgumentException("Flow selection requires at least one candidate.", nameof(candidates));
        }

        DesiredDirection(map, source, outlet, out var desiredX, out var desiredY);
        var best = -1;
        var bestAlignment = double.MinValue;
        var bestTie = double.MinValue;
        foreach (var candidate in candidates)
        {
            var alignment = DirectionAlignment(map, source, candidate, desiredX, desiredY);
            var tie = CoordinateTie(map, source, candidate, seed);
            if (alignment > bestAlignment + ScoreEpsilon ||
                NearlyEqual(alignment, bestAlignment) && tie > bestTie)
            {
                best = candidate;
                bestAlignment = alignment;
                bestTie = tie;
            }
        }

        return best;
    }

    private static int SelectTarget(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int seed,
        int source)
    {
        var outlet = ParentLookahead(map, source, profile.DrainageGuidanceLookaheadSteps);
        DesiredDirection(map, source, outlet, out var desiredX, out var desiredY);
        var best = -1;
        var bestScore = double.MinValue;
        var bestTie = double.MinValue;
        for (var edge = 0; edge < 3; edge++)
        {
            var target = map.Grid.NeighborTriangleIndex(source, edge);
            if (target < 0 || !IsLegalAdjacentTarget(map, source, target))
            {
                continue;
            }

            var alignment = (map.Grid.TriangleEdgeUnitX(source, edge) * desiredX) +
                (map.Grid.TriangleEdgeUnitY(source, edge) * desiredY);
            var score = CandidateScore(map, profile, source, target, alignment);
            if (score > bestScore + ScoreEpsilon)
            {
                best = target;
                bestScore = score;
                bestTie = CoordinateTie(map, source, target, seed);
            }
            else if (NearlyEqual(score, bestScore))
            {
                var tie = CoordinateTie(map, source, target, seed);
                if (tie > bestTie)
                {
                    best = target;
                    bestTie = tie;
                }
            }
        }

        return best >= 0
            ? best
            : throw new InvalidOperationException($"Conditioned triangle {source} has no outlet.");
    }

    private static double CandidateScore(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int source,
        int target,
        double alignment)
    {
        var inheritedValley = InheritedValley(map, target);
        if (map.WaterKindCodes[target] == PhysicalWaterKindCodes.Ocean)
        {
            return 10_000d + alignment;
        }

        var conditionedDrop = (map.DrainageElevations[source] - map.DrainageElevations[target]) /
            (double)PhysicalElevation.SubunitsPerUnit;
        if (conditionedDrop <= 0d)
        {
            var rankProgress = Math.Clamp(
                map.DrainageRanks[source] - map.DrainageRanks[target],
                0,
                20);
            return (alignment * profile.DrainageOutletAlignmentWeight / 10d) +
                (rankProgress * 0.15d) +
                (inheritedValley * profile.DrainageInheritedValleyWeight / 1000d);
        }

        var floorDrop = Math.Clamp(
            (map.Elevation[source] - map.Elevation[target]) /
                (double)PhysicalElevation.SubunitsPerUnit,
            -50d,
            200d);
        return (floorDrop * profile.DrainageFloorGradientWeight / 1000d) +
            (Math.Min(200d, conditionedDrop) *
                profile.DrainageConditionedGradientWeight / 1000d) +
            (alignment * profile.DrainageOutletAlignmentWeight / 20d) +
            (inheritedValley * profile.DrainageInheritedValleyWeight / 1000d);
    }

    private static double InheritedValley(PhysicalGeographyField map, int triangleIndex) =>
        Math.Clamp(
            PhysicalElevation.ToUnits(
                map.CumulativeFluvialErosion[triangleIndex] +
                map.CumulativeGlacialErosion[triangleIndex]),
            0,
            100);

    private static int ParentLookahead(
        PhysicalGeographyField map,
        int source,
        int maximumSteps)
    {
        var current = source;
        for (var step = 0; step < maximumSteps; step++)
        {
            var parent = map.DrainageParents[current];
            if (parent < 0)
            {
                break;
            }

            current = parent;
        }

        return current;
    }

    private static void DesiredDirection(
        PhysicalGeographyField map,
        int source,
        int target,
        out double desiredX,
        out double desiredY) =>
        Normalize(
            map.Grid.TriangleX(target) - map.Grid.TriangleX(source),
            map.Grid.TriangleY(target) - map.Grid.TriangleY(source),
            out desiredX,
            out desiredY);

    private static double DirectionAlignment(
        PhysicalGeographyField map,
        int source,
        int target,
        double desiredX,
        double desiredY)
    {
        Normalize(
            map.Grid.TriangleX(target) - map.Grid.TriangleX(source),
            map.Grid.TriangleY(target) - map.Grid.TriangleY(source),
            out var directionX,
            out var directionY);
        return (directionX * desiredX) + (directionY * desiredY);
    }

    private static double CoordinateTie(
        PhysicalGeographyField map,
        int source,
        int target,
        int seed)
    {
        var sourceX = QuantizeCoordinate(map.Grid.TriangleX(source));
        var sourceY = QuantizeCoordinate(map.Grid.TriangleY(source));
        var targetX = QuantizeCoordinate(map.Grid.TriangleX(target));
        var targetY = QuantizeCoordinate(map.Grid.TriangleY(target));
        return PhysicalNoise.HashUnit(
            seed ^ sourceX,
            sourceY ^ targetX,
            targetY);
    }

    private static int QuantizeCoordinate(float value) => checked((int)Math.Round(
        value * 4096d,
        MidpointRounding.AwayFromZero));

    private static void Normalize(double x, double y, out double normalizedX, out double normalizedY)
    {
        var length = Math.Max(0.000001d, Math.Sqrt((x * x) + (y * y)));
        normalizedX = x / length;
        normalizedY = y / length;
    }

    private static bool NearlyEqual(double first, double second) =>
        Math.Abs(first - second) < ScoreEpsilon;
}
