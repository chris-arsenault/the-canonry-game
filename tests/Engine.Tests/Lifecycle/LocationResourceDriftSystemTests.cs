namespace Engine.Tests.Lifecycle;

using Engine.Domain;
using Engine.Kinds;
using Engine.Lifecycle;
using Engine.Tests.Behaviors;

public class LocationResourceDriftSystemTests
{
    [Fact]
    public void UpdateDriftEffects_HighFoodHexReducesHungerGrowthMoreThanLowFoodHex()
    {
        var config = TestDomain.Create();
        var world = new StubWorldState(gridRadius: 5, withResources: true);
        var richNpc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Rich", "c", 0, (0, 0));
        var poorNpc = world.Graph.CreateEntity(EntityKind.Npc, "warrior", "Poor", "c", 0, (1, 0));
        DomainLoader.InitializeEntityAttributes(world.Graph, config, new Random(42));

        world.Resources!.Food.SetStockpile(new Engine.Map.HexCoord(0, 0), 5f);
        world.Resources!.Food.SetStockpile(new Engine.Map.HexCoord(1, 0), 0f);

        var system = new LocationResourceDriftSystem(world.Graph, config.Kinds, world.Resources);
        system.InitializeEffects();
        system.UpdateDriftEffects();

        var maintenance = new MaintenanceSystem(world.Graph);
        maintenance.Tick();

        Assert.InRange(richNpc.Attributes.GetValue("hunger"), 0.14f, 0.16f);
        Assert.InRange(poorNpc.Attributes.GetValue("hunger"), 0.34f, 0.36f);
    }
}
