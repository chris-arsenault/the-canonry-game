namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Kinds;
using Engine.WorldGen;

using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;

public static partial class IceRemembersGeneratorDefinition
{
    private const float RoutesDrawReadersMinimumTradeRouteStrength = 0.70f;
    private const float RoutesDrawReadersTradeRouteStrengthSpend = -0.30f;
    private const float CorruptionWakesMonstersMinimumCorruptionStrength = 0.64f;
    private const float CorruptionWakesMonstersCorruptionStrengthSpend = -0.32f;
    private const float WarsGetNamesMinimumWarStrength = 0.65f;
    private const float WarsGetNamesWarStrengthSpend = -0.24f;
    private const float TradeRoutesConveneNegotiationMinimumTradeRouteStrength = 0.72f;
    private const float TradeRoutesConveneNegotiationTradeRouteStrengthSpend = -0.28f;
    private const float ClaimedWatersDrawBullsMinimumClaimStrength = 0.70f;
    private const float OverclaimedBloomsDrawBeastsMinimumClaimStrength = 0.70f;
    private const int WarBreaksTheLandWasteRadius = 5;
    private const int WarBreaksTheLandMaximumLocalWastes = 8;

    public static IReadOnlyList<SemanticGeneratorConfig> Create() =>
    [
        ..CoreGenerators(),
        .. CreateFrontierGenerators(),
        ..WarSagaGenerators(),
        ..EndingsGenerators(),
        ..CreatePaletteGenerators(),
        TradeRoutesConveneNegotiation(),
        ClaimedWatersDrawBulls(),
        PodCurrentsLaunchScouts(),
        OrcaRouteWarsGetNames(),
        ConduitsMapHiddenPeaks(),
        MappedPeaksBirthTechniques(),
        ControlledDepthsWakeLeviathans(),
        OverclaimedBloomsDrawBeasts(),
        CorruptedMonstersRaiseRevenants(),
        StolenArtifactsDrawHunters(),
        MonsterSlayingsBecomeFestivals(),
        HauntedTransportsSpreadPlague(),
        WarOccurrencesLaunchScouts(),
        TradeConferencesRaiseCitadels(),
        ConferencesBecomeFestivals(),
        CleansedSpringsFoundSanctuaries(),
        SanctuariesAnchorColonies(),
        ConduitPointsFoundReefholds(),
        DeepConduitsNameDepths(),
        WardedMonstersFoundOrders(),
        ..RemembranceGenerators(),
        TheAccordHolds(),
        TheSilenceAnswers(),
        RichHubsGrowUndermarkets(),
        WarsFeedShadowBrokers(),
        RecordsCastShadows(),
        TheDeepConclave(),
        KillsSungToTheDeep(),
        EldersDescendToTheGod(),
        RelicsAreGivenToTheDeep(),
        VigilsAtTheCurrent(),
        TheCurrentProvides(),
        PilgrimsFoundNewCurrents(),
        PodsMarkTheirPrey(),
        AHeroFallsToTheDeep(),
        GloryToTheDeadGod(),
        CounterOffensive(),
        TheAccordConvenes(),
        TheHuntSettlesIn(),
        TheFearFades(),
        MartyrsAreWitnessed(),
        WardensRiseWhereTheHuntBites(),
        MonstersStalkTheMarches(),
        PodsHuntTheConvoys(),
        TheDreadSongBreaksTheWatch(),
        WardensTakeUpTheOldForms(),
    ];

    private static string L(Rel relationship) => relationship.ToDomainName();

    private static SemanticEntityQuery SameCultureAsTarget(bool requireCultureRoute = false) =>
        new()
        {
            TargetCultureMatch = QueryTargetCultureMatch.SameAsTarget,
            RequireTargetCultureRoute = requireCultureRoute,
        };

    private static IReadOnlyList<SemanticGeneratorConfig> CoreGenerators() =>
    [
        IceSettlesAccounts(),
        WhatTheSpringRemembers(),
        WarBreaksTheLand(),
        RoutesDrawReaders(),
        WhatTheArtifactKnows(),
        UnclaimedPlacesDrawWardens(),
        PracticedFormsFindTomes(),
        ChartersFindHarbors(),
        VigilsLeaveRelics(),
        CorruptionWakesMonsters(),
    ];

    private static IReadOnlyList<SemanticGeneratorConfig> WarSagaGenerators() =>
    [
        WarsGetNames(),
        AlliesAnswerTheCall(),
        WarTakesItsToll(),
        TheColumnHolds(),
    ];

