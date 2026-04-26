namespace Engine.WorldGen;

/// <summary>
/// A single cell in the hex grid. Stores terrain, map-seeded ambient resource
/// values, and derived attributes.
/// Cells are map infrastructure, not entities in the graph.
/// </summary>
public sealed class HexCell
{
    private readonly Dictionary<string, float> _ambientResources = new(StringComparer.Ordinal);

    public HexCoord Coord { get; }
    public TerrainDefinition Terrain { get; private set; }
    public float Elevation { get; set; }
    public float Fertility { get; set; }
    public HashSet<string> Resources { get; } = [];
    public IReadOnlyDictionary<string, float> AmbientResources => _ambientResources;

    /// <summary>Derived: how viable this cell is for settlement. Recompute when base values change.</summary>
    public float Habitability => Terrain.HabitabilityFactor * Fertility;

    /// <summary>Derived: how expensive it is to cross this cell. Used by pathfinding.</summary>
    public float MovementCost => Terrain.IsPassable ? Terrain.MovementCost : float.PositiveInfinity;

    public HexCell(HexCoord coord, TerrainDefinition terrain, float elevation = 0f, float fertility = 0.5f)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        Coord = coord;
        Terrain = terrain;
        Elevation = elevation;
        Fertility = Math.Clamp(fertility, 0f, 1f);
    }

    public void SetTerrain(TerrainDefinition terrain, float elevation = 0f, float fertility = 0.5f)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        Terrain = terrain;
        Elevation = elevation;
        Fertility = Math.Clamp(fertility, 0f, 1f);
    }

    public void SetAmbientResource(string stableName, float value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stableName);
        _ambientResources[stableName] = value;
    }

    public float AmbientResource(string stableName) =>
        _ambientResources.GetValueOrDefault(stableName);
}
