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

        var settlement = Assert.Contains(EntityKind.Settlement, graph.Kinds);
        Assert.Contains(DefinitionAttributeId.Residents, settlement.Attributes.Keys);
        Assert.Contains(DefinitionAttributeId.Security, settlement.Attributes.Keys);
        Assert.Equal(DefinitionAttributeId.Residents, settlement.HealthAttribute);
        Assert.Contains(DefinitionResourceId.Food, graph.Resources.Keys);
        Assert.Contains(DefinitionRelationshipId.ControlledBy, graph.Relationships.Keys);
        Assert.Contains(DefinitionPressureId.CulturalFriction, graph.Pressures.Keys);
        Assert.Contains(EntityKind.Npc, graph.Lifecycles.Keys);

        var npcEat = Assert.Contains("npc_eat", graph.Processes);
        Assert.Contains(
            npcEat.GoalEffects,
            reference => reference.ValueId is AttributeValueId { AttributeId: DefinitionAttributeId.Satiety });
        Assert.Contains(
            graph.Attributes.Values,
            attribute => attribute.Key == new DefinitionAttributeKey(EntityKind.Npc, DefinitionAttributeId.Satiety) &&
                         attribute.CriticalEffect is { TargetAttributeId: DefinitionAttributeId.Vitality });
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
    public void SemanticFlowGraph_StartsEmptyUntilCsvContentIsImported()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());

        Assert.Empty(graph.SemanticFlows.Stocks);
        Assert.Empty(graph.SemanticFlows.Flows);
        Assert.Empty(graph.SemanticFlows.Modifiers);
        Assert.Empty(graph.SemanticFlows.Couplings);
    }

    [Fact]
    public void Build_DomainWithUnknownDeclaredResource_Throws()
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

        var ex = Assert.Throws<DefinitionGraphException>(() => DomainLoader.BuildDefinitionGraph(badConfig));
        Assert.Contains("manna", ex.Message, StringComparison.Ordinal);
    }
}
