namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class TectonicUpliftLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "tectonic-uplift",
        0x4f2b17,
        PhysicalGeographyFacts.TectonicProvinces | PhysicalGeographyFacts.ContinentalDomains,
        PhysicalGeographyFacts.Elevation,
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Bathymetry |
        PhysicalGeographyFacts.Landmasses |
        PhysicalGeographyFacts.Fractures |
        PhysicalGeographyFacts.Geothermal |
        PhysicalGeographyFacts.OceanCurrents |
        PhysicalGeographyFacts.Warmth |
        PhysicalGeographyFacts.Temperature |
        PhysicalGeographyFacts.AtmosphericFlow |
        PhysicalGeographyFacts.Moisture |
        PhysicalGeographyFacts.Precipitation |
        PhysicalGeographyFacts.Runoff |
        PhysicalGeographyFacts.DrainageConditioning |
        PhysicalGeographyFacts.Lakes |
        PhysicalGeographyFacts.FlowRouting |
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.LandIce |
        PhysicalGeographyFacts.IceFlow |
        PhysicalGeographyFacts.SeaIce |
        PhysicalGeographyFacts.GlacialMeltwater |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.RiverGeometry |
        PhysicalGeographyFacts.Floodplains |
        PhysicalGeographyFacts.Deltas |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var x = map.Grid.TriangleX(triangleIndex);
            var y = map.Grid.TriangleY(triangleIndex);
            var potential = map.ContinentalPotential[triangleIndex] / 1000d;
            var baseElevation = ((potential - 0.5d) * 1.72d) - 0.04d;
            var strain = map.TectonicStrain[triangleIndex] / 1000d;
            var boundaryRelief = BoundaryRelief(map, triangleIndex, strain, context);
            var assemblySeparation = map.CrustAssemblySeparation[triangleIndex] / 1000d;
            var variation = PhysicalNoise.FractalNoise(
                x,
                y,
                context.Seed ^ 0x52c9,
                context.Profile.ElevationVariationScale,
                3) * context.Profile.ElevationVariationStrength;
            var elevation = baseElevation + boundaryRelief + variation - assemblySeparation;
            if (context.IsEvolutionEpoch)
            {
                ApplyIncrementalUplift(map, context, triangleIndex, boundaryRelief);
            }
            else
            {
                map.Elevation[triangleIndex] = PhysicalElevation.FromScaled(
                    elevation,
                    context.Profile.ElevationScale);
            }

            map.Uplift[triangleIndex] = (float)Math.Clamp(
                Math.Max(0d, boundaryRelief) + (potential * 0.48d) +
                    InheritedRelief(map, triangleIndex, context.Profile) * 0.20d,
                0d,
                1.5d);
            map.RidgeInfluence[triangleIndex] = (float)Math.Clamp(
                Math.Max(
                    Math.Max(0d, boundaryRelief) / Math.Max(0.001d, context.Profile.RidgeUplift),
                    InheritedRelief(map, triangleIndex, context.Profile) * 0.52d),
                0d,
                1d);
        }
    }

    private static void ApplyIncrementalUplift(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        int triangleIndex,
        double boundaryRelief)
    {
        var delta = PhysicalElevation.FromUnits(
            boundaryRelief * context.Profile.EpochTectonicElevationStep *
                context.TectonicStrength / 1000d);
        var maximumBedrock = PhysicalElevation.FromUnits(2000) - map.SedimentThickness[triangleIndex];
        var nextBedrock = Math.Clamp(
            map.BedrockElevation[triangleIndex] + delta,
            PhysicalElevation.FromUnits(-2000),
            maximumBedrock);
        var applied = nextBedrock - map.BedrockElevation[triangleIndex];
        map.BedrockElevation[triangleIndex] = nextBedrock;
        LandscapeSurface.Rebuild(map, triangleIndex);
        map.TectonicActivityHistory[triangleIndex] = checked(
            map.TectonicActivityHistory[triangleIndex] + PhysicalElevation.ToUnits(Math.Abs(applied)));
    }

    private static double InheritedRelief(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile) =>
        Math.Clamp(
            map.TectonicActivityHistory[triangleIndex] /
                Math.Max(1d, profile.EpochTectonicElevationStep * 1.8d),
            0d,
            1d);

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        var convergentRidge = false;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.RidgeInfluence[triangleIndex] is < 0f or > 1f)
            {
                throw new InvalidOperationException("Tectonic ridge influence is outside its scalar range.");
            }

            convergentRidge |=
                map.TectonicBoundaryCodes[triangleIndex] == PhysicalTectonicBoundaryCodes.Convergent &&
                map.RidgeInfluence[triangleIndex] > 0.15f;
        }

        if (!convergentRidge)
        {
            throw new InvalidOperationException("Tectonic uplift produced no convergent mountain system.");
        }
    }

    private static double BoundaryRelief(
        PhysicalGeographyField map,
        int triangleIndex,
        double strain,
        PhysicalGeographyGenerationContext context)
    {
        var profile = context.Profile;
        var shaped = strain * strain;
        return map.TectonicBoundaryCodes[triangleIndex] switch
        {
            PhysicalTectonicBoundaryCodes.Convergent => shaped * profile.RidgeUplift *
                ConvergentCrustScale(map, triangleIndex, context),
            PhysicalTectonicBoundaryCodes.Divergent => -shaped * profile.RiftDepth,
            PhysicalTectonicBoundaryCodes.Transform =>
                shaped * profile.RidgeUplift *
                (map.CrustTypeCodes[triangleIndex] == PhysicalCrustTypeCodes.Continental ? 0.16d : 0.05d),
            _ => 0d,
        };
    }

    private static double ConvergentCrustScale(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationContext context)
    {
        if (map.CrustTypeCodes[triangleIndex] == PhysicalCrustTypeCodes.Continental)
        {
            return 1d;
        }

        var arcNoise = PhysicalNoise.UnitNoise(
            map.Grid.TriangleX(triangleIndex),
            map.Grid.TriangleY(triangleIndex),
            context.Seed ^ 0x39d1,
            Math.Max(3d, context.Profile.RidgeWidth * 0.62d),
            2);
        var exposedArc = Math.Pow(Math.Clamp((arcNoise - 0.56d) / 0.44d, 0d, 1d), 2d);
        return 0.08d + (exposedArc * 0.30d);
    }

}
