namespace Engine.Domain.Startup;

using Engine.Constants;
using Engine.Gameplay;
using Engine.Graph;

/// <summary>
/// Stable-name startup data for a domain. This is not a kernel program and does
/// not contain primitive runtime IDs; Engine.Host resolves it through the compiled
/// domain catalog during bootstrap.
/// </summary>
public sealed record DomainStartupSeed(
    int MapRadius,
    IReadOnlyList<DomainSeedEntity> Entities,
    IReadOnlyList<DomainSeedCell> Cells,
    IReadOnlyList<DomainSeedRelationship> Relationships,
    IReadOnlyList<DomainMapSubdivision> MapSubdivisions,
    IReadOnlyList<DomainSeedAttributeValue> Attributes,
    IReadOnlyList<DomainKnowledgeSite> KnowledgeSites,
    DomainPhysicalGeographyBlueprint? PhysicalGeography = null,
    DomainSemanticTerrainBlueprint? SemanticTerrain = null)
{
    public static DomainStartupSeed Empty(int mapRadius) =>
        new(mapRadius, [], [], [], [], [], []);
}

/// <summary>
/// One authored technology placed at a hex of the semantic (idea) plane. A
/// culture comes to know the tech when its idea-mass reaches this hex. Tier is
/// the radius band (also derivable from the coordinate); it is carried for
/// convenient tier telemetry. Grants are authored gameplay configuration;
/// Engine.Host resolves their stable targets before installing kernel data.
/// </summary>
public sealed record DomainKnowledgeSite(
    string TechId,
    int Q,
    int R,
    int Tier,
    IReadOnlyList<KnowledgeGrantConfig> Grants);

public sealed record DomainSeedEntity(
    int SeedEntityId,
    string Kind,
    string Subtype,
    string Culture,
    int PhysicalQ,
    int PhysicalR,
    int SemanticQ,
    int SemanticR,
    int SemanticS,
    string? DisplayName = null,
    string? ReferenceName = null);

public sealed record DomainSeedCell(
    int Q,
    int R,
    string Terrain,
    int? OwnerSeedEntityId = null,
    int SuperregionId = 0,
    int ContinentId = 0,
    int PlaneCode = PlaneCodes.Physical);

public enum DomainMapSubdivisionLevel
{
    Continent,
    Superregion,
}

public sealed record DomainMapSubdivision(
    int Id,
    DomainMapSubdivisionLevel Level,
    string StableName,
    string DisplayName);

public sealed record DomainSeedAttributeValue(
    int SeedEntityId,
    string Attribute,
    float Value);

public sealed record DomainSeedRelationship(
    string RelationshipKind,
    RelationshipPolarity Polarity,
    int SourceSeedEntityId,
    int TargetSeedEntityId,
    float Strength);
