namespace Engine.Domain.Tests.Domain;

using Engine.Domain.Config;
using Engine.Domain.Loader;
using IceRemembers;
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
    public void IceRemembers_InitialAttributesUseStandardizedBands()
    {
        var config = IceRemembersDefinition.Create();
        var settlement = config.Kinds[TestKinds.Settlement];

        Assert.Equal(IceRemembersAttributeId.TempHp.ToDomainName(), settlement.HealthAttribute);
        var settlementHealth = Assert.Single(
            settlement.Attributes,
            attribute => attribute.Name == IceRemembersAttributeId.TempHp.ToDomainName());
        Assert.Equal("Temp HP", settlementHealth.DisplayName);
        Assert.Equal(100f, settlementHealth.Max);

        foreach (var (_, kindConfig) in config.Kinds)
        {
            foreach (var attribute in kindConfig.Attributes)
            {
                if (attribute.Category == AttributeCategory.Health)
                {
                    Assert.Equal(90f, attribute.Default);
                    Assert.Equal(1f / 9f, attribute.DefaultVariance, precision: 6);
                    Assert.Equal(100f, attribute.Max);
                    continue;
                }

                Assert.Equal(50f, attribute.Default);
                Assert.Equal(0.4f, attribute.DefaultVariance);
                Assert.Equal(100f, attribute.Max);
            }
        }
    }

    [Fact]
    public void IceRemembers_LifecycleRulesExpressTargetLifespanBands()
    {
        var config = IceRemembersDefinition.Create();

        AssertNearNominalDrain(config.Lifecycles[TestKinds.Penguin], targetTicks: 500f);
        AssertNearNominalDrain(config.Lifecycles[TestKinds.Faction], targetTicks: 800f);
        AssertNearNominalDrain(config.Lifecycles[TestKinds.Region], targetTicks: 800f);
        AssertNearNominalDrain(config.Lifecycles[TestKinds.Settlement], targetTicks: 1200f);
        AssertNearNominalDrain(config.Lifecycles[TestKinds.Artifact], targetTicks: 1200f);
    }

    [Fact]
    public void IceRemembers_EffectsCarryRelationshipDrivenHealthDeltas()
    {
        var config = IceRemembersDefinition.Create();
        var effects = config.Effects.AttributeDeltaEffects;

        Assert.DoesNotContain(effects, effect => effect.Kind == TestKinds.Region);
        foreach (var kind in new[] { TestKinds.Penguin, TestKinds.Faction, TestKinds.Settlement, TestKinds.Artifact })
        {
            var kindEffects = effects.Where(effect => effect.Kind == kind).ToList();
            Assert.Contains(kindEffects, effect =>
                effect.Condition == EffectCondition.RelatedInfluenceAtPhysicalLocation &&
                effect.DeltaPerTick > 0f);
            Assert.Contains(kindEffects, effect =>
                effect.Condition == EffectCondition.RelatedInfluenceAtPhysicalLocation &&
                effect.DeltaPerTick < 0f);
            Assert.Contains(kindEffects, effect =>
                effect.Condition == EffectCondition.RelatedEntityDistance &&
                effect.DeltaPerTick > 0f);
            Assert.Contains(kindEffects, effect =>
                effect.Condition == EffectCondition.RelatedEntityDistance &&
                effect.DeltaPerTick < 0f);
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

    private static void AssertNearNominalDrain(LifecycleConfig config, float targetTicks)
    {
        var totalDrain = (config.BaseRate * targetTicks) +
            (config.AgeCoefficient * targetTicks * (targetTicks - 1f) * 0.5f);

        Assert.InRange(totalDrain, 89f, 91f);
    }
}
