namespace Engine.Domain.Tests.Domain;

using Engine.Domain.Graph;
using Engine.Wiring;

public sealed class SemanticLoopAnalyzerTests
{
    [Fact]
    public void FindClosedLoops_DetectsTwoKindCycleAndCanonicalizesRotations()
    {
        var graph = EmptyGraph();
        var warmth = AddAttributeStock(graph, TestKinds.Penguin, "warmth");
        var security = AddAttributeStock(graph, TestKinds.Settlement, "security");

        AddFlow(graph, "penguin_warms_settlement", source: warmth, target: security);
        AddFlow(graph, "settlement_shelters_penguin", source: security, target: warmth);

        var loop = Assert.Single(SemanticLoopAnalyzer.FindClosedLoops(graph));

        Assert.Equal(2, loop.DistinctKinds);
        Assert.Equal("penguin.warmth -> settlement.security", loop.Signature);
        Assert.Equal([warmth.Id, security.Id], loop.Edges.Select(edge => edge.SourceId).ToArray());
        Assert.All(loop.Edges, edge => Assert.Single(edge.SupportingFlows));
    }

    [Fact]
    public void FindClosedLoops_FiltersLoopsBelowDistinctKindThreshold()
    {
        var graph = EmptyGraph();
        var warmth = AddAttributeStock(graph, TestKinds.Penguin, "warmth");
        var echo = AddAttributeStock(graph, TestKinds.Penguin, "echo");

        AddFlow(graph, "warmth_feeds_echo", source: warmth, target: echo);
        AddFlow(graph, "echo_feeds_warmth", source: echo, target: warmth);

        Assert.Empty(SemanticLoopAnalyzer.FindClosedLoops(graph));

        var loop = Assert.Single(SemanticLoopAnalyzer.FindClosedLoops(graph, minDistinctKinds: 1));
        Assert.Equal(1, loop.DistinctKinds);
    }

    [Fact]
    public void FindClosedLoops_GroupsParallelSupportingFlowsAndSortsThemByVerb()
    {
        var graph = EmptyGraph();
        var warmth = AddAttributeStock(graph, TestKinds.Penguin, "warmth");
        var security = AddAttributeStock(graph, TestKinds.Settlement, "security");

        AddFlow(graph, "zeta", source: warmth, target: security);
        AddFlow(graph, "alpha", source: warmth, target: security);
        AddFlow(graph, "return", source: security, target: warmth);

        var loop = Assert.Single(SemanticLoopAnalyzer.FindClosedLoops(graph));

        var supporting = loop.Edges[0].SupportingFlows;
        Assert.Equal(["alpha", "zeta"], supporting.Select(flow => flow.Verb.Id).ToArray());
    }

    [Fact]
    public void FindClosedLoops_RespectsMaximumLengthForNonClosingEdges()
    {
        var graph = EmptyGraph();
        var warmth = AddAttributeStock(graph, TestKinds.Penguin, "warmth");
        var security = AddAttributeStock(graph, TestKinds.Settlement, "security");
        var reach = AddAttributeStock(graph, TestKinds.Faction, "reach");
        var power = AddAttributeStock(graph, TestKinds.Artifact, "power");

        AddFlow(graph, "a_to_b", source: warmth, target: security);
        AddFlow(graph, "b_to_c", source: security, target: reach);
        AddFlow(graph, "c_to_d", source: reach, target: power);
        AddFlow(graph, "d_to_a", source: power, target: warmth);

        Assert.Empty(SemanticLoopAnalyzer.FindClosedLoops(graph, maxLength: 3));
        Assert.Single(SemanticLoopAnalyzer.FindClosedLoops(graph, maxLength: 4));
    }

    private static DefinitionGraph EmptyGraph() =>
        new()
        {
            Kinds = new Dictionary<EntityKind, DefinitionKindNode>(),
            Gambits = new Dictionary<string, DefinitionGambitNode>(StringComparer.Ordinal),
            GameplayNodes = [],
            Generators = new Dictionary<string, DefinitionGeneratorNode>(StringComparer.Ordinal),
            Resources = new Dictionary<DefinitionResourceId, DefinitionResourceNode>(),
            Relationships = new Dictionary<DefinitionRelationshipId, DefinitionRelationshipNode>(),
            RelationshipFacts = new Dictionary<DefinitionRelationshipFactId, DefinitionRelationshipFactNode>(),
            Lifecycles = new Dictionary<EntityKind, DefinitionLifecycleNode>(),
            Attributes = new Dictionary<DefinitionAttributeKey, DefinitionAttributeNode>(),
        };

    private static SemanticStockNode AddAttributeStock(
        DefinitionGraph graph,
        EntityKind kind,
        string attributeName)
    {
        var stock = new SemanticStockNode
        {
            Id = new AttributeStockId(new DefinitionAttributeKey(kind, new DefinitionAttributeId(attributeName))),
        };
        graph.SemanticFlows.Stocks.Add(stock);
        return stock;
    }

    private static void AddFlow(
        DefinitionGraph graph,
        string verbId,
        SemanticStockNode source,
        SemanticStockNode target)
    {
        var verb = new SemanticVerbNode { Id = verbId };
        graph.SemanticFlows.Verbs.Add(verb);
        graph.SemanticFlows.Flows.Add(new SemanticFlowEdge
        {
            Id = $"{source.Id.Label}->{target.Id.Label}:{verbId}",
            Verb = verb,
            Role = SemanticFlowRole.Inflow,
            TargetStock = target,
            SourceRefs =
            [
                new TypedDefinitionRef(
                    RefScope.Self,
                    new AttributeValueId(((AttributeStockId)source.Id).Key.AttributeId),
                    ((AttributeStockId)source.Id).Key.Kind),
            ],
        });
    }
}
