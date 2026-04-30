namespace IceRemembers;

using Engine.Resources;

public enum IceRemembersAttributeId
{
    TempHp,
    Dominion,
    Shelter,
    Cohesion,
    Reach,
    Provisions,
    Vitality,
    Echo,
    Warmth,
    Stability,
    Holding,
    Pristiness,
    Condition,
    Power,
    Resonance,
}

public static class IceRemembersAttributeIdExtensions
{
    private static readonly string[] DomainNames =
    [
        "temp_hp",
        "dominion",
        "shelter",
        "cohesion",
        "reach",
        "provisions",
        "vitality",
        "echo",
        "warmth",
        "stability",
        "holding",
        "pristiness",
        "condition",
        "power",
        "resonance",
    ];

    public static string ToDomainName(this IceRemembersAttributeId attribute)
    {
        var index = (int)attribute;
        return (uint)index < (uint)DomainNames.Length
            ? DomainNames[index]
            : throw new ArgumentOutOfRangeException(nameof(attribute), attribute, "Unknown test-domain attribute");
    }
}

public enum IceRemembersResource
{
    Food,
    Wealth,
    Population,
}

public static class IceRemembersResourceExtensions
{
    public static string ToDomainName(this IceRemembersResource resource) => resource switch
    {
        IceRemembersResource.Food => "food",
        IceRemembersResource.Wealth => "wealth",
        IceRemembersResource.Population => "population",
        _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, "Unknown test-domain resource"),
    };

    public static ResourceType ToResourceType(this IceRemembersResource resource) => resource switch
    {
        IceRemembersResource.Food => ResourceType.Food,
        IceRemembersResource.Wealth => ResourceType.Wealth,
        IceRemembersResource.Population => ResourceType.Population,
        _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, "Unknown test-domain resource"),
    };
}

public enum IceRemembersRelationship
{
    ControlledBy,
    HomeBase,
    AlliedWith,
    RivalOf,
    LedBy,
    MemberOf,
    Holds,
    CreatedBy,
    Memorializes,
    ClaimedBy,
    TradeRoute,
    FoundedFrom,
    NativeTo,
    Anchored,
    ConduitOf,
    PartOf,
    WardAgainst,
    CorruptedBy,
    CleansedBy,
    AtWarWith,
    BetrayedBy,
}

public enum IceRemembersRelationshipFact
{
    CalledUnhuddled,
    SealedThinPlace,
    OpenedOldHolding,
    CrackedBerg,
    NamedOffice,
    RaisedReefhold,
    YieldedTome,
}

public static class IceRemembersRelationshipFactExtensions
{
    public static string ToDomainName(this IceRemembersRelationshipFact fact) => fact switch
    {
        IceRemembersRelationshipFact.CalledUnhuddled => "called_unhuddled",
        IceRemembersRelationshipFact.SealedThinPlace => "sealed_thin_place",
        IceRemembersRelationshipFact.OpenedOldHolding => "opened_old_holding",
        IceRemembersRelationshipFact.CrackedBerg => "cracked_berg",
        IceRemembersRelationshipFact.NamedOffice => "named_office",
        IceRemembersRelationshipFact.RaisedReefhold => "raised_reefhold",
        IceRemembersRelationshipFact.YieldedTome => "yielded_tome",
        _ => throw new ArgumentOutOfRangeException(nameof(fact), fact, "Unknown Ice Remembers relationship fact"),
    };
}

public static class IceRemembersRelationshipExtensions
{
    private static readonly string[] DomainNames =
    [
        "controlled_by",
        "home_base",
        "allied_with",
        "rival_of",
        "led_by",
        "member_of",
        "holds",
        "created_by",
        "memorializes",
        "claimed_by",
        "trade_route",
        "founded_from",
        "native_to",
        "anchored",
        "conduit_of",
        "part_of",
        "ward_against",
        "corrupted_by",
        "cleansed_by",
        "at_war_with",
        "betrayed_by",
    ];

    public static string ToDomainName(this IceRemembersRelationship relationship)
    {
        var index = (int)relationship;
        return (uint)index < (uint)DomainNames.Length
            ? DomainNames[index]
            : throw new ArgumentOutOfRangeException(nameof(relationship), relationship, "Unknown test-domain relationship");
    }
}

public enum IceRemembersPressure
{
    MilitaryTension,
    EconomicStress,
    CulturalFriction,
    DemographicPressure,
}

public static class IceRemembersPressureExtensions
{
    public static string ToDomainName(this IceRemembersPressure pressure) => pressure switch
    {
        IceRemembersPressure.MilitaryTension => "military_tension",
        IceRemembersPressure.EconomicStress => "economic_stress",
        IceRemembersPressure.CulturalFriction => "cultural_friction",
        IceRemembersPressure.DemographicPressure => "demographic_pressure",
        _ => throw new ArgumentOutOfRangeException(nameof(pressure), pressure, "Unknown test-domain pressure"),
    };
}

public enum IceRemembersStructuralFact
{
    Roam,
    SettlementContribution,
    TerritorialOrder,
}

public static class IceRemembersStructuralFactExtensions
{
    public static string ToDomainName(this IceRemembersStructuralFact fact) => fact switch
    {
        IceRemembersStructuralFact.Roam => "roam",
        IceRemembersStructuralFact.SettlementContribution => "settlement_contribution",
        IceRemembersStructuralFact.TerritorialOrder => "territorial_order",
        _ => throw new ArgumentOutOfRangeException(nameof(fact), fact, "Unknown test-domain structural fact"),
    };
}

public enum IceRemembersCulture
{
    AuroraStack,
    Nightshelf,
    WakeSingers,
}

