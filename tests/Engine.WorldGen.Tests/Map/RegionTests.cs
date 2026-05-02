namespace Engine.WorldGen.Tests.Map;

using Engine.WorldGen;

public class RegionTests
{
    private static (HexGrid Grid, MapCoverageMap Map) CreateTestSetup()
    {
        var grid = WorldGenTestData.Grid(3);
        var regionMap = new MapCoverageMap();
        var regionA = new MapCoverage("Highlands", 100);
        var regionB = new MapCoverage("Lowlands", 101);
        var regionC = new MapCoverage("Coast", 102);

        regionMap.RegisterCoverage(regionA);
        regionMap.RegisterCoverage(regionB);
        regionMap.RegisterCoverage(regionC);

        foreach (var cell in grid.AllCells)
        {
            if (cell.Coord.Q > 0)
            {
                regionMap.AssignCell(cell.Coord, regionA);
            }
            else if (cell.Coord.Q < 0)
            {
                regionMap.AssignCell(cell.Coord, regionC);
            }
            else
            {
                regionMap.AssignCell(cell.Coord, regionB);
            }
        }

        return (grid, regionMap);
    }

    [Fact]
    public void AllCells_AssignedToExactlyOneRegion()
    {
        var (grid, regionMap) = CreateTestSetup();

        foreach (var cell in grid.AllCells)
        {
            Assert.NotNull(regionMap.GetCoverageForCell(cell.Coord));
        }
    }

    [Fact]
    public void AssignCell_RejectsDoubleAssignment()
    {
        var regionMap = new MapCoverageMap();
        var regionA = new MapCoverage("A", 1);
        var regionB = new MapCoverage("B", 2);
        regionMap.RegisterCoverage(regionA);
        regionMap.RegisterCoverage(regionB);

        var coord = new HexCoord(0, 0);
        regionMap.AssignCell(coord, regionA);

        Assert.Throws<InvalidOperationException>(() => regionMap.AssignCell(coord, regionB));
    }

    [Fact]
    public void AssignCell_SameRegionTwice_Succeeds()
    {
        var regionMap = new MapCoverageMap();
        var region = new MapCoverage("A", 1);
        regionMap.RegisterCoverage(region);

        var coord = new HexCoord(0, 0);
        regionMap.AssignCell(coord, region);
        regionMap.AssignCell(coord, region);

        Assert.Same(region, regionMap.GetCoverageForCell(coord));
    }

    [Fact]
    public void GetRegionForCell_ReturnsCorrectRegion()
    {
        var (_, regionMap) = CreateTestSetup();

        var region = regionMap.GetCoverageForCell(new HexCoord(2, 0));

        Assert.NotNull(region);
        Assert.Equal("Highlands", region.Name);
    }

    [Fact]
    public void GetRegionForCell_ReturnsNullForUnassigned()
    {
        var regionMap = new MapCoverageMap();
        Assert.Null(regionMap.GetCoverageForCell(new HexCoord(99, 99)));
    }

    [Fact]
    public void GetRegion_ByEntityId()
    {
        var (_, regionMap) = CreateTestSetup();

        var region = regionMap.GetCoverage(100);

        Assert.NotNull(region);
        Assert.Equal("Highlands", region.Name);
    }

    [Fact]
    public void GetRegionByName_Works()
    {
        var (_, regionMap) = CreateTestSetup();

        var region = regionMap.GetCoverageByName("Coast");

        Assert.NotNull(region);
        Assert.Equal(102, region.OwnerEntityId);
    }

    [Fact]
    public void Region_CellCount_Correct()
    {
        var (grid, regionMap) = CreateTestSetup();

        var total = regionMap.AllCoverages.Sum(r => r.CellCount);

        Assert.Equal(grid.CellCount, total);
    }

    [Fact]
    public void ComputeAdjacency_FindsAdjacentRegions()
    {
        var (_, regionMap) = CreateTestSetup();

        var adjacency = regionMap.ComputeAdjacency().ToList();
        var idPairs = adjacency.Select(p => (
            regionMap.GetCoverage(p.A)!.Name,
            regionMap.GetCoverage(p.B)!.Name)).ToList();

        Assert.True(adjacency.Count >= 2);
        Assert.Contains(idPairs, p =>
            (p.Item1 == "Highlands" && p.Item2 == "Lowlands") ||
            (p.Item1 == "Lowlands" && p.Item2 == "Highlands"));
    }

    [Fact]
    public void ComputeAdjacency_NoDuplicatePairs()
    {
        var (_, regionMap) = CreateTestSetup();

        var adjacency = regionMap.ComputeAdjacency().ToList();

        Assert.Equal(adjacency.Count, adjacency.Distinct().Count());
    }

    [Fact]
    public void ComputeAggregates_DominantTerrain()
    {
        var grid = WorldGenTestData.Grid(2);
        var regionMap = new MapCoverageMap();
        var region = new MapCoverage("Crags", 1);
        regionMap.RegisterCoverage(region);

        foreach (var cell in grid.AllCells)
        {
            regionMap.AssignCell(cell.Coord, region);
            grid.SetCell(cell.Coord, WorldGenTestData.Crags);
        }

        grid.SetCell(new HexCoord(0, 0), WorldGenTestData.Tundra);

        var agg = MapCoverageMap.ComputeAggregates(region, grid);

        Assert.Equal(WorldGenTestData.Crags, agg.DominantTerrain);
        Assert.Equal(grid.CellCount, agg.CellCount);
    }
}
