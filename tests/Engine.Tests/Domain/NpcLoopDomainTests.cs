namespace Engine.Tests.Domain;

using Engine.Behaviors;
using Engine.Domain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Messages;
using Engine.Processes;
using Engine.Resources;
using Engine.Tests.Behaviors;

public class NpcLoopDomainTests
{
    [Fact]
    public void TestDomain_HungryNpcGeneratesSustenancePlanWithMoveSubGoal()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Target", "c", 0, (2, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "hunger", 20f);
        SetAmbientFood(world, settlement, 5f);

        var generator = new GoalGenerator(config.Kinds, config.Processes);
        var goals = generator.Generate(npc, world);

        var sustainGoal = Assert.Single(goals, g => g.PlannedProcessId == "npc_take_sustenance" && !g.IsSubGoal);
        var moveGoal = Assert.Single(goals, g => g.PlannedProcessId == "npc_move_to_target" && g.IsSubGoal);

        Assert.Equal(settlement.Id, sustainGoal.TargetEntityId);
        Assert.Equal(settlement.Id, moveGoal.TargetEntityId);
        Assert.Equal(sustainGoal.Id, moveGoal.ParentGoalId);
    }

    [Fact]
    public void TestDomain_HungryNpcMovesTowardSustenanceSettlement()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Target", "c", 0, (2, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "hunger", 20f);
        SetAmbientFood(world, settlement, 5f);

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(npc, world);

        Assert.Equal("Move To Target", npc.LastEvaluation!.ExecutedProcessName);
        Assert.Equal((1, 0), npc.Coordinates);
        Assert.Equal(0, npc.GoalsGeneratedAt);
    }

    [Fact]
    public void TestDomain_HungryNpcTakesSustenanceAtFoodRichSettlement()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Target", "c", 0, (0, 0));
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        world.Graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, npc.Id, settlement.Id, 1f, 0);
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "hunger", 20f);
        SetAmbientFood(world, settlement, 5f);

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(npc, world);

        Assert.Equal("Take Sustenance", npc.LastEvaluation!.ExecutedProcessName);
        Assert.InRange(npc.Attributes.GetValue("hunger"), 6.8f, 9.2f);
        Assert.True(settlement.Attributes.GetValue("prestige") > 0f);
    }

    [Fact]
    public void TestDomain_CalmNpcAttendsSettlementFromStandingDrive()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Home", "c", 0, (0, 0));
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        world.Graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, npc.Id, settlement.Id, 1f, 0);
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "hunger", 0f);
        var prestigeBefore = settlement.Attributes.GetValue("prestige");

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(npc, world);

        Assert.Equal("Attend Settlement", npc.LastEvaluation!.ExecutedProcessName);
        Assert.True(settlement.Attributes.GetValue("prestige") > prestigeBefore);
    }

    [Fact]
    public void TestDomain_NpcWandersWhenNoNamedPlacesExist()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "hunger", 0f);

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(npc, world);

        Assert.Equal("Wander", npc.LastEvaluation!.ExecutedProcessName);
        Assert.NotEqual((0, 0), npc.Coordinates);
    }

    [Fact]
    public void TestDomain_LowFoodSettlementIrrigatesBeforeGrowing()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", 0, (0, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(settlement, "residents", 5f);
        SetAmbientFood(world, settlement, 0.5f);
        SetAmbientPopulation(world, settlement, 5f);

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(settlement, world);

        Assert.Equal("Irrigate", settlement.LastEvaluation!.ExecutedProcessName);
        Assert.True(GetAmbientFood(world, settlement) > 0.5f);
    }

    [Fact]
    public void TestDomain_HealthySettlementGrowsDirectly()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", 0, (0, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(settlement, "residents", 5f);
        SetAmbientFood(world, settlement, 5f);
        SetAmbientPopulation(world, settlement, 5f);
        var residentsBefore = settlement.Attributes.GetValue("residents");
        var ambientPopulationBefore = GetAmbientPopulation(world, settlement);

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(settlement, world);

        Assert.Equal("Grow", settlement.LastEvaluation!.ExecutedProcessName);
        Assert.True(settlement.Attributes.GetValue("residents") > residentsBefore);
        Assert.True(GetAmbientPopulation(world, settlement) < ambientPopulationBefore);
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

    private static void SetAmbientFood(StubWorldState world, Entity settlement, float value)
    {
        Assert.NotNull(world.Resources);
        Assert.NotNull(settlement.Coordinates);
        world.Resources!.Food.SetStockpile(
            new HexCoord(settlement.Coordinates.Value.Q, settlement.Coordinates.Value.R),
            value);
    }

    private static float GetAmbientFood(StubWorldState world, Entity settlement)
    {
        Assert.NotNull(world.Resources);
        Assert.NotNull(settlement.Coordinates);
        return world.Resources!.Food.GetStockpile(
            new HexCoord(settlement.Coordinates.Value.Q, settlement.Coordinates.Value.R));
    }

    private static void SetAmbientPopulation(StubWorldState world, Entity settlement, float value)
    {
        Assert.NotNull(world.Resources);
        Assert.NotNull(settlement.Coordinates);
        world.Resources!.PopulationLayer.SetStockpile(
            new HexCoord(settlement.Coordinates.Value.Q, settlement.Coordinates.Value.R),
            value);
    }

    private static float GetAmbientPopulation(StubWorldState world, Entity settlement)
    {
        Assert.NotNull(world.Resources);
        Assert.NotNull(settlement.Coordinates);
        return world.Resources!.PopulationLayer.GetStockpile(
            new HexCoord(settlement.Coordinates.Value.Q, settlement.Coordinates.Value.R));
    }
}
