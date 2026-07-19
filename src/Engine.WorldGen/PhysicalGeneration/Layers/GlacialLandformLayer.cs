namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class GlacialLandformLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "glacial-landforms",
        0,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.LandIce |
        PhysicalGeographyFacts.IceFlow,
        PhysicalGeographyFacts.Elevation | PhysicalGeographyFacts.GlacialRelief,
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
        PhysicalGeographyFacts.SeaIce |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        map.LandformCodes.Clear();
        var original = map.Elevation.ToArray();
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None ||
                map.IceFlux[triangleIndex] < context.Profile.GlacierFluxThreshold)
            {
                continue;
            }

            if (IsGlacialValley(map, triangleIndex, context.Profile))
            {
                map.LandformCodes[triangleIndex] = PhysicalLandformCodes.UValley;
                map.Elevation[triangleIndex] = Math.Min(
                    original[triangleIndex],
                    NeighborMean(original, map.Grid, triangleIndex) - 10);
            }

            if (IsCirqueHead(map, triangleIndex, context.Profile))
            {
                map.LandformCodes[triangleIndex] = PhysicalLandformCodes.Cirque;
                map.Elevation[triangleIndex] -= 18;
            }

            if (IsTerminus(map, triangleIndex, context.Profile))
            {
                map.LandformCodes[triangleIndex] = PhysicalLandformCodes.Moraine;
                map.Elevation[triangleIndex] += 14;
            }
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.LandformCodes[triangleIndex] is
                    PhysicalLandformCodes.UValley or
                    PhysicalLandformCodes.Cirque or
                    PhysicalLandformCodes.Moraine &&
                map.IceFlux[triangleIndex] < context.Profile.GlacierFluxThreshold)
            {
                throw new InvalidOperationException("Glacial landform lacks qualifying ice flux.");
            }
        }
    }

    private static bool IsGlacialValley(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile) =>
        map.RiverOrders[triangleIndex] > 0 ||
        map.FlowAccumulation[triangleIndex] >= profile.RiverAccumulationThreshold / 2;

    private static bool IsCirqueHead(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile)
    {
        if (map.IceThickness[triangleIndex] < profile.GlacierThicknessThreshold)
        {
            return false;
        }

        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 && map.IceFlux[neighbor] > map.IceFlux[triangleIndex])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsTerminus(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 &&
                (map.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean ||
                    map.IceFlux[neighbor] < profile.GlacierFluxThreshold / 3))
            {
                return true;
            }
        }

        return false;
    }

    private static int NeighborMean(int[] elevation, PhysicalGeographyGrid grid, int triangleIndex)
    {
        var total = 0;
        var count = 0;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0)
            {
                total += elevation[neighbor];
                count++;
            }
        }

        return count == 0 ? elevation[triangleIndex] : total / count;
    }
}
