namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class GeothermalLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "geothermal",
        0x152b37,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Fractures,
        PhysicalGeographyFacts.Geothermal,
        PhysicalGeographyFacts.Warmth |
        PhysicalGeographyFacts.Temperature |
        PhysicalGeographyFacts.Moisture |
        PhysicalGeographyFacts.Runoff |
        PhysicalGeographyFacts.LandIce |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var x = map.Grid.TriangleX(triangleIndex);
            var y = map.Grid.TriangleY(triangleIndex);
            var tectonic = (map.RidgeInfluence[triangleIndex] * 0.30d) +
                (map.FractureConductance[triangleIndex] / 1000d * 0.34d);
            var broad = PhysicalNoise.UnitNoise(x, y, context.Seed, 42d, 3) * 0.16d;
            var hotspot = Math.Max(
                0d,
                PhysicalNoise.UnitNoise(x, y, context.Seed ^ 0x37c1, 17d, 2) - 0.78d) * 2.8d;
            map.Geothermal[triangleIndex] = ScaleUnit(tectonic + broad + hotspot);
        }

        ForcePeakSources(map, context);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            map.HeatSourceDensity[triangleIndex] = Math.Clamp(
                ((map.Geothermal[triangleIndex] * 2) + map.FractureConductance[triangleIndex]) / 3,
                0,
                1000);
            var flags = map.FeatureFlags[triangleIndex] & ~PhysicalGeographyFeatureCodes.GeothermalVent;
            if (map.Geothermal[triangleIndex] >= context.Profile.GeothermalVentThreshold)
            {
                flags |= PhysicalGeographyFeatureCodes.GeothermalVent;
            }

            map.FeatureFlags[triangleIndex] = flags;
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        var vents = 0;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.Geothermal[triangleIndex] is < 0 or > 1000)
            {
                throw new InvalidOperationException("Geothermal intensity is out of range.");
            }

            vents += (map.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.GeothermalVent) != 0 ? 1 : 0;
        }

        if (vents == 0)
        {
            throw new InvalidOperationException("Geothermal generation produced no vents.");
        }
    }

    private static void ForcePeakSources(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context)
    {
        const int sourceCount = 5;
        var candidates = Enumerable.Range(0, map.Grid.TriangleCount)
            .Where(index => map.Grid.TriangleIsInsideConfiguredDisk(index) &&
                map.WaterKindCodes[index] != PhysicalWaterKindCodes.Ocean)
            .OrderByDescending(index =>
                map.Elevation[index] +
                (map.FractureConductance[index] / 2) +
                (int)(PhysicalNoise.HashUnit(context.Seed, index, 0) * 100d))
            .ToArray();
        var sources = new List<int>(sourceCount);
        foreach (var candidate in candidates)
        {
            if (sources.Count == sourceCount)
            {
                break;
            }

            if (sources.TrueForAll(source => DistanceSquared(map.Grid, source, candidate) >= 24d * 24d))
            {
                sources.Add(candidate);
            }
        }

        foreach (var source in sources)
        {
            PaintSource(map, source, 8d, 920);
        }
    }

    private static void PaintSource(PhysicalGeographyField map, int source, double radius, int strength)
    {
        var radiusSquared = radius * radius;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var distanceSquared = DistanceSquared(map.Grid, source, triangleIndex);
            if (distanceSquared > radiusSquared)
            {
                continue;
            }

            var amount = 1d - (Math.Sqrt(distanceSquared) / radius);
            map.Geothermal[triangleIndex] = Math.Max(
                map.Geothermal[triangleIndex],
                (int)Math.Round(strength * amount * amount, MidpointRounding.AwayFromZero));
        }
    }

    private static double DistanceSquared(PhysicalGeographyGrid grid, int first, int second)
    {
        var dx = grid.TriangleX(first) - grid.TriangleX(second);
        var dy = grid.TriangleY(first) - grid.TriangleY(second);
        return (dx * dx) + (dy * dy);
    }

    private static int ScaleUnit(double value) =>
        (int)Math.Round(Math.Clamp(value, 0d, 1d) * 1000d, MidpointRounding.AwayFromZero);
}
