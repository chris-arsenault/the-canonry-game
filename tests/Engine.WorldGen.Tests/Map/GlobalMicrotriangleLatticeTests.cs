namespace Engine.WorldGen.Tests.Map;

public sealed class GlobalMicrotriangleLatticeTests
{
    private static readonly PhysicalGeographyGrid Grid =
        PhysicalGeographyGrid.CreateWithPadding(1_200, 12);

    [Fact]
    public void EveryTriangleEdge_IsReciprocal()
    {
        var checkedEdges = 0;
        for (var triangle = 0; triangle < Grid.TriangleCount; triangle++)
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = Grid.NeighborTriangleIndex(triangle, edge);
                if (neighbor < 0)
                {
                    continue;
                }

                Assert.True(Grid.EdgeLeadingTo(neighbor, triangle) >= 0);
                checkedEdges++;
            }
        }

        Assert.True(checkedEdges > 0);
    }

    [Fact]
    public void EveryNeighboringCentroid_IsOneGenerationUnitApart()
    {
        for (var triangle = 0; triangle < Grid.TriangleCount; triangle++)
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = Grid.NeighborTriangleIndex(triangle, edge);
                if (neighbor < 0)
                {
                    continue;
                }

                var dx = Grid.TriangleX(neighbor) - Grid.TriangleX(triangle);
                var dy = Grid.TriangleY(neighbor) - Grid.TriangleY(triangle);
                Assert.InRange(Math.Sqrt((dx * dx) + (dy * dy)), 0.99999d, 1.00001d);
            }
        }
    }

    [Fact]
    public void CoordinateNeighbors_MatchDenseNeighborIndexes()
    {
        for (var triangle = 0; triangle < Grid.TriangleCount; triangle++)
        {
            var coord = Grid.TriangleCoordAt(triangle);
            for (var edge = 0; edge < 3; edge++)
            {
                var expectedCoord = coord.Neighbor(edge);
                var neighbor = Grid.NeighborTriangleIndex(triangle, edge);
                Assert.Equal(
                    Grid.TryGetTriangleIndex(expectedCoord, out var expected),
                    neighbor >= 0);
                if (neighbor >= 0)
                {
                    Assert.Equal(expected, neighbor);
                }
            }
        }
    }

    [Fact]
    public void ContentDomain_IsExactlyTheRequestedTriangleBudget()
    {
        Assert.Equal(
            Grid.ContentTriangleCount,
            Enumerable.Range(0, Grid.TriangleCount)
                .Count(Grid.TriangleIsInsideContentDomain));
        Assert.True(Grid.TriangleCount > Grid.ContentTriangleCount);
    }
}
