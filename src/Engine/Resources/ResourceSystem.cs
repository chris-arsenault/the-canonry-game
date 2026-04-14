namespace Engine.Resources;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;

/// <summary>
/// Per-tick resource computation: calculates production and consumption from
/// entity attributes and terrain, then handles intra-national flow (surplus
/// from one settlement flows toward deficit at another within the same nation).
/// Registers with TickClock at LowAltitude phase.
/// </summary>
public sealed class ResourceSystem
{
    private readonly WorldGraph _graph;
    private readonly HexGrid _grid;

    public ResourceLayer Sustenance { get; }
    public ResourceLayer Gold { get; }
    public ResourceLayer PopulationLayer { get; }

    /// <summary>Sustenance produced per unit of (fertility * population / 100).</summary>
    public float SustenanceProductionFactor { get; set; } = 1f;

    /// <summary>Sustenance consumed per unit of population per tick.</summary>
    public float SustenanceConsumptionPerCapita { get; set; } = 0.01f;

    /// <summary>Gold produced per unit of development per tick.</summary>
    public float GoldProductionFactor { get; set; } = 0.1f;

    /// <summary>Fixed gold consumption per settlement per tick.</summary>
    public float GoldConsumptionPerSettlement { get; set; } = 0.5f;

    /// <summary>Population growth rate from sustenance surplus.</summary>
    public float PopulationGrowthFromSustenance { get; set; } = 0.5f;

    /// <summary>Population decline rate from sustenance deficit.</summary>
    public float PopulationDeclineFromDeficit { get; set; } = 0.3f;

    /// <summary>Fraction of surplus redistributed per tick during intra-national flow.</summary>
    public float FlowRate { get; set; } = 0.3f;

    public ResourceSystem(WorldGraph graph, HexGrid grid)
    {
        _graph = graph;
        _grid = grid;

        Sustenance = new ResourceLayer(ResourceType.Sustenance);
        Gold = new ResourceLayer(ResourceType.Gold);
        PopulationLayer = new ResourceLayer(ResourceType.Population);
    }

    /// <summary>
    /// Process one tick: compute production/consumption, population dynamics, then redistribute.
    /// </summary>
    public void Tick(int currentTick)
    {
        ComputeProductionAndConsumption();
        ComputePopulationDynamics();
        RedistributeWithinNations(Sustenance);
        RedistributeWithinNations(Gold);
        RedistributeWithinNations(PopulationLayer);
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

            var population = entity.Attributes.GetValue("population");
            var development = entity.Attributes.GetValue("development");

            // Sustenance: fertility * population labor -> production, population -> consumption
            var sustenanceProd = cell.Fertility * (population / 100f) * SustenanceProductionFactor;
            var sustenanceCons = population * SustenanceConsumptionPerCapita;
            Sustenance.SetProduction(coord, sustenanceProd);
            Sustenance.SetConsumption(coord, sustenanceCons);

            // Gold: development-based production, fixed per-settlement consumption
            var goldProd = development * GoldProductionFactor;
            Gold.SetProduction(coord, goldProd);
            Gold.SetConsumption(coord, GoldConsumptionPerSettlement);

            // Population: existing population at this cell (from entity attributes or stockpile)
            // Initial population set from settlement population attribute
            if (PopulationLayer.GetStockpile(coord) <= 0 && population > 0)
            {
                PopulationLayer.AddToStockpile(coord, population);
            }
        }
    }

    private void ComputePopulationDynamics()
    {
        // Population grows from sustenance surplus, declines from deficit
        foreach (var coord in Sustenance.ActiveCells)
        {
            var sustenanceNet = Sustenance.GetNet(coord);
            var currentPop = PopulationLayer.GetStockpile(coord);

            if (sustenanceNet > 0)
            {
                // Surplus = population growth
                var growth = sustenanceNet * PopulationGrowthFromSustenance;
                PopulationLayer.AddToStockpile(coord, growth);
            }
            else if (sustenanceNet < 0 && currentPop > 0)
            {
                // Deficit = population decline
                var decline = -sustenanceNet * PopulationDeclineFromDeficit;
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

    private void RedistributeWithinNations(ResourceLayer layer)
    {
        // Group settlements by controlling nation
        var nationSettlements = new Dictionary<EntityId, List<(HexCoord Coord, EntityId SettlementId)>>();

        foreach (var settlementId in _graph.GetEntitiesByKind(EntityKind.Settlement))
        {
            var entity = _graph.GetEntity(settlementId);
            if (entity is null || entity.Status != EntityStatus.Active || entity.Coordinates is null)
            {
                continue;
            }

            // Find controlling nation
            var controlRels = _graph.OutgoingRelationships(settlementId, "controlled_by");
            foreach (var rel in controlRels)
            {
                if (!nationSettlements.TryGetValue(rel.TargetId, out var list))
                {
                    list = [];
                    nationSettlements[rel.TargetId] = list;
                }

                var coord = new HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R);
                list.Add((coord, settlementId));
                break; // controlled_by is exclusive
            }
        }

        // For each nation, redistribute surplus to deficit settlements
        foreach (var (_, settlements) in nationSettlements)
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
