namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class RiverNetworkLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "river-network",
        0,
        PhysicalGeographyFacts.Runoff | PhysicalGeographyFacts.FlowRouting,
        PhysicalGeographyFacts.Rivers,
        PhysicalGeographyFacts.RiverGeometry |
        PhysicalGeographyFacts.Floodplains |
        PhysicalGeographyFacts.Deltas |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        map.FlowAccumulation.Clear();
        map.RiverOrders.Clear();
        var order = FlowGraphAlgorithms.SourceToOutletOrder(map);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            map.FlowAccumulation[triangleIndex] = map.Runoff[triangleIndex];
        }

        foreach (var triangleIndex in order)
        {
            var target = map.FlowTargets[triangleIndex];
            if (target >= 0)
            {
                map.FlowAccumulation[target] = FlowGraphAlgorithms.SaturatingAdd(
                    map.FlowAccumulation[target],
                    map.FlowAccumulation[triangleIndex]);
            }
        }

        var channels = new bool[map.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < channels.Length; triangleIndex++)
        {
            channels[triangleIndex] = map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None &&
                map.FlowAccumulation[triangleIndex] >= context.Profile.RiverAccumulationThreshold;
        }

        PruneShortHeadwaters(map, channels, context.Profile.MinimumRiverReachTriangles);
        AssignStrahlerOrder(map, order, channels);
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.RiverOrders[triangleIndex] <= 0)
            {
                continue;
            }

            var target = map.FlowTargets[triangleIndex];
            if (target < 0 || map.FlowAccumulation[target] < map.FlowAccumulation[triangleIndex])
            {
                throw new InvalidOperationException("River accumulation does not continue downstream.");
            }
        }
    }

    private static void PruneShortHeadwaters(
        PhysicalGeographyField map,
        bool[] channels,
        int minimumLength)
    {
        var incoming = ChannelIncomingCounts(map, channels);
        for (var triangleIndex = 0; triangleIndex < channels.Length; triangleIndex++)
        {
            if (!channels[triangleIndex] || incoming[triangleIndex] != 0)
            {
                continue;
            }

            var reach = CollectHeadwaterReach(map, channels, incoming, triangleIndex);
            if (reach.Count < minimumLength)
            {
                foreach (var member in reach)
                {
                    channels[member] = false;
                }
            }
        }
    }

    private static List<int> CollectHeadwaterReach(
        PhysicalGeographyField map,
        bool[] channels,
        int[] incoming,
        int headwater)
    {
        var reach = new List<int>();
        var current = headwater;
        while (current >= 0 && channels[current] && incoming[current] <= 1)
        {
            reach.Add(current);
            var target = map.FlowTargets[current];
            if (target < 0 || !channels[target] || incoming[target] > 1)
            {
                break;
            }

            current = target;
        }

        return reach;
    }

    private static int[] ChannelIncomingCounts(PhysicalGeographyField map, bool[] channels)
    {
        var incoming = new int[channels.Length];
        for (var triangleIndex = 0; triangleIndex < channels.Length; triangleIndex++)
        {
            var target = map.FlowTargets[triangleIndex];
            if (channels[triangleIndex] && target >= 0 && channels[target])
            {
                incoming[target]++;
            }
        }

        return incoming;
    }

    private static void AssignStrahlerOrder(
        PhysicalGeographyField map,
        IReadOnlyList<int> sourceToOutlet,
        bool[] channels)
    {
        var largestIncoming = new int[channels.Length];
        var largestCount = new int[channels.Length];
        foreach (var triangleIndex in sourceToOutlet)
        {
            if (!channels[triangleIndex])
            {
                continue;
            }

            var order = CalculateStrahlerOrder(
                largestIncoming[triangleIndex],
                largestCount[triangleIndex]);
            map.RiverOrders[triangleIndex] = order;
            var target = map.FlowTargets[triangleIndex];
            if (target < 0)
            {
                continue;
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
    }

    private static int CalculateStrahlerOrder(int largestIncoming, int largestCount)
    {
        if (largestIncoming == 0)
        {
            return 1;
        }

        return largestCount >= 2 ? largestIncoming + 1 : largestIncoming;
    }
}
