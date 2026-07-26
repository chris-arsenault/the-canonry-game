namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class LifecycleResolutionTests
{
    [Fact]
    public void LateLifeAgingModel_StartsWithLowerEarlyDrain()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        var lateLife = CreateSimulation(new KernelTuningOptions
        {
            Programs = new KernelProgramTuningOptions
            {
                Lifecycle = new LifecycleTuningOptions
                {
                    AgingModelCode = LifecycleAgingModelCodes.LateLife,
                },
            },
        });
        var linear = CreateSimulation();
        var lateLifeEntity = RegisterLifecycleEntity(lateLife, kindId, healthAttributeId);
        var linearEntity = RegisterLifecycleEntity(linear, kindId, healthAttributeId);

        lateLife.AdvanceTicks(1);
        linear.AdvanceTicks(1);

        Assert.True(
            lateLife.World.GetAttribute(lateLifeEntity, healthAttributeId) >
            linear.World.GetAttribute(linearEntity, healthAttributeId));
    }

    private static int RegisterLifecycleEntity(
        Simulation simulation,
        int kindId,
        int healthAttributeId)
    {
        simulation.RegisterAttribute(
            kindId,
            healthAttributeId,
            minimum: 0,
            maximum: 100,
            defaultValue: 100);
        simulation.Effects.RegisterLifecycle(
            kindId,
            healthAttributeId,
            targetTicks: 100,
            nominalHealthUnits: KernelFixedPoint.AttributeUnits(100f),
            flatDrainShare: FixedScalar.FromSingle(0.2f),
            variance: 0);
        return simulation.CreateEntity(
            kindId,
            subtypeId: 1,
            cultureId: 1,
            0,
            0,
            0,
            0,
            0,
            tick: 0);
    }

    private static Simulation CreateSimulation(KernelTuningOptions? tuning = null) =>
        KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 8,
            RelationshipCapacity = 16,
            AttributeCapacity = 16,
            EventCapacity = 128,
            LifecycleRuleCapacity = 4,
            EffectRuleCapacity = 4,
            KernelTuning = tuning ?? new KernelTuningOptions(),
            Seed = 1,
        });
}
