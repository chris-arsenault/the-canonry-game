namespace Engine.Tests.Generators;

using Engine.Domain.Config;
using Engine.Domain.Loader;
using Engine.Domain.TestDomain;
using Engine.Generators;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Messages;
using Engine.Simulation;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1030:Use events where appropriate",
    Justification = "Test names intentionally mirror the ticket acceptance criteria.")]
public class GeneratorSystemTests
{
    [Fact]
    public void Tick_BeforePulseInterval_DoesNothing()
    {
        var (sim, system) = CreateSystem(new Random(42), CreateGenerator("hero_emergence"));

        system.Tick(5, new HashSet<string>(StringComparer.Ordinal) { "hero_emergence" });

        var instance = sim.WorldState.Generators.ById["hero_emergence"];
        Assert.Equal(0, instance.TotalRuns);
        Assert.Null(instance.LastFiredTick);
    }

    [Fact]
    public void Tick_AtPulse_NoActiveGenerators_DoesNothing()
    {
        var (sim, system) = CreateSystem(new Random(42), CreateGenerator("hero_emergence"));

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal));

        var instance = sim.WorldState.Generators.ById["hero_emergence"];
        Assert.Equal(0, instance.TotalRuns);
        Assert.Null(instance.LastFiredTick);
    }

    [Fact]
    public void Tick_AtPulse_TriggersGateApplicability()
    {
        var (sim, system) = CreateSystem(
            new Random(42),
            CreateGenerator(
                "crowding_event",
                triggers:
                [
                    new SemanticGeneratorTrigger
                    {
                        OnKind = EntityKind.Settlement,
                        AttributeName = "residents",
                        Direction = TriggerDirection.High,
                    },
                ]));

        foreach (var settlementId in sim.Graph.GetEntitiesByKind(EntityKind.Settlement))
        {
            sim.Graph.GetEntity(settlementId)!.Attributes.GetScalar("residents")!.SetBase(10f);
        }

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "crowding_event" });

        var instance = sim.WorldState.Generators.ById["crowding_event"];
        Assert.Equal(0, instance.TotalRuns);
        Assert.Null(instance.LastFiredTick);
    }

    [Fact]
    public void Tick_AtPulse_MaxRunsPerSim_Respected()
    {
        var (sim, system) = CreateSystem(
            new Random(42),
            CreateGenerator("one_shot", maxRunsPerSim: 1));

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "one_shot" });
        system.Tick(50, new HashSet<string>(StringComparer.Ordinal) { "one_shot" });

        var instance = sim.WorldState.Generators.ById["one_shot"];
        Assert.Equal(1, instance.TotalRuns);
        Assert.Equal(25, instance.LastFiredTick);
    }

    [Fact]
    public void Tick_AtPulse_WeightedRandom_DistributesCorrectly()
    {
        var (sim, system) = CreateSystem(
            new Random(42),
            CreateGenerator("rare_event", pulseWeight: 1),
            CreateGenerator("common_event", pulseWeight: 4));

        for (var pulse = 1; pulse <= 1000; pulse++)
        {
            system.Tick(
                pulse * system.PulseInterval,
                new HashSet<string>(StringComparer.Ordinal) { "rare_event", "common_event" });
        }

        var rareRuns = sim.WorldState.Generators.ById["rare_event"].TotalRuns;
        var commonRuns = sim.WorldState.Generators.ById["common_event"].TotalRuns;
        var totalRuns = rareRuns + commonRuns;
        var rareShare = rareRuns / (float)totalRuns;

        Assert.Equal(1000, totalRuns);
        Assert.InRange(rareShare, 0.15f, 0.25f);
        Assert.True(commonRuns > rareRuns);
    }

    [Fact]
    public void Fire_CreatesAllEntitiesInProducesList()
    {
        var generator = CreateGenerator(
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

        var (_, _, _, message) = FireAndCaptureMessage(new Random(42), generator);

        Assert.Equal(2, message.CreatedEntityIds.Count);
        Assert.Contains(message.CreatedEntityIds, id => id.Value >= 0);
    }

    [Fact]
    public void Fire_AppliesCreateChance_SomeEntitiesOptionallyAbsent()
    {
        var generator = CreateGenerator(
            "optional_weapon",
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$weapon",
                    Kind = EntityKind.Artifact,
                    Subtype = "weapon",
                    Role = "weapon",
                    CreateChance = 0f,
                },
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = EntityKind.Npc,
                    Subtype = "hero",
                    Role = "hero",
                    InheritCultureFrom = "$target",
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

        var (sim, _, _, message) = FireAndCaptureMessage(new Random(42), generator);

        Assert.Single(message.CreatedEntityIds);
        Assert.Equal(EntityKind.Npc, sim.Graph.GetEntity(message.CreatedEntityIds[0])!.Kind);
    }

    [Fact]
    public void Fire_CultureInheritance_FromTarget_Works()
    {
        var generator = CreateGenerator("culture_inheritance");

        var (sim, _, target, message) = FireAndCaptureMessage(new Random(42), generator);

        var created = sim.Graph.GetEntity(message.CreatedEntityIds[0])!;
        Assert.Equal(target.Culture, created.Culture);
    }

    [Fact]
    public void Fire_Relationships_WiredCorrectly_WhenAllEntitiesPresent()
    {
        var generator = CreateGenerator(
            "relationship_package",
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

        var (sim, _, target, message) = FireAndCaptureMessage(new Random(42), generator);
        var createdEntities = message.CreatedEntityIds
            .Select(id => sim.Graph.GetEntity(id)!)
            .ToDictionary(entity => entity.Kind);
        var hero = createdEntities[EntityKind.Npc];
        var weapon = createdEntities[EntityKind.Artifact];

        Assert.Contains(
            sim.Graph.OutgoingRelationships(hero.Id, "located_at"),
            relationship => relationship.TargetId == target.Id);
        Assert.Contains(
            sim.Graph.OutgoingRelationships(weapon.Id, "stored_at"),
            relationship => relationship.TargetId == hero.Id);
        Assert.Equal(2, message.CreatedRelationshipCount);
    }

    [Fact]
    public void Fire_Relationships_AutoSkipWhenOptionalRefMissing()
    {
        var generator = CreateGenerator(
            "optional_relationship_skip",
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$weapon",
                    Kind = EntityKind.Artifact,
                    Subtype = "weapon",
                    Role = "weapon",
                    CreateChance = 0f,
                },
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = EntityKind.Npc,
                    Subtype = "hero",
                    Role = "hero",
                    InheritCultureFrom = "$target",
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

        var (sim, _, target, message) = FireAndCaptureMessage(new Random(42), generator);
        var heroId = Assert.Single(message.CreatedEntityIds);

        Assert.Contains(
            sim.Graph.OutgoingRelationships(heroId, "located_at"),
            relationship => relationship.TargetId == target.Id);
        Assert.DoesNotContain(
            sim.Graph.GetEntitiesByKind(EntityKind.Artifact),
            entityId => message.CreatedEntityIds.Contains(entityId));
        Assert.Equal(1, message.CreatedRelationshipCount);
    }

    [Fact]
    public void Fire_EntitiesPlacedNearTarget_OnBothPlanes()
    {
        var generator = CreateGenerator("near_placement");

        var (sim, _, target, message) = FireAndCaptureMessage(new Random(42), generator);
        var created = sim.Graph.GetEntity(message.CreatedEntityIds[0])!;
        var targetPhysical = new HexCoord(target.Coordinates.Q, target.Coordinates.R);

        Assert.Equal(1, HexCoord.Distance(targetPhysical, new HexCoord(created.Coordinates.Q, created.Coordinates.R)));
        Assert.Equal(1, SemanticHexCoord.Distance(target.SemanticPosition, created.SemanticPosition));
    }

    [Fact]
    public void Fire_EmitsGeneratorFiredMessage()
    {
        var generator = CreateGenerator("message_emission");

        var (_, _, target, message) = FireAndCaptureMessage(new Random(42), generator, currentTick: 25);

        Assert.Equal("message_emission", message.GeneratorId);
        Assert.Equal(target.Id, message.TargetEntityId);
        Assert.Single(message.CreatedEntityIds);
        Assert.Equal(1, message.CreatedRelationshipCount);
        Assert.Equal(25, message.Tick);
    }

    [Fact]
    public void Tick_AllowWilderness_KeepsRegionTargetForRelationshipsAndInheritsRegionCulture()
    {
        var generator = CreateGenerator(
            "colony_founding",
            target: new SemanticGeneratorTarget
            {
                Kind = EntityKind.Region,
                AllowWilderness = true,
                SaturationLimits =
                [
                    new SemanticSaturationLimit
                    {
                        RelationshipKind = "contains",
                        Direction = "out",
                        MaxCount = 3,
                    },
                ],
            },
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$settlement",
                    Kind = EntityKind.Settlement,
                    Subtype = "village",
                    Role = "primary",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$leader",
                    Kind = EntityKind.Npc,
                    Subtype = "warrior",
                    Role = "leader",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$faction",
                    Kind = EntityKind.Faction,
                    Subtype = "generic_faction",
                    Role = "founding_faction",
                    InheritCultureFrom = "$target",
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$leader",
                    DestinationRef = "$settlement",
                    Kind = "located_at",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$settlement",
                    DestinationRef = "$faction",
                    Kind = "controlled_by",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$leader",
                    DestinationRef = "$faction",
                    Kind = "loyal_to",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$target",
                    DestinationRef = "$settlement",
                    Kind = "contains",
                },
            ],
            triggers:
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = EntityKind.Region,
                    AttributeName = "pristineness",
                    Direction = TriggerDirection.High,
                },
            ]);
        var (sim, system) = CreateSystem(new Random(42), generator);
        var regionIds = sim.Graph.GetEntitiesByKind(EntityKind.Region).ToHashSet();
        var settlementIds = sim.Graph.GetEntitiesByKind(EntityKind.Settlement).ToHashSet();

        foreach (var regionId in regionIds)
        {
            sim.Graph.GetEntity(regionId)!.Attributes.GetScalar("pristineness")!.SetBase(90f);
        }

        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "colony_founding" });

        var newSettlementId = sim.Graph.GetEntitiesByKind(EntityKind.Settlement).Single(id => !settlementIds.Contains(id));
        var newSettlement = sim.Graph.GetEntity(newSettlementId)!;
        var containingRegionId = regionIds.Single(regionId =>
            sim.Graph.OutgoingRelationships(regionId, "contains")
                .Any(relationship => relationship.TargetId == newSettlementId));

        Assert.Equal("none", newSettlement.Culture);
        Assert.Contains(containingRegionId, regionIds);
    }

    private static (SimulationRunner Sim, GeneratorSystem System) CreateSystem(
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

    private static SemanticGeneratorConfig CreateGenerator(
        string id,
        int pulseWeight = 1,
        int? maxRunsPerSim = null,
        SemanticGeneratorTarget? target = null,
        IReadOnlyList<SemanticProducedEntity>? produces = null,
        IReadOnlyList<SemanticGeneratorRelationship>? relationships = null,
        IReadOnlyList<SemanticGeneratorTrigger>? triggers = null) =>
        new()
        {
            Id = id,
            Name = id,
            Target = target ?? new SemanticGeneratorTarget
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
                    Direction = TriggerDirection.Any,
                },
            ],
            PulseWeight = pulseWeight,
            MaxRunsPerSim = maxRunsPerSim,
        };

    private static (SimulationRunner Sim, GeneratorSystem System, Entity Target, GeneratorFiredMessage Message)
        FireAndCaptureMessage(Random random, SemanticGeneratorConfig generator, int currentTick = 25)
    {
        var (sim, system) = CreateSystem(random, generator);
        GeneratorFiredMessage? message = null;
        sim.MessageBus.Subscribe(msg =>
        {
            if (msg is GeneratorFiredMessage fired)
            {
                message = fired;
            }
        });

        system.Tick(currentTick, new HashSet<string>(StringComparer.Ordinal) { generator.Id });

        var captured = Assert.IsType<GeneratorFiredMessage>(message);
        var target = sim.Graph.GetEntity(captured.TargetEntityId!.Value)!;
        return (sim, system, target, captured);
    }

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
