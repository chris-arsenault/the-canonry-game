namespace Engine.WorldGen;

using Engine.Constants;

/// <summary>
/// Builds a reduced, generation-only ocean circulation field. The field is
/// shaped by broad curl forcing, basin depth, and coast normals; it is not a
/// runtime fluid simulation.
/// </summary>
internal sealed class OceanCurrentLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "ocean-currents",
        0x6c41e2,
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Bathymetry |
        PhysicalGeographyFacts.Geothermal,
        PhysicalGeographyFacts.OceanCurrents,
        PhysicalGeographyFacts.Warmth |
        PhysicalGeographyFacts.Temperature |
        PhysicalGeographyFacts.Moisture |
        PhysicalGeographyFacts.Precipitation |
        PhysicalGeographyFacts.Runoff |
        PhysicalGeographyFacts.SeaIce |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        map.OceanCurrentX.Clear();
        map.OceanCurrentY.Clear();
        map.OceanCurrentStrength.Clear();
        map.OceanCurrentHeat.Clear();
        map.WaveExposure.Clear();
        SeedDirections(map, context);
        SmoothDirections(map, context.Profile.OceanCurrentSmoothingIterations);
        MeasureStrength(map, context);
        TransportHeat(map, context.Profile.OceanCurrentHeatIterations);
        MeasureWaveExposure(map, context.Profile.OceanCurrentFetchSteps);
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        var movingOcean = 0;
        var directionVariation = false;
        var firstX = int.MinValue;
        var firstY = int.MinValue;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var ocean = map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean;
            if (!ocean)
            {
                ValidateDryTriangle(map, triangleIndex);
                continue;
            }

            ValidateOceanTriangle(map, triangleIndex);

            if (map.OceanCurrentStrength[triangleIndex] <= 0)
            {
                continue;
            }

            movingOcean++;
            if (firstX == int.MinValue)
            {
                firstX = map.OceanCurrentX[triangleIndex];
                firstY = map.OceanCurrentY[triangleIndex];
            }
            else
            {
                directionVariation |= Math.Abs(firstX - map.OceanCurrentX[triangleIndex]) > 80 ||
                    Math.Abs(firstY - map.OceanCurrentY[triangleIndex]) > 80;
            }
        }

        if (movingOcean == 0 || !directionVariation)
        {
            throw new InvalidOperationException("Ocean circulation did not form a varied current field.");
        }
    }

    private static void ValidateDryTriangle(PhysicalGeographyField map, int triangleIndex)
    {
        if (map.OceanCurrentX[triangleIndex] != 0 || map.OceanCurrentY[triangleIndex] != 0 ||
            map.OceanCurrentStrength[triangleIndex] != 0 || map.OceanCurrentHeat[triangleIndex] != 0 ||
            map.WaveExposure[triangleIndex] != 0)
        {
            throw new InvalidOperationException("Ocean circulation was assigned away from ocean.");
        }
    }

    private static void ValidateOceanTriangle(PhysicalGeographyField map, int triangleIndex)
    {
        if (!IsSignedScalar(map.OceanCurrentX[triangleIndex]) ||
            !IsSignedScalar(map.OceanCurrentY[triangleIndex]) ||
            !IsUnitScalar(map.OceanCurrentStrength[triangleIndex]) ||
            !IsUnitScalar(map.OceanCurrentHeat[triangleIndex]) ||
            !IsUnitScalar(map.WaveExposure[triangleIndex]))
        {
            throw new InvalidOperationException("Ocean circulation contains an invalid scalar.");
        }
    }

    private static bool IsSignedScalar(int value) => value is >= -1000 and <= 1000;

    private static bool IsUnitScalar(int value) => value is >= 0 and <= 1000;

    private static void SeedDirections(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context)
    {
        var noiseScale = Math.Max(18d, map.Grid.Radius * 0.46d);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            var x = map.Grid.TriangleX(triangleIndex);
            var y = map.Grid.TriangleY(triangleIndex);
            var gradientX = PhysicalNoise.FractalNoise(
                    x + 1.5d, y, context.Seed, noiseScale, 3) -
                PhysicalNoise.FractalNoise(x - 1.5d, y, context.Seed, noiseScale, 3);
            var gradientY = PhysicalNoise.FractalNoise(
                    x, y + 1.5d, context.Seed, noiseScale, 3) -
                PhysicalNoise.FractalNoise(x, y - 1.5d, context.Seed, noiseScale, 3);
            var curlX = gradientY;
            var curlY = -gradientX;
            var radius = Math.Max(1d, Math.Sqrt((x * x) + (y * y)));
            var gyreSign = PhysicalNoise.UnitNoise(x, y, context.Seed ^ 0x2d61, noiseScale * 1.8d, 2) >= 0.5d
                ? 1d
                : -1d;
            var tangentX = -y / radius * gyreSign;
            var tangentY = x / radius * gyreSign;
            BasinInfluence(map, triangleIndex, out var basinX, out var basinY);
            WriteDirection(
                map,
                triangleIndex,
                (curlX * 3.4d) + (tangentX * 0.48d) + (basinX * 0.88d),
                (curlY * 3.4d) + (tangentY * 0.48d) + (basinY * 0.88d));
        }
    }

    private static void BasinInfluence(
        PhysicalGeographyField map,
        int triangleIndex,
        out double influenceX,
        out double influenceY)
    {
        influenceX = 0d;
        influenceY = 0d;
        var depth = map.WaterDepths[triangleIndex];
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor < 0)
            {
                continue;
            }

            double dx = map.Grid.TriangleX(neighbor) - map.Grid.TriangleX(triangleIndex);
            double dy = map.Grid.TriangleY(neighbor) - map.Grid.TriangleY(triangleIndex);
            var length = Math.Max(0.001d, Math.Sqrt((dx * dx) + (dy * dy)));
            dx /= length;
            dy /= length;
            if (map.WaterKindCodes[neighbor] != PhysicalWaterKindCodes.Ocean)
            {
                influenceX -= dx * 1.4d;
                influenceY -= dy * 1.4d;
                continue;
            }

            var depthGain = Math.Clamp((map.WaterDepths[neighbor] - depth) / 240d, -1d, 1d);
            influenceX += dx * depthGain;
            influenceY += dy * depthGain;
        }
    }

    private static void SmoothDirections(PhysicalGeographyField map, int iterations)
    {
        var nextX = new int[map.Grid.TriangleCount];
        var nextY = new int[map.Grid.TriangleCount];
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
            {
                if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
                {
                    continue;
                }

                var x = map.OceanCurrentX[triangleIndex] * 3d;
                var y = map.OceanCurrentY[triangleIndex] * 3d;
                var weight = 3;
                for (var edge = 0; edge < 3; edge++)
                {
                    var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
                    if (neighbor >= 0 && map.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean)
                    {
                        x += map.OceanCurrentX[neighbor];
                        y += map.OceanCurrentY[neighbor];
                        weight++;
                    }
                }

                Normalize(x / weight, y / weight, out nextX[triangleIndex], out nextY[triangleIndex]);
            }

            nextX.AsSpan().CopyTo(map.OceanCurrentX);
            nextY.AsSpan().CopyTo(map.OceanCurrentY);
        }
    }

    private static void MeasureStrength(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationContext context)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            var depth = Math.Clamp(
                map.WaterDepths[triangleIndex] * 420 / context.Profile.ShelfBreakDepth,
                0,
                420);
            var broad = (int)Math.Round(
                PhysicalNoise.UnitNoise(
                    map.Grid.TriangleX(triangleIndex),
                    map.Grid.TriangleY(triangleIndex),
                    context.Seed ^ 0x19af,
                    Math.Max(20d, map.Grid.Radius * 0.38d),
                    2) * 250d,
                MidpointRounding.AwayFromZero);
            map.OceanCurrentStrength[triangleIndex] = Math.Clamp(
                context.Profile.OceanCurrentMinimumStrength + depth + broad,
                1,
                1000);
        }
    }

    private static void TransportHeat(PhysicalGeographyField map, int iterations)
    {
        var source = InitializeHeatSources(map);

        var next = new int[map.Grid.TriangleCount];
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            for (var triangleIndex = 0; triangleIndex < next.Length; triangleIndex++)
            {
                next[triangleIndex] = AdvectedHeat(map, source, triangleIndex);
            }

            next.AsSpan().CopyTo(map.OceanCurrentHeat);
        }
    }

    private static int[] InitializeHeatSources(PhysicalGeographyField map)
    {
        var source = new int[map.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < source.Length; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            var heat = map.Geothermal[triangleIndex];
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
                heat = neighbor >= 0
                    ? Math.Max(heat, map.Geothermal[neighbor] * 4 / 5)
                    : heat;
            }

            source[triangleIndex] = Math.Clamp(60 + (heat * 4 / 5), 0, 1000);
            map.OceanCurrentHeat[triangleIndex] = source[triangleIndex];
        }

        return source;
    }

    private static int AdvectedHeat(PhysicalGeographyField map, int[] source, int triangleIndex)
    {
        if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
        {
            return 0;
        }

        var upstream = MostAlignedOceanNeighbor(map, triangleIndex, upstream: true);
        var advected = upstream >= 0 ? map.OceanCurrentHeat[upstream] : 0;
        return Math.Max(
            source[triangleIndex],
            Math.Max(0, ((map.OceanCurrentHeat[triangleIndex] * 3) + (advected * 7)) / 10 - 5));
    }

    private static void MeasureWaveExposure(PhysicalGeographyField map, int fetchSteps)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            var fetch = 0;
            var current = triangleIndex;
            var previous = -1;
            while (fetch < fetchSteps)
            {
                var next = MostAlignedOceanNeighbor(map, current, upstream: true);
                if (next < 0 || next == previous)
                {
                    break;
                }

                fetch++;
                previous = current;
                current = next;
            }

            map.WaveExposure[triangleIndex] = Math.Clamp(
                (fetch * 760 / fetchSteps) + (map.OceanCurrentStrength[triangleIndex] * 240 / 1000),
                0,
                1000);
        }
    }

    private static int MostAlignedOceanNeighbor(
        PhysicalGeographyField map,
        int triangleIndex,
        bool upstream)
    {
        var directionX = map.OceanCurrentX[triangleIndex] * (upstream ? -1d : 1d);
        var directionY = map.OceanCurrentY[triangleIndex] * (upstream ? -1d : 1d);
        var best = -1;
        var bestAlignment = 0d;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor < 0 || map.WaterKindCodes[neighbor] != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            var dx = map.Grid.TriangleX(neighbor) - map.Grid.TriangleX(triangleIndex);
            var dy = map.Grid.TriangleY(neighbor) - map.Grid.TriangleY(triangleIndex);
            var length = Math.Max(0.001d, Math.Sqrt((dx * dx) + (dy * dy)));
            var alignment = ((dx / length) * directionX) + ((dy / length) * directionY);
            if (alignment > bestAlignment)
            {
                best = neighbor;
                bestAlignment = alignment;
            }
        }

        return best;
    }

    private static void WriteDirection(
        PhysicalGeographyField map,
        int triangleIndex,
        double x,
        double y)
    {
        Normalize(x, y, out var normalizedX, out var normalizedY);
        map.OceanCurrentX[triangleIndex] = normalizedX;
        map.OceanCurrentY[triangleIndex] = normalizedY;
    }

    private static void Normalize(double x, double y, out int normalizedX, out int normalizedY)
    {
        var length = Math.Sqrt((x * x) + (y * y));
        if (length < 0.000001d)
        {
            normalizedX = 0;
            normalizedY = 0;
            return;
        }

        normalizedX = (int)Math.Round(x / length * 1000d, MidpointRounding.AwayFromZero);
        normalizedY = (int)Math.Round(y / length * 1000d, MidpointRounding.AwayFromZero);
    }
}
