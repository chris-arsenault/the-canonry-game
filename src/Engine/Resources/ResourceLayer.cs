namespace Engine.Resources;

using Engine.Map;

/// <summary>
/// Per-cell resource data for one resource type.
/// In the ambient-resource model this is a single map-state value per hex:
/// how much food/wealth/population potential or presence a place currently has.
/// Resources are map-graph properties, not entity attributes.
/// </summary>
public sealed class ResourceLayer
{
    public ResourceType Type { get; }

    private readonly Dictionary<HexCoord, float> _values = [];

    public ResourceLayer(ResourceType type)
    {
        Type = type;
    }

    public float GetStockpile(HexCoord coord)
    {
        return _values.GetValueOrDefault(coord);
    }

    public void SetStockpile(HexCoord coord, float amount)
    {
        _values[coord] = Math.Max(0f, amount);
    }

    public void AddToStockpile(HexCoord coord, float amount)
    {
        var current = _values.GetValueOrDefault(coord);
        _values[coord] = Math.Max(0f, current + amount);
    }

    /// <summary>Total value across all cells.</summary>
    public float TotalValue => _values.Values.Sum();

    /// <summary>All cells with an explicitly tracked value.</summary>
    public IEnumerable<HexCoord> ActiveCells => _values.Keys;
}
