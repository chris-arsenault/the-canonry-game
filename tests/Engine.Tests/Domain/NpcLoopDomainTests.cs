namespace Engine.Tests.Domain;

using Engine.Behaviors;
using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Messages;
using Engine.Processes;
using Engine.Tests.Behaviors;

public class NpcLoopDomainTests
{
    [Fact]
    public void TestDomain_HungryNpcGeneratesEatPlanWithMoveSubGoal()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Target", "c", 0, (2, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "hunger", 20f);
        SetScalar(settlement, "sustenance", 10f);

        var generator = new GoalGenerator(config.Kinds, config.Processes);
        var goals = generator.Generate(npc, world);

        var eatGoal = Assert.Single(goals, g => g.PlannedProcessId == "npc_eat" && !g.IsSubGoal);
        var moveGoal = Assert.Single(goals, g => g.PlannedProcessId == "npc_move_to_target" && g.IsSubGoal);

        Assert.Equal(settlement.Id, eatGoal.TargetEntityId);
        Assert.Equal(settlement.Id, moveGoal.TargetEntityId);
        Assert.Equal(eatGoal.Id, moveGoal.ParentGoalId);
    }

    [Fact]
    public void TestDomain_HungryNpcMovesTowardFoodSettlement()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Target", "c", 0, (2, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "hunger", 20f);
        SetScalar(settlement, "sustenance", 10f);

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(npc, world);

        Assert.Equal("Move To Target", npc.LastEvaluation!.ExecutedProcessName);
        Assert.Equal((1, 0), npc.Coordinates);
        Assert.Equal(0, npc.GoalsGeneratedAt);
    }

    [Fact]
    public void TestDomain_HungryNpcEatsWhenAlreadyAtFoodSettlement()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5);
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Target", "c", 0, (0, 0));
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        world.Graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, npc.Id, settlement.Id, 1f, 0);
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "hunger", 20f);
        SetScalar(settlement, "sustenance", 10f);

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(npc, world);

        Assert.Equal("Eat", npc.LastEvaluation!.ExecutedProcessName);
        Assert.InRange(npc.Attributes.GetValue("hunger"), 6.8f, 9.2f);
        Assert.Equal(9f, settlement.Attributes.GetValue("sustenance"), precision: 3);
    }

    [Fact]
    public void TestDomain_CalmNpcWandersFromStandingDrive()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5);
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Home", "c", 0, (0, 0));
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        world.Graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, npc.Id, settlement.Id, 1f, 0);
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "hunger", 0f);

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(npc, world);

        Assert.Equal("Wander", npc.LastEvaluation!.ExecutedProcessName);
        Assert.NotEqual((0, 0), npc.Coordinates);
        Assert.Empty(world.Graph.OutgoingRelationships(npc.Id, "located_at"));
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