public static class IceRemembersCultureExtensions
{
    public static string ToDomainName(this IceRemembersCulture culture) => culture switch
    {
        IceRemembersCulture.AuroraStack => "aurora_stack",
        IceRemembersCulture.Nightshelf => "nightshelf",
        IceRemembersCulture.WakeSingers => "wake_singers",
        _ => throw new ArgumentOutOfRangeException(nameof(culture), culture, "Unknown test-domain culture"),
    };
}

public enum IceRemembersVerbId
{
    ArtifactChannel,
    ArtifactCirculate,
    ArtifactCleanse,
    ArtifactCompel,
    ArtifactCorrupt,
    ArtifactDevote,
    ArtifactEmpower,
    ArtifactRepair,
    ArtifactEnshrine,
    ArtifactBind,
    ArtifactHeal,
    ArtifactPurify,
    ArtifactVivify,
    EngineDecay,
    FactionAttract,
    FactionConsecrate,
    FactionDistribute,
    FactionPreach,
    FactionExtort,
    FactionIndoctrinate,
    FactionMaintain,
    FactionPatrol,
    FactionQuarrel,
    FactionAgitate,
    FactionRally,
    FactionRecruit,
    FactionStarve,
    FactionOrganize,
    PenguinCreateWorks,
    PenguinWarm,
    PenguinEndure,
    PenguinForage,
    PenguinGuard,
    PenguinWitness,
    PenguinTravel,
    PenguinWander,
    PenguinWield,
    RegionAttune,
    RegionCleanse,
    RegionCommune,
    RegionDream,
    RegionFormMilitia,
    RegionFortify,
    RegionIrrigate,
    RegionBreach,
    RegionReclaim,
    RegionRoot,
    RegionShelter,
    SettlementAttract,
    SettlementDeliberate,
    SettlementClaimsEcho,
    SettlementGarrison,
    SettlementGossip,
    SettlementGrow,
    SettlementStripRookeries,
    SettlementOverreach,
    SettlementPropaganda,
    SettlementSanctify,
    SettlementTrade,
    PenguinExplore,
    PenguinVisitRookery,
    PenguinVisitRival,
    PenguinGoFishing,
    PenguinFollowArtifact,
    ArtifactSeekBearer,
    ArtifactReturnToShrine,
    ArtifactDrawToScar,
    ArtifactSinkSeaward,
    SettlementCastLamplight,
    SettlementBankTheHearth,
    SettlementDrownOutClaim,
    SettlementBuildTheRookery,
    FactionSendEnvoys,
    FactionHoldCouncil,
    FactionBreakRivalHold,
    FactionMoveSeat,
}

public static class IceRemembersVerbIdExtensions
{
    private static readonly string[] DomainNames =
    [
        "artifact_channel",
        "artifact_circulate",
        "artifact_cleanse",
        "artifact_compel",
        "artifact_corrupt",
        "artifact_devote",
        "artifact_empower",
        "artifact_repair",
        "artifact_enshrine",
        "artifact_bind",
        "artifact_heal",
        "artifact_purify",
        "artifact_vivify",
        "engine_decay",
        "faction_attract",
        "faction_consecrate",
        "faction_distribute",
        "faction_preach",
        "faction_extort",
        "faction_indoctrinate",
        "faction_maintain",
        "faction_patrol",
        "faction_quarrel",
        "faction_agitate",
        "faction_rally",
        "faction_recruit",
        "faction_starve",
        "faction_organize",
        "penguin_create_works",
        "penguin_warm",
        "penguin_endure",
        "penguin_forage",
        "penguin_guard",
        "penguin_witness",
        "penguin_travel",
        "penguin_wander",
        "penguin_wield",
        "region_attune",
        "region_cleanse",
        "region_commune",
        "region_dream",
        "region_form_militia",
        "region_fortify",
        "region_irrigate",
        "region_breach",
        "region_reclaim",
        "region_root",
        "region_shelter",
        "settlement_attract",
        "settlement_deliberate",
        "settlement_claims_echo",
        "settlement_garrison",
        "settlement_gossip",
        "settlement_grow",
        "settlement_strip_rookeries",
        "settlement_overreach",
        "settlement_propaganda",
        "settlement_sanctify",
        "settlement_trade",
        "penguin_explore",
        "penguin_visit_rookery",
        "penguin_visit_rival",
        "penguin_go_fishing",
        "penguin_follow_artifact",
        "artifact_seek_bearer",
        "artifact_return_to_shrine",
        "artifact_draw_to_scar",
        "artifact_sink_seaward",
        "settlement_cast_lamplight",
        "settlement_bank_the_hearth",
        "settlement_drown_out_claim",
        "settlement_build_the_rookery",
        "faction_send_envoys",
        "faction_hold_council",
        "faction_break_rival_hold",
        "faction_move_seat",
    ];

    public static string ToVerbId(this IceRemembersVerbId verbId)
    {
        var index = (int)verbId;
        return (uint)index < (uint)DomainNames.Length
            ? DomainNames[index]
            : throw new ArgumentOutOfRangeException(nameof(verbId), verbId, "Unknown test-domain verb id");
    }
}

public enum IceRemembersGambitId
{
    PenguinWieldsArtifact,
    FactionDeclaresWar,
    FactionBetraysOath,
    PenguinSlaysRival,
}

public static class IceRemembersGambitIdExtensions
{
    private static readonly string[] DomainNames =
    [
        "penguin_wields_artifact",
        "faction_declares_war",
        "faction_betrays_oath",
        "penguin_slays_rival",
    ];

    public static string ToGambitId(this IceRemembersGambitId gambitId)
    {
        var index = (int)gambitId;
        return (uint)index < (uint)DomainNames.Length
            ? DomainNames[index]
            : throw new ArgumentOutOfRangeException(nameof(gambitId), gambitId, "Unknown Ice Remembers gambit id");
    }
}
