namespace Engine.Tests.Domain.Graph;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Loader;
using Engine.Domain.TestDomain;
using Engine.Kinds;
using Engine.Processes;
using Engine.Wiring;

public class DefinitionGraphTests
{
    [Fact]
    public void Build_FromTestDomain_CreatesTypedGraph()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());
        var residents = new DefinitionAttributeId(TestDomainAttributeId.Residents.ToDomainName());
        var dominion = new DefinitionAttributeId(TestDomainAttributeId.Dominion.ToDomainName());
        var satiety = new DefinitionAttributeId(TestDomainAttributeId.Satiety.ToDomainName());
        var vitality = new DefinitionAttributeId(TestDomainAttributeId.Vitality.ToDomainName());
        var food = new DefinitionResourceId(TestDomainResource.Food.ToDomainName());
        var controlledBy = new DefinitionRelationshipId(TestDomainRelationship.ControlledBy.ToDomainName());
        var culturalFriction = new DefinitionPressureId(TestDomainPressure.CulturalFriction.ToDomainName());

        var settlement = Assert.Contains(EntityKind.Settlement, graph.Kinds);
        Assert.Contains(residents, settlement.Attributes.Keys);
        Assert.Contains(dominion, settlement.Attributes.Keys);
        Assert.Equal(residents, settlement.HealthAttribute);
        Assert.Contains(food, graph.Resources.Keys);
        Assert.Contains(controlledBy, graph.Relationships.Keys);
        Assert.Contains(culturalFriction, graph.Pressures.Keys);
        Assert.Contains(EntityKind.Npc, graph.Lifecycles.Keys);

        var npcEat = Assert.Contains("npc_eat", graph.Processes);
        Assert.Contains(
            npcEat.GoalEffects,
            reference => reference.ValueId is AttributeValueId { AttributeId: var attributeId } && attributeId == satiety);
        Assert.Contains(
            graph.Attributes.Values,
            attribute => attribute.Key == new DefinitionAttributeKey(EntityKind.Npc, satiety) &&
                         attribute.CriticalEffect is { TargetAttributeId: var targetId } && targetId == vitality);
    }

    [Fact]
    public void Build_ProcessWithUnknownAttributeRef_Throws()
    {
        var config = TestDomainDefinition.Create();
        var process = new ProcessDefinition(
            "bad_process",
            "Bad Process",
            DefinitionRole.Process,
            EntityKind.Npc,
            new AlwaysTrue(),
            Expr.Score(Expr.Lit(1f)));
        process.Reads.Add(AttributeRef.Self("not_a_real_attribute", EntityKind.Npc));

        var badConfig = new DomainConfig
        {
            Kinds = config.Kinds,
            Processes = [.. config.Processes, process],
            Relationships = config.Relationships,
            Pressures = config.Pressures,
            Lifecycles = config.Lifecycles,
            Resources = config.Resources,
            Cultures = config.Cultures,
        };

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(badConfig));
        Assert.Contains("not_a_real_attribute", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_FromTestDomain_PopulatesTypedSemanticGraph()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());

        Assert.Equal(15, graph.SemanticFlows.Stocks.Count);
        Assert.Single(graph.SemanticFlows.Verbs);
        Assert.Equal(15, graph.SemanticFlows.Flows.Count);
        Assert.Empty(graph.SemanticFlows.Modifiers);
        Assert.Empty(graph.SemanticFlows.ProcessReferences);
        Assert.Empty(graph.SemanticFlows.Diagnostics);
    }

    [Fact]
    public void Build_DomainWithNewDeclaredResource_AcceptsItAsDomainData()
    {
        var config = TestDomainDefinition.Create();
        var badConfig = new DomainConfig
        {
            Kinds = config.Kinds,
            Processes = config.Processes,
            Relationships = config.Relationships,
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
    public void Build_FromTestDomain_HasEngineDecayVerb()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());

        Assert.Contains(
            graph.SemanticFlows.Verbs,
            verb => verb.Id == TestDomainVerbId.EngineDecay.ToVerbId() &&
                    verb.OwnerType == SemanticOwnerType.Engine);
    }

    [Fact]
    public void Build_FromTestDomain_AllSemanticStocksSatisfyRequiredFlowCoverage()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());

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
    public void Build_FromTestDomain_ProjectionIsEmptyUntilVerbsReauthored()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());
        var projected = SemanticStockGraphProjection.ProjectAttributeEdges(graph.SemanticFlows);

        // Reconstruction phase: only engine_decay flows exist and they carry no source refs,
        // so the projected stock graph is empty. This test will grow assertions back in when
        // non-decay verbs are re-authored.
        Assert.Empty(projected);
    }

    [Fact]
    public void Build_FromTestDomain_HasNoGateFlows()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());

        Assert.DoesNotContain(graph.SemanticFlows.Flows, flow => flow.IsGate);
    }

    [Fact]
    public void Build_FromTestDomain_HasNoDirectTwoNodeReversals()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());
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
        var config = TestDomainDefinition.Create();
        var semantics = config.Semantics!;
        var brokenStocks = semantics.Stocks
            .Select(stock =>
                stock is SemanticAttributeStockConfig attr &&
                attr.Kind == EntityKind.Npc &&
                attr.AttributeName == TestDomainAttributeId.Motivation.ToDomainName()
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
                !(flow.TargetKind == EntityKind.Npc &&
                  flow.TargetAttribute == TestDomainAttributeId.Motivation.ToDomainName() &&
                  flow.Role == SemanticFlowRole.Outflow))
            .ToList();

        var brokenConfig = new DomainConfig
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
                Stocks = brokenStocks,
                Verbs = semantics.Verbs,
                Flows = brokenFlows,
                Modifiers = semantics.Modifiers,
                ProcessReferences = semantics.ProcessReferences,
            },
        };

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(brokenConfig));
        Assert.Contains("Npc.motivation", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("outflow", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_ScalarStockMissingDecayOutflow_Throws()
    {
        var config = TestDomainDefinition.Create();
        var semantics = config.Semantics!;
        var withoutAmbitionDecay = semantics.Flows
            .Where(flow => !(flow.TargetKind == EntityKind.Npc &&
                             flow.TargetAttribute == TestDomainAttributeId.Motivation.ToDomainName() &&
                             flow.VerbId == TestDomainVerbId.EngineDecay.ToVerbId()))
            .ToList();

        var brokenConfig = new DomainConfig
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
                Flows = withoutAmbitionDecay,
                Modifiers = semantics.Modifiers,
                ProcessReferences = semantics.ProcessReferences,
            },
        };

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(brokenConfig));
        Assert.Contains("Npc.motivation", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("engine_decay", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FindClosedLoops_FromTestDomain_EmptyUntilVerbsReauthored()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());

        var loops = SemanticLoopAnalyzer.FindClosedLoops(graph, minDistinctKinds: 3, maxLength: 6);

        Assert.Empty(loops);
    }
}
