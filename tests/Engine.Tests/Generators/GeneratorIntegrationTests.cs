namespace Engine.Tests.Generators;

using System.Diagnostics.CodeAnalysis;
using Engine.Domain.Config;
using Engine.Domain.Loader;
using Engine.Domain.IceRemembers;
using Engine.Eventing;
using Engine.Generators;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
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
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$weapon",
                    Kind = TestKinds.Artifact,
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
                    Kind = "home_base",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$weapon",
                    DestinationRef = "$hero",
                    Kind = "anchored",
                },
            ]);
        var sim = SimulationRunner.CreateFromDomain(WithGenerators(generator));
        var initialEntityCount = sim.Graph.EntityCount;
        _ = SetSingleSettlementShelterLow(sim);
        var cursor = sim.KernelEvents.CreateCursor(KernelEventCursorStart.Latest);

        RunTicks(sim, 25);

        var message = CaptureGeneratorFire(sim, cursor);
        Assert.Equal(1, sim.GeneratorSystem.TotalFired);
        Assert.Equal(initialEntityCount + 2, sim.Graph.EntityCount);
        Assert.Equal(2, message.CreatedEntityIds.Count);
        Assert.Equal(2, message.CreatedRelationshipCount);

        var actualTargetId = Assert.IsType<EntityId>(message.TargetEntityId);
        var created = message.CreatedEntityIds
            .Select(id => sim.Graph.GetEntity(id)!)
            .ToDictionary(entity => entity.Kind);
        var hero = created[TestKinds.Penguin];
        var weapon = created[TestKinds.Artifact];

        Assert.Contains(
            sim.Graph.OutgoingRelationships(hero.Id, "home_base"),
            relationship => relationship.TargetId == actualTargetId);
        Assert.Contains(
            sim.Graph.OutgoingRelationships(weapon.Id, "anchored"),
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
    public void WeightedPick_IsPerGenerator_NotPerApplicableTargetCount()
    {
        // Regression: earlier weighting multiplied by applicable-target count, so a
        // generator with many eligible targets unfairly outvoted one with fewer.
        // Here both generators have pulseWeight=1 but different target kinds with
        // different counts (5 settlements vs 3 regions in the test map). Expected
        // share: 50/50. Buggy share would be 5/8 vs 3/8.
        var settlementGen = CreateSettlementGenerator("gen_settlement", pulseWeight: 1, triggers: [AnySettlementTrigger()]);
        var regionGen = new SemanticGeneratorConfig
        {
            Id = "gen_region",
            Name = "gen_region",
            Target = new SemanticGeneratorTarget { Kind = TestKinds.Region },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
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
                    Kind = "home_base",
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Region,
                    Direction = TriggerDirection.Any,
                },
            ],
            PulseWeight = 1,
        };
        var (sim, system) = CreateGeneratorOnlySystem(new Random(42), settlementGen, regionGen);
        var active = new HashSet<string>(StringComparer.Ordinal) { "gen_settlement", "gen_region" };

        for (var pulse = 1; pulse <= 500; pulse++)
        {
            system.Tick(pulse * system.PulseInterval, active);
        }

        var settlementFires = sim.WorldState.Generators.ById["gen_settlement"].TotalRuns;
        var regionFires = sim.WorldState.Generators.ById["gen_region"].TotalRuns;

        Assert.Equal(500, settlementFires + regionFires);
        var settlementShare = settlementFires / 500f;
        Assert.InRange(settlementShare, 0.40f, 0.60f);
    }

    [Fact]
    public void Generator_TargetingSettlement_FiresWhenAnySettlementMatchesTrigger()
    {
        var sim = SimulationRunner.CreateFromDomain(WithGenerators(CreateSettlementGenerator("hero_emergence")));
        SetSingleSettlementShelterLow(sim);

        RunTicks(sim, 25);

        var instance = sim.WorldState.Generators.ById["hero_emergence"];
        Assert.Equal(1, sim.GeneratorSystem.TotalFired);
        Assert.Equal(1, instance.TotalRuns);
        var firedTargetId = Assert.Single(instance.RecentTargets);
        Assert.Equal(TestKinds.Settlement, sim.Graph.GetEntity(firedTargetId)!.Kind);
    }

    [Fact]
    public void Generator_TargetingSettlement_DoesNotFireWhenNoSettlementMatchesTrigger()
    {
        var sim = SimulationRunner.CreateFromDomain(WithGenerators(CreateSettlementGenerator("hero_emergence")));

        // Shelter is drifted by both the aging drift and cross-entity processes;
        // pin it high every tick so the Low trigger never passes during the window.
        for (var i = 0; i < 25; i++)
        {
            SetAllSettlementShelter(sim, 100f);
            sim.Tick();
        }

        Assert.Equal(0, sim.GeneratorSystem.TotalFired);
        Assert.Equal(0, sim.WorldState.Generators.ById["hero_emergence"].TotalRuns);
    }

    [Fact]
    public void Generator_RegionOpening_CreatesRelicAndWitnessBoundToTargetRegion()
    {
        var regionHolding = IceRemembersGeneratorDefinition.Create()
            .Single(generator => generator.Id == "region_opens_the_old_holding");
        var (sim, system) = CreateGeneratorOnlySystem(new Random(42), regionHolding);
        var targetRegionId = sim.Graph.GetEntitiesByKind(TestKinds.Region)
            .OrderBy(id => id.Value)
            .First();
        var cursor = sim.KernelEvents.CreateCursor(KernelEventCursorStart.Latest);

        SetRegionHolding(sim, targetRegionId, highValue: 90f, lowValue: 10f);
        system.Tick(25, new HashSet<string>(StringComparer.Ordinal) { "region_opens_the_old_holding" });

        var message = CaptureGeneratorFire(sim, cursor);
        var relicId = message.CreatedEntityIds.Single(id => sim.Graph.GetEntity(id)!.Kind == TestKinds.Artifact);
        var witnessId = message.CreatedEntityIds.Single(id => sim.Graph.GetEntity(id)!.Kind == TestKinds.Penguin);

        Assert.Equal("region_opens_the_old_holding", message.GeneratorId);
        Assert.Equal(targetRegionId, message.TargetEntityId);
        Assert.Contains(
            sim.Graph.OutgoingRelationships(relicId, "native_to"),
            relationship => relationship.TargetId == targetRegionId);
        Assert.Contains(
            sim.Graph.OutgoingRelationships(witnessId, "holds"),
            relationship => relationship.TargetId == relicId);
    }

    [Fact]
    public void Generator_Disabled_ViaTableauRemoval_StopsFiring()
    {
        var sim = SimulationRunner.CreateFromDomain(WithGenerators(CreateSettlementGenerator("hero_emergence")));
        var card = Assert.Single(sim.Tableau.ActiveCards);

        SetSingleSettlementShelterLow(sim);
        sim.Tableau.RemoveCard(card);
        RunTicks(sim, 50);

        Assert.Equal(0, sim.GeneratorSystem.TotalFired);
        Assert.Equal(0, sim.WorldState.Generators.ById["hero_emergence"].TotalRuns);
    }

    [Fact]
    public void GeneratorInstanceRegistry_Reset_ClearsAllRecordedRuns()
    {
        var sim = SimulationRunner.CreateFromDomain(
            WithGenerators(CreateSettlementGenerator("one_shot", maxRunsPerSim: 1)));
        SetSingleSettlementShelterLow(sim);
        RunTicks(sim, 25);

        var instance = sim.WorldState.Generators.ById["one_shot"];
        Assert.Equal(1, instance.TotalRuns);
        Assert.Equal(25, instance.LastFiredTick);
        Assert.Single(instance.RecentTargets);

        sim.WorldState.Generators.Reset();

        Assert.Equal(0, instance.TotalRuns);
        Assert.Null(instance.LastFiredTick);
        Assert.Empty(instance.RecentTargets);
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
                Kind = TestKinds.Settlement,
            },
            Produces = produces ??
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
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
                    Kind = "home_base",
                },
            ],
            Triggers = triggers ??
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Settlement,
                    AttributeName = "shelter",
                    Direction = TriggerDirection.Low,
                },
            ],
            PulseWeight = pulseWeight,
            MaxRunsPerSim = maxRunsPerSim,
        };

    private static SemanticGeneratorTrigger AnySettlementTrigger() =>
        new()
        {
            OnKind = TestKinds.Settlement,
            Direction = TriggerDirection.Any,
        };

    private static DomainConfig WithGenerators(params SemanticGeneratorConfig[] generators)
    {
        var config = IceRemembersDefinition.Create();
        var semantics = config.Semantics!;

        return new DomainConfig
        {
            Kinds = config.Kinds,
            Processes = config.Processes,
            Relationships = config.Relationships,
            RelationshipFacts = config.RelationshipFacts,
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
            sim.CoverageMap,
            random,
            sim.KernelEvents,
            sim.WorldState,
            sim.DomainIds);

        return (sim, system);
    }

    private static void RunTicks(SimulationRunner sim, int tickCount)
    {
        for (var i = 0; i < tickCount; i++)
        {
            sim.Tick();
        }
    }

    private static CapturedGeneratorFire CaptureGeneratorFire(
        SimulationRunner sim,
        KernelEventCursor cursor)
    {
        while (sim.KernelEvents.TryRead(cursor, out var sequenced))
        {
            if (sequenced.Event.EventType != KernelEventType.GeneratorFired)
            {
                continue;
            }

            var fired = sequenced.Event.AsGeneratorFired();
            var createdEntityIds = sim.Graph.AllEntities.Values
                .Where(entity => entity.CreatedAt == fired.Tick)
                .Select(entity => entity.Id)
                .ToList();
            var generatorId = sim.DomainIds.GeneratorNameOf(new global::Engine.Domain.GeneratorRuntimeId(fired.GeneratorId));
            return new CapturedGeneratorFire(generatorId, fired, createdEntityIds);
        }

        throw new Xunit.Sdk.XunitException("Expected a primitive GeneratorFired kernel event.");
    }

    private sealed record CapturedGeneratorFire(
        string GeneratorId,
        GeneratorFiredKernelEvent Event,
        IReadOnlyList<EntityId> CreatedEntityIds)
    {
        public EntityId? TargetEntityId => new EntityId(Event.TargetEntityId);

        public int CreatedRelationshipCount => Event.CreatedRelationshipCount;
    }

    private static EntityId SetSingleSettlementShelterLow(SimulationRunner sim, float lowValue = 10f, float highValue = 90f)
    {
        var settlements = sim.Graph.GetEntitiesByKind(TestKinds.Settlement)
            .OrderBy(id => id.Value)
            .ToList();
        var targetId = settlements[0];

        foreach (var settlementId in settlements)
        {
            sim.Graph.GetEntity(settlementId)!.Attributes.GetScalar("shelter")!.SetBase(
                settlementId == targetId ? lowValue : highValue);
        }

        return targetId;
    }

    private static void SetAllSettlementShelter(SimulationRunner sim, float value)
    {
        foreach (var settlementId in sim.Graph.GetEntitiesByKind(TestKinds.Settlement))
        {
            sim.Graph.GetEntity(settlementId)!.Attributes.GetScalar("shelter")!.SetBase(value);
        }
    }

    private static void SetRegionHolding(
        SimulationRunner sim,
        EntityId targetRegionId,
        float highValue,
        float lowValue)
    {
        foreach (var regionId in sim.Graph.GetEntitiesByKind(TestKinds.Region))
        {
            sim.Graph.GetEntity(regionId)!.Attributes.GetScalar("holding")!.SetBase(
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
