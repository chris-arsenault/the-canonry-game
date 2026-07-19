namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class BathymetryLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "bathymetry",
        0,
        PhysicalGeographyFacts.Elevation | PhysicalGeographyFacts.Ocean,
        PhysicalGeographyFacts.Bathymetry,
        PhysicalGeographyFacts.Landmasses |
        PhysicalGeographyFacts.OceanCurrents |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        MeasureSlopes(map);
        var oceanDistance = PhysicalDistanceFields.FromOcean(map);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                map.BathymetricZoneCodes[triangleIndex] = OceanZone(
                    map.WaterDepths[triangleIndex],
                    context.Profile);
            }
            else
            {
                map.BathymetricZoneCodes[triangleIndex] =
                    map.Elevation[triangleIndex] <= map.EffectiveSeaLevel + (context.Profile.ShelfDepth / 2) &&
                    oceanDistance[triangleIndex] is >= 0 and <= 18
                        ? PhysicalBathymetricZoneCodes.ExposedShelf
                        : PhysicalBathymetricZoneCodes.Land;
            }
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var zone = map.BathymetricZoneCodes[triangleIndex];
            if (zone is < PhysicalBathymetricZoneCodes.Min or > PhysicalBathymetricZoneCodes.Max ||
                (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean &&
                    zone == PhysicalBathymetricZoneCodes.Land))
            {
                throw new InvalidOperationException("Bathymetric classification is inconsistent.");
            }
        }
    }

    internal static void MeasureSlopes(PhysicalGeographyField map)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var slope = 0;
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0)
                {
                    slope = Math.Max(slope, Math.Abs(map.Elevation[triangleIndex] - map.Elevation[neighbor]));
                }
            }

            map.Slope[triangleIndex] = slope;
        }
    }

    private static int OceanZone(int depth, PhysicalGeographyGenerationProfile profile)
    {
        if (depth <= profile.ShelfDepth)
        {
            return PhysicalBathymetricZoneCodes.Shelf;
        }

        return depth <= profile.ShelfBreakDepth
            ? PhysicalBathymetricZoneCodes.ShelfBreak
            : PhysicalBathymetricZoneCodes.DeepOcean;
    }
}
