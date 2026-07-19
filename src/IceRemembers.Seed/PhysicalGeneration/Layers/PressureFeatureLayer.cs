namespace IceRemembers.Seed;

using Engine.WorldGen;

using IceRemembers;

internal sealed class PressureFeatureLayer : IIceRemembersMicrogeographyLayer
{
    private static readonly ulong OwnedFeatures = IceRemembersFeatureMasks.Of(
        IceRemembersPhysicalFeature.PressureGodLocus,
        IceRemembersPhysicalFeature.CrushZone,
        IceRemembersPhysicalFeature.WakeScar,
        IceRemembersPhysicalFeature.InterferenceLattice,
        IceRemembersPhysicalFeature.RogueImpact);

    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "pressure-features",
        0,
        IceRemembersMicrogeographyFacts.PressureField,
        IceRemembersMicrogeographyFacts.PressureFeatures,
        IceRemembersMicrogeographyFacts.Interactions |
        IceRemembersMicrogeographyFacts.FinalSurfaces |
        IceRemembersMicrogeographyFacts.Validated);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        ClearOwnedFeatures(map);
        var region = new CausalFeatureRegionClassifier(map.Physical.Grid);
        var hysteresis = context.Profile.MagicFeatureHysteresis;

        AddExactFeature(
            map,
            IceRemembersPhysicalFeature.PressureGodLocus,
            triangleIndex => triangleIndex == map.DeadGodTriangleIndex);
        AddRegionFeature(
            map,
            region,
            IceRemembersPhysicalFeature.CrushZone,
            triangleIndex => map.PressureIntensity[triangleIndex] >= 690,
            triangleIndex => map.PressureIntensity[triangleIndex] >= 690 - hysteresis);
        AddExactFeature(
            map,
            IceRemembersPhysicalFeature.WakeScar,
            triangleIndex => map.PressureWake[triangleIndex]);
        AddRegionFeature(
            map,
            region,
            IceRemembersPhysicalFeature.InterferenceLattice,
            triangleIndex => IsLatticeSeed(map, triangleIndex),
            triangleIndex => IsLatticeRetained(map, triangleIndex, hysteresis));
        AddExactFeature(
            map,
            IceRemembersPhysicalFeature.RogueImpact,
            triangleIndex => map.RogueImpact[triangleIndex]);
    }

    private static void ClearOwnedFeatures(IceRemembersMicrogeographyMap map)
    {
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            map.SignificantFeatureFlags[triangleIndex] &= ~OwnedFeatures;
        }
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = context;
        var locusCount = 0;
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            var flags = map.SignificantFeatureFlags[triangleIndex];
            locusCount += IceRemembersFeatureMasks.Has(
                flags,
                IceRemembersPhysicalFeature.PressureGodLocus) ? 1 : 0;
            if (IceRemembersFeatureMasks.Has(flags, IceRemembersPhysicalFeature.WakeScar) !=
                    map.PressureWake[triangleIndex] ||
                IceRemembersFeatureMasks.Has(flags, IceRemembersPhysicalFeature.RogueImpact) !=
                    map.RogueImpact[triangleIndex] ||
                (IceRemembersFeatureMasks.Has(
                        flags,
                        IceRemembersPhysicalFeature.InterferenceLattice) &&
                    !IsLatticeRetained(map, triangleIndex, context.Profile.MagicFeatureHysteresis)))
            {
                throw new InvalidOperationException("Pressure feature classification is inconsistent with its field.");
            }
        }

        if (locusCount != 1 || !IceRemembersFeatureMasks.Has(
                map.SignificantFeatureFlags[map.DeadGodTriangleIndex],
                IceRemembersPhysicalFeature.PressureGodLocus))
        {
            throw new InvalidOperationException("Pressure feature classification lost its unique locus.");
        }
    }

    private static bool IsLatticeSeed(IceRemembersMicrogeographyMap map, int triangleIndex) =>
        map.PressureIntensity[triangleIndex] >= 380 && LatticeNoise(map, triangleIndex) >= 0.62d;

    private static bool IsLatticeRetained(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        int hysteresis) =>
        map.PressureIntensity[triangleIndex] >= 380 - hysteresis &&
        LatticeNoise(map, triangleIndex) >= 0.40d;

    private static double LatticeNoise(IceRemembersMicrogeographyMap map, int triangleIndex) =>
        PhysicalNoise.UnitNoise(
            map.Physical.Grid.TriangleX(triangleIndex),
            map.Physical.Grid.TriangleY(triangleIndex),
            0x621d,
            7d,
            1);

    private static void AddExactFeature(
        IceRemembersMicrogeographyMap map,
        IceRemembersPhysicalFeature feature,
        Func<int, bool> includes)
    {
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            IceRemembersFeatureMasks.Add(
                ref map.SignificantFeatureFlags[triangleIndex],
                includes(triangleIndex),
                feature);
        }
    }

    private static void AddRegionFeature(
        IceRemembersMicrogeographyMap map,
        CausalFeatureRegionClassifier region,
        IceRemembersPhysicalFeature feature,
        Func<int, bool> isSeed,
        Func<int, bool> isRetained)
    {
        region.Classify(isSeed, isRetained);
        AddExactFeature(map, feature, region.Includes);
    }
}
