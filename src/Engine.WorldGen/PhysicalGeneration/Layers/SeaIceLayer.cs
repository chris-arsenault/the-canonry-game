namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class SeaIceLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "sea-ice",
        0,
        PhysicalGeographyFacts.Ocean | PhysicalGeographyFacts.Temperature | PhysicalGeographyFacts.LandIce,
        PhysicalGeographyFacts.SeaIce,
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        var ocean = new List<int>();
        var suitability = new List<int>();
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            map.FeatureFlags[triangleIndex] &=
                ~(PhysicalGeographyFeatureCodes.PermanentSeaIce |
                    PhysicalGeographyFeatureCodes.PermanentIceMargin);
            if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean &&
                map.Grid.TriangleIsInsideConfiguredDisk(triangleIndex))
            {
                map.IceThickness[triangleIndex] = 0;
                ocean.Add(triangleIndex);
                suitability.Add(1000 - map.Temperature[triangleIndex]);
            }
        }

        if (ocean.Count == 0)
        {
            throw new InvalidOperationException("Sea-ice generation requires ocean.");
        }

        var targetCount = Math.Clamp(
            (int)Math.Round(
                ocean.Count * context.Profile.QualityTargets.SeaIceCoverage.Target,
                MidpointRounding.AwayFromZero),
            1,
            ocean.Count);
        var order = IntegerKeyOrdering.DescendingThenIndex(
            System.Runtime.InteropServices.CollectionsMarshal.AsSpan(suitability));
        var cutoff = suitability[order[targetCount - 1]];
        for (var selectionIndex = 0; selectionIndex < targetCount; selectionIndex++)
        {
            var triangleIndex = ocean[order[selectionIndex]];
            var coldSuitability = 1000 - map.Temperature[triangleIndex];
            map.IceThickness[triangleIndex] = Math.Clamp(
                50 + ((coldSuitability - cutoff) * 2),
                50,
                240);
            map.FeatureFlags[triangleIndex] |=
                map.Temperature[triangleIndex] <= context.Profile.PermanentSeaIceTemperature
                    ? PhysicalGeographyFeatureCodes.PermanentSeaIce
                    : PhysicalGeographyFeatureCodes.PermanentIceMargin;
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if ((map.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.PermanentSeaIce) != 0 &&
                map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
            {
                throw new InvalidOperationException("Permanent sea ice was assigned off ocean.");
            }
        }
    }
}
