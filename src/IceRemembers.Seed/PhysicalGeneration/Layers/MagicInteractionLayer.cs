namespace IceRemembers.Seed;

using Engine.Constants;

using IceRemembers;

internal sealed class MagicInteractionLayer : IIceRemembersMicrogeographyLayer
{
    private static readonly ulong OwnedFeatures = IceRemembersFeatureMasks.Of(
        IceRemembersPhysicalFeature.ThermalWarRim,
        IceRemembersPhysicalFeature.SteamFracture,
        IceRemembersPhysicalFeature.PressureAuroraBreach);

    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "magic-interactions",
        0,
        IceRemembersMicrogeographyFacts.FireFeatures |
        IceRemembersMicrogeographyFacts.AuroraFeatures |
        IceRemembersMicrogeographyFacts.PressureFeatures,
        IceRemembersMicrogeographyFacts.Interactions,
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
            var fire = map.FireIntensity[triangleIndex];
            var aurora = map.AuroraIntensity[triangleIndex];
            var pressure = map.PressureIntensity[triangleIndex];
            IceRemembersFeatureMasks.Add(
                ref flags,
                fire >= 260 && aurora >= 340,
                IceRemembersPhysicalFeature.ThermalWarRim);
            IceRemembersFeatureMasks.Add(
                ref flags,
                fire >= 390 && pressure >= 420 &&
                    map.Physical.FractureConductance[triangleIndex] >= 520,
                IceRemembersPhysicalFeature.SteamFracture);
            IceRemembersFeatureMasks.Add(
                ref flags,
                pressure >= 390 && aurora >= 430 &&
                    map.Physical.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean &&
                    map.Physical.IceThickness[triangleIndex] >= 220,
                IceRemembersPhysicalFeature.PressureAuroraBreach);
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
            var flags = map.SignificantFeatureFlags[triangleIndex];
            if (IceRemembersFeatureMasks.Has(flags, IceRemembersPhysicalFeature.SteamFracture) &&
                (map.FireIntensity[triangleIndex] < 390 || map.PressureIntensity[triangleIndex] < 420 ||
                    map.Physical.FractureConductance[triangleIndex] < 520) ||
                IceRemembersFeatureMasks.Has(flags, IceRemembersPhysicalFeature.PressureAuroraBreach) &&
                map.Physical.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
            {
                throw new InvalidOperationException("Magic interaction classification is inconsistent with its fields.");
            }
        }
    }
}
