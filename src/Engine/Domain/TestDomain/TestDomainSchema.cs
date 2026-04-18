namespace Engine.Domain.TestDomain;

using Engine.Resources;

public enum TestDomainAttributeId
{
    Residents,
    Dominion,
    Sanitation,
    Cohesion,
    Influence,
    Provisions,
    Vitality,
    Motivation,
    Satiety,
    GeologicStability,
    RegionalSafety,
    Pristineness,
    Condition,
    Power,
    Aura,
}

public static class TestDomainAttributeIdExtensions
{
    public static string ToDomainName(this TestDomainAttributeId attribute) => attribute switch
    {
        TestDomainAttributeId.Residents => "residents",
        TestDomainAttributeId.Dominion => "dominion",
        TestDomainAttributeId.Sanitation => "sanitation",
        TestDomainAttributeId.Cohesion => "cohesion",
        TestDomainAttributeId.Influence => "influence",
        TestDomainAttributeId.Provisions => "provisions",
        TestDomainAttributeId.Vitality => "vitality",
        TestDomainAttributeId.Motivation => "motivation",
        TestDomainAttributeId.Satiety => "satiety",
        TestDomainAttributeId.GeologicStability => "geologic_stability",
        TestDomainAttributeId.RegionalSafety => "regional_safety",
        TestDomainAttributeId.Pristineness => "pristineness",
        TestDomainAttributeId.Condition => "condition",
        TestDomainAttributeId.Power => "power",
        TestDomainAttributeId.Aura => "aura",
        _ => throw new ArgumentOutOfRangeException(nameof(attribute), attribute, "Unknown test-domain attribute"),
    };
}

public enum TestDomainResource
{
    Food,
    Wealth,
    Population,
}

public static class TestDomainResourceExtensions
{
    public static string ToDomainName(this TestDomainResource resource) => resource switch
    {
        TestDomainResource.Food => "food",
        TestDomainResource.Wealth => "wealth",
        TestDomainResource.Population => "population",
        _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, "Unknown test-domain resource"),
    };

    public static ResourceType ToResourceType(this TestDomainResource resource) => resource switch
    {
        TestDomainResource.Food => ResourceType.Food,
        TestDomainResource.Wealth => ResourceType.Wealth,
        TestDomainResource.Population => ResourceType.Population,
        _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, "Unknown test-domain resource"),
    };
}

public enum TestDomainRelationship
{
    Borders,
    Contains,
    ControlledBy,
    LocatedAt,
    HomeBase,
    AlliedWith,
    RivalOf,
    LedBy,
    MemberOf,
    LoyalTo,
    Holds,
    CreatedBy,
    StoredAt,
    InvolvedIn,
}

public static class TestDomainRelationshipExtensions
{
    public static string ToDomainName(this TestDomainRelationship relationship) => relationship switch
    {
        TestDomainRelationship.Borders => "borders",
        TestDomainRelationship.Contains => "contains",
        TestDomainRelationship.ControlledBy => "controlled_by",
        TestDomainRelationship.LocatedAt => "located_at",
        TestDomainRelationship.HomeBase => "home_base",
        TestDomainRelationship.AlliedWith => "allied_with",
        TestDomainRelationship.RivalOf => "rival_of",
        TestDomainRelationship.LedBy => "led_by",
        TestDomainRelationship.MemberOf => "member_of",
        TestDomainRelationship.LoyalTo => "loyal_to",
        TestDomainRelationship.Holds => "holds",
        TestDomainRelationship.CreatedBy => "created_by",
        TestDomainRelationship.StoredAt => "stored_at",
        TestDomainRelationship.InvolvedIn => "involved_in",
        _ => throw new ArgumentOutOfRangeException(nameof(relationship), relationship, "Unknown test-domain relationship"),
    };
}

public enum TestDomainPressure
{
    MilitaryTension,
    EconomicStress,
    CulturalFriction,
    DemographicPressure,
}

public static class TestDomainPressureExtensions
{
    public static string ToDomainName(this TestDomainPressure pressure) => pressure switch
    {
        TestDomainPressure.MilitaryTension => "military_tension",
        TestDomainPressure.EconomicStress => "economic_stress",
        TestDomainPressure.CulturalFriction => "cultural_friction",
        TestDomainPressure.DemographicPressure => "demographic_pressure",
        _ => throw new ArgumentOutOfRangeException(nameof(pressure), pressure, "Unknown test-domain pressure"),
    };
}

public enum TestDomainStructuralFact
{
    Roam,
    SettlementContribution,
    TerritorialOrder,
}

public static class TestDomainStructuralFactExtensions
{
    public static string ToDomainName(this TestDomainStructuralFact fact) => fact switch
    {
        TestDomainStructuralFact.Roam => "roam",
        TestDomainStructuralFact.SettlementContribution => "settlement_contribution",
        TestDomainStructuralFact.TerritorialOrder => "territorial_order",
        _ => throw new ArgumentOutOfRangeException(nameof(fact), fact, "Unknown test-domain structural fact"),
    };
}

public enum TestDomainCulture
{
    CultureA,
    CultureB,
}

public static class TestDomainCultureExtensions
{
    public static string ToDomainName(this TestDomainCulture culture) => culture switch
    {
        TestDomainCulture.CultureA => "culture_a",
        TestDomainCulture.CultureB => "culture_b",
        _ => throw new ArgumentOutOfRangeException(nameof(culture), culture, "Unknown test-domain culture"),
    };
}

