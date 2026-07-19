namespace Engine.WorldGen;

using Engine.Constants;

internal static class AtmosphericFlow
{
    public static int TraceUpwindBarrier(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        int start,
        int steps,
        out int oceanSource)
    {
        WindDirection(map, context, start, out var windX, out var windY);
        var current = start;
        var maximumBarrier = 0;
        oceanSource = -1;
        for (var step = 0; step < steps; step++)
        {
            var next = MostAlignedNeighbor(map, current, -windX, -windY);
            if (next < 0 || next == current)
            {
                break;
            }

            current = next;
            maximumBarrier = Math.Max(maximumBarrier, map.Elevation[current] - map.Elevation[start]);
            if (map.WaterKindCodes[current] == PhysicalWaterKindCodes.Ocean)
            {
                oceanSource = current;
                break;
            }
        }

        return Math.Clamp(
            maximumBarrier * 1000 / Math.Max(1, context.Profile.ElevationScale / 2),
            0,
            1000);
    }

    public static void WindDirection(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        int triangleIndex,
        out double windX,
        out double windY)
    {
        var x = map.Grid.TriangleX(triangleIndex);
        var y = map.Grid.TriangleY(triangleIndex);
        var baseAngle = PhysicalNoise.HashUnit(context.Seed, 0x2a1, 0x7c9) * Math.PI * 2d;
        var turn = PhysicalNoise.FractalNoise(
            x,
            y,
            context.Seed ^ 0x713b,
            Math.Max(20d, map.Grid.Radius * 0.55d),
            2) * 0.9d;
        windX = Math.Cos(baseAngle + turn);
        windY = Math.Sin(baseAngle + turn);
    }

    public static int MostAlignedNeighbor(
        PhysicalGeographyField map,
        int triangleIndex,
        double directionX,
        double directionY)
    {
        var best = -1;
        var bestAlignment = double.MinValue;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor < 0)
            {
                continue;
            }

            var dx = map.Grid.TriangleX(neighbor) - map.Grid.TriangleX(triangleIndex);
            var dy = map.Grid.TriangleY(neighbor) - map.Grid.TriangleY(triangleIndex);
            var length = Math.Max(0.001d, Math.Sqrt((dx * dx) + (dy * dy)));
            var alignment = ((dx / length) * directionX) + ((dy / length) * directionY);
            if (alignment > bestAlignment)
            {
                best = neighbor;
                bestAlignment = alignment;
            }
        }

        return best;
    }
}
