namespace Engine.WorldGen;

internal sealed class LandscapeInitializationLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "landscape-initialization",
        0,
        PhysicalGeographyFacts.TectonicProvinces | PhysicalGeographyFacts.Elevation,
        PhysicalGeographyFacts.LandscapeHistory);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        map.SedimentThickness.Clear();
        map.SedimentSupply.Clear();
        map.CumulativeFluvialErosion.Clear();
        map.CumulativeGlacialErosion.Clear();
        map.CumulativeCoastalErosion.Clear();
        map.CumulativeDeposition.Clear();
        map.MaximumIceThickness.Clear();
        map.MaximumIceFlux.Clear();
        map.WaveExposureHistory.Clear();
        map.GeothermalHistory.Clear();
        map.ExternalHeatForcing.Clear();
        map.ExternalCoastalForcing.Clear();
        map.ExternalCoastalForcingHistory.Clear();
        map.CompletedEpochCount = 0;
        map.SedimentProducedTotal = 0;
        map.SedimentDepositedTotal = 0;
        map.SedimentExportedTotal = 0;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            map.BedrockElevation[triangleIndex] = map.Elevation[triangleIndex];
            map.TectonicActivityHistory[triangleIndex] = map.TectonicStrain[triangleIndex];
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        LandscapeSurface.Validate(map);
    }
}
