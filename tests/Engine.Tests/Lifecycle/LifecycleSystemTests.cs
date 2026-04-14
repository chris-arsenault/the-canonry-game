namespace Engine.Tests.Lifecycle;

using Engine.Attributes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Lifecycle;

public class LifecycleSystemTests
{
    [Fact]
    public void InitializeEffects_RegistersDrainOnHealthAttributeForActiveEntities()
    {
        var graph = new WorldGraph();
        var settlement = CreateSettlement(graph, "Town", population: 10f, createdAt: 0);
        var config = CreateConfig();

        var system = new LifecycleSystem(graph, config);
        system.InitializeEffects();

        var effect = Assert.Single(settlement.Attributes.GetScalar("population")!.TickEffects);
        Assert.Equal("lifecycle_drain_population", effect.SourceId);
        Assert.Equal(-0.1f, effect.Value, precision: 3);
    }

    [Fact]
    public void UpdateDrainRates_IncreasesDrainWithAge()
    {
        var graph = new WorldGraph();
        var settlement = CreateSettlement(graph, "Town", population: 10f, createdAt: 0);
        var config = CreateConfig();
        var system = new LifecycleSystem(graph, config);
        system.InitializeEffects();

        system.UpdateDrainRates(10);

        var effect = Assert.Single(settlement.Attributes.GetScalar("population")!.TickEffects);
        Assert.Equal(-(0.1f + (10f * 0.01f)), effect.Value, precision: 3);
    }

    [Fact]
    public void CheckDeaths_TransitionsEntityToHistoricalWhenHealthIsZeroOrLess()
    {
        var graph = new WorldGraph();
        var settlement = CreateSettlement(graph, "Town", population: 0f, createdAt: 0);
        var system = new LifecycleSystem(graph, CreateConfig());

        system.CheckDeaths(5);

        Assert.Equal(EntityStatus.Historical, settlement.Status);
    }

    [Fact]
    public void CheckDeaths_EmitsEntityDiedEvent()
    {
        var graph = new WorldGraph();
        var settlement = CreateSettlement(graph, "Town", population: 0f, createdAt: 0);
        var system = new LifecycleSystem(graph, CreateConfig());
        LifecycleDeathEventArgs? args = null;
        system.EntityDied += (_, eventArgs) => args = eventArgs;

        system.CheckDeaths(5);

        Assert.NotNull(args);
        Assert.Equal(settlement.Id, args.Entity.Id);
        Assert.Equal(5, args.DeathTick);
    }

    [Fact]
    public void UpdateDrainRates_SkipsHistoricalEntities()
    {
        var graph = new WorldGraph();
        var settlement = CreateSettlement(graph, "Town", population: 10f, createdAt: 0);
        var config = CreateConfig();
        var system = new LifecycleSystem(graph, config);
        system.InitializeEffects();
        graph.SetEntityHistorical(settlement.Id, 1);

        var before = settlement.Attributes.GetScalar("population")!.TickEffects[0].Value;
        system.UpdateDrainRates(10);

        Assert.Equal(before, settlement.Attributes.GetScalar("population")!.TickEffects[0].Value);
    }

    [Fact]
    public void InitializeEffects_AndCheckDeaths_SkipEntitiesWithoutLifecycleConfig()
    {
        var graph = new WorldGraph();
        var region = graph.CreateEntity(EntityKind.Region, "generic_region", "Region", "c", 0, (0, 0));
        region.Attributes.Add(new ScalarValue("fertility", 0f, 1f, 0.5f));

        var system = new LifecycleSystem(graph, CreateConfig());
        system.InitializeEffects();
        system.UpdateDrainRates(10);
        system.CheckDeaths(10);

        Assert.Empty(region.Attributes.GetScalar("fertility")!.TickEffects);
        Assert.Equal(EntityStatus.Active, region.Status);
    }

    [Fact]
    public void DrainEffect_IsVisibleOnAttributesAndCanBeCounteractedExternally()
    {
        var graph = new WorldGraph();
        var settlement = CreateSettlement(graph, "Town", population: 10f, createdAt: 0);
        var system = new LifecycleSystem(graph, CreateConfig());
        system.InitializeEffects();

        system.UpdateDrainRates(10);
        settlement.Attributes.ApplyTickEffects();
        settlement.Attributes.Modify("population", 1f);

        var effect = Assert.Single(settlement.Attributes.GetScalar("population")!.TickEffects);
        Assert.True(effect.Value < 0f);
        Assert.True(settlement.Attributes.GetValue("population") > 9f);
    }

    private static LifecycleConfig CreateConfig()
    {
        var config = new LifecycleConfig();
        config.Register(
            EntityKind.Settlement,
            "population",
            new LinearAging(new Random(42)) { BaseRate = 0.1f, AgeCoefficient = 0.01f, Variance = 0f });
        return config;
    }

    private static Entity CreateSettlement(WorldGraph graph, string name, float population, int createdAt)
    {
        var entity = graph.CreateEntity(EntityKind.Settlement, "village", name, "c", createdAt, (0, 0));
        entity.Attributes.Add(new ScalarValue("population", 0f, 100f, population));
        return entity;
    }
}
