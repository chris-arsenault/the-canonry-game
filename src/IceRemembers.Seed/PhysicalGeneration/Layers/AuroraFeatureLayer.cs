namespace IceRemembers.Seed;

using Engine.Constants;

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
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            var flags = map.SignificantFeatureFlags[triangleIndex] & ~OwnedFeatures;
            var aurora = map.AuroraIntensity[triangleIndex];
            IceRemembersFeatureMasks.Add(
                ref flags,
                IsAuroraField(map, triangleIndex, aurora),
                IceRemembersPhysicalFeature.AuroraField);
            IceRemembersFeatureMasks.Add(
                ref flags,
                IsLightWell(map, triangleIndex, aurora),
                IceRemembersPhysicalFeature.LightWell);
            IceRemembersFeatureMasks.Add(
                ref flags,
                IsFrostPrism(map, triangleIndex, aurora),
                IceRemembersPhysicalFeature.FrostPrism);
            IceRemembersFeatureMasks.Add(
                ref flags,
                IsMeltLine(map, triangleIndex, aurora),
                IceRemembersPhysicalFeature.MeltLine);
            map.SignificantFeatureFlags[triangleIndex] = flags;
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
                (map.AuroraIntensity[triangleIndex] < 650 ||
                    map.Physical.IceThickness[triangleIndex] is < 80 or > 360))
            {
                throw new InvalidOperationException("Aurora feature classification is inconsistent with its field.");
            }
        }
    }

    private static bool IsAuroraField(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        int aurora) =>
        aurora >= 470 && map.Physical.IceThickness[triangleIndex] >= 260;

    private static bool IsLightWell(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        int aurora) =>
        aurora >= 680 && map.Physical.LandformCodes[triangleIndex] is
            PhysicalLandformCodes.Cirque or PhysicalLandformCodes.Basin;

    private static bool IsFrostPrism(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        int aurora) =>
        aurora >= 510 && map.Physical.LandformCodes[triangleIndex] is
            PhysicalLandformCodes.Peak or PhysicalLandformCodes.Ridge;

    private static bool IsMeltLine(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        int aurora) =>
        aurora >= 650 && map.Physical.IceThickness[triangleIndex] is >= 80 and <= 360 &&
        map.Physical.Temperature[triangleIndex] is >= 275 and <= 380;
}
