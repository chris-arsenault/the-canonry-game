namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class LifecycleResolutionTests
{
    [Fact]
    public void LateLifeAgingModel_StartsWithLowerEarlyDrain()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        var simulation = CreateSimulation(new KernelTuningOptions
        {
            Programs = new KernelProgramTuningOptions
            {
                Lifecycle = new LifecycleTuningOptions
                {
                    AgingModelCode = LifecycleAgingModelCodes.LateLife,
                },
            },
        });
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(100f));
        simulation.Effects.RegisterLifecycle(kindId, healthAttributeId, baseRate: FixedScalar.FromSingle(1f), ageCoefficient: FixedScalar.FromSingle(1f), variance: FixedScalar.FromSingle(0f));
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.InRange(simulation.World.GetAttribute(entity, healthAttributeId), FixedScalar.FromSingle(99.49f), FixedScalar.FromSingle(99.50f));
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
