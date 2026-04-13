namespace Engine.Resources;

using Engine.Attributes;
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
    private readonly AttributeStore _attributes;
    private readonly HexGrid _grid;

    public ResourceLayer Sustenance { get; }
    public ResourceLayer Gold { get; }

    /// <summary>Sustenance produced per unit of (fertility * population / 100).</summary>
    public float SustenanceProductionFactor { get; set; } = 1f;

    /// <summary>Sustenance consumed per unit of population per tick.</summary>
    public float SustenanceConsumptionPerCapita { get; set; } = 0.01f;

    /// <summary>Gold produced per unit of development per tick.</summary>
    public float GoldProductionFactor { get; set; } = 0.1f;

    /// <summary>Fixed gold consumption per settlement per tick.</summary>
    public float GoldConsumptionPerSettlement { get; set; } = 0.5f;

    /// <summary>Fraction of surplus redistributed per tick during intra-national flow.</summary>
    public float FlowRate { get; set; } = 0.3f;

    public ResourceSystem(WorldGraph graph, AttributeStore attributes, HexGrid grid)
    {
        _graph = graph;
        _attributes = attributes;
        _grid = grid;

        Sustenance = new ResourceLayer(ResourceType.Sustenance);
        Gold = new ResourceLayer(ResourceType.Gold);
    }

    /// <summary>
    /// Process one tick: compute production/consumption, then redistribute within nations.
    /// </summary>
    public void Tick(int currentTick)
    {
        ComputeProductionAndConsumption();
        RedistributeWithinNations(Sustenance);
        RedistributeWithinNations(Gold);
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

            var population = _attributes.Get(settlementId, "population");
            var development = _attributes.Get(settlementId, "development");

            // Sustenance: fertility * population labor -> production, population -> consumption
            var sustenanceProd = cell.Fertility * (population / 100f) * SustenanceProductionFactor;
            var sustenanceCons = population * SustenanceConsumptionPerCapita;
            Sustenance.SetProduction(coord, sustenanceProd);
            Sustenance.SetConsumption(coord, sustenanceCons);

            // Gold: development-based production, fixed per-settlement consumption
            var goldProd = development * GoldProductionFactor;
            Sustenance.SetConsumption(coord, sustenanceCons); // Already set above
            Gold.SetProduction(coord, goldProd);
            Gold.SetConsumption(coord, GoldConsumptionPerSettlement);
        }
    }

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
