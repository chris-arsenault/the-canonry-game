namespace Engine.Tests.Simulation;

using Engine.Cards;
using Engine.Processes;
using Engine.Clock;
using Engine.Graph;
using Engine.Kinds;
using Engine.Simulation;

public class SimulationRunnerTests
{
    [Fact]
    public void CreateFromTestMap_Initializes()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        Assert.NotNull(sim.Graph);
        Assert.NotNull(sim.Clock);
        Assert.NotNull(sim.Tableau);
        Assert.NotNull(sim.EventLog);
        Assert.True(sim.Graph.EntityCount > 0);
    }

    [Fact]
    public void Tick_AdvancesSimulation()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        Assert.Equal(0, sim.Clock.CurrentTick);

        sim.Tick();

        Assert.Equal(1, sim.Clock.CurrentTick);
    }

    [Fact]
    public void MultipleTicksRun_WithoutCrashing()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        // Run 100 ticks -- the simulation should not crash
        for (var i = 0; i < 100; i++)
        {
            sim.Tick();
        }

        Assert.Equal(100, sim.Clock.CurrentTick);
    }

    [Fact]
    public void Attributes_ChangeOverTime()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        // Get an NPC and record initial vitality
        var npcId = sim.Graph.GetEntitiesByKind(EntityKind.Npc).First();
        var npc = sim.Graph.GetEntity(npcId)!;
        var initialVitality = npc.Attributes.GetValue("vitality");

        // Run 50 ticks
        for (var i = 0; i < 50; i++)
        {
            sim.Tick();
        }

        var finalVitality = npc.Attributes.GetValue("vitality");

        // Vitality should change from lifecycle drain over time.
        Assert.NotEqual(initialVitality, finalVitality);
    }

    [Fact]
    public void SpeedControl_WorksWithRunner()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        sim.Clock.Speed = SimulationSpeed.Fast; // 2x
        var processed = sim.AdvanceFrame();

        Assert.Equal(2, processed);
        Assert.Equal(2, sim.Clock.CurrentTick);
    }

    [Fact]
    public void Pause_AdvancesNothing()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        sim.Clock.Speed = SimulationSpeed.Paused;
        var processed = sim.AdvanceFrame();

        Assert.Equal(0, processed);
        Assert.Equal(0, sim.Clock.CurrentTick);
    }

    [Fact]
    public void PlaceCard_AndItFires()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        // Create a simple system card that modifies an attribute
        var npcId = sim.Graph.GetEntitiesByKind(EntityKind.Npc).First();
        var card = new CardDefinition("test_boost", "Test Boost", CardType.System,
            new AlwaysTrue(), manaCost: 1, cooldown: 0, cadence: 1, lifespan: 50);
        card.Mutations.Add(new ModifyAttributeMutation(npcId, "vitality", 1f));

        var instance = sim.Tableau.PlaceCard(card, sim.Clock.CurrentTick);
        Assert.NotNull(instance);

        // Run ticks until card fires
        for (var i = 0; i < 5; i++)
        {
            sim.Tick();
        }

        // Card should have fired multiple times
        Assert.True(instance.FireCount > 0, $"Card fired {instance.FireCount} times");
    }

    [Fact]
    public void DegeneracyDetectors_RunPeriodically()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        // Run 20 ticks (detectors run every 10)
        for (var i = 0; i < 20; i++)
        {
            sim.Tick();
        }

        // Should have evaluated detectors at least once
        var severity = sim.Detectors.GetSeverity("settlement_depopulation");
        // We're just checking it ran without crashing
        Assert.True(severity is Engine.Degeneracy.Severity.Normal or Engine.Degeneracy.Severity.Warning or Engine.Degeneracy.Severity.Critical);
    }

    [Fact]
    public void LongRun_SimulationRemainsStable()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        // Run 500 ticks -- simulation should remain stable
        for (var i = 0; i < 500; i++)
        {
            sim.Tick();
        }

        // Should still have entities (anti-degeneracy shock absorbers)
        Assert.True(sim.Graph.EntityCount > 0, "All entities died in 500 ticks");
        Assert.Equal(500, sim.Clock.CurrentTick);
    }

    [Fact]
    public void AllEntityKinds_HaveAttributes()
    {
        var sim = SimulationRunner.CreateFromTestMap();

        foreach (var kind in new[] { EntityKind.Settlement, EntityKind.Faction, EntityKind.Npc, EntityKind.Artifact })
        {
            foreach (var entityId in sim.Graph.GetEntitiesByKind(kind))
            {
                var entity = sim.Graph.GetEntity(entityId)!;
                var attrNames = entity.Attributes.Names.ToList();
                Assert.True(attrNames.Count > 0,
                    $"Entity {entityId} of kind {kind} has no attributes");
            }
        }
    }
}
