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
                GeneratorSourceCodeId: 14),
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
}
