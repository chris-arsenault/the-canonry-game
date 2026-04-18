namespace Engine.Tests.Generators;

using System.Diagnostics.CodeAnalysis;
using Engine.Domain.Config;
using Engine.Domain.Loader;
using Engine.Domain.TestDomain;
using Engine.Generators;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Messages;
using Engine.Simulation;

[SuppressMessage(
    "Design",
    "CA1030:Use events where appropriate",
    Justification = "Test names intentionally mirror the ticket acceptance criteria.")]
public class GeneratorIntegrationTests
{
    [Fact]
    public void FullPipeline_SemanticToFiring_CreatesGroupedEntities()
    {
        var generator = CreateSettlementGenerator(
            "hero_package",
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = EntityKind.Npc,
                    Subtype = "hero",
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$weapon",
                    Kind = EntityKind.Artifact,
                    Subtype = "weapon",
                    Role = "weapon",
                    InheritCultureFrom = "$hero",
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$target",
                    Kind = "located_at",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$weapon",
                    DestinationRef = "$hero",
                    Kind = "stored_at",
                },
            ]);
        var sim = SimulationRunner.CreateFromDomain(WithGenerators(generator));
        var initialEntityCount = sim.Graph.EntityCount;
        _ = SetSingleSettlementSanitationLow(sim);
        GeneratorFiredMessage? fired = null;
        sim.MessageBus.Subscribe(message =>
        {
            if (message is GeneratorFiredMessage generatorFired)
            {
                fired = generatorFired;
            }
        });

        RunTicks(sim, 25);

        var message = Assert.IsType<GeneratorFiredMessage>(fired);
        Assert.Equal(1, sim.GeneratorSystem.TotalFired);
        Assert.Equal(initialEntityCount + 2, sim.Graph.EntityCount);
        Assert.Equal(2, message.CreatedEntityIds.Count);
        Assert.Equal(2, message.CreatedRelationshipCount);

        var actualTargetId = Assert.IsType<EntityId>(message.TargetEntityId);
        var created = message.CreatedEntityIds
            .Select(id => sim.Graph.GetEntity(id)!)
            .ToDictionary(entity => entity.Kind);
        var hero = created[EntityKind.Npc];
        var weapon = created[EntityKind.Artifact];

        Assert.Contains(
            sim.Graph.OutgoingRelationships(hero.Id, "located_at"),
            relationship => relationship.TargetId == actualTargetId);
        Assert.Contains(
            sim.Graph.OutgoingRelationships(weapon.Id, "stored_at"),
            relationship => relationship.TargetId == hero.Id);
    }

    [Fact]
    public void MultipleGenerators_OneFiresPerPulse()
    {
        var (sim, system) = CreateGeneratorOnlySystem(
            new Random(42),
            CreateSettlementGenerator("rare", pulseWeight: 1, triggers: [AnySettlementTrigger()]),
            CreateSettlementGenerator("steady", pulseWeight: 2, triggers: [AnySettlementTrigger()]),
            CreateSettlementGenerator("frequent", pulseWeight: 3, triggers: [AnySettlementTrigger()]));
        var activeGeneratorIds = new HashSet<string>(StringComparer.Ordinal) { "rare", "steady", "frequent" };

        for (var pulse = 1; pulse <= 300; pulse++)
        {
            system.Tick(pulse * system.PulseInterval, activeGeneratorIds);
        }

        var rareRuns = sim.WorldState.Generators.ById["rare"].TotalRuns;
        var steadyRuns = sim.WorldState.Generators.ById["steady"].TotalRuns;
        var frequentRuns = sim.WorldState.Generators.ById["frequent"].TotalRuns;
        var totalRuns = rareRuns + steadyRuns + frequentRuns;

        Assert.Equal(300, totalRuns);
        AssertApproximatelyMatchesWeightRatio(rareRuns, totalRuns, expectedShare: 1f / 6f);
        AssertApproximatelyMatchesWeightRatio(steadyRuns, totalRuns, expectedShare: 2f / 6f);
        AssertApproximatelyMatchesWeightRatio(frequentRuns, totalRuns, expectedShare: 3f / 6f);
    }

    [Fact]
    public void Generator_TargetingSettlement_FiresWhenAnySettlementMatchesTrigger()
    {
        var sim = SimulationRunner.CreateFromDomain(WithGenerators(CreateSettlementGenerator("hero_emergence")));
        var targetId = SetSingleSettlementSanitationLow(sim);

        RunTicks(sim, 25);

        Assert.Equal(1, sim.GeneratorSystem.TotalFired);
        Assert.Equal(1, sim.WorldState.Generators.ById["hero_emergence"].TotalRuns);
        Assert.Equal(targetId, sim.WorldState.Generators.ById["hero_emergence"].RecentTargets.Single());

        SetAllSettlementSanitation(sim, 90f);
        RunTicks(sim, 25);

        Assert.Equal(1, sim.GeneratorSystem.TotalFired);
        Assert.Equal(1, sim.WorldState.Generators.ById["hero_emergence"].TotalRuns);
    }

    [Fact]
    public void Generator_Wilderness_PlacesOnFreeHexInRegion()
    {
        var colonyFounding = TestDomainGeneratorDefinition.Create()
            .Single(generator => generator.Id == "colony_founding");
        var (sim, system) = CreateGeneratorOnlySystem(new Random(42), colonyFounding);
        var targetRegionId = FindRegionWithSaturationCapacityAndFreeHex(sim);
        var occupiedBefore = sim.Graph.AllEntities
            .Values
            .Where(entity => entity.Status == EntityStatus.Active)
            .Select(entity => new HexCoord(entity.Coordinates.Q, entity.Coordinates.R))
            .ToHashSet();
        GeneratorFiredMessage? fired = null;
        sim.MessageBus.Subscribe(message =>
        {
            if (message is GeneratorFiredMessage generatorFired)
            {
                fired = generatorFired;
            }
        });

        SetRegionPristineness(sim, targetRegionId, highValue: 90f, lowValue: 10f);
        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "colony_founding" });

        var message = Assert.IsType<GeneratorFiredMessage>(fired);
        var settlementId = message.CreatedEntityIds.Single(id => sim.Graph.GetEntity(id)!.Kind == EntityKind.Settlement);
        var settlement = sim.Graph.GetEntity(settlementId)!;
        var coord = new HexCoord(settlement.Coordinates.Q, settlement.Coordinates.R);
        var cell = sim.Grid.GetCell(coord);

        Assert.Equal("colony_founding", message.GeneratorId);
        Assert.Equal(targetRegionId, message.TargetEntityId);
        Assert.DoesNotContain(coord, occupiedBefore);
        Assert.NotNull(cell);
        Assert.False(float.IsPositiveInfinity(cell!.MovementCost));
        Assert.Contains(
            sim.Graph.OutgoingRelationships(targetRegionId, "contains"),
            relationship => relationship.TargetId == settlementId);
    }

    [Fact]
    public void Generator_Disabled_ViaTableauRemoval_StopsFiring()
    {
        var sim = SimulationRunner.CreateFromDomain(WithGenerators(CreateSettlementGenerator("hero_emergence")));
        var card = Assert.Single(sim.Tableau.ActiveCards);

        SetSingleSettlementSanitationLow(sim);
        sim.Tableau.RemoveCard(card);
        RunTicks(sim, 50);

        Assert.Equal(0, sim.GeneratorSystem.TotalFired);
        Assert.Equal(0, sim.WorldState.Generators.ById["hero_emergence"].TotalRuns);
    }

    [Fact]
    public void Generator_ResetSim_ClearsGeneratorInstanceState()
    {
        var domain = WithGenerators(CreateSettlementGenerator("one_shot", maxRunsPerSim: 1));
        var firstRun = SimulationRunner.CreateFromDomain(domain);

        SetSingleSettlementSanitationLow(firstRun);
        RunTicks(firstRun, 25);

        Assert.Equal(1, firstRun.WorldState.Generators.ById["one_shot"].TotalRuns);

        var resetRun = SimulationRunner.CreateFromDomain(domain);

        Assert.Equal(0, resetRun.WorldState.Generators.ById["one_shot"].TotalRuns);
        SetSingleSettlementSanitationLow(resetRun);
        RunTicks(resetRun, 25);

        Assert.Equal(1, resetRun.WorldState.Generators.ById["one_shot"].TotalRuns);
        Assert.Equal(1, resetRun.GeneratorSystem.TotalFired);
    }

    private static SemanticGeneratorConfig CreateSettlementGenerator(
        string id,
        int pulseWeight = 1,
        int? maxRunsPerSim = null,
        IReadOnlyList<SemanticProducedEntity>? produces = null,
        IReadOnlyList<SemanticGeneratorRelationship>? relationships = null,
        IReadOnlyList<SemanticGeneratorTrigger>? triggers = null) =>
        new()
        {
            Id = id,
            Name = id,
            Target = new SemanticGeneratorTarget
            {
                Kind = EntityKind.Settlement,
            },
            Produces = produces ??
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
            Relationships = relationships ??
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$target",
                    Kind = "located_at",
                },
            ],
            Triggers = triggers ??
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = EntityKind.Settlement,
                    AttributeName = "sanitation",
                    Direction = TriggerDirection.Low,
                },
            ],
            PulseWeight = pulseWeight,
            MaxRunsPerSim = maxRunsPerSim,
        };

    private static SemanticGeneratorTrigger AnySettlementTrigger() =>
        new()
        {
            OnKind = EntityKind.Settlement,
            Direction = TriggerDirection.Any,
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

    private static (SimulationRunner Sim, GeneratorSystem System) CreateGeneratorOnlySystem(
        Random random,
        params SemanticGeneratorConfig[] generators)
    {
        var domain = WithGenerators(generators);
        var definitionGraph = DomainLoader.BuildDefinitionGraph(domain);
        var sim = SimulationRunner.CreateFromDomain(domain);
        var system = new GeneratorSystem(
            sim.Graph,
            sim.WorldState.Generators,
            definitionGraph,
            sim.SpatialIndex,
            sim.WorldState.SemanticPlane,
            sim.MessageBus,
            random);

        return (sim, system);
    }

    private static void RunTicks(SimulationRunner sim, int tickCount)
    {
        for (var i = 0; i < tickCount; i++)
        {
            sim.Tick();
        }
    }

    private static EntityId SetSingleSettlementSanitationLow(SimulationRunner sim, float lowValue = 10f, float highValue = 90f)
    {
        var settlements = sim.Graph.GetEntitiesByKind(EntityKind.Settlement)
            .OrderBy(id => id.Value)
            .ToList();
        var targetId = settlements[0];

        foreach (var settlementId in settlements)
        {
            sim.Graph.GetEntity(settlementId)!.Attributes.GetScalar("sanitation")!.SetBase(
                settlementId == targetId ? lowValue : highValue);
        }

        return targetId;
    }

    private static void SetAllSettlementSanitation(SimulationRunner sim, float value)
    {
        foreach (var settlementId in sim.Graph.GetEntitiesByKind(EntityKind.Settlement))
        {
            sim.Graph.GetEntity(settlementId)!.Attributes.GetScalar("sanitation")!.SetBase(value);
        }
    }

    private static EntityId FindRegionWithSaturationCapacityAndFreeHex(SimulationRunner sim)
    {
        return sim.Graph.GetEntitiesByKind(EntityKind.Region)
            .OrderBy(id => id.Value)
            .First(regionId =>
            {
                var hasCapacity = sim.Graph.OutgoingRelationships(regionId, "contains").Count() < 3;
                if (!hasCapacity)
                {
                    return false;
                }

                var region = sim.Graph.GetEntity(regionId)!;
                var regionOrigin = new HexCoord(region.Coordinates.Q, region.Coordinates.R);
                return sim.SpatialIndex.FindFreePhysicalHex(regionOrigin) is not null;
            });
    }

    private static void SetRegionPristineness(
        SimulationRunner sim,
        EntityId targetRegionId,
        float highValue,
        float lowValue)
    {
        foreach (var regionId in sim.Graph.GetEntitiesByKind(EntityKind.Region))
        {
            sim.Graph.GetEntity(regionId)!.Attributes.GetScalar("pristineness")!.SetBase(
                regionId == targetRegionId ? highValue : lowValue);
        }
    }

    private static void AssertApproximatelyMatchesWeightRatio(int actualRuns, int totalRuns, float expectedShare)
    {
        var actualShare = actualRuns / (float)totalRuns;
        var tolerance = expectedShare * 0.30f;

        Assert.InRange(actualShare, expectedShare - tolerance, expectedShare + tolerance);
    }
}
