namespace Engine.WorldGen;

using Engine.Constants;

internal static class PriorityFlood
{
    private const int JitterBits = 8;
    private const long JitterMask = (1L << JitterBits) - 1L;

    public static void FillFromOcean(PhysicalGeographyField map, int seed)
    {
        ArgumentNullException.ThrowIfNull(map);
        map.DrainageParents.Fill(-1);
        map.DrainageRanks.Fill(-1);
        var orderCount = 0;
        var queue = new MonotoneRadixPriorityQueue();
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            map.DrainageRanks[triangleIndex] = int.MaxValue;
            map.DrainageElevations[triangleIndex] = map.Elevation[triangleIndex];
            queue.Enqueue(triangleIndex, JitteredKey(map, seed, triangleIndex));
        }

        if (queue.Count == 0)
        {
            throw new InvalidOperationException("Priority flood requires a connected ocean outlet.");
        }

        while (queue.TryDequeue(out var triangleIndex, out var priority))
        {
            map.DrainageRanks[triangleIndex] = orderCount++;
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor < 0 || map.DrainageRanks[neighbor] >= 0)
                {
                    continue;
                }

                map.DrainageRanks[neighbor] = int.MaxValue;
                var neighborKey = JitteredKey(map, seed, neighbor);
                map.FloodFillTotal++;
                if (neighborKey <= priority)
                {
                    map.FloodFlatFillTotal++;
                }

                // Epsilon fill: depressions drain along a strictly decreasing
                // conditioned surface, so no flat conditioned areas survive for
                // routing to tie-break across. The per-step epsilon is itself
                // hashed (1..8 subunits) so fill wavefronts advance unevenly
                // and chains across flats meander instead of running straight
                // graph geodesics to the spill.
                var epsilon = 1L + (neighborKey & 0x7L);
                var filledKey = Math.Max(neighborKey, priority + (epsilon << JitterBits));
                map.DrainageElevations[neighbor] = filledKey >> JitterBits;
                map.DrainageParents[neighbor] = triangleIndex;
                queue.Enqueue(neighbor, filledKey);
            }
        }

        if (orderCount != map.Grid.TriangleCount)
        {
            throw new InvalidOperationException("Priority flood did not visit the complete triangle graph.");
        }
    }

    /// <summary>
    /// Priority key: the floor elevation in the high bits and a per-triangle
    /// hash in the low bits, so equal-elevation cells flood in seeded random
    /// order instead of enqueue order. Only the elevation bits are stored back
    /// to <see cref="PhysicalGeographyField.DrainageElevations"/>.
    /// </summary>
    private static long JitteredKey(PhysicalGeographyField map, int seed, int triangleIndex)
    {
        var jitter = (long)(PhysicalNoise.HashUnit(seed ^ 0x1f7d, triangleIndex, 0x2c11) *
            JitterMask) & JitterMask;
        return checked(map.Elevation[triangleIndex] << JitterBits) | jitter;
    }
}
