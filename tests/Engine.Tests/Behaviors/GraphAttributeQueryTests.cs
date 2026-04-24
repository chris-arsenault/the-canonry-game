namespace Engine.Tests.Behaviors;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Graph;
using Engine.Kinds;
using Engine.Wiring;

public class GraphAttributeQueryTests
{
    [Fact]
    public void FindCandidates_FindsEntitiesOfTheSpecifiedKindWithMatchingAttribute()
    {
        var world = CreateWorld();

        var candidates = GraphAttributeQuery.FindCandidates(
            AttributeRef.Self("population", TestKinds.Settlement),
            world);

        Assert.Equal(3, candidates.Count);
        Assert.Equal(world.Graph.GetEntitiesByKind(TestKinds.Settlement).Count, candidates.Count);
    }

    [Fact]
    public void FindCandidates_UsesStructuralEligibilityNotCurrentValue()
    {
        var world = CreateWorld();

        var candidates = GraphAttributeQuery.FindCandidates(
            AttributeRef.Self("fame", TestKinds.Settlement),
            world);

        Assert.Equal(3, candidates.Count);
        Assert.Contains(candidates, id => world.Graph.GetEntity(id)!.Name == "Positive");
        Assert.Contains(candidates, id => world.Graph.GetEntity(id)!.Name == "Zero");
        Assert.Contains(candidates, id => world.Graph.GetEntity(id)!.Name == "Negative");
    }

    [Fact]
    public void FindCandidates_ExcludesInactiveEntities()
    {
        var world = CreateWorld();
        var inactive = world.Graph.CreateEntity(TestKinds.Settlement, "village", "Inactive", "c", 0, (3, 0));
        inactive.Attributes.Add(new ScalarValue("population", 0f, 100f, 20f));
        inactive.Attributes.Add(new ScalarValue("fame", 0f, 100f, 20f));
        world.Graph.SetEntityHistorical(inactive.Id, 1);

        var candidates = GraphAttributeQuery.FindCandidates(
            AttributeRef.Self("population", TestKinds.Settlement),
            world);

        Assert.DoesNotContain(inactive.Id, candidates);
    }

    [Fact]
    public void FindCandidates_ReturnsEmptyWhenNothingMatches()
    {
        var world = CreateWorld();

        var candidates = GraphAttributeQuery.FindCandidates(
            AttributeRef.Self("nonexistent", TestKinds.Settlement),
            world);

        Assert.Empty(candidates);
    }

    private static StubWorldState CreateWorld()
    {
        var world = new StubWorldState(gridRadius: 5, withResources: true);

        var positive = world.Graph.CreateEntity(TestKinds.Settlement, "village", "Positive", "c", 0, (0, 0));
        positive.Attributes.Add(new ScalarValue("population", 0f, 1000f, 100f));
        positive.Attributes.Add(new ScalarValue("fame", 0f, 1000f, 5f));

        var zero = world.Graph.CreateEntity(TestKinds.Settlement, "village", "Zero", "c", 0, (1, 0));
        zero.Attributes.Add(new ScalarValue("population", 0f, 1000f, 50f));
        zero.Attributes.Add(new ScalarValue("fame", -100f, 1000f, 0f));

        var negative = world.Graph.CreateEntity(TestKinds.Settlement, "village", "Negative", "c", 0, (2, 0));
        negative.Attributes.Add(new ScalarValue("population", 0f, 1000f, 0f));
        negative.Attributes.Add(new ScalarValue("fame", -100f, 1000f, -10f));

        return world;
    }
}
