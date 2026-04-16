namespace Engine.Domain.TestDomain;

using Engine.Resources;

public enum TestDomainAttributeId
{
    Residents,
    Security,
    Prestige,
    CivicIdeology,
    Cohesion,
    Support,
    Influence,
    Vitality,
    Satiety,
    Ambition,
    RegionalSafety,
    GeologicStability,
    MysticResonance,
    Condition,
    Power,
    Containment,
}

public static class TestDomainAttributeIdExtensions
{
    public static string ToDomainName(this TestDomainAttributeId attribute) => attribute switch
    {
        TestDomainAttributeId.Residents => "residents",
        TestDomainAttributeId.Security => "security",
        TestDomainAttributeId.Prestige => "prestige",
        TestDomainAttributeId.CivicIdeology => "civic_ideology",
        TestDomainAttributeId.Cohesion => "cohesion",
        TestDomainAttributeId.Support => "support",
        TestDomainAttributeId.Influence => "influence",
        TestDomainAttributeId.Vitality => "vitality",
        TestDomainAttributeId.Satiety => "satiety",
        TestDomainAttributeId.Ambition => "ambition",
        TestDomainAttributeId.RegionalSafety => "regional_safety",
        TestDomainAttributeId.GeologicStability => "geologic_stability",
        TestDomainAttributeId.MysticResonance => "mystic_resonance",
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
