namespace Engine.Domain.Spatial;

/// <summary>
/// A single cell in the hex grid. Stores terrain identity for map generation.
/// Cells are map infrastructure, not entities in the graph.
/// </summary>
public sealed class HexCell
{
    public HexCoord Coord { get; }
    public TerrainDefinition Terrain { get; private set; }

    public HexCell(HexCoord coord, TerrainDefinition terrain)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        Coord = coord;
        Terrain = terrain;
    }

    public void SetTerrain(TerrainDefinition terrain)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        Terrain = terrain;
    }
}
