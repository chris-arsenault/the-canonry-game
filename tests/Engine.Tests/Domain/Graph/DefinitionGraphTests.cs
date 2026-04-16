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
        var security = new DefinitionAttributeId(TestDomainAttributeId.Security.ToDomainName());
        var satiety = new DefinitionAttributeId(TestDomainAttributeId.Satiety.ToDomainName());
        var vitality = new DefinitionAttributeId(TestDomainAttributeId.Vitality.ToDomainName());
        var food = new DefinitionResourceId(TestDomainResource.Food.ToDomainName());
        var controlledBy = new DefinitionRelationshipId(TestDomainRelationship.ControlledBy.ToDomainName());
        var culturalFriction = new DefinitionPressureId(TestDomainPressure.CulturalFriction.ToDomainName());

        var settlement = Assert.Contains(EntityKind.Settlement, graph.Kinds);
        Assert.Contains(residents, settlement.Attributes.Keys);
        Assert.Contains(security, settlement.Attributes.Keys);
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

        Assert.Equal(19, graph.SemanticFlows.Stocks.Count);
        Assert.Equal(32, graph.SemanticFlows.Verbs.Count);
        Assert.Equal(66, graph.SemanticFlows.Flows.Count);
        Assert.Equal(8, graph.SemanticFlows.Modifiers.Count);
        Assert.Equal(33, graph.SemanticFlows.ProcessReferences.Count);
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
        };

        var graph = DomainLoader.BuildDefinitionGraph(badConfig);

        Assert.Contains(new DefinitionResourceId("manna"), graph.Resources.Keys);
    }

    [Fact]
    public void Build_FromTestDomain_ResolvesRepresentativeSemanticEdges()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());

        Assert.Contains(
            graph.SemanticFlows.Verbs,
            verb => verb.Id == TestDomainVerbId.SettlementGrow.ToVerbId() &&
                    verb.ImplementedProcess?.Id == TestDomainProcessId.SettlementGrow.ToProcessId());
        Assert.Contains(
            graph.SemanticFlows.Verbs,
            verb => verb.Id == TestDomainVerbId.EngineDecay.ToVerbId() &&
                    verb.OwnerType == SemanticOwnerType.Engine);
        Assert.Contains(
            graph.SemanticFlows.Flows,
            flow => flow.Verb.Id == TestDomainVerbId.NpcEat.ToVerbId() &&
                    flow.Role == SemanticFlowRole.Inflow &&
                    flow.TargetStock.AttributeKey == new DefinitionAttributeKey(
                        EntityKind.Npc,
                        new DefinitionAttributeId(TestDomainAttributeId.Satiety.ToDomainName())) &&
                    flow.SourceRefs.Any(reference => reference.ValueId is ResourceValueId { ResourceId.Value: "food" }));
        Assert.Contains(
            graph.SemanticFlows.ProcessReferences,
            reference => reference.ProcessId == TestDomainProcessId.NpcEat.ToProcessId() &&
                         reference.Kind == SemanticProcessReferenceKind.Reads &&
                         reference.TargetStock.AttributeKey == new DefinitionAttributeKey(
                             EntityKind.Npc,
                             new DefinitionAttributeId(TestDomainAttributeId.Satiety.ToDomainName())) &&
                         reference.MatchesDefinitionGraph);
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
    public void Build_FromTestDomain_PlaneCoverageIsAxisAware()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());
        var artifactResonance = Assert.Single(
            graph.SemanticFlows.Stocks,
            stock => stock.AttributeKey == new DefinitionAttributeKey(
                EntityKind.Artifact,
                new DefinitionAttributeId(TestDomainAttributeId.MysticResonance.ToDomainName())));

        var coverage = SemanticCoverageAnalyzer.Summarize(artifactResonance, graph.SemanticFlows);

        Assert.Equal(0, coverage.ScalarInflows);
        Assert.Equal(0, coverage.ScalarOutflows);
        Assert.Equal(0, coverage.XInflows);
        Assert.Equal(0, coverage.XOutflows);
        Assert.Equal(1, coverage.YInflows);
        Assert.Equal(1, coverage.YOutflows);
    }

    [Fact]
    public void Build_SemanticStockMissingRequiredOutflow_Throws()
    {
        var config = TestDomainDefinition.Create();
        var semantics = config.Semantics!;
        var brokenStocks = semantics.Stocks
            .Select(stock =>
                stock.Kind == EntityKind.Npc &&
                stock.AttributeName == TestDomainAttributeId.Ambition.ToDomainName()
                    ? new SemanticStockConfig
                    {
                        Kind = stock.Kind,
                        AttributeName = stock.AttributeName,
                        StockClass = stock.StockClass,
                        FeedbackClass = stock.FeedbackClass,
                        RequiredFlowCoverage = SemanticFlowCoverageRequirement.Both,
                        BoundsLabel = stock.BoundsLabel,
                        ConservationLabel = stock.ConservationLabel,
                        CoverageNotes = stock.CoverageNotes,
                        Notes = stock.Notes,
                    }
                    : stock)
            .ToList();
        var brokenFlows = semantics.Flows
            .Where(flow =>
                !(flow.TargetKind == EntityKind.Npc &&
                  flow.TargetAttribute == TestDomainAttributeId.Ambition.ToDomainName() &&
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
        Assert.Contains("Npc.ambition", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("outflow", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FindClosedLoops_FromTestDomain_FindsOnlyLoopsMeetingDistinctKindThreshold()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());

        var loops = SemanticLoopAnalyzer.FindClosedLoops(graph, minDistinctKinds: 3, maxLength: 6);

        Assert.NotEmpty(loops);
        Assert.All(loops, loop => Assert.True(loop.DistinctKinds >= 3));
    }

    [Fact]
    public void FindClosedLoops_FromTestDomain_FindsRepresentativeThreeKindLoop()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());

        var loops = SemanticLoopAnalyzer.FindClosedLoops(graph, minDistinctKinds: 3, maxLength: 6);

        Assert.Contains(
            loops,
            loop => loop.Signature == "Npc.satiety -> Settlement.security -> Region.regional_safety -> Npc.vitality");
    }
}
