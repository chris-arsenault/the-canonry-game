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

        var basinDischarge = BuildBasinMaximumDischarge(map);
        var channels = BuildChannels(map, context.Profile, basinDischarge, order);
        var lakeOutflowStarts = FindLakeOutflowStarts(map, channels);

        PruneShortHeadwaters(
            map,
            channels,
            lakeOutflowStarts,
            context.Profile.MinimumRiverReachTriangles);
        CompleteDownstreamChannels(map, order, channels);
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

            ValidateContinuousOutletPath(map, triangleIndex);
        }
    }

    private static int[] BuildBasinMaximumDischarge(PhysicalGeographyField map)
    {
        var largestBasin = 0;
        foreach (var basin in map.DrainageBasinIds)
        {
            largestBasin = Math.Max(largestBasin, basin);
        }

        var maximum = new int[largestBasin + 1];
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var basin = map.DrainageBasinIds[triangleIndex];
            if (basin > 0)
            {
                maximum[basin] = Math.Max(maximum[basin], map.FlowAccumulation[triangleIndex]);
            }
        }

        return maximum;
    }

    private static bool[] BuildChannels(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int[] basinDischarge,
        IReadOnlyList<int> sourceToOutlet)
    {
        var channels = new bool[map.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < channels.Length; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            var basin = map.DrainageBasinIds[triangleIndex];
            var maximum = basin > 0 && basin < basinDischarge.Length
                ? basinDischarge[basin]
                : profile.RiverAccumulationThreshold;
            var basinThreshold = (int)Math.Min(
                int.MaxValue,
                (long)maximum * profile.RiverBasinHeadwaterFraction / 1000L);
            var threshold = Math.Clamp(
                basinThreshold,
                profile.RiverMinimumAccumulationThreshold,
                profile.RiverAccumulationThreshold);
            channels[triangleIndex] = map.FlowAccumulation[triangleIndex] >= threshold;
        }

        CompleteDownstreamChannels(map, sourceToOutlet, channels);
        return channels;
    }

    private static void CompleteDownstreamChannels(
        PhysicalGeographyField map,
        IReadOnlyList<int> sourceToOutlet,
        bool[] channels)
    {
        var downstreamOfChannel = (bool[])channels.Clone();
        foreach (var triangleIndex in sourceToOutlet)
        {
            if (!downstreamOfChannel[triangleIndex])
            {
                continue;
            }

            var target = map.FlowTargets[triangleIndex];
            if (target < 0)
            {
                continue;
            }

            downstreamOfChannel[target] = true;
            if (map.WaterKindCodes[target] == PhysicalWaterKindCodes.None)
            {
                channels[target] = true;
            }
        }
    }

    private static bool[] FindLakeOutflowStarts(PhysicalGeographyField map, bool[] channels)
    {
        var outflowStarts = new bool[channels.Length];
        for (var triangleIndex = 0; triangleIndex < channels.Length; triangleIndex++)
        {
            var target = map.FlowTargets[triangleIndex];
            if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake &&
                target >= 0 && channels[target])
            {
                outflowStarts[target] = true;
            }
        }

        return outflowStarts;
    }

    private static void ValidateContinuousOutletPath(PhysicalGeographyField map, int source)
    {
        var current = source;
        for (var step = 0; step <= map.Grid.TriangleCount; step++)
        {
            var target = map.FlowTargets[current];
            if (target < 0)
            {
                if (map.WaterKindCodes[current] is PhysicalWaterKindCodes.Ocean or PhysicalWaterKindCodes.Lake)
                {
                    return;
                }

                throw new InvalidOperationException("River channel terminates before reaching water.");
            }

            if (map.WaterKindCodes[target] == PhysicalWaterKindCodes.None &&
                map.RiverOrders[target] <= 0)
            {
                throw new InvalidOperationException(
                    $"River channel {source} contains a downstream break from {current} to {target}; " +
                    $"source basin={map.DrainageBasinIds[source]}, current water=" +
                    $"{map.WaterKindCodes[current]}, target basin={map.DrainageBasinIds[target]}.");
            }

            current = target;
        }

        throw new InvalidOperationException("River channel outlet path contains a cycle.");
    }

    private static void PruneShortHeadwaters(
        PhysicalGeographyField map,
        bool[] channels,
        bool[] protectedHeadwaters,
        int minimumLength)
    {
        var incoming = ChannelIncomingCounts(map, channels);
        for (var triangleIndex = 0; triangleIndex < channels.Length; triangleIndex++)
        {
            if (!channels[triangleIndex] || incoming[triangleIndex] != 0 ||
                protectedHeadwaters[triangleIndex])
            {
                continue;
            }

            var reach = CollectHeadwaterReach(
                map,
                channels,
                incoming,
                protectedHeadwaters,
                triangleIndex);
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
        bool[] protectedHeadwaters,
        int headwater)
    {
        var reach = new List<int>();
        var current = headwater;
        while (current >= 0 && channels[current] && incoming[current] <= 1)
        {
            reach.Add(current);
            var target = map.FlowTargets[current];
            if (target < 0 || !channels[target] || incoming[target] > 1 ||
                protectedHeadwaters[target])
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
