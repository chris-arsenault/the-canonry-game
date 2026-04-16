namespace Engine.Domain.Graph;

using Engine.Kinds;

public enum DefinitionAttributeId
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

public enum DefinitionAttributeShape
{
    Scalar,
    Plane2D,
}

public enum DefinitionAttributeBucket
{
    Integrity,
    Need,
    Drive,
    Capacity,
    Projection,
    Alignment,
}

public enum DefinitionConservationClass
{
    Unspecified,
    Creatable,
    Destroyable,
    Transferable,
    Hybrid,
}

public enum DefinitionResourceId
{
    Food,
    Wealth,
    Population,
}

public enum DefinitionRelationshipId
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

public enum DefinitionPressureId
{
    MilitaryTension,
    EconomicStress,
    CulturalFriction,
    DemographicPressure,
}

public sealed record DefinitionAttributeMetadata(
    DefinitionAttributeId Id,
    string DomainName,
    DefinitionAttributeShape Shape,
    DefinitionAttributeBucket Bucket,
    IReadOnlySet<EntityKind> AllowedKinds);

public sealed record DefinitionResourceMetadata(
    DefinitionResourceId Id,
    string DomainName);

public sealed record DefinitionRelationshipMetadata(
    DefinitionRelationshipId Id,
    string DomainName);

public sealed record DefinitionPressureMetadata(
    DefinitionPressureId Id,
    string DomainName);

public static class DefinitionAttributeCatalog
{
    private static readonly Dictionary<DefinitionAttributeId, DefinitionAttributeMetadata> ById =
        new Dictionary<DefinitionAttributeId, DefinitionAttributeMetadata>
        {
            [DefinitionAttributeId.Residents] = new(
                DefinitionAttributeId.Residents,
                "residents",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Integrity,
                new HashSet<EntityKind> { EntityKind.Settlement }),
            [DefinitionAttributeId.Security] = new(
                DefinitionAttributeId.Security,
                "security",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Capacity,
                new HashSet<EntityKind> { EntityKind.Settlement }),
            [DefinitionAttributeId.Prestige] = new(
                DefinitionAttributeId.Prestige,
                "prestige",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Projection,
                new HashSet<EntityKind> { EntityKind.Settlement }),
            [DefinitionAttributeId.CivicIdeology] = new(
                DefinitionAttributeId.CivicIdeology,
                "civic_ideology",
                DefinitionAttributeShape.Plane2D,
                DefinitionAttributeBucket.Alignment,
                new HashSet<EntityKind> { EntityKind.Settlement, EntityKind.Faction, EntityKind.Npc }),
            [DefinitionAttributeId.Cohesion] = new(
                DefinitionAttributeId.Cohesion,
                "cohesion",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Integrity,
                new HashSet<EntityKind> { EntityKind.Faction }),
            [DefinitionAttributeId.Support] = new(
                DefinitionAttributeId.Support,
                "support",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Capacity,
                new HashSet<EntityKind> { EntityKind.Faction }),
            [DefinitionAttributeId.Influence] = new(
                DefinitionAttributeId.Influence,
                "influence",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Projection,
                new HashSet<EntityKind> { EntityKind.Faction }),
            [DefinitionAttributeId.Vitality] = new(
                DefinitionAttributeId.Vitality,
                "vitality",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Integrity,
                new HashSet<EntityKind> { EntityKind.Npc }),
            [DefinitionAttributeId.Satiety] = new(
                DefinitionAttributeId.Satiety,
                "satiety",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Need,
                new HashSet<EntityKind> { EntityKind.Npc }),
            [DefinitionAttributeId.Ambition] = new(
                DefinitionAttributeId.Ambition,
                "ambition",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Drive,
                new HashSet<EntityKind> { EntityKind.Npc }),
            [DefinitionAttributeId.RegionalSafety] = new(
                DefinitionAttributeId.RegionalSafety,
                "regional_safety",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Projection,
                new HashSet<EntityKind> { EntityKind.Region }),
            [DefinitionAttributeId.GeologicStability] = new(
                DefinitionAttributeId.GeologicStability,
                "geologic_stability",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Integrity,
                new HashSet<EntityKind> { EntityKind.Region }),
            [DefinitionAttributeId.MysticResonance] = new(
                DefinitionAttributeId.MysticResonance,
                "mystic_resonance",
                DefinitionAttributeShape.Plane2D,
                DefinitionAttributeBucket.Alignment,
                new HashSet<EntityKind> { EntityKind.Region, EntityKind.Artifact }),
            [DefinitionAttributeId.Condition] = new(
                DefinitionAttributeId.Condition,
                "condition",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Integrity,
                new HashSet<EntityKind> { EntityKind.Artifact }),
            [DefinitionAttributeId.Power] = new(
                DefinitionAttributeId.Power,
                "power",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Capacity,
                new HashSet<EntityKind> { EntityKind.Artifact }),
            [DefinitionAttributeId.Containment] = new(
                DefinitionAttributeId.Containment,
                "containment",
                DefinitionAttributeShape.Scalar,
                DefinitionAttributeBucket.Drive,
                new HashSet<EntityKind> { EntityKind.Artifact }),
        };

    private static readonly Dictionary<string, DefinitionAttributeId> ByDomainName =
        ById.Values.ToDictionary(metadata => metadata.DomainName, metadata => metadata.Id, StringComparer.Ordinal);

