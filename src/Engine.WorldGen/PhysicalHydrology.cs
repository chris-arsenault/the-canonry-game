namespace Engine.WorldGen;

using Engine.Constants;

internal static class PhysicalHydrology
{
    private const int OceanWaterBodyId = 1;

    public static void ClassifyOcean(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        var seaLevel = ScaleSeaLevel(profile);
        var queue = new Queue<int>();
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var hexIndex = field.Grid.TriangleHexIndex(triangleIndex);
            if (field.Grid.HexIsOnWorkBoundary(hexIndex) && field.Elevation[triangleIndex] <= seaLevel)
            {
                MarkOcean(field, triangleIndex, seaLevel, queue);
            }
        }

        if (queue.Count == 0)
        {
            throw new InvalidOperationException(
                "Physical worldgen produced no below-sea outlet on the padded field boundary.");
        }

        FloodConnectedOcean(field, seaLevel, queue);
    }

    public static int[] MeasureOceanDistance(PhysicalGeographyField field)
    {
        var distance = new int[field.Grid.TriangleCount];
        Array.Fill(distance, -1);
        var queue = new Queue<int>();
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                distance[triangleIndex] = 0;
                queue.Enqueue(triangleIndex);
            }
        }

        while (queue.TryDequeue(out var triangleIndex))
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0 && distance[neighbor] < 0)
                {
                    distance[neighbor] = distance[triangleIndex] + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return distance;
    }

    public static void SolveDrainage(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        var floodOrder = BuildConditionedDrainage(field);
        ClassifyLakes(field, profile);
        AssignDrainageBasins(field, floodOrder);
        AccumulateRunoff(field, floodOrder);
        ClassifyRivers(field, floodOrder, profile.RiverAccumulationThreshold);
    }

    private static int ScaleSeaLevel(PhysicalGeographyGenerationProfile profile) =>
        (int)Math.Round(
            profile.SeaLevel * profile.ElevationScale,
            MidpointRounding.AwayFromZero);

    private static void MarkOcean(
        PhysicalGeographyField field,
        int triangleIndex,
        int seaLevel,
        Queue<int> queue)
    {
        if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
        {
            return;
        }

        field.WaterKindCodes[triangleIndex] = PhysicalWaterKindCodes.Ocean;
        field.WaterDepths[triangleIndex] = Math.Max(1, seaLevel - field.Elevation[triangleIndex]);
        field.WaterBodyIds[triangleIndex] = OceanWaterBodyId;
        queue.Enqueue(triangleIndex);
    }

    private static void FloodConnectedOcean(
        PhysicalGeographyField field,
        int seaLevel,
        Queue<int> queue)
    {
        while (queue.TryDequeue(out var triangleIndex))
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0 &&
                    field.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.None &&
                    field.Elevation[neighbor] <= seaLevel)
                {
                    MarkOcean(field, neighbor, seaLevel, queue);
                }
            }
        }
    }

    private static int[] BuildConditionedDrainage(PhysicalGeographyField field)
    {
        var visited = new bool[field.Grid.TriangleCount];
        var order = new int[field.Grid.TriangleCount];
        var orderCount = 0;
        var queue = new PriorityQueue<int, (int Elevation, int TriangleIndex)>();
        SeedDrainageOutlets(field, visited, queue);

        while (queue.TryDequeue(out var triangleIndex, out _))
        {
            order[orderCount++] = triangleIndex;
            VisitDrainageNeighbors(field, triangleIndex, visited, queue);
        }

        if (orderCount != field.Grid.TriangleCount)
        {
            throw new InvalidOperationException("Physical drainage did not cover the padded triangle graph.");
        }

        return order;
    }

    private static void SeedDrainageOutlets(
        PhysicalGeographyField field,
        bool[] visited,
        PriorityQueue<int, (int Elevation, int TriangleIndex)> queue)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (field.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            visited[triangleIndex] = true;
            field.DrainageElevations[triangleIndex] = field.Elevation[triangleIndex];
            queue.Enqueue(
                triangleIndex,
                (field.DrainageElevations[triangleIndex], triangleIndex));
        }
    }

    private static void VisitDrainageNeighbors(
        PhysicalGeographyField field,
        int triangleIndex,
        bool[] visited,
        PriorityQueue<int, (int Elevation, int TriangleIndex)> queue)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor < 0 || visited[neighbor])
            {
                continue;
            }

            visited[neighbor] = true;
            field.DrainageElevations[neighbor] = Math.Max(
                field.Elevation[neighbor],
                field.DrainageElevations[triangleIndex]);
            field.FlowTargets[neighbor] = triangleIndex;
            field.FlowEdges[neighbor] = field.Grid.EdgeLeadingTo(neighbor, triangleIndex);
            queue.Enqueue(neighbor, (field.DrainageElevations[neighbor], neighbor));
        }
    }

    private static void ClassifyLakes(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        var candidate = new bool[field.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            candidate[triangleIndex] =
                field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None &&
                field.DrainageElevations[triangleIndex] - field.Elevation[triangleIndex] >=
                    profile.LakeDepthThreshold;
        }

        var waterBodyId = OceanWaterBodyId;
        var queue = new Queue<int>();
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (!candidate[triangleIndex] || field.WaterBodyIds[triangleIndex] != 0)
            {
                continue;
            }

            waterBodyId++;
            MarkLake(field, triangleIndex, waterBodyId, queue);
            FloodLakeBody(field, candidate, waterBodyId, queue);
        }
    }

    private static void MarkLake(
        PhysicalGeographyField field,
        int triangleIndex,
        int waterBodyId,
        Queue<int> queue)
    {
        field.WaterKindCodes[triangleIndex] = PhysicalWaterKindCodes.Lake;
        field.WaterBodyIds[triangleIndex] = waterBodyId;
        field.WaterDepths[triangleIndex] = Math.Max(
            1,
            field.DrainageElevations[triangleIndex] - field.Elevation[triangleIndex]);
        queue.Enqueue(triangleIndex);
    }

    private static void FloodLakeBody(
        PhysicalGeographyField field,
        bool[] candidate,
        int waterBodyId,
        Queue<int> queue)
    {
        while (queue.TryDequeue(out var triangleIndex))
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0 && candidate[neighbor] && field.WaterBodyIds[neighbor] == 0)
                {
                    MarkLake(field, neighbor, waterBodyId, queue);
                }
            }
        }
    }

    private static void AssignDrainageBasins(PhysicalGeographyField field, int[] floodOrder)
    {
        var nextBasinId = 0;
        foreach (var triangleIndex in floodOrder)
        {
            if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            var target = field.FlowTargets[triangleIndex];
            if (target < 0 || field.WaterKindCodes[target] == PhysicalWaterKindCodes.Ocean)
            {
                field.DrainageBasinIds[triangleIndex] = ++nextBasinId;
            }
            else
            {
                field.DrainageBasinIds[triangleIndex] = field.DrainageBasinIds[target];
            }
        }
    }

    private static void AccumulateRunoff(PhysicalGeographyField field, int[] floodOrder)
    {
        field.Runoff.CopyTo(field.FlowAccumulation);
        for (var orderIndex = floodOrder.Length - 1; orderIndex >= 0; orderIndex--)
        {
            var triangleIndex = floodOrder[orderIndex];
            var target = field.FlowTargets[triangleIndex];
            if (target >= 0)
            {
                field.FlowAccumulation[target] = SaturatingAdd(
                    field.FlowAccumulation[target],
                    field.FlowAccumulation[triangleIndex]);
            }
        }
    }

    private static void ClassifyRivers(
        PhysicalGeographyField field,
        int[] floodOrder,
        int riverThreshold)
    {
        var largestIncomingOrder = new int[field.Grid.TriangleCount];
        var largestIncomingCount = new int[field.Grid.TriangleCount];
        for (var orderIndex = floodOrder.Length - 1; orderIndex >= 0; orderIndex--)
        {
            var triangleIndex = floodOrder[orderIndex];
            var order = RiverOrderAt(
                field.FlowAccumulation[triangleIndex],
                riverThreshold,
                largestIncomingOrder[triangleIndex],
                largestIncomingCount[triangleIndex]);
            if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None)
            {
                field.RiverOrders[triangleIndex] = order;
            }

            PassRiverOrderDownstream(field, triangleIndex, order, largestIncomingOrder, largestIncomingCount);
        }
    }

    private static int RiverOrderAt(
        int accumulation,
        int riverThreshold,
        int largestIncoming,
        int largestIncomingCount)
    {
        if (accumulation < riverThreshold && largestIncoming == 0)
        {
            return 0;
        }

        if (largestIncoming == 0)
        {
            return 1;
        }

        return largestIncomingCount >= 2 ? largestIncoming + 1 : largestIncoming;
    }

    private static void PassRiverOrderDownstream(
        PhysicalGeographyField field,
        int triangleIndex,
        int order,
        int[] largestIncomingOrder,
        int[] largestIncomingCount)
    {
        var target = field.FlowTargets[triangleIndex];
        if (target < 0 || order == 0)
        {
            return;
        }

        if (order > largestIncomingOrder[target])
        {
            largestIncomingOrder[target] = order;
            largestIncomingCount[target] = 1;
        }
        else if (order == largestIncomingOrder[target])
        {
            largestIncomingCount[target]++;
        }
    }

    private static int SaturatingAdd(int first, int second) =>
        first > int.MaxValue - second ? int.MaxValue : first + second;
}
