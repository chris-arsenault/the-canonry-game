namespace Engine.Domain.Tests.Domain;

using Engine.Behaviors;
using Engine.Constants;
using Engine.Domain.Compilation;
using Engine.Domain.Config;
using Engine.Domain.Loader;
using Engine.Gameplay;
using Engine.Kinds;

using IceRemembers;
using IceRemembers.Seed;

using Rel = IceRemembers.IceRemembersRelationship;

// Slow: compiles the full domain configuration; excluded from the fast loop.
[Trait("Category", "Slow")]
public class DomainConfigTests
{
    [Fact]
    public void IceRemembers_CreatesValidConfig()
    {
        var config = IceRemembersDefinition.Create();

        Assert.Equal(IceRemembersKinds.All.Count, config.Kinds.Count);
        Assert.True(config.Relationships.Count > 10);
        // Region is the only WorldAnchor kind without passive lifecycle drain.
        Assert.Equal(IceRemembersKinds.All.Count - 1, config.Lifecycles.Count);
        Assert.Equal(2, config.TravelStructures.Count);
        Assert.Equal(IceRemembersGeneratorDefinition.Create().Count, config.Semantics!.Generators.Count);
    }

    [Fact]
    public void IceRemembers_EntityCatalogCoversCanonicalKinds()
    {
        var declaredKinds = IceRemembersEntities.All.Select(entity => entity.Kind).ToArray();

        Assert.Equal(IceRemembersKinds.All.Count, declaredKinds.Distinct().Count());
        Assert.Empty(IceRemembersKinds.All.Except(declaredKinds));
        Assert.Empty(declaredKinds.Except(IceRemembersKinds.All));
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
    public void IceRemembers_WardsCanBindMonsterScaleThreats()
    {
        var config = IceRemembersDefinition.Create();

        var wardAgainst = Assert.Single(
            config.Relationships,
            relationship => relationship.Name == IceRemembersRelationship.WardAgainst.ToDomainName());
        Assert.Contains(EntityCategory.Asset, wardAgainst.ValidSourceCategories);
        Assert.Contains(EntityCategory.Agency, wardAgainst.ValidTargetCategories);
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
        var war = Assert.Single(generators, generator => generator.Id == "war_breaks_the_land");
        var routes = Assert.Single(generators, generator => generator.Id == "routes_draw_readers");
        var artifact = Assert.Single(generators, generator => generator.Id == "what_the_artifact_knows");

        var accountEffect = Assert.IsType<RemoveTargetRelationshipsEffect>(
            Assert.Single(accounts.Effects, effect => effect is RemoveTargetRelationshipsEffect));
        Assert.Equal(GeneratorRelationshipInstanceSelection.TriggerRelationship, accountEffect.InstanceSelection);
        Assert.True(accountEffect.IncludeReciprocal);

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
        var producedRoute = Assert.Single(routes.Relationships, relationship =>
            relationship.Kind == IceRemembersRelationship.TradeRoute.ToDomainName());
        Assert.Equal(IceRemembersRelationshipStrengths.InitialTradeRoute, producedRoute.Strength);
        Assert.True(producedRoute.Strength > 0.05f);
        Assert.True(producedRoute.Strength < routeRequirement.MinimumStrength);

        Assert.Contains(IceRemembersRelationship.HomeBase.ToDomainName(), artifact.Target.WithoutOutgoingRelationships!);
    }

    [Fact]
    public void IceRemembers_SecondWaveGeneratorSliceUsesRelationshipLocks()
    {
        var generators = IceRemembersDefinition.Create().Semantics!.Generators;

        var places = Assert.Single(generators, generator => generator.Id == "unclaimed_places_draw_wardens");
        Assert.Equal(IceRemembersKinds.PointOfInterest, places.Target.Kind);
        Assert.Contains(Rel.ClaimedBy.ToDomainName(), places.Target.WithoutOutgoingRelationships!);
        Assert.Contains(places.Relationships, relationship =>
            relationship.SourceRef == "$target" &&
            relationship.DestinationRef == "$patron" &&
            relationship.Kind == Rel.ClaimedBy.ToDomainName());

        var forms = Assert.Single(generators, generator => generator.Id == "practiced_forms_find_tomes");
        Assert.Equal(IceRemembersKinds.Ability, forms.Target.Kind);
        Assert.Contains(Rel.PracticedBy.ToDomainName(), forms.Target.RequiredOutgoingRelationships!);
        Assert.Contains(Rel.ConduitOf.ToDomainName(), forms.Target.WithoutOutgoingRelationships!);
        Assert.Contains(forms.Relationships, relationship =>
            relationship.SourceRef == "$target" &&
            relationship.Kind == Rel.ConduitOf.ToDomainName());

        var charters = Assert.Single(generators, generator => generator.Id == "charters_find_harbors");
        Assert.Equal(IceRemembersKinds.Transport, charters.Target.Kind);
        Assert.Contains(Rel.Holds.ToDomainName(), charters.Target.RequiredIncomingRelationships!);
        Assert.Contains(Rel.NativeTo.ToDomainName(), charters.Target.WithoutOutgoingRelationships!);
        Assert.Contains(charters.Relationships, relationship =>
            relationship.SourceRef == "$target" &&
            relationship.Kind == Rel.NativeTo.ToDomainName());

        var vigils = Assert.Single(generators, generator => generator.Id == "vigils_leave_relics");
        Assert.Equal(IceRemembersKinds.Occurrence, vigils.Target.Kind);
        Assert.Contains(Rel.HomeBase.ToDomainName(), vigils.Target.RequiredOutgoingRelationships!);
        Assert.Contains(Rel.Memorializes.ToDomainName(), vigils.Target.WithoutIncomingRelationships!);
        Assert.Contains(vigils.Relationships, relationship =>
            relationship.DestinationRef == "$target" &&
            relationship.Kind == Rel.Memorializes.ToDomainName());
    }

    [Fact]
    public void IceRemembers_SecondLedgerGeneratorPassUsesCausalSpendAndLocks()
    {
        var generators = IceRemembersDefinition.Create().Semantics!.Generators;

        var corruption = Assert.Single(generators, generator => generator.Id == "corruption_wakes_monsters");
        Assert.Equal(IceRemembersKinds.Region, corruption.Target.Kind);
        Assert.Contains("scar", corruption.Target.Subtypes!);
        var corruptionRequirement = Assert.Single(corruption.Target.RequiredOutgoingRelationshipRequirements);
        Assert.Equal(Rel.CorruptedBy.ToDomainName(), corruptionRequirement.RelationshipKind);
        Assert.True(corruptionRequirement.MinimumStrength > 0f);
        Assert.Contains(Rel.CleansedBy.ToDomainName(), corruption.Target.WithoutIncomingRelationships!);
        var corruptionSpend = Assert.IsType<AdjustTargetRelationshipStrengthEffect>(
            Assert.Single(corruption.Effects, effect => effect is AdjustTargetRelationshipStrengthEffect));
        Assert.Equal(GeneratorRelationshipInstanceSelection.TriggerRelationship, corruptionSpend.InstanceSelection);
        Assert.True(corruptionSpend.Delta < 0f);
        Assert.Contains(corruption.Relationships, relationship =>
            relationship.SourceRef == "$target" &&
            relationship.DestinationRef == "$monster" &&
            relationship.Kind == Rel.CorruptedBy.ToDomainName());
        Assert.Contains(corruption.Relationships, relationship =>
            relationship.SourceRef == "$scout" &&
            relationship.DestinationRef == "$target" &&
            relationship.Kind == Rel.NativeTo.ToDomainName());

        var namedWar = Assert.Single(generators, generator => generator.Id == "wars_get_names");
        Assert.Equal(IceRemembersKinds.Faction, namedWar.Target.Kind);
        var warRequirement = Assert.Single(namedWar.Target.RequiredOutgoingRelationshipRequirements);
        Assert.Equal(Rel.AtWarWith.ToDomainName(), warRequirement.RelationshipKind);
        var warSpend = Assert.IsType<AdjustTargetRelationshipStrengthEffect>(
            Assert.Single(namedWar.Effects, effect => effect is AdjustTargetRelationshipStrengthEffect));
        Assert.True(warSpend.IncludeReciprocal);
        Assert.True(warSpend.Delta < 0f);
        Assert.Contains(namedWar.Produces, produce =>
            produce.Kind == IceRemembersKinds.Occurrence &&
            produce.Subtype == "war");
        // The named war is world-owned memory: it wages no war of its own,
        // so naming a war never mechanically begets more war edges.
        Assert.DoesNotContain(namedWar.Relationships, relationship =>
            relationship.SourceRef == "$war" &&
            relationship.Kind == Rel.AtWarWith.ToDomainName());
        Assert.Contains(namedWar.Produces, produce =>
            produce.CultureSource == ProducedCultureSource.World);

        var conference = Assert.Single(generators, generator => generator.Id == "trade_routes_convene_negotiation");
        Assert.Equal(IceRemembersKinds.Settlement, conference.Target.Kind);
        var tradeRequirement = Assert.Single(conference.Target.RequiredOutgoingRelationshipRequirements);
        Assert.Equal(Rel.TradeRoute.ToDomainName(), tradeRequirement.RelationshipKind);
        Assert.Contains(Rel.Memorializes.ToDomainName(), conference.Target.WithoutIncomingRelationships!);
        var tradeSpend = Assert.IsType<AdjustTargetRelationshipStrengthEffect>(
            Assert.Single(conference.Effects, effect => effect is AdjustTargetRelationshipStrengthEffect));
        Assert.True(tradeSpend.IncludeReciprocal);
        Assert.True(tradeSpend.Delta < 0f);
        Assert.Contains(conference.Produces, produce =>
            produce.Kind == IceRemembersKinds.Occurrence &&
            produce.Subtype == "trade_conference");
        Assert.Contains(conference.Relationships, relationship =>
            relationship.SourceRef == "$accord" &&
            relationship.DestinationRef == "$target" &&
            relationship.Kind == Rel.Memorializes.ToDomainName());
    }

    [Fact]
    public void IceRemembers_CorrectiveSecondWaveGeneratorPassProducesSecondWaveKinds()
    {
        var generators = IceRemembersDefinition.Create().Semantics!.Generators;
        var produced = generators
            .SelectMany(generator => generator.Produces.Select(produce => (generator.Id, produce.Kind, produce.Subtype)))
            .ToHashSet();

        Assert.Contains(produced, item => item.Id == "claimed_waters_draw_bulls" &&
            item.Kind == IceRemembersKinds.Orca &&
            item.Subtype == "bull");
        Assert.Contains(produced, item => item.Id == "conduits_map_hidden_peaks" &&
            item.Kind == IceRemembersKinds.PointOfInterest &&
            item.Subtype == "peak");
        Assert.Contains(produced, item => item.Id == "mapped_peaks_birth_techniques" &&
            item.Kind == IceRemembersKinds.Ability &&
            item.Subtype == "technique");
        Assert.Contains(produced, item => item.Id == "controlled_depths_wake_leviathans" &&
            item.Kind == IceRemembersKinds.Monster &&
            item.Subtype == "leviathan");
        Assert.Contains(produced, item => item.Id == "overclaimed_blooms_draw_beasts" &&
            item.Kind == IceRemembersKinds.Monster &&
            item.Subtype == "beast");
        Assert.Contains(produced, item => item.Id == "corrupted_monsters_raise_revenants" &&
            item.Kind == IceRemembersKinds.Monster &&
            item.Subtype == "revenant");
        Assert.Contains(produced, item => item.Id == "war_occurrences_launch_scouts" &&
            item.Kind == IceRemembersKinds.Transport &&
            item.Subtype == "scout");
        Assert.Contains(produced, item => item.Id == "trade_conferences_raise_citadels" &&
            item.Kind == IceRemembersKinds.Transport &&
            item.Subtype == "citadel");
        Assert.Contains(produced, item => item.Id == "controlled_depths_wake_leviathans" &&
            item.Kind == IceRemembersKinds.Occurrence &&
            item.Subtype == "plague");
        Assert.Contains(produced, item => item.Id == "conferences_become_festivals" &&
            item.Kind == IceRemembersKinds.Occurrence &&
            item.Subtype == "festival");

        var secondWaveKinds = new HashSet<EntityKind>
        {
            IceRemembersKinds.Orca,
            IceRemembersKinds.PointOfInterest,
            IceRemembersKinds.Ability,
            IceRemembersKinds.Monster,
            IceRemembersKinds.Transport,
            IceRemembersKinds.Occurrence,
        };
        var secondWaveProducedKinds = produced
            .Where(item => secondWaveKinds.Contains(item.Kind))
            .Select(item => item.Kind)
            .ToHashSet();

        Assert.Empty(secondWaveKinds.Except(secondWaveProducedKinds));
    }

    [Fact]
    public void IceRemembers_NextGeneratorPassProducesWeakSubtypeTargets()
    {
        var generators = IceRemembersDefinition.Create().Semantics!.Generators;
        var produced = generators
            .SelectMany(generator => generator.Produces.Select(produce => (generator.Id, produce.Kind, produce.Subtype)))
            .ToHashSet();

        Assert.Contains(produced, item => item.Id == "cleansed_springs_found_sanctuaries" &&
            item.Kind == IceRemembersKinds.Settlement &&
            item.Subtype == "sanctuary");
        Assert.Contains(produced, item => item.Id == "cleansed_springs_found_sanctuaries" &&
            item.Kind == IceRemembersKinds.Faction &&
            item.Subtype == "choir");
        Assert.Contains(produced, item => item.Id == "cleansed_springs_found_sanctuaries" &&
            item.Kind == IceRemembersKinds.Ability &&
            item.Subtype == "rite");
        Assert.Contains(produced, item => item.Id == "sanctuaries_anchor_colonies" &&
            item.Kind == IceRemembersKinds.Settlement &&
            item.Subtype == "colony");
        Assert.Contains(produced, item => item.Id == "sanctuaries_anchor_colonies" &&
            item.Kind == IceRemembersKinds.Transport &&
            item.Subtype == "trade");
        Assert.Contains(produced, item => item.Id == "conduit_points_found_reefholds" &&
            item.Kind == IceRemembersKinds.Settlement &&
            item.Subtype == "reefhold");
        Assert.Contains(produced, item => item.Id == "deep_conduits_name_depths" &&
            item.Kind == IceRemembersKinds.Region &&
            item.Subtype == "depths");
        Assert.Contains(produced, item => item.Id == "warded_monsters_found_orders" &&
            item.Kind == IceRemembersKinds.Faction &&
            item.Subtype == "order");
        Assert.Contains(produced, item => item.Id == "warded_monsters_found_orders" &&
            item.Kind == IceRemembersKinds.Transport &&
            item.Subtype == "battleship");
    }

    [Fact]
    public void IceRemembers_OrcaMonsterTransportLoopsGenerateTheirOwnActors()
    {
        var generators = IceRemembersDefinition.Create().Semantics!.Generators;
        var produced = generators
            .SelectMany(generator => generator.Produces.Select(produce => (generator.Id, produce.Kind, produce.Subtype)))
            .ToHashSet();

        Assert.Contains(produced, item => item.Id == "pod_currents_launch_scouts" &&
            item.Kind == IceRemembersKinds.Orca &&
            item.Subtype == "scout");
        Assert.Contains(produced, item => item.Id == "pod_currents_launch_scouts" &&
            item.Kind == IceRemembersKinds.Transport &&
            item.Subtype == "scout");
        Assert.Contains(produced, item => item.Id == "pod_currents_launch_scouts" &&
            item.Kind == IceRemembersKinds.PointOfInterest &&
            item.Subtype == "shoal");
        Assert.Contains(produced, item => item.Id == "orca_route_wars_get_names" &&
            item.Kind == IceRemembersKinds.Occurrence &&
            item.Subtype == "war");
        Assert.Contains(produced, item => item.Id == "orca_route_wars_get_names" &&
            item.Kind == IceRemembersKinds.Transport &&
            item.Subtype == "trade");
        Assert.Contains(produced, item => item.Id == "stolen_artifacts_draw_hunters" &&
            item.Kind == IceRemembersKinds.Penguin &&
            item.Subtype == "warden");
        Assert.Contains(produced, item => item.Id == "monster_slayings_become_festivals" &&
            item.Kind == IceRemembersKinds.Occurrence &&
            item.Subtype == "festival");
        Assert.Contains(produced, item => item.Id == "monster_slayings_become_festivals" &&
            item.Kind == IceRemembersKinds.Transport &&
            item.Subtype == "trade");
        Assert.Contains(produced, item => item.Id == "haunted_transports_spread_plague" &&
            item.Kind == IceRemembersKinds.Monster &&
            item.Subtype == "wraith");
        Assert.Contains(produced, item => item.Id == "haunted_transports_spread_plague" &&
            item.Kind == IceRemembersKinds.Occurrence &&
            item.Subtype == "plague");
    }

    [Fact]
    public void IceRemembers_NextGeneratorPassUsesCausalLocks()
    {
        var generators = IceRemembersDefinition.Create().Semantics!.Generators;

        var sanctuaries = Assert.Single(generators, generator => generator.Id == "cleansed_springs_found_sanctuaries");
        var cleansingRequirement = Assert.Single(sanctuaries.Target.RequiredIncomingRelationshipRequirements);
        Assert.Equal(Rel.CleansedBy.ToDomainName(), cleansingRequirement.RelationshipKind);
        Assert.Contains(Rel.FoundedFrom.ToDomainName(), sanctuaries.Target.WithoutIncomingRelationships!);
        Assert.Contains(sanctuaries.Relationships, relationship =>
            relationship.SourceRef == "$sanctuary" &&
            relationship.DestinationRef == "$target" &&
            relationship.Kind == Rel.FoundedFrom.ToDomainName());

        var reefholds = Assert.Single(generators, generator => generator.Id == "conduit_points_found_reefholds");
        Assert.Contains(Rel.ConduitOf.ToDomainName(), reefholds.Target.RequiredIncomingRelationships!);
        Assert.Contains(Rel.FoundedFrom.ToDomainName(), reefholds.Target.WithoutIncomingRelationships!);
        Assert.Contains(reefholds.Relationships, relationship =>
            relationship.SourceRef == "$reefhold" &&
            relationship.Kind == Rel.TradeRoute.ToDomainName());

        var depths = Assert.Single(generators, generator => generator.Id == "deep_conduits_name_depths");
        Assert.Contains(Rel.ConduitOf.ToDomainName(), depths.Target.RequiredIncomingRelationships!);
        Assert.Contains(Rel.FoundedFrom.ToDomainName(), depths.Target.RequiredIncomingRelationships!);
        Assert.Contains(Rel.FoundedFrom.ToDomainName(), depths.Target.WithoutOutgoingRelationships!);
        Assert.Contains(depths.Relationships, relationship =>
            relationship.SourceRef == "$target" &&
            relationship.DestinationRef == "$depths" &&
            relationship.Kind == Rel.FoundedFrom.ToDomainName());

        var orders = Assert.Single(generators, generator => generator.Id == "warded_monsters_found_orders");
        var wardRequirement = Assert.Single(orders.Target.RequiredIncomingRelationshipRequirements);
        Assert.Equal(Rel.WardAgainst.ToDomainName(), wardRequirement.RelationshipKind);
        Assert.Contains(Rel.Memorializes.ToDomainName(), orders.Target.WithoutIncomingRelationships!);
        Assert.Contains(orders.Relationships, relationship =>
            relationship.SourceRef == "$battleship" &&
            relationship.DestinationRef == "$target" &&
            relationship.Kind == Rel.Memorializes.ToDomainName());
    }

    [Fact]
    public void IceRemembers_PodLaunchesAreGenerationalSuccession()
    {
        var generators = IceRemembersDefinition.Create().Semantics!.Generators;

        // Each homed parent launches once - the successors' member-of edges
        // are the lock - and the desperation surge guards the extinction
        // edge. Unlimited launches per matriarch grew the pods into the
        // world's dominant war machine.
        var orcas = Assert.Single(generators, generator => generator.Id == "pod_currents_launch_scouts");
        Assert.Equal(IceRemembersKinds.Orca, orcas.Target.Kind);
        Assert.Contains(Rel.HomeBase.ToDomainName(), orcas.Target.RequiredOutgoingRelationships!);
        Assert.Contains(Rel.MemberOf.ToDomainName(), orcas.Target.WithoutIncomingRelationships!);
        Assert.Contains(orcas.Relationships, relationship =>
            relationship.SourceRef == "$scout" &&
            relationship.DestinationRef == "$target" &&
            relationship.Kind == Rel.MemberOf.ToDomainName());
        Assert.Contains(orcas.Relationships, relationship =>
            relationship.SourceRef == "$scout" &&
            relationship.DestinationRef == "$shoal" &&
            relationship.Kind == Rel.HomeBase.ToDomainName());
        Assert.Contains(orcas.Relationships, relationship =>
            relationship.SourceRef == "$scout" &&
            relationship.DestinationRef == "$skiff" &&
            relationship.Kind == Rel.Holds.ToDomainName());
        Assert.Contains(orcas.Relationships, relationship =>
            relationship.SourceRef == "$skiff" &&
            relationship.DestinationRef == "$shoal" &&
            relationship.Kind == Rel.ConduitOf.ToDomainName());
    }

    [Fact]
    public void IceRemembers_OrcaMonsterTransportLoopsUseCausalLocksAndSpend()
    {
        var generators = IceRemembersDefinition.Create().Semantics!.Generators;

        var routeWar = Assert.Single(generators, generator => generator.Id == "orca_route_wars_get_names");
        Assert.Equal(IceRemembersKinds.Orca, routeWar.Target.Kind);
        var warRequirement = Assert.Single(routeWar.Target.RequiredOutgoingRelationshipRequirements);
        Assert.Equal(Rel.AtWarWith.ToDomainName(), warRequirement.RelationshipKind);
        Assert.Contains(Rel.Memorializes.ToDomainName(), routeWar.Target.WithoutIncomingRelationships!);
        var warSpend = Assert.IsType<AdjustTargetRelationshipStrengthEffect>(
            Assert.Single(routeWar.Effects, effect => effect is AdjustTargetRelationshipStrengthEffect));
        Assert.Equal(Rel.AtWarWith.ToDomainName(), warSpend.RelationshipKind);
        Assert.True(warSpend.Delta < 0f);
        Assert.True(warSpend.IncludeReciprocal);
        Assert.Contains(routeWar.Relationships, relationship =>
            relationship.SourceRef == "$convoy" &&
            relationship.DestinationRef == "$target" &&
            relationship.Kind == Rel.Memorializes.ToDomainName());

        var hunters = Assert.Single(generators, generator => generator.Id == "stolen_artifacts_draw_hunters");
        Assert.Contains(Rel.Holds.ToDomainName(), hunters.Target.RequiredOutgoingRelationships!);
        Assert.Contains(Rel.AtWarWith.ToDomainName(), hunters.Target.WithoutIncomingRelationships!);
        Assert.Contains(hunters.Relationships, relationship =>
            relationship.SourceRef == "$hunter" &&
            relationship.DestinationRef == "$target" &&
            relationship.Kind == Rel.AtWarWith.ToDomainName());

        var festivals = Assert.Single(generators, generator => generator.Id == "monster_slayings_become_festivals");
        Assert.Contains(Rel.CleansedBy.ToDomainName(), festivals.Target.RequiredOutgoingRelationships!);
        Assert.Contains(Rel.Memorializes.ToDomainName(), festivals.Target.WithoutIncomingRelationships!);
        Assert.Contains(festivals.Relationships, relationship =>
            relationship.SourceRef == "$convoy" &&
            relationship.DestinationRef == "$rite" &&
            relationship.Kind == Rel.ConduitOf.ToDomainName());

        var haunted = Assert.Single(generators, generator => generator.Id == "haunted_transports_spread_plague");
        var corruptionRequirement = Assert.Single(haunted.Target.RequiredOutgoingRelationshipRequirements);
        Assert.Equal(Rel.CorruptedBy.ToDomainName(), corruptionRequirement.RelationshipKind);
        Assert.Contains(Rel.Memorializes.ToDomainName(), haunted.Target.WithoutOutgoingRelationships!);
        var spend = Assert.IsType<AdjustTargetRelationshipStrengthEffect>(
            Assert.Single(haunted.Effects, effect => effect is AdjustTargetRelationshipStrengthEffect));
        Assert.Equal(Rel.CorruptedBy.ToDomainName(), spend.RelationshipKind);
        Assert.True(spend.Delta < 0f);
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
    public void IceRemembers_RelationshipStrengthVerbsDeclareDeficitOrSurplusFitness()
    {
        var config = IceRemembersDefinition.Create();
        var issues = new List<string>();

        foreach (var verb in config.Gameplay?.LowAltitudeVerbs.OfType<RelationshipStrengthLowAltitudeVerbConfig>() ?? [])
        {
            if (verb.Delta > 0f && verb.FitnessPolarity != LowAltitudeVerbFitnessPolarity.Strengthen)
            {
                issues.Add($"{verb.Id} strengthens {verb.RelationshipKind} but does not use strengthen fitness.");
            }

            if (verb.Delta < 0f && verb.FitnessPolarity != LowAltitudeVerbFitnessPolarity.Weaken)
            {
                issues.Add($"{verb.Id} weakens {verb.RelationshipKind} but does not use weaken fitness.");
            }

            if (verb.FitnessThreshold <= 0f)
            {
                issues.Add($"{verb.Id} has no relationship fitness threshold.");
            }
        }

        Assert.Empty(issues);
    }

    [Fact]
    public void IceRemembers_EmitsNoSuppressedRelationshipStrengthAffordanceDiagnostics()
    {
        var compiled = DomainCompiler.Compile(IceRemembersDefinition.Create());
        var diagnostics = compiled.Diagnostics
            .Where(diagnostic => diagnostic.Code == DomainCompileDiagnosticCodes.SuppressedRelationshipStrengthAffordance)
            .Select(diagnostic => diagnostic.Message)
            .ToArray();

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void DomainCompiler_EmitsDiagnosticWhenRelationshipStrengthAffordanceHasNoMatchingVerb()
    {
        var config = WithArtifactGeneratorRequiringStrongHoldsRelationship(IceRemembersDefinition.Create());
        var compiled = DomainCompiler.Compile(config);

        var diagnostic = Assert.Single(
            compiled.Diagnostics,
            diagnostic => diagnostic.Code == DomainCompileDiagnosticCodes.SuppressedRelationshipStrengthAffordance);
        Assert.Equal("generator", diagnostic.SourceKind);
        Assert.Equal("what_the_artifact_knows", diagnostic.Source);
        Assert.Equal(IceRemembersKinds.Penguin.Value, diagnostic.ActorKind);
        Assert.Equal(Rel.Holds.ToDomainName(), diagnostic.RelationshipKind);
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

        AssertLifecycleTarget(config.Lifecycles[TestKinds.Penguin], targetTicks: 2000);
        AssertLifecycleTarget(config.Lifecycles[TestKinds.Faction], targetTicks: 10000);
        AssertLifecycleTarget(config.Lifecycles[TestKinds.Settlement], targetTicks: 3500);
        // Orcas are Enduring: the Wake-Singers' identity rests on long-lived
        // matriarchs carrying the song.
        AssertLifecycleTarget(config.Lifecycles[IceRemembersKinds.Orca], targetTicks: 4500);
        AssertLifecycleTarget(config.Lifecycles[IceRemembersKinds.Ability], targetTicks: 10000);
        AssertLifecycleTarget(config.Lifecycles[IceRemembersKinds.Monster], targetTicks: 4500);
        AssertLifecycleTarget(config.Lifecycles[TestKinds.Artifact], targetTicks: 4500);
        AssertLifecycleTarget(config.Lifecycles[IceRemembersKinds.PointOfInterest], targetTicks: 20000);
    }

    [Fact]
    public void IceRemembers_OnlyWorldAnchorKindsAreAgeless()
    {
        var config = IceRemembersDefinition.Create();

        Assert.DoesNotContain(TestKinds.Region, config.Lifecycles.Keys);
        Assert.Contains(TestKinds.Artifact, config.Lifecycles.Keys);
        Assert.Contains(IceRemembersKinds.PointOfInterest, config.Lifecycles.Keys);
    }

    [Fact]
    public void DomainCompiler_PreservesLifecycleTargetsAndHealthBudgets()
    {
        var config = IceRemembersDefinition.Create();
        var compiled = DomainCompiler.Compile(config);

        foreach (var (kind, lifecycle) in config.Lifecycles)
        {
            var kindId = compiled.Ids.KindIdOf(kind).Value;
            var health = config.Kinds[kind].Health;
            var rule = Assert.Single(
                compiled.Programs.Lifecycle.Rules,
                candidate => candidate.KindId == kindId);

            Assert.Equal(lifecycle.TargetTicks, rule.TargetTicks);
            Assert.Equal(
                KernelFixedPoint.AttributeUnits(health.Default - health.Min),
                rule.NominalHealthUnits);
            Assert.Equal(FixedScalar.FromSingle(lifecycle.FlatDrainShare), rule.FlatDrainShare);
        }
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

    private static void AssertLifecycleTarget(LifecycleConfig config, int targetTicks)
    {
        Assert.Equal(targetTicks, config.TargetTicks);
        Assert.Equal(0.2f, config.FlatDrainShare);
    }

    private static DomainConfig WithArtifactGeneratorRequiringStrongHoldsRelationship(DomainConfig config)
    {
        var semantics = config.Semantics!;
        var generators = semantics.Generators
            .Select(generator => generator.Id == "what_the_artifact_knows"
                ? CopyGeneratorWithTarget(
                    generator,
                    new SemanticGeneratorTarget
                    {
                        Kind = generator.Target.Kind,
                        RequiredOutgoingRelationshipRequirements =
                        [
                            new SemanticGeneratorRelationshipRequirement
                            {
                                RelationshipKind = Rel.Holds.ToDomainName(),
                                MinimumStrength = 0.7f,
                            },
                        ],
                        WithoutOutgoingRelationships = generator.Target.WithoutOutgoingRelationships,
                    })
                : generator)
            .ToArray();

        return new DomainConfig
        {
            Kinds = config.Kinds,
            Gambits = config.Gambits,
            Relationships = config.Relationships,
            Lifecycles = config.Lifecycles,
            Terrains = config.Terrains,
            TravelStructures = config.TravelStructures,
            MovementProfiles = config.MovementProfiles,
            Cultures = config.Cultures,
            WorldCulture = config.WorldCulture,
            CulturePower = config.CulturePower,
            Tags = config.Tags,
            Gameplay = config.Gameplay,
            Semantics = new SemanticDomainConfig
            {
                Stocks = semantics.Stocks,
                Verbs = semantics.Verbs,
                Flows = semantics.Flows,
                Modifiers = semantics.Modifiers,
                Generators = generators,
            },
            Chronicle = config.Chronicle,
        };
    }

    private static SemanticGeneratorConfig CopyGeneratorWithTarget(
        SemanticGeneratorConfig generator,
        SemanticGeneratorTarget target) =>
        new()
        {
            Id = generator.Id,
            Name = generator.Name,
            Description = generator.Description,
            CausalLimit = generator.CausalLimit,
            Target = target,
            SecondaryTargets = generator.SecondaryTargets,
            Produces = generator.Produces,
            Relationships = generator.Relationships,
            Effects = generator.Effects,
            Triggers = generator.Triggers,
            FireWeight = generator.FireWeight,
        };

    private static Dictionary<string, bool> RelationshipStrengthMeaningByName() =>
        new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            [Rel.ControlledBy.ToDomainName()] = true,
            [Rel.HomeBase.ToDomainName()] = false,
            [Rel.WagesCampaignAgainst.ToDomainName()] = true,
            [Rel.Carries.ToDomainName()] = false,
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
            [Rel.OfferedTo.ToDomainName()] = true,
            [Rel.ConsignedTo.ToDomainName()] = true,
            [Rel.PreysOn.ToDomainName()] = true,
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
