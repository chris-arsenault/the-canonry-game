namespace Engine.Domain.Tests.Domain.Graph;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Loader;
using IceRemembers;
using Engine.Kinds;

public class RuntimeGeneratorBuilderTests
{
    [Fact]
    public void Build_FromIceRemembers_CompilesActiveGeneratorCatalog()
    {
        var graph = DomainLoader.BuildDefinitionGraph(IceRemembersDefinition.Create());

        Assert.NotEmpty(graph.Generators);
        Assert.Equal(IceRemembersGeneratorDefinition.Create().Count, graph.Generators.Count);
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
        Assert.Equal(TestKinds.Settlement, Assert.Single(generator.Triggers).OnKind);
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
    public void Build_GeneratorTargetRelationshipRequirements_ProjectsTypedNode()
    {
        var generator = CreateValidGenerator(
            target: new SemanticGeneratorTarget
            {
                Kind = TestKinds.Settlement,
                RequiredOutgoingRelationships = new HashSet<string>(StringComparer.Ordinal) { "trade_route" },
                RequiredIncomingRelationships = new HashSet<string>(StringComparer.Ordinal) { "controlled_by" },
                RequiredRelationships = new HashSet<string>(StringComparer.Ordinal) { "home_base" },
            });

        var graph = DomainLoader.BuildDefinitionGraph(WithGenerators(generator));
        var target = graph.Generators["test_penguin_package"].Target;

        Assert.Equal("trade_route", Assert.Single(target.RequiredOutgoingRelationships!));
        Assert.Equal("controlled_by", Assert.Single(target.RequiredIncomingRelationships!));
        Assert.Equal("home_base", Assert.Single(target.RequiredRelationships!));
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
            Terrains = config.Terrains,
            Gambits = config.Gambits,
            Relationships = config.Relationships,
            Lifecycles = config.Lifecycles,
            Cultures = config.Cultures,
            Gameplay = config.Gameplay,
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
