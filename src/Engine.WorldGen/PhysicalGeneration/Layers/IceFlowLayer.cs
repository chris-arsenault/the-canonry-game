namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class IceFlowLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "ice-flow",
        0,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.LandIce,
        PhysicalGeographyFacts.IceFlow,
        PhysicalGeographyFacts.GlacialMeltwater |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        map.IceFlux.Clear();
        map.IceFlowTargets.Fill(-1);
        var surface = new long[map.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < surface.Length; triangleIndex++)
        {
            surface[triangleIndex] = map.Elevation[triangleIndex] +
                PhysicalElevation.FromUnits(map.IceThickness[triangleIndex] / 3d);
            map.IceFlux[triangleIndex] = map.IceThickness[triangleIndex];
        }

        for (var triangleIndex = 0; triangleIndex < surface.Length; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            var best = -1;
            var bestHeight = surface[triangleIndex];
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0 && surface[neighbor] < bestHeight)
                {
                    best = neighbor;
                    bestHeight = surface[neighbor];
                }
            }

            map.IceFlowTargets[triangleIndex] = best;
        }

        var order = IntegerKeyOrdering.DescendingThenIndex(surface);
        foreach (var triangleIndex in order)
        {
            var target = map.IceFlowTargets[triangleIndex];
            if (target >= 0)
            {
                map.IceFlux[target] = FlowGraphAlgorithms.SaturatingAdd(
                    map.IceFlux[target],
                    map.IceFlux[triangleIndex]);
            }
        }

        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            map.MaximumIceFlux[triangleIndex] = Math.Max(
                map.MaximumIceFlux[triangleIndex],
                map.IceFlux[triangleIndex]);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.IceFlux[triangleIndex] < map.IceThickness[triangleIndex])
            {
                throw new InvalidOperationException("Ice flux lost local accumulated ice.");
            }
        }
    }
}
