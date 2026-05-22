namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class UpkeepProgramTests
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
                Upkeep = new UpkeepProgramTuningOptions
                {
                    AgingModelCode = UpkeepAgingModelCodes.LateLife,
                },
            },
        });
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: 0f, maximum: 100f, defaultValue: 100f);
        simulation.Upkeep.RegisterLifecycle(kindId, healthAttributeId, baseRate: 1f, ageCoefficient: 1f, variance: 0f);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.InRange(simulation.World.GetAttribute(entity, healthAttributeId), 99.49f, 99.50f);
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
