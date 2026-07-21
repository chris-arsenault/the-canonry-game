// Map-generation regression diagnostics (see docs/analysis/map-generation-
// review-20260719.md). Console reporting tool; style rules relaxed on purpose.
#pragma warning disable S1541, S3776, MA0051, S3358, MA0193, S104, S1200, MA0048

namespace WorldGeneratorTool;

using Engine.Constants;
using Engine.WorldGen;

internal static partial class WorldDiagnosticsCli
{
    // Does the world read as a world, or merely as rough ground?
    //
    // Real reference points, for the shape of the answer rather than its
    // units. Continental high points span roughly fourfold, from Australia's
    // 2,228m to Everest's 8,849m, so a world where every landmass tops out the
    // same has no Himalaya and no Australia. Topographic isolation is heavily
    // skewed: nearly every peak is overtopped within a few kilometres, while a
    // handful stand hundreds or thousands clear. And most continental high
    // points sit surprisingly near the sea - Aconcagua about 160km, Denali and
    // Kilimanjaro about 250km - because subduction builds mountains along
    // margins; only collision interiors like the Himalaya stand far inland. A
    // world with every peak on the shore is missing collision; one with every
    // peak deep inland is missing subduction.
    private static void ReportOrography(PhysicalGeographyField field)
    {
        var distance = CoastDistances(field);
        var peaks = FindPeaks(field);
        Console.WriteLine("== Orography ==");
        ReportCollision(field, distance);
        ReportIsolation(field, peaks);
        ReportLandmassHighPoints(field, distance);
    }

    // Is continental collision happening at all, and where? A suture that only
    // ever forms on a shoreline is subduction wearing a collision's name.
    private static void ReportCollision(PhysicalGeographyField field, int[] distance)
    {
        var colliding = 0;
        var land = 0;
        List<long> collisionDistance = [];
        List<long> intensity = [];
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            land++;
            if (field.CrustCollisionIntensity[t] <= 0)
            {
                continue;
            }

            colliding++;
            collisionDistance.Add(distance[t]);
            intensity.Add(field.CrustCollisionIntensity[t]);
        }

        Console.WriteLine(
            $"  collision triangles={colliding} of {land} land " +
            $"({colliding * 100d / Math.Max(1, land):0.00}%)");
        PrintSlopeRow("collision coast distance", collisionDistance);
        PrintSlopeRow("collision intensity", intensity);
    }

    private static List<int> FindPeaks(PhysicalGeographyField field)
    {
        var peaks = new List<int>();
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            var higher = false;
            for (var edge = 0; edge < 3 && !higher; edge++)
            {
                var n = field.Grid.NeighborTriangleIndex(t, edge);
                higher = n >= 0 && field.Elevation[n] > field.Elevation[t];
            }

            if (!higher)
            {
                peaks.Add(t);
            }
        }

        return peaks;
    }

    // Distance from each peak to the nearest higher ground, capped so the
    // search stays affordable. Capped peaks are the interesting ones: they are
    // the summits nothing nearby overtops.
    private static void ReportIsolation(PhysicalGeographyField field, List<int> peaks)
    {
        const int Cap = 120;
        var sampled = peaks
            .OrderByDescending(peak => field.Elevation[peak])
            .Take(3000)
            .ToArray();
        var isolations = new List<long>(sampled.Length);
        var visited = new int[field.Grid.TriangleCount];
        Array.Fill(visited, -1);
        var frontier = new Queue<int>();
        for (var index = 0; index < sampled.Length; index++)
        {
            isolations.Add(IsolationOf(field, sampled[index], index, visited, frontier, Cap));
        }

        var histogram = new int[7];
        int[] edges = [1, 2, 4, 8, 16, 32, int.MaxValue];
        foreach (var isolation in isolations)
        {
            var bucket = 0;
            while (bucket < edges.Length - 1 && isolation > edges[bucket])
            {
                bucket++;
            }

            histogram[bucket]++;
        }

        PrintSlopeRow("isolation of top 3000 peaks", isolations);
        for (var i = 0; i < histogram.Length; i++)
        {
            var label = i == histogram.Length - 1 ? $">{edges[i - 1]}" : $"<={edges[i]}";
            Console.WriteLine(
                $"    isolation {label,6}: {histogram[i],6} " +
                $"({histogram[i] * 100d / Math.Max(1, isolations.Count):0.0}%)");
        }
    }

    private static long IsolationOf(
        PhysicalGeographyField field,
        int peak,
        int stamp,
        int[] visited,
        Queue<int> frontier,
        int cap)
    {
        frontier.Clear();
        frontier.Enqueue(peak);
        visited[peak] = stamp;
        var depth = 0;
        var remaining = 1;
        while (frontier.Count > 0 && depth < cap)
        {
            var current = frontier.Dequeue();
            remaining--;
            for (var edge = 0; edge < 3; edge++)
            {
                var n = field.Grid.NeighborTriangleIndex(current, edge);
                if (n < 0 || visited[n] == stamp)
                {
                    continue;
                }

                if (field.Elevation[n] > field.Elevation[peak])
                {
                    return depth + 1;
                }

                visited[n] = stamp;
                frontier.Enqueue(n);
            }

            if (remaining == 0)
            {
                depth++;
                remaining = frontier.Count;
            }
        }

        return cap;
    }

    private const int ContinentMinimumTriangles = 50000;

    private static void ReportLandmassHighPoints(PhysicalGeographyField field, int[] distance)
    {
        var sea = field.EffectiveSeaLevel;
        var size = new Dictionary<int, int>();
        var best = new Dictionary<int, int>();
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            var landmass = field.LandmassIds[t];
            if (landmass <= 0)
            {
                continue;
            }

            size[landmass] = size.GetValueOrDefault(landmass) + 1;
            if (!best.TryGetValue(landmass, out var current) ||
                field.Elevation[t] > field.Elevation[current])
            {
                best[landmass] = t;
            }
        }

        // A 5,000-triangle island is not a continent, and letting islands in
        // and out of the comparison made the spread swing between runs on
        // membership rather than on relief. Only real continents qualify.
        var continents = size
            .Where(entry => entry.Value >= ContinentMinimumTriangles)
            .OrderByDescending(entry => entry.Value)
            .Take(10)
            .ToArray();
        Console.WriteLine(
            $"  continents (>={ContinentMinimumTriangles} triangles): {continents.Length}");
        var highs = new List<long>();
        foreach (var (landmass, area) in continents)
        {
            var peak = best[landmass];
            var height = (field.Elevation[peak] - sea) / 1024L;
            highs.Add(height);
            Console.WriteLine(
                $"    landmass {landmass,4}: area={area,7} high={height,5} " +
                $"coastDistance={distance[peak],4}");
        }

        if (highs.Count > 1)
        {
            var sorted = highs.ToArray();
            Array.Sort(sorted);
            Console.WriteLine(
                $"  high-point spread: low={sorted[0]} high={sorted[^1]} " +
                $"ratio={sorted[^1] / (double)Math.Max(1, sorted[0]):0.00} " +
                $"(Earth is about 3.97: Australia 2228m to Everest 8849m)");
        }
    }
}
