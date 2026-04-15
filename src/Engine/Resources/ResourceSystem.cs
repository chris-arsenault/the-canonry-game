namespace Engine.Resources;

using Engine.Graph;
using Engine.Map;

/// <summary>
/// Maintains ambient per-hex resource fields.
/// The current model treats food, wealth, and population as map-level affordance
/// values: what a place can support, not a concrete actor-owned inventory layer.
/// Named entities/processes turn these ambient conditions into concrete outcomes.
/// This system therefore initializes and preserves ambient map-state rather than
/// running a settlement-centric economy.
/// </summary>
public sealed class ResourceSystem
{
    private readonly HexGrid _grid;
    private bool _initialized;

    public ResourceLayer Food { get; }
    public ResourceLayer Wealth { get; }
    public ResourceLayer PopulationLayer { get; }

    public ResourceSystem(WorldGraph graph, HexGrid grid)
    {
        _grid = grid;

        Food = new ResourceLayer(ResourceType.Food);
        Wealth = new ResourceLayer(ResourceType.Wealth);
        PopulationLayer = new ResourceLayer(ResourceType.Population);

        InitializeAmbientResources();
        _initialized = true;
    }

    /// <summary>
    /// Process one tick. Ambient resource fields are seeded from map generation at
    /// construction time; richer slow-change systems will layer on top of this.
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
            Food.SetStockpile(cell.Coord, cell.AmbientFood);
            Wealth.SetStockpile(cell.Coord, cell.AmbientWealth);
            PopulationLayer.SetStockpile(cell.Coord, cell.AmbientPopulation);
        }
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
