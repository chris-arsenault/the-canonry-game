namespace Engine.Resources;

using Engine.Graph;
using Engine.Map;

/// <summary>
/// Maintains ambient per-hex resource fields.
/// The current model treats food, wealth, and population as map-level affordance
/// values: what a place can support, not the concrete stockpiles actors directly
/// consume. Named entities/processes convert these values into usable stores or
/// outcomes. This system therefore initializes and preserves ambient map-state
/// rather than running a settlement-centric economy.
/// </summary>
public sealed class ResourceSystem
{
    private readonly HexGrid _grid;
    private bool _initialized;

    public ResourceLayer Food { get; }
    public ResourceLayer Wealth { get; }
    public ResourceLayer PopulationLayer { get; }

    /// <summary>Scale factor turning fertility into ambient food potential.</summary>
    public float FoodPotentialFactor { get; set; } = 10f;

    /// <summary>Scale factor turning habitability into ambient wealth potential.</summary>
    public float WealthPotentialFactor { get; set; } = 8f;

    /// <summary>Scale factor turning habitability into ambient population presence.</summary>
    public float PopulationPresenceFactor { get; set; } = 10f;

    public ResourceSystem(WorldGraph graph, HexGrid grid)
    {
        _grid = grid;

        Food = new ResourceLayer(ResourceType.Food);
        Wealth = new ResourceLayer(ResourceType.Wealth);
        PopulationLayer = new ResourceLayer(ResourceType.Population);
    }

    /// <summary>
    /// Process one tick: ensure ambient resource fields are initialized.
    /// Richer slow-change systems will layer on top of this, but this class no longer
    /// runs a settlement-driven economy or faction redistribution model.
    /// </summary>
    public void Tick(int currentTick)
    {
        if (_initialized)
        {
            return;
        }

        InitializeAmbientResources();
        _initialized = true;
    }

    private void InitializeAmbientResources()
    {
        foreach (var cell in _grid.AllCells)
        {
            Food.SetStockpile(cell.Coord, ComputeAmbientFood(cell));
            Wealth.SetStockpile(cell.Coord, ComputeAmbientWealth(cell));
            PopulationLayer.SetStockpile(cell.Coord, ComputeAmbientPopulation(cell));
        }
    }

    private float ComputeAmbientFood(HexCell cell)
    {
        if (cell.Terrain == TerrainType.Water)
        {
            return 0f;
        }

        return cell.Fertility * FoodPotentialFactor;
    }

    private float ComputeAmbientWealth(HexCell cell)
    {
        if (cell.Terrain == TerrainType.Water)
        {
            return 0f;
        }

        var terrainBonus = cell.Terrain switch
        {
            TerrainType.Coastal => 2f,
            TerrainType.Plains => 1.5f,
            TerrainType.Forest => 1f,
            TerrainType.Mountain => 1.5f,
            _ => 0f,
        };

        return (cell.Habitability * WealthPotentialFactor) + terrainBonus + cell.Resources.Count;
    }

    private float ComputeAmbientPopulation(HexCell cell)
    {
        if (cell.Terrain == TerrainType.Water)
        {
            return 0f;
        }

        return cell.Habitability * PopulationPresenceFactor;
    }

    /// <summary>
    /// Consume population at a cell. Called by decisions that cost population
    /// (expand borders, build infrastructure, etc.). Returns actual amount consumed.
    /// </summary>
    public float ConsumePopulation(HexCoord coord, float amount)
    {
        var available = PopulationLayer.GetStockpile(coord);
        var consumed = Math.Min(amount, available);
        PopulationLayer.AddToStockpile(coord, -consumed);
        return consumed;
    }

    /// <summary>Check if a cell has minimum population for a decision.</summary>
    public bool HasMinimumPopulation(HexCoord coord, float minimum)
    {
        return PopulationLayer.GetStockpile(coord) >= minimum;
    }

    /// <summary>Get population at a cell.</summary>
    public float GetPopulation(HexCoord coord) => PopulationLayer.GetStockpile(coord);

}
