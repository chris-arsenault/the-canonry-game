namespace Engine.Domain.Tests.Domain;

using Engine.Domain.Config;
using Engine.Domain.Loader;
using Engine.Gameplay;
using IceRemembers;
using IceRemembers.Seed;
using Engine.Kinds;

public class DomainConfigTests
{
    [Fact]
    public void IceRemembers_CreatesValidConfig()
    {
        var config = IceRemembersDefinition.Create();

        Assert.Equal(IceRemembersKinds.All.Count, config.Kinds.Count);
        Assert.True(config.Relationships.Count > 10);
        Assert.Equal(IceRemembersKinds.All.Count, config.Lifecycles.Count);
        Assert.Equal(3, config.Resources.Count);
        Assert.Equal(3, config.Cultures.Count);
        Assert.Equal(IceRemembersGeneratorDefinition.Create().Count, config.Semantics!.Generators.Count);
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
    public void IceRemembers_TeachingLineageUsesRelationshipNotRelationshipFact()
    {
        var config = IceRemembersDefinition.Create();

        Assert.Empty(config.RelationshipFacts);
        var taughtBy = Assert.Single(
            config.Relationships,
            relationship => relationship.Name == IceRemembersRelationship.TaughtBy.ToDomainName());
        Assert.Contains(EntityCategory.Agency, taughtBy.ValidSourceCategories);
        Assert.Contains(EntityCategory.Agency, taughtBy.ValidTargetCategories);
    }

    [Fact]
    public void IceRemembers_StartupAbilityLineageCreatesTaughtByEdgesWithoutFacts()
    {
        var seed = IceRemembersSeedBuilder.BuildDefault(seed: 42);

        Assert.DoesNotContain(seed.Relationships, relationship => relationship.Facts is { Count: > 0 });
        Assert.Contains(
            seed.Relationships,
            relationship => relationship.RelationshipKind == IceRemembersRelationship.TaughtBy.ToDomainName());
        Assert.All(
            seed.Relationships.Where(relationship =>
                relationship.RelationshipKind == IceRemembersRelationship.PracticedBy.ToDomainName()),
            relationship => Assert.True(relationship.Facts is null or { Count: 0 }));
    }

    [Fact]
    public void IceRemembers_SelectedGeneratorsUseCausalSpendInsteadOfSaturation()
    {
        var generators = IceRemembersDefinition.Create().Semantics!.Generators;
        var accounts = Assert.Single(generators, generator => generator.Id == "ice_settles_accounts");
        var spring = Assert.Single(generators, generator => generator.Id == "what_the_spring_remembers");
        var routes = Assert.Single(generators, generator => generator.Id == "routes_draw_readers");
        var artifact = Assert.Single(generators, generator => generator.Id == "what_the_artifact_knows");

        Assert.Null(accounts.TargetSaturation);
        var accountEffect = Assert.IsType<RemoveTargetRelationshipsEffect>(
            Assert.Single(accounts.Effects, effect => effect is RemoveTargetRelationshipsEffect));
        Assert.Equal(GeneratorRelationshipInstanceSelection.TriggerRelationship, accountEffect.InstanceSelection);
        Assert.True(accountEffect.IncludeReciprocal);

        Assert.Null(spring.TargetSaturation);
        Assert.Contains(IceRemembersRelationship.Anchored.ToDomainName(), spring.Target.WithoutIncomingRelationships!);

        Assert.Null(routes.TargetSaturation);
        var routeRequirement = Assert.Single(routes.Target.RequiredOutgoingRelationshipRequirements);
        Assert.Equal(IceRemembersRelationship.TradeRoute.ToDomainName(), routeRequirement.RelationshipKind);
        Assert.True(routeRequirement.MinimumStrength > 0f);
        var routeEffect = Assert.IsType<AdjustTargetRelationshipStrengthEffect>(
            Assert.Single(routes.Effects, effect => effect is AdjustTargetRelationshipStrengthEffect));
        Assert.Equal(GeneratorRelationshipInstanceSelection.TriggerRelationship, routeEffect.InstanceSelection);
        Assert.True(routeEffect.IncludeReciprocal);
        Assert.True(routeEffect.Delta < 0f);

        Assert.Null(artifact.TargetSaturation);
        Assert.Contains(IceRemembersRelationship.HomeBase.ToDomainName(), artifact.Target.WithoutOutgoingRelationships!);
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
    public void IceRemembers_KindsHaveHealthOnlyAttributes()
    {
        var config = IceRemembersDefinition.Create();

        foreach (var (_, kindConfig) in config.Kinds)
        {
            Assert.False(string.IsNullOrWhiteSpace(kindConfig.Health.Name));
        }
    }

    [Fact]
    public void IceRemembers_KindsHavePlaceholderTraits()
    {
        var config = IceRemembersDefinition.Create();

        foreach (var (_, kindConfig) in config.Kinds)
        {
            Assert.Contains(kindConfig.Traits, trait => trait.Polarity == TraitPolarity.Positive);
            Assert.Contains(kindConfig.Traits, trait => trait.Polarity == TraitPolarity.Negative);
        }
    }

    [Fact]
    public void IceRemembers_AllKindsHaveHealthAttribute()
    {
        var config = IceRemembersDefinition.Create();

        foreach (var (_, kindConfig) in config.Kinds)
        {
            Assert.NotNull(kindConfig.Health);
        }
    }

    [Fact]
    public void IceRemembers_InitialAttributesUseStandardizedBands()
    {
        var config = IceRemembersDefinition.Create();
        var settlement = config.Kinds[TestKinds.Settlement];

        Assert.Equal(IceRemembersAttributeId.TempHp.ToDomainName(), settlement.Health.Name);
        var settlementHealth = settlement.Health;
        Assert.Equal("Temp HP", settlementHealth.DisplayName);
        Assert.Equal(100f, settlementHealth.Max);

        foreach (var (_, kindConfig) in config.Kinds)
        {
            var attribute = kindConfig.Health;
            Assert.Equal(90f, attribute.Default);
            Assert.Equal(1f / 9f, attribute.DefaultVariance, precision: 6);
            Assert.Equal(100f, attribute.Max);
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
        AssertNearNominalDrain(config.Lifecycles[IceRemembersKinds.Orca], targetTicks: 500f);
        AssertNearNominalDrain(config.Lifecycles[IceRemembersKinds.PointOfInterest], targetTicks: 1200f);
        AssertNearNominalDrain(config.Lifecycles[IceRemembersKinds.Ability], targetTicks: 800f);
        AssertNearNominalDrain(config.Lifecycles[IceRemembersKinds.Monster], targetTicks: 1200f);
    }

    [Fact]
    public void IceRemembers_EffectsCarryRelationshipDrivenHealthDeltas()
    {
        var config = IceRemembersDefinition.Create();
        var effects = config.Gameplay?.Effects.AttributeDeltaEffects ?? [];

        foreach (var kind in IceRemembersKinds.All)
        {
            var kindEffects = effects.Where(effect => effect.Kind == kind.Value).ToList();
            Assert.Contains(kindEffects, effect =>
                effect.Condition == EffectCondition.RelatedInfluenceAtLocation &&
                effect.DeltaPerTick > 0f);
            Assert.Contains(kindEffects, effect =>
                effect.Condition == EffectCondition.RelatedInfluenceAtLocation &&
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

    private static void AssertNearNominalDrain(LifecycleConfig config, float targetTicks)
    {
        var totalDrain = (config.BaseRate * targetTicks) +
            (config.AgeCoefficient * targetTicks * (targetTicks - 1f) * 0.5f);

        Assert.InRange(totalDrain, 89f, 91f);
    }
}
