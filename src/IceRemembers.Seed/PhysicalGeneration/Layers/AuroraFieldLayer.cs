namespace IceRemembers.Seed;

internal sealed class AuroraFieldLayer : IIceRemembersMicrogeographyLayer
{
    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "aurora-field",
        0,
        IceRemembersMicrogeographyFacts.FireClimate,
        IceRemembersMicrogeographyFacts.AuroraField,
        IceRemembersMicrogeographyFacts.PressureField |
        IceRemembersMicrogeographyFacts.AuroraFeatures |
        IceRemembersMicrogeographyFacts.PressureCoastalFeedback |
        IceRemembersMicrogeographyFacts.Interactions |
        IceRemembersMicrogeographyFacts.FinalSurfaces |
        IceRemembersMicrogeographyFacts.Validated);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        Array.Clear(map.AuroraIntensity);
        var accumulation = new int[map.Physical.Grid.TriangleCount];
        var targets = new int[map.Physical.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            accumulation[triangleIndex] = Math.Max(
                0,
                context.Profile.AuroraBaseInput - map.Physical.Temperature[triangleIndex] +
                    (Math.Max(0, map.Physical.Elevation[triangleIndex]) / 10));
            targets[triangleIndex] = ColdestNeighbor(map, triangleIndex);
        }

        var order = ScalarOrder(map.Physical.Temperature);
        for (var orderIndex = order.Length - 1; orderIndex >= 0; orderIndex--)
        {
            var triangleIndex = order[orderIndex];
            var capacity = Math.Clamp(map.Physical.IceThickness[triangleIndex], 0, 900);
            var retained = accumulation[triangleIndex] * capacity / 1200;
            map.AuroraIntensity[triangleIndex] = Math.Clamp(
                ((int)Math.Round(
                        Math.Log2(accumulation[triangleIndex] + 1d) * context.Profile.AuroraLogScale,
                        MidpointRounding.ToEven) +
                    (capacity / 4)) * context.AuroraStrength / 1000,
                0,
                1000);
            var target = targets[triangleIndex];
            if (target >= 0)
            {
                accumulation[target] = SaturatingAdd(
                    accumulation[target],
                    Math.Max(0, accumulation[triangleIndex] - retained));
            }
        }
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = context;
        var charged = false;
        foreach (var intensity in map.AuroraIntensity)
        {
            if (intensity is < 0 or > 1000)
            {
                throw new InvalidOperationException("Aurora intensity is outside its scalar range.");
            }

            charged |= intensity > 0;
        }

        if (!charged && context.AuroraStrength > 0)
        {
            throw new InvalidOperationException("Aurora generation produced no charged terrain.");
        }
    }

    private static int ColdestNeighbor(IceRemembersMicrogeographyMap map, int triangleIndex)
    {
        var target = -1;
        var targetTemperature = map.Physical.Temperature[triangleIndex];
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Physical.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 &&
                (map.Physical.Temperature[neighbor] < targetTemperature ||
                    (map.Physical.Temperature[neighbor] == targetTemperature &&
                        neighbor < triangleIndex && (target < 0 || neighbor < target))))
            {
                target = neighbor;
                targetTemperature = map.Physical.Temperature[neighbor];
            }
        }

        return target;
    }

    private static int[] ScalarOrder(Span<int> values)
    {
        var counts = new int[1001];
        foreach (var value in values)
        {
            counts[Math.Clamp(value, 0, 1000)]++;
        }

        var offset = 0;
        for (var index = 0; index < counts.Length; index++)
        {
            var count = counts[index];
            counts[index] = offset;
            offset += count;
        }

        var order = new int[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            var bucket = Math.Clamp(values[index], 0, 1000);
            order[counts[bucket]++] = index;
        }

        return order;
    }

    private static int SaturatingAdd(int first, int second) =>
        first > int.MaxValue - second ? int.MaxValue : first + second;
}
