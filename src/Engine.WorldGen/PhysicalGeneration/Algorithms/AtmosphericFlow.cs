namespace Engine.WorldGen;

internal static class AtmosphericFlow
{
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

            var alignment = (map.Grid.TriangleEdgeUnitX(triangleIndex, edge) * directionX) +
                (map.Grid.TriangleEdgeUnitY(triangleIndex, edge) * directionY);
            if (alignment > bestAlignment)
            {
                best = neighbor;
                bestAlignment = alignment;
            }
        }

        return best;
    }
}
