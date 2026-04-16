namespace Engine.Tests.Lifecycle;

using Engine.Domain.Loader;
using Engine.Domain.TestDomain;
using Engine.Kinds;
using Engine.Lifecycle;
using Engine.Tests.Behaviors;

public class CriticalAttributeEffectSystemTests
{
    [Fact]
    public void UpdateEffects_CriticalSatietyAppliesConfiguredVitalityPenalty()
    {
        var config = TestDomainDefinition.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));

        SetScalar(npc, "satiety", 10f);
        SetScalar(npc, "vitality", 50f);

        var system = new CriticalAttributeEffectSystem(world.Graph, config.Kinds);
        system.UpdateEffects();

        var maintenance = new MaintenanceSystem(world.Graph);
        maintenance.Tick();

        Assert.InRange(npc.Attributes.GetValue("vitality"), 49.79f, 49.81f);
    }

    [Fact]
    public void UpdateEffects_NonCriticalSatietyDoesNotApplyConfiguredVitalityPenalty()
    {
        var config = TestDomainDefinition.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var npc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Self", "c", 0, (0, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));

        SetScalar(npc, "satiety", 20f);
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
