namespace Engine.Tests.Behaviors;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Resources;
using Engine.Wiring;

public class GraphAttributeQueryTests
{
    [Fact]
    public void FindProviders_ResourceRef_FindsSettlementsWithStockpile()
    {
        var world = CreateTestWorld();

        // Add sustenance stockpile at settlement coordinates
        world.Resources!.Sustenance.AddToStockpile(new HexCoord(0, 0), 100f);
        world.Resources.Sustenance.AddToStockpile(new HexCoord(2, 0), 50f);

        var consumer = world.Graph.CreateEntity(EntityKind.Npc, "generic_npc", "Npc", "c", tick: 0,
            coordinates: (0, 1)); // Near (0,0)

        var providerRef = AttributeRef.Resource("sustenance");
        var candidates = GraphAttributeQuery.FindProviders(providerRef, consumer, world);

        Assert.Equal(2, candidates.Count);
        // Closer settlement with more food should have higher weight
        Assert.True(candidates[0].Weight > candidates[1].Weight);
    }

    [Fact]
    public void FindProviders_EmptyStockpile_ReturnsEmpty()
    {
        var world = CreateTestWorld();
        // No sustenance added

        var consumer = world.Graph.CreateEntity(EntityKind.Npc, "generic_npc", "Npc", "c", tick: 0,
            coordinates: (0, 0));

        var candidates = GraphAttributeQuery.FindProviders(
            AttributeRef.Resource("sustenance"), consumer, world);

        Assert.Empty(candidates);
    }

    [Fact]
    public void FindProviders_TopN_LimitsCandidates()
    {
        var world = CreateTestWorld();

        // Add stockpile at many settlements
        for (var i = 0; i < 10; i++)
        {
            var s = world.Graph.CreateEntity(EntityKind.Settlement, "village", $"S{i}", "c", tick: 0,
                coordinates: (i - 5, 0));
            world.Resources!.Sustenance.AddToStockpile(new HexCoord(i - 5, 0), 10f + i);
        }

        var consumer = world.Graph.CreateEntity(EntityKind.Npc, "generic_npc", "Npc", "c", tick: 0,
            coordinates: (0, 0));

        var candidates = GraphAttributeQuery.FindProviders(
            AttributeRef.Resource("sustenance"), consumer, world, topN: 3);

        Assert.Equal(3, candidates.Count);
    }

    [Fact]
    public void WeightedRandomPick_ReturnsFromCandidates()
    {
        var candidates = new (EntityId Id, float Weight)[]
        {
            (new EntityId(1), 10f),
            (new EntityId(2), 1f),
        };

        var random = new Random(42);
        var picked = GraphAttributeQuery.WeightedRandomPick(candidates, random);
        Assert.NotNull(picked);
    }

    [Fact]
    public void WeightedRandomPick_EmptyReturnsNull()
    {
        var result = GraphAttributeQuery.WeightedRandomPick(
            Array.Empty<(EntityId, float)>(), new Random(42));
        Assert.Null(result);
    }

    [Fact]
    public void FindProviders_ConsumerWithNoCoordinates_ReturnsEmpty()
    {
        var world = CreateTestWorld();
        var consumer = world.Graph.CreateEntity(EntityKind.Npc, "generic_npc", "Npc", "c", tick: 0);
        // No coordinates

        var candidates = GraphAttributeQuery.FindProviders(
            AttributeRef.Resource("sustenance"), consumer, world);

        Assert.Empty(candidates);
    }

    private static StubWorldState CreateTestWorld()
    {
        var world = new StubWorldState(gridRadius: 5, withResources: true);

        // Create two settlements at known positions
        world.Graph.CreateEntity(EntityKind.Settlement, "village", "S1", "c", tick: 0,
            coordinates: (0, 0));
        world.Graph.CreateEntity(EntityKind.Settlement, "village", "S2", "c", tick: 0,
            coordinates: (2, 0));

        return world;
    }
}
