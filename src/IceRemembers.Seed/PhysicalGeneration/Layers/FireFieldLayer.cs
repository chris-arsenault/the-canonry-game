namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.WorldGen;

internal sealed class FireFieldLayer : IIceRemembersMicrogeographyLayer
{
    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "fire-field",
        0x192d31,
        IceRemembersMicrogeographyFacts.None,
        IceRemembersMicrogeographyFacts.FireField,
        IceRemembersMicrogeographyFacts.FireClimate |
        IceRemembersMicrogeographyFacts.AuroraField |
        IceRemembersMicrogeographyFacts.PressureField |
        IceRemembersMicrogeographyFacts.FireFeatures |
        IceRemembersMicrogeographyFacts.AuroraFeatures |
        IceRemembersMicrogeographyFacts.PressureCoastalFeedback |
        IceRemembersMicrogeographyFacts.Interactions |
        IceRemembersMicrogeographyFacts.FinalSurfaces |
        IceRemembersMicrogeographyFacts.Validated);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        Array.Clear(map.FireIntensity);
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            map.Physical.FeatureFlags[triangleIndex] &=
                ~(PhysicalGeographyFeatureCodes.FireCore | PhysicalGeographyFeatureCodes.MagmaChamber);
        }

        var cores = context.FireStrength == 0
            ? []
            : SelectCores(map.Physical, context.Profile, context.Seed);
        var queue = new PriorityQueue<int, (int NegativeIntensity, int Index)>();
        SeedCores(map, cores, queue, context.FireStrength);
        Propagate(map, context.Profile, queue);
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        var coreCount = 0;
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            if (map.FireIntensity[triangleIndex] is < 0 or > 1000)
            {
                throw new InvalidOperationException("Fire intensity is outside its scalar range.");
            }

            if ((map.Physical.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.FireCore) != 0)
            {
                coreCount++;
                if (map.FireIntensity[triangleIndex] <= 0)
                {
                    throw new InvalidOperationException("A fire core has no fire intensity.");
                }
            }
        }

        if (context.FireStrength == 0 ? coreCount != 0 :
            coreCount is < 1 || coreCount > context.Profile.MaximumFireCores)
        {
            throw new InvalidOperationException("Fire-core selection missed its configured count bounds.");
        }
    }

    private static void SeedCores(
        IceRemembersMicrogeographyMap map,
        ReadOnlySpan<int> cores,
        PriorityQueue<int, (int NegativeIntensity, int Index)> queue,
        int strengthScale)
    {
        foreach (var core in cores)
        {
            var strength = Math.Max(
                1,
                Math.Clamp(720 + (map.Physical.HeatSourceDensity[core] / 3), 0, 1000) *
                    strengthScale / 1000);
            map.FireIntensity[core] = strength;
            map.Physical.FeatureFlags[core] |= PhysicalGeographyFeatureCodes.FireCore;
            if (strength >= 940)
            {
                map.Physical.FeatureFlags[core] |= PhysicalGeographyFeatureCodes.MagmaChamber;
            }

            queue.Enqueue(core, (-strength, core));
        }
    }

    private static void Propagate(
        IceRemembersMicrogeographyMap map,
        IceRemembers.IceRemembersMicrogeographyGenerationProfile profile,
        PriorityQueue<int, (int NegativeIntensity, int Index)> queue)
    {
        while (queue.TryDequeue(out var triangleIndex, out var priority))
        {
            var intensity = -priority.NegativeIntensity;
            if (intensity != map.FireIntensity[triangleIndex])
            {
                continue;
            }

            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Physical.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor < 0)
                {
                    continue;
                }

                var conductance = (map.Physical.FractureConductance[triangleIndex] +
                    map.Physical.FractureConductance[neighbor]) / 2;
                var cost = profile.FirePropagationBaseCost +
                    ((1000 - conductance) / profile.FireConductanceCostDivisor);
                var candidate = intensity - cost;
                if (candidate > map.FireIntensity[neighbor])
                {
                    map.FireIntensity[neighbor] = candidate;
                    queue.Enqueue(neighbor, (-candidate, neighbor));
                }
            }
        }
    }

    private static int[] SelectCores(
        PhysicalGeographyField field,
        IceRemembers.IceRemembersMicrogeographyGenerationProfile profile,
        int seed)
    {
        var candidates = new List<CoreCandidate>();
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var density = field.HeatSourceDensity[triangleIndex];
            if (density < profile.FireCoreDensityThreshold ||
                !IsLocalMaximum(field, triangleIndex, field.HeatSourceDensity))
            {
                continue;
            }

            var score = density * 1000L +
                (long)(PhysicalNoise.HashUnit(
                    seed,
                    field.Grid.TriangleIdentity(triangleIndex),
                    0) * 999d);
            candidates.Add(new CoreCandidate(triangleIndex, score));
        }

        candidates.Sort(static (first, second) => second.Score.CompareTo(first.Score));
        var selected = new List<int>(Math.Min(profile.MaximumFireCores, candidates.Count));
        foreach (var candidate in candidates)
        {
            if (selected.Count >= profile.MaximumFireCores)
            {
                break;
            }

            var radius = 4.5d + ((1000 - field.HeatSourceDensity[candidate.TriangleIndex]) / 150d);
            if (FarEnoughFromSelected(field.Grid, candidate.TriangleIndex, selected, radius))
            {
                selected.Add(candidate.TriangleIndex);
            }
        }

        if (selected.Count == 0)
        {
            selected.Add(DeepestHeatSource(field));
        }

        return selected.ToArray();
    }

    private static bool IsLocalMaximum(PhysicalGeographyField field, int triangleIndex, Span<int> values)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 && values[neighbor] > values[triangleIndex])
            {
                return false;
            }
        }

        return true;
    }

    private static bool FarEnoughFromSelected(
        PhysicalGeographyGrid grid,
        int candidate,
        IReadOnlyList<int> selected,
        double radius)
    {
        var x = grid.TriangleX(candidate);
        var y = grid.TriangleY(candidate);
        var radiusSquared = radius * radius;
        foreach (var existing in selected)
        {
            var dx = x - grid.TriangleX(existing);
            var dy = y - grid.TriangleY(existing);
            if ((dx * dx) + (dy * dy) < radiusSquared)
            {
                return false;
            }
        }

        return true;
    }

    private static int DeepestHeatSource(PhysicalGeographyField field)
    {
        var best = 0;
        for (var triangleIndex = 1; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (field.HeatSourceDensity[triangleIndex] > field.HeatSourceDensity[best])
            {
                best = triangleIndex;
            }
        }

        return best;
    }

    private readonly record struct CoreCandidate(int TriangleIndex, long Score);
}
