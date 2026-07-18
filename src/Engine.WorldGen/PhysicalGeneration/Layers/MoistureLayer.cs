namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class MoistureLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "moisture-transport",
        0x5e29,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Temperature,
        PhysicalGeographyFacts.Moisture,
        PhysicalGeographyFacts.Runoff |
        PhysicalGeographyFacts.LandIce |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        var oceanDistance = PhysicalDistanceFields.FromOcean(map);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None)
            {
                map.Moisture[triangleIndex] = 1000;
                continue;
            }

            var exposure = Math.Max(
                0,
                720 - (oceanDistance[triangleIndex] * 720 / context.Profile.OceanMoistureDistance));
            var weather = (int)Math.Round(
                PhysicalNoise.UnitNoise(
                    map.Grid.TriangleX(triangleIndex),
                    map.Grid.TriangleY(triangleIndex),
                    context.Seed,
                    31d,
                    3) * 230d,
                MidpointRounding.AwayFromZero);
            var rainShadow = UpwindBarrier(map, triangleIndex) * 2;
            var thaw = Math.Max(0, map.Temperature[triangleIndex] - 260) / 3;
            map.Moisture[triangleIndex] = Math.Clamp(
                80 + exposure + weather + thaw - rainShadow,
                0,
                1000);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        foreach (var value in map.Moisture)
        {
            if (value is < 0 or > 1000)
            {
                throw new InvalidOperationException("Moisture is out of range.");
            }
        }
    }

    private static int UpwindBarrier(PhysicalGeographyField map, int start)
    {
        var current = start;
        var barrier = 0;
        var startElevation = map.Elevation[start];
        for (var step = 0; step < 14; step++)
        {
            var next = -1;
            var bestProjection = double.MinValue;
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Grid.NeighborTriangleIndex(current, edge);
                if (neighbor < 0)
                {
                    continue;
                }

                var dx = map.Grid.TriangleX(current) - map.Grid.TriangleX(neighbor);
                var dy = map.Grid.TriangleY(current) - map.Grid.TriangleY(neighbor);
                var projection = (dx * 0.94d) + (dy * 0.34d);
                if (projection > bestProjection)
                {
                    next = neighbor;
                    bestProjection = projection;
                }
            }

            if (next < 0 || next == current)
            {
                break;
            }

            current = next;
            barrier = Math.Max(barrier, map.Elevation[current] - startElevation);
        }

        return Math.Max(0, barrier) / 6;
    }
}
