namespace Engine.WorldGen;

using Engine.Constants;

internal static class PhysicalLandforms
{
    public static void Classify(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        PhysicalHydrology.MeasureSlopes(field);
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            field.LandformCodes[triangleIndex] = ClassifyTriangle(field, triangleIndex, profile);
        }
    }

    private static int ClassifyTriangle(
        PhysicalGeographyField field,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile)
    {
        var existing = ExistingLandform(field, triangleIndex);
        if (existing != 0)
        {
            return existing;
        }

        var shape = MeasureNeighborShape(field, triangleIndex);
        if (IsPeak(field, triangleIndex, shape, profile))
        {
            return PhysicalLandformCodes.Peak;
        }

        if (IsPass(field, triangleIndex, shape, profile))
        {
            return PhysicalLandformCodes.Pass;
        }

        if (IsRidge(field, triangleIndex, profile))
        {
            return PhysicalLandformCodes.Ridge;
        }

        if (IsValley(field, triangleIndex, shape, profile))
        {
            return PhysicalLandformCodes.Valley;
        }

        return field.Slope[triangleIndex] <= profile.PlainSlopeThreshold
            ? PhysicalLandformCodes.Plain
            : PhysicalLandformCodes.Slope;
    }

    private static int ExistingLandform(PhysicalGeographyField field, int triangleIndex)
    {
        var glacial = field.LandformCodes[triangleIndex];
        if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
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

        return field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake
            ? PhysicalLandformCodes.Basin
            : 0;
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
        field.RidgeInfluence[triangleIndex] >= 0.34f &&
        shape.HigherNeighbors >= 2 && shape.LowerNeighbors >= 1;

    private static bool IsRidge(
        PhysicalGeographyField field,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile) =>
        field.Elevation[triangleIndex] >= profile.RidgeElevationThreshold &&
        field.Slope[triangleIndex] >= profile.RidgeSlopeThreshold &&
        (field.RidgeInfluence[triangleIndex] >= 0.26f || field.Uplift[triangleIndex] >= 0.42f);

    private static bool IsValley(
        PhysicalGeographyField field,
        int triangleIndex,
        NeighborShape shape,
        PhysicalGeographyGenerationProfile profile) =>
        field.RiverOrders[triangleIndex] > 0 ||
        (shape.HigherNeighbors >= 2 &&
            field.Slope[triangleIndex] > profile.PlainSlopeThreshold &&
            field.FlowAccumulation[triangleIndex] >= profile.RiverAccumulationThreshold / 3);

    private static NeighborShape MeasureNeighborShape(PhysicalGeographyField field, int triangleIndex)
    {
        var higher = 0;
        var lower = 0;
        var highest = int.MinValue;
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
            highest = Math.Max(highest, neighborElevation);
        }

        return new NeighborShape(higher, lower, highest);
    }

    private readonly record struct NeighborShape(
        int HigherNeighbors,
        int LowerNeighbors,
        int HighestNeighbor);
}
