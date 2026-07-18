namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class FluvialErosionLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "ancestral-fluvial-erosion",
        0,
        PhysicalGeographyFacts.Elevation | PhysicalGeographyFacts.Ocean,
        PhysicalGeographyFacts.Elevation,
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Bathymetry |
        PhysicalGeographyFacts.Landmasses |
        PhysicalGeographyFacts.Lakes |
        PhysicalGeographyFacts.FlowRouting |
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        for (var iteration = 0; iteration < context.Profile.FluvialErosionIterations; iteration++)
        {
            _ = PriorityFlood.FillFromOcean(map);
            var targets = ConditionedFlowResolver.ResolveLandTargets(map);
            var accumulation = AccumulateContributingArea(map, targets);
            var original = map.Elevation.ToArray();
            for (var triangleIndex = 0; triangleIndex < targets.Length; triangleIndex++)
            {
                var target = targets[triangleIndex];
                if (target < 0 || map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
                {
                    continue;
                }

                var fall = Math.Max(0, map.DrainageElevations[triangleIndex] - map.DrainageElevations[target]);
                var streamPower = Math.Pow(accumulation[triangleIndex], 0.42d) * Math.Sqrt(fall + 1d);
                var cut = (int)Math.Round(
                    Math.Min(22d, streamPower * context.Profile.FluvialErosionStrength),
                    MidpointRounding.AwayFromZero);
                map.Elevation[triangleIndex] = original[triangleIndex] - cut;
            }
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        foreach (var value in map.Elevation)
        {
            if (value is < -2000 or > 2000)
            {
                throw new InvalidOperationException("Fluvial erosion produced an invalid elevation.");
            }
        }
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
