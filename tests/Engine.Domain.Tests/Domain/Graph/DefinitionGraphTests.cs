namespace Engine.Domain.Tests.Domain.Graph;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Loader;
using IceRemembers;
using Engine.Kinds;
using Engine.Processes;
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
    public void Build_ProcessWithUnknownAttributeRef_Throws()
    {
        var config = IceRemembersDefinition.Create();
        var process = new ProcessDefinition(
            "bad_process",
            "Bad Process",
            TestKinds.Penguin,
            new AlwaysTrue(),
            Expr.Score(Expr.Lit(1f)));
        process.Reads.Add(AttributeRef.Self("not_a_real_attribute", TestKinds.Penguin));

        var badConfig = new DomainConfig
        {
            Kinds = config.Kinds,
            Terrains = config.Terrains,
            Processes = [.. config.Processes, process],
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

        // 5 kinds * 3 attributes = 15 attribute stocks, plus 3 hex resource stocks.
        Assert.Equal(18, graph.SemanticFlows.Stocks.Count);

        // engine_decay + 5 self-sacrifice + 5 hex-to-health + 5 untouched-to-hex
        // + 10 RPS ring + 5 mobility + 5 drift = 36 verbs.
        Assert.Equal(36, graph.SemanticFlows.Verbs.Count);

        // Flows unchanged: mobility/drift verbs are flowless.
        // 15 decay + 10 self-sac halves + 5 hex-to-health + 5 untouched-to-hex + 20 conversion halves = 55.
        Assert.Equal(55, graph.SemanticFlows.Flows.Count);

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
            Processes = config.Processes,
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
    public void Build_FromIceRemembers_HasEngineDecayVerb()
    {
        var graph = DomainLoader.BuildDefinitionGraph(IceRemembersDefinition.Create());

        Assert.Contains(
            graph.SemanticFlows.Verbs,
            verb => verb.Id == IceRemembersVerbId.EngineDecay.ToVerbId() &&
                    verb.OwnerType == SemanticOwnerType.Engine);
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
    public void Build_FromIceRemembers_ProjectsRpsRingEdges()
    {
        var graph = DomainLoader.BuildDefinitionGraph(IceRemembersDefinition.Create());
        var projected = SemanticStockGraphProjection.ProjectAttributeEdges(graph.SemanticFlows);

        var attributeEdges = projected
            .Where(edge =>
                edge.SourceId is AttributeStockId &&
                edge.TargetId is AttributeStockId)
            .Select(edge => (
                Source: ((AttributeStockId)edge.SourceId).Key,
                Target: ((AttributeStockId)edge.TargetId).Key))
            .ToHashSet();

        DefinitionAttributeKey Key(EntityKind kind, IceRemembersAttributeId attr) =>
            new(kind, new DefinitionAttributeId(attr.ToDomainName()));

        (DefinitionAttributeKey, DefinitionAttributeKey)[] expectedRingEdges =
        [
            // +1 edges (K_N → K_{N+1}).
            (Key(TestKinds.Penguin, IceRemembersAttributeId.Echo), Key(TestKinds.Settlement, IceRemembersAttributeId.Dominion)),
            (Key(TestKinds.Settlement, IceRemembersAttributeId.Shelter), Key(TestKinds.Faction, IceRemembersAttributeId.Reach)),
            (Key(TestKinds.Faction, IceRemembersAttributeId.Provisions), Key(TestKinds.Region, IceRemembersAttributeId.Holding)),
            (Key(TestKinds.Region, IceRemembersAttributeId.Pristiness), Key(TestKinds.Artifact, IceRemembersAttributeId.Power)),
            (Key(TestKinds.Artifact, IceRemembersAttributeId.Power), Key(TestKinds.Penguin, IceRemembersAttributeId.Echo)),
            // +2 edges (K_N → K_{N+2}).
            (Key(TestKinds.Penguin, IceRemembersAttributeId.Warmth), Key(TestKinds.Faction, IceRemembersAttributeId.Provisions)),
            (Key(TestKinds.Settlement, IceRemembersAttributeId.Dominion), Key(TestKinds.Region, IceRemembersAttributeId.Pristiness)),
            (Key(TestKinds.Faction, IceRemembersAttributeId.Reach), Key(TestKinds.Artifact, IceRemembersAttributeId.Resonance)),
            (Key(TestKinds.Region, IceRemembersAttributeId.Holding), Key(TestKinds.Penguin, IceRemembersAttributeId.Warmth)),
            (Key(TestKinds.Artifact, IceRemembersAttributeId.Resonance), Key(TestKinds.Settlement, IceRemembersAttributeId.Shelter)),
        ];

        foreach (var expected in expectedRingEdges)
        {
            Assert.Contains(expected, attributeEdges);
        }
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

        var brokenConfig = new DomainConfig
        {
            Kinds = config.Kinds,
            Terrains = config.Terrains,
            Processes = config.Processes,
            Relationships = config.Relationships,
            RelationshipFacts = config.RelationshipFacts,
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
            },
        };

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(brokenConfig));
        Assert.Contains("penguin.echo", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("outflow", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_ScalarStockMissingDecayOutflow_Throws()
    {
        var config = IceRemembersDefinition.Create();
        var semantics = config.Semantics!;
        var withoutAmbitionDecay = semantics.Flows
            .Where(flow => !(flow.TargetKind == TestKinds.Penguin &&
                             flow.TargetAttribute == IceRemembersAttributeId.Echo.ToDomainName() &&
                             flow.VerbId == IceRemembersVerbId.EngineDecay.ToVerbId()))
            .ToList();

        var brokenConfig = new DomainConfig
        {
            Kinds = config.Kinds,
            Terrains = config.Terrains,
            Processes = config.Processes,
            Relationships = config.Relationships,
            RelationshipFacts = config.RelationshipFacts,
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
            },
        };

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(brokenConfig));
        Assert.Contains("penguin.echo", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("engine_decay", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FindClosedLoops_FromIceRemembers_DetectsRpsRingStructure()
    {
        var graph = DomainLoader.BuildDefinitionGraph(IceRemembersDefinition.Create());

        var loops = SemanticLoopAnalyzer.FindClosedLoops(graph, minDistinctKinds: 3, maxLength: 8);

        // The RPS ring creates closed loops across multiple kinds; at least one loop
        // should span ≥4 distinct kinds (ring arcs touch ≥4 kinds before returning).
        Assert.NotEmpty(loops);
        Assert.Contains(loops, loop => loop.DistinctKinds >= 4);
    }
}
