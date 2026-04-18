namespace Engine.Tests.Map;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;

public class SemanticPlaneIndexTests
{
    [Fact]
    public void OriginFree_ReturnsOrigin()
    {
        var index = CreateIndex(out _);

        var result = index.FindFreeSemanticHex(SemanticHexCoord.Origin);

        Assert.Equal(SemanticHexCoord.Origin, result);
    }

    [Fact]
    public void OriginOccupied_ReturnsNearestRing1FreeCell()
    {
        var index = CreateIndex(out var graph);
        PlaceEntity(graph, SemanticHexCoord.Origin);

        var result = index.FindFreeSemanticHex(SemanticHexCoord.Origin);

        Assert.NotNull(result);
        Assert.Equal(1, SemanticHexCoord.Distance(SemanticHexCoord.Origin, result!.Value));
    }

    [Fact]
    public void Surrounded_ReturnsRing2Cell()
    {
        var origin = SemanticHexCoord.Origin;
        var index = CreateIndex(out var graph);
        foreach (var coord in SemanticHexCoord.CoordsInRadius(origin, 1))
        {
            PlaceEntity(graph, coord);
        }

        var result = index.FindFreeSemanticHex(origin);

        Assert.NotNull(result);
        Assert.Equal(2, SemanticHexCoord.Distance(origin, result!.Value));
    }

    [Fact]
    public void FullyExhaustedWithinMaxRadius_ReturnsNull()
    {
        var origin = SemanticHexCoord.Origin;
        var index = CreateIndex(out var graph);
        foreach (var coord in SemanticHexCoord.CoordsInRadius(origin, 1))
        {
            PlaceEntity(graph, coord);
        }

        var result = index.FindFreeSemanticHex(origin, maxRadius: 1);

        Assert.Null(result);
    }

    [Fact]
    public void HistoricalEntities_DoNotBlockFreeHexSearch()
    {
        var index = CreateIndex(out var graph);
        var entity = PlaceEntity(graph, SemanticHexCoord.Origin);
        graph.SetEntityHistorical(entity.Id, tick: 1);

        var result = index.FindFreeSemanticHex(SemanticHexCoord.Origin);

        Assert.Equal(SemanticHexCoord.Origin, result);
    }

    private static SemanticPlaneIndex CreateIndex(out WorldGraph graph)
    {
        graph = new WorldGraph();
        return new SemanticPlaneIndex(graph);
    }

    private static Entity PlaceEntity(WorldGraph graph, SemanticHexCoord coord)
    {
        var entity = graph.CreateEntity(
            EntityKind.Npc,
            "generic_npc",
            $"entity_{coord.Q}_{coord.R}",
            "culture_a",
            tick: 0);
        entity.SemanticPosition = coord;
        return entity;
    }
}
