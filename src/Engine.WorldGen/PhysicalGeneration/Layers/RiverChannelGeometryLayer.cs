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
        var dominantUpstream = BuildDominantUpstream(map);
        var frontier = new Queue<ChannelFront>();
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var order = map.RiverOrders[triangleIndex];
            if (order <= 0)
            {
                continue;
            }

            map.RiverChannelStrength[triangleIndex] = SourceStrength(map, triangleIndex, context.Profile);
            map.RiverChannelOrders[triangleIndex] = order;
            var expansionSteps = ExpansionSteps(map, triangleIndex, context.Profile);
            if (expansionSteps > 0)
            {
                frontier.Enqueue(new ChannelFront(triangleIndex, triangleIndex, 0, expansionSteps));
            }
        }

        ExpandChannels(map, context.Profile, dominantUpstream, frontier);
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

    internal static int[] BuildDominantUpstream(PhysicalGeographyField map)
    {
        var upstream = new int[map.Grid.TriangleCount];
        Array.Fill(upstream, -1);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var target = map.FlowTargets[triangleIndex];
            if (map.RiverOrders[triangleIndex] <= 0 || target < 0 || map.RiverOrders[target] <= 0)
            {
                continue;
            }

            var previous = upstream[target];
            if (previous < 0 || map.FlowAccumulation[triangleIndex] > map.FlowAccumulation[previous])
            {
                upstream[target] = triangleIndex;
            }
        }

        return upstream;
    }

    private static void ExpandChannels(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int[] dominantUpstream,
        Queue<ChannelFront> frontier)
    {
        while (frontier.TryDequeue(out var front))
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var target = map.Grid.NeighborTriangleIndex(front.TriangleIndex, edge);
                if (TryWiden(map, profile, dominantUpstream, front, target))
                {
                    frontier.Enqueue(front.AdvanceTo(target));
                }
            }
        }
    }

    private static bool TryWiden(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int[] dominantUpstream,
        ChannelFront front,
        int target)
    {
        var depth = front.Depth + 1;
        if (depth > front.MaximumDepth || !CanWidenInto(
                map,
                profile,
                dominantUpstream,
                front.SpineIndex,
                target))
        {
            return false;
        }

        var strength = WidenedStrength(map, profile, front.SpineIndex, target, depth);
        var order = map.RiverChannelOrders[front.SpineIndex];
        if (strength < map.RiverChannelStrength[target] ||
            strength == map.RiverChannelStrength[target] && order <= map.RiverChannelOrders[target])
        {
            return false;
        }

        map.RiverChannelStrength[target] = strength;
        map.RiverChannelOrders[target] = order;
        return depth < front.MaximumDepth;
    }

    private static bool CanWidenInto(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int[] dominantUpstream,
        int spine,
        int target)
    {
        if (target < 0 || map.RiverOrders[target] > 0 ||
            map.WaterKindCodes[target] != PhysicalWaterKindCodes.None ||
            map.DrainageBasinIds[target] != map.DrainageBasinIds[spine] ||
            Math.Abs(map.Elevation[spine] - map.Elevation[target]) >
                PhysicalElevation.FromUnits(profile.RiverChannelMaximumBankRelief))
        {
            return false;
        }

        var alignment = LongitudinalAlignment(map, dominantUpstream, spine, target);
        return alignment <= profile.RiverChannelMaximumLongitudinalAlignment / 1000d;
    }

    internal static double LongitudinalAlignment(
        PhysicalGeographyField map,
        int[] dominantUpstream,
        int spine,
        int target)
    {
        FlowTangent(map, dominantUpstream, spine, out var tangentX, out var tangentY);
        Normalize(
            map.Grid.TriangleX(target) - map.Grid.TriangleX(spine),
            map.Grid.TriangleY(target) - map.Grid.TriangleY(spine),
            out var bankX,
            out var bankY);
        return Math.Abs((tangentX * bankX) + (tangentY * bankY));
    }

    private static void FlowTangent(
        PhysicalGeographyField map,
        int[] dominantUpstream,
        int spine,
        out double tangentX,
        out double tangentY)
    {
        tangentX = 0d;
        tangentY = 0d;
        var downstream = map.FlowTargets[spine];
        if (downstream >= 0)
        {
            AddUnitDirection(map, spine, downstream, ref tangentX, ref tangentY);
        }

        var upstream = dominantUpstream[spine];
        if (upstream >= 0)
        {
            AddUnitDirection(map, upstream, spine, ref tangentX, ref tangentY);
        }

        Normalize(tangentX, tangentY, out tangentX, out tangentY);
    }

    private static void AddUnitDirection(
        PhysicalGeographyField map,
        int source,
        int target,
        ref double totalX,
        ref double totalY)
    {
        Normalize(
            map.Grid.TriangleX(target) - map.Grid.TriangleX(source),
            map.Grid.TriangleY(target) - map.Grid.TriangleY(source),
            out var x,
            out var y);
        totalX += x;
        totalY += y;
    }

    private static void Normalize(double x, double y, out double normalizedX, out double normalizedY)
    {
        var length = Math.Max(0.000001d, Math.Sqrt((x * x) + (y * y)));
        normalizedX = x / length;
        normalizedY = y / length;
    }

    private static int WidenedStrength(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int spine,
        int target,
        int depth)
    {
        var reliefPenalty = PhysicalElevation.ToUnits(
            Math.Abs(map.Elevation[spine] - map.Elevation[target]));
        var slopePenalty = Math.Max(
            0,
            PhysicalElevation.ToUnits(map.Slope[target]) - profile.PlainSlopeThreshold);
        return Math.Max(
            1,
            map.RiverChannelStrength[spine] - (profile.RiverChannelWidthDecay * depth) -
                reliefPenalty - slopePenalty);
    }

    private readonly record struct ChannelFront(
        int TriangleIndex,
        int SpineIndex,
        int Depth,
        int MaximumDepth)
    {
        public ChannelFront AdvanceTo(int triangleIndex) => this with
        {
            TriangleIndex = triangleIndex,
            Depth = Depth + 1,
        };
    }
}
