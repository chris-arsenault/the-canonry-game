namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.WorldGen;

using IceRemembers;

internal sealed class AuroraFeatureLayer : IIceRemembersMicrogeographyLayer
{
    private static readonly ulong OwnedFeatures = IceRemembersFeatureMasks.Of(
        IceRemembersPhysicalFeature.AuroraField,
        IceRemembersPhysicalFeature.LightWell,
        IceRemembersPhysicalFeature.FrostPrism,
        IceRemembersPhysicalFeature.MeltLine);

    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "aurora-features",
        0,
        IceRemembersMicrogeographyFacts.AuroraField,
        IceRemembersMicrogeographyFacts.AuroraFeatures,
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

        AddRegionFeature(
            map,
            region,
            IceRemembersPhysicalFeature.AuroraField,
            triangleIndex => IsAuroraFieldSeed(map, triangleIndex),
            triangleIndex => map.AuroraIntensity[triangleIndex] >= 470 - hysteresis &&
                map.Physical.IceThickness[triangleIndex] >= 180);
        AddRegionFeature(
            map,
            region,
            IceRemembersPhysicalFeature.LightWell,
            triangleIndex => IsLightWellSeed(map, triangleIndex),
            triangleIndex => map.AuroraIntensity[triangleIndex] >= 680 - hysteresis &&
                map.Physical.LandformCodes[triangleIndex] is PhysicalLandformCodes.Cirque or
                    PhysicalLandformCodes.Basin or PhysicalLandformCodes.Valley);
        AddRegionFeature(
            map,
            region,
            IceRemembersPhysicalFeature.FrostPrism,
            triangleIndex => IsFrostPrismSeed(map, triangleIndex),
            triangleIndex => IsFrostPrismRetained(map, context, triangleIndex, hysteresis));
        AddRegionFeature(
            map,
            region,
            IceRemembersPhysicalFeature.MeltLine,
            triangleIndex => IsMeltLineSeed(map, triangleIndex),
            triangleIndex => IsMeltLineRetained(map, triangleIndex, hysteresis));
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
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            if (IceRemembersFeatureMasks.Has(
                    map.SignificantFeatureFlags[triangleIndex],
                    IceRemembersPhysicalFeature.MeltLine) &&
                !IsMeltLineRetained(map, triangleIndex, context.Profile.MagicFeatureHysteresis))
            {
                throw new InvalidOperationException("Aurora feature classification is inconsistent with its field.");
            }
        }
    }

    private static bool IsAuroraFieldSeed(
        IceRemembersMicrogeographyMap map,
        int triangleIndex) =>
        map.AuroraIntensity[triangleIndex] >= 470 && map.Physical.IceThickness[triangleIndex] >= 260;

    private static bool IsLightWellSeed(
        IceRemembersMicrogeographyMap map,
        int triangleIndex) =>
        map.AuroraIntensity[triangleIndex] >= 680 && map.Physical.LandformCodes[triangleIndex] is
            PhysicalLandformCodes.Cirque or PhysicalLandformCodes.Basin;

    private static bool IsFrostPrismSeed(
        IceRemembersMicrogeographyMap map,
        int triangleIndex) =>
        map.AuroraIntensity[triangleIndex] >= 510 && map.Physical.LandformCodes[triangleIndex] is
            PhysicalLandformCodes.Peak or PhysicalLandformCodes.Ridge;

    /// <summary>
    /// Prism fields stay anchored to ridge systems and break up on a hashed
    /// scatter. Retaining on generic slopes let the aurora threshold contour
    /// paint periodic dashed stripes across open terrain that read as broken
    /// rivers.
    /// </summary>
    private static bool IsFrostPrismRetained(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context,
        int triangleIndex,
        int hysteresis) =>
        map.AuroraIntensity[triangleIndex] >= 510 - hysteresis &&
        map.Physical.LandformCodes[triangleIndex] is PhysicalLandformCodes.Peak or
            PhysicalLandformCodes.Ridge &&
        PhysicalNoise.HashUnit(context.Seed ^ 0x77aa, triangleIndex, 0x1d3) >= 0.25d;

    private static bool IsMeltLineSeed(
        IceRemembersMicrogeographyMap map,
        int triangleIndex) =>
        map.AuroraIntensity[triangleIndex] >= 650 &&
        map.Physical.IceThickness[triangleIndex] is >= 80 and <= 360 &&
        map.Physical.Temperature[triangleIndex] is >= 275 and <= 380;

    private static bool IsMeltLineRetained(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        int hysteresis) =>
        map.AuroraIntensity[triangleIndex] >= 650 - hysteresis &&
        map.Physical.IceThickness[triangleIndex] is >= 50 and <= 430 &&
        map.Physical.Temperature[triangleIndex] is >= 240 and <= 420;

    private static void AddRegionFeature(
        IceRemembersMicrogeographyMap map,
        CausalFeatureRegionClassifier region,
        IceRemembersPhysicalFeature feature,
        Func<int, bool> isSeed,
        Func<int, bool> isRetained)
    {
        region.Classify(isSeed, isRetained);
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            IceRemembersFeatureMasks.Add(
                ref map.SignificantFeatureFlags[triangleIndex],
                region.Includes(triangleIndex),
                feature);
        }
    }
}
