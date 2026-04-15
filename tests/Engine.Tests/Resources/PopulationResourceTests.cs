namespace Engine.Tests.Resources;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Resources;

public class PopulationResourceTests
{
    [Fact]
    public void Population_GrowsFromSustenanceSurplus()
    {
        // Low residents + high fertility = food surplus = ambient population growth
        // Production = 0.9 * (50/100) * 1 = 0.45, Consumption = 50 * 0.01 = 0.5
        // Still deficit at these rates. Increase production factor.
        var (system, _) = CreateSystem(fertility: 0.9f, population: 50);
        system.FoodProductionFactor = 3f; // Boost production to ensure surplus

        system.Tick(1);

        var coord = new HexCoord(0, 0);
        var pop = system.GetPopulation(coord);
        Assert.True(pop > 50f, $"Population {pop} should have grown from food surplus");
    }

    [Fact]
    public void Population_DeclinesFromSustenanceDeficit()
    {
        // Very low fertility, very high population = deficit
        var (system, _) = CreateSystem(fertility: 0.05f, population: 5000);

        system.Tick(1);

        var coord = new HexCoord(0, 0);
        var pop = system.GetPopulation(coord);
        Assert.True(pop < 5000f, $"Population {pop} should have declined from deficit");
    }

    [Fact]
    public void ConsumePopulation_ReducesPopulation()
    {
        var (system, _) = CreateSystem(fertility: 0.5f, population: 500);
        system.Tick(1); // Initialize population stockpile

        var coord = new HexCoord(0, 0);
        var before = system.GetPopulation(coord);

        var consumed = system.ConsumePopulation(coord, 50f);

        Assert.Equal(50f, consumed);
        Assert.Equal(before - 50f, system.GetPopulation(coord));
    }

    [Fact]
    public void ConsumePopulation_ClampsToAvailable()
    {
        var (system, _) = CreateSystem(fertility: 0.5f, population: 100);
        system.Tick(1);

        var coord = new HexCoord(0, 0);
        var available = system.GetPopulation(coord);

        var consumed = system.ConsumePopulation(coord, available + 1000f);

        Assert.Equal(available, consumed); // Can't consume more than available
        Assert.Equal(0f, system.GetPopulation(coord));
    }

    [Fact]
    public void HasMinimumPopulation_ChecksThreshold()
    {
        var (system, _) = CreateSystem(fertility: 0.5f, population: 200);
        system.Tick(1);

        var coord = new HexCoord(0, 0);
        Assert.True(system.HasMinimumPopulation(coord, 50f));
        Assert.False(system.HasMinimumPopulation(coord, 100000f));
    }

    [Fact]
    public void Population_FlowsWithinNation()
    {
        var graph = new WorldGraph();
        var grid = new HexGrid(3);

        var nation = graph.CreateEntity(EntityKind.Faction, "generic_faction", "N", "c", 0);

        // Rich cell
        var rich = graph.CreateEntity(EntityKind.Settlement, "village", "Rich", "c", 0, (0, 0));
        rich.Attributes.Add(new Engine.Attributes.ScalarValue("residents", 0, 100000, 1000));
        rich.Attributes.Add(new Engine.Attributes.ScalarValue("prestige", 0, 1000, 10));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, rich.Id, nation.Id, 1f, 0);
        grid.SetCell(new HexCoord(0, 0), TerrainType.Plains, fertility: 0.9f);

        // Poor cell
        var poor = graph.CreateEntity(EntityKind.Settlement, "village", "Poor", "c", 0, (1, 0));
        poor.Attributes.Add(new Engine.Attributes.ScalarValue("residents", 0, 100000, 50));
        poor.Attributes.Add(new Engine.Attributes.ScalarValue("prestige", 0, 1000, 5));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, poor.Id, nation.Id, 1f, 0);
        grid.SetCell(new HexCoord(1, 0), TerrainType.Desert, fertility: 0.1f);

        var system = new ResourceSystem(graph, grid);

        // Run multiple ticks to see population flow
        for (var i = 0; i < 5; i++)
        {
            system.Tick(i);
        }

        // Population at the rich cell should be higher than the poor cell
        // and flow should have moved some population toward the poor cell
        var richPop = system.GetPopulation(new HexCoord(0, 0));
        var poorPop = system.GetPopulation(new HexCoord(1, 0));

        Assert.True(richPop > poorPop, "Rich cell should still have more population");
        Assert.True(poorPop > 0, "Poor cell should have some population from flow");
    }

    [Fact]
    public void Population_DoesNotFlowBetweenFactions()
    {
        var graph = new WorldGraph();
        var grid = new HexGrid(3);

        var nationA = graph.CreateEntity(EntityKind.Faction, "generic_faction", "A", "c", 0);
        var nationB = graph.CreateEntity(EntityKind.Faction, "generic_faction", "B", "c", 0);

        var sA = graph.CreateEntity(EntityKind.Settlement, "village", "SA", "c", 0, (0, 0));
        sA.Attributes.Add(new Engine.Attributes.ScalarValue("residents", 0, 100000, 1000));
        sA.Attributes.Add(new Engine.Attributes.ScalarValue("prestige", 0, 1000, 10));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, sA.Id, nationA.Id, 1f, 0);
        grid.SetCell(new HexCoord(0, 0), TerrainType.Plains, fertility: 0.9f);

        var sB = graph.CreateEntity(EntityKind.Settlement, "village", "SB", "c", 0, (2, 0));
        sB.Attributes.Add(new Engine.Attributes.ScalarValue("residents", 0, 100000, 10));
        sB.Attributes.Add(new Engine.Attributes.ScalarValue("prestige", 0, 1000, 5));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, sB.Id, nationB.Id, 1f, 0);
        grid.SetCell(new HexCoord(2, 0), TerrainType.Desert, fertility: 0.1f);

        var system = new ResourceSystem(graph, grid);
        system.Tick(1);

        // Faction B's settlement should not receive population from Faction A
        Assert.True(system.GetPopulation(new HexCoord(2, 0)) <= 10f + 5f, // Only initial + possible small growth
            "Population should not flow between nations");
    }

    // --- Helper ---

    private static (ResourceSystem System, WorldGraph Graph) CreateSystem(
        float fertility = 0.5f, float population = 100f)
    {
        var graph = new WorldGraph();
        var grid = new HexGrid(3);

        var nation = graph.CreateEntity(EntityKind.Faction, "generic_faction", "N", "c", 0);
        var settlement = graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", 0, (0, 0));
        settlement.Attributes.Add(new Engine.Attributes.ScalarValue("residents", 0, 100000, population));
        settlement.Attributes.Add(new Engine.Attributes.ScalarValue("prestige", 0, 1000, 10));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, settlement.Id, nation.Id, 1f, 0);
        grid.SetCell(new HexCoord(0, 0), TerrainType.Plains, fertility: fertility);

        return (new ResourceSystem(graph, grid), graph);
    }
}
