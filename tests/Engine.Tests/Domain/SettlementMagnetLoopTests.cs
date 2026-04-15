namespace Engine.Tests.Domain;

using Engine.Behaviors;
using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Messages;
using Engine.Processes;
using Engine.Tests;
using Engine.Tests.Behaviors;

public class SettlementMagnetLoopTests
{
    [Fact]
    public void TestDomain_NpcCreateWorksRaisesSettlementPrestigeAndFactionSupport()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", 0, (0, 0));
        var faction = world.Graph.CreateEntity(EntityKind.Faction, "league", "League", "c", 0, null);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "scholar", "Poet", "c", 0, (0, 0));
        world.Graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, npc.Id, settlement.Id, 1f, 0);
        world.Graph.CreateRelationship("loyal_to", RelationshipPolarity.Positive, npc.Id, faction.Id, 1f, 0);
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "satiety", 90f);
        SetScalar(settlement, "security", 40f);
        var prestigeBefore = settlement.Attributes.GetValue("prestige");
        var supportBefore = faction.Attributes.GetValue("support");
        var satietyBefore = npc.Attributes.GetValue("satiety");

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(npc, world);

        Assert.Equal("Create Works", npc.LastEvaluation!.ExecutedProcessName);
        Assert.True(settlement.Attributes.GetValue("prestige") > prestigeBefore);
        Assert.True(faction.Attributes.GetValue("support") > supportBefore);
        Assert.True(npc.Attributes.GetValue("satiety") < satietyBefore);
    }

    [Fact]
    public void TestDomain_FactionPatrolRaisesSettlementSecurityAndRegionalSafety()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var region = world.Graph.CreateEntity(EntityKind.Region, "plains", "Region", "c", 0, null);
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", 0, (0, 0));
        var faction = world.Graph.CreateEntity(EntityKind.Faction, "league", "League", "c", 0, null);
        world.Graph.CreateRelationship("contains", RelationshipPolarity.Neutral, region.Id, settlement.Id, 1f, 0);
        world.Graph.CreateRelationship("controlled_by", RelationshipPolarity.Neutral, settlement.Id, faction.Id, 1f, 0);
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(faction, "support", 50f);
        SetScalar(settlement, "security", 20f);
        SetScalar(region, "regional_safety", 35f);
        var supportBefore = faction.Attributes.GetValue("support");
        var securityBefore = settlement.Attributes.GetValue("security");
        var safetyBefore = region.Attributes.GetValue("regional_safety");

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(faction, world);

        Assert.Equal("Patrol", faction.LastEvaluation!.ExecutedProcessName);
        Assert.True(faction.Attributes.GetValue("support") < supportBefore);
        Assert.True(settlement.Attributes.GetValue("security") > securityBefore);
        Assert.True(region.Attributes.GetValue("regional_safety") > safetyBefore);
    }

    [Fact]
    public void TestDomain_UnsafeRegionFormsMilitiaAcrossContainedSettlements()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var region = world.Graph.CreateEntity(EntityKind.Region, "plains", "Region", "c", 0, null);
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", 0, (0, 0));
        world.Graph.CreateRelationship("contains", RelationshipPolarity.Neutral, region.Id, settlement.Id, 1f, 0);
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(region, "regional_safety", 25f);
        SetScalar(region, "geologic_stability", 80f);
        SetScalar(settlement, "security", 20f);
        var safetyBefore = region.Attributes.GetValue("regional_safety");
        var securityBefore = settlement.Attributes.GetValue("security");

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(region, world);

        Assert.Equal("Form Militia", region.LastEvaluation!.ExecutedProcessName);
        Assert.True(region.Attributes.GetValue("regional_safety") > safetyBefore);
        Assert.True(settlement.Attributes.GetValue("security") > securityBefore);
    }

    private static EntityEvaluator CreateEvaluator(DomainConfig config, Random random)
    {
        var definitions = new BehaviorDefinitionIndex();
        DomainLoader.LoadProcesses(config, definitions);
        var goalGenerator = new GoalGenerator(config.Kinds, config.Processes);
        return new EntityEvaluator(definitions, new MessageBus(), random, goalGenerator);
    }

    private static void SetScalar(Entity entity, string attributeName, float value)
    {
        var scalar = Assert.IsType<Engine.Attributes.ScalarValue>(entity.Attributes[attributeName]);
        scalar.SetBase(value);
    }
}
