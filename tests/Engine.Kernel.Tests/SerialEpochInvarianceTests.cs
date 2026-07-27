namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Eventing;
using Engine.Graph;
using Engine.Kernel;

public sealed class SerialEpochInvarianceTests
{
    [Fact]
    public void KeyedRandomDrawsDoNotDependOnCallOrder()
    {
        var random = new KeyedRandom(seed: 73);

        var firstA = random.Int(10_000, 4, 3, 2, 101, 7, 5, 0);
        var firstB = random.Int(10_000, 4, 3, 2, 202, 7, 5, 0);
        var secondB = random.Int(10_000, 4, 3, 2, 202, 7, 5, 0);
        var secondA = random.Int(10_000, 4, 3, 2, 101, 7, 5, 0);

        Assert.Equal(firstA, secondA);
        Assert.Equal(firstB, secondB);
    }

    [Fact]
    public void AttributeReductionSumsBeforeClampingAcrossInsertionOrderAndLogicalShards()
    {
        var forward = CreateSimulation(new KernelExecutionOptions());
        var reordered = CreateSimulation(new KernelExecutionOptions
        {
            ReverseDeltaInsertionOrder = true,
            LogicalShardCount = 7,
        });

        var forwardEntity = CreateAttributeEntity(forward, defaultValue: 99f);
        var reorderedEntity = CreateAttributeEntity(reordered, defaultValue: 99f);

        ResolveCancellingAttributeDeltas(forward, forwardEntity);
        ResolveCancellingAttributeDeltas(reordered, reorderedEntity);

        Assert.Equal(FixedScalar.FromSingle(99f), forward.World.GetAttribute(forwardEntity, AttributeId));
        Assert.Equal(
            forward.World.GetAttribute(forwardEntity, AttributeId),
            reordered.World.GetAttribute(reorderedEntity, AttributeId));
    }

    [Fact]
    public void ReorderedEvaluationAndLogicalShardsConvergeToIdenticalPublishedState()
    {
        var forward = CreateSimulation(new KernelExecutionOptions());
        var reordered = CreateSimulation(new KernelExecutionOptions
        {
            ReverseEvaluationOrder = true,
            ReverseDeltaInsertionOrder = true,
            LogicalShardCount = 7,
        });
        var forwardState = ConfigureLocalEpochScenario(forward);
        var reorderedState = ConfigureLocalEpochScenario(reordered);
        using var reorderedEvents = reordered.Events.CreateCursor(KernelEventCursorStart.Latest);

        forward.AdvanceTicks(4);
        reordered.AdvanceTicks(4);

        Assert.Equal(forward.CurrentTick, reordered.CurrentTick);
        Assert.Equal(forward.WorldVersion, reordered.WorldVersion);
        Assert.Equal(forward.PhaseRevision, reordered.PhaseRevision);
        Assert.Equal(forward.World.ActiveEntityCount, reordered.World.ActiveEntityCount);
        Assert.Equal(forward.World.ActiveRelationshipCount, reordered.World.ActiveRelationshipCount);
        AssertEntityStateEqual(forward, forwardState.FirstEntity, reordered, reorderedState.FirstEntity);
        AssertEntityStateEqual(forward, forwardState.SecondEntity, reordered, reorderedState.SecondEntity);
        Assert.Equal(
            forward.World.RelationshipIsActive(forwardState.Relationship),
            reordered.World.RelationshipIsActive(reorderedState.Relationship));
        Assert.Equal(
            forward.World.RelationshipStrength(forwardState.Relationship),
            reordered.World.RelationshipStrength(reorderedState.Relationship));

        var publishedAttributes = ReadAttributeChanges(reordered, reorderedEvents);
        foreach (var tick in publishedAttributes.GroupBy(static change => change.Tick))
        {
            Assert.Equal(
                tick.Select(static change => change.EntityId).Order(),
                tick.Select(static change => change.EntityId));
        }
    }

    [Fact]
    public void WorkerCountsPublishIdenticalStateAndEvents()
    {
        var expected = RunPublishedSnapshot(workerCount: 1);
        foreach (var workerCount in new[] { 2, 4, 8, 16 })
        {
            Assert.Equal(
                expected,
                RunPublishedSnapshot(workerCount));
        }
    }

