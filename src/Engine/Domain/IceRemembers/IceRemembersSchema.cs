namespace Engine.Domain.IceRemembers;

using Engine.Resources;

public enum IceRemembersAttributeId
{
    Residents,
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
    public static string ToDomainName(this IceRemembersAttributeId attribute) => attribute switch
    {
        IceRemembersAttributeId.Residents => "residents",
        IceRemembersAttributeId.Dominion => "dominion",
        IceRemembersAttributeId.Shelter => "shelter",
        IceRemembersAttributeId.Cohesion => "cohesion",
        IceRemembersAttributeId.Reach => "reach",
        IceRemembersAttributeId.Provisions => "provisions",
        IceRemembersAttributeId.Vitality => "vitality",
        IceRemembersAttributeId.Echo => "echo",
        IceRemembersAttributeId.Warmth => "warmth",
        IceRemembersAttributeId.Stability => "stability",
        IceRemembersAttributeId.Holding => "holding",
        IceRemembersAttributeId.Pristiness => "pristiness",
        IceRemembersAttributeId.Condition => "condition",
        IceRemembersAttributeId.Power => "power",
        IceRemembersAttributeId.Resonance => "resonance",
        _ => throw new ArgumentOutOfRangeException(nameof(attribute), attribute, "Unknown test-domain attribute"),
    };
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
    public static string ToDomainName(this IceRemembersRelationship relationship) => relationship switch
    {
        IceRemembersRelationship.ControlledBy => "controlled_by",
        IceRemembersRelationship.HomeBase => "home_base",
        IceRemembersRelationship.AlliedWith => "allied_with",
        IceRemembersRelationship.RivalOf => "rival_of",
        IceRemembersRelationship.LedBy => "led_by",
        IceRemembersRelationship.MemberOf => "member_of",
        IceRemembersRelationship.Holds => "holds",
        IceRemembersRelationship.CreatedBy => "created_by",
        IceRemembersRelationship.Memorializes => "memorializes",
        IceRemembersRelationship.ClaimedBy => "claimed_by",
        IceRemembersRelationship.TradeRoute => "trade_route",
        IceRemembersRelationship.FoundedFrom => "founded_from",
        IceRemembersRelationship.NativeTo => "native_to",
        IceRemembersRelationship.Anchored => "anchored",
        IceRemembersRelationship.ConduitOf => "conduit_of",
        IceRemembersRelationship.PartOf => "part_of",
        IceRemembersRelationship.WardAgainst => "ward_against",
        IceRemembersRelationship.CorruptedBy => "corrupted_by",
        IceRemembersRelationship.CleansedBy => "cleansed_by",
        IceRemembersRelationship.AtWarWith => "at_war_with",
        IceRemembersRelationship.BetrayedBy => "betrayed_by",
        _ => throw new ArgumentOutOfRangeException(nameof(relationship), relationship, "Unknown test-domain relationship"),
    };
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
}

public static class IceRemembersVerbIdExtensions
{
    public static string ToVerbId(this IceRemembersVerbId verbId) => verbId switch
    {
        IceRemembersVerbId.ArtifactChannel => "artifact_channel",
        IceRemembersVerbId.ArtifactCirculate => "artifact_circulate",
        IceRemembersVerbId.ArtifactCleanse => "artifact_cleanse",
        IceRemembersVerbId.ArtifactCompel => "artifact_compel",
        IceRemembersVerbId.ArtifactCorrupt => "artifact_corrupt",
        IceRemembersVerbId.ArtifactDevote => "artifact_devote",
        IceRemembersVerbId.ArtifactEmpower => "artifact_empower",
        IceRemembersVerbId.ArtifactRepair => "artifact_repair",
        IceRemembersVerbId.ArtifactEnshrine => "artifact_enshrine",
        IceRemembersVerbId.ArtifactBind => "artifact_bind",
        IceRemembersVerbId.ArtifactHeal => "artifact_heal",
        IceRemembersVerbId.ArtifactPurify => "artifact_purify",
        IceRemembersVerbId.ArtifactVivify => "artifact_vivify",
        IceRemembersVerbId.EngineDecay => "engine_decay",
        IceRemembersVerbId.FactionAttract => "faction_attract",
        IceRemembersVerbId.FactionConsecrate => "faction_consecrate",
        IceRemembersVerbId.FactionDistribute => "faction_distribute",
        IceRemembersVerbId.FactionPreach => "faction_preach",
        IceRemembersVerbId.FactionExtort => "faction_extort",
        IceRemembersVerbId.FactionIndoctrinate => "faction_indoctrinate",
        IceRemembersVerbId.FactionMaintain => "faction_maintain",
        IceRemembersVerbId.FactionPatrol => "faction_patrol",
        IceRemembersVerbId.FactionQuarrel => "faction_quarrel",
        IceRemembersVerbId.FactionAgitate => "faction_agitate",
        IceRemembersVerbId.FactionRally => "faction_rally",
        IceRemembersVerbId.FactionRecruit => "faction_recruit",
        IceRemembersVerbId.FactionStarve => "faction_starve",
        IceRemembersVerbId.FactionOrganize => "faction_organize",
        IceRemembersVerbId.PenguinCreateWorks => "penguin_create_works",
        IceRemembersVerbId.PenguinWarm => "penguin_warm",
        IceRemembersVerbId.PenguinEndure => "penguin_endure",
        IceRemembersVerbId.PenguinForage => "penguin_forage",
        IceRemembersVerbId.PenguinGuard => "penguin_guard",
        IceRemembersVerbId.PenguinWitness => "penguin_witness",
        IceRemembersVerbId.PenguinTravel => "penguin_travel",
        IceRemembersVerbId.PenguinWander => "penguin_wander",
        IceRemembersVerbId.PenguinWield => "penguin_wield",
        IceRemembersVerbId.RegionAttune => "region_attune",
        IceRemembersVerbId.RegionCleanse => "region_cleanse",
        IceRemembersVerbId.RegionCommune => "region_commune",
        IceRemembersVerbId.RegionDream => "region_dream",
        IceRemembersVerbId.RegionFormMilitia => "region_form_militia",
        IceRemembersVerbId.RegionFortify => "region_fortify",
        IceRemembersVerbId.RegionIrrigate => "region_irrigate",
        IceRemembersVerbId.RegionBreach => "region_breach",
        IceRemembersVerbId.RegionReclaim => "region_reclaim",
        IceRemembersVerbId.RegionRoot => "region_root",
        IceRemembersVerbId.RegionShelter => "region_shelter",
        IceRemembersVerbId.SettlementAttract => "settlement_attract",
        IceRemembersVerbId.SettlementDeliberate => "settlement_deliberate",
        IceRemembersVerbId.SettlementClaimsEcho => "settlement_claims_echo",
        IceRemembersVerbId.SettlementGarrison => "settlement_garrison",
        IceRemembersVerbId.SettlementGossip => "settlement_gossip",
        IceRemembersVerbId.SettlementGrow => "settlement_grow",
        IceRemembersVerbId.SettlementStripRookeries => "settlement_strip_rookeries",
        IceRemembersVerbId.SettlementOverreach => "settlement_overreach",
        IceRemembersVerbId.SettlementPropaganda => "settlement_propaganda",
        IceRemembersVerbId.SettlementSanctify => "settlement_sanctify",
        IceRemembersVerbId.SettlementTrade => "settlement_trade",
        _ => throw new ArgumentOutOfRangeException(nameof(verbId), verbId, "Unknown test-domain verb id"),
    };
}
