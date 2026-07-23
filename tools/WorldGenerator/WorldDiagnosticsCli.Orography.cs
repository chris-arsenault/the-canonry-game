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
        ReportContinentalSutures(field, distance);
        ReportOrogenicSystems(field);
        ReportIsolation(field, peaks);
        ReportLandmassHighPoints(field, distance);
    }

    private static void ReportContinentalSutures(PhysicalGeographyField field, int[] distance)
    {
        var systems = new Dictionary<int, SutureStats>();
        var strengths = new List<long>();
        var coastDistances = new List<long>();
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            var pair = field.CrustSuturePairIds[t];
            if (pair <= 0 || !field.Grid.TriangleIsInsideContentDomain(t))
            {
                continue;
            }

            if (!systems.TryGetValue(pair, out var stats))
            {
                stats = new SutureStats();
                systems.Add(pair, stats);
            }

            stats.Add(field, t, distance[t]);
            strengths.Add(field.CrustSutureStrength[t]);
            if (field.WaterKindCodes[t] != PhysicalWaterKindCodes.Ocean)
            {
                coastDistances.Add(distance[t]);
            }
        }

        Console.WriteLine($"  material suture systems={systems.Count}");
        PrintSlopeRow("suture strength", strengths);
        PrintSlopeRow("suture coast distance", coastDistances);
        foreach (var (pair, stats) in systems
            .OrderByDescending(entry => entry.Value.MaximumHeight)
            .ThenByDescending(entry => entry.Value.Triangles)
            .Take(12))
        {
            Console.WriteLine(
                $"    suture {pair,7}: triangles={stats.Triangles,6} land={stats.Land,6} " +
                $"active={stats.Active,6} maxHeight={stats.MaximumHeight,5} " +
                $"maxCoastDistance={stats.MaximumCoastDistance,4}");
        }
    }

    private sealed class SutureStats
    {
        public int Triangles { get; private set; }

        public int Land { get; private set; }

        public int Active { get; private set; }

        public int MaximumCoastDistance { get; private set; }

        public long MaximumHeight { get; private set; } = long.MinValue;

        public void Add(PhysicalGeographyField field, int triangleIndex, int coastDistance)
        {
            Triangles++;
            Active += field.CrustCollisionIntensity[triangleIndex] > 0 ? 1 : 0;
            if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                return;
            }

            Land++;
            MaximumCoastDistance = Math.Max(MaximumCoastDistance, coastDistance);
            MaximumHeight = Math.Max(
                MaximumHeight,
                PhysicalElevation.ToUnits(
                    field.Elevation[triangleIndex] - field.EffectiveSeaLevel));
        }
    }

    private static void ReportOrogenicSystems(PhysicalGeographyField field)
    {
        var systems = new Dictionary<int, OrogenicSystemStats>();
        var clipped = 0;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (!field.Grid.TriangleIsInsideContentDomain(t))
            {
                continue;
            }

            var component = field.OrogenicComponentIds[t];
            if (component < 0)
            {
                continue;
            }

            if (!systems.TryGetValue(component, out var stats))
            {
                stats = new OrogenicSystemStats();
                systems.Add(component, stats);
            }

            stats.Add(field, t);
            if (field.CrustThickness[t] >=
                PhysicalElevation.FromUnits(PhysicalCrust.MaximumThicknessUnits))
            {
                clipped++;
            }
        }

        Console.WriteLine(
            $"  active orogenic systems={systems.Count}; influenced crust at ceiling={clipped}");
        foreach (var entry in systems
            .OrderByDescending(pair => pair.Value.Prominence)
            .ThenByDescending(pair => pair.Value.MaximumLandHeight)
            .Take(12))
        {
            var stats = entry.Value;
            Console.WriteLine(
                $"    system {entry.Key,3} pair={stats.BoundaryPair,7} " +
                $"mode={BoundaryName(stats.BoundaryCode),10} " +
                $"prom={stats.Prominence,4} " +
                $"boundary={stats.BoundaryTriangles,6} reach={stats.Triangles,7} " +
                $"land={stats.LandTriangles,7} high={stats.MaximumLandHeight,5} " +
                $"relief={stats.LandRelief,5} " +
                $"last-build=[{stats.MinimumConstruction,5},{stats.MaximumConstruction,5}] " +
                $"ceiling={stats.ClippedTriangles,5}");
        }
    }

    private static string BoundaryName(int code) => code switch
    {
        PhysicalTectonicBoundaryCodes.Convergent => "convergent",
        PhysicalTectonicBoundaryCodes.Divergent => "divergent",
        PhysicalTectonicBoundaryCodes.Transform => "transform",
        _ => "none",
    };

    private sealed class OrogenicSystemStats
    {
        public int BoundaryCode { get; private set; }

        public int BoundaryPair { get; private set; }

        public int BoundaryTriangles { get; private set; }

        public int Prominence { get; private set; }

        public int Triangles { get; private set; }

        public int LandTriangles { get; private set; }

        public int ClippedTriangles { get; private set; }

        public long MaximumLandHeight { get; private set; } = long.MinValue;

        public long MinimumLandHeight { get; private set; } = long.MaxValue;

        public long LandRelief => LandTriangles == 0
            ? 0
            : MaximumLandHeight - MinimumLandHeight;

        public long MinimumConstruction { get; private set; } = long.MaxValue;

        public long MaximumConstruction { get; private set; } = long.MinValue;

        public void Add(PhysicalGeographyField field, int triangleIndex)
        {
            Triangles++;
            Prominence = Math.Max(Prominence, field.OrogenicSystemProminence[triangleIndex]);
            if (field.TectonicBoundaryCodes[triangleIndex] != PhysicalTectonicBoundaryCodes.None)
            {
                BoundaryTriangles++;
                BoundaryCode = field.TectonicBoundaryCodes[triangleIndex];
                BoundaryPair = field.TectonicBoundaryPairIds[triangleIndex];
            }

            var construction = PhysicalElevation.ToUnits(field.OrogenicConstruction[triangleIndex]);
            MinimumConstruction = Math.Min(MinimumConstruction, construction);
            MaximumConstruction = Math.Max(MaximumConstruction, construction);
            ClippedTriangles += field.CrustThickness[triangleIndex] >=
                PhysicalElevation.FromUnits(PhysicalCrust.MaximumThicknessUnits) ? 1 : 0;
            if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None)
            {
                LandTriangles++;
                MaximumLandHeight = Math.Max(
                    MaximumLandHeight,
                    PhysicalElevation.ToUnits(
                        field.Elevation[triangleIndex] - field.EffectiveSeaLevel));
                MinimumLandHeight = Math.Min(
                    MinimumLandHeight,
                    PhysicalElevation.ToUnits(
                        field.Elevation[triangleIndex] - field.EffectiveSeaLevel));
            }
        }
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

    // The hex terrain ladder counts surfaces, so its cuts can only be
    // calibrated against what the surfaces actually are. Reporting only the
    // wet ones hid that the climate cuts had drifted out of step with the
    // world underneath them.
    public static void ReportLandSurfaceCensus(
        PhysicalGeographyField field,
        IReadOnlyList<string> surfaceNames,
        IReadOnlyList<int> surfaceIndexes)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var land = 0;
        for (var t = 0; t < field.Grid.TriangleCount; t++)
        {
            if (field.WaterKindCodes[t] == PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            land++;
            var index = surfaceIndexes[t];
            var name = index >= 0 && index < surfaceNames.Count ? surfaceNames[index] : "?";
            counts[name] = counts.GetValueOrDefault(name) + 1;
        }

        Console.WriteLine($"== Land surface census ({land} non-ocean triangles) ==");
        foreach (var entry in counts.OrderByDescending(pair => pair.Value).Take(16))
        {
            Console.WriteLine(
                $"    {entry.Key,-20} {entry.Value,7} ({entry.Value * 100d / Math.Max(1, land):0.0}%)");
        }
    }

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
