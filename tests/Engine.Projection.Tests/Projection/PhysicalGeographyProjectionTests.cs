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
            MaterialPalette: ["tundra_soil"],
            Cells: cells,
            Microtriangles: triangles);

        var projection = PhysicalGeographyProjection.FromBlueprint(blueprint);

        Assert.True(projection.HasData);
        Assert.Equal(1, projection.Radius);
        Assert.Equal(7, projection.Cells.Count);
        Assert.Equal("tundra_soil", projection.MaterialName(0));
        Assert.True(projection.TryGetCell(new HexCoord(1, -1), out var cell));
        Assert.Equal("tundra", cell.BaseTerrainStableName);
        Assert.Equal(PhysicalGeographyCodes.MicrotrianglesPerHex, projection.MicrotrianglesFor(cell).Length);
        Assert.Equal(cell.MicrotriangleOffset, projection.MicrotrianglesFor(cell)[0].Elevation);
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
            RiverCoverage: 0,
            PeakCount: 0,
            RiverEdgeMask: 0,
            CoastEdgeMask: 0,
            RidgeEdgeMask: 0,
            CliffEdgeMask: 0,
            PassEdgeMask: 0);

    private static DomainPhysicalMicrotriangle Triangle(int index) =>
        new(
            MaterialIndex: 0,
            LandformCode: PhysicalLandformCodes.Plain,
            Elevation: index,
            Temperature: 0,
            Moisture: 0,
            Geothermal: 0,
            WaterKindCode: PhysicalWaterKindCodes.None,
            WaterDepth: 0,
            WaterBodyId: 0,
            DrainageBasinId: 1,
            FlowEdge: PhysicalGeographyCodes.NoFlowEdge,
            FlowAccumulation: 0,
            RiverOrder: 0,
            FeatureFlags: 0);
}
