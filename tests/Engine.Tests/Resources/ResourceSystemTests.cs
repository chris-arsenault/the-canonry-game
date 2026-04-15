namespace Engine.Tests.Resources;

using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Resources;

public class ResourceSystemTests
{
    // --- ResourceLayer ---

    [Fact]
    public void Layer_SetStockpileStoresAmbientValue()
    {
        var layer = new ResourceLayer(ResourceType.Food);
        var coord = new HexCoord(0, 0);

        layer.SetStockpile(coord, 10f);

        Assert.Equal(10f, layer.GetStockpile(coord));
    }

    [Fact]
    public void Layer_SetStockpileClampsNegativeValues()
    {
        var layer = new ResourceLayer(ResourceType.Food);
        var coord = new HexCoord(0, 0);

        layer.SetStockpile(coord, -8f);

        Assert.Equal(0f, layer.GetStockpile(coord));
    }

    [Fact]
    public void Layer_StockpileAccumulates()
    {
        var layer = new ResourceLayer(ResourceType.Wealth);
        var coord = new HexCoord(0, 0);

        layer.AddToStockpile(coord, 10f);
        layer.AddToStockpile(coord, 5f);

        Assert.Equal(15f, layer.GetStockpile(coord));
    }

    [Fact]
    public void Layer_StockpileNeverNegative()
    {
        var layer = new ResourceLayer(ResourceType.Wealth);
        var coord = new HexCoord(0, 0);

        layer.AddToStockpile(coord, 5f);
        layer.AddToStockpile(coord, -20f);

        Assert.Equal(0f, layer.GetStockpile(coord));
    }

    [Fact]
    public void Layer_TotalsAcrossCells()
    {
        var layer = new ResourceLayer(ResourceType.Food);
        layer.SetStockpile(new HexCoord(0, 0), 10f);
        layer.SetStockpile(new HexCoord(1, 0), 5f);

        Assert.Equal(15f, layer.TotalValue);
    }

    // --- ResourceSystem: ambient initialization ---

    [Fact]
    public void System_InitializesAmbientFoodFromMapSeed()
    {
        var (system, _, grid) = CreateTestSystem();

        var coord = new HexCoord(0, -2);
        var cell = grid.GetCell(coord)!;

        Assert.Equal(cell.AmbientFood, system.Food.GetStockpile(coord));
    }

    [Fact]
    public void System_InitializesAmbientWealthFromMapSeed()
    {
        var (system, _, grid) = CreateTestSystem();

        var coord = new HexCoord(0, -2);
        var cell = grid.GetCell(coord)!;

        Assert.Equal(cell.AmbientWealth, system.Wealth.GetStockpile(coord));
    }

    [Fact]
    public void System_DoesNotDeriveAmbientFoodFromSettlementResidents()
    {
        var graph = new WorldGraph();
        var grid = new HexGrid(3);
        var coord = new HexCoord(0, 0);
        grid.SetCell(coord, TerrainType.Plains, fertility: 0.7f);
        var settlement = graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", 0, (0, 0));
        settlement.Attributes.Add(new Engine.Attributes.ScalarValue("residents", 0f, 100000f, 10f));
        settlement.Attributes.Add(new Engine.Attributes.ScalarValue("stored_food", 0f, 10000f, 0f));
        settlement.Attributes.Add(new Engine.Attributes.ScalarValue("security", 0f, 100f, 20f));
        settlement.Attributes.Add(new Engine.Attributes.ScalarValue("prestige", 0f, 1000f, 5f));
        grid.GetCell(coord)!.AmbientFood = 4f;

        var system = new ResourceSystem(graph, grid);
        var initial = system.Food.GetStockpile(coord);

        settlement.Attributes.Modify("residents", 5000f);
        system.Tick(2);

        Assert.Equal(initial, system.Food.GetStockpile(coord));
    }

    [Fact]
    public void System_LeavesAmbientFoodStableAcrossTicksWithoutActions()
    {
        var (system, _, _) = CreateTestSystem();
        var coord = new HexCoord(0, -2);

        var before = system.Food.GetStockpile(coord);
        system.Tick(2);
        system.Tick(3);

        Assert.Equal(before, system.Food.GetStockpile(coord));
    }

    // --- No faction redistribution ---

    [Fact]
    public void System_DoesNotRedistributeAmbientFoodBetweenFactions()
    {
        var graph = new WorldGraph();
        var grid = new HexGrid(3);

        // Two factions, one settlement each
        var nationA = graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "c", 0);
        var nationB = graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "c", 0);

        // Settlement A on fertile land
        var sA = graph.CreateEntity(EntityKind.Settlement, "village", "Rich", "c", 0, (0, 0));
        sA.Attributes.Add(new Engine.Attributes.ScalarValue("residents", 0, 100000, 200));
        sA.Attributes.Add(new Engine.Attributes.ScalarValue("stored_food", 0, 10000, 0));
        sA.Attributes.Add(new Engine.Attributes.ScalarValue("security", 0, 100, 20));
        sA.Attributes.Add(new Engine.Attributes.ScalarValue("prestige", 0, 1000, 20));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, sA.Id, nationA.Id, 1f, 0);
        grid.SetCell(new HexCoord(0, 0), TerrainType.Plains, fertility: 0.9f);
        grid.GetCell(new HexCoord(0, 0))!.AmbientFood = 5f;

        // Settlement B on poor land
        var sB = graph.CreateEntity(EntityKind.Settlement, "village", "Poor", "c", 0, (2, 0));
        sB.Attributes.Add(new Engine.Attributes.ScalarValue("residents", 0, 100000, 200));
        sB.Attributes.Add(new Engine.Attributes.ScalarValue("stored_food", 0, 10000, 0));
        sB.Attributes.Add(new Engine.Attributes.ScalarValue("security", 0, 100, 20));
        sB.Attributes.Add(new Engine.Attributes.ScalarValue("prestige", 0, 1000, 5));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, sB.Id, nationB.Id, 1f, 0);
        grid.SetCell(new HexCoord(2, 0), TerrainType.Desert, fertility: 0.1f);
        grid.GetCell(new HexCoord(2, 0))!.AmbientFood = 1f;

        var system = new ResourceSystem(graph, grid);
        system.Food.SetStockpile(new HexCoord(0, 0), 10f);
        system.Food.SetStockpile(new HexCoord(2, 0), 0f);
        system.Tick(2);

        Assert.Equal(0f, system.Food.GetStockpile(new HexCoord(2, 0)));
    }

    // --- Helper ---

    private static (ResourceSystem System, WorldGraph Graph, HexGrid Grid) CreateTestSystem()
    {
        var world = TestMapBuilder.BuildDefault(seed: 42);

        DomainLoader.InitializeEntityAttributes(world.Graph, TestDomain.Create(), new Random(42));

        var system = new ResourceSystem(world.Graph, world.Grid);
        return (system, world.Graph, world.Grid);
    }
}
