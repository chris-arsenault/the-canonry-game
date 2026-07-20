// Map-generation regression diagnostics (see docs/analysis/map-generation-
// review-20260719.md). Console reporting tool; style rules relaxed on purpose.
#pragma warning disable S1541, S3776, MA0051, S3358, MA0193, S104, S1200, MA0048

namespace WorldGeneratorTool;

using System.Globalization;

using Engine.Constants;
using Engine.WorldGen;

using IceRemembers;
using IceRemembers.Seed;

internal static class WorldDiagnosticsCli
{
    public static int Run(string[] args)
    {
        var seed = 42;
        var radius = 128;
        foreach (var argument in args)
        {
            if (argument.StartsWith("--seed=", StringComparison.Ordinal))
            {
                seed = int.Parse(argument["--seed=".Length..], CultureInfo.InvariantCulture);
            }
            else if (argument.StartsWith("--radius=", StringComparison.Ordinal))
            {
                radius = int.Parse(argument["--radius=".Length..], CultureInfo.InvariantCulture);
            }
        }

        if (args.Contains("--heart", StringComparer.Ordinal))
        {
            return ReportHeartTerrain(seed);
        }

        Console.WriteLine($"Generating radius={radius} seed={seed} with production profile...");
        var result = IceRemembersWorldGenerationOrchestrator.Generate(
            radius,
            seed,
            IceRemembersPhysicalGeography.GenerationProfile,
            IceRemembersPhysicalGeography.MicrogeographyProfile,
            IceRemembersPhysicalGeography.WorldGenerationEpochs);
        var field = result.Physical;
        Console.WriteLine("Generation complete. Computing diagnostics...\n");

        var flowTargets = ReconstructFlowTargets(field);
        ReportTieUsage(field);
        ReportDirectionAttribution(field);
        ReportRiverDirections(field, flowTargets);
        ReportRiverWindows(field, flowTargets);
        ReportRiverDrops(field, flowTargets);
        ReportConfluences(field, flowTargets);
        ReportElevation(field);
        ReportLandforms(field);
        ReportVolcanism(field);
        ReportStripes(field);
        return 0;
    }

    private static int ReportHeartTerrain(int seed)
    {
        Console.WriteLine($"Building accepted startup seed {seed} for heart terrain shares...");
        var result = IceRemembersSeedBuilder.BuildAcceptedStartupSeed(seed);
        var physical = result.StartupSeed.Cells
            .Where(static cell => cell.PlaneCode == Engine.Constants.PlaneCodes.Physical)
            .ToArray();
        Console.WriteLine($"accepted={result.Seed.BaseSeed}/{result.Seed.Attempt} cells={physical.Length}");
        foreach (var group in physical
            .GroupBy(static cell => cell.Terrain, StringComparer.Ordinal)
            .OrderByDescending(static group => group.Count()))
        {
            Console.WriteLine(
                $"  {group.Key}: {group.Count()} ({group.Count() * 100d / physical.Length:0.0}%)");
        }

        return 0;
    }

    private static int[] ReconstructFlowTargets(PhysicalGeographyField field)
    {
        var targets = new int[field.Grid.TriangleCount];
        for (var t = 0; t < targets.Length; t++)
        {
            var edge = field.FlowEdges[t];
            targets[t] = edge is >= 0 and <= 2 ? field.Grid.NeighborTriangleIndex(t, edge) : -1;
        }

        return targets;
    }

    private static void ReportTieUsage(PhysicalGeographyField field)
    {
        Console.WriteLine("== Tie-break usage (cumulative over all epochs; want near 0) ==");
        Console.WriteLine(
            $"  guidance tie decisions: {field.DrainageTieDecisionTotal} / " +
            $"{field.DrainageDecisionTotal} " +
            $"({Share(field.DrainageTieDecisionTotal, field.DrainageDecisionTotal):0.00}%)");
        Console.WriteLine(
            $"  flood flat fills: {field.FloodFlatFillTotal} / {field.FloodFillTotal} " +
            $"({Share(field.FloodFlatFillTotal, field.FloodFillTotal):0.00}%)");
    }

    private static double Share(long part, long total) => total <= 0
        ? 0d
        : part * 100d / total;

    private static void ReportDirectionAttribution(PhysicalGeographyField field)
    {
        Console.WriteLine("\n== Direction attribution (land triangles) ==");
        var parentBins = new long[12];
        var descentBins = new long[12];
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None ||
                !field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            AddParentChainDirection(field, parentBins, t);
            AddSteepestDescentDirection(field, descentBins, t);
        }

