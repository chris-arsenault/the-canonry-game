namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Domain.Spatial;
using Engine.Kinds;
using Engine.WorldGen;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

// The Great Thaw's living echo: "Bold explorers from both colonies claim
// territory and found settlements... Every marginal rookery dreams of
// becoming a great berg." Frontier colonies gather the people, stones, and
// crafts of new ground, and the far waters draw the deed-hungry.
public static partial class IceRemembersGeneratorDefinition
{
    private static IReadOnlyList<SemanticGeneratorConfig> CreateFrontierGenerators() =>
    [
        TheThawCallsTheBold(),
        BullsRangeTheFarCurrents(),
        TheWhiteHasTeeth(),
        TheRiverEarnsItsName(),
        TheStillWaterIsSounded(),
        TheStoneRemembersTheWalk(),
    ];

    private static SemanticGeneratorConfig TheStoneRemembersTheWalk() => new()
    {
        Id = "the_stone_remembers_the_walk",
        Name = "The Stone Remembers the Walk",
        Description = "A walker offered a far walk to the ice, and the ice answers: a remembrance-stone rises where they stand, holding the deed where the frost can keep it. Around such stones the far trails stay warm a little longer.",
        CausalLimit = "Depends on a walker who committed a waymark deed and has no stone yet; the stone's memorializes edge is the repeat lock.",
        Target = new SemanticGeneratorTarget
        {
            Kind = K.Penguin,
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.Memorializes) },
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(Rel.Memorializes) },
        },
        Produces =
        [
            // Beside the walker, not beneath them: the stone rises on the
            // nearest free ground to where the deed was offered.
            Required("$waystone", K.Artifact, "relic", "$target", "$target"),
        ],
        Relationships =
        [
            R("$waystone", "$target", Rel.Memorializes, 0.85f),
        ],
        Triggers = TriggerOn(K.Penguin),
        PulseWeight = 4,
    };

    // Discovery: the retreating sheets reveal "new waters rich with krill
    // and ancient secrets frozen beneath." When the peoples reach a named
    // piece of the world, the ice finally has someone to remember it for.
    private static SemanticGeneratorConfig TheRiverEarnsItsName() => new()
    {
        Id = "the_river_earns_its_name",
        Name = "The River Earns Its Name",
        Description = "A settlement's people walk the length of the water that feeds them, and the whole running system takes a name the ice can hold. What was a line on Piien's unfinished charts becomes a place the living claim.",
        CausalLimit = "Depends on a revealed, unclaimed river-system anchor; the named region standing on the anchor is the claim.",
        Target = new SemanticGeneratorTarget { Kind = K.Settlement },
        Produces =
        [
            FeatureDiscovery("$namedwater", K.Region, "reach", "river_system"),
        ],
        Relationships =
        [
            R("$namedwater", "$target", Rel.ClaimedBy, 0.5f),
        ],
        Triggers = TriggerOn(K.Settlement),
        PulseWeight = 2,
    };

    private static SemanticGeneratorConfig TheStillWaterIsSounded() => new()
    {
        Id = "the_still_water_is_sounded",
        Name = "The Still Water Is Sounded",
        Description = "A crew takes its craft onto an unmeasured lake and sounds the bottom. The still water joins the route ledger with a depth beside its name, and the charts grow by one more truth.",
        CausalLimit = "Depends on a revealed, unclaimed lake anchor; the sounding site standing on the anchor is the claim.",
        Target = new SemanticGeneratorTarget { Kind = K.Transport },
        Produces =
        [
            FeatureDiscovery("$sounding", K.PointOfInterest, "shoal", "lake"),
        ],
        Relationships =
        [
            R("$sounding", "$target", Rel.ConduitOf, 0.6f),
        ],
        Triggers = TriggerOn(K.Transport),
        PulseWeight = 2,
    };

    // A discovery lands on the revealed anchor hex of a geological feature
    // and inherits the discoverer's culture - naming is a deed, and deeds
    // belong to someone.
    private static SemanticProducedEntity FeatureDiscovery(
        string @ref,
        EntityKind kind,
        string subtype,
        string featureKind)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(kind, subtype);
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(kind, subtype);
        return new()
        {
            Ref = @ref,
            Kind = kind,
            Subtype = subtype,
            Role = subtype,
            PhysicalPlacement = SemanticPlacement.FeatureAnchor,
            FeatureKind = featureKind,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            SemanticPlacement = SemanticPlacement.Near,
            SemanticPlacementPreferredTerrains = semanticProfile.PreferredTerrains,
            SemanticPlacementAvoidedTerrains = semanticProfile.AvoidedTerrains,
            SemanticPlacementFallbackTerrain = semanticProfile.FallbackTerrain,
            PlacementRef = "$target",
            InheritCultureFrom = "$target",
            CulturePlacement = GeneratorCulturePlacement.Ignore,
            Coverage = kind == K.Region
                ? new SemanticRegionCoverage
                {
                    Mode = CoveragePlacementMode.CreateNew,
                    DesiredCellCount = 18,
                    MinimumCellCount = 6,
                }
                : null,
        };
    }

    private static SemanticGeneratorConfig TheThawCallsTheBold() => new()
    {
        Id = "the_thaw_calls_the_bold",
        Name = "The Thaw Calls the Bold",
        Description = "A young colony on new ground draws those who want the ice to see them. A witness walks the unread white so the deed is recorded, a remembrance-stone is raised so the founding cannot be forgotten, and the craft of surviving the far cold begins to be taught.",
        CausalLimit = "Depends on a founded colony that no remembrance-stone yet marks; the memorializes edge is the repeat lock.",
        Target = TargetSubtypeWithOutgoingAndWithoutIncoming(K.Settlement, "colony", Rel.FoundedFrom, Rel.Memorializes),
        Produces =
        [
            Required("$wayfinder", K.Penguin, "witness", "$target", "$target"),
            Optional("$waystone", K.Artifact, "relic", "$target", "$target", chance: 0.6f),
            Optional("$coldcraft", K.Ability, "craft", "$target", "$target", chance: 0.45f),
            Optional("$charter", K.Canon, "accord", "$target", "$target", chance: 0.4f),
        ],
        Relationships =
        [
            R("$wayfinder", "$target", Rel.MemberOf, 0.85f),
            R("$wayfinder", "$target", Rel.HomeBase, 1f),
            R("$waystone", "$target", Rel.Memorializes, 0.9f),
            R("$waystone", "$target", Rel.Anchored, 1f),
            R("$coldcraft", "$wayfinder", Rel.PracticedBy, 0.8f),
            R("$coldcraft", "$target", Rel.Anchored, 0.85f),
            R("$charter", "$target", Rel.Anchored, 0.9f),
        ],
        Triggers = TriggerOn(K.Settlement),
        PulseWeight = 6,
    };

    private static SemanticGeneratorConfig BullsRangeTheFarCurrents() => new()
    {
        Id = "bulls_range_the_far_currents",
        Name = "Bulls Range the Far Currents",
        Description = "A pod rich in devotion sends a bull past the edge of the claimed water, where the currents have no bones in them yet and a deed-name has room to grow. Stories of the distant patrols come back to the ice-edge in hushed trades.",
        CausalLimit = "Depends on a faction with devotion flow; the new bull's member_of edge carries the pod's reach outward.",
        Target = TargetWithOutgoing(K.Faction, Rel.OfferedTo),
        Produces =
        [
            RequiredWakeSingers(
                "$farbull",
                K.Orca,
                "bull",
                "$target",
                GeneratorCulturePlacement.CultureFrontier,
                physicalFrontier: true),
        ],
        Relationships =
        [
            R("$farbull", "$target", Rel.MemberOf, 0.8f),
            R("$farbull", "$target", Rel.HomeBase, 0.7f),
        ],
        Triggers = TriggerOn(K.Faction),
        PulseWeight = 3,
    };

    private static SemanticGeneratorConfig TheWhiteHasTeeth() => new()
    {
        Id = "the_white_has_teeth",
        Name = "The White Has Teeth",
        Description = "New ground is not empty ground. Something that was already living in the far white begins working the young colony's outskirts - the marches around every founding have hungry things in them, and they do not care whose shore it is.",
        CausalLimit = "Depends on a colony no monster yet preys on; the preys_on edge is the repeat lock.",
        Target = TargetSubtypeWithoutIncoming(K.Settlement, "colony", Rel.PreysOn),
        Produces =
        [
            FrontierWilderness("$lurker", K.Monster, "wraith"),
        ],
        Relationships =
        [
            R("$lurker", "$target", Rel.PreysOn, 0.5f),
        ],
        Effects =
        [
            new SetBindingTagEffect("$target", "hunted"),
        ],
        Triggers = TriggerOn(K.Settlement),
        PulseWeight = 3,
    };

    // A wilderness thing of the far ring: placed well away from its target
    // on ground no culture holds, wearing whatever the land there is. The
    // frontier's dangers are of the frontier, not spawned at the gate.
    private static SemanticProducedEntity FrontierWilderness(
        string @ref,
        EntityKind kind,
        string subtype)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(kind, subtype);
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(kind, subtype);
        return new()
        {
            Ref = @ref,
            Kind = kind,
            Subtype = subtype,
            Role = subtype,
            PhysicalPlacement = SemanticPlacement.Across,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            PhysicalPlacementPreferredSites = physicalProfile.PreferredSites,
            SemanticPlacement = SemanticPlacement.Near,
            SemanticPlacementPreferredTerrains = semanticProfile.PreferredTerrains,
            SemanticPlacementAvoidedTerrains = semanticProfile.AvoidedTerrains,
            SemanticPlacementFallbackTerrain = semanticProfile.FallbackTerrain,
            PlacementRef = "$target",
            // The far white's hungry things are the Long Dark's own.
            Culture = IceRemembersCulture.TheIce.ToDomainName(),
            CulturePlacement = GeneratorCulturePlacement.Wilderness,
        };
    }
}