public enum TestDomainVerbId
{
    ArtifactChannel,
    ArtifactCirculate,
    ArtifactCleanse,
    ArtifactCompel,
    ArtifactCorrupt,
    ArtifactDevote,
    ArtifactEmpower,
    ArtifactEndow,
    ArtifactEnshrine,
    ArtifactEnthrall,
    ArtifactHeal,
    ArtifactPurify,
    ArtifactVivify,
    EngineDecay,
    FactionAttract,
    FactionConsecrate,
    FactionDistribute,
    FactionEvangelize,
    FactionExtort,
    FactionIndoctrinate,
    FactionMaintain,
    FactionPatrol,
    FactionQuarrel,
    FactionRadicalize,
    FactionRally,
    FactionRecruit,
    FactionStarve,
    FactionTax,
    NpcCreateWorks,
    NpcEat,
    NpcEndure,
    NpcForage,
    NpcGuard,
    NpcInspire,
    NpcTravel,
    NpcWander,
    NpcWield,
    RegionAttune,
    RegionCleanse,
    RegionCommune,
    RegionDream,
    RegionFormMilitia,
    RegionGarrison,
    RegionIrrigate,
    RegionMine,
    RegionReclaim,
    RegionRoot,
    RegionShelter,
    SettlementAttract,
    SettlementDebate,
    SettlementEmploy,
    SettlementGarrison,
    SettlementGossip,
    SettlementGrow,
    SettlementIndustrialize,
    SettlementOverreach,
    SettlementPropaganda,
    SettlementSanctify,
    SettlementTrade,
}

public static class TestDomainVerbIdExtensions
{
    public static string ToVerbId(this TestDomainVerbId verbId) => verbId switch
    {
        TestDomainVerbId.ArtifactChannel => "artifact_channel",
        TestDomainVerbId.ArtifactCirculate => "artifact_circulate",
        TestDomainVerbId.ArtifactCleanse => "artifact_cleanse",
        TestDomainVerbId.ArtifactCompel => "artifact_compel",
        TestDomainVerbId.ArtifactCorrupt => "artifact_corrupt",
        TestDomainVerbId.ArtifactDevote => "artifact_devote",
        TestDomainVerbId.ArtifactEmpower => "artifact_empower",
        TestDomainVerbId.ArtifactEndow => "artifact_endow",
        TestDomainVerbId.ArtifactEnshrine => "artifact_enshrine",
        TestDomainVerbId.ArtifactEnthrall => "artifact_enthrall",
        TestDomainVerbId.ArtifactHeal => "artifact_heal",
        TestDomainVerbId.ArtifactPurify => "artifact_purify",
        TestDomainVerbId.ArtifactVivify => "artifact_vivify",
        TestDomainVerbId.EngineDecay => "engine_decay",
        TestDomainVerbId.FactionAttract => "faction_attract",
        TestDomainVerbId.FactionConsecrate => "faction_consecrate",
        TestDomainVerbId.FactionDistribute => "faction_distribute",
        TestDomainVerbId.FactionEvangelize => "faction_evangelize",
        TestDomainVerbId.FactionExtort => "faction_extort",
        TestDomainVerbId.FactionIndoctrinate => "faction_indoctrinate",
        TestDomainVerbId.FactionMaintain => "faction_maintain",
        TestDomainVerbId.FactionPatrol => "faction_patrol",
        TestDomainVerbId.FactionQuarrel => "faction_quarrel",
        TestDomainVerbId.FactionRadicalize => "faction_radicalize",
        TestDomainVerbId.FactionRally => "faction_rally",
        TestDomainVerbId.FactionRecruit => "faction_recruit",
        TestDomainVerbId.FactionStarve => "faction_starve",
        TestDomainVerbId.FactionTax => "faction_tax",
        TestDomainVerbId.NpcCreateWorks => "npc_create_works",
        TestDomainVerbId.NpcEat => "npc_eat",
        TestDomainVerbId.NpcEndure => "npc_endure",
        TestDomainVerbId.NpcForage => "npc_forage",
        TestDomainVerbId.NpcGuard => "npc_guard",
        TestDomainVerbId.NpcInspire => "npc_inspire",
        TestDomainVerbId.NpcTravel => "npc_travel",
        TestDomainVerbId.NpcWander => "npc_wander",
        TestDomainVerbId.NpcWield => "npc_wield",
        TestDomainVerbId.RegionAttune => "region_attune",
        TestDomainVerbId.RegionCleanse => "region_cleanse",
        TestDomainVerbId.RegionCommune => "region_commune",
        TestDomainVerbId.RegionDream => "region_dream",
        TestDomainVerbId.RegionFormMilitia => "region_form_militia",
        TestDomainVerbId.RegionGarrison => "region_garrison",
        TestDomainVerbId.RegionIrrigate => "region_irrigate",
        TestDomainVerbId.RegionMine => "region_mine",
        TestDomainVerbId.RegionReclaim => "region_reclaim",
        TestDomainVerbId.RegionRoot => "region_root",
        TestDomainVerbId.RegionShelter => "region_shelter",
        TestDomainVerbId.SettlementAttract => "settlement_attract",
        TestDomainVerbId.SettlementDebate => "settlement_debate",
        TestDomainVerbId.SettlementEmploy => "settlement_employ",
        TestDomainVerbId.SettlementGarrison => "settlement_garrison",
        TestDomainVerbId.SettlementGossip => "settlement_gossip",
        TestDomainVerbId.SettlementGrow => "settlement_grow",
        TestDomainVerbId.SettlementIndustrialize => "settlement_industrialize",
        TestDomainVerbId.SettlementOverreach => "settlement_overreach",
        TestDomainVerbId.SettlementPropaganda => "settlement_propaganda",
        TestDomainVerbId.SettlementSanctify => "settlement_sanctify",
        TestDomainVerbId.SettlementTrade => "settlement_trade",
        _ => throw new ArgumentOutOfRangeException(nameof(verbId), verbId, "Unknown test-domain verb id"),
    };
}
