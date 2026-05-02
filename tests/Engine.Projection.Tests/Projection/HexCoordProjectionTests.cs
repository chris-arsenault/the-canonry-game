namespace Engine.Projection.Tests.Projection;

public sealed class HexCoordProjectionTests
{
    [Fact]
    public void Distance_UsesCubeCoordinateMetric()
    {
        var origin = new HexCoord(0, 0);
        var far = new HexCoord(3, -1);

        Assert.Equal(0, HexCoord.Distance(origin, origin));
        Assert.Equal(3, HexCoord.Distance(origin, far));
        Assert.Equal(3, HexCoord.Distance(far, origin));
        Assert.Equal(3, far.DistanceFromOrigin);
    }

    [Fact]
    public void Neighbors_ReturnsSixAdjacentHexesInDirectionOrder()
    {
        var center = new HexCoord(2, -1);

        var neighbors = center.Neighbors().ToArray();

        Assert.Equal(
            [
                new HexCoord(3, -1),
                new HexCoord(3, -2),
                new HexCoord(2, -2),
                new HexCoord(1, -1),
                new HexCoord(1, 0),
                new HexCoord(2, 0),
            ],
            neighbors);
        Assert.All(neighbors, neighbor => Assert.Equal(1, HexCoord.Distance(center, neighbor)));
        Assert.Equal(center.Neighbor(0), center.Neighbor(6));
    }

    [Fact]
    public void CoordsInRadius_ReturnsFullHexDiskAroundCenter()
    {
        var center = new HexCoord(4, -2);

        var coords = HexCoord.CoordsInRadius(center, radius: 2).ToArray();

        Assert.Equal(19, coords.Length);
        Assert.Equal(coords.Length, coords.Distinct().Count());
        Assert.Contains(center, coords);
        Assert.All(coords, coord => Assert.True(HexCoord.Distance(center, coord) <= 2));
        Assert.Contains(new HexCoord(6, -4), coords);
        Assert.Contains(new HexCoord(2, 0), coords);
    }

    [Fact]
    public void CoordsInRadius_RejectsNegativeRadius()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HexCoord.CoordsInRadius(new HexCoord(0, 0), radius: -1).ToArray());
    }

    [Fact]
    public void ToString_UsesCompactAxialCoordinateFormat()
    {
        Assert.Equal("(3,-2)", new HexCoord(3, -2).ToString());
    }
}
