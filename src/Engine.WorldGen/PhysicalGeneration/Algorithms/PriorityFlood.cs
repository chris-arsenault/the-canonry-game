namespace Engine.WorldGen;

using Engine.Constants;

internal static class PriorityFlood
{
    public static int[] FillFromOcean(PhysicalGeographyField map)
    {
        ArgumentNullException.ThrowIfNull(map);
        var visited = new bool[map.Grid.TriangleCount];
        map.DrainageParents.Fill(-1);
        var order = new int[map.Grid.TriangleCount];
        var orderCount = 0;
        var (minimumElevation, maximumElevation) = ElevationBounds(map);
        var queue = new MonotoneIntegerPriorityQueue(
            map.Grid.TriangleCount,
            minimumElevation,
            maximumElevation);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            visited[triangleIndex] = true;
            map.DrainageElevations[triangleIndex] = map.Elevation[triangleIndex];
            queue.Enqueue(triangleIndex, map.Elevation[triangleIndex]);
        }

        if (queue.Count == 0)
        {
            throw new InvalidOperationException("Priority flood requires a connected ocean outlet.");
        }

        while (queue.TryDequeue(out var triangleIndex, out var priority))
        {
            map.DrainageRanks[triangleIndex] = orderCount;
            order[orderCount++] = triangleIndex;
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor < 0 || visited[neighbor])
                {
                    continue;
                }

                visited[neighbor] = true;
                var filledElevation = Math.Max(map.Elevation[neighbor], priority);
                map.DrainageElevations[neighbor] = filledElevation;
                map.DrainageParents[neighbor] = triangleIndex;
                queue.Enqueue(neighbor, filledElevation);
            }
        }

        if (orderCount != order.Length)
        {
            throw new InvalidOperationException("Priority flood did not visit the complete triangle graph.");
        }

        return order;
    }

    private static (int Minimum, int Maximum) ElevationBounds(PhysicalGeographyField map)
    {
        var minimum = int.MaxValue;
        var maximum = int.MinValue;
        foreach (var elevation in map.Elevation)
        {
            minimum = Math.Min(minimum, elevation);
            maximum = Math.Max(maximum, elevation);
        }

        return (minimum, maximum);
    }
}
