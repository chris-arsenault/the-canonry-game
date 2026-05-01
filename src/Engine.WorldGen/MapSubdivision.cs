namespace Engine.WorldGen;

/// <summary>
/// Non-entity map hierarchy. These IDs partition the physical map at scales
/// larger than entity-owned region coverage; they are geography, not graph nodes.
/// </summary>
public sealed class MapSubdivisionMap
{
    private readonly Dictionary<HexCoord, MapSubdivisionCell> _cells = [];

    public IReadOnlyDictionary<HexCoord, MapSubdivisionCell> Cells => _cells;

    public void AssignCell(HexCoord coord, int superregionId, int continentId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(superregionId);
        ArgumentOutOfRangeException.ThrowIfNegative(continentId);
        _cells[coord] = new MapSubdivisionCell(superregionId, continentId);
    }

    public MapSubdivisionCell GetCell(HexCoord coord)
    {
        return _cells.GetValueOrDefault(coord);
    }
}

public readonly record struct MapSubdivisionCell(
    int SuperregionId,
    int ContinentId);
