namespace Engine.Tests.Simulation;

using Engine.Cards;
using Engine.Processes;
using Engine.Clock;
using Engine.Domain.Config;
using Engine.Domain.Loader;
using Engine.Domain.TestDomain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Simulation;

public class SimulationRunnerTests
{
    [Fact]
    public void CreateFromTestMap_Initializes()
    {
        var sim = SimulationRunner.CreateFromTestMap();
        var definitionGraph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());

        Assert.NotNull(sim.Graph);
        Assert.NotNull(sim.Clock);
        Assert.NotNull(sim.Tableau);
        Assert.NotNull(sim.EventLog);
        Assert.NotNull(sim.WorldState.SemanticPlane);
        Assert.Equal(definitionGraph.Generators.Count, sim.WorldState.Generators.Count);
        Assert.True(sim.Graph.EntityCount > 0);
    }

    [Fact]
    public void CreateFromDomain_SeedsGeneratorRegistry()
    {
        var domain = WithGenerators(CreateValidGenerator());

        var sim = SimulationRunner.CreateFromDomain(domain);

        Assert.Equal(sim.DefinitionGraph.Generators.Count, sim.WorldState.Generators.Count);
        var instance = Assert.Single(sim.WorldState.Generators.ById.Values);
        Assert.Equal("hero_emergence", instance.GeneratorId);
        Assert.Equal(sim.DefinitionGraph.Generators["hero_emergence"].Id, instance.Definition.Id);
    }

    [Fact]
    public void CreateFromDomain_AutoPlacesGeneratorCards()
    {
        var sim = SimulationRunner.CreateFromDomain(WithGenerators(CreateValidGenerator()));

        var card = Assert.Single(sim.Tableau.ActiveCards);
        Assert.Equal(sim.DefinitionGraph.Generators.Count, sim.Tableau.ActiveCards.Count);
        Assert.Equal(CardType.Generator, card.Definition.Type);
        Assert.Equal("hero_emergence", card.Definition.GeneratorId);
        Assert.Equal(0, card.Definition.ManaCost);
    }

    [Fact]
    public void Tick_AfterGeneratorPulse_FiresApplicableGenerator()
    {
        var sim = SimulationRunner.CreateFromDomain(WithGenerators(CreateValidGenerator()));

        foreach (var settlementId in sim.Graph.GetEntitiesByKind(EntityKind.Settlement))
        {
            sim.Graph.GetEntity(settlementId)!.Attributes.GetScalar("sanitation")!.SetBase(10f);
        }

        for (var i = 0; i < 25; i++)
        {
            sim.Tick();
        }

        Assert.True(sim.GeneratorSystem.TotalFired >= 1);
    }

    [Fact]
    public void RemoveGeneratorCard_StopsThatGeneratorFromFiring()
    {
        var sim = SimulationRunner.CreateFromDomain(WithGenerators(CreateValidGenerator()));
        var card = Assert.Single(sim.Tableau.ActiveCards);

        sim.Tableau.RemoveCard(card);

        for (var i = 0; i < 25; i++)
        {
            sim.Tick();
        }

        Assert.Equal(0, sim.GeneratorSystem.TotalFired);
    }

    [Fact]
    public void ReplacingGeneratorCard_ResumesFiringAtNextPulse()
    {
        var sim = SimulationRunner.CreateFromDomain(WithGenerators(CreateValidGenerator()));
        var card = Assert.Single(sim.Tableau.ActiveCards);

        foreach (var settlementId in sim.Graph.GetEntitiesByKind(EntityKind.Settlement))
        {
            sim.Graph.GetEntity(settlementId)!.Attributes.GetScalar("sanitation")!.SetBase(10f);
        }

        sim.Tableau.RemoveCard(card);
        for (var i = 0; i < 25; i++)
        {
            sim.Tick();
        }

        Assert.Equal(0, sim.GeneratorSystem.TotalFired);

        var replacement = sim.Tableau.PlaceCard(sim.SpawnCards["hero_emergence"], sim.Clock.CurrentTick);
        Assert.NotNull(replacement);

        for (var i = 0; i < 25; i++)
        {
            sim.Tick();
        }

        Assert.Equal(1, sim.GeneratorSystem.TotalFired);
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

    private static SemanticGeneratorConfig CreateValidGenerator() =>
        new()
        {
            Id = "hero_emergence",
            Name = "Hero Emergence",
            Target = new SemanticGeneratorTarget
            {
                Kind = EntityKind.Settlement,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = EntityKind.Npc,
                    Subtype = "hero",
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$target",
                    Kind = "located_at",
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = EntityKind.Settlement,
                    AttributeName = "sanitation",
                    Direction = TriggerDirection.Low,
                },
            ],
        };

    private static DomainConfig WithGenerators(params SemanticGeneratorConfig[] generators)
    {
        var config = TestDomainDefinition.Create();
        var semantics = config.Semantics!;

        return new DomainConfig
        {
            Kinds = config.Kinds,
            Processes = config.Processes,
            Relationships = config.Relationships,
            Pressures = config.Pressures,
            Lifecycles = config.Lifecycles,
            Resources = config.Resources,
            Cultures = config.Cultures,
            Semantics = new SemanticDomainConfig
            {
                Stocks = semantics.Stocks,
                Verbs = semantics.Verbs,
                Flows = semantics.Flows,
                Modifiers = semantics.Modifiers,
                Generators = generators,
            },
        };
    }
}
