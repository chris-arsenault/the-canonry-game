namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class LandformClassificationLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "landform-classification",
        0,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Bathymetry |
        PhysicalGeographyFacts.Lakes |
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.GlacialRelief |
        PhysicalGeographyFacts.LandscapeHistory |
        PhysicalGeographyFacts.Impacts,
        PhysicalGeographyFacts.Landforms,
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        BathymetryLayer.MeasureSlopes(map);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            map.LandformCodes[triangleIndex] = ClassifyTriangle(map, triangleIndex, context.Profile);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        foreach (var code in map.LandformCodes)
        {
            if (code is < PhysicalLandformCodes.Min or > PhysicalLandformCodes.Max)
            {
                throw new InvalidOperationException("Landform classification produced an invalid code.");
            }
        }
    }

    private static int ClassifyTriangle(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile)
    {
        var shape = MeasureShape(map, triangleIndex);
        var retained = RetainedPhysicalLandform(map, triangleIndex);
        if (retained != 0)
        {
            return retained;
        }

        var historical = HistoricalLandform(map, triangleIndex, profile, shape);
        if (historical != 0)
        {
            return historical;
        }

        if (IsPeak(map, triangleIndex, profile, shape))
        {
            return PhysicalLandformCodes.Peak;
        }

        if (IsPass(map, triangleIndex, profile, shape))
        {
            return PhysicalLandformCodes.Pass;
        }

        if (IsRidge(map, triangleIndex, profile))
        {
            return PhysicalLandformCodes.Ridge;
        }

        if (IsValley(map, triangleIndex, profile, shape))
        {
            return PhysicalLandformCodes.Valley;
        }

        return map.Slope[triangleIndex] <= profile.PlainSlopeThreshold
            ? PhysicalLandformCodes.Plain
            : PhysicalLandformCodes.Slope;
    }

    private static bool IsPeak(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile,
        NeighborShape shape) =>
        map.Elevation[triangleIndex] >= profile.PeakElevationThreshold &&
        shape.LowerNeighbors >= 2 &&
        map.Elevation[triangleIndex] - shape.HighestNeighbor >= profile.PeakProminenceThreshold;

    private static bool IsPass(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile,
        NeighborShape shape) =>
        map.Elevation[triangleIndex] >= profile.RidgeElevationThreshold &&
        map.RidgeInfluence[triangleIndex] >= 0.34f &&
        shape.HigherNeighbors >= 2 && shape.LowerNeighbors >= 1;

    private static bool IsRidge(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile) =>
        map.Elevation[triangleIndex] >= profile.RidgeElevationThreshold &&
        map.Slope[triangleIndex] >= profile.RidgeSlopeThreshold &&
        (map.RidgeInfluence[triangleIndex] >= 0.26f || map.Uplift[triangleIndex] >= 0.42f ||
            map.TectonicActivityHistory[triangleIndex] >= profile.InheritedRidgeActivityThreshold);

    private static bool IsValley(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile,
        NeighborShape shape) =>
        map.RiverOrders[triangleIndex] > 0 ||
        map.CumulativeFluvialErosion[triangleIndex] >= profile.AncestralValleyMinimumErosion ||
        (shape.HigherNeighbors >= 2 && map.Slope[triangleIndex] > profile.PlainSlopeThreshold &&
            map.FlowAccumulation[triangleIndex] >= profile.RiverAccumulationThreshold / 3);

    private static int HistoricalLandform(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile,
        NeighborShape shape)
    {
        if (map.ImpactRimUplift[triangleIndex] >= profile.ImpactLandformMinimumRimUplift &&
            map.ImpactRimUplift[triangleIndex] * 2 > map.ImpactExcavation[triangleIndex])
        {
            return PhysicalLandformCodes.ImpactRim;
        }

        if (map.ImpactExcavation[triangleIndex] >= profile.ImpactLandformMinimumExcavation &&
            shape.HigherNeighbors >= 2)
        {
            return PhysicalLandformCodes.ImpactBasin;
        }

        return map.CumulativeGlacialDeposition[triangleIndex] >=
                profile.OutwashMinimumGlacialDeposition &&
            map.Slope[triangleIndex] <= profile.PlainSlopeThreshold
                ? PhysicalLandformCodes.OutwashPlain
                : 0;
    }

    private static int RetainedPhysicalLandform(PhysicalGeographyField map, int triangleIndex)
    {
        var glacial = map.LandformCodes[triangleIndex];
        if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
        {
            return glacial is PhysicalLandformCodes.UValley or PhysicalLandformCodes.Cirque
                ? PhysicalLandformCodes.Fjord
                : PhysicalLandformCodes.Seabed;
        }

        if (glacial is PhysicalLandformCodes.UValley or
            PhysicalLandformCodes.Cirque or PhysicalLandformCodes.Moraine)
        {
            return glacial;
        }

        return map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake
            ? PhysicalLandformCodes.Basin
            : 0;
    }

    private static NeighborShape MeasureShape(PhysicalGeographyField map, int triangleIndex)
    {
        var higher = 0;
        var lower = 0;
        var highest = int.MinValue;
        var elevation = map.Elevation[triangleIndex];
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor < 0)
            {
                continue;
            }

            var neighborElevation = map.Elevation[neighbor];
            higher += neighborElevation > elevation ? 1 : 0;
            lower += neighborElevation < elevation ? 1 : 0;
            highest = Math.Max(highest, neighborElevation);
        }

        return new NeighborShape(higher, lower, highest);
    }

    private readonly record struct NeighborShape(
        int HigherNeighbors,
        int LowerNeighbors,
        int HighestNeighbor);
}
