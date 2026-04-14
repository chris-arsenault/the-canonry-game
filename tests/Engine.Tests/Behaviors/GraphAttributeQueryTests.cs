namespace Engine.Tests.Behaviors;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Graph;
using Engine.Kinds;
using Engine.Wiring;

public class GraphAttributeQueryTests
{
    [Fact]
    public void FindCandidates_FindsEntitiesOfTheSpecifiedKindWithPositiveAttributeValue()
    {
        var world = CreateWorld();

        var candidates = GraphAttributeQuery.FindCandidates(
            AttributeRef.Self("population", EntityKind.Settlement),
            world);

        Assert.Equal(2, candidates.Count);
        Assert.Contains(world.Graph.GetEntitiesByKind(EntityKind.Settlement), id => candidates.Contains(id));
    }

    [Fact]
    public void FindCandidates_ExcludesEntitiesWithZeroOrNegativeValues()
    {
        var world = CreateWorld();

        var candidates = GraphAttributeQuery.FindCandidates(
            AttributeRef.Self("fame", EntityKind.Settlement),
            world);

        Assert.Single(candidates);
        var onlyCandidate = world.Graph.GetEntity(candidates[0])!;
        Assert.Equal("Positive", onlyCandidate.Name);
    }

    [Fact]
    public void FindCandidates_ExcludesInactiveEntities()
    {
        var world = CreateWorld();
        var inactive = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Inactive", "c", 0, (3, 0));
        inactive.Attributes.Add(new ScalarValue("population", 0f, 100f, 20f));
        inactive.Attributes.Add(new ScalarValue("fame", 0f, 100f, 20f));
        world.Graph.SetEntityHistorical(inactive.Id, 1);

        var candidates = GraphAttributeQuery.FindCandidates(
            AttributeRef.Self("population", EntityKind.Settlement),
            world);

        Assert.DoesNotContain(inactive.Id, candidates);
    }

    [Fact]
    public void FindCandidates_ReturnsEmptyWhenNothingMatches()
    {
        var world = CreateWorld();

        var candidates = GraphAttributeQuery.FindCandidates(
            AttributeRef.Self("nonexistent", EntityKind.Settlement),
            world);

        Assert.Empty(candidates);
    }

    private static StubWorldState CreateWorld()
    {
        var world = new StubWorldState(gridRadius: 5, withResources: true);

        var positive = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Positive", "c", 0, (0, 0));
        positive.Attributes.Add(new ScalarValue("population", 0f, 1000f, 100f));
        positive.Attributes.Add(new ScalarValue("fame", 0f, 1000f, 5f));

        var zero = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Zero", "c", 0, (1, 0));
        zero.Attributes.Add(new ScalarValue("population", 0f, 1000f, 50f));
        zero.Attributes.Add(new ScalarValue("fame", -100f, 1000f, 0f));

        var negative = world.Graph.CreateEntity(EntityKind.Settlement, "village", "Negative", "c", 0, (2, 0));
        negative.Attributes.Add(new ScalarValue("population", 0f, 1000f, 0f));
        negative.Attributes.Add(new ScalarValue("fame", -100f, 1000f, -10f));

        return world;
    }
}
