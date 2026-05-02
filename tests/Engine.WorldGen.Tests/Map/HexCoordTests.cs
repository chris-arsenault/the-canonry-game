namespace Engine.WorldGen.Tests.Map;

using Engine.WorldGen;

public class HexCoordTests
{
    [Fact]
    public void CubeCoordinate_SumsToZero()
    {
        var coord = new HexCoord(3, -1);
        Assert.Equal(0, coord.Q + coord.R + coord.S);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(0, 0, 3, 0, 3)]
    [InlineData(0, 0, 1, 2, 3)]
    [InlineData(-2, 1, 2, -1, 4)]
    public void Distance_ComputesCorrectly(int q1, int r1, int q2, int r2, int expected)
    {
        var a = new HexCoord(q1, r1);
        var b = new HexCoord(q2, r2);
        Assert.Equal(expected, HexCoord.Distance(a, b));
    }

    [Fact]
    public void Distance_IsSymmetric()
    {
        var first = new HexCoord(1, 2);
        var second = new HexCoord(-3, 1);
        var forward = HexCoord.Distance(first, second);
        var reverse = HexCoord.Distance(second, first);

        Assert.Equal(forward, reverse);
    }

    [Fact]
    public void Neighbors_Returns6()
    {
        var center = new HexCoord(0, 0);
        var neighbors = center.Neighbors().ToList();
        Assert.Equal(6, neighbors.Count);
    }

    [Fact]
    public void Neighbors_AllAtDistance1()
    {
        var center = new HexCoord(0, 0);
        foreach (var neighbor in center.Neighbors())
        {
            Assert.Equal(1, HexCoord.Distance(center, neighbor));
        }
    }

    [Fact]
    public void CoordsInRadius0_ReturnsOnlyCenter()
    {
        var coords = HexCoord.CoordsInRadius(new HexCoord(0, 0), 0).ToList();
        Assert.Single(coords);
        Assert.Equal(new HexCoord(0, 0), coords[0]);
    }

    [Fact]
    public void CoordsInRadius1_Returns7()
    {
        var coords = HexCoord.CoordsInRadius(new HexCoord(0, 0), 1).ToList();
        Assert.Equal(7, coords.Count); // 1 center + 6 ring
    }

    [Fact]
    public void CoordsInRadius2_Returns19()
    {
        var coords = HexCoord.CoordsInRadius(new HexCoord(0, 0), 2).ToList();
        Assert.Equal(19, coords.Count); // 1 + 6 + 12
    }

    [Theory]
    [InlineData(5, 91)]   // 3*25 + 15 + 1
    [InlineData(10, 331)] // 3*100 + 30 + 1
    public void CoordsInRadius_MatchesFormula(int radius, int expected)
    {
        var count = HexCoord.CoordsInRadius(new HexCoord(0, 0), radius).Count();
        Assert.Equal(expected, count);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        Assert.Equal("(3,-1)", new HexCoord(3, -1).ToString());
    }
}
