namespace Engine.Resources;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;

/// <summary>
/// Per-tick resource computation: calculates production and consumption from
/// entity attributes and terrain, then handles intra-faction flow (surplus
/// from one settlement flows toward deficit at another within the same faction).
/// Registers with TickClock at LowAltitude phase.
/// </summary>
public sealed class ResourceSystem
{
    private readonly WorldGraph _graph;
    private readonly HexGrid _grid;

    public ResourceLayer Food { get; }
    public ResourceLayer Wealth { get; }
    public ResourceLayer PopulationLayer { get; }

    /// <summary>Food produced per unit of (fertility * residents / 100).</summary>
    public float FoodProductionFactor { get; set; } = 1f;

    /// <summary>Food consumed per resident per tick.</summary>
    public float FoodConsumptionPerResident { get; set; } = 0.01f;

    /// <summary>Wealth produced per unit of local activity per tick.</summary>
    public float WealthProductionFactor { get; set; } = 0.1f;

    /// <summary>Fixed wealth consumption per settlement per tick.</summary>
    public float WealthConsumptionPerSettlement { get; set; } = 0.5f;

    /// <summary>Ambient population growth rate from food surplus.</summary>
    public float PopulationGrowthFromFood { get; set; } = 0.5f;

    /// <summary>Ambient population decline rate from food deficit.</summary>
    public float PopulationDeclineFromDeficit { get; set; } = 0.3f;

    /// <summary>Fraction of surplus redistributed per tick during intra-faction flow.</summary>
    public float FlowRate { get; set; } = 0.3f;

    public ResourceSystem(WorldGraph graph, HexGrid grid)
    {
        _graph = graph;
        _grid = grid;

        Food = new ResourceLayer(ResourceType.Food);
        Wealth = new ResourceLayer(ResourceType.Wealth);
        PopulationLayer = new ResourceLayer(ResourceType.Population);
    }

    /// <summary>
    /// Process one tick: compute production/consumption, population dynamics, then redistribute.
    /// </summary>
    public void Tick(int currentTick)
    {
        ComputeProductionAndConsumption();
        ComputePopulationDynamics();
        RedistributeWithinFactions(Food);
        RedistributeWithinFactions(Wealth);
        RedistributeWithinFactions(PopulationLayer);
    }

    private void ComputeProductionAndConsumption()
    {
        foreach (var settlementId in _graph.GetEntitiesByKind(EntityKind.Settlement))
        {
            var entity = _graph.GetEntity(settlementId);
            if (entity is null || entity.Status != EntityStatus.Active || entity.Coordinates is null)
            {
                continue;
            }

            var coord = new HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R);
            var cell = _grid.GetCell(coord);
            if (cell is null)
            {
                continue;
            }

            var residents = entity.Attributes.GetValue("residents");
            var prestige = entity.Attributes.GetValue("prestige");

            // Food: fertility * resident labor -> production, residents -> consumption
            var foodProd = cell.Fertility * (residents / 100f) * FoodProductionFactor;
            var foodCons = residents * FoodConsumptionPerResident;
            Food.SetProduction(coord, foodProd);
            Food.SetConsumption(coord, foodCons);

            // Wealth: resident activity and prestige generate output, each settlement consumes upkeep.
            var wealthProd = ((residents / 100f) + (prestige / 50f)) * WealthProductionFactor;
            Wealth.SetProduction(coord, wealthProd);
            Wealth.SetConsumption(coord, WealthConsumptionPerSettlement);

            // Ambient population seeds around occupied settlements and later grows or declines
            // from local food surplus/deficit.
            if (PopulationLayer.GetStockpile(coord) <= 0 && residents > 0)
            {
                PopulationLayer.AddToStockpile(coord, residents);
            }
        }
    }

    private void ComputePopulationDynamics()
    {
        // Ambient population grows from food surplus, declines from deficit.
        foreach (var coord in Food.ActiveCells)
        {
            var foodNet = Food.GetNet(coord);
            var currentPop = PopulationLayer.GetStockpile(coord);

            if (foodNet > 0)
            {
                var growth = foodNet * PopulationGrowthFromFood;
                PopulationLayer.AddToStockpile(coord, growth);
            }
            else if (foodNet < 0 && currentPop > 0)
            {
                var decline = -foodNet * PopulationDeclineFromDeficit;
                PopulationLayer.AddToStockpile(coord, -decline);
            }
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

    private void RedistributeWithinFactions(ResourceLayer layer)
    {
        // Group settlements by controlling faction
        var factionSettlements = new Dictionary<EntityId, List<(HexCoord Coord, EntityId SettlementId)>>();

        foreach (var settlementId in _graph.GetEntitiesByKind(EntityKind.Settlement))
        {
            var entity = _graph.GetEntity(settlementId);
            if (entity is null || entity.Status != EntityStatus.Active || entity.Coordinates is null)
            {
                continue;
            }

            // Find controlling faction
            var controlRels = _graph.OutgoingRelationships(settlementId, "controlled_by");
            foreach (var rel in controlRels)
            {
                if (!factionSettlements.TryGetValue(rel.TargetId, out var list))
                {
                    list = [];
                    factionSettlements[rel.TargetId] = list;
                }

                var coord = new HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R);
                list.Add((coord, settlementId));
                break; // controlled_by is exclusive
            }
        }

        // For each faction, redistribute surplus to deficit settlements
        foreach (var (_, settlements) in factionSettlements)
        {
            if (settlements.Count < 2)
            {
                continue;
            }

            var totalSurplus = 0f;
            var totalDeficit = 0f;
            var surplusCells = new List<(HexCoord Coord, float Surplus)>();
            var deficitCells = new List<(HexCoord Coord, float Deficit)>();

            foreach (var (coord, _) in settlements)
            {
                var net = layer.GetNet(coord);
                if (net > 0)
                {
                    surplusCells.Add((coord, net));
                    totalSurplus += net;
                }
                else if (net < 0)
                {
                    deficitCells.Add((coord, -net));
                    totalDeficit += -net;
                }
            }

            if (totalSurplus <= 0 || totalDeficit <= 0)
            {
                continue;
            }

            // Transfer proportionally: each deficit cell gets a share of available surplus
            var available = Math.Min(totalSurplus, totalDeficit) * FlowRate;

            foreach (var (coord, deficit) in deficitCells)
            {
                var share = (deficit / totalDeficit) * available;
                layer.AddToStockpile(coord, share);
            }

            // Deduct from surplus cells proportionally
            foreach (var (coord, surplus) in surplusCells)
            {
                var deduction = (surplus / totalSurplus) * available;
                layer.AddToStockpile(coord, -deduction);
            }
        }
    }
}