    public static IReadOnlyCollection<DefinitionAttributeMetadata> All => ById.Values.ToArray();

    public static bool TryParse(string domainName, out DefinitionAttributeId id) =>
        ByDomainName.TryGetValue(domainName, out id);

    public static DefinitionAttributeMetadata Get(DefinitionAttributeId id) => ById[id];

    public static string GetDomainName(DefinitionAttributeId id) => ById[id].DomainName;
}

public static class DefinitionResourceCatalog
{
    private static readonly Dictionary<DefinitionResourceId, DefinitionResourceMetadata> ById =
        new Dictionary<DefinitionResourceId, DefinitionResourceMetadata>
        {
            [DefinitionResourceId.Food] = new(DefinitionResourceId.Food, "food"),
            [DefinitionResourceId.Wealth] = new(DefinitionResourceId.Wealth, "wealth"),
            [DefinitionResourceId.Population] = new(DefinitionResourceId.Population, "population"),
        };

    private static readonly Dictionary<string, DefinitionResourceId> ByDomainName =
        ById.Values.ToDictionary(metadata => metadata.DomainName, metadata => metadata.Id, StringComparer.Ordinal);

    public static IReadOnlyCollection<DefinitionResourceMetadata> All => ById.Values.ToArray();

    public static bool TryParse(string domainName, out DefinitionResourceId id) =>
        ByDomainName.TryGetValue(domainName, out id);

    public static DefinitionResourceMetadata Get(DefinitionResourceId id) => ById[id];
}

public static class DefinitionRelationshipCatalog
{
    private static readonly Dictionary<DefinitionRelationshipId, DefinitionRelationshipMetadata> ById =
        new Dictionary<DefinitionRelationshipId, DefinitionRelationshipMetadata>
        {
            [DefinitionRelationshipId.Borders] = new(DefinitionRelationshipId.Borders, "borders"),
            [DefinitionRelationshipId.Contains] = new(DefinitionRelationshipId.Contains, "contains"),
            [DefinitionRelationshipId.ControlledBy] = new(DefinitionRelationshipId.ControlledBy, "controlled_by"),
            [DefinitionRelationshipId.LocatedAt] = new(DefinitionRelationshipId.LocatedAt, "located_at"),
            [DefinitionRelationshipId.HomeBase] = new(DefinitionRelationshipId.HomeBase, "home_base"),
            [DefinitionRelationshipId.AlliedWith] = new(DefinitionRelationshipId.AlliedWith, "allied_with"),
            [DefinitionRelationshipId.RivalOf] = new(DefinitionRelationshipId.RivalOf, "rival_of"),
            [DefinitionRelationshipId.LedBy] = new(DefinitionRelationshipId.LedBy, "led_by"),
            [DefinitionRelationshipId.MemberOf] = new(DefinitionRelationshipId.MemberOf, "member_of"),
            [DefinitionRelationshipId.LoyalTo] = new(DefinitionRelationshipId.LoyalTo, "loyal_to"),
            [DefinitionRelationshipId.Holds] = new(DefinitionRelationshipId.Holds, "holds"),
            [DefinitionRelationshipId.CreatedBy] = new(DefinitionRelationshipId.CreatedBy, "created_by"),
            [DefinitionRelationshipId.StoredAt] = new(DefinitionRelationshipId.StoredAt, "stored_at"),
            [DefinitionRelationshipId.InvolvedIn] = new(DefinitionRelationshipId.InvolvedIn, "involved_in"),
        };

    private static readonly Dictionary<string, DefinitionRelationshipId> ByDomainName =
        ById.Values.ToDictionary(metadata => metadata.DomainName, metadata => metadata.Id, StringComparer.Ordinal);

    public static IReadOnlyCollection<DefinitionRelationshipMetadata> All => ById.Values.ToArray();

    public static bool TryParse(string domainName, out DefinitionRelationshipId id) =>
        ByDomainName.TryGetValue(domainName, out id);

    public static DefinitionRelationshipMetadata Get(DefinitionRelationshipId id) => ById[id];
}

public static class DefinitionPressureCatalog
{
    private static readonly Dictionary<DefinitionPressureId, DefinitionPressureMetadata> ById =
        new Dictionary<DefinitionPressureId, DefinitionPressureMetadata>
        {
            [DefinitionPressureId.MilitaryTension] = new(DefinitionPressureId.MilitaryTension, "military_tension"),
            [DefinitionPressureId.EconomicStress] = new(DefinitionPressureId.EconomicStress, "economic_stress"),
            [DefinitionPressureId.CulturalFriction] = new(DefinitionPressureId.CulturalFriction, "cultural_friction"),
            [DefinitionPressureId.DemographicPressure] = new(DefinitionPressureId.DemographicPressure, "demographic_pressure"),
        };

    private static readonly Dictionary<string, DefinitionPressureId> ByDomainName =
        ById.Values.ToDictionary(metadata => metadata.DomainName, metadata => metadata.Id, StringComparer.Ordinal);

    public static IReadOnlyCollection<DefinitionPressureMetadata> All => ById.Values.ToArray();

    public static bool TryParse(string domainName, out DefinitionPressureId id) =>
        ByDomainName.TryGetValue(domainName, out id);

    public static DefinitionPressureMetadata Get(DefinitionPressureId id) => ById[id];
}
