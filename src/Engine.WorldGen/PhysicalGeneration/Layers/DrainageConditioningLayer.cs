namespace Engine.WorldGen;

internal sealed class DrainageConditioningLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "drainage-conditioning",
        0,
        PhysicalGeographyFacts.Elevation | PhysicalGeographyFacts.Ocean,
        PhysicalGeographyFacts.DrainageConditioning,
        PhysicalGeographyFacts.Lakes |
        PhysicalGeographyFacts.FlowRouting |
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        _ = PriorityFlood.FillFromOcean(map);
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.DrainageElevations[triangleIndex] < map.Elevation[triangleIndex])
            {
                throw new InvalidOperationException("Conditioned drainage lowered the physical floor.");
            }
        }
    }
}
