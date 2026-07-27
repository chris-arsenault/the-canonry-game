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
    public void AttributeOwnerCoalescesAdditiveTrafficBeforeClamping()
    {
        var simulation = CreateSimulation(new KernelExecutionOptions
        {
            OwnerPartitionCount = 7,
        });
        var entity = CreateAttributeEntity(
            simulation,
            defaultValue: 99f);
        _ = simulation.RegisterSystem(
            cadence: 1,
            new CancellingAttributeSystem(entity));

        simulation.AdvanceTicks(1);

        Assert.Equal(
            FixedScalar.FromSingle(99f),
            simulation.World.GetAttribute(entity, AttributeId));
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
                TickPhaseCodes.Publish,
            },
            phases.Select(static phase => phase.PhaseCode));
        Assert.Equal(
            new long[] { 1, 2, 3, 4, 5 },
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

    private const int EntityKindId = 1;
    private const int AttributeId = 1;

    private sealed class CancellingAttributeSystem(int entityId) :
        ISimulationSystem
    {
        public void Tick(Simulation simulation, int tick)
        {
            simulation.ModifyAttribute(
                entityId,
                AttributeId,
                FixedScalar.FromSingle(10f),
                tick);
            simulation.ModifyAttribute(
                entityId,
                AttributeId,
                FixedScalar.FromSingle(-10f),
                tick);
        }
    }
}
