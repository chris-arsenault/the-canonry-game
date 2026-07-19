namespace Engine.Projection.Tests.Projection;

using Engine.Constants;
using Engine.Domain.Startup;

public sealed class PhysicalGeographyProjectionTests
{
    [Fact]
    public void FromBlueprint_PreservesCompleteLatentFieldForReadOnlyLookup()
    {
        var coords = HexCoord.CoordsInRadius(new HexCoord(0, 0), radius: 1).ToArray();
        var cells = coords
            .Select((coord, index) => Cell(coord, index * PhysicalGeographyCodes.MicrotrianglesPerHex))
            .ToArray();
        var triangles = Enumerable.Range(
                0,
                coords.Length * PhysicalGeographyCodes.MicrotrianglesPerHex)
            .Select(index => Triangle(index))
            .ToArray();
        var blueprint = new DomainPhysicalGeographyBlueprint(
            Radius: 1,
            GenerationEpochCount: 4,
            MaterialPalette: ["tundra_soil"],
            SurfacePalette: ["tundra"],
            FeaturePalette: [],
            ScalarFields:
            [
                new DomainPhysicalScalarField(
                    "fire_history",
                    Enumerable.Range(0, triangles.Length).ToArray()),
            ],
            Cells: cells,
            Microtriangles: triangles);

        var projection = PhysicalGeographyProjection.FromBlueprint(blueprint);

        Assert.True(projection.HasData);
        Assert.Equal(1, projection.Radius);
        Assert.Equal(4, projection.GenerationEpochCount);
        Assert.Equal(7, projection.Cells.Count);
        Assert.Equal("tundra_soil", projection.MaterialName(0));
        Assert.True(projection.TryGetCell(new HexCoord(1, -1), out var cell));
        Assert.Equal("tundra", cell.BaseTerrainStableName);
        Assert.Equal(4, cell.RiverCoverage);
        Assert.Equal(2, cell.FloodplainCoverage);
        Assert.Equal(1, cell.DeltaCoverage);
        Assert.Equal(3, cell.WideRiverCoverage);
        Assert.Equal(PhysicalGeographyCodes.MicrotrianglesPerHex, projection.MicrotrianglesFor(cell).Length);
        Assert.Equal(cell.MicrotriangleOffset, projection.MicrotrianglesFor(cell)[0].Elevation);
        Assert.Equal(500, projection.MicrotrianglesFor(cell)[0].RiverChannelStrength);
        Assert.Equal(100, projection.MicrotrianglesFor(cell)[0].DeltaIntensity);
        Assert.True(projection.TryGetScalarValue("fire_history", cell.MicrotriangleOffset, out var fireHistory));
        Assert.Equal(cell.MicrotriangleOffset, fireHistory);
        Assert.False(projection.TryGetCell(new HexCoord(2, 0), out _));
    }

    private static DomainPhysicalGeographyCell Cell(HexCoord coord, int offset) =>
        new(
            coord.Q,
            coord.R,
            BaseTerrain: "tundra",
            SuperregionId: 1,
            ContinentId: 1,
            MicrotriangleOffset: offset,
            DominantMaterialIndex: 0,
            PrimarySurfaceIndex: 0,
            SignificantFeatureFlags: 0,
            MeanElevation: offset,
            MinElevation: offset,
            MaxElevation: offset + 23,
            Relief: 23,
            Roughness: 1,
            MeanTemperature: 0,
            MeanMoisture: 0,
            MeanGeothermal: 0,
            LandCoverage: 24,
            WaterCoverage: 0,
            OceanCoverage: 0,
            LakeCoverage: 0,
            PermanentIceCoverage: 0,
            RiverCoverage: 4,
            FloodplainCoverage: 2,
            DeltaCoverage: 1,
            WideRiverCoverage: 3,
            PeakCount: 0,
            MeanIceThickness: 0,
            MaxIceThickness: 0,
            MeanFireIntensity: 0,
            MaxFireIntensity: 0,
            MeanAuroraIntensity: 0,
            MaxAuroraIntensity: 0,
            MeanPressureIntensity: 0,
            MaxPressureIntensity: 0,
            RiverEdgeMask: 0,
            CoastEdgeMask: 0,
            RidgeEdgeMask: 0,
            CliffEdgeMask: 0,
            PassEdgeMask: 0,
            FractureEdgeMask: 0);

    private static DomainPhysicalMicrotriangle Triangle(int index) =>
        new(
            MaterialIndex: 0,
            BaseSurfaceIndex: 0,
            SurfaceIndex: 0,
            SignificantFeatureFlags: 0,
            LandformCode: PhysicalLandformCodes.Plain,
            Elevation: index,
            Slope: 0,
            Temperature: 0,
            Moisture: 0,
            Geothermal: 0,
            LandmassId: 1,
            LandmassClassCode: PhysicalLandmassClassCodes.Continent,
            ContinentalGroupId: 1,
            BathymetricZoneCode: PhysicalBathymetricZoneCodes.Land,
            WaterKindCode: PhysicalWaterKindCodes.None,
            WaterDepth: 0,
            WaterBodyId: 0,
            DrainageBasinId: 1,
            FlowEdge: PhysicalGeographyCodes.NoFlowEdge,
            FlowAccumulation: 0,
            RiverOrder: 0,
            RiverSpineEdgeMask: 0,
            RiverChannelStrength: 500,
            RiverChannelOrder: 2,
            FloodplainIntensity: 0,
            DeltaIntensity: 100,
            IceThickness: 0,
            IceFlux: 0,
            FractureConductance: 0,
            HeatSourceDensity: 0,
            FeatureFlags: 0);
}
