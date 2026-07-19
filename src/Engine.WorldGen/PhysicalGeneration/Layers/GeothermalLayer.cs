namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class GeothermalLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "geothermal",
        0x152b37,
        PhysicalGeographyFacts.TectonicProvinces |
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.Fractures,
        PhysicalGeographyFacts.Geothermal,
        PhysicalGeographyFacts.OceanCurrents |
        PhysicalGeographyFacts.Warmth |
        PhysicalGeographyFacts.Temperature |
        PhysicalGeographyFacts.Moisture |
        PhysicalGeographyFacts.Precipitation |
        PhysicalGeographyFacts.Runoff |
        PhysicalGeographyFacts.LandIce |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            map.FeatureFlags[triangleIndex] &=
                ~(PhysicalGeographyFeatureCodes.GeothermalVent |
                    PhysicalGeographyFeatureCodes.MantlePlume);
            var strain = map.TectonicStrain[triangleIndex] / 1000d;
            var boundaryHeat = map.TectonicBoundaryCodes[triangleIndex] switch
            {
                PhysicalTectonicBoundaryCodes.Divergent => strain * 0.58d,
                PhysicalTectonicBoundaryCodes.Convergent => strain * 0.43d,
                PhysicalTectonicBoundaryCodes.Transform => strain * 0.24d,
                _ => 0d,
            };
            var fractureHeat = map.FractureConductance[triangleIndex] / 1000d * 0.28d;
            var broad = PhysicalNoise.UnitNoise(
                map.Grid.TriangleX(triangleIndex),
                map.Grid.TriangleY(triangleIndex),
                context.Seed,
                Math.Max(14d, map.Grid.Radius * 0.34d),
                2) * 0.10d;
            map.Geothermal[triangleIndex] = ScaleUnit(boundaryHeat + fractureHeat + broad);
        }

        PaintMantlePlumes(map, context);
        ClassifyHeatSources(map, context.Profile);
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        var vents = 0;
        var groundedVents = 0;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.Geothermal[triangleIndex] is < 0 or > 1000)
            {
                throw new InvalidOperationException("Geothermal intensity is out of range.");
            }

            if ((map.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.GeothermalVent) == 0)
            {
                continue;
            }

            vents++;
            groundedVents += map.TectonicStrain[triangleIndex] >= 180 ||
                map.FractureConductance[triangleIndex] >= 560 ||
                (map.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.MantlePlume) != 0
                    ? 1
                    : 0;
        }

        if (vents == 0 || groundedVents != vents)
        {
            throw new InvalidOperationException("Geothermal vents lack tectonic, fracture, or plume provenance.");
        }
    }

    private static void PaintMantlePlumes(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context)
    {
        if (context.Profile.MantlePlumeCount == 0)
        {
            return;
        }

        var candidateLimit = Math.Max(256, context.Profile.MantlePlumeCount * 256);
        var candidateSelection = new BoundedCandidateSelection(candidateLimit);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (!map.Grid.TriangleIsInsideConfiguredDisk(triangleIndex))
            {
                continue;
            }

            candidateSelection.Consider(
                triangleIndex,
                (1000 - map.TectonicStrain[triangleIndex]) +
                    (int)(PhysicalNoise.HashUnit(context.Seed ^ 0x37c1, triangleIndex, 0) * 420d));
        }

        var candidates = candidateSelection.GetBestIndexes();
        var selected = new List<int>(context.Profile.MantlePlumeCount);
        foreach (var candidate in candidates)
        {
            if (selected.Count >= context.Profile.MantlePlumeCount)
            {
                break;
            }

            if (map.TectonicStrain[candidate] <= 420 &&
                selected.TrueForAll(source => DistanceSquared(map.Grid, source, candidate) >=
                    context.Profile.MantlePlumeMinimumSpacing *
                    context.Profile.MantlePlumeMinimumSpacing))
            {
                selected.Add(candidate);
            }
        }

        foreach (var source in selected)
        {
            PaintPlume(
                map,
                source,
                context.Profile.MantlePlumeRadius,
                context.Profile.MantlePlumeStrength);
        }
    }

    private static void PaintPlume(
        PhysicalGeographyField map,
        int source,
        double radius,
        int strength)
    {
        map.FeatureFlags[source] |= PhysicalGeographyFeatureCodes.MantlePlume;
        PhysicalRadialFieldPainter.PaintMaximum(
            map.Grid,
            map.Geothermal,
            map.Grid.TriangleX(source),
            map.Grid.TriangleY(source),
            radius,
            strength);
    }

    private static void ClassifyHeatSources(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            map.HeatSourceDensity[triangleIndex] = Math.Clamp(
                ((map.Geothermal[triangleIndex] * 2) +
                    map.FractureConductance[triangleIndex] +
                    map.TectonicStrain[triangleIndex]) / 4,
                0,
                1000);
            if (map.Geothermal[triangleIndex] < profile.GeothermalVentThreshold ||
                (map.TectonicStrain[triangleIndex] < 180 &&
                    map.FractureConductance[triangleIndex] < 560 &&
                    (map.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.MantlePlume) == 0))
            {
                continue;
            }

            map.FeatureFlags[triangleIndex] |= PhysicalGeographyFeatureCodes.GeothermalVent;
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
