namespace Engine.WorldGen.Tests.Map;

public sealed class PhysicalGenerationAlgorithmTests
{
    [Fact]
    public void BoundedCandidateSelection_RetainsBestScoresWithStableIndexTies()
    {
        var selection = new BoundedCandidateSelection(3);
        selection.Consider(8, 10d);
        selection.Consider(3, 12d);
        selection.Consider(6, 10d);
        selection.Consider(1, 12d);
        selection.Consider(4, 11d);

        Assert.Equal([1, 3, 4], selection.GetBestIndexes());
    }

    [Theory]
    [InlineData(0d, 0d, 3.5d, 700)]
    [InlineData(7.2d, -4.1d, 6d, 1000)]
    [InlineData(-11.4d, 9.7d, 2.25d, 425)]
    public void RadialPainter_MatchesExhaustiveTriangleEvaluation(
        double centerX,
        double centerY,
        double radius,
        int strength)
    {
        var grid = new PhysicalGeographyGrid(10, 10);
        var expected = new int[grid.TriangleCount];
        var actual = new int[grid.TriangleCount];
        PaintExhaustively(grid, expected, centerX, centerY, radius, strength);

        PhysicalRadialFieldPainter.PaintMaximum(
            grid,
            actual,
            centerX,
            centerY,
            radius,
            strength);

        Assert.Equal(expected, actual);
    }

    private static void PaintExhaustively(
        PhysicalGeographyGrid grid,
        Span<int> field,
        double centerX,
        double centerY,
        double radius,
        int strength)
    {
        var radiusSquared = radius * radius;
        for (var triangleIndex = 0; triangleIndex < grid.TriangleCount; triangleIndex++)
        {
            var dx = grid.TriangleX(triangleIndex) - centerX;
            var dy = grid.TriangleY(triangleIndex) - centerY;
            var distanceSquared = (dx * dx) + (dy * dy);
            if (distanceSquared > radiusSquared)
            {
                continue;
            }

            var amount = 1d - (Math.Sqrt(distanceSquared) / radius);
            field[triangleIndex] = Math.Max(
                field[triangleIndex],
                (int)Math.Round(strength * amount * amount, MidpointRounding.AwayFromZero));
        }
    }
}
