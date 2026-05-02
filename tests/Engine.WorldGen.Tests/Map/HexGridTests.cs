namespace Engine.WorldGen.Tests.Map;

using Engine.WorldGen;

public class HexGridTests
{
    [Theory]
    [InlineData(1, 7)]
    [InlineData(3, 37)]
    [InlineData(5, 91)]
    public void Constructor_CreatesCorrectCellCount(int radius, int expectedCount)
    {
        var grid = WorldGenTestData.Grid(radius);
        Assert.Equal(expectedCount, grid.CellCount);
    }

    [Fact]
    public void Constructor_RejectsZeroRadius()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HexGrid(0, WorldGenTestData.TerrainCatalog(), WorldGenTestData.Wastes));
    }

    [Fact]
    public void GetCell_ReturnsNullOutsideGrid()
    {
        var grid = WorldGenTestData.Grid(2);
        Assert.Null(grid.GetCell(new HexCoord(10, 10)));
    }

    [Fact]
    public void GetCell_ReturnsCellInsideGrid()
    {
        var grid = WorldGenTestData.Grid(3);

        var cell = grid.GetCell(new HexCoord(0, 0));

        Assert.NotNull(cell);
        Assert.Equal(new HexCoord(0, 0), cell.Coord);
    }

    [Fact]
    public void AllCells_DefaultToConfiguredTerrain()
    {
        var grid = WorldGenTestData.Grid(2);
        Assert.All(grid.AllCells, c => Assert.Equal(WorldGenTestData.Wastes, c.Terrain.StableName));
    }

    [Fact]
    public void SetCell_ChangesTerrain()
    {
        var grid = WorldGenTestData.Grid(3);

        grid.SetCell(new HexCoord(1, 0), WorldGenTestData.Crags);

        var cell = grid.GetCell(new HexCoord(1, 0));
        Assert.NotNull(cell);
        Assert.Equal(WorldGenTestData.Crags, cell.Terrain.StableName);
    }

    [Fact]
    public void GetNeighbors_CenterHas6()
    {
        var grid = WorldGenTestData.Grid(3);
        var neighbors = grid.GetNeighbors(new HexCoord(0, 0)).ToList();
        Assert.Equal(6, neighbors.Count);
    }

    [Fact]
    public void GetNeighbors_EdgeHasFewerThan6()
    {
        var grid = WorldGenTestData.Grid(2);
        var neighbors = grid.GetNeighbors(new HexCoord(2, 0)).ToList();
        Assert.True(neighbors.Count < 6);
    }

    [Fact]
    public void GetCellsInRadius_ReturnsOnlyGridCells()
    {
        var grid = WorldGenTestData.Grid(3);
        var cells = grid.GetCellsInRadius(new HexCoord(0, 0), 5).ToList();
        Assert.Equal(grid.CellCount, cells.Count);
    }

    [Fact]
    public void GetCellsInRadius_ReturnsSubsetFromOffset()
    {
        var grid = WorldGenTestData.Grid(5);
        var cells = grid.GetCellsInRadius(new HexCoord(3, 0), 1).ToList();

        Assert.True(cells.Count >= 4);
        Assert.True(cells.Count <= 7);
    }
}

public class HexCellTests
{
    [Fact]
    public void Constructor_StoresTerrain()
    {
        var cell = WorldGenTestData.Cell(WorldGenTestData.Ocean);
        Assert.Equal(WorldGenTestData.Ocean, cell.Terrain.StableName);
    }
}
