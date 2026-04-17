namespace Engine.Tests.Domain;

using Engine.Behaviors;
using Engine.Domain.Config;
using Engine.Domain.Loader;
using Engine.Domain.TestDomain;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Messages;
using Engine.Processes;
using Engine.Resources;
using Engine.Tests.Behaviors;
using Engine.Wiring;

public class NpcLoopDomainTests
{
    [Fact]
    public void TestDomain_HungryNpcGeneratesEatPlanWithMoveSubGoal()
    {
        var config = TestDomainDefinition.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Target", "c", 0, (2, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "satiety", 20f);
        SetAmbientFood(world, settlement, 5f);

        var generator = new GoalGenerator(config.Kinds, config.Processes);
        var goals = generator.Generate(npc, world);

        var sustainGoal = Assert.Single(goals, g => g.PlannedProcessId == "npc_eat" && !g.IsSubGoal);
        var moveGoal = Assert.Single(goals, g => g.PlannedProcessId == "npc_move_to_target" && g.IsSubGoal);

        Assert.Equal(settlement.Id, sustainGoal.TargetEntityId);
        Assert.Equal(settlement.Id, moveGoal.TargetEntityId);
        Assert.Equal(sustainGoal.Id, moveGoal.ParentGoalId);
    }

    [Fact]
    public void TestDomain_HungryNpcMovesTowardEatingSettlement()
    {
        var config = TestDomainDefinition.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Target", "c", 0, (2, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "satiety", 20f);
        SetAmbientFood(world, settlement, 5f);

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(npc, world);

        Assert.Equal("Move To Target", npc.LastEvaluation!.ExecutedProcessName);
        Assert.Equal((1, 0), npc.Coordinates);
        Assert.Equal(0, npc.GoalsGeneratedAt);
    }

    [Fact]
    public void TestDomain_HungryNpcEatsAtFoodRichSettlement()
    {
        var config = TestDomainDefinition.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Target", "c", 0, (0, 0));
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        world.Graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, npc.Id, settlement.Id, 1f, 0);
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "satiety", 20f);
        SetAmbientFood(world, settlement, 5f);

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(npc, world);

        Assert.Equal("Eat", npc.LastEvaluation!.ExecutedProcessName);
        Assert.InRange(npc.Attributes.GetValue("satiety"), 30.8f, 33.2f);
    }

    [Fact]
    public void TestDomain_HungryNpcKeepsPreviousEatTargetWhenAlternativeIsOnlySlightlyBetter()
    {
        var config = TestDomainDefinition.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (1, 0));
        var current = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Current", "c", 0, (0, 0));
        var challenger = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Challenger", "c", 0, (2, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "satiety", 20f);
        SetScalar(current, "residents", 20f);
        SetScalar(challenger, "residents", 20f);
        SetAmbientFood(world, current, 5f);
        SetAmbientFood(world, challenger, 5f);

        npc.Goals.Add(new EntityGoal(
            AttributeRef.Self("satiety", EntityKind.Npc),
            current.Id,
            urgency: 0.5f,
            plannedProcessId: "npc_eat",
            isActionable: false));

        var generator = new GoalGenerator(config.Kinds, config.Processes);
        var goals = generator.Generate(npc, world)
            .Where(g => !g.IsSubGoal && g.PlannedProcessId == "npc_eat")
            .ToList();

        var root = Assert.Single(goals);
        Assert.Equal(current.Id, root.TargetEntityId);
        Assert.DoesNotContain(goals, g => g.TargetEntityId == challenger.Id);
    }

    [Fact]
    public void TestDomain_HighSatietyNpcDoesNotGenerateEatPlanPrematurely()
    {
        var config = TestDomainDefinition.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Target", "c", 0, (2, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "satiety", 95f);
        SetAmbientFood(world, settlement, 5f);

        var generator = new GoalGenerator(config.Kinds, config.Processes);
        var goals = generator.Generate(npc, world);

        Assert.DoesNotContain(goals, g => g.PlannedProcessId == "npc_eat");
        Assert.Contains(goals, g => g.PlannedProcessId == "npc_wander");
    }

    [Fact]
    public void TestDomain_CalmNpcWandersEvenWhenSettlementExists()
    {
        var config = TestDomainDefinition.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Home", "c", 0, (0, 0));
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        world.Graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, npc.Id, settlement.Id, 1f, 0);
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "satiety", 100f);

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(npc, world);

        Assert.Equal("Wander", npc.LastEvaluation!.ExecutedProcessName);
        Assert.NotEqual((0, 0), npc.Coordinates);
        Assert.Empty(world.Graph.OutgoingRelationships(npc.Id, "located_at"));
    }

    [Fact]
    public void TestDomain_NpcWandersWhenNoNamedPlacesExist()
    {
        var config = TestDomainDefinition.Create();
        var world = new StubWorldState(gridRadius: 5);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(npc, "satiety", 100f);

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(npc, world);

        Assert.Equal("Wander", npc.LastEvaluation!.ExecutedProcessName);
        Assert.NotEqual((0, 0), npc.Coordinates);
    }

    [Fact]
    public void TestDomain_LowFoodSettlementIrrigatesBeforeGrowing()
    {
        var config = TestDomainDefinition.Create();
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
        var config = TestDomainDefinition.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var settlement = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Town", "c", 0, (0, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));
        world.SpatialIndex.RebuildFromGraph();

        SetScalar(settlement, "residents", 5f);
        SetAmbientFood(world, settlement, 5f);
        SetAmbientPopulation(world, settlement, 5f);
        var residentsBefore = settlement.Attributes.GetValue("residents");

        var evaluator = CreateEvaluator(config, new FixedRandom(0.0, 0.5, 0.5, 0.5));
        evaluator.Evaluate(settlement, world);

        Assert.Equal("Grow", settlement.LastEvaluation!.ExecutedProcessName);
        Assert.True(settlement.Attributes.GetValue("residents") > residentsBefore);
        Assert.Equal(5f, GetAmbientPopulation(world, settlement), precision: 3);
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
