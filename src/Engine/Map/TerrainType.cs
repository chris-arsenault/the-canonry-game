namespace Engine.Map;

/// <summary>
/// Terrain classification for hex cells. Affects movement cost, fertility,
/// resource production, and visual rendering.
/// </summary>
public enum TerrainType
{
    Plains,
    Mountain,
    Forest,
    Desert,
    Water,
    Coastal,
    Swamp,
    Tundra,
}
