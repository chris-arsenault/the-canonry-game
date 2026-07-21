// Map-generation regression diagnostics (see docs/analysis/map-generation-
// review-20260719.md). Console reporting tool; style rules relaxed on purpose.
#pragma warning disable S1541, S3776, MA0051, S3358, MA0193, S104, S1200, MA0048

namespace WorldGeneratorTool;

using Engine.Constants;
using Engine.WorldGen;

internal static partial class WorldDiagnosticsCli
{
    // Channel initiation in real landscapes follows contributing area AND
    // slope: steep ground channelises with a small catchment, flat ground needs
    // an enormous one. We use area alone. This asks whether slope carries
    // enough signal to discriminate at all - if flat and steep land sit in the
    // same part of the distribution, no channel rule can recover the
    // difference and the fix belongs upstream in relief.
    private static void ReportSlopeSeparation(PhysicalGeographyField field)
    {
        var landSlopes = new List<long>();
        var channelSlopes = new List<long>();
        var saturatedHexSlopes = new List<long>();
        var sparseHexSlopes = new List<long>();

        for (var hex = 0; hex < field.Grid.ConfiguredHexCount; hex++)
        {
            var hexIndex = field.Grid.ConfiguredHexIndexAt(hex);
            var channelCount = 0;
            var landCount = 0;
            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                var t = field.Grid.TriangleIndex(hexIndex, local);
                if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None)
                {
                    continue;
                }

                landCount++;
                if (field.RiverOrders[t] > 0)
                {
                    channelCount++;
                }
            }

            if (landCount == 0)
            {
                continue;
            }

            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                var t = field.Grid.TriangleIndex(hexIndex, local);
                if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None)
                {
                    continue;
                }

                var slope = field.Slope[t];
                landSlopes.Add(slope);
                if (field.RiverOrders[t] > 0)
                {
                    channelSlopes.Add(slope);
                }

                if (channelCount >= 20)
                {
                    saturatedHexSlopes.Add(slope);
                }
                else if (channelCount == 0)
                {
                    sparseHexSlopes.Add(slope);
                }
            }
        }

        Console.WriteLine("== Slope signal (fixed-point subunits) ==");
        PrintSlopeRow("all land", landSlopes);
        PrintSlopeRow("channel triangles", channelSlopes);
        PrintSlopeRow("in hexes >=20/24 channel", saturatedHexSlopes);
        PrintSlopeRow("in hexes with no channel", sparseHexSlopes);
    }

    private static void PrintSlopeRow(string label, List<long> values)
    {
        if (values.Count == 0)
        {
            Console.WriteLine($"  {label,-26}: none");
            return;
        }

        var sorted = values.ToArray();
        Array.Sort(sorted);
        Console.WriteLine(
            $"  {label,-26}: n={sorted.Length,7} p10={PercentileLong(sorted, 10),5} " +
            $"p50={PercentileLong(sorted, 50),5} p90={PercentileLong(sorted, 90),6} " +
            $"p99={PercentileLong(sorted, 99),6} mean={sorted.Average():0}");
    }

    // A drainage that fragments into many tiny basins cannot grow long rivers:
    // neighbouring strands belong to different drainages, so they run parallel
    // and never converge no matter how the channel threshold is tuned.
    private static void ReportBasinSizes(PhysicalGeographyField field)
    {
        var sizes = new Dictionary<int, int>();
        var landTriangles = 0;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] == PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            landTriangles++;
            var basin = field.DrainageBasinIds[t];
            if (basin > 0)
            {
                sizes[basin] = sizes.GetValueOrDefault(basin) + 1;
            }
        }

        if (sizes.Count == 0)
        {
            Console.WriteLine("  basins: none");
            return;
        }

        var ordered = sizes.Values.Order().ToArray();
        var total = ordered.Sum();
        Console.WriteLine(
            $"  basins={ordered.Length} over {landTriangles} non-ocean triangles; " +
            $"mean={total / (double)ordered.Length:0.0}");
        Console.WriteLine(
            $"    size p10={Percentile(ordered, 10)} p50={Percentile(ordered, 50)} " +
            $"p90={Percentile(ordered, 90)} p99={Percentile(ordered, 99)} max={ordered[^1]}");
        var tiny = ordered.Count(static size => size <= 24);
        var largest = ordered[^1] * 100d / total;
        Console.WriteLine(
            $"    basins <=1 hex: {tiny} ({tiny * 100d / ordered.Length:0.0}% of basins); " +
            $"largest basin holds {largest:0.0}% of drained land");
    }

    private static long PercentileLong(long[] ascending, int percentile) =>
        ascending[Math.Clamp(percentile * ascending.Length / 100, 0, ascending.Length - 1)];

    private static int Percentile(int[] ascending, int percentile) =>
        ascending[Math.Clamp(percentile * ascending.Length / 100, 0, ascending.Length - 1)];

}
