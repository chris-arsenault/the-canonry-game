namespace Engine.Domain.Tests.Domain;

using Engine.Behaviors;
using Engine.Domain.Config;
using Engine.Domain.Loader;
using Engine.Gameplay;
using Engine.Kinds;

using IceRemembers;
using IceRemembers.Seed;

using Rel = IceRemembers.IceRemembersRelationship;

public class DomainConfigTests
{
    [Fact]
    public void IceRemembers_CreatesValidConfig()
    {
        var config = IceRemembersDefinition.Create();

        Assert.Equal(IceRemembersKinds.All.Count, config.Kinds.Count);
        Assert.True(config.Relationships.Count > 10);
        Assert.Equal(IceRemembersKinds.All.Count, config.Lifecycles.Count);
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
    public void IceRemembers_TeachingLineageUsesTaughtByRelationship()
    {
        var config = IceRemembersDefinition.Create();

        var taughtBy = Assert.Single(
            config.Relationships,
            relationship => relationship.Name == IceRemembersRelationship.TaughtBy.ToDomainName());
        Assert.Contains(EntityCategory.Agency, taughtBy.ValidSourceCategories);
        Assert.Contains(EntityCategory.Agency, taughtBy.ValidTargetCategories);
    }

    [Fact]
    public void IceRemembers_StartupAbilityLineageCreatesTaughtByEdges()
    {
        var seed = IceRemembersSeedBuilder.BuildDefault(seed: 42);

        Assert.Contains(
            seed.Relationships,
            relationship => relationship.RelationshipKind == IceRemembersRelationship.TaughtBy.ToDomainName());
    }

    [Fact]
    public void IceRemembers_SelectedGeneratorsUseCausalSpend()
    {
        var generators = IceRemembersDefinition.Create().Semantics!.Generators;
        var accounts = Assert.Single(generators, generator => generator.Id == "ice_settles_accounts");
        var spring = Assert.Single(generators, generator => generator.Id == "what_the_spring_remembers");
        var war = Assert.Single(generators, generator => generator.Id == "war_breaks_the_land");
        var routes = Assert.Single(generators, generator => generator.Id == "routes_draw_readers");
        var artifact = Assert.Single(generators, generator => generator.Id == "what_the_artifact_knows");

        var accountEffect = Assert.IsType<RemoveTargetRelationshipsEffect>(
            Assert.Single(accounts.Effects, effect => effect is RemoveTargetRelationshipsEffect));
        Assert.Equal(GeneratorRelationshipInstanceSelection.TriggerRelationship, accountEffect.InstanceSelection);
        Assert.True(accountEffect.IncludeReciprocal);

        Assert.Contains(IceRemembersRelationship.Anchored.ToDomainName(), spring.Target.WithoutIncomingRelationships!);

        var warTerrainRequirement = Assert.Single(war.Target.TerrainCountRequirements);
        Assert.Equal(IceRemembersTerrain.Wastes.ToDomainName(), warTerrainRequirement.TerrainName);
        Assert.Equal(5, warTerrainRequirement.Radius);
        Assert.Equal(8, warTerrainRequirement.MaximumExclusive);

        var routeRequirement = Assert.Single(routes.Target.RequiredOutgoingRelationshipRequirements);
        Assert.Equal(IceRemembersRelationship.TradeRoute.ToDomainName(), routeRequirement.RelationshipKind);
        Assert.True(routeRequirement.MinimumStrength > 0f);
        var routeEffect = Assert.IsType<AdjustTargetRelationshipStrengthEffect>(
            Assert.Single(routes.Effects, effect => effect is AdjustTargetRelationshipStrengthEffect));
        Assert.Equal(GeneratorRelationshipInstanceSelection.TriggerRelationship, routeEffect.InstanceSelection);
        Assert.True(routeEffect.IncludeReciprocal);
        Assert.True(routeEffect.Delta < 0f);

        Assert.Contains(IceRemembersRelationship.HomeBase.ToDomainName(), artifact.Target.WithoutOutgoingRelationships!);
    }

    [Fact]
    public void IceRemembers_RelationshipStrengthMeaningHasBalancedSourcesAndSinks()
    {
        var config = IceRemembersDefinition.Create();
        var meanings = RelationshipStrengthMeaningByName();
        var relationshipNames = config.Relationships
            .Select(relationship => relationship.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Empty(meanings.Keys.Except(relationshipNames, StringComparer.Ordinal));
        Assert.Empty(relationshipNames.Except(meanings.Keys, StringComparer.Ordinal));

        var flows = RelationshipStrengthFlowInventory.From(config);
        var issues = new List<string>();

        foreach (var relationship in config.Relationships.OrderBy(relationship => relationship.Name, StringComparer.Ordinal))
        {
            var meaningful = meanings[relationship.Name];
            var hasSource = flows.Sources.Contains(relationship.Name);
            var hasSink = flows.Sinks.Contains(relationship.Name);

            if (meaningful && (!hasSource || !hasSink))
            {
                issues.Add($"{relationship.Name} strength is meaningful but has source={hasSource}, sink={hasSink}.");
            }

            if (!meaningful && (hasSource || hasSink))
            {
                issues.Add($"{relationship.Name} strength is topology-only but has source={hasSource}, sink={hasSink}.");
            }
        }

        Assert.Empty(issues);
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

    private static Dictionary<string, bool> RelationshipStrengthMeaningByName() =>
        new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            [Rel.ControlledBy.ToDomainName()] = true,
            [Rel.HomeBase.ToDomainName()] = false,
            [Rel.AlliedWith.ToDomainName()] = true,
            [Rel.RivalOf.ToDomainName()] = true,
            [Rel.LedBy.ToDomainName()] = true,
            [Rel.MemberOf.ToDomainName()] = true,
            [Rel.Holds.ToDomainName()] = false,
            [Rel.CreatedBy.ToDomainName()] = false,
            [Rel.Memorializes.ToDomainName()] = true,
            [Rel.ClaimedBy.ToDomainName()] = true,
            [Rel.TradeRoute.ToDomainName()] = true,
            [Rel.FoundedFrom.ToDomainName()] = false,
            [Rel.NativeTo.ToDomainName()] = false,
            [Rel.Anchored.ToDomainName()] = true,
            [Rel.ConduitOf.ToDomainName()] = true,
            [Rel.PartOf.ToDomainName()] = false,
            [Rel.WardAgainst.ToDomainName()] = true,
            [Rel.CorruptedBy.ToDomainName()] = true,
            [Rel.CleansedBy.ToDomainName()] = true,
            [Rel.AtWarWith.ToDomainName()] = true,
            [Rel.BetrayedBy.ToDomainName()] = true,
            [Rel.PracticedBy.ToDomainName()] = true,
            [Rel.TaughtBy.ToDomainName()] = true,
        };

    private sealed record RelationshipStrengthFlowInventory(
        HashSet<string> Sources,
        HashSet<string> Sinks)
    {
        public static RelationshipStrengthFlowInventory From(DomainConfig config)
        {
            var sources = new HashSet<string>(StringComparer.Ordinal);
            var sinks = new HashSet<string>(StringComparer.Ordinal);

            AddMaintenanceFlows(config, sources, sinks);
            AddLowAltitudeFlows(config, sources, sinks);
            AddGeneratorFlows(config, sources, sinks);
            AddGambitFlows(config, sources, sinks);

            return new RelationshipStrengthFlowInventory(sources, sinks);
        }

        private static void AddMaintenanceFlows(
            DomainConfig config,
            HashSet<string> sources,
            HashSet<string> sinks)
        {
            foreach (var relationship in config.Relationships)
            {
                foreach (var rule in relationship.Maintenance)
                {
                    AddFlow(relationship.Name, rule.Delta, sources, sinks);
                }
            }
        }

        private static void AddLowAltitudeFlows(
            DomainConfig config,
            HashSet<string> sources,
            HashSet<string> sinks)
        {
            foreach (var verb in config.Gameplay?.LowAltitudeVerbs ?? [])
            {
                if (verb is RelationshipStrengthLowAltitudeVerbConfig &&
                    !string.IsNullOrWhiteSpace(verb.RelationshipKind))
                {
                    AddFlow(verb.RelationshipKind, verb.RelationshipStrengthDelta, sources, sinks);
                }
            }
        }

        private static void AddGeneratorFlows(
            DomainConfig config,
            HashSet<string> sources,
            HashSet<string> sinks)
        {
            foreach (var generator in config.Semantics?.Generators ?? [])
            {
                foreach (var effect in generator.Effects)
                {
                    if (effect is AdjustTargetRelationshipStrengthEffect adjust)
                    {
                        AddFlow(adjust.RelationshipKind, adjust.Delta, sources, sinks);
                    }
                }
            }
        }

        private static void AddGambitFlows(
            DomainConfig config,
            HashSet<string> sources,
            HashSet<string> sinks)
        {
            foreach (var gambit in config.Gambits)
            {
                foreach (var mutation in gambit.Mutations)
                {
                    if (mutation is SetContextRelationshipStrengthMutation set)
                    {
                        sources.Add(set.RelationshipKind);
                        sinks.Add(set.RelationshipKind);
                    }
                }
            }
        }

        private static void AddFlow(
            string relationship,
            float delta,
            HashSet<string> sources,
            HashSet<string> sinks)
        {
            if (delta > 0f)
            {
                sources.Add(relationship);
            }
            else if (delta < 0f)
            {
                sinks.Add(relationship);
            }
        }
    }
}
