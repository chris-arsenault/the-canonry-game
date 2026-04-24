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
            TerrainType.Ocean => 0f,
            TerrainType.Crags => 0.1f * Fertility,
            TerrainType.Wastes => 0.2f * Fertility,
            TerrainType.Ice => 0.3f * Fertility,
            TerrainType.Shelf => 0.45f * Fertility,
            TerrainType.Reef => 0.55f * Fertility,
            TerrainType.Tundra => 0.75f * Fertility,
            TerrainType.HotSprings => 0.95f * Fertility,
            _ => 0.5f * Fertility,
        };
    }

    private float ComputeMovementCost()
    {
        return Terrain switch
        {
            TerrainType.Ocean => float.PositiveInfinity,
            TerrainType.Crags => 4f,
            TerrainType.Ice => 2.5f,
            TerrainType.Shelf => 1.8f,
            TerrainType.Reef => 1.6f,
            TerrainType.Wastes => 1.4f,
            TerrainType.Tundra => 1.1f,
            TerrainType.HotSprings => 1f,
            _ => 1.5f,
        };
    }
}
