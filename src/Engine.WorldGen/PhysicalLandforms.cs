namespace Engine.WorldGen;

using Engine.Constants;

internal static class PhysicalLandforms
{
    public static void Classify(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        MeasureSlopes(field);
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            field.LandformCodes[triangleIndex] = ClassifyTriangle(field, triangleIndex, profile);
        }
    }

    private static void MeasureSlopes(PhysicalGeographyField field)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var slope = 0;
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0)
                {
                    slope = Math.Max(
                        slope,
                        Math.Abs(field.Elevation[triangleIndex] - field.Elevation[neighbor]));
                }
            }

            field.Slope[triangleIndex] = slope;
        }
    }

    private static int ClassifyTriangle(
        PhysicalGeographyField field,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile)
    {
        if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
        {
            return PhysicalLandformCodes.Seabed;
        }

        if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake)
        {
            return PhysicalLandformCodes.Basin;
        }

        var neighborShape = MeasureNeighborShape(field, triangleIndex);
        if (IsPeak(field, triangleIndex, neighborShape, profile))
        {
            return PhysicalLandformCodes.Peak;
        }

        if (IsPass(field, triangleIndex, neighborShape, profile))
        {
            return PhysicalLandformCodes.Pass;
        }

        if (IsRidge(field, triangleIndex, profile))
        {
            return PhysicalLandformCodes.Ridge;
        }

        if (IsValley(field, triangleIndex, neighborShape, profile))
        {
            return PhysicalLandformCodes.Valley;
        }

        return field.Slope[triangleIndex] <= profile.PlainSlopeThreshold
            ? PhysicalLandformCodes.Plain
            : PhysicalLandformCodes.Slope;
    }

    private static NeighborShape MeasureNeighborShape(PhysicalGeographyField field, int triangleIndex)
    {
        var higher = 0;
        var lower = 0;
        var lowestNeighbor = int.MaxValue;
        var highestNeighbor = int.MinValue;
        var elevation = field.Elevation[triangleIndex];
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor < 0)
            {
                continue;
            }

            var neighborElevation = field.Elevation[neighbor];
            higher += neighborElevation > elevation ? 1 : 0;
            lower += neighborElevation < elevation ? 1 : 0;
            lowestNeighbor = Math.Min(lowestNeighbor, neighborElevation);
            highestNeighbor = Math.Max(highestNeighbor, neighborElevation);
        }

        return new NeighborShape(higher, lower, lowestNeighbor, highestNeighbor);
    }

    private static bool IsPeak(
        PhysicalGeographyField field,
        int triangleIndex,
        NeighborShape shape,
        PhysicalGeographyGenerationProfile profile) =>
        field.Elevation[triangleIndex] >= profile.PeakElevationThreshold &&
        shape.LowerNeighbors >= 2 &&
        field.Elevation[triangleIndex] - shape.HighestNeighbor >= profile.PeakProminenceThreshold;

    private static bool IsPass(
        PhysicalGeographyField field,
        int triangleIndex,
        NeighborShape shape,
        PhysicalGeographyGenerationProfile profile) =>
        field.Elevation[triangleIndex] >= profile.RidgeElevationThreshold &&
        field.PlateBoundaryInfluence[triangleIndex] >= 0.35f &&
        shape.HigherNeighbors >= 2 &&
        shape.LowerNeighbors >= 1;

    private static bool IsRidge(
        PhysicalGeographyField field,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile) =>
        field.Elevation[triangleIndex] >= profile.RidgeElevationThreshold &&
        field.Slope[triangleIndex] >= profile.RidgeSlopeThreshold &&
        (field.PlateBoundaryInfluence[triangleIndex] >= 0.3f || field.Uplift[triangleIndex] >= 0.08f);

    private static bool IsValley(
        PhysicalGeographyField field,
        int triangleIndex,
        NeighborShape shape,
        PhysicalGeographyGenerationProfile profile) =>
        field.RiverOrders[triangleIndex] > 0 ||
        (shape.HigherNeighbors >= 2 &&
            field.Slope[triangleIndex] > profile.PlainSlopeThreshold &&
            field.FlowAccumulation[triangleIndex] >= profile.RiverAccumulationThreshold / 3);

    private readonly record struct NeighborShape(
        int HigherNeighbors,
        int LowerNeighbors,
        int LowestNeighbor,
        int HighestNeighbor);
}
