namespace Engine.WorldGen;

internal sealed class TectonicUpliftLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "tectonic-uplift",
        0x4f2b17,
        PhysicalGeographyFacts.ContinentalDomains,
        PhysicalGeographyFacts.Elevation);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        var ridges = CreateRidges(map.Grid, context);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var x = map.Grid.TriangleX(triangleIndex);
            var y = map.Grid.TriangleY(triangleIndex);
            var potential = map.ContinentalPotential[triangleIndex] / 1000d;
            var baseElevation = ((potential - 0.5d) * 1.75d) - 0.04d;
            var landGate = SmoothStep(Math.Clamp((potential - 0.40d) / 0.34d, 0d, 1d));
            var ridge = RidgeUplift(x, y, ridges, context.Profile) * landGate;
            var variation = PhysicalNoise.FractalNoise(
                x,
                y,
                context.Seed ^ 0x52c9,
                context.Profile.ElevationVariationScale,
                3) * context.Profile.ElevationVariationStrength;
            var elevation = baseElevation + ridge + variation;
            map.Elevation[triangleIndex] = ScaleElevation(elevation, context.Profile.ElevationScale);
            map.Uplift[triangleIndex] = (float)Math.Clamp((potential * 0.72d) + ridge, 0d, 1.5d);
            map.RidgeInfluence[triangleIndex] = (float)Math.Clamp(
                ridge / Math.Max(0.001d, context.Profile.RidgeUplift),
                0d,
                1d);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.ContinentalPotential[triangleIndex] < 250 && map.RidgeInfluence[triangleIndex] > 0.05f)
            {
                throw new InvalidOperationException("Tectonic ridge uplift escaped into a deep-ocean domain.");
            }
        }
    }

    private static RidgeAxis[] CreateRidges(
        PhysicalGeographyGrid grid,
        PhysicalGeographyGenerationContext context)
    {
        var axes = new RidgeAxis[context.Profile.RidgeAxisCount];
        for (var index = 0; index < axes.Length; index++)
        {
            var centerAngle = index * Math.PI * 2d / axes.Length;
            var centerRadius = grid.Radius *
                (0.22d + (PhysicalNoise.HashUnit(context.Seed ^ 0x193d, index, 1) * 0.48d));
            var centerX = Math.Cos(centerAngle) * centerRadius;
            var centerY = Math.Sin(centerAngle) * centerRadius;
            var angle = centerAngle + (Math.PI * 0.5d) +
                (PhysicalNoise.HashSigned(context.Seed ^ 0x6d2f, index, 0) * 0.75d);
            var halfLength = Math.Min(
                context.Profile.RidgeHalfLength,
                grid.Radius * 0.68d) *
                (0.55d + (PhysicalNoise.HashUnit(context.Seed ^ 0x7f45, index, 2) * 0.45d));
            axes[index] = new RidgeAxis(
                centerX - (Math.Cos(angle) * halfLength),
                centerY - (Math.Sin(angle) * halfLength),
                centerX + (Math.Cos(angle) * halfLength),
                centerY + (Math.Sin(angle) * halfLength),
                PhysicalNoise.HashSigned(context.Seed ^ 0x2b57, index, 3) * context.Profile.RidgeWidth * 1.8d);
        }

        return axes;
    }

    private static double RidgeUplift(
        double x,
        double y,
        ReadOnlySpan<RidgeAxis> axes,
        PhysicalGeographyGenerationProfile profile)
    {
        var strongest = 0d;
        foreach (var axis in axes)
        {
            var distance = DistanceToCurvedSegment(x, y, axis);
            var amount = Math.Clamp(1d - (distance / profile.RidgeWidth), 0d, 1d);
            strongest = Math.Max(strongest, amount * amount * profile.RidgeUplift);
        }

        return strongest;
    }

    private static double DistanceToCurvedSegment(double x, double y, RidgeAxis axis)
    {
        var best = double.MaxValue;
        const int segmentCount = 8;
        var previousX = axis.Ax;
        var previousY = axis.Ay;
        for (var segment = 1; segment <= segmentCount; segment++)
        {
            var amount = segment / (double)segmentCount;
            var curve = Math.Sin(amount * Math.PI) * axis.Curve;
            var dx = axis.Bx - axis.Ax;
            var dy = axis.By - axis.Ay;
            var length = Math.Max(0.001d, Math.Sqrt((dx * dx) + (dy * dy)));
            var nextX = axis.Ax + (dx * amount) - (dy / length * curve);
            var nextY = axis.Ay + (dy * amount) + (dx / length * curve);
            best = Math.Min(best, DistanceToSegment(x, y, previousX, previousY, nextX, nextY));
            previousX = nextX;
            previousY = nextY;
        }

        return best;
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
        var offsetX = x - (ax + (dx * amount));
        var offsetY = y - (ay + (dy * amount));
        return Math.Sqrt((offsetX * offsetX) + (offsetY * offsetY));
    }

    private static double SmoothStep(double value) => value * value * (3d - (2d * value));

    private static int ScaleElevation(double value, int scale) =>
        (int)Math.Round(Math.Clamp(value, -2d, 2d) * scale, MidpointRounding.AwayFromZero);

    private readonly record struct RidgeAxis(
        double Ax,
        double Ay,
        double Bx,
        double By,
        double Curve);
}
