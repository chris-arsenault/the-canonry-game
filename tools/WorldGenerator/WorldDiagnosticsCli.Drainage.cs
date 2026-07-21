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
        Console.WriteLine(
            $"  scree threshold = {72 * 1024}; a floodplain is by definition low-gradient");
        PrintSlopeRow("all land", landSlopes);
        PrintSlopeRow("channel triangles", channelSlopes);
        PrintSlopeRow("in hexes >=20/24 channel", saturatedHexSlopes);
        PrintSlopeRow("in hexes with no channel", sparseHexSlopes);
        ReportFloodplainGradient(field);
        ReportSlopeByHeight(field);
        ReportErosionBudget(field);
        ReportReliefByCoastDistance(field);
        ReportTerrainStructure(field);
        ReportConvergenceCoupling(field);
        ReportOrography(field);
        ReportOverlayContinuity(field, ReconstructFlowTargets(field));
    }

    // Does the drawn river actually reach the sea? The spine network is
    // continuous by construction, but the overlay only draws one triangle per
    // cross-section, and if the deepest thread changes sides the drawn line
    // can step to a triangle that is not edge-adjacent - a river that breaks
    // on screen while the data underneath is fine.
    private static void ReportOverlayContinuity(PhysicalGeographyField field, int[] flowTargets)
    {
        var thalweg = new bool[field.Grid.TriangleCount];
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            thalweg[t] = field.RiverSpineEdgeMasks[t] != 0 && IsThalweg(field, flowTargets, t);
        }

        var drawn = 0;
        var breaks = 0;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (!thalweg[t])
            {
                continue;
            }

            drawn++;
            var target = flowTargets[t];
            if (target < 0)
            {
                continue;
            }

            var landsOnWater = field.WaterKindCodes[target] != PhysicalWaterKindCodes.None;
            if (!landsOnWater && !thalweg[target])
            {
                breaks++;
            }
        }

        Console.WriteLine(
            $"  overlay: drawn={drawn} breaks={breaks} " +
            $"({(drawn == 0 ? 0 : breaks * 100d / drawn):0.0}% of drawn triangles " +
            $"hand off to nothing drawn)");
    }

    private static bool IsThalweg(PhysicalGeographyField field, int[] flowTargets, int triangleIndex)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var n = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (n < 0 || field.RiverSpineEdgeMasks[n] == 0 ||
                n == flowTargets[triangleIndex] || flowTargets[n] == triangleIndex)
            {
                continue;
            }

            if (field.FlowAccumulation[n] > field.FlowAccumulation[triangleIndex])
            {
                return false;
            }
        }

        return true;
    }

    // Phase 2's gate: does tectonics show up in the ground? Interior triangles
    // on convergent boundaries should be measurably thicker, higher and
    // steeper than interior triangles that are not. If they are not, uplift is
    // reaching the crust without expressing itself as relief.
    private static void ReportConvergenceCoupling(PhysicalGeographyField field)
    {
        var distance = CoastDistances(field);
        var sea = field.EffectiveSeaLevel;
        List<long> convHeight = [];
        List<long> convSlope = [];
        List<long> convThickness = [];
        List<long> quietHeight = [];
        List<long> quietSlope = [];
        List<long> quietThickness = [];
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None || distance[t] < 25)
            {
                continue;
            }

            var convergent =
                field.TectonicBoundaryCodes[t] == PhysicalTectonicBoundaryCodes.Convergent;
            (convergent ? convHeight : quietHeight).Add((field.Elevation[t] - sea) / 1024L);
            (convergent ? convSlope : quietSlope).Add(field.Slope[t]);
            (convergent ? convThickness : quietThickness).Add(
                PhysicalElevation.ToUnits(field.CrustThickness[t]));
        }

        Console.WriteLine("== Convergence coupling (interior, d>=25) ==");
        PrintSlopeRow("convergent slope", convSlope);
        PrintSlopeRow("quiet slope", quietSlope);
        PrintSlopeRow("convergent height", convHeight);
        PrintSlopeRow("quiet height", quietHeight);
        PrintSlopeRow("convergent thickness", convThickness);
        PrintSlopeRow("quiet thickness", quietThickness);
    }

    // Gradient alone can be raised by tilting a plane, which would pass a
    // slope gate while producing no terrain. Divides are what a drainage
    // network organises around, so they are the honest test that relief is
    // structure rather than a ramp. Coastline counts ride along so a relief
    // change cannot soften coastlines unnoticed.
    private static int[] CoastDistances(PhysicalGeographyField field)
    {
        var distance = new int[field.Grid.TriangleCount];
        Array.Fill(distance, -1);
        var frontier = new Queue<int>();
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] == PhysicalWaterKindCodes.Ocean)
            {
                distance[t] = 0;
                frontier.Enqueue(t);
            }
        }

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(current, edge);
                if (neighbor >= 0 && distance[neighbor] < 0 &&
                    field.WaterKindCodes[neighbor] != PhysicalWaterKindCodes.Ocean)
                {
                    distance[neighbor] = distance[current] + 1;
                    frontier.Enqueue(neighbor);
                }
            }
        }

        return distance;
    }

    private static void ReportTerrainStructure(PhysicalGeographyField field)
    {
        var land = 0;
        var ocean = 0;
        var coastal = 0;
        var divides = 0;
        var pits = 0;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] == PhysicalWaterKindCodes.Ocean)
            {
                ocean++;
                continue;
            }

            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            land++;
            var higher = 0;
            var lower = 0;
            var neighbors = 0;
            var touchesOcean = false;
            for (var edge = 0; edge < 3; edge++)
            {
                var n = field.Grid.NeighborTriangleIndex(t, edge);
                if (n < 0)
                {
                    continue;
                }

                if (field.WaterKindCodes[n] == PhysicalWaterKindCodes.Ocean)
                {
                    touchesOcean = true;
                }

                neighbors++;
                if (field.Elevation[n] > field.Elevation[t])
                {
                    higher++;
                }
                else if (field.Elevation[n] < field.Elevation[t])
                {
                    lower++;
                }
            }

            if (touchesOcean)
            {
                coastal++;
            }

            if (neighbors > 0 && lower == neighbors)
            {
                divides++;
            }

            if (neighbors > 0 && higher == neighbors)
            {
                pits++;
            }
        }

        Console.WriteLine("== Terrain structure ==");
        Console.WriteLine(
            $"  land={land} ocean={ocean} coastal={coastal} " +
            $"(coast/land={coastal * 100d / Math.Max(1, land):0.00}%)");
        Console.WriteLine(
            $"  divides={divides} ({divides * 10000d / Math.Max(1, land):0.0} per 10k land) " +
            $"pits={pits} ({pits * 10000d / Math.Max(1, land):0.0} per 10k land)");
    }

    // Height above sea level conflates "far inland" with "high", because the
    // swells centre inland. Walking outward from the ocean separates them: it
    // shows whether local gradient decays with distance from the coast, which
    // is the signature of relief that lives only at the continental margin.
    private static void ReportReliefByCoastDistance(PhysicalGeographyField field)
    {
        var distance = CoastDistances(field);

        int[] bands = [1, 3, 6, 12, 25, 50, 100, 200, int.MaxValue];
        var slopes = new List<long>[bands.Length];
        var heights = new List<long>[bands.Length];
        var potentials = new List<long>[bands.Length];
        var basePotentials = new List<long>[bands.Length];
        for (var i = 0; i < bands.Length; i++)
        {
            slopes[i] = [];
            heights[i] = [];
            potentials[i] = [];
            basePotentials[i] = [];
        }

        var counts = new int[bands.Length];
        var convergent = new int[bands.Length];
        var strained = new int[bands.Length];
        var volcanic = new int[bands.Length];

        var sea = field.EffectiveSeaLevel;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None || distance[t] <= 0)
            {
                continue;
            }

            var band = 0;
            while (band < bands.Length - 1 && distance[t] >= bands[band + 1])
            {
                band++;
            }

            slopes[band].Add(field.Slope[t]);
            heights[band].Add((field.Elevation[t] - sea) / 1024L);
            potentials[band].Add(field.ContinentalPotential[t]);
            basePotentials[band].Add(field.CrustBasePotential[t]);
            counts[band]++;
            if (field.TectonicBoundaryCodes[t] == PhysicalTectonicBoundaryCodes.Convergent)
            {
                convergent[band]++;
            }

            if (field.TectonicStrain[t] >= 300)
            {
                strained[band]++;
            }

            if ((field.FeatureFlags[t] &
                (PhysicalGeographyFeatureCodes.VolcanicCone |
                    PhysicalGeographyFeatureCodes.VolcanicCaldera)) != 0)
            {
                volcanic[band]++;
            }
        }

        PrintCoastDistanceRows(bands, slopes, heights, potentials, basePotentials, counts, convergent, strained, volcanic);
    }

    private static void PrintCoastDistanceRows(
        int[] bands,
        List<long>[] slopes,
        List<long>[] heights,
        List<long>[] potentials,
        List<long>[] basePotentials,
        int[] counts,
        int[] convergent,
        int[] strained,
        int[] volcanic)
    {
        Console.WriteLine("== Relief by triangle distance from ocean ==");
        for (var i = 0; i < bands.Length; i++)
        {
            if (slopes[i].Count == 0)
            {
                continue;
            }

            var sortedHeight = heights[i].ToArray();
            Array.Sort(sortedHeight);
            var sortedPotential = potentials[i].ToArray();
            Array.Sort(sortedPotential);
            var sortedBase = basePotentials[i].ToArray();
            Array.Sort(sortedBase);
            var upper = i == bands.Length - 1 ? "+" : $"-{bands[i + 1]}";
            PrintSlopeRow(
                $"d{bands[i]}{upper} h={PercentileLong(sortedHeight, 50)} " +
                $"pot={PercentileLong(sortedPotential, 50)} " +
                $"base(p10={PercentileLong(sortedBase, 10)}," +
                $"p50={PercentileLong(sortedBase, 50)}," +
                $"p90={PercentileLong(sortedBase, 90)}) " +
                $"conv={convergent[i] * 100d / counts[i]:0.00}% " +
                $"strain={strained[i] * 100d / counts[i]:0.00}% " +
                $"volc={volcanic[i] * 100d / counts[i]:0.00}%",
                slopes[i]);
        }
    }

    // Relief is generated, so if the interior is flat something removed it.
    // Comparing cumulative erosion against the elevation spread says whether
    // erosion is planing the continents into a peneplain.
    private static void ReportErosionBudget(PhysicalGeographyField field)
    {
        var elevations = new List<long>();
        var erosion = new List<long>();
        var bedrockGap = new List<long>();
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            elevations.Add(field.Elevation[t]);
            erosion.Add(field.CumulativeFluvialErosion[t]);
            bedrockGap.Add(field.Elevation[t] - field.BedrockElevation[t]);
        }

        Console.WriteLine("== Erosion budget vs elevation spread (subunits) ==");
        PrintSlopeRow("land elevation", elevations);
        PrintSlopeRow("cumulative fluvial erosion", erosion);
        PrintSlopeRow("elevation - bedrock", bedrockGap);
        var sortedElevation = elevations.ToArray();
        Array.Sort(sortedElevation);
        var spread = PercentileLong(sortedElevation, 90) - PercentileLong(sortedElevation, 10);
        var sortedErosion = erosion.ToArray();
        Array.Sort(sortedErosion);
        Console.WriteLine(
            $"  p10-p90 elevation spread={spread}; median erosion={PercentileLong(sortedErosion, 50)} " +
            $"({(spread == 0 ? 0 : PercentileLong(sortedErosion, 50) * 100d / spread):0.0}% of spread)");
    }

    // A global slope spread cannot tell a mountainous world from a flat one
    // ringed by sea cliffs. This splits slope by height above sea level: if
    // relief lives only at the coast and the interior is a plain, then
    // headwaters have no gradient to organise a drainage around and the
    // parallel rills follow from that rather than from any channel rule.
    private static void ReportSlopeByHeight(PhysicalGeographyField field)
    {
        var sea = field.EffectiveSeaLevel;
        int[] bands = [0, 25, 60, 120, 250, 500, 1000, int.MaxValue];
        var slopes = new List<long>[bands.Length];
        var channelCounts = new int[bands.Length];
        for (var i = 0; i < bands.Length; i++)
        {
            slopes[i] = [];
        }

        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            var height = (field.Elevation[t] - sea) / 1024L;
            var band = 0;
            while (band < bands.Length - 1 && height >= bands[band + 1])
            {
                band++;
            }

            slopes[band].Add(field.Slope[t]);
            if (field.RiverOrders[t] > 0)
            {
                channelCounts[band]++;
            }
        }

        Console.WriteLine("== Slope by height above sea level ==");
        for (var i = 0; i < bands.Length; i++)
        {
            if (slopes[i].Count == 0)
            {
                continue;
            }

            var upper = i == bands.Length - 1 ? "+" : $"-{bands[i + 1]}";
            var channelShare = channelCounts[i] * 100d / slopes[i].Count;
            PrintSlopeRow($"{bands[i]}{upper} ({channelShare:0.0}% channel)", slopes[i]);
        }
    }

    // A hex reads floodplain once eight of its twenty-four triangles are
    // river-land, with nothing said about gradient. If those hexes are steep,
    // the label is a category error: that ground is a ravine, not a floodplain.
    private static void ReportFloodplainGradient(PhysicalGeographyField field)
    {
        const long ScreeSlope = 72 * 1024;
        var wouldBeFloodplain = 0;
        var steepFloodplain = 0;
        var medians = new List<long>();
        for (var hex = 0; hex < field.Grid.ConfiguredHexCount; hex++)
        {
            var hexIndex = field.Grid.ConfiguredHexIndexAt(hex);
            var riverLand = 0;
            var slopes = new List<long>();
            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                var t = field.Grid.TriangleIndex(hexIndex, local);
                if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None)
                {
                    continue;
                }

                slopes.Add(field.Slope[t]);
                if (field.RiverOrders[t] > 0)
                {
                    riverLand++;
                }
            }

            if (riverLand < 8 || slopes.Count == 0)
            {
                continue;
            }

            wouldBeFloodplain++;
            var sorted = slopes.ToArray();
            Array.Sort(sorted);
            var median = PercentileLong(sorted, 50);
            medians.Add(median);
            if (median >= ScreeSlope)
            {
                steepFloodplain++;
            }
        }

        Console.WriteLine(
            $"  hexes reading floodplain (>=8/24 channel): {wouldBeFloodplain}; " +
            $"of those, median slope at or above scree: {steepFloodplain} " +
            $"({(wouldBeFloodplain == 0 ? 0 : steepFloodplain * 100d / wouldBeFloodplain):0.0}%)");
        PrintSlopeRow("their per-hex median slope", medians);
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
