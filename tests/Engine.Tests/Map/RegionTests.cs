namespace Engine.Tests.Map;

using Engine.Graph;
using Engine.Map;

public class RegionTests
{
    private static (HexGrid Grid, MapCoverageMap Map) CreateTestSetup()
    {
        var grid = new HexGrid(3);
        var regionMap = new MapCoverageMap();

        // Create 3 regions splitting the grid roughly into thirds
        var regionA = new MapCoverage("Highlands", new EntityId(100));
        var regionB = new MapCoverage("Lowlands", new EntityId(101));
        var regionC = new MapCoverage("Coast", new EntityId(102));

        regionMap.RegisterCoverage(regionA);
        regionMap.RegisterCoverage(regionB);
        regionMap.RegisterCoverage(regionC);

        // Assign cells: positive Q = Highlands, negative Q = Coast, center = Lowlands
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

    // --- Region Assignment ---

    [Fact]
    public void AllCells_AssignedToExactlyOneRegion()
    {
        var (grid, regionMap) = CreateTestSetup();

        foreach (var cell in grid.AllCells)
        {
            var region = regionMap.GetCoverageForCell(cell.Coord);
            Assert.NotNull(region);
        }
    }

    [Fact]
    public void AssignCell_RejectsDoubleAssignment()
    {
        var regionMap = new MapCoverageMap();
        var regionA = new MapCoverage("A", new EntityId(1));
        var regionB = new MapCoverage("B", new EntityId(2));
        regionMap.RegisterCoverage(regionA);
        regionMap.RegisterCoverage(regionB);

        var coord = new HexCoord(0, 0);
        regionMap.AssignCell(coord, regionA);

        Assert.Throws<InvalidOperationException>(() =>
            regionMap.AssignCell(coord, regionB));
    }

    [Fact]
    public void AssignCell_SameRegionTwice_Succeeds()
    {
        var regionMap = new MapCoverageMap();
        var region = new MapCoverage("A", new EntityId(1));
        regionMap.RegisterCoverage(region);

        var coord = new HexCoord(0, 0);
        regionMap.AssignCell(coord, region);
        regionMap.AssignCell(coord, region); // Idempotent
    }

    // --- Lookups ---

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

        var region = regionMap.GetCoverage(new EntityId(100));
        Assert.NotNull(region);
        Assert.Equal("Highlands", region.Name);
    }

    [Fact]
    public void GetRegionByName_Works()
    {
        var (_, regionMap) = CreateTestSetup();

        var region = regionMap.GetCoverageByName("Coast");
        Assert.NotNull(region);
        Assert.Equal(new EntityId(102), region.OwnerEntityId);
    }

    [Fact]
    public void Region_CellCount_Correct()
    {
        var (grid, regionMap) = CreateTestSetup();

        var total = regionMap.AllCoverages.Sum(r => r.CellCount);
        Assert.Equal(grid.CellCount, total);
    }

    // --- Adjacency ---

    [Fact]
    public void ComputeAdjacency_FindsAdjacentRegions()
    {
        var (_, regionMap) = CreateTestSetup();

        var adjacency = regionMap.ComputeAdjacency().ToList();

        // Highlands (Q>0) borders Lowlands (Q=0), Lowlands borders Coast (Q<0)
        // Highlands and Coast may or may not border each other depending on grid geometry
        Assert.True(adjacency.Count >= 2); // At least Highlands-Lowlands and Lowlands-Coast

        var idPairs = adjacency.Select(p => (
            regionMap.GetCoverage(p.A)!.Name,
            regionMap.GetCoverage(p.B)!.Name)).ToList();

        // Lowlands should border both Highlands and Coast
        Assert.Contains(idPairs, p =>
            (p.Item1 == "Highlands" && p.Item2 == "Lowlands") ||
            (p.Item1 == "Lowlands" && p.Item2 == "Highlands"));
    }

    [Fact]
    public void ComputeAdjacency_NoDuplicatePairs()
    {
        var (_, regionMap) = CreateTestSetup();

        var adjacency = regionMap.ComputeAdjacency().ToList();
        var uniqueCount = adjacency.Distinct().Count();
        Assert.Equal(adjacency.Count, uniqueCount);
    }

    // --- Aggregates ---

    [Fact]
    public void ComputeAggregates_CorrectAverageFertility()
    {
        var grid = new HexGrid(2);
        var regionMap = new MapCoverageMap();
        var region = new MapCoverage("Test", new EntityId(1));
        regionMap.RegisterCoverage(region);

        // Assign all cells to one region, set varied fertility
        var cells = grid.AllCells.ToList();
        foreach (var cell in cells)
        {
            regionMap.AssignCell(cell.Coord, region);
        }

        // Set some cells to different fertility
        grid.SetCell(new HexCoord(0, 0), TerrainType.Tundra, fertility: 1.0f);
        grid.SetCell(new HexCoord(1, 0), TerrainType.Tundra, fertility: 0.0f);

        var agg = regionMap.ComputeAggregates(region, grid);
        Assert.True(agg.AverageFertility > 0f);
        Assert.True(agg.AverageFertility < 1f);
        Assert.Equal(grid.CellCount, agg.CellCount);
    }

    [Fact]
    public void ComputeAggregates_DominantTerrain()
    {
        var grid = new HexGrid(2);
        var regionMap = new MapCoverageMap();
        var region = new MapCoverage("Crags", new EntityId(1));
        regionMap.RegisterCoverage(region);

        foreach (var cell in grid.AllCells)
        {
            regionMap.AssignCell(cell.Coord, region);
            grid.SetCell(cell.Coord, TerrainType.Crags);
        }

        // Set one cell to tundra
        grid.SetCell(new HexCoord(0, 0), TerrainType.Tundra);

        var agg = regionMap.ComputeAggregates(region, grid);
        Assert.Equal(TerrainType.Crags, agg.DominantTerrain);
    }

    [Fact]
    public void ComputeAggregates_CollectsResources()
    {
        var grid = new HexGrid(2);
        var regionMap = new MapCoverageMap();
        var region = new MapCoverage("Rich", new EntityId(1));
        regionMap.RegisterCoverage(region);

        foreach (var cell in grid.AllCells)
        {
            regionMap.AssignCell(cell.Coord, region);
        }

        grid.GetCell(new HexCoord(0, 0))!.Resources.Add("iron");
        grid.GetCell(new HexCoord(1, 0))!.Resources.Add("gold_ore");
        grid.GetCell(new HexCoord(1, 0))!.Resources.Add("iron"); // Duplicate resource, different cell

        var agg = regionMap.ComputeAggregates(region, grid);
        Assert.Contains("iron", agg.Resources);
        Assert.Contains("gold_ore", agg.Resources);
        Assert.Equal(2, agg.Resources.Count); // Deduplicated
    }
}
