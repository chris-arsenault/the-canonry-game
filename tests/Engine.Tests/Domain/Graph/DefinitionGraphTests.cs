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
    public void SemanticFlowGraph_StartsEmptyUntilCsvContentIsImported()
    {
        var graph = DomainLoader.BuildDefinitionGraph(TestDomainDefinition.Create());

        Assert.Empty(graph.SemanticFlows.Stocks);
        Assert.Empty(graph.SemanticFlows.Flows);
        Assert.Empty(graph.SemanticFlows.Modifiers);
        Assert.Empty(graph.SemanticFlows.Couplings);
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
}
