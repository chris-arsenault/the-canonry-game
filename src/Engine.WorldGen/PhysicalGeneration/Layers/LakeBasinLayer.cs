namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class LakeBasinLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "lake-basins",
        0x7a31,
        PhysicalGeographyFacts.Elevation | PhysicalGeographyFacts.Ocean,
        PhysicalGeographyFacts.Lakes,
        PhysicalGeographyFacts.FlowRouting |
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        ClearPreviousLakes(map);
        _ = PriorityFlood.FillFromOcean(map);
        var depression = BuildDepressionMask(map);
        var componentMarks = new int[map.Grid.TriangleCount];
        var nextComponentId = 0;
        var qualified = new List<DepressionComponent>();
        for (var triangleIndex = 0; triangleIndex < depression.Length; triangleIndex++)
        {
            if (!depression[triangleIndex] || componentMarks[triangleIndex] != 0)
            {
                continue;
            }

            var surface = map.DrainageElevations[triangleIndex];
            var componentId = ++nextComponentId;
            var component = CollectComponent(
                map,
                depression,
                componentMarks,
                triangleIndex,
                componentId,
                surface);
            if (!Qualifies(component, context.Profile))
            {
                continue;
            }

            qualified.Add(component);
        }

        var selected = qualified
            .OrderByDescending(component => component.Significance)
            .ThenBy(component => component.Members[0])
            .Take(context.Profile.QualityTargets.InlandLakeBodyCount.Maximum);
        var nextBodyId = 1;
        foreach (var component in selected)
        {
            MarkLakeBody(
                map,
                context,
                component,
                componentMarks,
                component.ComponentId,
                ++nextBodyId);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        var surfaces = new Dictionary<int, int>();
        var counts = new Dictionary<int, int>();
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Lake)
            {
                continue;
            }

            var bodyId = map.WaterBodyIds[triangleIndex];
            var surface = map.Elevation[triangleIndex] + map.WaterDepths[triangleIndex];
            if (bodyId <= 1 || map.WaterDepths[triangleIndex] <= 0 ||
                (!surfaces.TryAdd(bodyId, surface) && surfaces[bodyId] != surface))
            {
                throw new InvalidOperationException("Lake body has inconsistent depth or surface data.");
            }

            counts[bodyId] = counts.GetValueOrDefault(bodyId) + 1;
        }

        if (counts.Values.Any(count => count < context.Profile.MinimumLakeTriangles))
        {
            throw new InvalidOperationException("Lake body is smaller than its configured minimum.");
        }
    }

    private static void ClearPreviousLakes(PhysicalGeographyField map)
    {
        map.LakeOutletTargets.Fill(-1);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake)
            {
                map.WaterKindCodes[triangleIndex] = PhysicalWaterKindCodes.None;
                map.WaterBodyIds[triangleIndex] = 0;
                map.WaterDepths[triangleIndex] = 0;
            }
        }
    }

    private static bool[] BuildDepressionMask(PhysicalGeographyField map)
    {
        var depression = new bool[map.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < depression.Length; triangleIndex++)
        {
            depression[triangleIndex] = map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None &&
                map.DrainageElevations[triangleIndex] > map.Elevation[triangleIndex];
        }

        return depression;
    }

    private static DepressionComponent CollectComponent(
        PhysicalGeographyField map,
        bool[] depression,
        int[] componentMarks,
        int start,
        int componentId,
        int surface)
    {
        var queue = new Queue<int>();
        var members = new List<int>();
        var maximumDepth = 0;
        componentMarks[start] = componentId;
        queue.Enqueue(start);
        while (queue.TryDequeue(out var member))
        {
            members.Add(member);
            maximumDepth = Math.Max(maximumDepth, surface - map.Elevation[member]);
            EnqueueComponentNeighbors(map, depression, componentMarks, queue, member, componentId, surface);
        }

        return new DepressionComponent(componentId, members, surface, maximumDepth);
    }

    private static void EnqueueComponentNeighbors(
        PhysicalGeographyField map,
        bool[] depression,
        int[] componentMarks,
        Queue<int> queue,
        int member,
        int componentId,
        int surface)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(member, edge);
            if (neighbor >= 0 && depression[neighbor] && componentMarks[neighbor] == 0 &&
                map.DrainageElevations[neighbor] == surface)
            {
                componentMarks[neighbor] = componentId;
                queue.Enqueue(neighbor);
            }
        }
    }

    private static bool Qualifies(
        DepressionComponent component,
        PhysicalGeographyGenerationProfile profile) =>
        component.Members.Count >= profile.MinimumLakeTriangles &&
        component.MaximumDepth >= profile.LakeDepthThreshold;

    private static void MarkLakeBody(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context,
        DepressionComponent component,
        int[] componentMarks,
        int componentId,
        int bodyId)
    {
        foreach (var member in component.Members)
        {
            map.WaterKindCodes[member] = PhysicalWaterKindCodes.Lake;
            map.WaterBodyIds[member] = bodyId;
            map.WaterDepths[member] = Math.Max(1, component.Surface - map.Elevation[member]);
        }

        var endorheic = PhysicalNoise.HashUnit(context.Seed, bodyId, component.Members.Count) <
            context.Profile.EndorheicLakeFraction;
        if (endorheic)
        {
            return;
        }

        var outlet = FindOutlet(map, component.Members, componentMarks, componentId, component.Surface);
        if (outlet.Member >= 0)
        {
            map.LakeOutletTargets[outlet.Member] = outlet.Target;
        }
    }

    private static (int Member, int Target) FindOutlet(
        PhysicalGeographyField map,
        IReadOnlyList<int> members,
        int[] componentMarks,
        int componentId,
        int surface)
    {
        var bestMember = -1;
        var bestTarget = -1;
        var bestRank = int.MaxValue;
        foreach (var member in members)
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Grid.NeighborTriangleIndex(member, edge);
                if (neighbor != map.DrainageParents[member] ||
                    !CanBeOutlet(map, componentMarks, componentId, surface, neighbor))
                {
                    continue;
                }

                var rank = map.DrainageRanks[member];
                if (rank < bestRank || (rank == bestRank && neighbor < bestTarget))
                {
                    bestMember = member;
                    bestTarget = neighbor;
                    bestRank = rank;
                }
            }
        }

        return (bestMember, bestTarget);
    }

    private static bool CanBeOutlet(
        PhysicalGeographyField map,
        int[] componentMarks,
        int componentId,
        int surface,
        int neighbor)
    {
        if (neighbor < 0 || componentMarks[neighbor] == componentId)
        {
            return false;
        }

        var elevation = map.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean
            ? int.MinValue
            : map.DrainageElevations[neighbor];
        return elevation <= surface;
    }

    private sealed record DepressionComponent(
        int ComponentId,
        List<int> Members,
        int Surface,
        int MaximumDepth)
    {
        public long Significance => (long)Members.Count * MaximumDepth;
    }
}
