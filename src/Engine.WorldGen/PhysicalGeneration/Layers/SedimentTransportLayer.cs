namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class SedimentTransportLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "sediment-transport",
        0,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.Bathymetry |
        PhysicalGeographyFacts.SedimentSupply |
        PhysicalGeographyFacts.Runoff |
        PhysicalGeographyFacts.FlowRouting |
        PhysicalGeographyFacts.Rivers,
        PhysicalGeographyFacts.Elevation | PhysicalGeographyFacts.SedimentTransport,
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Bathymetry |
        PhysicalGeographyFacts.Landmasses |
        PhysicalGeographyFacts.OceanCurrents |
        PhysicalGeographyFacts.Warmth |
        PhysicalGeographyFacts.Temperature |
        PhysicalGeographyFacts.Moisture |
        PhysicalGeographyFacts.Precipitation |
        PhysicalGeographyFacts.Runoff |
        PhysicalGeographyFacts.Lakes |
        PhysicalGeographyFacts.FlowRouting |
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.LandIce |
        PhysicalGeographyFacts.IceFlow |
        PhysicalGeographyFacts.SeaIce |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        var moving = new long[map.Grid.TriangleCount];
        var sourceToOutlet = FlowGraphAlgorithms.SourceToOutletOrder(map);
        foreach (var triangleIndex in sourceToOutlet)
        {
            var load = moving[triangleIndex] + map.SedimentSupply[triangleIndex];
            map.SedimentSupply[triangleIndex] = 0;
            if (load <= 0)
            {
                continue;
            }

            var target = map.FlowTargets[triangleIndex];
            var deposited = DepositAtTriangle(map, context.Profile, triangleIndex, target, load);
            var remaining = load - deposited;
            if (target >= 0)
            {
                moving[target] = checked(moving[target] + remaining);
            }
            else
            {
                map.SedimentExportedTotal = checked(map.SedimentExportedTotal + remaining);
            }
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        LandscapeSurface.Validate(map);
        if (map.SedimentSupply.ToArray().Any(static value => value != 0) ||
            map.SedimentDepositedTotal + map.SedimentExportedTotal != map.SedimentProducedTotal)
        {
            throw new InvalidOperationException("Sediment transport did not conserve its supplied load.");
        }
    }

    private static long DepositAtTriangle(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int triangleIndex,
        int target,
        long load)
    {
        if (!ShouldDeposit(map, profile, triangleIndex, target))
        {
            return 0;
        }

        var requested = (int)Math.Min(
            profile.MaximumSedimentDepositionPerPass,
            Math.Max(1L, load * profile.SedimentTransportFraction / 1000L));
        return LandscapeSurface.Deposit(map, triangleIndex, requested);
    }

    private static bool ShouldDeposit(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int triangleIndex,
        int target) =>
        map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake ||
        map.Slope[triangleIndex] <= profile.SedimentDepositionSlopeThreshold ||
        target >= 0 && map.WaterKindCodes[target] == PhysicalWaterKindCodes.Ocean;
}
