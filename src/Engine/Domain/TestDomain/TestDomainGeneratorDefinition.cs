namespace Engine.Domain.TestDomain;

using Engine.Domain.Config;
using Engine.Kinds;
using Attr = Engine.Domain.TestDomain.TestDomainAttributeId;
using Rel = Engine.Domain.TestDomain.TestDomainRelationship;

public static class TestDomainGeneratorDefinition
{
    public static IReadOnlyList<SemanticGeneratorConfig> Create() =>
    [
        new()
        {
            Id = "colony_founding",
            Name = "Colony Founding",
            Description = "Found a new village package in a still-pristine region.",
            Target = new SemanticGeneratorTarget
            {
                Kind = EntityKind.Region,
                AllowWilderness = true,
                SaturationLimits =
                [
                    new SemanticSaturationLimit
                    {
                        RelationshipKind = L(Rel.Contains),
                        Direction = "out",
                        MaxCount = 3,
                    },
                ],
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$settlement",
                    Kind = EntityKind.Settlement,
                    Subtype = "village",
                    Role = "primary",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$leader",
                    Kind = EntityKind.Npc,
                    Subtype = "warrior",
                    Role = "leader",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$faction",
                    Kind = EntityKind.Faction,
                    Subtype = "generic_faction",
                    Role = "founding_faction",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$leader",
                    DestinationRef = "$settlement",
                    Kind = L(Rel.LocatedAt),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$settlement",
                    DestinationRef = "$faction",
                    Kind = L(Rel.ControlledBy),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$leader",
                    DestinationRef = "$faction",
                    Kind = L(Rel.LoyalTo),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$target",
                    DestinationRef = "$settlement",
                    Kind = L(Rel.Contains),
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = EntityKind.Region,
                    AttributeName = A(Attr.Pristineness),
                    Direction = TriggerDirection.High,
                },
            ],
            PulseWeight = 2,
        },
        new()
        {
            Id = "hero_emergence",
            Name = "Hero Emergence",
            Description = "A strained settlement produces a hero, sometimes with a weapon.",
            Target = new SemanticGeneratorTarget
            {
                Kind = EntityKind.Settlement,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = EntityKind.Npc,
                    Subtype = "hero",
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$weapon",
                    Kind = EntityKind.Artifact,
                    Subtype = "weapon",
                    Role = "hero_weapon",
                    CreateChance = 0.25f,
                    InheritCultureFrom = "$hero",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$target",
                    Kind = L(Rel.LocatedAt),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$weapon",
                    DestinationRef = "$hero",
                    Kind = L(Rel.StoredAt),
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = EntityKind.Settlement,
                    AttributeName = A(Attr.Sanitation),
                    Direction = TriggerDirection.Low,
                },
            ],
            PulseWeight = 3,
        },
        new()
        {
            Id = "faction_splinter",
            Name = "Faction Splinter",
            Description = "A cohesive faction throws off a rival splinter and its leader.",
            Target = new SemanticGeneratorTarget
            {
                Kind = EntityKind.Faction,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$new_faction",
                    Kind = EntityKind.Faction,
                    Subtype = "generic_faction",
                    Role = "splinter",
                    InheritCultureFrom = "$target",
                },
                new SemanticProducedEntity
                {
                    Ref = "$schism_leader",
                    Kind = EntityKind.Npc,
                    Subtype = "warrior",
                    Role = "leader",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships =
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$schism_leader",
                    DestinationRef = "$new_faction",
                    Kind = L(Rel.MemberOf),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$new_faction",
                    DestinationRef = "$target",
                    Kind = L(Rel.RivalOf),
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$schism_leader",
                    DestinationRef = "$target",
                    Kind = L(Rel.RivalOf),
                },
            ],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = EntityKind.Faction,
                    AttributeName = A(Attr.Cohesion),
                    Direction = TriggerDirection.High,
                },
            ],
            PulseWeight = 1,
        },
        new()
        {
            Id = "artifact_discovery",
            Name = "Artifact Discovery",
            Description = "A pristine region yields a newly discovered instrument artifact.",
            Target = new SemanticGeneratorTarget
            {
                Kind = EntityKind.Region,
                AllowWilderness = true,
            },
            Produces =
            [
                new SemanticProducedEntity
                {
                    Ref = "$artifact",
                    Kind = EntityKind.Artifact,
                    Subtype = "instrument",
                    Role = "discovered",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships = [],
            Triggers =
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = EntityKind.Region,
                    AttributeName = A(Attr.Pristineness),
                    Direction = TriggerDirection.High,
                },
            ],
            PulseWeight = 1,
        },
    ];

    private static string A(Attr attribute) => attribute.ToDomainName();
    private static string L(Rel relationship) => relationship.ToDomainName();
}
