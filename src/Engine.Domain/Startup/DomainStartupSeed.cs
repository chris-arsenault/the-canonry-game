namespace Engine.Domain.Startup;

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
    IReadOnlyList<DomainSeedResource> Resources,
    IReadOnlyList<DomainSeedRelationship> Relationships,
    IReadOnlyList<DomainSeedAttributeValue> Attributes)
{
    public static DomainStartupSeed Empty(int mapRadius) =>
        new(mapRadius, [], [], [], [], []);
}

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
    string? DisplayName = null);

public sealed record DomainSeedCell(
    int Q,
    int R,
    string Terrain,
    int? OwnerSeedEntityId = null);

public sealed record DomainSeedResource(
    string Resource,
    int Q,
    int R,
    float Value);

public sealed record DomainSeedAttributeValue(
    int SeedEntityId,
    string Attribute,
    float Value);

public sealed record DomainSeedRelationship(
    string RelationshipKind,
    RelationshipPolarity Polarity,
    int SourceSeedEntityId,
    int TargetSeedEntityId,
    float Strength,
    IReadOnlyList<string>? Facts = null);