        var edgeBins = new long[12];
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (!field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(t, edge);
                if (neighbor >= 0)
                {
                    edgeBins[AngleBin(
                        field.Grid.TriangleX(neighbor) - field.Grid.TriangleX(t),
                        field.Grid.TriangleY(neighbor) - field.Grid.TriangleY(t))]++;
                }
            }
        }

        PrintBins("lattice edge availability", edgeBins);
        PrintBins("12-step flood-parent chain", parentBins);
        PrintBins("steepest floor descent", descentBins);
    }

    private static void AddParentChainDirection(
        PhysicalGeographyField field,
        long[] bins,
        int start)
    {
        var current = start;
        for (var step = 0; step < 12; step++)
        {
            var parent = field.DrainageParents[current];
            if (parent < 0)
            {
                break;
            }

            current = parent;
        }

        var dx = field.Grid.TriangleX(current) - field.Grid.TriangleX(start);
        var dy = field.Grid.TriangleY(current) - field.Grid.TriangleY(start);
        if ((dx * dx) + (dy * dy) >= 1.0)
        {
            bins[AngleBin(dx, dy)]++;
        }
    }

    private static void AddSteepestDescentDirection(
        PhysicalGeographyField field,
        long[] bins,
        int t)
    {
        var best = -1;
        var bestDrop = 0L;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(t, edge);
            if (neighbor < 0)
            {
                continue;
            }

            var drop = field.Elevation[t] - field.Elevation[neighbor];
            if (drop > bestDrop)
            {
                bestDrop = drop;
                best = neighbor;
            }
        }

        if (best >= 0)
        {
            bins[AngleBin(
                field.Grid.TriangleX(best) - field.Grid.TriangleX(t),
                field.Grid.TriangleY(best) - field.Grid.TriangleY(t))]++;
        }
    }

    private static void ReportRiverDirections(PhysicalGeographyField field, int[] flowTargets)
    {
        Console.WriteLine("== Spine step directions (all river-order triangles) ==");
        var bins = new long[12];
        var weighted = new long[12];
        var majorBins = new long[12];
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.RiverOrders[t] <= 0 || flowTargets[t] < 0 ||
                !field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            var target = flowTargets[t];
            var angleBin = AngleBin(
                field.Grid.TriangleX(target) - field.Grid.TriangleX(t),
                field.Grid.TriangleY(target) - field.Grid.TriangleY(t));
            bins[angleBin]++;
            weighted[angleBin] += field.FlowAccumulation[t];
            if (field.RiverOrders[t] >= 3)
            {
                majorBins[angleBin]++;
            }
        }

        PrintBins("count", bins);
        PrintBins("accum-weighted", weighted);
        PrintBins("order>=3 count", majorBins);
    }

    private static void ReportRiverWindows(PhysicalGeographyField field, int[] flowTargets)
    {
        Console.WriteLine("\n== 12-step downstream net displacement (spine triangles) ==");
        var bins = new long[12];
        var majorBins = new long[12];
        double straightSum = 0;
        long straightCount = 0;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.RiverOrders[t] <= 0 || !field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            var current = t;
            double pathLength = 0;
            var steps = 0;
            for (; steps < 12; steps++)
            {
                var next = flowTargets[current];
                if (next < 0)
                {
                    break;
                }

                var dx = field.Grid.TriangleX(next) - field.Grid.TriangleX(current);
                var dy = field.Grid.TriangleY(next) - field.Grid.TriangleY(current);
                pathLength += Math.Sqrt((dx * dx) + (dy * dy));
                current = next;
            }

            if (steps < 12)
            {
                continue;
            }

            var netX = field.Grid.TriangleX(current) - field.Grid.TriangleX(t);
            var netY = field.Grid.TriangleY(current) - field.Grid.TriangleY(t);
            var netLength = Math.Sqrt((netX * netX) + (netY * netY));
            if (netLength < 0.5d)
            {
                continue;
            }

            var bin = AngleBin(netX, netY);
            bins[bin]++;
            if (field.RiverOrders[t] >= 3)
            {
                majorBins[bin]++;
            }

            straightSum += netLength / Math.Max(0.0001d, pathLength);
            straightCount++;
        }

        PrintBins("count", bins);
        PrintBins("order>=3 count", majorBins);
        Console.WriteLine(
            $"  mean straightness (net/path over 12 steps): {straightSum / Math.Max(1, straightCount):0.000} " +
            $"({straightCount} windows)");
    }

    private static void ReportRiverDrops(PhysicalGeographyField field, int[] flowTargets)
    {
        Console.WriteLine("\n== Elevation drop per spine step (floor units) ==");
        var drops = new List<double>();
        long steep20 = 0;
        long steep50 = 0;
        long steep100 = 0;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.RiverOrders[t] <= 0 || flowTargets[t] < 0 ||
                !field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            var drop = (field.Elevation[t] - field.Elevation[flowTargets[t]]) /
                (double)PhysicalElevation.SubunitsPerUnit;
            drops.Add(drop);
            if (drop >= 20)
            {
                steep20++;
            }

            if (drop >= 50)
            {
                steep50++;
            }

            if (drop >= 100)
            {
                steep100++;
            }
        }

        drops.Sort();
        Console.WriteLine(
            $"  steps={drops.Count} p50={Percentile(drops, 50):0.00} p90={Percentile(drops, 90):0.00} " +
            $"p99={Percentile(drops, 99):0.00} max={(drops.Count > 0 ? drops[^1] : 0):0.00}");
        Console.WriteLine($"  drops >=20u: {steep20}, >=50u: {steep50}, >=100u: {steep100}");
    }

    private static void ReportConfluences(PhysicalGeographyField field, int[] flowTargets)
    {
        Console.WriteLine("\n== Network structure ==");
        var incoming = new int[field.Grid.TriangleCount];
        long channelTriangles = 0;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.RiverOrders[t] <= 0 || !field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            channelTriangles++;
            var target = flowTargets[t];
            if (target >= 0 && field.RiverOrders[target] > 0)
            {
                incoming[target]++;
            }
        }

        long confluences = 0;
        var orders = new long[12];
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.RiverOrders[t] <= 0 || !field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            if (incoming[t] >= 2)
            {
                confluences++;
            }

            orders[Math.Min(11, field.RiverOrders[t])]++;
        }

        var basins = new HashSet<int>();
        long lakeTriangles = 0;
        long deltas = 0;
        long floodplains = 0;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (!field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            if (field.RiverOrders[t] > 0 && field.DrainageBasinIds[t] > 0)
            {
                basins.Add(field.DrainageBasinIds[t]);
            }

            if (field.WaterKindCodes[t] == PhysicalWaterKindCodes.Lake)
            {
                lakeTriangles++;
            }

            if (field.DeltaIntensity[t] > 0)
            {
                deltas++;
            }

            if (field.FloodplainIntensity[t] > 0)
            {
                floodplains++;
            }
        }

        Console.WriteLine(
            $"  channel triangles={channelTriangles} confluences(>=2 in)={confluences} " +
            $"basins-with-rivers={basins.Count}");
        Console.WriteLine(
            $"  lake triangles={lakeTriangles} delta triangles={deltas} floodplain triangles={floodplains}");
        Console.Write("  Strahler order counts:");
        for (var order = 1; order < orders.Length; order++)
        {
            if (orders[order] > 0)
            {
                Console.Write($" {order}:{orders[order]}");
            }
        }

        Console.WriteLine();
    }

    private static void ReportElevation(PhysicalGeographyField field)
    {
        Console.WriteLine("\n== Land elevation above sea level (units) ==");
        long seaLevel = long.MinValue;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] == PhysicalWaterKindCodes.Ocean)
            {
                seaLevel = Math.Max(seaLevel, field.WaterSurfaceElevations[t]);
            }
        }

        var land = new List<double>();
        var slopes = new List<double>();
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None ||
                !field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            land.Add((field.Elevation[t] - seaLevel) / (double)PhysicalElevation.SubunitsPerUnit);
            slopes.Add(field.Slope[t] / (double)PhysicalElevation.SubunitsPerUnit);
        }

        land.Sort();
        slopes.Sort();
        Console.WriteLine(
            $"  land triangles={land.Count} p5={Percentile(land, 5):0.0} p25={Percentile(land, 25):0.0} " +
            $"p50={Percentile(land, 50):0.0} p75={Percentile(land, 75):0.0} p95={Percentile(land, 95):0.0} " +
            $"p99={Percentile(land, 99):0.0} max={(land.Count > 0 ? land[^1] : 0):0.0}");
        Console.WriteLine(
            $"  land slope units: p50={Percentile(slopes, 50):0.00} p90={Percentile(slopes, 90):0.00} " +
            $"p99={Percentile(slopes, 99):0.00} max={(slopes.Count > 0 ? slopes[^1] : 0):0.00}");
        long above200 = land.Count(value => value >= 200);
        long above500 = land.Count(value => value >= 500);
        long above800 = land.Count(value => value >= 800);
        Console.WriteLine($"  land >=200u: {above200}, >=500u: {above500}, >=800u: {above800}");
    }

    private static void ReportLandforms(PhysicalGeographyField field)
    {
        Console.WriteLine("\n== Landform codes (configured disk) ==");
        string[] names =
        [
            "none", "plain", "slope", "valley", "basin", "ridge", "peak", "pass",
            "seabed", "u-valley", "cirque", "fjord", "moraine", "outwash", "impact-basin", "impact-rim",
            "gorge", "waterfall",
        ];
        var counts = new long[names.Length];
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (!field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            var code = field.LandformCodes[t];
            if (code >= 0 && code < counts.Length)
            {
                counts[code]++;
            }
        }

        for (var code = 0; code < counts.Length; code++)
        {
            if (counts[code] > 0)
            {
                Console.WriteLine($"  {names[code]}: {counts[code]}");
            }
        }
    }

    private static void ReportVolcanism(PhysicalGeographyField field)
    {
        Console.WriteLine("\n== Volcanism ==");
        long constructed = 0;
        long constructedUnits = 0;
        long active = 0;
        long maxUnits = 0;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (!field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            var history = field.VolcanicConstructionHistory[t];
            if (history > 0)
            {
                constructed++;
                constructedUnits += history / PhysicalElevation.SubunitsPerUnit;
                maxUnits = Math.Max(maxUnits, history / PhysicalElevation.SubunitsPerUnit);
            }

            if (field.VolcanicActivity[t] > 0)
            {
                active++;
            }
        }

        Console.WriteLine(
            $"  triangles with construction={constructed} total-units={constructedUnits} " +
            $"max-units={maxUnits} active-triangles={active}");
    }

    private static void ReportStripes(PhysicalGeographyField field)
    {
        Console.WriteLine("\n== Stripe index (variance of bucket means / total variance) ==");
        ReportStripe(field, "Moisture", t => field.Moisture[t]);
        ReportStripe(field, "Rainfall", t => field.Rainfall[t]);
        ReportStripe(field, "Temperature", t => field.Temperature[t]);
        ReportStripe(
            field,
            "Elevation",
            t => field.Elevation[t] / (double)PhysicalElevation.SubunitsPerUnit);
    }

    private static void ReportStripe(
        PhysicalGeographyField field,
        string name,
        Func<int, double> value)
    {
        const double bucketWidth = 0.8660254d;
        var xBuckets = new Dictionary<int, (double Sum, long Count)>();
        var yBuckets = new Dictionary<int, (double Sum, long Count)>();
        double total = 0;
        double totalSquared = 0;
        long count = 0;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (!field.Grid.TriangleIsInsideConfiguredDisk(t))
            {
                continue;
            }

            var sample = value(t);
            total += sample;
            totalSquared += sample * sample;
            count++;
            var xBucket = (int)Math.Floor(field.Grid.TriangleX(t) / bucketWidth);
            var yBucket = (int)Math.Floor(field.Grid.TriangleY(t) / bucketWidth);
            xBuckets[xBucket] = xBuckets.TryGetValue(xBucket, out var x)
                ? (x.Sum + sample, x.Count + 1)
                : (sample, 1);
            yBuckets[yBucket] = yBuckets.TryGetValue(yBucket, out var y)
                ? (y.Sum + sample, y.Count + 1)
                : (sample, 1);
        }

        var mean = total / count;
        var variance = (totalSquared / count) - (mean * mean);
        Console.WriteLine(
            $"  {name}: X-stripe={BucketVariance(xBuckets, mean) / Math.Max(0.0001d, variance):0.0000} " +
            $"Y-stripe={BucketVariance(yBuckets, mean) / Math.Max(0.0001d, variance):0.0000}");
    }

    private static double BucketVariance(
        Dictionary<int, (double Sum, long Count)> buckets,
        double mean)
    {
        double weighted = 0;
        long total = 0;
        foreach (var bucket in buckets.Values)
        {
            var bucketMean = bucket.Sum / bucket.Count;
            weighted += bucket.Count * (bucketMean - mean) * (bucketMean - mean);
            total += bucket.Count;
        }

        return weighted / Math.Max(1, total);
    }

    private static double Percentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 0)
        {
            return 0;
        }

        var rank = (int)Math.Round(
            percentile / 100d * (sorted.Count - 1),
            MidpointRounding.AwayFromZero);
        return sorted[Math.Clamp(rank, 0, sorted.Count - 1)];
    }

    private static int AngleBin(double dx, double dy)
    {
        var angle = Math.Atan2(dy, dx) * 180d / Math.PI;
        if (angle < 0)
        {
            angle += 360d;
        }

        return (int)Math.Round(angle / 30d) % 12;
    }

    private static void PrintBins(string label, long[] bins)
    {
        var total = Math.Max(1, bins.Sum());
        Console.Write($"  {label} (total {bins.Sum()}):");
        for (var bin = 0; bin < bins.Length; bin++)
        {
            Console.Write($" {bin * 30}°:{bins[bin] * 100d / total:0.0}%");
        }

        Console.WriteLine();
    }
}
