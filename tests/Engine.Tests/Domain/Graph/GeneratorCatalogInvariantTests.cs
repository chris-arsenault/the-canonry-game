namespace Engine.Tests.Domain.Graph;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Loader;
using Engine.Domain.TestDomain;
using Engine.Kinds;

public class GeneratorCatalogInvariantTests
{
    [Fact]
    public void Validate_GeneratorProducedRefWithoutRelationship_ThrowsActionableMessage()
    {
        var generator = CreateBaseGenerator(relationships: []);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("produces '$hero' but no relationship references it", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_GeneratorTriggerKindMismatch_ThrowsActionableMessage()
    {
        var generator = CreateBaseGenerator(
            triggers:
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = EntityKind.Faction,
                    AttributeName = "cohesion",
                    Direction = TriggerDirection.Low,
                },
            ]);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("target kind 'Settlement' does not match trigger kind 'Faction'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_OptionalProduceWithTooManyRelationshipAnchors_ThrowsActionableMessage()
    {
        var generator = CreateBaseGenerator(
            produces:
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
                    Ref = "$artifact",
                    Kind = EntityKind.Artifact,
                    Subtype = "weapon",
                    Role = "weapon",
                    CreateChance = 0.25f,
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$target",
                    Kind = "located_at",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$artifact",
                    DestinationRef = "$target",
                    Kind = "stored_at",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$artifact",
                    DestinationRef = "$hero",
                    Kind = "created_by",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$artifact",
                    DestinationRef = "$hero",
                    Kind = "involved_in",
                },
            ]);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("optional produce '$artifact' is referenced by 3 relationships", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_CyclicInheritCultureFrom_ThrowsActionableMessage()
    {
        var generator = CreateBaseGenerator(
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = EntityKind.Npc,
                    Subtype = "hero",
                    Role = "hero",
                    InheritCultureFrom = "$rival",
                },
                new SemanticProducedEntity
                {
                    Ref = "$rival",
                    Kind = EntityKind.Npc,
                    Subtype = "warrior",
                    Role = "rival",
                    InheritCultureFrom = "$hero",
                },
            ],
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$rival",
                    Kind = "loyal_to",
                },
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$rival",
                    DestinationRef = "$hero",
                    Kind = "loyal_to",
                },
            ]);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("cyclic InheritCultureFrom chain", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_GeneratorWithoutTriggers_ThrowsActionableMessage()
    {
        var generator = CreateBaseGenerator(triggers: []);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("must declare at least one trigger", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ArtifactDiscoveryWithoutRelationships_IsAllowed()
    {
        var generator = new SemanticGeneratorConfig
        {
            Id = "artifact_discovery",
            Name = "Artifact Discovery",
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
                    AttributeName = "pristineness",
                    Direction = TriggerDirection.High,
                },
            ],
        };

        var graph = DomainLoader.BuildDefinitionGraph(WithGenerators(generator));

        Assert.Contains("artifact_discovery", graph.Generators.Keys);
    }

    private static SemanticGeneratorConfig CreateBaseGenerator(
        IReadOnlyList<SemanticProducedEntity>? produces = null,
        IReadOnlyList<SemanticGeneratorRelationship>? relationships = null,
        IReadOnlyList<SemanticGeneratorTrigger>? triggers = null) =>
        new()
        {
            Id = "hero_emergence",
            Name = "Hero Emergence",
            Target = new SemanticGeneratorTarget
            {
                Kind = EntityKind.Settlement,
            },
            Produces = produces ??
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = EntityKind.Npc,
                    Subtype = "hero",
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
            ],
            Relationships = relationships ??
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$target",
                    Kind = "located_at",
                },
            ],
            Triggers = triggers ??
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = EntityKind.Settlement,
                    AttributeName = "sanitation",
                    Direction = TriggerDirection.Low,
                },
            ],
        };

    private static DomainConfig WithGenerators(params SemanticGeneratorConfig[] generators)
    {
        var config = TestDomainDefinition.Create();
        var semantics = config.Semantics!;

        return new DomainConfig
        {
            Kinds = config.Kinds,
            Processes = config.Processes,
            Relationships = config.Relationships,
            Pressures = config.Pressures,
            Lifecycles = config.Lifecycles,
            Resources = config.Resources,
            Cultures = config.Cultures,
            Semantics = new SemanticDomainConfig
            {
                Stocks = semantics.Stocks,
                Verbs = semantics.Verbs,
                Flows = semantics.Flows,
                Modifiers = semantics.Modifiers,
                Generators = generators,
            },
        };
    }
}
