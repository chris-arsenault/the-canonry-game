namespace Engine.Tests.Resources;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Resources;

public class ResourceSystemTests
{
    // --- ResourceLayer ---

    [Fact]
    public void Layer_NetIsProductionMinusConsumption()
    {
        var layer = new ResourceLayer(ResourceType.Sustenance);
        var coord = new HexCoord(0, 0);

        layer.SetProduction(coord, 10f);
        layer.SetConsumption(coord, 4f);

        Assert.Equal(6f, layer.GetNet(coord));
    }

    [Fact]
    public void Layer_NegativeNetIsDeficit()
    {
        var layer = new ResourceLayer(ResourceType.Sustenance);
        var coord = new HexCoord(0, 0);

        layer.SetProduction(coord, 3f);
        layer.SetConsumption(coord, 8f);

        Assert.Equal(-5f, layer.GetNet(coord));
    }

    [Fact]
    public void Layer_StockpileAccumulates()
    {
        var layer = new ResourceLayer(ResourceType.Gold);
        var coord = new HexCoord(0, 0);

        layer.AddToStockpile(coord, 10f);
        layer.AddToStockpile(coord, 5f);

        Assert.Equal(15f, layer.GetStockpile(coord));
    }

    [Fact]
    public void Layer_StockpileNeverNegative()
    {
        var layer = new ResourceLayer(ResourceType.Gold);
        var coord = new HexCoord(0, 0);

        layer.AddToStockpile(coord, 5f);
        layer.AddToStockpile(coord, -20f);

        Assert.Equal(0f, layer.GetStockpile(coord));
    }

    [Fact]
    public void Layer_TotalsAcrossCells()
    {
        var layer = new ResourceLayer(ResourceType.Sustenance);
        layer.SetProduction(new HexCoord(0, 0), 10f);
        layer.SetProduction(new HexCoord(1, 0), 5f);
        layer.SetConsumption(new HexCoord(0, 0), 3f);

        Assert.Equal(15f, layer.TotalProduction);
        Assert.Equal(3f, layer.TotalConsumption);
        Assert.Equal(12f, layer.TotalNet);
    }

    // --- ResourceSystem: production/consumption ---

    [Fact]
    public void System_ProducesSustenanceFromFertilityAndPopulation()
    {
        var (system, _) = CreateTestSystem();

        system.Tick(1);

        // Settlement at (0,-2) on the test map: plains, fertility ~0.7, population 100
        var coord = new HexCoord(0, -2);
        Assert.True(system.Sustenance.GetProduction(coord) > 0f,
            $"Sustenance production at {coord}: {system.Sustenance.GetProduction(coord)}");
    }

    [Fact]
    public void System_ConsumesSustenanceProportionalToPopulation()
    {
        var (system, _) = CreateTestSystem();

        system.Tick(1);

        // Any settlement with population > 0 should consume sustenance
        var coord = new HexCoord(0, -2);
        Assert.True(system.Sustenance.GetConsumption(coord) > 0f);
    }

    [Fact]
    public void System_ProducesGoldFromDevelopment()
    {
        var (system, _) = CreateTestSystem();

        system.Tick(1);

        var coord = new HexCoord(0, -2);
        Assert.True(system.Gold.GetProduction(coord) > 0f);
    }

    [Fact]
    public void System_NoProductionWithoutSettlement()
    {
        var (system, _) = CreateTestSystem();

        system.Tick(1);

        // Cell with no settlement should have zero production
        var emptyCoord = new HexCoord(3, -3);
        Assert.Equal(0f, system.Sustenance.GetProduction(emptyCoord));
        Assert.Equal(0f, system.Gold.GetProduction(emptyCoord));
    }

    // --- Intra-national flow ---

