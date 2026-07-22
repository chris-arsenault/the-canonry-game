namespace Engine.Domain.Spatial;

/// <summary>
/// A hex grid of cells. Created with a radius parameter (hexagonal shape).
/// Cells are stored by axial coordinate for O(1) lookup.
/// </summary>
public sealed class HexGrid
{
    private readonly Dictionary<HexCoord, HexCell> _cells = [];
    private readonly TerrainCatalog _terrainCatalog;

    public int Radius { get; }
    public TerrainCatalog TerrainCatalog => _terrainCatalog;

    /// <summary>Total cell count for a hex grid of given radius: 3r^2 + 3r + 1.</summary>
    public int CellCount => _cells.Count;

    public HexGrid(int radius, TerrainCatalog terrainCatalog, string defaultTerrainName)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        ArgumentNullException.ThrowIfNull(terrainCatalog);

        Radius = radius;
        _terrainCatalog = terrainCatalog;
        var defaultTerrain = terrainCatalog.Resolve(defaultTerrainName);

        foreach (var coord in HexCoord.CoordsInRadius(new HexCoord(0, 0), radius))
        {
            _cells[coord] = new HexCell(coord, defaultTerrain);
        }
    }

    public HexCell? GetCell(HexCoord coord)
    {
        return _cells.GetValueOrDefault(coord);
    }

    public bool ContainsCoord(HexCoord coord)
    {
        return _cells.ContainsKey(coord);
    }

    /// <summary>Get all cells in the grid.</summary>
    public IEnumerable<HexCell> AllCells => _cells.Values;

    /// <summary>Get the valid neighbors of a cell (only those within the grid).</summary>
    public IEnumerable<HexCell> GetNeighbors(HexCoord coord)
    {
        foreach (var neighbor in coord.Neighbors())
        {
            if (_cells.TryGetValue(neighbor, out var cell))
            {
                yield return cell;
            }
        }
    }

    /// <summary>Get all cells within a radius of a center (only those within the grid).</summary>
    public IEnumerable<HexCell> GetCellsInRadius(HexCoord center, int radius)
    {
        foreach (var coord in HexCoord.CoordsInRadius(center, radius))
        {
            if (_cells.TryGetValue(coord, out var cell))
            {
                yield return cell;
            }
        }
    }

    /// <summary>Set the terrain of a cell. Used during map generation.</summary>
    public void SetCell(HexCoord coord, string terrainName)
    {
        if (_cells.TryGetValue(coord, out var cell))
        {
            cell.SetTerrain(_terrainCatalog.Resolve(terrainName));
        }
    }
}
