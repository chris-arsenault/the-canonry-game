namespace Engine.WorldGen;

/// <summary>
/// Applies the common squared radial falloff while visiting only hexes whose
/// microtriangle centroids can intersect the requested circle.
/// </summary>
internal static class PhysicalRadialFieldPainter
{
    private const double HexTriangleExtent = 0.8d;
    private const double RootThree = 1.7320508075688772d;

    public static void PaintMaximum(
        PhysicalGeographyGrid grid,
        Span<int> field,
        double centerX,
        double centerY,
        double radius,
        int strength)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (field.Length != grid.TriangleCount)
        {
            throw new ArgumentException("Radial field length does not match the physical grid.", nameof(field));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        var centerExtent = radius + HexTriangleExtent;
        var minimumR = (int)Math.Floor((centerY - centerExtent) / 1.5d);
        var maximumR = (int)Math.Ceiling((centerY + centerExtent) / 1.5d);
        var radiusSquared = radius * radius;
        for (var r = minimumR; r <= maximumR; r++)
        {
            var minimumQ = (int)Math.Floor(
                ((centerX - centerExtent) / RootThree) - (r * 0.5d));
            var maximumQ = (int)Math.Ceiling(
                ((centerX + centerExtent) / RootThree) - (r * 0.5d));
            for (var q = minimumQ; q <= maximumQ; q++)
            {
                if (!grid.TryGetHexIndex(new HexCoord(q, r), out var hexIndex))
                {
                    continue;
                }

                PaintHex(
                    grid,
                    field,
                    hexIndex,
                    centerX,
                    centerY,
                    radius,
                    radiusSquared,
                    strength);
            }
        }
    }

    private static void PaintHex(
        PhysicalGeographyGrid grid,
        Span<int> field,
        int hexIndex,
        double centerX,
        double centerY,
        double radius,
        double radiusSquared,
        int strength)
    {
        for (var local = 0; local < Engine.Constants.PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
        {
            var triangleIndex = grid.TriangleIndex(hexIndex, local);
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
