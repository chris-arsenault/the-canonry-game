namespace Engine.Tests.Generators;

using Engine.Domain.Config;
using Engine.Domain.Loader;
using Engine.Domain.TestDomain;
using Engine.Generators;
using Engine.Kinds;
using Engine.Simulation;

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
        IReadOnlyList<SemanticGeneratorTrigger>? triggers = null) =>
        new()
        {
            Id = id,
            Name = id,
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
