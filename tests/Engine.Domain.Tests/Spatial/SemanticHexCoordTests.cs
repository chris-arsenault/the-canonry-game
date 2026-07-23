namespace Engine.Domain.Tests.Spatial;

using Engine.Domain.Spatial;

public class SemanticHexCoordTests
{
    [Fact]
    public void CubeCoordinate_SumsToZero()
    {
        var coord = new SemanticHexCoord(3, -1);
        Assert.Equal(0, coord.Q + coord.R + coord.S);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(0, 0, 3, 0, 3)]
    [InlineData(0, 0, 1, 2, 3)]
    [InlineData(-2, 1, 2, -1, 4)]
    public void Distance_ComputesCorrectly(int q1, int r1, int q2, int r2, int expected)
    {
        var a = new SemanticHexCoord(q1, r1);
        var b = new SemanticHexCoord(q2, r2);
        Assert.Equal(expected, SemanticHexCoord.Distance(a, b));
    }

    [Fact]
    public void Origin_IsZeroZero()
    {
        Assert.Equal(new SemanticHexCoord(0, 0), SemanticHexCoord.Origin);
    }

    [Fact]
    public void Neighbors_ReturnsSixDistinctNeighbors()
    {
        var origin = SemanticHexCoord.Origin;
        var neighbors = origin.Neighbors().ToList();
        Assert.Equal(6, neighbors.Count);
        Assert.Equal(6, neighbors.Distinct().Count());
        Assert.All(neighbors, n => Assert.Equal(1, SemanticHexCoord.Distance(origin, n)));
    }
}
