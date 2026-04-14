namespace Engine.Tests.Lifecycle;

using Engine.Attributes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Lifecycle;

public class MaintenanceSystemTests
{
    [Fact]
    public void Tick_AppliesTickEffectsToAllActiveEntities()
    {
        var graph = new WorldGraph();
        var a = graph.CreateEntity(EntityKind.Settlement, "village", "A", "c", 0, (0, 0));
        var b = graph.CreateEntity(EntityKind.Settlement, "village", "B", "c", 0, (1, 0));

        var aPopulation = new ScalarValue("population", 0f, 100f, 10f);
        aPopulation.AddTickEffect(new AttributeEffect("tick", "tick", 2f));
        a.Attributes.Add(aPopulation);

        var bPopulation = new ScalarValue("population", 0f, 100f, 20f);
        bPopulation.AddTickEffect(new AttributeEffect("tick", "tick", 3f));
        b.Attributes.Add(bPopulation);

        var system = new MaintenanceSystem(graph);
        system.Tick();

        Assert.Equal(12f, a.Attributes.GetValue("population"));
        Assert.Equal(23f, b.Attributes.GetValue("population"));
    }

    [Fact]
    public void Tick_SkipsHistoricalEntities()
    {
        var graph = new WorldGraph();
        var active = graph.CreateEntity(EntityKind.Settlement, "village", "Active", "c", 0, (0, 0));
        var historical = graph.CreateEntity(EntityKind.Settlement, "village", "Historical", "c", 0, (1, 0));

        var activePopulation = new ScalarValue("population", 0f, 100f, 10f);
        activePopulation.AddTickEffect(new AttributeEffect("tick", "tick", 2f));
        active.Attributes.Add(activePopulation);

        var historicalPopulation = new ScalarValue("population", 0f, 100f, 20f);
        historicalPopulation.AddTickEffect(new AttributeEffect("tick", "tick", 3f));
        historical.Attributes.Add(historicalPopulation);

        graph.SetEntityHistorical(historical.Id, 1);

        var system = new MaintenanceSystem(graph);
        system.Tick();

        Assert.Equal(12f, active.Attributes.GetValue("population"));
        Assert.Equal(20f, historical.Attributes.GetValue("population"));
    }
}
