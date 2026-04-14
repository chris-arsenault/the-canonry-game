namespace Engine.Tests.Lifecycle;

using Engine.Attributes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Lifecycle;

public class LifecycleSystemTests
{
    private readonly WorldGraph _graph = new();

    private static LifecycleConfig CreateDefaultConfig()
    {
        var config = new LifecycleConfig();
        // Variance = 0 for deterministic tests
        config.Register(EntityKind.Npc, "hp", new LinearAging { BaseRate = 0.01f, AgeCoefficient = 0.001f, Variance = 0f });
        config.Register(EntityKind.Settlement, "population", new LinearAging { BaseRate = 0.1f, AgeCoefficient = 0.0005f, Variance = 0f });
        config.Register(EntityKind.Nation, "cohesion", new LinearAging { BaseRate = 0.005f, AgeCoefficient = 0.0002f, Variance = 0f });
        config.Register(EntityKind.Artifact, "condition", new LinearAging { BaseRate = 0.005f, AgeCoefficient = 0.0001f, Variance = 0f });
        // Region has no lifecycle config (permanent)
        return config;
    }

    /// <summary>Simulate one tick: update drain rates, apply tick effects, check deaths.</summary>
    private static void SimulateTick(LifecycleSystem system, WorldGraph graph, int tick)
    {
        system.UpdateDrainRates(tick);
        foreach (var (_, entity) in graph.AllEntities)
        {
            if (entity.Status == EntityStatus.Active)
                entity.Attributes.ApplyTickEffects();
        }

        system.CheckDeaths(tick);
    }

    // --- Aging function ---

    [Fact]
    public void LinearAging_NewEntityHasLowDrain()
    {
        var aging = new LinearAging { BaseRate = 0.01f, AgeCoefficient = 0.001f, Variance = 0f };

        var drain = aging.ComputeDrain(0);
        Assert.Equal(0.01f, drain, precision: 4);
    }

    [Fact]
    public void LinearAging_OldEntityHasHigherDrain()
    {
        var aging = new LinearAging { BaseRate = 0.01f, AgeCoefficient = 0.001f, Variance = 0f };

        var youngDrain = aging.ComputeDrain(10);
        var oldDrain = aging.ComputeDrain(100);

        Assert.True(oldDrain > youngDrain);
    }

    [Fact]
    public void LinearAging_DrainIncreasesWithAge()
    {
        // Use Variance=0 for monotonicity guarantee
        var aging = new LinearAging { BaseRate = 0.01f, AgeCoefficient = 0.001f, Variance = 0f };

        float previousDrain = 0;
        for (var age = 0; age <= 100; age += 10)
        {
            var drain = aging.ComputeDrain(age);
            Assert.True(drain >= previousDrain);
            previousDrain = drain;
        }
    }

    [Fact]
    public void LinearAging_WithVariance_ProducesDistinctDrains()
    {
        var aging = new LinearAging { BaseRate = 0.01f, AgeCoefficient = 0.001f, Variance = 0.3f };
        var drains = new HashSet<float>();

        for (var i = 0; i < 20; i++)
        {
            drains.Add(aging.ComputeDrain(50));
        }

        Assert.True(drains.Count > 1, "Variance should produce distinct drain values");
    }

    // --- Lifecycle system ---

    [Fact]
    public void Tick_AppliesDrainToHealthAttribute()
    {
        var npc = _graph.CreateEntity(EntityKind.Npc, "generic_npc", "Hero", "c", tick: 0);
        npc.Attributes.Add(new AttributeInstance("hp", 0f, 100f, 100f));

        var config = CreateDefaultConfig();
        var system = new LifecycleSystem(_graph, config);
        system.InitializeEffects();

        SimulateTick(system, _graph, 1);

        Assert.True(npc.Attributes.GetValue("hp") < 100f);
    }

    [Fact]
    public void Tick_NewEntityDrainsSlowly()
    {
        var npc = _graph.CreateEntity(EntityKind.Npc, "generic_npc", "Young", "c", tick: 0);
        npc.Attributes.Add(new AttributeInstance("hp", 0f, 100f, 100f));

        var config = CreateDefaultConfig();
        var system = new LifecycleSystem(_graph, config);
        system.InitializeEffects();

        // After 10 ticks of a new entity, drain should be minimal
        for (var i = 1; i <= 10; i++)
        {
            SimulateTick(system, _graph,i);
        }

        Assert.True(npc.Attributes.GetValue("hp") > 95f,
            $"Expected > 95, got {npc.Attributes.GetValue("hp")}");
    }

