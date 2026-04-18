namespace Engine.Tests.Domain.Graph;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Loader;
using Engine.Domain.TestDomain;
using Engine.Kinds;

public class RuntimeGeneratorBuilderTests
{
    [Fact]
    public void Build_FromTestDomain_ProjectsMvpGenerators()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());

        Assert.Equal(4, graph.Generators.Count);
        Assert.Contains("colony_founding", graph.Generators.Keys);
        Assert.Contains("hero_emergence", graph.Generators.Keys);
        Assert.Contains("faction_splinter", graph.Generators.Keys);
        Assert.Contains("artifact_discovery", graph.Generators.Keys);
    }

    [Fact]
    public void Build_WithValidGenerator_ProjectsTypedNode()
    {
        var graph = DomainLoader.BuildDefinitionGraph(WithGenerators(CreateValidGenerator()));

        var generator = Assert.Contains("hero_emergence", graph.Generators);
        Assert.Equal(EntityKind.Settlement, generator.Target.Kind);
        Assert.Equal("town", Assert.Single(generator.Target.Subtypes!));
        Assert.Equal(EntityKind.Npc, Assert.Single(generator.Produces).Kind);
        Assert.Equal(new DefinitionRelationshipId("located_at"), Assert.Single(generator.Relationships).RelationshipId);
        Assert.Equal(new DefinitionAttributeId("sanitation"), Assert.Single(generator.Triggers).AttributeId);
    }

    [Fact]
    public void Build_GeneratorWithMalformedRef_Throws()
    {
        var generator = CreateValidGenerator(
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$typo",
                    DestinationRef = "$target",
                    Kind = "located_at",
                },
            ]);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("$typo", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_GeneratorWithUnknownTriggerAttribute_Throws()
    {
        var generator = CreateValidGenerator(
            triggers:
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = EntityKind.Settlement,
                    AttributeName = "not_a_real_attribute",
                },
            ]);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("not_a_real_attribute", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_GeneratorWithUndeclaredRelationshipKind_Throws()
    {
        var generator = CreateValidGenerator(
            relationships:
            [
                new SemanticGeneratorRelationship
                {
                    SourceRef = "$hero",
                    DestinationRef = "$target",
                    Kind = "not_a_real_relationship",
                },
            ]);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("not_a_real_relationship", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_GeneratorWithUnknownTargetSubtype_Throws()
    {
        var generator = CreateValidGenerator(
            target: new SemanticGeneratorTarget
            {
                Kind = EntityKind.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal)
                {
                    "not_a_real_subtype",
                },
            });

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("not_a_real_subtype", ex.Message, StringComparison.Ordinal);
    }

    private static SemanticGeneratorConfig CreateValidGenerator(
        SemanticGeneratorTarget? target = null,
        IReadOnlyList<SemanticProducedEntity>? produces = null,
        IReadOnlyList<SemanticGeneratorRelationship>? relationships = null,
        IReadOnlyList<SemanticGeneratorTrigger>? triggers = null) =>
        new()
        {
            Id = "hero_emergence",
            Name = "Hero Emergence",
            Target = target ?? new SemanticGeneratorTarget
            {
                Kind = EntityKind.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal)
                {
                    "town",
                },
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
