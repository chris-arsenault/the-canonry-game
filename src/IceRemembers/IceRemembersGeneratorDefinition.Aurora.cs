namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

// Aurora's side of the parasitic web (docs/design/culture-balance-options.md):
// the open Stack grows from the other cultures' victims. Nightshelf's
// debt-traps leak escapees into the public court; Wake-Singer predation
// drives the wrecked under the beacon's protection. This closes the
// cross-cultural cycle - every culture has a themed feed off the others'
// excesses, so whoever runs away breeds their own counterweight.
public static partial class IceRemembersGeneratorDefinition
{
    // The corruption threshold sits against the measured edge distribution
    // (round-11/12 runs): transport-outgoing corrupted-by p50 is 0.72, so
    // 0.75 selects the deeply gripped. The watch answers standing terror
    // tags rather than predation edges - convoy predation proved to occur
    // roughly once per world while the dread song terrorizes settlements
    // ~19 times per world, and a tag stands until a responder clears it.
    private const float DebtorsFleeMinimumCorruptionStrength = 0.75f;
    private const float DebtorsFleeCorruptionSpend = -0.30f;

    private static SemanticGeneratorConfig DebtorsFleeToTheOpenCourt() => new()
    {
        Id = "debtors_flee_to_the_open_court",
        Name = "Debtors Flee to the Open Court",
        Description = "A hull indentured deep enough breaks its charter and sails for the open ice. The Stack's court hears the crew's debt, annuls it in public writ, and the freed stay to testify under the light.",
        CausalLimit = "Depends on a transport's strong outgoing corrupted-by grip and no memorializing writ; spends the corruption it frees and the writ is the repeat lock.",
        Target = TargetWithStrongOutgoingAndWithoutIncoming(
            K.Transport,
            Rel.CorruptedBy,
            DebtorsFleeMinimumCorruptionStrength,
            Rel.Memorializes),
        Produces =
        [
            RequiredAurora("$witness", K.Penguin, "witness", "$target"),
            RequiredAurora("$writ", K.Artifact, "tome", "$target"),
            // The feed must compound the way the loop it counters does: a
            // witness and a writ are inert, but a freeport founded by freed
            // crews emits culture, hosts events, and climbs the ladder.
            OptionalAurora("$freeport", K.Settlement, "holdfast", "$target", chance: 0.35f),
        ],
        Relationships =
        [
            R("$witness", "$writ", Rel.Holds, 1f),
            R("$writ", "$target", Rel.Memorializes, 0.80f),
            R("$witness", "$target", Rel.AlliedWith, 0.70f),
            R("$witness", "$freeport", Rel.HomeBase, 0.85f),
            R("$freeport", "$target", Rel.TradeRoute, IceRemembersRelationshipStrengths.InitialTradeRoute),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.CorruptedBy),
                DebtorsFleeCorruptionSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship),
        ],
        Triggers = TriggerOn(K.Transport),
        FireWeight = 6,
    };

    private static SemanticGeneratorConfig TheWatchAnswersTheSong() => new()
    {
        Id = "the_watch_answers_the_song",
        Name = "The Watch Answers the Song",
        Description = "A settlement the dread song has broken calls, and sometimes the light answers before the fear settles into worship. A warden of the Stack raises the watch, an escort hull holds the harbor, and the terror becomes a garrison instead of a hymn.",
        CausalLimit = "Depends on a standing terrified mark and no memorializing watch; clears the terror it answers and the watch's memorial is the repeat lock.",
        Target = TargetTaggedWithoutIncoming(K.Settlement, "terrified", Rel.Memorializes),
        Produces =
        [
            RequiredAurora("$warden", K.Penguin, "warden", "$target"),
            RequiredAurora("$escort", K.Transport, "battleship", "$target"),
        ],
        Relationships =
        [
            R("$warden", "$escort", Rel.Holds, 1f),
            R("$warden", "$target", Rel.WardAgainst, 0.72f),
            R("$warden", "$target", Rel.HomeBase, 0.80f),
            R("$escort", "$target", Rel.Memorializes, 0.80f),
        ],
        Effects =
        [
            new ClearBindingTagEffect("$target", "terrified"),
        ],
        Triggers = TriggerOn(K.Settlement),
        FireWeight = 6,
    };

    private static SemanticProducedEntity OptionalAurora(
        string @ref,
        EntityKind kind,
        string subtype,
        string placementRef,
        float chance) =>
        AuroraProduced(@ref, kind, subtype, placementRef, chance);

    private static SemanticProducedEntity RequiredAurora(
        string @ref,
        EntityKind kind,
        string subtype,
        string placementRef,
        GeneratorCulturePlacement culturePlacement = GeneratorCulturePlacement.HostileIntrusion,
        bool physicalFrontier = false) =>
        AuroraProduced(@ref, kind, subtype, placementRef, chance: 1f, culturePlacement, physicalFrontier);

    private static SemanticProducedEntity AuroraProduced(
        string @ref,
        EntityKind kind,
        string subtype,
        string placementRef,
        float chance,
        GeneratorCulturePlacement culturePlacement = GeneratorCulturePlacement.HostileIntrusion,
        bool physicalFrontier = false)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(kind, subtype);
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(kind, subtype);
        return new()
        {
            CreateChance = chance,
            Ref = @ref,
            Kind = kind,
            Subtype = subtype,
            Role = subtype,
            PhysicalPlacement = physicalFrontier ? SemanticPlacement.Frontier : SemanticPlacement.Near,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            PhysicalPlacementPreferredSites = physicalProfile.PreferredSites,
            PhysicalPlacementWarmthAffinity = physicalProfile.WarmthAffinity,
            PhysicalPlacementFireAffinity = physicalProfile.FireAffinity,
            PhysicalPlacementAuroraAffinity = physicalProfile.AuroraAffinity,
            PhysicalPlacementPressureAffinity = physicalProfile.PressureAffinity,
            SemanticPlacement = SemanticPlacement.Near,
            SemanticPlacementPreferredTerrains = semanticProfile.PreferredTerrains,
            SemanticPlacementAvoidedTerrains = semanticProfile.AvoidedTerrains,
            SemanticPlacementFallbackTerrain = semanticProfile.FallbackTerrain,
            PlacementRef = placementRef,
            Culture = IceRemembersCulture.AuroraStack.ToDomainName(),
            CulturePlacement = culturePlacement,
        };
    }
}
