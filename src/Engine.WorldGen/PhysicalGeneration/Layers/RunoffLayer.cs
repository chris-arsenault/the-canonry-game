namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class RunoffLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "runoff",
        0,
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Temperature |
        PhysicalGeographyFacts.Precipitation,
        PhysicalGeographyFacts.Runoff,
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.LandIce |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None)
            {
                map.Runoff[triangleIndex] = 0;
                continue;
            }

            var snowMelt = Math.Clamp(
                (map.Temperature[triangleIndex] -
                    (context.Profile.SnowTemperatureThreshold - context.Profile.SnowTemperatureTransition)) *
                1000 / context.Profile.SnowTemperatureTransition,
                0,
                1000);
            map.Runoff[triangleIndex] = Math.Clamp(
                map.Rainfall[triangleIndex] +
                    (map.Snowfall[triangleIndex] * snowMelt / 1000),
                0,
                1000);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var value = map.Runoff[triangleIndex];
            if (value is < 0 or > 1000 ||
                (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None && value != 0))
            {
                throw new InvalidOperationException("Runoff is out of range.");
            }
        }
    }
}
