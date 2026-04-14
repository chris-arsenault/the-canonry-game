namespace Engine.Tests.Behaviors;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Graph;
using Engine.Kinds;
using Engine.Wiring;

public class GraphAttributeQueryTests
{
    [Fact]
    public void FindCandidates_FindsSettlementsWithAttribute()
    {
        var world = CreateTestWorld();

        var candidates = GraphAttributeQuery.FindCandidates(
            AttributeRef.Self("population", EntityKind.Settlement), world);

        Assert.Equal(2, candidates.Count);
    }

    [Fact]
    public void FindCandidates_ExcludesZeroValues()
    {
        var world = CreateTestWorld();

        var candidates = GraphAttributeQuery.FindCandidates(
            AttributeRef.Self("fame", EntityKind.Settlement), world);

        Assert.Empty(candidates);
    }

    [Fact]
    public void FindCandidates_ReturnsEmpty_WhenNoMatch()
    {
        var world = CreateTestWorld();

        var candidates = GraphAttributeQuery.FindCandidates(
            AttributeRef.Self("nonexistent", EntityKind.Settlement), world);

        Assert.Empty(candidates);
    }

    private static StubWorldState CreateTestWorld()
    {
        var world = new StubWorldState(gridRadius: 5, withResources: true);

        var s1 = world.Graph.CreateEntity(EntityKind.Settlement, "village", "S1", "c", tick: 0, coordinates: (0, 0));
        s1.Attributes.Add(new ScalarValue("population", 0, 10000, 100));
        s1.Attributes.Add(new ScalarValue("fame", 0, 1000, 0));

        var s2 = world.Graph.CreateEntity(EntityKind.Settlement, "village", "S2", "c", tick: 0, coordinates: (2, 0));
        s2.Attributes.Add(new ScalarValue("population", 0, 10000, 50));
        s2.Attributes.Add(new ScalarValue("fame", 0, 1000, 0));

        return world;
    }
}
