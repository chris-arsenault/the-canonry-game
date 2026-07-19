namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class RiverChannelGeometryLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "river-channel-geometry",
        0,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.Lakes |
        PhysicalGeographyFacts.FlowRouting |
        PhysicalGeographyFacts.Rivers,
        PhysicalGeographyFacts.RiverGeometry,
        PhysicalGeographyFacts.Floodplains |
        PhysicalGeographyFacts.Deltas |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        map.RiverChannelStrength.Clear();
        map.RiverChannelOrders.Clear();
        var remaining = new int[map.Grid.TriangleCount];
        var frontier = new List<int>();
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var order = map.RiverOrders[triangleIndex];
            if (order <= 0)
            {
                continue;
            }

            map.RiverChannelStrength[triangleIndex] = SourceStrength(map, triangleIndex, context.Profile);
            map.RiverChannelOrders[triangleIndex] = order;
            remaining[triangleIndex] = ExpansionSteps(map, triangleIndex, context.Profile);
            if (remaining[triangleIndex] > 0)
            {
                frontier.Add(triangleIndex);
            }
        }

        ExpandChannels(map, context.Profile, frontier, remaining);
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var strength = map.RiverChannelStrength[triangleIndex];
            var channelOrder = map.RiverChannelOrders[triangleIndex];
            if (!ChannelStateIsValid(map, triangleIndex, strength, channelOrder) ||
                !SpineCoverageIsValid(map, triangleIndex, strength, channelOrder))
            {
                throw new InvalidOperationException("River channel coverage is inconsistent with its flow spine.");
            }
        }
    }

    private static bool ChannelStateIsValid(
        PhysicalGeographyField map,
        int triangleIndex,
        int strength,
        int channelOrder) =>
        strength is >= 0 and <= 1000 && channelOrder >= 0 &&
        (strength == 0) == (channelOrder == 0) &&
        (strength == 0 || map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None);

    private static bool SpineCoverageIsValid(
        PhysicalGeographyField map,
        int triangleIndex,
        int strength,
        int channelOrder) =>
        map.RiverOrders[triangleIndex] <= 0 ||
        strength > 0 && channelOrder >= map.RiverOrders[triangleIndex];

    private static int SourceStrength(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile)
    {
        var dischargeMultiple = Math.Min(
            12,
            map.FlowAccumulation[triangleIndex] / Math.Max(1, profile.RiverAccumulationThreshold));
        return Math.Clamp(
            profile.RiverChannelBaseStrength +
                ((map.RiverOrders[triangleIndex] - 1) * profile.RiverChannelOrderStrength) +
                (dischargeMultiple * profile.RiverChannelDischargeStrength),
            1,
            1000);
    }

    private static int ExpansionSteps(
        PhysicalGeographyField map,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile)
    {
        var order = map.RiverOrders[triangleIndex];
        var dischargeMultiple = map.FlowAccumulation[triangleIndex] /
            Math.Max(1, profile.RiverAccumulationThreshold);
        if (order < profile.RiverChannelWideningOrder &&
            dischargeMultiple < profile.RiverChannelWideningDischargeMultiplier)
        {
            return 0;
        }

        var scale = Math.Max(
            order - profile.RiverChannelWideningOrder,
            dischargeMultiple / profile.RiverChannelWideningDischargeMultiplier);
        return Math.Min(profile.RiverChannelMaximumExpansionSteps, 1 + (scale / 3));
    }

    private static void ExpandChannels(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        List<int> frontier,
        int[] remaining)
    {
        var queued = new bool[map.Grid.TriangleCount];
        while (frontier.Count > 0)
        {
            var next = new List<int>();
            foreach (var triangleIndex in frontier)
            {
                for (var edge = 0; edge < 3; edge++)
                {
                    var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
                    if (TryWiden(map, profile, triangleIndex, neighbor, remaining) &&
                        !queued[neighbor])
                    {
                        queued[neighbor] = true;
                        next.Add(neighbor);
                    }
                }
            }

            foreach (var triangleIndex in next)
            {
                queued[triangleIndex] = false;
            }

            frontier = next;
        }
    }

    private static bool TryWiden(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int source,
        int target,
        int[] remaining)
    {
        if (!CanWidenInto(map, source, target))
        {
            return false;
        }

        var strength = WidenedStrength(map, profile, source, target);
        var order = Math.Max(map.RiverChannelOrders[source], map.RiverOrders[target]);
        if (strength < map.RiverChannelStrength[target] ||
            strength == map.RiverChannelStrength[target] && order <= map.RiverChannelOrders[target])
        {
            return false;
        }

        map.RiverChannelStrength[target] = strength;
        map.RiverChannelOrders[target] = order;
        remaining[target] = remaining[source] - 1;
        return remaining[target] > 0;
    }

    private static bool CanWidenInto(PhysicalGeographyField map, int source, int target) =>
        target >= 0 &&
        map.WaterKindCodes[target] == PhysicalWaterKindCodes.None &&
        map.DrainageBasinIds[target] == map.DrainageBasinIds[source];

    private static int WidenedStrength(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int source,
        int target)
    {
        var reliefPenalty = Math.Abs(map.Elevation[source] - map.Elevation[target]);
        var slopePenalty = Math.Max(0, map.Slope[target] - profile.PlainSlopeThreshold);
        return Math.Max(
            1,
            map.RiverChannelStrength[source] - profile.RiverChannelWidthDecay -
                reliefPenalty - slopePenalty);
    }
}
