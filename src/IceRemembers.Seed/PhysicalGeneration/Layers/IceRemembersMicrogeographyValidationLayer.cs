namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.WorldGen;

using IceRemembers;

internal sealed class IceRemembersMicrogeographyValidationLayer : IIceRemembersMicrogeographyLayer
{
    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "microgeography-quality-validation",
        0,
        IceRemembersMicrogeographyFacts.FinalSurfaces,
        IceRemembersMicrogeographyFacts.Validated);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = map;
        _ = context;
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        ValidateCodes(map);
        if (map.Physical.Grid.Radius >= context.Profile.QualityTargets.MinimumEvaluationRadius)
        {
            ValidateTargets(map, context.Profile);
        }
    }

    private static void ValidateCodes(IceRemembersMicrogeographyMap map)
    {
        var surfaceCount = IceRemembersPhysicalGeography.SurfacePalette.Count;
        var allFeatures = (1UL << IceRemembersPhysicalGeography.FeaturePalette.Count) - 1UL;
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            if (map.BaseSurfaceIndexes[triangleIndex] is < (int)IceRemembersPhysicalSurface.Tundra or
                    > (int)IceRemembersPhysicalSurface.Delta ||
                (uint)map.SurfaceIndexes[triangleIndex] >= (uint)surfaceCount ||
                (map.SignificantFeatureFlags[triangleIndex] & ~allFeatures) != 0)
            {
                throw new InvalidOperationException("Ice Remembers microgeography contains an invalid code.");
            }
        }
    }

    private static void ValidateTargets(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationProfile profile)
    {
        var configuredTriangleCount = checked(
            map.Physical.Grid.ConfiguredHexCount * PhysicalGeographyCodes.MicrotrianglesPerHex);
        var naturalCounts = new int[IceRemembersPhysicalGeography.SurfacePalette.Count];
        var finalCounts = new int[naturalCounts.Length];
        var overrideCount = 0;
        var fireCoreCount = 0;
        for (var configuredIndex = 0;
            configuredIndex < map.Physical.Grid.ConfiguredHexCount;
            configuredIndex++)
        {
            var hexIndex = map.Physical.Grid.ConfiguredHexIndexAt(configuredIndex);
            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                var triangleIndex = map.Physical.Grid.TriangleIndex(hexIndex, local);
                naturalCounts[map.BaseSurfaceIndexes[triangleIndex]]++;
                finalCounts[map.SurfaceIndexes[triangleIndex]]++;
                overrideCount += map.BaseSurfaceIndexes[triangleIndex] != map.SurfaceIndexes[triangleIndex]
                    ? 1
                    : 0;
                fireCoreCount += (map.Physical.FeatureFlags[triangleIndex] &
                    PhysicalGeographyFeatureCodes.FireCore) != 0 ? 1 : 0;
            }
        }

        var targets = profile.QualityTargets;
        ValidateProportion(
            "Largest natural surface",
            naturalCounts.Max() / (double)configuredTriangleCount,
            targets.LargestNaturalSurfaceCoverage);
        ValidateProportion(
            "Largest final surface",
            finalCounts.Max() / (double)configuredTriangleCount,
            targets.LargestFinalSurfaceCoverage);
        ValidateProportion(
            "Magic override",
            overrideCount / (double)configuredTriangleCount,
            targets.MagicOverrideCoverage);
        ValidateProportion(
            "Melt-line",
            finalCounts[(int)IceRemembersPhysicalSurface.MeltLine] /
                (double)configuredTriangleCount,
            targets.MeltLineCoverage);
        ValidateCount("Fire-core", fireCoreCount, targets.FireCoreCount);
        ValidateCount(
            "Warm habitat component",
            CountWarmHabitatComponents(map, profile),
            targets.WarmHabitatComponentCount);
    }

    private static int CountWarmHabitatComponents(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationProfile profile)
    {
        var visited = new bool[map.Physical.Grid.TriangleCount];
        var queue = new Queue<int>();
        var components = 0;
        for (var configuredIndex = 0;
            configuredIndex < map.Physical.Grid.ConfiguredHexCount;
            configuredIndex++)
        {
            var hexIndex = map.Physical.Grid.ConfiguredHexIndexAt(configuredIndex);
            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                var triangleIndex = map.Physical.Grid.TriangleIndex(hexIndex, local);
                if (visited[triangleIndex] || !IsWarmHabitat(map, triangleIndex, profile))
                {
                    continue;
                }

                components++;
                FloodWarmComponent(map, profile, triangleIndex, visited, queue);
            }
        }

        return components;
    }

    private static void FloodWarmComponent(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationProfile profile,
        int source,
        bool[] visited,
        Queue<int> queue)
    {
        visited[source] = true;
        queue.Enqueue(source);
        while (queue.TryDequeue(out var current))
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Physical.Grid.NeighborTriangleIndex(current, edge);
                if (neighbor >= 0 && map.Physical.Grid.TriangleIsInsideConfiguredDisk(neighbor) &&
                    !visited[neighbor] && IsWarmHabitat(map, neighbor, profile))
                {
                    visited[neighbor] = true;
                    queue.Enqueue(neighbor);
                }
            }
        }
    }

    private static bool IsWarmHabitat(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        IceRemembersMicrogeographyGenerationProfile profile) =>
        map.Physical.Temperature[triangleIndex] >= profile.OpenFieldMinimumTemperature &&
        map.Physical.Moisture[triangleIndex] >= profile.OpenFieldMinimumMoisture;

    private static void ValidateProportion(
        string name,
        double value,
        ProportionRangeTarget target)
    {
        if (!target.Contains(value))
        {
            throw new InvalidOperationException(
                $"{name} coverage {value:P1} misses configured target " +
                $"{target.Minimum:P1}–{target.Maximum:P1}.");
        }
    }

    private static void ValidateCount(string name, int value, CountTarget target)
    {
        if (!target.Contains(value))
        {
            throw new InvalidOperationException(
                $"{name} count {value} misses configured target {target.Minimum}–{target.Maximum}.");
        }
    }
}