    private static SemanticGeneratorConfig IceSettlesAccounts() => new()
    {
        Id = "ice_settles_accounts",
        Name = "Ice Settles Accounts",
        Description = "The reckoning. The ice calls a price for the war: a penguin of the faction's culture does not return, a memorial seal closes the account, and the war ends.",
        CausalLimit = "Depends on an at-war relationship and an offered penguin; marks the penguin historical and removes the selected at-war relationship.",
        Target = TargetWithOutgoing(K.Faction, Rel.AtWarWith),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$offered",
                Kind = K.Penguin,
                Query = new SemanticEntityQuery { TargetCultureMatch = QueryTargetCultureMatch.SameAsTarget },
            },
        ],
        Produces =
        [
            Required("$seal", K.Artifact, "seal", "$target", "$target"),
            Required("$witness", K.Penguin, "witness", "$target", "$target"),
            Optional("$exile", K.Penguin, "exile", "$target", "$target", chance: 0.40f),
            ProducedCanon("$peace", "accord", "$target", culture: C(IceRemembersCulture.TheIce), inheritCultureFrom: null, chance: 0.50f),
        ],
        Effects =
        [
            new ErodeCultureAtTargetEffect(radius: 4, amountPerCell: 25f),
            new SetBindingHistoricalEffect("$offered"),
            new ClaimSemanticJurisdictionEffect("$peace", CanonJurisdictionRadius),
            new RemoveTargetRelationshipsEffect(
                L(Rel.AtWarWith),
                GeneratorRelationshipInstanceSelection.TriggerRelationship,
                includeReciprocal: true),
        ],
        Relationships =
        [
            R("$seal", "$target", Rel.Memorializes, 0.90f),
            R("$witness", "$target", Rel.MemberOf, 0.85f),
            R("$exile", "$target", Rel.BetrayedBy, 0.70f),
            R("$peace", "$target", Rel.ClaimedBy, 0.50f),
        ],
        Triggers = TriggerOn(K.Faction),
        PulseWeight = 10,
    };

    private static SemanticGeneratorConfig WhatTheSpringRemembers() => new()
    {
        Id = "what_the_spring_remembers",
        Name = "What the Spring Remembers",
        Description = "A cleansed hot spring draws a witness and a rite of memory. Where the warmth holds long enough, a sanctuary may form, and a choir may settle to sing what the ice forgot.",
        CausalLimit = "Depends on an incoming cleansed-by relationship and absence of memorialization; creates an anchored memorial rite on the region.",
        Target = TargetWithIncomingAndWithoutIncoming(K.Region, Rel.CleansedBy, Rel.Memorializes),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$patron",
                Kind = K.Faction,
                Query = SameCultureAsTarget(requireCultureRoute: true),
            },
        ],
        Produces =
        [
            Required("$rite", K.Ability, "rite", "$target", "$patron"),
            Required("$witness", K.Penguin, "witness", "$target", "$patron"),
            Optional("$sanctuary", K.Settlement, "sanctuary", "$target", "$patron", chance: 0.55f),
            Optional("$choir", K.Faction, "choir", "$target", "$patron", chance: 0.35f),
        ],
        Relationships =
        [
            R("$rite", "$target", Rel.Anchored, 0.90f),
            R("$rite", "$target", Rel.Memorializes, 0.90f),
            R("$rite", "$witness", Rel.PracticedBy, 0.78f),
            R("$witness", "$patron", Rel.MemberOf, 0.85f),
            R("$sanctuary", "$patron", Rel.ControlledBy, 0.80f),
            R("$sanctuary", "$target", Rel.FoundedFrom, 0.85f),
            R("$choir", "$patron", Rel.AlliedWith, 0.70f),
        ],
        Triggers = TriggerOn(K.Region),
    };

    private static SemanticGeneratorConfig WarBreaksTheLand() => new()
    {
        Id = "war_breaks_the_land",
        Name = "War Breaks the Land",
        Description = "Sustained war scars the outer face into wastes. Survivors sometimes form a compact in the ruin; weapons are left where the fighting cracked.",
        CausalLimit = "Depends on an at-war relationship and fewer than eight local waste cells; creates a scar region that turns finite nearby land into wastes.",
        Target = TargetWithOutgoingAndLocalTerrainBelow(
            K.Faction,
            Rel.AtWarWith,
            Terrain.Wastes,
            WarBreaksTheLandWasteRadius,
            WarBreaksTheLandMaximumLocalWastes),
        Produces =
        [
            ScarRegion("$scar", "$target"),
            Optional("$weapon", K.Artifact, "weapon", "$scar", "$target", chance: 0.35f),
            Optional("$compact", K.Faction, "compact", "$scar", "$target", chance: 0.40f),
        ],
        Relationships =
        [
            R("$scar", "$target", Rel.CorruptedBy, 0.82f),
            R("$weapon", "$scar", Rel.Anchored, 0.85f),
            R("$scar", "$compact", Rel.ClaimedBy, 0.78f),
            R("$compact", "$target", Rel.RivalOf, 0.75f),
        ],
        Triggers = TriggerOn(K.Faction),
        PulseWeight = 18,
    };

    private static SemanticGeneratorConfig RoutesDrawReaders() => new()
    {
        Id = "routes_draw_readers",
        Name = "Routes Draw Readers",
        Description = "An active trade route attracts a broker and a waystation harbor. Where the wealth concentrates, a formal order may take charge of what passes through.",
        CausalLimit = "Depends on high trade-route strength and reduces the selected trade route strength.",
        Target = TargetWithStrongOutgoing(K.Settlement, Rel.TradeRoute, RoutesDrawReadersMinimumTradeRouteStrength),
        Produces =
        [
            Required("$broker", K.Penguin, "broker", "$target", "$target"),
            HarborAcross("$harbor", "$target"),
            Optional("$order", K.Faction, "order", "$harbor", "$target", chance: 0.40f),
            Optional("$instrument", K.Artifact, "instrument", "$harbor", "$target", chance: 0.45f),
        ],
        Relationships =
        [
            R("$broker", "$harbor", Rel.HomeBase, 1f),
            R("$harbor", "$target", Rel.FoundedFrom, 0.90f),
            R("$harbor", "$target", Rel.TradeRoute, IceRemembersRelationshipStrengths.InitialTradeRoute),
            R("$harbor", "$order", Rel.ControlledBy, 0.82f),
            R("$broker", "$order", Rel.MemberOf, 0.80f),
            R("$instrument", "$harbor", Rel.NativeTo, 0.82f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.TradeRoute),
                RoutesDrawReadersTradeRouteStrengthSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship,
                includeReciprocal: true),
        ],
        Triggers = TriggerOn(K.Settlement),
    };

    private static SemanticGeneratorConfig WhatTheArtifactKnows() => new()
    {
        Id = "what_the_artifact_knows",
        Name = "What the Artifact Knows",
        Description = "A penguin who has carried an artifact long enough recognises a place to stop. They put down a rookery, seal the artifact into the ground, and a small cell may form around them.",
        CausalLimit = "Depends on a held artifact and absence of a home base; creates a home-base relationship for the penguin.",
        Target = TargetWithOutgoingAndWithoutOutgoing(K.Penguin, Rel.Holds, Rel.HomeBase),
        Produces =
        [
            Required("$rookery", K.Settlement, "rookery", "$target", "$target", frontier: true),
            Required("$seal", K.Artifact, "seal", "$rookery", "$target"),
            Optional("$cell", K.Faction, "cell", "$target", "$target", chance: 0.45f),
        ],
        Relationships =
        [
            R("$target", "$rookery", Rel.HomeBase, 1f),
            R("$seal", "$rookery", Rel.Anchored, 0.88f),
            R("$rookery", "$cell", Rel.ControlledBy, 0.82f),
            R("$cell", "$target", Rel.LedBy, 0.85f),
        ],
        Triggers = TriggerOn(K.Penguin),
    };

    private static SemanticGeneratorConfig UnclaimedPlacesDrawWardens() => new()
    {
        Id = "unclaimed_places_draw_wardens",
        Name = "Unclaimed Places Draw Wardens",
        Description = "A named feature without a claimant draws a factional warden and a survey scout. The place becomes claimed, and the warden gains a route asset for the watch.",
        CausalLimit = "Depends on a point of interest with no outgoing claimed-by relationship; creates that claimed-by relationship as the repeat lock.",
        Target = TargetWithoutOutgoing(K.PointOfInterest, Rel.ClaimedBy),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget { Ref = "$patron", Kind = K.Faction },
        ],
        Produces =
        [
            Required("$warden", K.Penguin, "warden", "$target", "$patron"),
            Required("$scout", K.Transport, "scout", "$target", "$patron"),
        ],
        Relationships =
        [
            R("$target", "$patron", Rel.ClaimedBy, 0.76f),
            R("$warden", "$patron", Rel.MemberOf, 0.82f),
            R("$warden", "$target", Rel.HomeBase, 0.85f),
            R("$warden", "$scout", Rel.Holds, 1f),
            R("$scout", "$target", Rel.NativeTo, 0.82f),
            R("$scout", "$target", Rel.ConduitOf, 0.70f),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        PulseWeight = 2,
    };

    private static SemanticGeneratorConfig PracticedFormsFindTomes() => new()
    {
        Id = "practiced_forms_find_tomes",
        Name = "Practiced Forms Find Tomes",
        Description = "A practiced ability gathers enough lineage force to become a written form. A student enters the record, carrying the first tome of the technique.",
        CausalLimit = "Depends on an ability with practiced-by lineage and no outgoing conduit-of relationship; creates a conduit-of tome as the repeat lock.",
        Target = TargetWithOutgoingAndWithoutOutgoing(K.Ability, Rel.PracticedBy, Rel.ConduitOf),
        Produces =
        [
            Required("$student", K.Penguin, "witness", "$target", "$target"),
            Required("$tome", K.Artifact, "tome", "$target", "$target"),
        ],
        Relationships =
        [
            R("$target", "$student", Rel.PracticedBy, 0.76f),
            R("$student", "$tome", Rel.Holds, 1f),
            R("$target", "$tome", Rel.ConduitOf, 0.84f),
            R("$tome", "$target", Rel.PartOf, 0.80f),
        ],
        Triggers = TriggerOn(K.Ability),
        PulseWeight = 2,
    };

    private static SemanticGeneratorConfig ChartersFindHarbors() => new()
    {
        Id = "charters_find_harbors",
        Name = "Charters Find Harbors",
        Description = "A held transport becomes more than cargo: its charter calls a harbor into the named world, and a broker settles where the route can be read.",
        CausalLimit = "Depends on a transport held by an agency and lacking a native-to port; creates that native-to port as the repeat lock.",
        Target = TargetWithIncomingAndWithoutOutgoing(K.Transport, Rel.Holds, Rel.NativeTo),
        Produces =
        [
            Required("$harbor", K.Settlement, "harbor", "$target", "$target", frontier: true, physicalFrontier: true),
            Required("$broker", K.Penguin, "broker", "$harbor", "$target"),
            Optional("$instrument", K.Artifact, "instrument", "$harbor", "$target", chance: 0.45f),
        ],
        Relationships =
        [
            R("$target", "$harbor", Rel.NativeTo, 0.90f),
            R("$broker", "$harbor", Rel.HomeBase, 1f),
            R("$harbor", "$broker", Rel.ControlledBy, 0.72f),
            R("$instrument", "$harbor", Rel.Anchored, 0.80f),
        ],
        Triggers = TriggerOn(K.Transport),
        PulseWeight = 2,
    };

    private static SemanticGeneratorConfig VigilsLeaveRelics() => new()
    {
        Id = "vigils_leave_relics",
        Name = "Vigils Leave Relics",
        Description = "An active occurrence with a home base leaves a witness and a relic behind. The event becomes something the world can remember instead of only endure.",
        CausalLimit = "Depends on an occurrence with a home base and no incoming memorializes relationship; creates an incoming memorializes relic as the repeat lock.",
        Target = TargetWithOutgoingAndWithoutIncoming(K.Occurrence, Rel.HomeBase, Rel.Memorializes),
        Produces =
        [
            Required("$relic", K.Artifact, "relic", "$target", "$target"),
            Required("$witness", K.Penguin, "witness", "$target", "$target"),
        ],
        Relationships =
        [
            R("$relic", "$target", Rel.Memorializes, 0.88f),
            R("$witness", "$target", Rel.MemberOf, 0.70f),
            R("$witness", "$relic", Rel.Holds, 1f),
        ],
        Triggers = TriggerOn(K.Occurrence),
        PulseWeight = 2,
    };

    private static SemanticGeneratorConfig CorruptionWakesMonsters() => new()
    {
        Id = "corruption_wakes_monsters",
        Name = "Corruption Wakes Monsters",
        Description = "A strong uncured scar wakes a monster under the ice. The waking leaves a route-scarred scout and a local lair; transports can later ward or carry the haunting onward.",
        CausalLimit = "Depends on a strong outgoing corrupted-by relationship from a scar with no incoming cleansed-by; spends the selected corruption strength below the wake threshold.",
        Target = TargetWithStrongOutgoingSubtypeAndWithoutIncoming(
            K.Region,
            "scar",
            Rel.CorruptedBy,
            CorruptionWakesMonstersMinimumCorruptionStrength,
            Rel.CleansedBy),
        Produces =
        [
            Required("$monster", K.Monster, "wraith", "$target", "$target"),
            Required("$scout", K.Transport, "scout", "$target", "$target"),
            Optional("$witness", K.Penguin, "witness", "$target", "$target", chance: 0.45f),
        ],
        Relationships =
        [
            R("$target", "$monster", Rel.CorruptedBy, 0.86f),
            R("$monster", "$target", Rel.HomeBase, 1f),
            R("$scout", "$target", Rel.NativeTo, 0.82f),
            R("$scout", "$monster", Rel.Memorializes, 0.76f),
            R("$witness", "$scout", Rel.Holds, 1f),
            R("$witness", "$target", Rel.HomeBase, 0.72f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.CorruptedBy),
                CorruptionWakesMonstersCorruptionStrengthSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship),
        ],
        Triggers = TriggerOn(K.Region),
        PulseWeight = 12,
    };

    private static SemanticGeneratorConfig WarsGetNames() => new()
    {
        Id = "wars_get_names",
        Name = "Wars Get Names",
        Description = "A war that stays strong long enough becomes a named occurrence. Witnesses enter the record, weapons circulate, and the active war force is partly spent into the event.",
        CausalLimit = "Depends on a strong outgoing at-war relationship and reduces the selected at-war relationship, including its reciprocal.",
        Target = TargetWithStrongOutgoing(K.Faction, Rel.AtWarWith, WarsGetNamesMinimumWarStrength),
        Produces =
        [
            Required("$war", K.Occurrence, "war", "$target", "$target"),
            Required("$witness", K.Penguin, "witness", "$target", "$target"),
            Optional("$weapon", K.Artifact, "weapon", "$target", "$target", chance: 0.55f),
        ],
        Relationships =
        [
            R("$war", "$target", Rel.AtWarWith, 0.62f),
            R("$witness", "$war", Rel.MemberOf, 0.78f),
            R("$witness", "$target", Rel.MemberOf, 0.72f),
            R("$witness", "$weapon", Rel.Holds, 1f),
            R("$weapon", "$war", Rel.Memorializes, 0.80f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.AtWarWith),
                WarsGetNamesWarStrengthSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship,
                includeReciprocal: true),
        ],
        Triggers = TriggerOn(K.Faction),
        PulseWeight = 18,
    };

    private static SemanticGeneratorConfig TradeRoutesConveneNegotiation() => new()
    {
        Id = "trade_routes_convene_negotiation",
        Name = "Trade Routes Convene Negotiation",
        Description = "A strong trade route convenes a trade conference instead of only drawing more readers. A broker, accord instrument, and sometimes a trade vessel enter the route network.",
        CausalLimit = "Depends on a strong outgoing trade-route relationship and no incoming memorializes lock on the host settlement; spends the selected trade route and creates a memorial accord.",
        Target = TargetWithStrongOutgoingAndWithoutIncoming(
            K.Settlement,
            Rel.TradeRoute,
            TradeRoutesConveneNegotiationMinimumTradeRouteStrength,
            Rel.Memorializes),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$patron",
                Kind = K.Faction,
                Query = SameCultureAsTarget(requireCultureRoute: true),
            },
        ],
        Produces =
        [
            Required("$conference", K.Occurrence, "trade_conference", "$target", "$target"),
            Required("$broker", K.Penguin, "broker", "$target", "$target"),
            Required("$accord", K.Artifact, "instrument", "$target", "$target"),
            Optional("$charter", K.Transport, "trade", "$target", "$target", chance: 0.35f),
        ],
        Relationships =
        [
            R("$conference", "$target", Rel.HomeBase, 1f),
            R("$broker", "$conference", Rel.MemberOf, 0.80f),
            R("$broker", "$target", Rel.HomeBase, 0.85f),
            R("$broker", "$patron", Rel.MemberOf, 0.75f),
            R("$patron", "$conference", Rel.MemberOf, 0.72f),
            R("$accord", "$target", Rel.Memorializes, 0.82f),
            R("$accord", "$conference", Rel.Memorializes, 0.84f),
            R("$charter", "$target", Rel.NativeTo, 0.78f),
            R("$patron", "$charter", Rel.Holds, 1f),
        ],
        Effects =
        [
            new AdjustTargetRelationshipStrengthEffect(
                L(Rel.TradeRoute),
                TradeRoutesConveneNegotiationTradeRouteStrengthSpend,
                GeneratorRelationshipInstanceSelection.TriggerRelationship,
                includeReciprocal: true),
        ],
        Triggers = TriggerOn(K.Settlement),
    };

    private static SemanticGeneratorConfig ClaimedWatersDrawBulls() => new()
    {
        Id = "claimed_waters_draw_bulls",
        Name = "Claimed Waters Draw Bulls",
        Description = "A named place claimed strongly enough to matter attracts an orca bull from the circling pod. The claim stops being abstract jurisdiction and becomes a living presence at the site.",
        CausalLimit = "Depends on a strong claimed-by relationship and no current home-base presence at the point; creates an orca home-base relationship as the repeat lock.",
        Target = TargetWithStrongOutgoingAndWithoutIncoming(
            K.PointOfInterest,
            Rel.ClaimedBy,
            ClaimedWatersDrawBullsMinimumClaimStrength,
            Rel.HomeBase),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$caller",
                Kind = K.Orca,
                Query = SameCultureAsTarget(requireCultureRoute: true),
            },
        ],
        Produces =
        [
            Required("$bull", K.Orca, "bull", "$target", "$caller"),
        ],
        Relationships =
        [
            R("$bull", "$target", Rel.HomeBase, 1f),
            R("$bull", "$caller", Rel.AlliedWith, 0.74f),
            R("$target", "$bull", Rel.ClaimedBy, 0.68f),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
    };

    private static SemanticGeneratorConfig ConduitsMapHiddenPeaks() => new()
    {
        Id = "conduits_map_hidden_peaks",
        Name = "Conduits Map Hidden Peaks",
        Description = "A transport with a native port begins to make its route legible. A peak enters the record as a navigational fact, not a decorative landmark.",
        CausalLimit = "Depends on a native-to transport and no incoming memorial lock on the transport; creates a memorial peak on that transport as the repeat lock.",
        Target = TargetWithOutgoingAndWithoutIncoming(K.Transport, Rel.NativeTo, Rel.Memorializes),
        Produces =
        [
            // The peak the record gains is a real one: the named point
            // lands on a revealed summit feature the charts can point to.
            FeatureDiscovery("$peak", K.PointOfInterest, "peak", "peak"),
        ],
        Relationships =
        [
            R("$target", "$peak", Rel.ConduitOf, 0.75f),
            R("$peak", "$target", Rel.Memorializes, 0.80f),
        ],
        Triggers = TriggerOn(K.Transport),
    };

    private static SemanticGeneratorConfig MappedPeaksBirthTechniques() => new()
    {
        Id = "mapped_peaks_birth_techniques",
        Name = "Mapped Peaks Birth Techniques",
        Description = "A newly mapped peak changes how bodies cross the ice. The route becomes a technique, then a carried form.",
        CausalLimit = "Depends on a peak with incoming conduit topology and no incoming memorialized technique; creates that memorialized technique as the repeat lock.",
        Target = TargetWithIncomingSubtypeAndWithoutIncoming(K.PointOfInterest, "peak", Rel.ConduitOf, Rel.Memorializes),
        Produces =
        [
            Required("$technique", K.Ability, "technique", "$target", "$target"),
            Required("$manual", K.Artifact, "tome", "$technique", "$technique"),
        ],
        Relationships =
        [
            R("$technique", "$target", Rel.ConduitOf, 0.78f),
            R("$technique", "$target", Rel.Memorializes, 0.82f),
            R("$manual", "$technique", Rel.PartOf, 0.80f),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
    };

    private static SemanticGeneratorConfig ControlledDepthsWakeLeviathans() => new()
    {
        Id = "controlled_depths_wake_leviathans",
        Name = "Controlled Depths Wake Leviathans",
        Description = "A controlled bloom or shoal is no longer merely a resource fact. The depth answers with a leviathan, and the disturbance organizes into plague.",
        CausalLimit = "Depends on a controlled bloom or shoal with no outgoing corruption; creates point-to-leviathan corruption as the repeat lock.",
        Target = TargetWithOutgoingSubtypesAndWithoutOutgoing(
            K.PointOfInterest,
            ["bloom", "shoal"],
            Rel.ControlledBy,
            Rel.CorruptedBy),
        Produces =
        [
            Required("$leviathan", K.Monster, "leviathan", "$target", "$target"),
            Required("$plague", K.Occurrence, "plague", "$target", "$target"),
        ],
        Relationships =
        [
            R("$target", "$leviathan", Rel.CorruptedBy, 0.86f),
            R("$leviathan", "$target", Rel.HomeBase, 1f),
            R("$plague", "$target", Rel.HomeBase, 1f),
            R("$plague", "$leviathan", Rel.CorruptedBy, 0.78f),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        PulseWeight = 8,
    };

    private static SemanticGeneratorConfig OverclaimedBloomsDrawBeasts() => new()
    {
        Id = "overclaimed_blooms_draw_beasts",
        Name = "Overclaimed Blooms Draw Beasts",
        Description = "A bloom or shoal claimed as property becomes overworked. The extraction names a beast at the edge of the harvest.",
        CausalLimit = "Depends on a strong claimed-by relationship at a bloom or shoal and no outgoing corruption; creates point-to-beast corruption as the repeat lock.",
        Target = TargetWithStrongOutgoingSubtypesAndWithoutOutgoing(
            K.PointOfInterest,
            ["bloom", "shoal"],
            Rel.ClaimedBy,
            OverclaimedBloomsDrawBeastsMinimumClaimStrength,
            Rel.CorruptedBy),
        Produces =
        [
            Required("$beast", K.Monster, "beast", "$target", "$target"),
        ],
        Relationships =
        [
            R("$target", "$beast", Rel.CorruptedBy, 0.82f),
            R("$beast", "$target", Rel.HomeBase, 1f),
        ],
        Triggers = TriggerOn(K.PointOfInterest),
        PulseWeight = 8,
    };

    private static SemanticGeneratorConfig CorruptedMonstersRaiseRevenants() => new()
    {
        Id = "corrupted_monsters_raise_revenants",
        Name = "Corrupted Monsters Raise Revenants",
        Description = "A monster named by corruption does not stay singular. The harm throws off an older return with its own appetite and memory.",
        CausalLimit = "Depends on a monster with incoming corruption and no outgoing rivalry; creates a monster-to-revenant rivalry as the repeat lock.",
        Target = TargetWithIncomingAndWithoutOutgoing(K.Monster, Rel.CorruptedBy, Rel.RivalOf),
        Produces =
        [
            Required("$revenant", K.Monster, "revenant", "$target", "$target"),
        ],
        Relationships =
        [
            R("$target", "$revenant", Rel.RivalOf, 0.78f),
            R("$revenant", "$target", Rel.CorruptedBy, 0.74f),
        ],
        Triggers = TriggerOn(K.Monster),
    };

    private static SemanticGeneratorConfig WarOccurrencesLaunchScouts() => new()
    {
        Id = "war_occurrences_launch_scouts",
        Name = "War Occurrences Launch Scouts",
        Description = "Once a war has a name, it needs eyes. A scout becomes part of the occurrence rather than an anonymous errand.",
        CausalLimit = "Depends on a named war occurrence with no held transport; creates the held scout as the repeat lock.",
        Target = TargetSubtypeWithOutgoingAndWithoutOutgoing(K.Occurrence, "war", Rel.AtWarWith, Rel.Holds),
        Produces =
        [
            Required("$scout", K.Transport, "scout", "$target", "$target"),
        ],
        Relationships =
        [
            R("$target", "$scout", Rel.Holds, 1f),
            R("$scout", "$target", Rel.Memorializes, 0.78f),
        ],
        Triggers = TriggerOn(K.Occurrence),
    };

    private static SemanticGeneratorConfig TradeConferencesRaiseCitadels() => new()
    {
        Id = "trade_conferences_raise_citadels",
        Name = "Trade Conferences Raise Citadels",
        Description = "A trade conference that gathers enough logistical gravity creates a fortified moving ground: not a settlement, but a citadel under tow.",
        CausalLimit = "Depends on a trade-conference occurrence with a home base and no held transport; creates the held citadel as the repeat lock.",
        Target = TargetSubtypeWithOutgoingAndWithoutOutgoing(K.Occurrence, "trade_conference", Rel.HomeBase, Rel.Holds),
        Produces =
        [
            Required("$citadel", K.Transport, "citadel", "$target", "$target"),
        ],
        Relationships =
        [
            R("$target", "$citadel", Rel.Holds, 1f),
            R("$citadel", "$target", Rel.Memorializes, 0.82f),
        ],
        Triggers = TriggerOn(K.Occurrence),
        PulseWeight = 6,
    };

    private static SemanticGeneratorConfig ConferencesBecomeFestivals() => new()
    {
        Id = "conferences_become_festivals",
        Name = "Conferences Become Festivals",
        Description = "A conference with an accord stops being only negotiation. The host culture turns attention into festival before the obligations have cooled.",
        CausalLimit = "Depends on a memorialized trade conference with no allied festival; creates an allied festival as the repeat lock.",
        Target = TargetSubtypeWithIncomingAndWithoutOutgoing(K.Occurrence, "trade_conference", Rel.Memorializes, Rel.AlliedWith),
        SecondaryTargets =
        [
            new SemanticGeneratorBindingTarget
            {
                Ref = "$host",
                Kind = K.Settlement,
                Query = SameCultureAsTarget(requireCultureRoute: true),
            },
        ],
        Produces =
        [
            Required("$festival", K.Occurrence, "festival", "$host", "$target"),
        ],
        Relationships =
        [
            R("$target", "$festival", Rel.AlliedWith, 0.74f),
            R("$festival", "$target", Rel.AlliedWith, 0.74f),
            R("$festival", "$host", Rel.HomeBase, 1f),
        ],
        Triggers = TriggerOn(K.Occurrence),
    };

    private static SemanticGeneratorTarget TargetWithOutgoing(EntityKind kind, Rel required) =>
        new()
        {
            Kind = kind,
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
        };

    private static SemanticGeneratorTarget TargetWithStrongIncomingAndWithoutIncoming(
        EntityKind kind,
        Rel required,
        float minimumStrength,
        Rel without) =>
        new()
        {
            Kind = kind,
            RequiredIncomingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement
                {
                    RelationshipKind = L(required),
                    MinimumStrength = minimumStrength,
                },
            ],
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetWithStrongOutgoingSubtypeAndWithoutIncoming(
        EntityKind kind,
        string subtype,
        Rel required,
        float minimumStrength,
        Rel without) =>
        new()
        {
            Kind = kind,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { subtype },
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement
                {
                    RelationshipKind = L(required),
                    MinimumStrength = minimumStrength,
                },
            ],
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetWithStrongOutgoingSubtypesAndWithoutOutgoing(
        EntityKind kind,
        IReadOnlyList<string> subtypes,
        Rel required,
        float minimumStrength,
        Rel without) =>
        new()
        {
            Kind = kind,
            Subtypes = new HashSet<string>(subtypes, StringComparer.Ordinal),
            RequiredOutgoingRelationshipRequirements =
            [
                new SemanticGeneratorRelationshipRequirement
                {
                    RelationshipKind = L(required),
                    MinimumStrength = minimumStrength,
                },
            ],
            WithoutOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetWithOutgoingSubtypesAndWithoutOutgoing(
        EntityKind kind,
        IReadOnlyList<string> subtypes,
        Rel required,
        Rel without) =>
        new()
        {
            Kind = kind,
            Subtypes = new HashSet<string>(subtypes, StringComparer.Ordinal),
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            WithoutOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetWithOutgoingAndLocalTerrainBelow(
        EntityKind kind,
        Rel required,
        Terrain terrain,
        int radius,
        int maximumExclusive) =>
        new()
        {
            Kind = kind,
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            TerrainCountRequirements =
            [
                new SemanticGeneratorTerrainCountRequirement
                {
                    TerrainName = terrain.ToDomainName(),
                    Radius = radius,
                    MaximumExclusive = maximumExclusive,
                },
            ],
        };

    private static SemanticGeneratorTarget TargetWithOutgoingAndWithoutOutgoing(
        EntityKind kind,
        Rel required,
        Rel without) =>
        new()
        {
            Kind = kind,
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            WithoutOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetWithIncomingAndWithoutOutgoing(
        EntityKind kind,
        Rel required,
        Rel without) =>
        new()
        {
            Kind = kind,
            RequiredIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            WithoutOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetSubtypeWithOutgoingAndWithoutOutgoing(
        EntityKind kind,
        string subtype,
        Rel required,
        Rel without) =>
        new()
        {
            Kind = kind,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { subtype },
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            WithoutOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetSubtypeWithIncomingAndWithoutOutgoing(
        EntityKind kind,
        string subtype,
        Rel required,
        Rel without) =>
        new()
        {
            Kind = kind,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { subtype },
            RequiredIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            WithoutOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetWithOutgoingAndWithoutIncoming(
        EntityKind kind,
        Rel required,
        Rel without) =>
        new()
        {
            Kind = kind,
            RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetWithIncomingAndWithoutIncoming(
        EntityKind kind,
        Rel required,
        Rel without) =>
        new()
        {
            Kind = kind,
            RequiredIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetWithIncomingSubtypeAndWithoutIncoming(
        EntityKind kind,
        string subtype,
        Rel required,
        Rel without) =>
        new()
        {
            Kind = kind,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { subtype },
            RequiredIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(required) },
            WithoutIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static SemanticGeneratorTarget TargetWithoutOutgoing(EntityKind kind, Rel without) =>
        new()
        {
            Kind = kind,
            WithoutOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { L(without) },
        };

    private static IReadOnlyList<SemanticGeneratorTrigger> TriggerOn(EntityKind kind) =>
    [
        new() { OnKind = kind },
    ];

    // frontier: true marks a produced entity as an outward expansion anchor - it
    // lands at the leading edge of its culture on the idea-plane (a daughter that
    // spreads the culture) rather than near its parent. Reserve it for entities
    // whose fiction is reaching new ground (colonies, newly founded sites), not
    // retinue or entities anchored to a specific target.
    private static SemanticProducedEntity Required(
        string @ref,
        EntityKind kind,
        string subtype,
        string placementRef,
        string cultureRef,
        bool on = false,
        bool frontier = false,
        bool physicalFrontier = false)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(kind, subtype);
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(kind, subtype);
        // Physical frontier is the founding ratchet: the entity lands on
        // known free ground just past its culture's border, reveals the fog
        // around itself, and becomes the anchor the next push starts from.
        var physicalPlacement = on ? SemanticPlacement.On : SemanticPlacement.Near;
        if (physicalFrontier)
        {
            physicalPlacement = SemanticPlacement.Frontier;
        }

        return new()
        {
            Ref = @ref,
            Kind = kind,
            Subtype = subtype,
            Role = subtype,
            PhysicalPlacement = physicalPlacement,
            PhysicalPlacementPreferredTerrains = physicalProfile.PreferredTerrains,
            PhysicalPlacementAvoidedTerrains = physicalProfile.AvoidedTerrains,
            PhysicalPlacementFallbackTerrain = physicalProfile.FallbackTerrain,
            PhysicalPlacementPreferredSites = physicalProfile.PreferredSites,
            PhysicalPlacementWarmthAffinity = physicalProfile.WarmthAffinity,
            PhysicalPlacementFireAffinity = physicalProfile.FireAffinity,
            PhysicalPlacementAuroraAffinity = physicalProfile.AuroraAffinity,
            PhysicalPlacementPressureAffinity = physicalProfile.PressureAffinity,
            SemanticPlacement = frontier ? SemanticPlacement.Frontier : SemanticPlacement.Near,
            SemanticPlacementPreferredTerrains = semanticProfile.PreferredTerrains,
            SemanticPlacementAvoidedTerrains = semanticProfile.AvoidedTerrains,
            SemanticPlacementFallbackTerrain = semanticProfile.FallbackTerrain,
            PlacementRef = placementRef,
            InheritCultureFrom = cultureRef,
            CulturePlacement = physicalFrontier
                ? GeneratorCulturePlacement.CultureFrontier
                : CulturePlacementFor(kind),
        };
    }

    private static SemanticProducedEntity Optional(
        string @ref,
        EntityKind kind,
        string subtype,
        string placementRef,
        string cultureRef,
        float chance,
        bool on = false)
    {
        var physicalProfile = IceRemembersPlacementTerrainProfiles.Physical(kind, subtype);
        var semanticProfile = IceRemembersPlacementTerrainProfiles.Semantic(kind, subtype);
        return new()
        {
            Ref = @ref,
            Kind = kind,
            Subtype = subtype,
            Role = subtype,
            CreateChance = chance,
            PhysicalPlacement = on ? SemanticPlacement.On : SemanticPlacement.Near,
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
            InheritCultureFrom = cultureRef,
            CulturePlacement = CulturePlacementFor(kind),
        };
    }

    private static SemanticGeneratorRelationship R(
        string source,
        string destination,
        Rel kind,
        float strength,
        bool replaceExisting = false) =>
        new()
        {
            SourceRef = source,
            DestinationRef = destination,
            Kind = L(kind),
            Strength = strength,
            ReplaceExistingOutgoingOfSameKind = replaceExisting,
        };
}
