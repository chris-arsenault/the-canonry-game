namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class LandIceLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "land-ice-mass-balance",
        0,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Temperature |
        PhysicalGeographyFacts.Moisture |
        PhysicalGeographyFacts.Precipitation |
        PhysicalGeographyFacts.Geothermal,
        PhysicalGeographyFacts.LandIce,
        PhysicalGeographyFacts.IceFlow |
        PhysicalGeographyFacts.GlacialMeltwater |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        map.IceBalance.Clear();
        var land = new List<int>();
        var balances = new List<int>();
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None)
            {
                map.IceThickness[triangleIndex] = 0;
                continue;
            }

            var balance = MassBalance(map, triangleIndex, context.Profile);
            map.IceBalance[triangleIndex] = balance;
            if (map.Grid.TriangleIsInsideConfiguredDisk(triangleIndex))
            {
                land.Add(triangleIndex);
                balances.Add(balance);
            }
        }

        if (balances.Count == 0)
        {
            throw new InvalidOperationException("Land-ice generation requires dry land.");
        }

        var targetCount = Math.Clamp(
            (int)Math.Round(
                land.Count * context.Profile.QualityTargets.LandIceCoverage.Target *
                    context.GlaciationStrength / 1000d,
                MidpointRounding.AwayFromZero),
            0,
            land.Count);
        var cutoff = targetCount == 0
            ? int.MaxValue
            : Math.Max(
                context.Profile.MinimumIceBalance,
                IntegerOrderStatistics.ValueAtDescendingRank(
                    System.Runtime.InteropServices.CollectionsMarshal.AsSpan(balances),
                    targetCount - 1));
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var balance = map.IceBalance[triangleIndex];
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            map.IceThickness[triangleIndex] = balance >= cutoff
                ? AccumulateIce(
                    map.IceThickness[triangleIndex],
                    balance - cutoff,
                    context.Profile,
                    context.GlaciationStrength)
                : Math.Max(0, map.IceThickness[triangleIndex] - context.Profile.IceAblationStep);
            map.MaximumIceThickness[triangleIndex] = Math.Max(
                map.MaximumIceThickness[triangleIndex],
                map.IceThickness[triangleIndex]);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.IceThickness[triangleIndex] > 0 &&
                map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None)
            {
                throw new InvalidOperationException("Land glacier was assigned to water.");
            }
        }
    }

    private static int MassBalance(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile)
    {
        var cold = Math.Max(0, profile.IceTemperatureThreshold - map.Temperature[triangleIndex]);
        var snowfall = map.Snowfall[triangleIndex] * cold / 300;
        var altitude = Math.Max(0, map.Elevation[triangleIndex] - map.EffectiveSeaLevel) / 4;
        var concavity = LocalConcavity(map, triangleIndex);
        var warmAblation = Math.Max(0, map.Temperature[triangleIndex] - profile.IceTemperatureThreshold) * 4;
        var geothermalAblation = map.Geothermal[triangleIndex] / 3;
        var dryAblation = Math.Max(0, 280 - map.Moisture[triangleIndex]);
        return (cold * 2) + snowfall + altitude + concavity -
            warmAblation - geothermalAblation - dryAblation;
    }

    private static int AccumulateIce(
        int current,
        int balanceAboveCutoff,
        PhysicalGeographyGenerationProfile profile,
        int glaciationStrength)
    {
        var growth = Math.Min(
            profile.IceAccumulationStep,
            profile.GlacierThicknessThreshold + (balanceAboveCutoff * 2)) *
            glaciationStrength / 1000;
        return Math.Clamp(current + growth, profile.GlacierThicknessThreshold, 1000);
    }

    private static int LocalConcavity(PhysicalGeographyField map, int triangleIndex)
    {
        var total = 0;
        var count = 0;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0)
            {
                total += map.Elevation[neighbor] - map.Elevation[triangleIndex];
                count++;
            }
        }

        return count == 0 ? 0 : Math.Max(0, total / count) * 2;
    }
}
