namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class FluvialErosionLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "ancestral-fluvial-erosion",
        0,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.LandscapeHistory,
        PhysicalGeographyFacts.Elevation | PhysicalGeographyFacts.SedimentSupply,
        // Sediment and glaciation consume the beginning-of-epoch climate and
        // drainage graph before the post-glacial reconciliation.
        PhysicalGeographyFacts.SedimentTransport |
        PhysicalGeographyFacts.CoastalEvolution |
        PhysicalGeographyFacts.GlacialEvolution |
        PhysicalGeographyFacts.LandIce |
        PhysicalGeographyFacts.IceFlow |
        PhysicalGeographyFacts.SeaIce |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.RiverGeometry |
        PhysicalGeographyFacts.Floodplains |
        PhysicalGeographyFacts.Deltas |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        var iterations = context.HydrologyStrength == 0
            ? 0
            : Math.Max(
                1,
                context.Profile.FluvialErosionIterations * context.HydrologyStrength / 1000);
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            PriorityFlood.FillFromOcean(map, context.Seed ^ iteration);
            var targets = ConditionedFlowResolver.ResolveLandTargets(
                map,
                context.Profile,
                context.Seed ^ iteration);
            var accumulation = AccumulateContributingArea(map, targets);
            for (var triangleIndex = 0; triangleIndex < targets.Length; triangleIndex++)
            {
                var target = targets[triangleIndex];
                if (target < 0 || map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
                {
                    continue;
                }

                var fallEdge = map.Grid.EdgeLeadingTo(triangleIndex, target);
                var fallScale = fallEdge >= 0
                    ? map.Grid.TriangleEdgeSlopeScale(triangleIndex, fallEdge)
                    : 1f;
                var fall = Math.Max(
                    0d,
                    (map.DrainageElevations[triangleIndex] - map.DrainageElevations[target]) *
                        fallScale / PhysicalElevation.SubunitsPerUnit);
                var streamPower = Math.Pow(accumulation[triangleIndex], 0.42d) * Math.Sqrt(fall + 1d);

                // Hard rock wears slower, which is what holds up escarpments,
                // gorge walls, waterfall lips and summits that stand clear of
                // their surroundings. Without it a landscape dissects into
                // even roughness no matter how much relief it started with.
                var resistance = SurfaceResistance(map, triangleIndex);
                var cut = PhysicalElevation.FromUnits(
                    Math.Min(
                        22d,
                        streamPower * context.Profile.FluvialErosionStrength *
                            PhysicalRock.BaselineResistance / resistance) *
                        context.HydrologyStrength / 1000d);
                if (cut <= 0)
                {
                    continue;
                }

                _ = LandscapeSurface.Erode(map, triangleIndex, cut);
                map.CumulativeFluvialErosion[triangleIndex] = checked(
                    map.CumulativeFluvialErosion[triangleIndex] + cut);
                map.SedimentSupply[triangleIndex] = checked(
                    map.SedimentSupply[triangleIndex] + cut);
                map.SedimentProducedTotal = checked(map.SedimentProducedTotal + cut);
            }
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        LandscapeSurface.Validate(map);
        foreach (var value in map.Elevation)
        {
            if (value < PhysicalElevation.FromUnits(-2000) ||
                value > PhysicalElevation.FromUnits(2000))
            {
                throw new InvalidOperationException("Fluvial erosion produced an invalid elevation.");
            }
        }
    }

    /// <summary>
    /// What the water is actually cutting. Loose cover governs while it is
    /// there: an ejecta apron is debris, so it strips back off and re-exposes
    /// the hard ring beneath rather than protecting the ground it landed on.
    /// </summary>
    private static double SurfaceResistance(PhysicalGeographyField map, int triangleIndex)
    {
        var cover = Math.Clamp(
            map.SedimentThickness[triangleIndex] /
                (double)PhysicalElevation.FromUnits(PhysicalRock.SedimentCoverUnits),
            0d,
            1d);
        var rock = Math.Max(1, map.RockResistance[triangleIndex]);
        return Math.Max(1d, rock + ((PhysicalRock.SedimentResistance - rock) * cover));
    }

    private static int[] AccumulateContributingArea(PhysicalGeographyField map, int[] targets)
    {
        var indegree = new int[targets.Length];
        for (var index = 0; index < targets.Length; index++)
        {
            if (targets[index] >= 0)
            {
                indegree[targets[index]]++;
            }
        }

        var queue = new Queue<int>();
        for (var index = 0; index < indegree.Length; index++)
        {
            if (indegree[index] == 0)
            {
                queue.Enqueue(index);
            }
        }

        var accumulation = new int[targets.Length];
        Array.Fill(accumulation, 1);
        var visited = 0;
        while (queue.TryDequeue(out var triangleIndex))
        {
            visited++;
            var target = targets[triangleIndex];
            if (target >= 0)
            {
                accumulation[target] = FlowGraphAlgorithms.SaturatingAdd(
                    accumulation[target],
                    accumulation[triangleIndex]);
                if (--indegree[target] == 0)
                {
                    queue.Enqueue(target);
                }
            }
        }

        if (visited != targets.Length)
        {
            throw new InvalidOperationException("Ancestral drainage contains a cycle.");
        }

        return accumulation;
    }
}
