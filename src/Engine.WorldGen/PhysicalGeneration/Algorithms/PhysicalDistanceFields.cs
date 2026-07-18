namespace Engine.WorldGen;

using Engine.Constants;

internal static class PhysicalDistanceFields
{
    public static int[] FromOcean(PhysicalGeographyField map)
    {
        ArgumentNullException.ThrowIfNull(map);
        var distance = new int[map.Grid.TriangleCount];
        Array.Fill(distance, -1);
        var queue = new Queue<int>();
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                distance[triangleIndex] = 0;
                queue.Enqueue(triangleIndex);
            }
        }

        while (queue.TryDequeue(out var triangleIndex))
        {
            var nextDistance = distance[triangleIndex] + 1;
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0 && distance[neighbor] < 0)
                {
                    distance[neighbor] = nextDistance;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return distance;
    }
}
