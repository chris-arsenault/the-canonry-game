namespace IceRemembers.Seed;

internal sealed class FireClimateFeedbackLayer : IIceRemembersMicrogeographyLayer
{
    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "fire-climate-feedback",
        0,
        IceRemembersMicrogeographyFacts.FireField,
        IceRemembersMicrogeographyFacts.FireClimate,
        IceRemembersMicrogeographyFacts.AuroraField |
        IceRemembersMicrogeographyFacts.AuroraFeatures |
        IceRemembersMicrogeographyFacts.Interactions |
        IceRemembersMicrogeographyFacts.FinalSurfaces |
        IceRemembersMicrogeographyFacts.Validated);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            var fire = map.FireIntensity[triangleIndex];
            if (fire <= 0)
            {
                continue;
            }

            map.Physical.Temperature[triangleIndex] = Math.Clamp(
                map.Physical.Temperature[triangleIndex] +
                    (fire * context.Profile.FireTemperatureFeedbackPercent / 100),
                0,
                1000);
            map.Physical.IceThickness[triangleIndex] = Math.Max(
                0,
                map.Physical.IceThickness[triangleIndex] -
                    (fire / context.Profile.FireIceMeltDivisor));
        }
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            if (map.Physical.Temperature[triangleIndex] is < 0 or > 1000 ||
                map.Physical.IceThickness[triangleIndex] < 0)
            {
                throw new InvalidOperationException("Fire climate feedback produced an invalid climate value.");
            }
        }
    }
}
