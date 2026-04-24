namespace Engine.Tests.Domain.Graph;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Loader;
using Engine.Domain.IceRemembers;
using Engine.Kinds;

public class RuntimeGeneratorBuilderTests
{
    [Fact]
    public void Build_FromIceRemembers_ProjectsMvpGenerators()
    {
        var generators = IceRemembersGeneratorDefinition.Create();

        Assert.Equal(7, generators.Count);
        Assert.Contains(generators, generator => generator.Id == "penguin_calls_the_unhuddled");
        Assert.Contains(generators, generator => generator.Id == "faction_seals_the_edge");
        Assert.Contains(generators, generator => generator.Id == "region_opens_the_old_holding");
        Assert.Contains(generators, generator => generator.Id == "artifact_cracks_the_berg");
        Assert.Contains(generators, generator => generator.Id == "settlement_names_the_office");
        Assert.Contains(generators, generator => generator.Id == "sea_raises_the_reefhold");
        Assert.Contains(generators, generator => generator.Id == "the_shelf_yields_a_tome");
    }

    [Fact]
    public void Build_WithValidGenerator_ProjectsTypedNode()
    {
        var graph = DomainLoader.BuildDefinitionGraph(WithGenerators(CreateValidGenerator()));

        var generator = Assert.Contains("test_penguin_package", graph.Generators);
        Assert.Equal(TestKinds.Settlement, generator.Target.Kind);
        Assert.Equal("rookery", Assert.Single(generator.Target.Subtypes!));
        Assert.Equal(IceRemembersKinds.Penguin, Assert.Single(generator.Produces).Kind);
        Assert.Equal(new DefinitionRelationshipId("home_base"), Assert.Single(generator.Relationships).RelationshipId);
        Assert.Equal(new DefinitionAttributeId("shelter"), Assert.Single(generator.Triggers).AttributeId);
    }

    [Fact]
    public void Build_GeneratorWithSubtypeSet_ProjectsSubtypeOptions()
    {
        var generator = CreateValidGenerator(
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
                    Subtypes = new HashSet<string>(StringComparer.Ordinal) { "warden", "exile" },
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
            ]);

        var graph = DomainLoader.BuildDefinitionGraph(WithGenerators(generator));

        var produced = Assert.Single(graph.Generators["test_penguin_package"].Produces);
        Assert.Null(produced.Subtype);
        Assert.Equal(2, produced.Subtypes!.Count);
        Assert.Contains("warden", produced.Subtypes);
        Assert.Contains("exile", produced.Subtypes);
    }

    [Fact]
    public void Build_GeneratorProduceWithBothSubtypeForms_Throws()
    {
        var generator = CreateValidGenerator(
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
                    Subtypes = new HashSet<string>(StringComparer.Ordinal) { "exile" },
                    Role = "hero",
                    InheritCultureFrom = "$target",
                },
            ]);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("exactly one of Subtype or Subtypes", ex.Message, StringComparison.Ordinal);
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
                    Kind = "home_base",
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
                    OnKind = TestKinds.Settlement,
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
                Kind = TestKinds.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal)
                {
                    "not_a_real_subtype",
                },
            });

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("not_a_real_subtype", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_GeneratorProduceWithoutInheritCultureFrom_Throws()
    {
        var generator = CreateValidGenerator(
            produces:
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = TestKinds.Penguin,
                    Subtype = "warden",
                    Role = "hero",
                    InheritCultureFrom = "",
                },
            ]);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(WithGenerators(generator)));

        Assert.Contains("must declare InheritCultureFrom", ex.Message, StringComparison.Ordinal);
    }

    private static SemanticGeneratorConfig CreateValidGenerator(
        SemanticGeneratorTarget? target = null,
        IReadOnlyList<SemanticProducedEntity>? produces = null,
        IReadOnlyList<SemanticGeneratorRelationship>? relationships = null,
        IReadOnlyList<SemanticGeneratorTrigger>? triggers = null) =>
        new()
        {
            Id = "test_penguin_package",
            Name = "Hero Emergence",
            Target = target ?? new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
                Subtypes = new HashSet<string>(StringComparer.Ordinal)
                {
                    "rookery",
                },
            },
            Produces = produces ??
            [
                new SemanticProducedEntity
                {
                    Ref = "$hero",
                    Kind = IceRemembersKinds.Penguin,
                    Subtype = "warden",
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
                    Kind = "home_base",
                },
            ],
            Triggers = triggers ??
            [
                new SemanticGeneratorTrigger
                {
                    OnKind = TestKinds.Settlement,
                    AttributeName = "shelter",
                    Direction = TriggerDirection.Low,
                },
            ],
        };

    private static DomainConfig WithGenerators(params SemanticGeneratorConfig[] generators)
    {
        var config = IceRemembersDefinition.Create();
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
