namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;

public sealed class UpkeepProgramTests
{
    [Fact]
    public void AttributeUpkeepEmitsAttributeChangedWithUpkeepSourceCode()
    {
        const int kindId = 1;
        const int attributeId = 1;
        const int upkeepSourceCodeId = 42;
        var simulation = new Simulation(new SimulationOptions
        {
            RuntimeCodes = new KernelRuntimeCodes(
                RelationshipCreatedCodeId: 1,
                RelationshipCulledCodeId: 2,
                RelationshipStrengthChangedCodeId: 3,
                ProcessSourceCodeId: 10,
                GambitSourceCodeId: 11,
                UpkeepSourceCodeId: upkeepSourceCodeId,
                StartupSourceCodeId: 12,
                RelationshipSourceCodeId: 13,
                GeneratorSourceCodeId: 14,
                PressureSourceCodeId: 15,
                DegeneracySourceCodeId: 16,
                PlanningSourceCodeId: 17),
        });

        simulation.RegisterAttribute(
            kindId,
            attributeId,
            minimum: 0f,
            maximum: 100f,
            defaultValue: 50f);
        simulation.Upkeep.RegisterAttributeUpkeep(kindId, attributeId, driftRate: -1f);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        Assert.True(simulation.Events.TryRead(cursor, out var read));
        var changed = read.Event.AsAttributeChanged();
        Assert.Equal(entityId, changed.EntityId);
        Assert.Equal(attributeId, changed.AttributeId);
        Assert.Equal(upkeepSourceCodeId, changed.SourceCodeId);
        Assert.Equal(50f, changed.OldValue);
        Assert.Equal(49f, changed.NewValue);
    }

    [Fact]
    public void NamedProviderSupplyModel_DisablesLocationResourceUpkeepOffset()
    {
        const int kindId = 1;
        const int attributeId = 1;
        const int resourceId = 20;
        var simulation = CreateSimulation(new KernelTuningOptions
        {
            Programs = new KernelProgramTuningOptions
            {
                Processes = new ProcessProgramTuningOptions
                {
                    ResourceSupplyModelCode = ProcessResourceSupplyModelCodes.NamedProviderNoHex,
                },
            },
        });
        simulation.RegisterAttribute(kindId, attributeId, minimum: 0f, maximum: 100f, defaultValue: 50f);
        simulation.Upkeep.RegisterAttributeUpkeep(
            kindId,
            attributeId,
            driftRate: 0f,
            locationResourceId: resourceId,
            locationResourceDriftPerUnit: 1f,
            locationResourceDriftMinOffset: 0f,
            locationResourceDriftMaxOffset: 100f);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        using (simulation.EnterExecutionContext(simulation.ProcessSourceCodeId, programId: 0))
        {
            simulation.SetResource(resourceId, 0, 0, 10f, tick: 0);
        }

        simulation.AdvanceTicks(1);

        Assert.Equal(50f, simulation.World.GetAttribute(entity, attributeId));
    }

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
        new(new SimulationOptions
        {
            EntityCapacity = 8,
            AttributeCapacity = 16,
            ResourceCapacity = 16,
            EventCapacity = 128,
            LifecycleRuleCapacity = 4,
            UpkeepRuleCapacity = 4,
            KernelTuning = tuning ?? new KernelTuningOptions(),
            Seed = 1,
        });
}
