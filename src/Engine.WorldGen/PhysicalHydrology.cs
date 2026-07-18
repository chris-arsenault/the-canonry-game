namespace Engine.WorldGen;

using Engine.Constants;

internal static class PhysicalHydrology
{
    public static void ClassifyLandmassesAndBathymetry(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        ClearWaterAndGeography(field);
        MeasureSlopes(field);
        FloodConnectedOcean(field, profile);
        LabelLandmasses(field, profile);
        ClassifyBathymetry(field, profile);
        LabelContinentalGroups(field, profile);
    }

    public static void SolveDrainage(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        ClearDrainage(field);
        var floodOrder = PriorityFlood(field);
        ClassifyLakes(field, profile);
        AssignDrainageBasins(field, floodOrder);
        AccumulateRunoff(field, floodOrder);
        ClassifyRivers(field, floodOrder, profile.RiverAccumulationThreshold);
    }

    public static void ReconcileAfterGlaciation(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        ClassifyLandmassesAndBathymetry(field, profile);
        SolveDrainage(field, profile);
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
            var nextDistance = distance[triangleIndex] + 1;
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0 && distance[neighbor] < 0)
                {
                    distance[neighbor] = nextDistance;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return distance;
    }

    internal static void MeasureSlopes(PhysicalGeographyField field)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var slope = 0;
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0)
                {
                    slope = Math.Max(slope, Math.Abs(field.Elevation[triangleIndex] - field.Elevation[neighbor]));
                }
            }

            field.Slope[triangleIndex] = slope;
        }
    }

    private static void ClearWaterAndGeography(PhysicalGeographyField field)
    {
        field.WaterKindCodes.Clear();
        field.WaterDepths.Clear();
        field.WaterBodyIds.Clear();
        field.LandmassIds.Clear();
        field.LandmassClassCodes.Clear();
        field.ContinentalGroupIds.Clear();
        field.BathymetricZoneCodes.Clear();
    }

    private static void ClearDrainage(PhysicalGeographyField field)
    {
        field.DrainageBasinIds.Clear();
        field.FlowAccumulation.Clear();
        field.RiverOrders.Clear();
        field.DrainageElevations.Clear();
        field.FlowTargets.Fill(-1);
        field.FlowEdges.Fill(PhysicalGeographyCodes.NoFlowEdge);
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake)
            {
                field.WaterKindCodes[triangleIndex] = PhysicalWaterKindCodes.None;
                field.WaterDepths[triangleIndex] = 0;
                field.WaterBodyIds[triangleIndex] = 0;
            }
        }
    }

    private static void FloodConnectedOcean(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        var seaLevel = ScaleSeaLevel(profile);
        var queue = new Queue<int>();
        SeedBoundaryOcean(field, seaLevel, queue);
        ExpandConnectedOcean(field, seaLevel, queue);
    }

    private static void SeedBoundaryOcean(
        PhysicalGeographyField field,
        int seaLevel,
        Queue<int> queue)
    {
        for (var hexIndex = 0; hexIndex < field.Grid.HexCount; hexIndex++)
        {
            if (!field.Grid.HexIsOnWorkBoundary(hexIndex))
            {
                continue;
            }

            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                var triangleIndex = field.Grid.TriangleIndex(hexIndex, local);
                if (CanBecomeOcean(field, triangleIndex, seaLevel))
                {
                    MarkOcean(field, triangleIndex, seaLevel, queue);
                }
            }
        }
    }

    private static void ExpandConnectedOcean(
        PhysicalGeographyField field,
        int seaLevel,
        Queue<int> queue)
    {
        while (queue.TryDequeue(out var triangleIndex))
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0 && CanBecomeOcean(field, neighbor, seaLevel))
                {
                    MarkOcean(field, neighbor, seaLevel, queue);
                }
            }
        }
    }

    private static bool CanBecomeOcean(
        PhysicalGeographyField field,
        int triangleIndex,
        int seaLevel) =>
        field.Elevation[triangleIndex] < seaLevel &&
        field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None;

    private static void MarkOcean(
        PhysicalGeographyField field,
        int triangleIndex,
        int seaLevel,
        Queue<int> queue)
    {
        field.WaterKindCodes[triangleIndex] = PhysicalWaterKindCodes.Ocean;
        field.WaterBodyIds[triangleIndex] = 1;
        field.WaterDepths[triangleIndex] = Math.Max(1, seaLevel - field.Elevation[triangleIndex]);
        queue.Enqueue(triangleIndex);
    }

    private static void LabelLandmasses(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        var queue = new Queue<int>();
        var members = new List<int>();
        var nextId = 0;
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean ||
                field.LandmassIds[triangleIndex] != 0)
            {
                continue;
            }

            nextId++;
            members.Clear();
            FloodLandmass(field, triangleIndex, nextId, members, queue);

            var classCode = members.Count >= profile.IslandTriangleThreshold
                ? PhysicalLandmassClassCodes.Continent
                : PhysicalLandmassClassCodes.Island;
            foreach (var member in members)
            {
                field.LandmassClassCodes[member] = classCode;
            }
        }
    }

    private static void FloodLandmass(
        PhysicalGeographyField field,
        int start,
        int landmassId,
        List<int> members,
        Queue<int> queue)
    {
        field.LandmassIds[start] = landmassId;
        queue.Enqueue(start);
        while (queue.TryDequeue(out var member))
        {
            members.Add(member);
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(member, edge);
                if (CanJoinLandmass(field, neighbor))
                {
                    field.LandmassIds[neighbor] = landmassId;
                    queue.Enqueue(neighbor);
                }
            }
        }
    }

    private static bool CanJoinLandmass(PhysicalGeographyField field, int triangleIndex) =>
        triangleIndex >= 0 &&
        field.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean &&
        field.LandmassIds[triangleIndex] == 0;

    private static void ClassifyBathymetry(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        var oceanDistance = MeasureOceanDistance(field);
        var seaLevel = ScaleSeaLevel(profile);
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            field.BathymetricZoneCodes[triangleIndex] = BathymetricZoneAt(
                field,
                oceanDistance,
                triangleIndex,
                seaLevel,
                profile);
        }
    }

    private static int BathymetricZoneAt(
        PhysicalGeographyField field,
        int[] oceanDistance,
        int triangleIndex,
        int seaLevel,
        PhysicalGeographyGenerationProfile profile)
    {
        if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
        {
            return OceanBathymetricZone(field.WaterDepths[triangleIndex], profile);
        }

        return field.Elevation[triangleIndex] <= seaLevel + (profile.ShelfDepth / 2) &&
            oceanDistance[triangleIndex] is >= 0 and <= 18
                ? PhysicalBathymetricZoneCodes.ExposedShelf
                : PhysicalBathymetricZoneCodes.Land;
    }

    private static int OceanBathymetricZone(
        int depth,
        PhysicalGeographyGenerationProfile profile)
    {
        if (depth <= profile.ShelfDepth)
        {
            return PhysicalBathymetricZoneCodes.Shelf;
        }

        return depth <= profile.ShelfBreakDepth
            ? PhysicalBathymetricZoneCodes.ShelfBreak
            : PhysicalBathymetricZoneCodes.DeepOcean;
    }

    private static void LabelContinentalGroups(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        var seaLevel = ScaleSeaLevel(profile);
        var shelfFloor = seaLevel - profile.ShelfBreakDepth;
        var queue = new Queue<int>();
        var nextId = 0;
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (field.Elevation[triangleIndex] < shelfFloor || field.ContinentalGroupIds[triangleIndex] != 0)
            {
                continue;
            }

            nextId++;
            field.ContinentalGroupIds[triangleIndex] = nextId;
            queue.Enqueue(triangleIndex);
            while (queue.TryDequeue(out var member))
            {
                for (var edge = 0; edge < 3; edge++)
                {
                    var neighbor = field.Grid.NeighborTriangleIndex(member, edge);
                    if (neighbor >= 0 &&
                        field.Elevation[neighbor] >= shelfFloor &&
                        field.ContinentalGroupIds[neighbor] == 0)
                    {
                        field.ContinentalGroupIds[neighbor] = nextId;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
    }

    private static int[] PriorityFlood(PhysicalGeographyField field)
    {
        var visited = new bool[field.Grid.TriangleCount];
        var order = new int[field.Grid.TriangleCount];
        var orderCount = 0;
        var queue = new PriorityQueue<int, (int Elevation, int Index)>();
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                SeedFlood(field, triangleIndex, visited, queue);
            }
        }

        if (queue.Count == 0)
        {
            throw new InvalidOperationException("Physical world has no connected ocean outlet.");
        }

        while (queue.TryDequeue(out var triangleIndex, out var priority))
        {
            order[orderCount++] = triangleIndex;
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor < 0 || visited[neighbor])
                {
                    continue;
                }

                visited[neighbor] = true;
                var drainageElevation = Math.Max(field.Elevation[neighbor], priority.Elevation);
                field.DrainageElevations[neighbor] = drainageElevation;
                field.FlowTargets[neighbor] = triangleIndex;
                field.FlowEdges[neighbor] = field.Grid.EdgeLeadingTo(neighbor, triangleIndex);
                queue.Enqueue(neighbor, (drainageElevation, neighbor));
            }
        }

        return orderCount == order.Length ? order : order[..orderCount];
    }

    private static void SeedFlood(
        PhysicalGeographyField field,
        int triangleIndex,
        bool[] visited,
        PriorityQueue<int, (int Elevation, int Index)> queue)
    {
        visited[triangleIndex] = true;
        field.DrainageElevations[triangleIndex] = field.Elevation[triangleIndex];
        queue.Enqueue(triangleIndex, (field.Elevation[triangleIndex], triangleIndex));
    }

    private static void ClassifyLakes(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        var candidate = new bool[field.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            candidate[triangleIndex] = field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None &&
                field.DrainageElevations[triangleIndex] - field.Elevation[triangleIndex] >= profile.LakeDepthThreshold;
        }

        var queue = new Queue<int>();
        var bodyId = 1;
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (!candidate[triangleIndex] || field.WaterBodyIds[triangleIndex] != 0)
            {
                continue;
            }

            bodyId++;
            MarkLake(field, triangleIndex, bodyId, queue);
            FloodLake(field, candidate, bodyId, queue);
        }
    }

    private static void FloodLake(
        PhysicalGeographyField field,
        bool[] candidate,
        int bodyId,
        Queue<int> queue)
    {
        while (queue.TryDequeue(out var member))
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(member, edge);
                if (CanJoinLake(field, candidate, member, neighbor))
                {
                    MarkLake(field, neighbor, bodyId, queue);
                }
            }
        }
    }

    private static bool CanJoinLake(
        PhysicalGeographyField field,
        bool[] candidate,
        int member,
        int neighbor) =>
        neighbor >= 0 && candidate[neighbor] && field.WaterBodyIds[neighbor] == 0 &&
        field.DrainageElevations[neighbor] == field.DrainageElevations[member];

    private static void MarkLake(
        PhysicalGeographyField field,
        int triangleIndex,
        int bodyId,
        Queue<int> queue)
    {
        field.WaterKindCodes[triangleIndex] = PhysicalWaterKindCodes.Lake;
        field.WaterBodyIds[triangleIndex] = bodyId;
        field.WaterDepths[triangleIndex] = Math.Max(
            1,
            field.DrainageElevations[triangleIndex] - field.Elevation[triangleIndex]);
        queue.Enqueue(triangleIndex);
    }

    private static void AssignDrainageBasins(PhysicalGeographyField field, ReadOnlySpan<int> floodOrder)
    {
        var nextBasin = 0;
        foreach (var triangleIndex in floodOrder)
        {
            if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            var target = field.FlowTargets[triangleIndex];
            if (target < 0 || field.WaterKindCodes[target] == PhysicalWaterKindCodes.Ocean)
            {
                field.DrainageBasinIds[triangleIndex] = ++nextBasin;
            }
            else
            {
                field.DrainageBasinIds[triangleIndex] = field.DrainageBasinIds[target];
            }
        }
    }

    private static void AccumulateRunoff(PhysicalGeographyField field, ReadOnlySpan<int> floodOrder)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            field.FlowAccumulation[triangleIndex] = Math.Max(1, field.Runoff[triangleIndex]);
        }

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
        ReadOnlySpan<int> floodOrder,
        int threshold)
    {
        var largestIncoming = new int[field.Grid.TriangleCount];
        var largestCount = new int[field.Grid.TriangleCount];
        for (var orderIndex = floodOrder.Length - 1; orderIndex >= 0; orderIndex--)
        {
            var triangleIndex = floodOrder[orderIndex];
            var order = RiverOrderAt(
                field.FlowAccumulation[triangleIndex],
                threshold,
                largestIncoming[triangleIndex],
                largestCount[triangleIndex]);
            if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None)
            {
                field.RiverOrders[triangleIndex] = order;
            }

            PassRiverOrder(field, triangleIndex, order, largestIncoming, largestCount);
        }
    }

    private static int RiverOrderAt(
        int accumulation,
        int threshold,
        int largestIncoming,
        int largestCount)
    {
        if (accumulation < threshold && largestIncoming == 0)
        {
            return 0;
        }

        if (largestIncoming == 0)
        {
            return 1;
        }

        return largestCount >= 2 ? largestIncoming + 1 : largestIncoming;
    }

    private static void PassRiverOrder(
        PhysicalGeographyField field,
        int triangleIndex,
        int order,
        int[] largestIncoming,
        int[] largestCount)
    {
        var target = field.FlowTargets[triangleIndex];
        if (target < 0 || order == 0)
        {
            return;
        }

        if (order > largestIncoming[target])
        {
            largestIncoming[target] = order;
            largestCount[target] = 1;
        }
        else if (order == largestIncoming[target])
        {
            largestCount[target]++;
        }
    }

    private static int ScaleSeaLevel(PhysicalGeographyGenerationProfile profile) =>
        (int)Math.Round(profile.SeaLevel * profile.ElevationScale, MidpointRounding.AwayFromZero);

    private static int SaturatingAdd(int first, int second) =>
        first > int.MaxValue - second ? int.MaxValue : first + second;
}