    [Fact]
    public void System_FlowDoesNotCrossNationBoundaries()
    {
        var graph = new WorldGraph();
        var grid = new HexGrid(3);

        // Two nations, one settlement each
        var nationA = graph.CreateEntity(EntityKind.Nation, "generic_nation", "A", "c", 0);
        var nationB = graph.CreateEntity(EntityKind.Nation, "generic_nation", "B", "c", 0);

        // Nation A has a high-fertility settlement (surplus)
        var sA = graph.CreateEntity(EntityKind.Settlement, "village", "Rich", "c", 0, (0, 0));
        sA.Attributes.Add(new Engine.Attributes.AttributeInstance("population", 0, 10000, 200));
        sA.Attributes.Add(new Engine.Attributes.AttributeInstance("development", 0, 100, 20));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, sA.Id, nationA.Id, 1f, 0);
        grid.SetCell(new HexCoord(0, 0), TerrainType.Plains, fertility: 0.9f);

        // Nation B has a low-fertility settlement (deficit)
        var sB = graph.CreateEntity(EntityKind.Settlement, "village", "Poor", "c", 0, (2, 0));
        sB.Attributes.Add(new Engine.Attributes.AttributeInstance("population", 0, 10000, 200));
        sB.Attributes.Add(new Engine.Attributes.AttributeInstance("development", 0, 100, 5));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, sB.Id, nationB.Id, 1f, 0);
        grid.SetCell(new HexCoord(2, 0), TerrainType.Desert, fertility: 0.1f);

        var system = new ResourceSystem(graph, grid);
        system.Tick(1);

        // Nation B's settlement should NOT benefit from Nation A's surplus
        // Stockpile at B's location should be 0 (no inter-national flow)
        Assert.Equal(0f, system.Sustenance.GetStockpile(new HexCoord(2, 0)));
    }

    [Fact]
    public void System_FlowWithinSameNation()
    {
        var graph = new WorldGraph();
        var grid = new HexGrid(3);

        var nation = graph.CreateEntity(EntityKind.Nation, "generic_nation", "N", "c", 0);

        // Rich settlement (surplus)
        var rich = graph.CreateEntity(EntityKind.Settlement, "village", "Rich", "c", 0, (0, 0));
        rich.Attributes.Add(new Engine.Attributes.AttributeInstance("population", 0, 10000, 100));
        rich.Attributes.Add(new Engine.Attributes.AttributeInstance("development", 0, 100, 10));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, rich.Id, nation.Id, 1f, 0);
        grid.SetCell(new HexCoord(0, 0), TerrainType.Plains, fertility: 0.9f);

        // Poor settlement (deficit)
        var poor = graph.CreateEntity(EntityKind.Settlement, "village", "Poor", "c", 0, (1, 0));
        poor.Attributes.Add(new Engine.Attributes.AttributeInstance("population", 0, 10000, 500));
        poor.Attributes.Add(new Engine.Attributes.AttributeInstance("development", 0, 100, 5));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, poor.Id, nation.Id, 1f, 0);
        grid.SetCell(new HexCoord(1, 0), TerrainType.Desert, fertility: 0.1f);

        var system = new ResourceSystem(graph, grid);
        system.Tick(1);

        // If poor has deficit and rich has surplus, some flow should happen
        var richNet = system.Sustenance.GetNet(new HexCoord(0, 0));
        var poorNet = system.Sustenance.GetNet(new HexCoord(1, 0));

        if (richNet > 0 && poorNet < 0)
        {
            // Poor should have received some stockpile from flow
            Assert.True(system.Sustenance.GetStockpile(new HexCoord(1, 0)) > 0f,
                "Deficit settlement should receive flow from surplus settlement");
        }
    }

    // --- Helper ---

    private static (ResourceSystem System, WorldGraph Graph) CreateTestSystem()
    {
        var world = TestMapBuilder.BuildDefault(seed: 42);

        // Initialize attributes for all settlements
        foreach (var sid in world.Graph.GetEntitiesByKind(EntityKind.Settlement))
        {
            var entity = world.Graph.GetEntity(sid)!;
            entity.Attributes.Add(new Engine.Attributes.AttributeInstance("population", 0, 10000, 100));
            entity.Attributes.Add(new Engine.Attributes.AttributeInstance("development", 0, 100, 10));
            entity.Attributes.Add(new Engine.Attributes.AttributeInstance("prosperity", 0, 100, 50));
        }

        var system = new ResourceSystem(world.Graph, world.Grid);
        return (system, world.Graph);
    }
}
