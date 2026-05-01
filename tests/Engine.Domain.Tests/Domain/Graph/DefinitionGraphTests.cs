namespace Engine.Domain.Tests.Domain.Graph;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Loader;
using Engine.Gambits;
using IceRemembers;
using Engine.Kinds;
using Engine.Behaviors;
using Engine.Wiring;

public class DefinitionGraphTests
{
    [Fact]
    public void Build_FromIceRemembers_CreatesTypedGraph()
    {
        var graph = DomainLoader.BuildDefinitionGraph(IceRemembersDefinition.Create());
        var tempHp = new DefinitionAttributeId(IceRemembersAttributeId.TempHp.ToDomainName());
        var dominion = new DefinitionAttributeId(IceRemembersAttributeId.Dominion.ToDomainName());
        var food = new DefinitionResourceId(IceRemembersResource.Food.ToDomainName());
        var controlledBy = new DefinitionRelationshipId(IceRemembersRelationship.ControlledBy.ToDomainName());
        var culturalFriction = new DefinitionPressureId(IceRemembersPressure.CulturalFriction.ToDomainName());

        var settlement = Assert.Contains(TestKinds.Settlement, graph.Kinds);
        Assert.Contains(tempHp, settlement.Attributes.Keys);
        Assert.Contains(dominion, settlement.Attributes.Keys);
        Assert.Equal(tempHp, settlement.HealthAttribute);
        Assert.Contains(food, graph.Resources.Keys);
        Assert.Contains(controlledBy, graph.Relationships.Keys);
        Assert.Contains(culturalFriction, graph.Pressures.Keys);
        Assert.Contains(TestKinds.Penguin, graph.Lifecycles.Keys);
    }

