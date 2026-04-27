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
    public void SetCell_ChangesTerrainAndAttributes()
    {
        var grid = WorldGenTestData.Grid(3);

        grid.SetCell(new HexCoord(1, 0), WorldGenTestData.Crags, elevation: 500f, fertility: 0.1f);

        var cell = grid.GetCell(new HexCoord(1, 0));
        Assert.NotNull(cell);
        Assert.Equal(WorldGenTestData.Crags, cell.Terrain.StableName);
        Assert.Equal(500f, cell.Elevation);
        Assert.Equal(0.1f, cell.Fertility);
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
    [Theory]
    [InlineData(WorldGenTestData.Tundra, 1.1f)]
    [InlineData(WorldGenTestData.Crags, 4f)]
    [InlineData(WorldGenTestData.Shelf, 1.8f)]
    public void MovementCost_VariesByTerrain(string terrain, float expected)
    {
        var cell = WorldGenTestData.Cell(terrain);
        Assert.Equal(expected, cell.MovementCost);
    }

    [Fact]
    public void MovementCost_OceanIsInfinity()
    {
        var cell = WorldGenTestData.Cell(WorldGenTestData.Ocean);
        Assert.Equal(float.PositiveInfinity, cell.MovementCost);
    }

    [Fact]
    public void Habitability_OceanIsZero()
    {
        var cell = WorldGenTestData.Cell(WorldGenTestData.Ocean, fertility: 1f);
        Assert.Equal(0f, cell.Habitability);
    }

    [Fact]
    public void Habitability_HotSpringsWithHighFertilityIsHigh()
    {
        var cell = WorldGenTestData.Cell(WorldGenTestData.HotSprings, fertility: 1f);
        Assert.True(cell.Habitability > 0.8f);
    }

    [Fact]
    public void Habitability_CragsWithLowFertilityIsLow()
    {
        var cell = WorldGenTestData.Cell(WorldGenTestData.Crags, fertility: 0.2f);
        Assert.True(cell.Habitability < 0.1f);
    }

    [Fact]
    public void Fertility_ClampedToRange()
    {
        var cell = WorldGenTestData.Cell(WorldGenTestData.Tundra, fertility: 2f);
        Assert.Equal(1f, cell.Fertility);
    }

    [Fact]
    public void Resources_CanBeAdded()
    {
        var cell = WorldGenTestData.Cell(WorldGenTestData.Crags);
        cell.Resources.Add("iron");
        cell.Resources.Add("gold_ore");

        Assert.Contains("iron", cell.Resources);
        Assert.Equal(2, cell.Resources.Count);
    }
}
