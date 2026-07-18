namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class RunoffLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "runoff",
        0,
        PhysicalGeographyFacts.Temperature | PhysicalGeographyFacts.Moisture,
        PhysicalGeographyFacts.Runoff,
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.LandIce |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None)
            {
                map.Runoff[triangleIndex] = 1000;
                continue;
            }

            var melt = 260 + (map.Temperature[triangleIndex] * 740 / 1000);
            map.Runoff[triangleIndex] = Math.Max(1, map.Moisture[triangleIndex] * melt / 1000);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        foreach (var value in map.Runoff)
        {
            if (value <= 0 || value > 1000)
            {
                throw new InvalidOperationException("Runoff is out of range.");
            }
        }
    }
}
