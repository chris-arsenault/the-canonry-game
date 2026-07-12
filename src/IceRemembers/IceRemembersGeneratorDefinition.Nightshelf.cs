namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

/// <summary>
/// The Nightshelf shadow economy: cross-cultural generators where the activity
/// belongs to the surface world (mostly Aurora) but the beneficiary is
/// Nightshelf. Rich hubs breed undermarkets, wars breed profiteers, public
/// records cast redacted shadows. The bigger the surface world grows, the more
/// underworld it hosts — balance that emerges from theme, not from a
/// scoreboard controller. The Deep Conclave is Nightshelf's authored timeline:
/// it convenes on its own cadence, outside the population-proportional pulse
/// pool.
/// </summary>
public static partial class IceRemembersGeneratorDefinition
{
    private const float RichHubMinimumTradeStrength = 0.60f;
    private const float ShadowBrokersMinimumWarStrength = 0.55f;
    private const float ShadowBrokersWarStrengthSpend = -0.06f;
    private const int DeepConclaveCadenceTicks = 1200;
    private const int DeepConclaveCadenceJitterTicks = 240;

    private static SemanticGeneratorConfig RichHubsGrowUndermarkets() => new()
    {
        Id = "rich_hubs_grow_undermarkets",
        Name = "Rich Hubs Grow Undermarkets",
        Description = "A settlement whose trade grows loud enough grows a quiet floor beneath it. An undermarket takes root under the hub, a fence keeps its ledger, and goods start moving that no tally acknowledges.",
        CausalLimit = "Depends on a strong outgoing trade route and no founding lock in either direction, so undermarkets cannot parent undermarkets; creates incoming founded-from as the repeat lock.",
        Target = TargetWithStrongOutgoingWithoutIncomingOrOutgoing(
            K.Settlement,
            Rel.TradeRoute,
            RichHubMinimumTradeStrength,
            withoutIncoming: Rel.FoundedFrom,
            withoutOutgoing: Rel.FoundedFrom),
        Produces =
        [
            RequiredNightshelf("$undermarket", K.Settlement, "holdfast", "$target"),
            RequiredNightshelf("$fence", K.Penguin, "broker", "$undermarket"),
            RequiredNightshelf("$ledger", K.Artifact, "seal", "$undermarket"),
        ],
        Relationships =
        [
            R("$undermarket", "$target", Rel.FoundedFrom, 0.85f),
            R("$undermarket", "$target", Rel.TradeRoute, IceRemembersRelationshipStrengths.InitialTradeRoute),
            R("$target", "$undermarket", Rel.TradeRoute, IceRemembersRelationshipStrengths.InitialTradeRoute),
            R("$fence", "$undermarket", Rel.HomeBase, 1f),
            R("$fence", "$ledger", Rel.Holds, 1f),
            R("$ledger", "$undermarket", Rel.Anchored, 0.85f),
        ],
        Triggers = TriggerOn(K.Settlement),
        PulseWeight = 8,
    };

    private static SemanticGeneratorConfig WarsFeedShadowBrokers() => new()
    {
        Id = "wars_feed_shadow_brokers",
        Name = "Wars Feed Shadow Brokers",
        Description = "Every war leaks. A profiteer arrives with contraband that remembers the fighting better than either side would like, and a little of the war's force drains into the dark exchange.",
        CausalLimit = "Depends on a strong outgoing at-war relationship; spends part of the selected war strength, which self-limits repeat firing.",
        Target = TargetWithStrongOutgoing(K.Faction, Rel.AtWarWith, ShadowBrokersMinimumWarStrength),
        Produces =
        [
            RequiredNightshelf("$profiteer", K.Penguin, "broker", "$target"),
            RequiredNightshelf("$contraband", K.Artifact, "instrument", "$target"),
        ],
        Relationships =
        [
            R("$profiteer", "$contraband", Rel.Holds, 1f),
            R("$contraband", "$target", Rel.Memorializes, 0.70f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.AtWarWith),
                ShadowBrokersWarStrengthSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship,
                includeReciprocal: true),
        ],
        Triggers = TriggerOn(K.Faction),
        PulseWeight = 6,
    };

    private static SemanticGeneratorConfig RecordsCastShadows() => new()
    {
        Id = "records_cast_shadows",
        Name = "Records Cast Shadows",
        Description = "Every public record implies the record it refuses to be. A held tome gains a redacted counterpart in Nightshelf custody, kept by an archivist who witnessed what the original left out.",
        CausalLimit = "Depends on a held tome without a memorializing counterpart; the counterfeit's memorializes edge is the repeat lock.",
        Target = TargetWithIncomingSubtypeAndWithoutIncoming(
            K.Artifact,
            "tome",
            Rel.Holds,
            Rel.Memorializes),
        Produces =
        [
            RequiredNightshelf("$counterfeit", K.Artifact, "tome", "$target"),
            RequiredNightshelf("$archivist", K.Penguin, "witness", "$target"),
        ],
        Relationships =
        [
            R("$archivist", "$counterfeit", Rel.Holds, 1f),
            R("$counterfeit", "$target", Rel.Memorializes, 0.80f),
        ],
        Triggers = TriggerOn(K.Artifact),
        PulseWeight = 5,
    };

    private static SemanticGeneratorConfig TheDeepConclave() => new()
    {
        Id = "the_deep_conclave",
        Name = "The Deep Conclave",
        Description = "On its own calendar, wherever secrets pool, the Conclave convenes. A keeper of things brings an initiate into the practice, and one more sealed secret enters Nightshelf custody.",
        CausalLimit = "Fires on an authored cadence against any custody-keeper; each convening adds one initiate and one sealed secret.",
        Target = TargetWithOutgoing(K.Penguin, Rel.Holds),
        Produces =
        [
            RequiredNightshelf("$initiate", K.Penguin, "broker", "$target"),
            RequiredNightshelf("$secret", K.Artifact, "seal", "$target"),
        ],
        Relationships =
        [
            R("$initiate", "$target", Rel.TaughtBy, 0.75f),
            R("$initiate", "$secret", Rel.Holds, 1f),
        ],
        Triggers = TriggerOn(K.Penguin),
        PulseWeight = 1,
        CadenceTicks = DeepConclaveCadenceTicks,
        CadenceJitterTicks = DeepConclaveCadenceJitterTicks,
    };

    /// <summary>
    /// A produced entity with authored Nightshelf culture and hostile-intrusion
    /// placement: shadow-economy outputs take root inside other cultures'
    /// territory, so the own-culture support gate must not apply.
    /// </summary>
    private static SemanticProducedEntity RequiredNightshelf(
        string @ref,
        EntityKind kind,
        string subtype,
        string placementRef)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(kind, subtype);
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(kind, subtype);
        return new()
        {
            Ref = @ref,
            Kind = kind,
            Subtype = subtype,
            Role = subtype,
            PhysicalPlacement = SemanticPlacement.Near,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            SemanticPlacement = SemanticPlacement.Near,
            SemanticPlacementPreferredTerrains = semanticProfile.PreferredTerrains,
            SemanticPlacementAvoidedTerrains = semanticProfile.AvoidedTerrains,
            SemanticPlacementFallbackTerrain = semanticProfile.FallbackTerrain,
            PlacementRef = placementRef,
            Culture = IceRemembersCulture.Nightshelf.ToDomainName(),
            CulturePlacement = GeneratorCulturePlacement.HostileIntrusion,
        };
    }
}
