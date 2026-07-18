namespace Engine.WorldGen;

internal static class PhysicalTerrainFormation
{
    public static void GenerateUpliftAndErode(
        PhysicalGeographyField field,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        var continents = CreateContinentalCenters(field.Grid, seed, profile);
        var ridges = CreateRidgeAxes(continents, seed, profile);
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var x = field.Grid.TriangleX(triangleIndex);
            var y = field.Grid.TriangleY(triangleIndex);
            var warpX = PhysicalNoise.FractalNoise(x, y, seed ^ 0x2d17, profile.ContinentalWarpScale, 3) *
                profile.ContinentalWarpStrength;
            var warpY = PhysicalNoise.FractalNoise(x, y, seed ^ 0x71b9, profile.ContinentalWarpScale, 3) *
                profile.ContinentalWarpStrength;
            var continental = ContinentalUplift(x + warpX, y + warpY, continents, profile);
            var ridge = RidgeUplift(x, y, ridges, profile);
            var variation = PhysicalNoise.FractalNoise(
                x + (warpX * 0.35d),
                y + (warpY * 0.35d),
                seed ^ 0x4f2b,
                profile.ElevationVariationScale,
                3) * profile.ElevationVariationStrength;
            var elevation = -0.58d + continental + ridge + variation;
            field.Elevation[triangleIndex] = ScaleElevation(elevation, profile.ElevationScale);
            field.Uplift[triangleIndex] = (float)Math.Clamp(continental + ridge, 0d, 1.5d);
            field.RidgeInfluence[triangleIndex] = (float)Math.Clamp(
                ridge / Math.Max(profile.RidgeUplift, 0.001d),
                0d,
                1d);
        }

