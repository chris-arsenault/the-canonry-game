namespace Engine.Tests.Domain;

using Engine.Domain.Loader;
using Engine.Domain.IceRemembers;
using Engine.Kinds;

public class DomainConfigTests
{
    [Fact]
    public void IceRemembers_CreatesValidConfig()
    {
        var config = IceRemembersDefinition.Create();

        Assert.Equal(5, config.Kinds.Count);
        Assert.True(config.Relationships.Count > 10);
        Assert.Equal(4, config.Pressures.Count);
        Assert.Equal(5, config.Lifecycles.Count);
        Assert.Equal(3, config.Resources.Count);
        Assert.Equal(3, config.Cultures.Count);
        Assert.Equal(7, config.Semantics!.Generators.Count);
    }

    [Fact]
    public void IceRemembers_AllMvpKindsPresent()
    {
        var config = IceRemembersDefinition.Create();

        Assert.Contains(TestKinds.Region, config.Kinds.Keys);
        Assert.Contains(TestKinds.Settlement, config.Kinds.Keys);
        Assert.Contains(TestKinds.Faction, config.Kinds.Keys);
        Assert.Contains(TestKinds.Penguin, config.Kinds.Keys);
        Assert.Contains(TestKinds.Artifact, config.Kinds.Keys);
    }

    [Fact]
    public void IceRemembers_KindsHaveSubtypes()
    {
        var config = IceRemembersDefinition.Create();

        foreach (var (_, kindConfig) in config.Kinds)
        {
            Assert.NotEmpty(kindConfig.Subtypes);
        }
    }

    [Fact]
    public void IceRemembers_KindsHaveAttributes()
    {
        var config = IceRemembersDefinition.Create();

        foreach (var (_, kindConfig) in config.Kinds)
        {
            Assert.NotEmpty(kindConfig.Attributes);
        }
    }

    [Fact]
    public void IceRemembers_AllKindsHaveHealthAttribute()
    {
        var config = IceRemembersDefinition.Create();

        foreach (var (_, kindConfig) in config.Kinds)
        {
            Assert.NotNull(kindConfig.HealthAttribute);
        }
    }

    [Fact]
    public void DomainLoader_CreatesKindRegistry()
    {
        var config = IceRemembersDefinition.Create();
        var registry = DomainLoader.LoadKindRegistry(config);

        Assert.NotNull(registry.GetKindDefinition(TestKinds.Settlement));
        Assert.NotNull(registry.GetRelationshipKindDefinition("controlled_by"));
        Assert.True(registry.IsValidSubtype(TestKinds.Penguin, "warden"));
    }

    [Fact]
    public void DomainLoader_CreatesLifecycleConfig()
    {
        var config = IceRemembersDefinition.Create();
        var lifecycleConfig = DomainLoader.LoadLifecycleConfig(config);

        Assert.True(lifecycleConfig.HasConfig(TestKinds.Penguin));
        Assert.True(lifecycleConfig.HasConfig(TestKinds.Settlement));
        Assert.True(lifecycleConfig.HasConfig(TestKinds.Region));
    }

    [Fact]
    public void DomainLoader_CreatesPressureDefinitions()
    {
        var config = IceRemembersDefinition.Create();
        var pressures = DomainLoader.LoadPressureDefinitions(config).ToList();

        Assert.Equal(4, pressures.Count);
    }
}
