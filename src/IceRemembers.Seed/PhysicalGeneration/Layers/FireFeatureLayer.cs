namespace IceRemembers.Seed;

using Engine.Constants;

using IceRemembers;

internal sealed class FireFeatureLayer : IIceRemembersMicrogeographyLayer
{
    private static readonly ulong OwnedFeatures = IceRemembersFeatureMasks.Of(
        IceRemembersPhysicalFeature.FirecoreVent,
        IceRemembersPhysicalFeature.ThermalVein,
        IceRemembersPhysicalFeature.HotSpring,
        IceRemembersPhysicalFeature.BasaltFlat,
        IceRemembersPhysicalFeature.ThawedHalo);

    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "fire-features",
        0,
        IceRemembersMicrogeographyFacts.FireClimate,
        IceRemembersMicrogeographyFacts.FireFeatures,
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
            var fire = map.FireIntensity[triangleIndex];
            var core = (map.Physical.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.FireCore) != 0;
            IceRemembersFeatureMasks.Add(
                ref flags,
                core,
                IceRemembersPhysicalFeature.FirecoreVent);
            IceRemembersFeatureMasks.Add(
                ref flags,
                fire >= 420 && map.Physical.FractureConductance[triangleIndex] >= 560,
                IceRemembersPhysicalFeature.ThermalVein);
            IceRemembersFeatureMasks.Add(
                ref flags,
                IsHotSpring(map, triangleIndex, fire),
                IceRemembersPhysicalFeature.HotSpring);
            IceRemembersFeatureMasks.Add(
                ref flags,
                fire >= 560 && map.Physical.Slope[triangleIndex] <= 45,
                IceRemembersPhysicalFeature.BasaltFlat);
            IceRemembersFeatureMasks.Add(
                ref flags,
                fire is >= 220 and < 560 && map.Physical.Temperature[triangleIndex] >= 300,
                IceRemembersPhysicalFeature.ThawedHalo);
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
            if (IceRemembersFeatureMasks.Has(flags, IceRemembersPhysicalFeature.FirecoreVent) !=
                ((map.Physical.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.FireCore) != 0) ||
                (IceRemembersFeatureMasks.Has(flags, IceRemembersPhysicalFeature.HotSpring) &&
                    !IsHotSpring(map, triangleIndex, map.FireIntensity[triangleIndex])))
            {
                throw new InvalidOperationException("Fire feature classification is inconsistent with its field.");
            }
        }
    }

    private static bool IsHotSpring(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        int fire)
    {
        var wetFireSpring = fire >= 430 &&
            (map.Physical.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake ||
                map.Physical.RiverOrders[triangleIndex] > 0 ||
                map.Physical.Moisture[triangleIndex] >= 650);
        var verdantGeothermalSpring = map.Physical.Geothermal[triangleIndex] >= 720 &&
            map.Physical.Temperature[triangleIndex] >= 335 &&
            map.Physical.Moisture[triangleIndex] >= 260;
        return wetFireSpring || verdantGeothermalSpring;
    }
}
