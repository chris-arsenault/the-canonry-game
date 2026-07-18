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
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            var flags = map.SignificantFeatureFlags[triangleIndex] & ~OwnedFeatures;
            var pressure = map.PressureIntensity[triangleIndex];
            IceRemembersFeatureMasks.Add(
                ref flags,
                triangleIndex == map.DeadGodTriangleIndex,
                IceRemembersPhysicalFeature.PressureGodLocus);
            IceRemembersFeatureMasks.Add(
                ref flags,
                pressure >= 690,
                IceRemembersPhysicalFeature.CrushZone);
            IceRemembersFeatureMasks.Add(
                ref flags,
                map.PressureWake[triangleIndex],
                IceRemembersPhysicalFeature.WakeScar);
            IceRemembersFeatureMasks.Add(
                ref flags,
                pressure >= 380 &&
                    PhysicalNoise.UnitNoise(
                        map.Physical.Grid.TriangleX(triangleIndex),
                        map.Physical.Grid.TriangleY(triangleIndex),
                        0x621d,
                        7d,
                        1) >= 0.54d,
                IceRemembersPhysicalFeature.InterferenceLattice);
            IceRemembersFeatureMasks.Add(
                ref flags,
                map.RogueImpact[triangleIndex],
                IceRemembersPhysicalFeature.RogueImpact);
            map.SignificantFeatureFlags[triangleIndex] = flags;
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
                    map.RogueImpact[triangleIndex])
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
}