    [Fact]
    public void TickPublishesMonotonicPhaseBoundaries()
    {
        var simulation = CreateSimulation(new KernelExecutionOptions());
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var phases = new List<PhaseCompletedKernelEvent>();
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType == KernelEventType.PhaseCompleted)
            {
                phases.Add(read.Event.AsPhaseCompleted());
            }
        }

        Assert.Equal(
            new[]
            {
                TickPhaseCodes.CommandAdmission,
                TickPhaseCodes.PrepareLocalView,
                TickPhaseCodes.EvaluateLocal,
                TickPhaseCodes.ResolveLocal,
                TickPhaseCodes.Strategic,
                TickPhaseCodes.Publish,
            },
            phases.Select(static phase => phase.PhaseCode));
        Assert.Equal(
            new long[] { 1, 2, 3, 4, 5, 6 },
            phases.Select(static phase => phase.PhaseRevision));
    }

    private static Simulation CreateSimulation(KernelExecutionOptions execution) =>
        KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 32,
            RelationshipCapacity = 32,
            AttributeCapacity = 128,
            TraitCapacity = 8,
            MapCellCapacity = 32,
            MapInfluenceCapacity = 64,
            EventCapacity = 4096,
            CommandCapacity = 8,
            GambitCapacity = 4,
            GambitStepCapacity = 16,
            GambitProviderSelectorCapacity = 8,
            GeneratorCapacity = 4,
            GeneratorProvinceCapacity = 4,
            GeneratorItemCapacity = 16,
            DistrictCapacity = 8,
            LifecycleRuleCapacity = 4,
            LowAltitudeVerbRuleCapacity = 4,
            LowAltitudeVerbItemCapacity = 16,
            MovementCellCapacity = 32,
            MovementProfileRuleCapacity = 4,
            EffectRuleCapacity = 4,
            CultureSlotCapacity = 4,
            CultureSlotAllowedKindCapacity = 8,
            CultureSlotScoreRuleCapacity = 8,
            MinorActorCapacity = 8,
            MapFrontierThawRingCapacity = 32,
            MapFrontierCandidateCapacity = 8,
            GeologicalFeatureCapacity = 8,
            KnowledgeSiteCapacity = 8,
            KnowledgeGrantCapacity = 8,
            KnowledgeModifierCapacity = 16,
            MinorActorRuleCapacity = 4,
            EntityAttributeCapacity = 32,
            EntityTraitCapacity = 16,
            AttributeIdCapacity = 16,
            TraitIdCapacity = 16,
            KindIdCapacity = 16,
            CultureIdCapacity = 16,
            TerrainIdCapacity = 16,
            RelationshipRuleCapacity = 8,
            RelationshipKindIdCapacity = 16,
            Execution = execution,
            Seed = 73,
        });

    private static byte[] RunPublishedSnapshot(int workerCount)
    {
        var simulation = CreateSimulation(new KernelExecutionOptions
        {
            WorkerCount = workerCount,
        });
        try
        {
            _ = ConfigureParallelEpochScenario(simulation);
            simulation.AdvanceTicks(4);
            using var stream = new MemoryStream();
            KernelSnapshot.SaveKernelState(simulation, stream);
            return stream.ToArray();
        }
        finally
        {
            simulation.ShutdownWorkers();
        }
    }

    private static int CreateAttributeEntity(Simulation simulation, float defaultValue)
    {
        simulation.RegisterAttribute(
            EntityKindId,
            AttributeId,
            FixedScalar.FromSingle(0f),
            FixedScalar.FromSingle(100f),
            FixedScalar.FromSingle(defaultValue));
        return simulation.CreateEntity(EntityKindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
    }

    private static void ResolveCancellingAttributeDeltas(Simulation simulation, int entityId)
    {
        var deltas = new TickDeltaBuffer(simulation.Options);
        var occurrence = simulation.World.EntityOccurrenceHandle(entityId);
        var context = new KernelExecutionContext(0, simulation.StartupSourceCodeId);
        deltas.AddAttribute(new AttributeDelta(
            occurrence,
            AttributeId,
            KernelFixedPoint.AttributeUnits(10f),
            SourceKey: 1,
            Context: context));
        deltas.AddAttribute(new AttributeDelta(
            occurrence,
            AttributeId,
            KernelFixedPoint.AttributeUnits(-10f),
            SourceKey: 2,
            Context: context));

        deltas.Resolve(
            simulation,
            simulation.Options.Execution.ReverseDeltaInsertionOrder,
            simulation.Options.Execution.LogicalShardCount);
    }

    private static ScenarioState ConfigureLocalEpochScenario(Simulation simulation)
    {
        var first = CreateAttributeEntity(simulation, defaultValue: 50f);
        var second = simulation.CreateEntity(EntityKindId, 1, 1, 1, 0, 0, 0, 0, tick: 0);
        var relationship = CreateMaintainedRelationship(
            simulation,
            first,
            second);
        return new ScenarioState(first, second, relationship);
    }

    private static ScenarioState ConfigureParallelEpochScenario(
        Simulation simulation)
    {
        var scenario = ConfigureLocalEpochScenario(simulation);
        CreateAdditionalEntities(simulation);
        ConfigureAlwaysSuccessfulGambit(simulation);
        return scenario;
    }

    private static void CreateAdditionalEntities(Simulation simulation)
    {
        for (var index = 2; index < 16; index++)
        {
            _ = simulation.CreateEntity(
                EntityKindId,
                subtypeId: 1,
                cultureId: 1,
                physicalQ: index,
                physicalR: 0,
                semanticQ: 0,
                semanticR: 0,
                semanticS: 0,
                tick: 0);
        }
    }

    private static int CreateMaintainedRelationship(
        Simulation simulation,
        int first,
        int second)
    {
        simulation.Relationships.RegisterRelationshipKind(
            RelationshipKindId,
            cullThreshold: FixedScalar.FromSingle(0.05f));
        simulation.Relationships.AddMaintenanceRule(
            RelationshipKindId,
            RelationshipMaintenanceConditionCodes.Always,
            PlaneCodes.None,
            maxDistance: 0,
            threshold: FixedScalar.FromSingle(0f),
            delta: FixedScalar.FromSingle(0.03f));
        var relationship = simulation.CreateRelationship(
            RelationshipKindId,
            (int)RelationshipPolarity.Positive,
            first,
            second,
            FixedScalar.FromSingle(0.5f),
            tick: 0);
        simulation.Effects.RegisterAttributeDeltaEffect(
            EntityKindId,
            AttributeId,
            EffectConditionCodes.RelatedEntityDistance,
            RelationshipKindId,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Physical,
            maxDistance: 2,
            threshold: FixedScalar.FromSingle(0f),
            minimumRelationshipStrength: FixedScalar.FromSingle(0.1f),
            deltaPerTick: FixedScalar.FromSingle(-1f));
        return relationship;
    }

    private static void ConfigureAlwaysSuccessfulGambit(
        Simulation simulation)
    {
        var gambit = simulation.Gambits.RegisterGambit(
            EntityKindId,
            gambitId: 100,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(BehaviorPreconditionCodes.TargetIsSelf));
        simulation.Gambits.AddExpressionOp(
            gambit,
            BehaviorExpressionCodes.Literal,
            x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(
            gambit,
            BehaviorOperationCodes.AddAttribute,
            AttributeId,
            x: FixedScalar.FromSingle(1f));
    }

    private static void AssertEntityStateEqual(
        Simulation expected,
        int expectedEntity,
        Simulation actual,
        int actualEntity)
    {
        Assert.Equal(expected.World.EntityIsActive(expectedEntity), actual.World.EntityIsActive(actualEntity));
        Assert.Equal(expected.World.EntityPhysicalQ(expectedEntity), actual.World.EntityPhysicalQ(actualEntity));
        Assert.Equal(expected.World.EntityPhysicalR(expectedEntity), actual.World.EntityPhysicalR(actualEntity));
        Assert.Equal(expected.World.EntitySemanticQ(expectedEntity), actual.World.EntitySemanticQ(actualEntity));
        Assert.Equal(expected.World.EntitySemanticR(expectedEntity), actual.World.EntitySemanticR(actualEntity));
        Assert.Equal(
            expected.World.GetAttribute(expectedEntity, AttributeId),
            actual.World.GetAttribute(actualEntity, AttributeId));
    }

    private static List<(int Tick, int EntityId)> ReadAttributeChanges(
        Simulation simulation,
        KernelEventCursor cursor)
    {
        var changes = new List<(int Tick, int EntityId)>();
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType == KernelEventType.AttributeChanged)
            {
                var changed = read.Event.AsAttributeChanged();
                changes.Add((changed.Tick, changed.EntityId));
            }
        }

        return changes;
    }

    private const int EntityKindId = 1;
    private const int AttributeId = 1;
    private const int RelationshipKindId = 9;

    private readonly record struct ScenarioState(
        int FirstEntity,
        int SecondEntity,
        int Relationship);
}
