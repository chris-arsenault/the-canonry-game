namespace Engine.Tests.Resources;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Resources;

public class PopulationResourceTests
{
    [Fact]
    public void Population_InitializesFromHabitability()
    {
        var (system, _) = CreateSystem(fertility: 0.9f);
        var poorSystem = CreateSystem(fertility: 0.1f).System;

        system.Tick(1);
        poorSystem.Tick(1);

        var coord = new HexCoord(0, 0);
        Assert.True(system.GetPopulation(coord) > poorSystem.GetPopulation(coord));
    }

    [Fact]
    public void Population_RemainsStableAcrossTicksWithoutActions()
    {
        var (system, _) = CreateSystem(fertility: 0.5f);
        system.Tick(1);
        var coord = new HexCoord(0, 0);
        var before = system.GetPopulation(coord);

        system.Tick(2);
        system.Tick(3);

        Assert.Equal(before, system.GetPopulation(coord));
    }

    [Fact]
    public void ConsumePopulation_ReducesPopulation()
    {
        var (system, _) = CreateSystem(fertility: 0.5f, ambientPopulation: 500f);
        system.Tick(1);

        var coord = new HexCoord(0, 0);
        var before = system.GetPopulation(coord);

        var consumed = system.ConsumePopulation(coord, 50f);

        Assert.Equal(50f, consumed);
        Assert.Equal(before - 50f, system.GetPopulation(coord));
    }

    [Fact]
    public void ConsumePopulation_ClampsToAvailable()
    {
        var (system, _) = CreateSystem(fertility: 0.5f, ambientPopulation: 100f);
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
        var (system, _) = CreateSystem(fertility: 0.5f, ambientPopulation: 200f);
        system.Tick(1);

        var coord = new HexCoord(0, 0);
        Assert.True(system.HasMinimumPopulation(coord, 50f));
        Assert.False(system.HasMinimumPopulation(coord, 100000f));
    }

    [Fact]
    public void Population_DoesNotFlowWithinFaction()
    {
        var graph = new WorldGraph();
        var grid = new HexGrid(3);

        var nation = graph.CreateEntity(EntityKind.Faction, "generic_faction", "N", "c", 0);

        // Rich cell
        var rich = graph.CreateEntity(EntityKind.Settlement, "village", "Rich", "c", 0, (0, 0));
        rich.Attributes.Add(new Engine.Attributes.ScalarValue("residents", 0, 100000, 1000));
        rich.Attributes.Add(new Engine.Attributes.ScalarValue("stored_food", 0, 10000, 0));
        rich.Attributes.Add(new Engine.Attributes.ScalarValue("security", 0, 100, 20));
        rich.Attributes.Add(new Engine.Attributes.ScalarValue("prestige", 0, 1000, 10));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, rich.Id, nation.Id, 1f, 0);
        grid.SetCell(new HexCoord(0, 0), TerrainType.Plains, fertility: 0.9f);

        // Poor cell
        var poor = graph.CreateEntity(EntityKind.Settlement, "village", "Poor", "c", 0, (1, 0));
        poor.Attributes.Add(new Engine.Attributes.ScalarValue("residents", 0, 100000, 50));
        poor.Attributes.Add(new Engine.Attributes.ScalarValue("stored_food", 0, 10000, 0));
        poor.Attributes.Add(new Engine.Attributes.ScalarValue("security", 0, 100, 20));
        poor.Attributes.Add(new Engine.Attributes.ScalarValue("prestige", 0, 1000, 5));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, poor.Id, nation.Id, 1f, 0);
        grid.SetCell(new HexCoord(1, 0), TerrainType.Desert, fertility: 0.1f);

        var system = new ResourceSystem(graph, grid);
        system.Tick(1);
        system.PopulationLayer.SetStockpile(new HexCoord(0, 0), 100f);
        system.PopulationLayer.SetStockpile(new HexCoord(1, 0), 5f);
        system.Tick(2);

        var richPop = system.GetPopulation(new HexCoord(0, 0));
        var poorPop = system.GetPopulation(new HexCoord(1, 0));

        Assert.Equal(100f, richPop);
        Assert.Equal(5f, poorPop);
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
        sA.Attributes.Add(new Engine.Attributes.ScalarValue("stored_food", 0, 10000, 0));
        sA.Attributes.Add(new Engine.Attributes.ScalarValue("security", 0, 100, 20));
        sA.Attributes.Add(new Engine.Attributes.ScalarValue("prestige", 0, 1000, 10));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, sA.Id, nationA.Id, 1f, 0);
        grid.SetCell(new HexCoord(0, 0), TerrainType.Plains, fertility: 0.9f);

        var sB = graph.CreateEntity(EntityKind.Settlement, "village", "SB", "c", 0, (2, 0));
        sB.Attributes.Add(new Engine.Attributes.ScalarValue("residents", 0, 100000, 10));
        sB.Attributes.Add(new Engine.Attributes.ScalarValue("stored_food", 0, 10000, 0));
        sB.Attributes.Add(new Engine.Attributes.ScalarValue("security", 0, 100, 20));
        sB.Attributes.Add(new Engine.Attributes.ScalarValue("prestige", 0, 1000, 5));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, sB.Id, nationB.Id, 1f, 0);
        grid.SetCell(new HexCoord(2, 0), TerrainType.Desert, fertility: 0.1f);

        var system = new ResourceSystem(graph, grid);
        system.Tick(1);

        system.PopulationLayer.SetStockpile(new HexCoord(0, 0), 100f);
        system.PopulationLayer.SetStockpile(new HexCoord(2, 0), 5f);
        system.Tick(2);

        Assert.Equal(5f, system.GetPopulation(new HexCoord(2, 0)));
    }

    // --- Helper ---

    private static (ResourceSystem System, WorldGraph Graph) CreateSystem(
        float fertility = 0.5f, float ambientPopulation = -1f)
    {
        var graph = new WorldGraph();
        var grid = new HexGrid(3);

        var nation = graph.CreateEntity(EntityKind.Faction, "generic_faction", "N", "c", 0);
        var settlement = graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", 0, (0, 0));
        settlement.Attributes.Add(new Engine.Attributes.ScalarValue("residents", 0, 100000, 10f));
        settlement.Attributes.Add(new Engine.Attributes.ScalarValue("stored_food", 0, 10000, 0f));
        settlement.Attributes.Add(new Engine.Attributes.ScalarValue("security", 0, 100, 20f));
        settlement.Attributes.Add(new Engine.Attributes.ScalarValue("prestige", 0, 1000, 10));
        graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, settlement.Id, nation.Id, 1f, 0);
        grid.SetCell(new HexCoord(0, 0), TerrainType.Plains, fertility: fertility);

        var system = new ResourceSystem(graph, grid);
        if (ambientPopulation >= 0f)
        {
            system.Tick(1);
            system.PopulationLayer.SetStockpile(new HexCoord(0, 0), ambientPopulation);
        }

        return (system, graph);
    }
}
