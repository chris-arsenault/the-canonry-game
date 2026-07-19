namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class GlacialErosionLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "glacial-erosion",
        0,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.LandscapeHistory |
        PhysicalGeographyFacts.LandIce |
        PhysicalGeographyFacts.IceFlow,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.SedimentSupply |
        PhysicalGeographyFacts.GlacialEvolution,
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Bathymetry |
        PhysicalGeographyFacts.Landmasses |
        PhysicalGeographyFacts.OceanCurrents |
        PhysicalGeographyFacts.Warmth |
        PhysicalGeographyFacts.Temperature |
        PhysicalGeographyFacts.Moisture |
        PhysicalGeographyFacts.Precipitation |
        PhysicalGeographyFacts.Runoff |
        PhysicalGeographyFacts.Lakes |
        PhysicalGeographyFacts.FlowRouting |
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.SeaIce |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.IceFlux[triangleIndex] < context.Profile.GlacierFluxThreshold ||
                map.IceThickness[triangleIndex] <= 0)
            {
                continue;
            }

            var power = 1 +
                ((map.IceFlux[triangleIndex] - context.Profile.GlacierFluxThreshold) /
                    Math.Max(1, context.Profile.GlacierFluxThreshold)) +
                (map.IceThickness[triangleIndex] / Math.Max(1, context.Profile.GlacierThicknessThreshold));
            var resistance = map.CrustTypeCodes[triangleIndex] ==
                PhysicalCrustTypeCodes.Continental ? 4 : 3;
            var cut = Math.Clamp(
                power * 3 / resistance,
                1,
                context.Profile.MaximumGlacialErosionPerPass);
            _ = LandscapeSurface.Erode(map, triangleIndex, cut);
            map.CumulativeGlacialErosion[triangleIndex] = checked(
                map.CumulativeGlacialErosion[triangleIndex] + cut);
            map.SedimentSupply[triangleIndex] = checked(map.SedimentSupply[triangleIndex] + cut);
            map.SedimentProducedTotal = checked(map.SedimentProducedTotal + cut);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        LandscapeSurface.Validate(map);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.CumulativeGlacialErosion[triangleIndex] > 0 &&
                map.MaximumIceFlux[triangleIndex] < context.Profile.GlacierFluxThreshold)
            {
                throw new InvalidOperationException("Glacial erosion lacks qualifying ice history.");
            }
        }
    }
}