    [Fact]
    public void Build_GambitWithUnknownAttributeRef_Throws()
    {
        var config = IceRemembersDefinition.Create();
        var gambit = new GambitDefinition(
            "bad_gambit",
            "Bad Gambit",
            TestKinds.Penguin,
            new AlwaysTrue(),
            Expr.Score(Expr.Lit(1f)));
        gambit.Reads.Add(AttributeRef.Self("not_a_real_attribute", TestKinds.Penguin));

        var badConfig = new DomainConfig
        {
            Kinds = config.Kinds,
            Terrains = config.Terrains,
            Gambits = [.. config.Gambits, gambit],
            Relationships = config.Relationships,
            RelationshipFacts = config.RelationshipFacts,
            Pressures = config.Pressures,
            Lifecycles = config.Lifecycles,
            Resources = config.Resources,
            Cultures = config.Cultures,
        };

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(badConfig));
        Assert.Contains("not_a_real_attribute", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_FromIceRemembers_PopulatesTypedSemanticGraph()
    {
        var graph = DomainLoader.BuildDefinitionGraph(IceRemembersDefinition.Create());

        Assert.NotEmpty(graph.SemanticFlows.Stocks);
        Assert.Empty(graph.SemanticFlows.Verbs);
        Assert.Empty(graph.SemanticFlows.Flows);

        Assert.Empty(graph.SemanticFlows.Modifiers);
        Assert.Empty(graph.SemanticFlows.Diagnostics);
    }

    [Fact]
    public void Build_DomainWithNewDeclaredResource_AcceptsItAsDomainData()
    {
        var config = IceRemembersDefinition.Create();
        var badConfig = new DomainConfig
        {
            Kinds = config.Kinds,
            Terrains = config.Terrains,
            Relationships = config.Relationships,
            RelationshipFacts = config.RelationshipFacts,
            Pressures = config.Pressures,
            Lifecycles = config.Lifecycles,
            Resources = [.. config.Resources, new ResourceConfig { Name = "manna", DisplayName = "Manna" }],
            Cultures = config.Cultures,
            Semantics = config.Semantics,
        };

        var graph = DomainLoader.BuildDefinitionGraph(badConfig);

        Assert.Contains(new DefinitionResourceId("manna"), graph.Resources.Keys);
    }

    [Fact]
    public void Build_FromIceRemembers_AllSemanticStocksSatisfyRequiredFlowCoverage()
    {
        var graph = DomainLoader.BuildDefinitionGraph(IceRemembersDefinition.Create());

        var uncovered = graph.SemanticFlows.Stocks
            .Select(stock => new
            {
                Stock = stock,
                Coverage = SemanticCoverageAnalyzer.Summarize(stock, graph.SemanticFlows),
            })
            .Where(entry =>
                (entry.Stock.RequiredFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Inflow) &&
                 entry.Coverage.ScalarInflows == 0) ||
                (entry.Stock.RequiredFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Outflow) &&
                 entry.Coverage.ScalarOutflows == 0) ||
                (entry.Stock.RequiredXFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Inflow) &&
                 entry.Coverage.XInflows == 0) ||
                (entry.Stock.RequiredXFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Outflow) &&
                 entry.Coverage.XOutflows == 0) ||
                (entry.Stock.RequiredYFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Inflow) &&
                 entry.Coverage.YInflows == 0) ||
                (entry.Stock.RequiredYFlowCoverage.HasFlag(SemanticFlowCoverageRequirement.Outflow) &&
                 entry.Coverage.YOutflows == 0))
            .ToList();

        Assert.Empty(uncovered);
    }

    [Fact]
    public void Build_FromIceRemembers_HasNoGateFlows()
    {
        var graph = DomainLoader.BuildDefinitionGraph(IceRemembersDefinition.Create());

        Assert.DoesNotContain(graph.SemanticFlows.Flows, flow => flow.IsGate);
    }

    [Fact]
    public void Build_FromIceRemembers_HasNoDirectTwoNodeReversals()
    {
        var graph = DomainLoader.BuildDefinitionGraph(IceRemembersDefinition.Create());
        var projected = SemanticStockGraphProjection.ProjectAttributeEdges(graph.SemanticFlows);
        var edgeSet = projected
            .Select(edge => (edge.SourceId, edge.TargetId))
            .ToHashSet();

        var reversals = projected
            .Where(edge =>
                edge.SourceId is AttributeStockId sourceAttr &&
                edge.TargetId is AttributeStockId targetAttr &&
                edge.SourceId != edge.TargetId &&
                graph.Attributes[sourceAttr.Key].Shape == DefinitionAttributeShape.Scalar &&
                graph.Attributes[targetAttr.Key].Shape == DefinitionAttributeShape.Scalar &&
                edgeSet.Contains((edge.TargetId, edge.SourceId)))
            .ToList();

        Assert.Empty(reversals);
    }

    [Fact]
    public void Build_SemanticStockMissingRequiredOutflow_Throws()
    {
        var config = IceRemembersDefinition.Create();
        var semantics = config.Semantics!;
        var brokenStocks = semantics.Stocks
            .Select(stock =>
                stock is SemanticAttributeStockConfig attr &&
                attr.Kind == TestKinds.Penguin &&
                attr.AttributeName == IceRemembersAttributeId.Echo.ToDomainName()
                    ? (SemanticStockConfig)new SemanticAttributeStockConfig
                    {
                        Kind = attr.Kind,
                        AttributeName = attr.AttributeName,
                        StockClass = attr.StockClass,
                        FeedbackClass = attr.FeedbackClass,
                        RequiredFlowCoverage = SemanticFlowCoverageRequirement.Both,
                        BoundsLabel = attr.BoundsLabel,
                        ConservationLabel = attr.ConservationLabel,
                        CoverageNotes = attr.CoverageNotes,
                        Notes = attr.Notes,
                    }
                    : stock)
            .ToList();
        var brokenFlows = semantics.Flows
            .Where(flow =>
                !(flow.TargetKind == TestKinds.Penguin &&
                  flow.TargetAttribute == IceRemembersAttributeId.Echo.ToDomainName() &&
                  flow.Role == SemanticFlowRole.Outflow))
            .ToList();
        brokenFlows.Add(new SemanticFlowConfig
        {
            VerbId = "test_echo_probe",
            Role = SemanticFlowRole.Inflow,
            TargetKind = TestKinds.Penguin,
            TargetAttribute = IceRemembersAttributeId.Vitality.ToDomainName(),
            SourceRefs = [AttributeRef.Self(IceRemembersAttributeId.Echo.ToDomainName(), TestKinds.Penguin)],
        });
        var brokenVerbs = semantics.Verbs
            .Append(new SemanticVerbConfig
            {
                Id = "test_echo_probe",
                OwnerType = SemanticOwnerType.Entity,
                OwnerKind = TestKinds.Penguin,
            })
            .ToList();

        var brokenConfig = new DomainConfig
        {
            Kinds = config.Kinds,
            Terrains = config.Terrains,
            Relationships = config.Relationships,
            RelationshipFacts = config.RelationshipFacts,
            Pressures = config.Pressures,
            Lifecycles = config.Lifecycles,
            Resources = config.Resources,
            Cultures = config.Cultures,
            Semantics = new SemanticDomainConfig
            {
                Stocks = brokenStocks,
                Verbs = brokenVerbs,
                Flows = brokenFlows,
                Modifiers = semantics.Modifiers,
            },
        };

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(brokenConfig));
        Assert.Contains("penguin.echo", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("outflow", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

}
