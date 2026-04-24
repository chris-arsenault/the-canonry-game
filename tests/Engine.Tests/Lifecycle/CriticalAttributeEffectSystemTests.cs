namespace Engine.Tests.Lifecycle;

using Engine.Domain.Loader;
using Engine.Domain.IceRemembers;
using Engine.Kinds;
using Engine.Lifecycle;
using Engine.Tests.Behaviors;

public class CriticalAttributeEffectSystemTests
{
    [Fact]
    public void UpdateEffects_CriticalWarmthAppliesConfiguredVitalityPenalty()
    {
        var config = IceRemembersDefinition.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var npc = world.Graph.CreateEntity(TestKinds.Penguin, "warrior", "Self", "c", 0, (0, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));

        SetScalar(npc, "warmth", 10f);
        SetScalar(npc, "vitality", 50f);

        var system = new CriticalAttributeEffectSystem(world.Graph, config.Kinds);
        system.UpdateEffects();

        var maintenance = new MaintenanceSystem(world.Graph);
        maintenance.Tick();

        Assert.InRange(npc.Attributes.GetValue("vitality"), 49.79f, 49.81f);
    }

    [Fact]
    public void UpdateEffects_NonCriticalWarmthDoesNotApplyConfiguredVitalityPenalty()
    {
        var config = IceRemembersDefinition.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var npc = world.Graph.CreateEntity(TestKinds.Penguin, "warrior", "Self", "c", 0, (0, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));

        SetScalar(npc, "warmth", 20f);
        SetScalar(npc, "vitality", 50f);

        var system = new CriticalAttributeEffectSystem(world.Graph, config.Kinds);
        system.UpdateEffects();

        var maintenance = new MaintenanceSystem(world.Graph);
        maintenance.Tick();

        Assert.InRange(npc.Attributes.GetValue("vitality"), 49.99f, 50.01f);
    }

    private static void SetScalar(Engine.Graph.Entity entity, string attributeName, float value)
    {
        var scalar = Assert.IsType<Engine.Attributes.ScalarValue>(entity.Attributes[attributeName]);
        scalar.SetBase(value);
    }
}
