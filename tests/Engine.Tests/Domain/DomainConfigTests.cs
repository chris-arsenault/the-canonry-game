namespace Engine.Tests.Domain;

using Engine.Domain.Loader;
using Engine.Domain.TestDomain;
using Engine.Kinds;
using Engine.Simulation;

public class DomainConfigTests
{
    [Fact]
    public void TestDomain_CreatesValidConfig()
    {
        var config = TestDomainDefinition.Create();

        Assert.Equal(5, config.Kinds.Count);
        Assert.True(config.Relationships.Count > 10);
        Assert.Equal(4, config.Pressures.Count);
        Assert.Equal(5, config.Lifecycles.Count);
        Assert.Equal(3, config.Resources.Count);
        Assert.Equal(2, config.Cultures.Count);
    }

    [Fact]
    public void TestDomain_AllMvpKindsPresent()
    {
        var config = TestDomainDefinition.Create();

        Assert.Contains(EntityKind.Region, config.Kinds.Keys);
        Assert.Contains(EntityKind.Settlement, config.Kinds.Keys);
        Assert.Contains(EntityKind.Faction, config.Kinds.Keys);
        Assert.Contains(EntityKind.Npc, config.Kinds.Keys);
        Assert.Contains(EntityKind.Artifact, config.Kinds.Keys);
    }

    [Fact]
    public void TestDomain_KindsHaveSubtypes()
    {
        var config = TestDomainDefinition.Create();

        foreach (var (_, kindConfig) in config.Kinds)
        {
            Assert.NotEmpty(kindConfig.Subtypes);
        }
    }

    [Fact]
    public void TestDomain_KindsHaveAttributes()
    {
        var config = TestDomainDefinition.Create();

        foreach (var (_, kindConfig) in config.Kinds)
        {
            Assert.NotEmpty(kindConfig.Attributes);
        }
    }

    [Fact]
    public void TestDomain_AllKindsHaveHealthAttribute()
    {
        var config = TestDomainDefinition.Create();

        foreach (var (_, kindConfig) in config.Kinds)
        {
            Assert.NotNull(kindConfig.HealthAttribute);
        }
    }

    [Fact]
    public void DomainLoader_CreatesKindRegistry()
    {
        var config = TestDomainDefinition.Create();
        var registry = DomainLoader.LoadKindRegistry(config);

        Assert.NotNull(registry.GetKindDefinition(EntityKind.Settlement));
        Assert.NotNull(registry.GetRelationshipKindDefinition("controlled_by"));
        Assert.True(registry.IsValidSubtype(EntityKind.Npc, "warrior"));
    }

    [Fact]
    public void DomainLoader_CreatesLifecycleConfig()
    {
        var config = TestDomainDefinition.Create();
        var lifecycleConfig = DomainLoader.LoadLifecycleConfig(config);

        Assert.True(lifecycleConfig.HasConfig(EntityKind.Npc));
        Assert.True(lifecycleConfig.HasConfig(EntityKind.Settlement));
        Assert.True(lifecycleConfig.HasConfig(EntityKind.Region));
    }

    [Fact]
    public void DomainLoader_CreatesPressureDefinitions()
    {
        var config = TestDomainDefinition.Create();
        var pressures = DomainLoader.LoadPressureDefinitions(config).ToList();

        Assert.Equal(4, pressures.Count);
    }

    [Fact]
    public void SimulationRunner_CreateFromDomain_Works()
    {
        var config = TestDomainDefinition.Create();
        var sim = SimulationRunner.CreateFromDomain(config);

        Assert.NotNull(sim);
        Assert.True(sim.Graph.EntityCount > 0);

        // Run a few ticks to verify it works
        for (var i = 0; i < 10; i++)
        {
            sim.Tick();
        }

        Assert.Equal(10, sim.Clock.CurrentTick);
    }

    [Fact]
    public void SimulationRunner_CreateFromTestMap_StillWorks()
    {
        // Verify the convenience method still works
        var sim = SimulationRunner.CreateFromTestMap();

        Assert.NotNull(sim);
        sim.Tick();
        Assert.Equal(1, sim.Clock.CurrentTick);
    }
}
