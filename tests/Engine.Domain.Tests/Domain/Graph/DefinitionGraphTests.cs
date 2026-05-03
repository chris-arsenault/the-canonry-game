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
        var controlledBy = new DefinitionRelationshipId(IceRemembersRelationship.ControlledBy.ToDomainName());

        var settlement = Assert.Contains(TestKinds.Settlement, graph.Kinds);
        Assert.Contains(tempHp, settlement.Attributes.Keys);
        Assert.Equal(tempHp, settlement.HealthAttribute);
        Assert.Contains(settlement.Traits.Values, trait => trait.SourceConfig.Polarity == TraitPolarity.Positive);
        Assert.Contains(controlledBy, graph.Relationships.Keys);
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
            Lifecycles = config.Lifecycles,
            Cultures = config.Cultures,
            Gameplay = config.Gameplay,
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
        var brokenConfig = WithRequiredPenguinVitalityOutflow(config);

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(brokenConfig));
        Assert.Contains("penguin.vitality", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("outflow", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DomainConfig WithRequiredPenguinVitalityOutflow(DomainConfig config)
    {
        var semantics = config.Semantics!;
        return new DomainConfig
        {
            Kinds = config.Kinds,
            Terrains = config.Terrains,
            Gambits = config.Gambits,
            Relationships = config.Relationships,
            RelationshipFacts = config.RelationshipFacts,
            Lifecycles = config.Lifecycles,
            Cultures = config.Cultures,
            Gameplay = config.Gameplay,
            Semantics = new SemanticDomainConfig
            {
                Stocks = RequirePenguinVitalityOutflow(semantics.Stocks),
                Verbs = [.. semantics.Verbs, VitalityProbeVerb()],
                Flows = [.. semantics.Flows, VitalityProbeInflow()],
                Modifiers = semantics.Modifiers,
            },
        };
    }

    private static List<SemanticStockConfig> RequirePenguinVitalityOutflow(IReadOnlyList<SemanticStockConfig> stocks) =>
        stocks.Select(stock =>
            IsPenguinVitalityStock(stock)
                ? CopyWithRequiredCoverage((SemanticAttributeStockConfig)stock)
                : stock)
            .ToList();

    private static bool IsPenguinVitalityStock(SemanticStockConfig stock) =>
        stock is SemanticAttributeStockConfig attr &&
        attr.Kind == TestKinds.Penguin &&
        attr.AttributeName == IceRemembersAttributeId.Vitality.ToDomainName();

    private static SemanticAttributeStockConfig CopyWithRequiredCoverage(SemanticAttributeStockConfig attr) =>
        new()
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
        };

    private static SemanticVerbConfig VitalityProbeVerb() =>
        new()
        {
            Id = "test_vitality_probe",
            OwnerType = SemanticOwnerType.Entity,
            OwnerKind = TestKinds.Penguin,
        };

    private static SemanticFlowConfig VitalityProbeInflow() =>
        new()
        {
            VerbId = "test_vitality_probe",
            Role = SemanticFlowRole.Inflow,
            TargetKind = TestKinds.Penguin,
            TargetAttribute = IceRemembersAttributeId.Vitality.ToDomainName(),
            SourceRefs = [AttributeRef.Self(IceRemembersAttributeId.Vitality.ToDomainName(), TestKinds.Penguin)],
        };

}
