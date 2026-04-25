namespace Engine.Tests.Processes;

using Engine.Attributes;
using Engine.Domain;
using Engine.Domain.Loader;
using Engine.Eventing;
using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;
using Engine.Tests;
using Engine.Tests.Behaviors;
using Engine.Wiring;

public class MutationTests
{
    [Fact]
    public void ModifySelfAttributeMutation_WithZeroVariance_AppliesExactDelta()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        var context = new TestMutationContext(self, worldState: world, random: new FixedRandom(0.5));

        new ModifySelfAttributeMutation("population", 4f, variance: 0f).Execute(context);

        Assert.Equal(14f, self.Attributes.GetValue("population"));
    }

    [Fact]
    public void ModifySelfAttributeMutation_WithVariance_ProducesDistinctResults()
    {
        var world = new StubWorldState(gridRadius: 5);
        var random = new Random(42);
        var values = new HashSet<float>();

        for (var i = 0; i < 10; i++)
        {
            var self = CreateSettlement(world, $"Self_{i}", (i, 0), population: 10f);
            var context = new TestMutationContext(self, worldState: world, random: random);
            new ModifySelfAttributeMutation("population", 4f, variance: 0.25f).Execute(context);
            values.Add(self.Attributes.GetValue("population"));
        }

        Assert.True(values.Count > 1);
    }

    [Fact]
    public void ModifyTargetAttributeMutation_ModifiesResolvedTarget()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        var target = CreateSettlement(world, "Target", (1, 0), population: 10f);
        var context = new TestMutationContext(self, target, worldState: world, random: new FixedRandom(0.5));

        new ModifyTargetAttributeMutation("population", -3f, variance: 0f).Execute(context);

        Assert.Equal(7f, target.Attributes.GetValue("population"));
    }

    [Fact]
    public void ModifyTargetAttributeMutation_IsNoOpWithoutTarget()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        var context = new TestMutationContext(self, worldState: world);

        new ModifyTargetAttributeMutation("population", -3f, variance: 0f).Execute(context);

        Assert.Equal(10f, self.Attributes.GetValue("population"));
    }

    [Fact]
    public void CalculateMutation_StoresVariablesAndSubsequentMutationsCanReadThem()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 8f);
        var target = CreateSettlement(world, "Target", (1, 0), population: 6f, fame: 3f);
        var context = new TestMutationContext(self, target, worldState: world, random: new FixedRandom(0.5));

        new CalculateMutation("delta", Expr.Sub(Expr.Self("fame"), Expr.Target("fame"))).Execute(context);
        new CalculateMutation("double_delta", Expr.Mul(Expr.Var("delta"), Expr.Lit(2f))).Execute(context);
        new ModifySelfByVariableMutation("population", "delta").Execute(context);
        new ModifyTargetByVariableMutation("population", "double_delta").Execute(context);

        Assert.Equal(5f, context.GetVariable("delta"));
        Assert.Equal(10f, context.GetVariable("double_delta"));
        Assert.Equal(15f, self.Attributes.GetValue("population"));
        Assert.Equal(16f, target.Attributes.GetValue("population"));
    }

    [Fact]
    public void ModifyTargetByVariableMutation_UsesZeroWhenVariableIsMissing()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f);
        var target = CreateSettlement(world, "Target", (1, 0), population: 6f);
        var context = new TestMutationContext(self, target, worldState: world, random: new FixedRandom(0.5));

        new ModifyTargetByVariableMutation("population", "missing").Execute(context);

        Assert.Equal(6f, target.Attributes.GetValue("population"));
    }

    [Fact]
    public void ModifyTargetByVariableMutation_WritesPrimitiveAttributeChangedEventFromNameAdapter()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 8f);
        var target = CreateSettlement(world, "Target", (1, 0), population: 6f, fame: 3f);
        var kernelEvents = new KernelEventLog(4, KernelEventOverflowPolicy.RejectWrite);
        var cursor = kernelEvents.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var domainIds = new DomainIdRegistry();
        var populationId = domainIds.RegisterAttribute("population");
        domainIds.RegisterSourceCode(DomainRuntimeIdLoader.ProcessSourceCode);
        domainIds.RegisterSourceCode(DomainRuntimeIdLoader.CardSourceCode);
        target.Attributes.Add(new ScalarValue("population", 0f, 100f, target.Attributes.GetValue("population"), populationId));

        var goal = new EntityGoal(AttributeRef.Target("population", TestKinds.Settlement), target.Id, urgency: 1f);
        var processId = ProcessRuntimeId.FromStableName("test_process");
        var context = new ProcessMutationContext(
            self,
            world,
            random: new FixedRandom(0.5),
            goal: goal,
            definitionId: processId,
            kernelEvents: kernelEvents,
            domainIds: domainIds);

        new CalculateMutation("delta", Expr.Lit(4f)).Execute(context);
        new ModifyTargetByVariableMutation("population", "delta").Execute(context);

        Assert.Equal(10f, target.Attributes.GetValue("population"));
        Assert.True(kernelEvents.TryRead(cursor, out var eventValue));
        Assert.Equal(KernelEventType.AttributeChanged, eventValue.Event.EventType);
        var changed = eventValue.Event.AsAttributeChanged();
        Assert.Equal(target.Id.Value, changed.EntityId);
        Assert.Equal(populationId.Value, changed.AttributeId);
        Assert.Equal(6f, changed.OldValue);
        Assert.Equal(10f, changed.NewValue);
    }

    [Fact]
    public void ModifyTargetByVariableMutation_WritesPrimitiveAttributeChangedEventThroughProcessContext()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateSettlement(world, "Self", (0, 0), population: 10f, fame: 8f);
        var target = CreateSettlement(world, "Target", (1, 0), population: 6f, fame: 3f);
        var kernelEvents = new KernelEventLog(4, KernelEventOverflowPolicy.RejectWrite);
        var cursor = kernelEvents.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var domainIds = new DomainIdRegistry();
        var populationId = domainIds.RegisterAttribute("population");
        var processSourceId = domainIds.RegisterSourceCode(DomainRuntimeIdLoader.ProcessSourceCode);
        domainIds.RegisterSourceCode(DomainRuntimeIdLoader.CardSourceCode);
        target.Attributes.Add(new ScalarValue("population", 0f, 100f, target.Attributes.GetValue("population"), populationId));

        var goal = new EntityGoal(AttributeRef.Target("population", TestKinds.Settlement), target.Id, urgency: 1f);
        var context = new ProcessMutationContext(
            self,
            world,
            random: new FixedRandom(0.5),
            goal: goal,
            definitionId: ProcessRuntimeId.FromStableName("test_process"),
            kernelEvents: kernelEvents,
            domainIds: domainIds);

        new CalculateMutation("delta", Expr.Lit(4f)).Execute(context);
        new ModifyTargetByVariableMutation("population", "delta").Execute(context);

        Assert.True(kernelEvents.TryRead(cursor, out var eventValue));
        Assert.Equal(KernelEventType.AttributeChanged, eventValue.Event.EventType);
        var changed = eventValue.Event.AsAttributeChanged();
        Assert.Equal(target.Id.Value, changed.EntityId);
        Assert.Equal(populationId.Value, changed.AttributeId);
        Assert.Equal(processSourceId.Value, changed.SourceCodeId);
        Assert.Equal(6f, changed.OldValue);
        Assert.Equal(10f, changed.NewValue);
        Assert.True(changed.WorldVersion > 0);
    }

    [Fact]
    public void RemovedDomainSpecificMutations_DoNotExist()
    {
        var mutationTypeNames = typeof(IMutation).Assembly.GetTypes().Select(t => t.Name).ToHashSet();

        Assert.DoesNotContain("StockpileMutation", mutationTypeNames);
        Assert.DoesNotContain("ConsumeResourceAtSelfMutation", mutationTypeNames);
    }

    [Fact]
    public void MoveTowardTargetMutation_MovesOneStepAndUpdatesSpatialIndex()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreatePenguin(world, "Self", (0, 0));
        var target = CreateSettlement(world, "Target", (2, 0), population: 10f);
        world.SpatialIndex.RebuildFromGraph();

        new MoveTowardTargetMutation(locationRelationshipKind: null, locationEntityKind: null)
            .Execute(new TestMutationContext(self, target, worldState: world));

        Assert.Equal((1, 0), self.Coordinates);
        Assert.DoesNotContain(self.Id, world.SpatialIndex.EntitiesAtCell(new Engine.Map.HexCoord(0, 0)));
        Assert.Contains(self.Id, world.SpatialIndex.EntitiesAtCell(new Engine.Map.HexCoord(1, 0)));
    }

    [Fact]
    public void MoveTowardTargetMutation_SynchronizesLocatedAtWhenEnteringSettlement()
    {
        var world = new StubWorldState(gridRadius: 5);
        var currentSettlement = CreateSettlement(world, "Current", (0, 0), population: 10f);
        var targetSettlement = CreateSettlement(world, "Target", (1, 0), population: 10f);
        var self = CreatePenguin(world, "Self", (0, 0));
        world.Graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, self.Id, currentSettlement.Id, 1f, 0);
        world.SpatialIndex.RebuildFromGraph();

        new MoveTowardTargetMutation("located_at", TestKinds.Settlement)
            .Execute(new TestMutationContext(self, targetSettlement, worldState: world));

        Assert.Equal((1, 0), self.Coordinates);
        var activeLocatedAt = Assert.Single(world.Graph.OutgoingRelationships(self.Id, "located_at"));
        Assert.Equal(targetSettlement.Id, activeLocatedAt.TargetId);
    }

    [Fact]
    public void WanderMutation_MovesToWalkableNeighborAndClearsLocatedAtWhenLeavingSettlement()
    {
        var world = new StubWorldState(gridRadius: 5);
        var settlement = CreateSettlement(world, "Home", (0, 0), population: 10f);
        var self = CreatePenguin(world, "Self", (0, 0));
        world.Graph.CreateRelationship("located_at", RelationshipPolarity.Neutral, self.Id, settlement.Id, 1f, 0);
        world.SpatialIndex.RebuildFromGraph();

        new WanderMutation("located_at", TestKinds.Settlement).Execute(new TestMutationContext(
            self,
            worldState: world,
            random: new FixedRandom(0.0)));

        Assert.Equal((1, 0), self.Coordinates);
        Assert.Empty(world.Graph.OutgoingRelationships(self.Id, "located_at"));
        Assert.DoesNotContain(self.Id, world.SpatialIndex.EntitiesAtCell(new Engine.Map.HexCoord(0, 0)));
        Assert.Contains(self.Id, world.SpatialIndex.EntitiesAtCell(new Engine.Map.HexCoord(1, 0)));
    }

    [Fact]
    public void MoveTowardTargetOnSemanticPlaneMutation_WritesPrimitiveMovementEvent()
    {
        var world = new StubWorldState(gridRadius: 5);
        var self = CreateFaction(world, "Self");
        var target = CreateFaction(world, "Target");
        self.SemanticPosition = new Engine.Map.SemanticHexCoord(0, 0);
        target.SemanticPosition = new Engine.Map.SemanticHexCoord(2, 0);
        var kernelEvents = new KernelEventLog(4, KernelEventOverflowPolicy.RejectWrite);
        var cursor = kernelEvents.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var domainIds = new DomainIdRegistry();
        domainIds.RegisterSourceCode(DomainRuntimeIdLoader.ProcessSourceCode);
        domainIds.RegisterSourceCode(DomainRuntimeIdLoader.CardSourceCode);

        var goal = new EntityGoal(AttributeRef.Target("reach", TestKinds.Faction), target.Id, urgency: 1f);
        var context = new ProcessMutationContext(
            self,
            world,
            goal: goal,
            definitionId: ProcessRuntimeId.FromStableName("semantic_move"),
            kernelEvents: kernelEvents,
            domainIds: domainIds);

        new MoveTowardTargetOnSemanticPlaneMutation().Execute(context);

        Assert.True(kernelEvents.TryRead(cursor, out var eventValue));
        Assert.Equal(KernelEventType.EntityMoved, eventValue.Event.EventType);
        var moved = eventValue.Event.AsEntityMoved();
        Assert.Equal(self.Id.Value, moved.EntityId);
        Assert.Equal(1, moved.PlaneCode);
        Assert.Equal(0, moved.FromQ);
        Assert.Equal(0, moved.FromR);
        Assert.Equal(0, moved.FromS);
        Assert.Equal(1, moved.ToQ);
        Assert.Equal(0, moved.ToR);
        Assert.Equal(-1, moved.ToS);
        Assert.Equal(new Engine.Map.SemanticHexCoord(1, 0), self.SemanticPosition);
    }

    [Fact]
    public void ModifyRelatedEntitiesAttributeMutation_ModifiesOutgoingRelatedEntitiesFromSelf()
    {
        var world = new StubWorldState(gridRadius: 5);
        var penguin = CreatePenguin(world, "Npc", (0, 0));
        var faction = CreateFaction(world, "Faction");
        world.Graph.CreateRelationship("loyal_to", RelationshipPolarity.Positive, penguin.Id, faction.Id, 1f, 0);
        var context = new TestMutationContext(penguin, worldState: world, random: new FixedRandom(0.5));

        new ModifyRelatedEntitiesAttributeMutation(
            "loyal_to",
            RelationshipTraversalDirection.Outgoing,
            "support",
            4f,
            variance: 0f).Execute(context);

        Assert.Equal(44f, faction.Attributes.GetValue("support"));
    }

    [Fact]
    public void ModifyRelatedEntitiesAttributeMutation_CanUseTargetAsAnchorForIncomingTraversal()
    {
        var world = new StubWorldState(gridRadius: 5);
        var faction = CreateFaction(world, "Faction");
        var region = CreateRegion(world, "Region");
        var settlement = CreateSettlement(world, "Settlement", (0, 0), population: 10f);
        settlement.Attributes.Add(new ScalarValue("security", 0f, 100f, 20f));
        world.Graph.CreateRelationship("contains", RelationshipPolarity.Neutral, region.Id, settlement.Id, 1f, 0);
        var context = new TestMutationContext(faction, settlement, worldState: world, random: new FixedRandom(0.5));

        new ModifyRelatedEntitiesAttributeMutation(
            "contains",
            RelationshipTraversalDirection.Incoming,
            "holding",
            3f,
            useTargetEntity: true,
            variance: 0f).Execute(context);

        Assert.Equal(53f, region.Attributes.GetValue("holding"));
    }

    private static Entity CreateSettlement(
        StubWorldState world,
        string name,
        (int Q, int R) coordinates,
        float population = 0f,
        float fame = 0f)
    {
        var entity = world.Graph.CreateEntity(TestKinds.Settlement, "village", name, "c", 0, coordinates);
        entity.Attributes.Add(new ScalarValue("population", 0f, 100f, population));
        entity.Attributes.Add(new ScalarValue("fame", 0f, 100f, fame));
        return entity;
    }

    private static Entity CreatePenguin(StubWorldState world, string name, (int Q, int R) coordinates)
    {
        var entity = world.Graph.CreateEntity(TestKinds.Penguin, "warrior", name, "c", 0, coordinates);
        entity.Attributes.Add(new ScalarValue("hp", 0f, 100f, 100f));
        entity.Attributes.Add(new ScalarValue("warmth", 0f, 100f, 0f));
        return entity;
    }

    private static Entity CreateFaction(StubWorldState world, string name)
    {
        var entity = world.Graph.CreateEntity(TestKinds.Faction, "order", name, "c", 0, null);
        entity.Attributes.Add(new ScalarValue("support", 0f, 100f, 40f));
        entity.Attributes.Add(new ScalarValue("reach", 0f, 100f, 30f));
        return entity;
    }

    private static Entity CreateRegion(StubWorldState world, string name)
    {
        var entity = world.Graph.CreateEntity(TestKinds.Region, "plains", name, "c", 0, null);
        entity.Attributes.Add(new ScalarValue("holding", 0f, 100f, 50f));
        entity.Attributes.Add(new ScalarValue("stability", 0f, 100f, 80f));
        return entity;
    }
}
