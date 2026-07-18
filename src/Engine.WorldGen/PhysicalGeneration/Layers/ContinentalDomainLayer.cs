namespace Engine.WorldGen;

internal sealed class ContinentalDomainLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "continental-domains",
        0x12e5a91,
        PhysicalGeographyFacts.None,
        PhysicalGeographyFacts.ContinentalDomains);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        var continents = CreateContinentalSites(
            map.Grid,
            context.Seed,
            context.Profile.ContinentalCenterCount);
        var oceans = CreateOceanSites(
            map.Grid,
            context.Seed ^ 0x5b31d,
            context.Profile.OceanBasinCenterCount,
            context.Profile.ContinentalCenterCount);
        var scores = new double[map.Grid.TriangleCount];
        var minimum = double.MaxValue;
        var maximum = double.MinValue;
        for (var triangleIndex = 0; triangleIndex < scores.Length; triangleIndex++)
        {
            var x = map.Grid.TriangleX(triangleIndex);
            var y = map.Grid.TriangleY(triangleIndex);
            var warpX = PhysicalNoise.FractalNoise(
                x,
                y,
                context.Seed ^ 0x2d17,
                context.Profile.ContinentalWarpScale,
                3) * context.Profile.ContinentalWarpStrength;
            var warpY = PhysicalNoise.FractalNoise(
                x,
                y,
                context.Seed ^ 0x71b9,
                context.Profile.ContinentalWarpScale,
                3) * context.Profile.ContinentalWarpStrength;
            var continentalDistance = NearestDistance(x + warpX, y + warpY, continents);
            var oceanDistance = NearestDistance(x + warpX, y + warpY, oceans);
            var oceanChannels = OceanChannelCarve(
                x + warpX,
                y + warpY,
                oceans,
                context.Profile.OceanBasinCenterCount / 2,
                map.Grid.Radius);
            var boundaryRoughness = PhysicalNoise.FractalNoise(
                x + (warpX * 0.2d),
                y + (warpY * 0.2d),
                context.Seed ^ 0x41c7,
                context.Profile.ContinentalRadius * 0.42d,
                2) * 0.16d;
            var score = oceanDistance - continentalDistance + boundaryRoughness - oceanChannels;
            scores[triangleIndex] = score;
            minimum = Math.Min(minimum, score);
            maximum = Math.Max(maximum, score);
        }

        var range = Math.Max(0.0001d, maximum - minimum);
        for (var triangleIndex = 0; triangleIndex < scores.Length; triangleIndex++)
        {
            map.ContinentalPotential[triangleIndex] = (int)Math.Round(
                Math.Clamp((scores[triangleIndex] - minimum) / range, 0d, 1d) * 1000d,
                MidpointRounding.AwayFromZero);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        var minimum = int.MaxValue;
        var maximum = int.MinValue;
        foreach (var value in map.ContinentalPotential)
        {
            minimum = Math.Min(minimum, value);
            maximum = Math.Max(maximum, value);
        }

        if (minimum < 0 || maximum > 1000 || maximum - minimum < 600)
        {
            throw new InvalidOperationException("Continental domains do not span coherent land and ocean potentials.");
        }
    }

    private static MacroSite[] CreateContinentalSites(
        PhysicalGeographyGrid grid,
        int seed,
        int count)
    {
        var sites = new MacroSite[count];
        sites[0] = new MacroSite(
            -Math.Min(12d, grid.Radius * 0.08d),
            Math.Min(6d, grid.Radius * 0.04d),
            0.78d);
        var ringCount = Math.Max(1, count - 1);
        var placementRadius = Math.Max(18d, grid.Radius * 0.98d);
        for (var index = 1; index < count; index++)
        {
            var ringIndex = index - 1;
            var angle = (ringIndex * Math.PI * 2d / ringCount) +
                (PhysicalNoise.HashSigned(seed ^ 0x1163, index, 0) * 0.32d);
            var radius = placementRadius *
                (0.72d + (PhysicalNoise.HashUnit(seed ^ 0x28a1, index, 1) * 0.34d));
            sites[index] = new MacroSite(
                Math.Cos(angle) * radius,
                Math.Sin(angle) * radius,
                0.72d + (PhysicalNoise.HashUnit(seed ^ 0x39d7, index, 2) * 0.30d));
        }

        return sites;
    }

    private static MacroSite[] CreateOceanSites(
        PhysicalGeographyGrid grid,
        int seed,
        int count,
        int continentCount)
    {
        var sites = new MacroSite[count];
        var innerCount = Math.Max(1, count / 2);
        var outerCount = count - innerCount;
        for (var index = 0; index < count; index++)
        {
            var inner = index < innerCount;
            var ringIndex = inner ? index : index - innerCount;
            var ringCount = inner ? innerCount : Math.Max(1, outerCount);
            var radius = grid.Radius * (inner ? 0.55d : 1.48d);
            var offset = inner
                ? Math.PI / Math.Max(2, continentCount - 1)
                : Math.PI / Math.Max(2, outerCount);
            var angle = offset + (ringIndex * Math.PI * 2d / ringCount) +
                (PhysicalNoise.HashSigned(seed ^ 0x24a7, index, 0) * 0.24d);
            var variedRadius = radius *
                (0.90d + (PhysicalNoise.HashUnit(seed ^ 0x3c19, index, 1) * 0.20d));
            sites[index] = new MacroSite(
                Math.Cos(angle) * variedRadius,
                Math.Sin(angle) * variedRadius,
                0.82d + (PhysicalNoise.HashUnit(seed ^ 0x51d3, index, 2) * 0.22d));
        }

        return sites;
    }

    private static double NearestDistance(double x, double y, ReadOnlySpan<MacroSite> sites)
    {
        var nearest = double.MaxValue;
        foreach (var site in sites)
        {
            var dx = x - site.X;
            var dy = y - site.Y;
            nearest = Math.Min(nearest, Math.Sqrt((dx * dx) + (dy * dy)) / site.Weight);
        }

        return nearest;
    }

    private static double OceanChannelCarve(
        double x,
        double y,
        ReadOnlySpan<MacroSite> oceanSites,
        int innerCount,
        int radius)
    {
        if (innerCount <= 0)
        {
            return 0d;
        }

        var nearest = double.MaxValue;
        var outerRadius = radius * 1.90d;
        for (var index = 0; index < innerCount; index++)
        {
            var site = oceanSites[index];
            var next = oceanSites[(index + 1) % innerCount];
            nearest = Math.Min(nearest, DistanceToSegment(x, y, site.X, site.Y, next.X, next.Y));
            var length = Math.Max(0.001d, Math.Sqrt((site.X * site.X) + (site.Y * site.Y)));
            nearest = Math.Min(
                nearest,
                DistanceToSegment(
                    x,
                    y,
                    site.X,
                    site.Y,
                    site.X / length * outerRadius,
                    site.Y / length * outerRadius));
        }

        var width = Math.Max(8d, radius * 0.09d);
        return Math.Clamp(1d - (nearest / width), 0d, 1d) * 48d;
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

    private readonly record struct MacroSite(double X, double Y, double Weight);
}
