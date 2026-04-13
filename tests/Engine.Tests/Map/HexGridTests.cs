namespace Engine.Tests.Map;

using Engine.Map;

public class HexGridTests
{
    [Theory]
    [InlineData(1, 7)]
    [InlineData(3, 37)]
    [InlineData(5, 91)]
    public void Constructor_CreatesCorrectCellCount(int radius, int expectedCount)
    {
        var grid = new HexGrid(radius);
        Assert.Equal(expectedCount, grid.CellCount);
    }

    [Fact]
    public void Constructor_RejectsZeroRadius()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexGrid(0));
    }

    [Fact]
    public void GetCell_ReturnsNullOutsideGrid()
    {
        var grid = new HexGrid(2);
        Assert.Null(grid.GetCell(new HexCoord(10, 10)));
    }

    [Fact]
    public void GetCell_ReturnsCellInsideGrid()
    {
        var grid = new HexGrid(3);
        var cell = grid.GetCell(new HexCoord(0, 0));
        Assert.NotNull(cell);
        Assert.Equal(new HexCoord(0, 0), cell.Coord);
    }

    [Fact]
    public void AllCells_DefaultToPlains()
    {
        var grid = new HexGrid(2);
        Assert.All(grid.AllCells, c => Assert.Equal(TerrainType.Plains, c.Terrain));
    }

    [Fact]
    public void SetCell_ChangesTerrainAndAttributes()
    {
        var grid = new HexGrid(3);
        grid.SetCell(new HexCoord(1, 0), TerrainType.Mountain, elevation: 500f, fertility: 0.1f);

        var cell = grid.GetCell(new HexCoord(1, 0));
        Assert.NotNull(cell);
        Assert.Equal(TerrainType.Mountain, cell.Terrain);
        Assert.Equal(500f, cell.Elevation);
        Assert.Equal(0.1f, cell.Fertility);
    }

    [Fact]
    public void GetNeighbors_CenterHas6()
    {
        var grid = new HexGrid(3);
        var neighbors = grid.GetNeighbors(new HexCoord(0, 0)).ToList();
        Assert.Equal(6, neighbors.Count);
    }

    [Fact]
    public void GetNeighbors_EdgeHasFewerThan6()
    {
        var grid = new HexGrid(2);
        // Corner cell of a radius-2 grid
        var neighbors = grid.GetNeighbors(new HexCoord(2, 0)).ToList();
        Assert.True(neighbors.Count < 6);
    }

    [Fact]
    public void GetCellsInRadius_ReturnsOnlyGridCells()
    {
        var grid = new HexGrid(3);
        // Ask for radius 5 from center -- should be clamped to actual grid cells
        var cells = grid.GetCellsInRadius(new HexCoord(0, 0), 5).ToList();
        Assert.Equal(grid.CellCount, cells.Count); // Grid only has radius 3
    }

    [Fact]
    public void GetCellsInRadius_ReturnsSubsetFromOffset()
    {
        var grid = new HexGrid(5);
        var cells = grid.GetCellsInRadius(new HexCoord(3, 0), 1).ToList();
        // Some neighbors of (3,0) will be outside the grid
        Assert.True(cells.Count >= 4); // At least center + a few neighbors
        Assert.True(cells.Count <= 7); // At most center + 6
    }
}

public class HexCellTests
{
    [Theory]
    [InlineData(TerrainType.Plains, 1f)]
    [InlineData(TerrainType.Mountain, 4f)]
    [InlineData(TerrainType.Forest, 2f)]
    public void MovementCost_VariesByTerrain(TerrainType terrain, float expected)
    {
        var cell = new HexCell(new HexCoord(0, 0), terrain);
        Assert.Equal(expected, cell.MovementCost);
    }

    [Fact]
    public void MovementCost_WaterIsInfinity()
    {
        var cell = new HexCell(new HexCoord(0, 0), TerrainType.Water);
        Assert.Equal(float.PositiveInfinity, cell.MovementCost);
    }

    [Fact]
    public void Habitability_WaterIsZero()
    {
        var cell = new HexCell(new HexCoord(0, 0), TerrainType.Water, fertility: 1f);
        Assert.Equal(0f, cell.Habitability);
    }

    [Fact]
    public void Habitability_PlainsWithHighFertilityIsHigh()
    {
        var cell = new HexCell(new HexCoord(0, 0), TerrainType.Plains, fertility: 1f);
        Assert.True(cell.Habitability > 0.8f);
    }

    [Fact]
    public void Habitability_MountainWithLowFertilityIsLow()
    {
        var cell = new HexCell(new HexCoord(0, 0), TerrainType.Mountain, fertility: 0.2f);
        Assert.True(cell.Habitability < 0.1f);
    }

    [Fact]
    public void Fertility_ClampedToRange()
    {
        var cell = new HexCell(new HexCoord(0, 0), TerrainType.Plains, fertility: 2f);
        Assert.Equal(1f, cell.Fertility);
    }

    [Fact]
    public void Resources_CanBeAdded()
    {
        var cell = new HexCell(new HexCoord(0, 0), TerrainType.Mountain);
        cell.Resources.Add("iron");
        cell.Resources.Add("gold_ore");

        Assert.Contains("iron", cell.Resources);
        Assert.Equal(2, cell.Resources.Count);
    }
}
