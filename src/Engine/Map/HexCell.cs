namespace Engine.Map;

/// <summary>
/// A single cell in the hex grid. Stores terrain, map-seeded ambient resource
/// values, and derived attributes.
/// Cells are map infrastructure, not entities in the graph.
/// </summary>
public sealed class HexCell
{
    public HexCoord Coord { get; }
    public TerrainType Terrain { get; set; }
    public float Elevation { get; set; }
    public float Fertility { get; set; }
    public float AmbientFood { get; set; }
    public float AmbientWealth { get; set; }
    public float AmbientPopulation { get; set; }
    public HashSet<string> Resources { get; } = [];

    /// <summary>Derived: how viable this cell is for settlement. Recompute when base values change.</summary>
    public float Habitability => ComputeHabitability();

    /// <summary>Derived: how expensive it is to cross this cell. Used by pathfinding.</summary>
    public float MovementCost => ComputeMovementCost();

    public HexCell(HexCoord coord, TerrainType terrain, float elevation = 0f, float fertility = 0.5f)
    {
        Coord = coord;
        Terrain = terrain;
        Elevation = elevation;
        Fertility = Math.Clamp(fertility, 0f, 1f);
        AmbientFood = 0f;
        AmbientWealth = 0f;
        AmbientPopulation = 0f;
    }

    private float ComputeHabitability()
    {
        return Terrain switch
        {
            TerrainType.Water => 0f,
            TerrainType.Mountain => 0.1f * Fertility,
            TerrainType.Desert => 0.2f * Fertility,
            TerrainType.Swamp => 0.3f * Fertility,
            TerrainType.Tundra => 0.3f * Fertility,
            TerrainType.Forest => 0.6f * Fertility,
            TerrainType.Coastal => 0.7f * Fertility,
            TerrainType.Plains => 0.9f * Fertility,
            _ => 0.5f * Fertility,
        };
    }

    private float ComputeMovementCost()
    {
        return Terrain switch
        {
            TerrainType.Water => float.PositiveInfinity,
            TerrainType.Mountain => 4f,
            TerrainType.Swamp => 3f,
            TerrainType.Forest => 2f,
            TerrainType.Desert => 2f,
            TerrainType.Tundra => 2f,
            TerrainType.Coastal => 1.2f,
            TerrainType.Plains => 1f,
            _ => 1.5f,
        };
    }
}
