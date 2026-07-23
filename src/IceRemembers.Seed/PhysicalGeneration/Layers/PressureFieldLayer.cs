namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.WorldGen;

internal sealed class PressureFieldLayer : IIceRemembersMicrogeographyLayer
{
    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "pressure-field",
        0x5a61b9,
        IceRemembersMicrogeographyFacts.AuroraField,
        IceRemembersMicrogeographyFacts.PressureField,
        IceRemembersMicrogeographyFacts.PressureFeatures |
        IceRemembersMicrogeographyFacts.PressureCoastalFeedback |
        IceRemembersMicrogeographyFacts.Interactions |
        IceRemembersMicrogeographyFacts.FinalSurfaces |
        IceRemembersMicrogeographyFacts.Validated);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        Array.Clear(map.PressureIntensity);
        Array.Clear(map.PressureWake);
        Array.Clear(map.RogueImpact);
        map.DeadGodTriangleIndex = SelectDeadGodLocus(map.Physical);
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            map.Physical.FeatureFlags[triangleIndex] &= ~PhysicalGeographyFeatureCodes.DeadGodLocus;
        }

        map.Physical.FeatureFlags[map.DeadGodTriangleIndex] |= PhysicalGeographyFeatureCodes.DeadGodLocus;
        if (context.PressureStrength == 0)
        {
            return;
        }

        for (var ray = 0; ray < context.Profile.StandingPressureRayCount; ray++)
        {
            var angle = ray / (double)context.Profile.StandingPressureRayCount * Math.PI * 2d;
            PropagatePressureRay(
                map.Physical,
                map.PressureIntensity,
                map.DeadGodTriangleIndex,
                angle,
                context.PressureStrength,
                440,
                context.Seed ^ ray);
        }

        PropagateCoastalImpacts(map, context);
        PropagateAncientWakes(map, context);
    }

    private static void PropagateCoastalImpacts(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        var coastSources = SelectOceanSources(
            map.Physical,
            context.Seed,
            context.Profile.CoastalPressureSourceCount,
            requireCoast: true);
        for (var sourceIndex = 0; sourceIndex < coastSources.Length; sourceIndex++)
        {
            var source = coastSources[sourceIndex];
            map.RogueImpact[source] = true;
            for (var ray = 0; ray < 24; ray++)
            {
                var angle = ray / 24d * Math.PI * 2d;
                PropagatePressureRay(
                    map.Physical,
                    map.PressureIntensity,
                    source,
                    angle,
                    620 * context.PressureStrength / 1000,
                    130,
                    context.Seed ^ (sourceIndex * 397) ^ ray);
            }
        }
    }

    private static void PropagateAncientWakes(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        var wakeSources = SelectOceanSources(
            map.Physical,
            context.Seed ^ 0x3359,
            context.Profile.AncientWakeSourceCount,
            requireCoast: false);
        for (var wakeIndex = 0; wakeIndex < wakeSources.Length; wakeIndex++)
        {
            TraceAncientWake(
                map,
                wakeSources[wakeIndex],
                context.Seed ^ (wakeIndex * 7919),
                context.PressureStrength);
        }
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = context;
        if (map.DeadGodTriangleIndex < 0 ||
            map.Physical.WaterKindCodes[map.DeadGodTriangleIndex] != PhysicalWaterKindCodes.Ocean)
        {
            throw new InvalidOperationException("Pressure generation lacks its deep-ocean locus.");
        }

        var locusCount = 0;
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            if (map.PressureIntensity[triangleIndex] is < 0 or > 1000)
            {
                throw new InvalidOperationException("Pressure intensity is outside its scalar range.");
            }

            locusCount += (map.Physical.FeatureFlags[triangleIndex] &
                PhysicalGeographyFeatureCodes.DeadGodLocus) != 0 ? 1 : 0;
        }

        if (locusCount != 1)
        {
            throw new InvalidOperationException("Pressure generation must have exactly one deep-ocean locus.");
        }
    }

    private static int SelectDeadGodLocus(PhysicalGeographyField field)
    {
        var best = -1;
        var bestScore = long.MinValue;
        for (var triangleIndex = 0; triangleIndex < field.Grid.ContentTriangleCount; triangleIndex++)
        {
            if (field.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            var score = field.WaterDepths[triangleIndex] + LocalBasinDepth(field, triangleIndex);
            if (score > bestScore)
            {
                best = triangleIndex;
                bestScore = score;
            }
        }

        return best >= 0
            ? best
            : throw new InvalidOperationException("Pressure magic requires a deep ocean locus.");
    }

    private static long LocalBasinDepth(PhysicalGeographyField field, int triangleIndex)
    {
        var depth = field.WaterDepths[triangleIndex];
        var total = 0L;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0)
            {
                total += Math.Max(0, depth - field.WaterDepths[neighbor]);
            }
        }

        return total;
    }

    private static void PropagatePressureRay(
        PhysicalGeographyField field,
        int[] pressure,
        int source,
        double angle,
        int energy,
        int maximumSteps,
        int seed)
    {
        var current = source;
        var previous = -1;
        var bounces = 0;
        for (var step = 0; step < maximumSteps && energy > 0; step++)
        {
            pressure[current] = Math.Min(1000, pressure[current] + Math.Max(1, energy / 36));
            var next = BestOceanNeighbor(field, current, previous, angle);
            if (next < 0)
            {
                bounces++;
                if (bounces > 4)
                {
                    break;
                }

                angle += Math.PI *
                    (0.58d + (PhysicalNoise.HashUnit(seed, current, bounces) * 0.62d));
                previous = -1;
                energy -= 90;
                continue;
            }

            var dx = field.Grid.TriangleX(next) - field.Grid.TriangleX(current);
            var dy = field.Grid.TriangleY(next) - field.Grid.TriangleY(current);
            angle = Math.Atan2(dy, dx);
            previous = current;
            current = next;
            energy -= 3 + (bounces * 2);
        }
    }

    private static int BestOceanNeighbor(
        PhysicalGeographyField field,
        int current,
        int previous,
        double angle)
    {
        var directionX = Math.Cos(angle);
        var directionY = Math.Sin(angle);
        var best = -1;
        var bestAlignment = -2d;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(current, edge);
            if (neighbor < 0 || neighbor == previous ||
                field.WaterKindCodes[neighbor] != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            var dx = field.Grid.TriangleX(neighbor) - field.Grid.TriangleX(current);
            var dy = field.Grid.TriangleY(neighbor) - field.Grid.TriangleY(current);
            var length = Math.Sqrt((dx * dx) + (dy * dy));
            var alignment = length <= double.Epsilon
                ? -2d
                : ((dx / length) * directionX) + ((dy / length) * directionY);
            if (alignment > bestAlignment)
            {
                best = neighbor;
                bestAlignment = alignment;
            }
        }

        return bestAlignment >= -0.15d ? best : -1;
    }

    private static int[] SelectOceanSources(
        PhysicalGeographyField field,
        int seed,
        int count,
        bool requireCoast)
    {
        var candidates = new PriorityQueue<CoreCandidate, (long Score, int TriangleIndex)>();
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (!field.Grid.TriangleIsInsideContentDomain(triangleIndex) ||
                field.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean ||
                (requireCoast && !HasDryNeighbor(field, triangleIndex)))
            {
                continue;
            }

            var candidate = new CoreCandidate(
                triangleIndex,
                (long)(PhysicalNoise.HashUnit(
                    seed,
                    field.Grid.TriangleIdentity(triangleIndex),
                    count) * long.MaxValue));
            AddBoundedCandidate(candidates, candidate, count);
        }

        var selected = new CoreCandidate[candidates.Count];
        for (var index = 0; index < selected.Length; index++)
        {
            selected[index] = candidates.Dequeue();
        }

        Array.Sort(selected, static (first, second) => second.Score.CompareTo(first.Score));
        return selected.Select(static candidate => candidate.TriangleIndex).ToArray();
    }

    private static void AddBoundedCandidate(
        PriorityQueue<CoreCandidate, (long Score, int TriangleIndex)> candidates,
        CoreCandidate candidate,
        int capacity)
    {
        var priority = (candidate.Score, candidate.TriangleIndex);
        if (candidates.Count < capacity)
        {
            candidates.Enqueue(candidate, priority);
            return;
        }

        _ = candidates.TryPeek(out _, out var weakest);
        if (priority.CompareTo(weakest) <= 0)
        {
            return;
        }

        _ = candidates.Dequeue();
        candidates.Enqueue(candidate, priority);
    }

    private static bool HasDryNeighbor(PhysicalGeographyField field, int triangleIndex)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 && field.WaterKindCodes[neighbor] != PhysicalWaterKindCodes.Ocean)
            {
                return true;
            }
        }

        return false;
    }

    private static void TraceAncientWake(
        IceRemembersMicrogeographyMap map,
        int source,
        int seed,
        int strength)
    {
        var angle = PhysicalNoise.HashUnit(seed, source, 0) * Math.PI * 2d;
        var current = source;
        var previous = -1;
        for (var step = 0; step < 190; step++)
        {
            map.PressureWake[current] = true;
            map.PressureIntensity[current] = Math.Min(
                1000,
                map.PressureIntensity[current] + (180 * strength / 1000));
            var next = BestOceanNeighbor(map.Physical, current, previous, angle);
            if (next < 0)
            {
                break;
            }

            angle += PhysicalNoise.HashSigned(seed, step, current) * 0.16d;
            previous = current;
            current = next;
        }
    }

    private readonly record struct CoreCandidate(int TriangleIndex, long Score);
}
