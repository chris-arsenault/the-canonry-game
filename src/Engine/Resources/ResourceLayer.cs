namespace Engine.Resources;

using Engine.Map;

/// <summary>
/// Per-cell resource data for one resource type. Tracks production, consumption,
/// and net flow at each hex cell. Resources are map-graph properties, not entity attributes.
/// </summary>
public sealed class ResourceLayer
{
    public ResourceType Type { get; }

    // Per-cell values
    private readonly Dictionary<HexCoord, float> _production = [];
    private readonly Dictionary<HexCoord, float> _consumption = [];
    private readonly Dictionary<HexCoord, float> _stockpile = [];

    public ResourceLayer(ResourceType type)
    {
        Type = type;
    }

    public void SetProduction(HexCoord coord, float rate)
    {
        _production[coord] = Math.Max(0f, rate);
    }

    public float GetProduction(HexCoord coord)
    {
        return _production.GetValueOrDefault(coord);
    }

    public void SetConsumption(HexCoord coord, float rate)
    {
        _consumption[coord] = Math.Max(0f, rate);
    }

    public float GetConsumption(HexCoord coord)
    {
        return _consumption.GetValueOrDefault(coord);
    }

    /// <summary>Net = production - consumption. Positive = surplus, negative = deficit.</summary>
    public float GetNet(HexCoord coord)
    {
        return GetProduction(coord) - GetConsumption(coord);
    }

    public float GetStockpile(HexCoord coord)
    {
        return _stockpile.GetValueOrDefault(coord);
    }

    public void AddToStockpile(HexCoord coord, float amount)
    {
        var current = _stockpile.GetValueOrDefault(coord);
        _stockpile[coord] = Math.Max(0f, current + amount);
    }

    /// <summary>Total production across all cells.</summary>
    public float TotalProduction => _production.Values.Sum();

    /// <summary>Total consumption across all cells.</summary>
    public float TotalConsumption => _consumption.Values.Sum();

    /// <summary>Total net across all cells.</summary>
    public float TotalNet => TotalProduction - TotalConsumption;

    /// <summary>All cells with non-zero production or consumption.</summary>
    public IEnumerable<HexCoord> ActiveCells =>
        _production.Keys.Union(_consumption.Keys).Distinct();
}
