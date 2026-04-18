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
    Containment,
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
        TestDomainAttributeId.Containment => "containment",
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

public enum TestDomainProcessId
{
    SettlementIrrigate,
    SettlementGrow,
    SettlementPropaganda,
    SettlementAttract,
    NpcEat,
    NpcMoveToTarget,
    NpcWander,
    NpcCreateWorks,
    FactionPatrol,
    RegionFormMilitia,
}

public static class TestDomainProcessIdExtensions
{
    public static string ToProcessId(this TestDomainProcessId processId) => processId switch
    {
        TestDomainProcessId.SettlementIrrigate => "settlement_irrigate",
        TestDomainProcessId.SettlementGrow => "settlement_grow",
        TestDomainProcessId.SettlementPropaganda => "settlement_propaganda",
        TestDomainProcessId.SettlementAttract => "settlement_attract",
        TestDomainProcessId.NpcEat => "npc_eat",
        TestDomainProcessId.NpcMoveToTarget => "npc_move_to_target",
        TestDomainProcessId.NpcWander => "npc_wander",
        TestDomainProcessId.NpcCreateWorks => "npc_create_works",
        TestDomainProcessId.FactionPatrol => "faction_patrol",
        TestDomainProcessId.RegionFormMilitia => "region_form_militia",
        _ => throw new ArgumentOutOfRangeException(nameof(processId), processId, "Unknown test-domain process id"),
    };
}

public enum TestDomainVerbId
{
    ArtifactChannel,
    ArtifactCleanse,
    ArtifactDevote,
    ArtifactEmanate,
    ArtifactEmpower,
    ArtifactEnshrine,
    ArtifactHeal,
    ArtifactPurify,
    EngineDecay,
    FactionAttract,
    FactionConsecrate,
    FactionIndoctrinate,
    FactionMaintain,
    FactionPatrol,
    FactionRadicalize,
    FactionRally,
    FactionRecruit,
    FactionTax,
    NpcCreateWorks,
    NpcEat,
    NpcEndure,
    NpcGuard,
    RegionAttune,
    RegionCleanse,
    RegionFormMilitia,
    RegionIrrigate,
    RegionSettle,
    RegionShelter,
    SettlementAttract,
    SettlementGarrison,
    SettlementGrow,
    SettlementMobilize,
    SettlementPropaganda,
    SettlementTrade,
}

public static class TestDomainVerbIdExtensions
{
    public static string ToVerbId(this TestDomainVerbId verbId) => verbId switch
    {
        TestDomainVerbId.ArtifactChannel => "artifact_channel",
        TestDomainVerbId.ArtifactCleanse => "artifact_cleanse",
        TestDomainVerbId.ArtifactDevote => "artifact_devote",
        TestDomainVerbId.ArtifactEmanate => "artifact_emanate",
        TestDomainVerbId.ArtifactEmpower => "artifact_empower",
        TestDomainVerbId.ArtifactEnshrine => "artifact_enshrine",
        TestDomainVerbId.ArtifactHeal => "artifact_heal",
        TestDomainVerbId.ArtifactPurify => "artifact_purify",
        TestDomainVerbId.EngineDecay => "engine_decay",
        TestDomainVerbId.FactionAttract => "faction_attract",
        TestDomainVerbId.FactionConsecrate => "faction_consecrate",
        TestDomainVerbId.FactionIndoctrinate => "faction_indoctrinate",
        TestDomainVerbId.FactionMaintain => "faction_maintain",
        TestDomainVerbId.FactionPatrol => "faction_patrol",
        TestDomainVerbId.FactionRadicalize => "faction_radicalize",
        TestDomainVerbId.FactionRally => "faction_rally",
        TestDomainVerbId.FactionRecruit => "faction_recruit",
        TestDomainVerbId.FactionTax => "faction_tax",
        TestDomainVerbId.NpcCreateWorks => "npc_create_works",
        TestDomainVerbId.NpcEat => "npc_eat",
        TestDomainVerbId.NpcEndure => "npc_endure",
        TestDomainVerbId.NpcGuard => "npc_guard",
        TestDomainVerbId.RegionAttune => "region_attune",
        TestDomainVerbId.RegionCleanse => "region_cleanse",
        TestDomainVerbId.RegionFormMilitia => "region_form_militia",
        TestDomainVerbId.RegionIrrigate => "region_irrigate",
        TestDomainVerbId.RegionSettle => "region_settle",
        TestDomainVerbId.RegionShelter => "region_shelter",
        TestDomainVerbId.SettlementAttract => "settlement_attract",
        TestDomainVerbId.SettlementGarrison => "settlement_garrison",
        TestDomainVerbId.SettlementGrow => "settlement_grow",
        TestDomainVerbId.SettlementMobilize => "settlement_mobilize",
        TestDomainVerbId.SettlementPropaganda => "settlement_propaganda",
        TestDomainVerbId.SettlementTrade => "settlement_trade",
        _ => throw new ArgumentOutOfRangeException(nameof(verbId), verbId, "Unknown test-domain verb id"),
    };
}
