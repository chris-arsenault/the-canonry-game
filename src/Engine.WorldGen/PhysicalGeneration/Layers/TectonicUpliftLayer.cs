namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class TectonicUpliftLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "tectonic-uplift",
        0x4f2b17,
        PhysicalGeographyFacts.TectonicProvinces | PhysicalGeographyFacts.ContinentalDomains,
        PhysicalGeographyFacts.Elevation,
        PhysicalGeographyFacts.VolcanicConstruction |
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
            InteriorRelief(map, context, triangleIndex, out var interiorRelief, out var belt);
            var elevation = baseElevation + boundaryRelief + variation + interiorRelief -
                assemblySeparation;
            if (context.IsEvolutionEpoch)
            {
                ApplyIncrementalUplift(map, context, triangleIndex, boundaryRelief, belt);
            }
            else
            {
                map.Elevation[triangleIndex] = PhysicalElevation.FromScaled(
                    elevation,
                    context.Profile.ElevationScale);
            }

            map.Uplift[triangleIndex] = (float)Math.Clamp(
                Math.Max(0d, boundaryRelief) + (belt * 0.55d) + (potential * 0.48d) +
                    InheritedRelief(map, triangleIndex, context.Profile) * 0.20d,
                0d,
                1.5d);
            map.RidgeInfluence[triangleIndex] = (float)Math.Clamp(
                Math.Max(
                    Math.Max(
                        Math.Max(0d, boundaryRelief) / Math.Max(0.001d, context.Profile.RidgeUplift),
                        belt / Math.Max(0.001d, context.Profile.InteriorBeltStrength) * 0.8d),
                    InheritedRelief(map, triangleIndex, context.Profile) * 0.52d),
                0d,
                1d);
        }
    }

    /// <summary>
    /// Persistent interior height structure on continental crust: broad
    /// highlands/basins plus elongated mountain belts along the zero contours
    /// of an epoch-stable belt field. Keeps a strong differential everywhere so
    /// downstream processes never operate on a flat plateau.
    /// </summary>
    private static void InteriorRelief(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        int triangleIndex,
        out double relief,
        out double belt)
    {
        var profile = context.Profile;
        var continentality = Math.Clamp(
            ((map.ContinentalPotential[triangleIndex] / 1000d) - 0.45d) / 0.25d,
            0d,
            1d);
        if (continentality <= 0d)
        {
            relief = 0d;
            belt = 0d;
            return;
        }

        var x = map.Grid.TriangleX(triangleIndex);
        var y = map.Grid.TriangleY(triangleIndex);
        var macro = PhysicalNoise.FractalNoise(
            x,
            y,
            context.EpochStableSeed ^ 0x5ee1,
            profile.InteriorReliefScale,
            4) * profile.InteriorReliefStrength;
        var beltField = PhysicalNoise.FractalNoise(
            x,
            y,
            context.EpochStableSeed ^ 0x77a3,
            profile.InteriorBeltScale * 2.6d,
            3);
        var beltMask = Math.Clamp(
            (profile.InteriorBeltCoverage - Math.Abs(beltField)) / 0.18d,
            0d,
            1d);
        var ridged = 1d - Math.Abs(PhysicalNoise.FractalNoise(
            x,
            y,
            context.EpochStableSeed ^ 0x3b59,
            profile.InteriorBeltScale,
            4));
        belt = continentality * beltMask * Math.Pow(ridged, 1.6d) * profile.InteriorBeltStrength;
        relief = (continentality * macro) + belt;
    }

    private static void ApplyIncrementalUplift(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        int triangleIndex,
        double boundaryRelief,
        double belt)
    {
        var beltUplift = belt * context.Profile.EpochInteriorUpliftShare / 1000d;
        var delta = PhysicalElevation.FromUnits(
            (boundaryRelief + beltUplift) * context.Profile.EpochTectonicElevationStep *
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
