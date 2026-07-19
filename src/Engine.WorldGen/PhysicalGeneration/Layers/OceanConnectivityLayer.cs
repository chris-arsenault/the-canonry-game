namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class OceanConnectivityLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "ocean-connectivity",
        0,
        PhysicalGeographyFacts.Elevation,
        PhysicalGeographyFacts.Ocean,
        PhysicalGeographyFacts.Bathymetry |
        PhysicalGeographyFacts.Landmasses |
        PhysicalGeographyFacts.OceanCurrents |
        PhysicalGeographyFacts.Warmth |
        PhysicalGeographyFacts.Temperature |
        PhysicalGeographyFacts.Moisture |
        PhysicalGeographyFacts.Precipitation |
        PhysicalGeographyFacts.Runoff |
        PhysicalGeographyFacts.Lakes |
        PhysicalGeographyFacts.FlowRouting |
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.SeaIce |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        map.WaterKindCodes.Clear();
        map.WaterDepths.Clear();
        map.WaterBodyIds.Clear();
        map.EffectiveSeaLevel = FindEffectiveSeaLevel(map, context);
        FloodConnectedOcean(map);
    }

    private static int FindEffectiveSeaLevel(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context)
    {
        var profile = context.Profile;
        var configuredElevations = new int[checked(
            map.Grid.ConfiguredHexCount * PhysicalGeographyCodes.MicrotrianglesPerHex)];
        var offset = 0;
        for (var configuredIndex = 0; configuredIndex < map.Grid.ConfiguredHexCount; configuredIndex++)
        {
            var hexIndex = map.Grid.ConfiguredHexIndexAt(configuredIndex);
            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                configuredElevations[offset++] = map.Elevation[map.Grid.TriangleIndex(hexIndex, local)];
            }
        }

        var targetIndex = Math.Clamp(
            (int)Math.Round(
                profile.QualityTargets.OceanCoverage.Target * (configuredElevations.Length - 1),
                MidpointRounding.AwayFromZero),
            0,
            configuredElevations.Length - 1);
        var descendingRank = configuredElevations.Length - targetIndex - 1;
        var seaLevel = IntegerOrderStatistics.ValueAtDescendingRank(
            configuredElevations,
            descendingRank) +
            (int)Math.Round(profile.SeaLevel * profile.ElevationScale, MidpointRounding.AwayFromZero) +
            context.SeaLevelOffset;
        var minimumBoundary = int.MaxValue;
        for (var hexIndex = 0; hexIndex < map.Grid.HexCount; hexIndex++)
        {
            if (!map.Grid.HexIsOnWorkBoundary(hexIndex))
            {
                continue;
            }

            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                minimumBoundary = Math.Min(minimumBoundary, map.Elevation[map.Grid.TriangleIndex(hexIndex, local)]);
            }
        }

        return Math.Max(seaLevel, minimumBoundary + 1);
    }

    private static void FloodConnectedOcean(PhysicalGeographyField map)
    {
        var queue = new Queue<int>();
        for (var hexIndex = 0; hexIndex < map.Grid.HexCount; hexIndex++)
        {
            if (!map.Grid.HexIsOnWorkBoundary(hexIndex))
            {
                continue;
            }

            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                MarkIfOcean(map, map.Grid.TriangleIndex(hexIndex, local), queue);
            }
        }

        while (queue.TryDequeue(out var triangleIndex))
        {
            for (var edge = 0; edge < 3; edge++)
            {
                MarkIfOcean(map, map.Grid.NeighborTriangleIndex(triangleIndex, edge), queue);
            }
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        var count = 0;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            count++;
            if (map.Elevation[triangleIndex] >= map.EffectiveSeaLevel ||
                map.WaterDepths[triangleIndex] <= 0 || map.WaterBodyIds[triangleIndex] != 1)
            {
                throw new InvalidOperationException("Ocean classification contradicts sea level.");
            }
        }

        if (count == 0)
        {
            throw new InvalidOperationException("Physical geography contains no connected ocean.");
        }
    }

    private static void MarkIfOcean(PhysicalGeographyField map, int triangleIndex, Queue<int> queue)
    {
        if (triangleIndex < 0 || map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None ||
            map.Elevation[triangleIndex] >= map.EffectiveSeaLevel)
        {
            return;
        }

        map.WaterKindCodes[triangleIndex] = PhysicalWaterKindCodes.Ocean;
        map.WaterBodyIds[triangleIndex] = 1;
        map.WaterDepths[triangleIndex] = map.EffectiveSeaLevel - map.Elevation[triangleIndex];
        queue.Enqueue(triangleIndex);
    }
}