        ErodeAncestralValleys(field, profile);
    }

    private static ContinentalCenter[] CreateContinentalCenters(
        PhysicalGeographyGrid grid,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        var centers = new ContinentalCenter[profile.ContinentalCenterCount];
        centers[0] = new ContinentalCenter(-12d, 6d, 1.04d, 0.82d, 0.18d);
        var placementRadius = Math.Max(28d, grid.Radius * 1.15d);
        for (var index = 1; index < centers.Length; index++)
        {
            var angle = ((index - 1d) / Math.Max(1, centers.Length - 1d) * Math.PI * 2d) +
                (PhysicalNoise.HashSigned(seed ^ 0x1163, index, 0) * 0.42d);
            var radius = placementRadius * (0.48d + (PhysicalNoise.HashUnit(seed ^ 0x28a1, index, 1) * 0.45d));
            centers[index] = new ContinentalCenter(
                Math.Cos(angle) * radius,
                Math.Sin(angle) * radius,
                0.78d + (PhysicalNoise.HashUnit(seed ^ 0x39d7, index, 2) * 0.32d),
                0.58d + (PhysicalNoise.HashUnit(seed ^ 0x4c91, index, 3) * 0.42d),
                PhysicalNoise.HashUnit(seed ^ 0x5ea9, index, 4) * Math.PI);
        }

        return centers;
    }

    private static RidgeAxis[] CreateRidgeAxes(
        ContinentalCenter[] centers,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        var axes = new RidgeAxis[profile.RidgeAxisCount];
        for (var index = 0; index < axes.Length; index++)
        {
            var center = centers[index % centers.Length];
            var angle = center.Angle +
                (PhysicalNoise.HashSigned(seed ^ 0x6d2f, index, 0) * 0.65d);
            var halfLength = profile.RidgeHalfLength *
                (0.55d + (PhysicalNoise.HashUnit(seed ^ 0x7f45, index, 1) * 0.55d));
            var offset = PhysicalNoise.HashSigned(seed ^ 0x193d, index, 2) * profile.ContinentalRadius * 0.22d;
            var normalX = -Math.Sin(angle);
            var normalY = Math.Cos(angle);
            var centerX = center.X + (normalX * offset);
            var centerY = center.Y + (normalY * offset);
            axes[index] = new RidgeAxis(
                centerX - (Math.Cos(angle) * halfLength),
                centerY - (Math.Sin(angle) * halfLength),
                centerX + (Math.Cos(angle) * halfLength),
                centerY + (Math.Sin(angle) * halfLength),
                0.72d + (PhysicalNoise.HashUnit(seed ^ 0x2b57, index, 3) * 0.42d));
        }

        return axes;
    }

    private static double ContinentalUplift(
        double x,
        double y,
        ContinentalCenter[] centers,
        PhysicalGeographyGenerationProfile profile)
    {
        var strongest = 0d;
        foreach (var center in centers)
        {
            var cos = Math.Cos(center.Angle);
            var sin = Math.Sin(center.Angle);
            var dx = x - center.X;
            var dy = y - center.Y;
            var along = ((dx * cos) + (dy * sin)) / (profile.ContinentalRadius * center.Aspect);
            var across = ((-dx * sin) + (dy * cos)) / profile.ContinentalRadius;
            var distance = Math.Sqrt((along * along) + (across * across));
            var amount = Math.Clamp(1d - distance, 0d, 1d);
            strongest = Math.Max(strongest, amount * amount * center.Strength);
        }

        return strongest;
    }

    private static double RidgeUplift(
        double x,
        double y,
        RidgeAxis[] axes,
        PhysicalGeographyGenerationProfile profile)
    {
        var total = 0d;
        foreach (var axis in axes)
        {
            var distance = DistanceToSegment(x, y, axis.Ax, axis.Ay, axis.Bx, axis.By);
            var amount = Math.Clamp(1d - (distance / profile.RidgeWidth), 0d, 1d);
            total += amount * amount * profile.RidgeUplift * axis.Strength;
        }

        return Math.Min(total, profile.RidgeUplift * 1.35d);
    }

    private static void ErodeAncestralValleys(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        var targets = new int[field.Grid.TriangleCount];
        var accumulation = new int[field.Grid.TriangleCount];
        for (var iteration = 0; iteration < profile.FluvialErosionIterations; iteration++)
        {
            RouteDownhill(field, targets);
            Array.Fill(accumulation, 1);
            var order = ElevationOrder(field.Elevation);
            for (var orderIndex = order.Length - 1; orderIndex >= 0; orderIndex--)
            {
                var triangleIndex = order[orderIndex];
                var target = targets[triangleIndex];
                if (target >= 0)
                {
                    accumulation[target] = SaturatingAdd(accumulation[target], accumulation[triangleIndex]);
                }
            }

            for (var triangleIndex = 0; triangleIndex < targets.Length; triangleIndex++)
            {
                var target = targets[triangleIndex];
                if (target < 0)
                {
                    continue;
                }

                var fall = Math.Max(0, field.Elevation[triangleIndex] - field.Elevation[target]);
                var streamPower = Math.Log2(accumulation[triangleIndex] + 1d) * Math.Sqrt(fall + 1d);
                var cut = (int)Math.Round(
                    Math.Min(26d, streamPower * profile.FluvialErosionStrength),
                    MidpointRounding.AwayFromZero);
                field.Elevation[triangleIndex] -= cut;
            }
        }
    }

    internal static void RouteDownhill(PhysicalGeographyField field, Span<int> targets)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var target = -1;
            var targetElevation = field.Elevation[triangleIndex];
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0 &&
                    (field.Elevation[neighbor] < targetElevation ||
                        (field.Elevation[neighbor] == targetElevation && target >= 0 && neighbor < target)))
                {
                    target = neighbor;
                    targetElevation = field.Elevation[neighbor];
                }
            }

            targets[triangleIndex] = target;
        }
    }

    internal static int[] ElevationOrder(ReadOnlySpan<int> elevation)
    {
        var minimum = int.MaxValue;
        var maximum = int.MinValue;
        foreach (var value in elevation)
        {
            minimum = Math.Min(minimum, value);
            maximum = Math.Max(maximum, value);
        }

        var range = checked(maximum - minimum + 1);
        var counts = new int[range];
        foreach (var value in elevation)
        {
            counts[value - minimum]++;
        }

        var offset = 0;
        for (var index = 0; index < counts.Length; index++)
        {
            var count = counts[index];
            counts[index] = offset;
            offset += count;
        }

        var order = new int[elevation.Length];
        for (var index = 0; index < elevation.Length; index++)
        {
            var bucket = elevation[index] - minimum;
            order[counts[bucket]++] = index;
        }

        return order;
    }

    private static double DistanceToSegment(
        double x,
        double y,
        double ax,
        double ay,
        double bx,
        double by)
    {
        var dx = bx - ax;
        var dy = by - ay;
        var lengthSquared = (dx * dx) + (dy * dy);
        var amount = lengthSquared <= double.Epsilon
            ? 0d
            : Math.Clamp((((x - ax) * dx) + ((y - ay) * dy)) / lengthSquared, 0d, 1d);
        var closestX = ax + (dx * amount);
        var closestY = ay + (dy * amount);
        var offsetX = x - closestX;
        var offsetY = y - closestY;
        return Math.Sqrt((offsetX * offsetX) + (offsetY * offsetY));
    }

    private static int ScaleElevation(double value, int scale) =>
        (int)Math.Round(Math.Clamp(value, -2d, 2d) * scale, MidpointRounding.AwayFromZero);

    private static int SaturatingAdd(int first, int second) =>
        first > int.MaxValue - second ? int.MaxValue : first + second;

    private readonly record struct ContinentalCenter(
        double X,
        double Y,
        double Strength,
        double Aspect,
        double Angle);

    private readonly record struct RidgeAxis(
        double Ax,
        double Ay,
        double Bx,
        double By,
        double Strength);
}