    [Fact]
    public void Tick_OldEntityDrainsFaster()
    {
        // Entity created at tick 0, we start ticking at 500
        var npc = _graph.CreateEntity(EntityKind.Npc, "generic_npc", "Old", "c", tick: 0);
        npc.Attributes.Add(new AttributeInstance("hp", 0f, 100f, 100f));

        var config = CreateDefaultConfig();
        var system = new LifecycleSystem(_graph, config);
        system.InitializeEffects();

        // Tick at age 500-510
        for (var i = 500; i <= 510; i++)
        {
            SimulateTick(system, _graph,i);
        }

        var hpAfterOldAge = npc.Attributes.GetValue("hp");

        // Compare: fresh entity ticking 10 times
        var young = _graph.CreateEntity(EntityKind.Npc, "generic_npc", "Young", "c", tick: 1000);
        young.Attributes.Add(new AttributeInstance("hp", 0f, 100f, 100f));
        for (var i = 1000; i <= 1010; i++)
        {
            SimulateTick(system, _graph,i);
        }

        var hpAfterYoungAge = young.Attributes.GetValue("hp");

        // Old entity should have lost more HP
        Assert.True(hpAfterOldAge < hpAfterYoungAge,
            $"Old HP {hpAfterOldAge} should be < Young HP {hpAfterYoungAge}");
    }

    // --- Death transition ---

    [Fact]
    public void Tick_TransitionsToHistoricalAtZeroHealth()
    {
        var npc = _graph.CreateEntity(EntityKind.Npc, "generic_npc", "Dying", "c", tick: 0);
        npc.Attributes.Add(new AttributeInstance("hp", 0f, 100f, 0.01f)); // Almost dead

        var config = CreateDefaultConfig();
        var system = new LifecycleSystem(_graph, config);
        system.InitializeEffects();

        SimulateTick(system, _graph,1);

        Assert.Equal(EntityStatus.Historical, npc.Status);
    }

    [Fact]
    public void Tick_EmitsDeathEvent()
    {
        var npc = _graph.CreateEntity(EntityKind.Npc, "generic_npc", "Dying", "c", tick: 0);
        npc.Attributes.Add(new AttributeInstance("hp", 0f, 100f, 0.001f));

        var config = CreateDefaultConfig();
        var system = new LifecycleSystem(_graph, config);
        system.InitializeEffects();

        Entity? dead = null;
        system.EntityDied += (_, e) => dead = e.Entity;

        SimulateTick(system, _graph,1);

        Assert.NotNull(dead);
        Assert.Equal(npc.Id, dead.Id);
    }

    [Fact]
    public void Tick_HistoricalEntityNotProcessed()
    {
        var npc = _graph.CreateEntity(EntityKind.Npc, "generic_npc", "Dead", "c", tick: 0);
        npc.Attributes.Add(new AttributeInstance("hp", 0f, 100f, 50f));
        _graph.SetEntityHistorical(npc.Id, tick: 5);

        var config = CreateDefaultConfig();
        var system = new LifecycleSystem(_graph, config);
        system.InitializeEffects();

        SimulateTick(system, _graph,10);

        // HP should not have changed (historical entities are skipped)
        Assert.Equal(50f, npc.Attributes.GetValue("hp"));
    }

    // --- No lifecycle for permanent entities ---

    [Fact]
    public void Tick_SkipsEntitiesWithoutConfig()
    {
        var region = _graph.CreateEntity(EntityKind.Region, "generic_region", "R", "c", tick: 0);
        region.Attributes.Add(new AttributeInstance("fertility", 0f, 1f, 0.8f));

        var config = CreateDefaultConfig(); // No config for Region
        var system = new LifecycleSystem(_graph, config);
        system.InitializeEffects();

        SimulateTick(system, _graph,100);

        // Fertility unchanged (regions have no lifecycle)
        Assert.Equal(0.8f, region.Attributes.GetValue("fertility"));
    }

    // --- Counter-drain (other systems sustain health) ---

    [Fact]
    public void Tick_HealthCanBeCounteredByExternalModification()
    {
        var settlement = _graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", tick: 0);
        settlement.Attributes.Add(new AttributeInstance("population", 0f, 10000f, 1000f));

        var config = CreateDefaultConfig();
        var system = new LifecycleSystem(_graph, config);
        system.InitializeEffects();

        // Simulate 10 ticks where an external system adds population each tick
        for (var i = 1; i <= 10; i++)
        {
            SimulateTick(system, _graph,i); // Drain
            settlement.Attributes.Modify("population", 5f); // External sustenance
        }

        // With sustenance counteracting drain, population should stay near or above 1000
        Assert.True(settlement.Attributes.GetValue("population") > 900f,
            $"Population {settlement.Attributes.GetValue("population")} dropped below 900 despite sustenance");
    }
}
