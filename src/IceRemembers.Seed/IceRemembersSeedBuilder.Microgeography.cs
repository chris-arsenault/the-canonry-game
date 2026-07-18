namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.Domain.Startup;
using Engine.WorldGen;

using IceRemembers;

internal static partial class IceRemembersSeedBuilder
{
    private const double SeaLevel = 0.02;
    private const int ElevationScale = 1000;

    private static readonly IceRemembersTerrain[] PhysicalMaterialPalette =
    [
        IceRemembersTerrain.Wastes,
        IceRemembersTerrain.Crags,
        IceRemembersTerrain.Tundra,
        IceRemembersTerrain.Ice,
        IceRemembersTerrain.HotSprings,
        IceRemembersTerrain.Shelf,
        IceRemembersTerrain.Reef,
        IceRemembersTerrain.Ocean,
    ];

    private readonly record struct PhysicalGeographyBuild(
        HexGrid HeartGrid,
        MapSubdivisionMap HeartSubdivisions,
        DomainPhysicalGeographyBlueprint Blueprint);

    private readonly record struct MicroSample(
        int MaterialIndex,
        int Elevation,
        int Temperature,
        int Moisture,
        int Geothermal,
        int HydrologyFlags);

    private static PhysicalGeographyBuild CreatePhysicalGeography(int salt)
    {
        var catalog = IceRemembersTerrainCatalog.CreateWorldGenCatalog();
        var fieldGrid = new HexGrid(
            GenerationRadius,
            catalog,
            IceRemembersTerrain.Wastes.ToDomainName());
        var fieldSubdivisions = CreateSubdivisions(fieldGrid, salt);
        var cells = new List<DomainPhysicalGeographyCell>(HexCellCount(GenerationRadius));
        var microtriangles = new List<DomainPhysicalMicrotriangle>(
            checked(HexCellCount(GenerationRadius) * PhysicalGeographyCodes.MicrotrianglesPerHex));

        var sampleBuffer = new MicroSample[PhysicalGeographyCodes.MicrotrianglesPerHex];
        foreach (var fieldCell in fieldGrid.AllCells.OrderBy(cell => cell.Coord.Q).ThenBy(cell => cell.Coord.R))
        {
            var coord = fieldCell.Coord;
            var microtriangleOffset = microtriangles.Count;
            global::System.Span<MicroSample> samples = sampleBuffer;
            GenerateMicrotriangles(coord, salt, samples);
            if (TouchesAnotherContinent(fieldGrid, fieldSubdivisions, coord))
            {
                ApplyContinentBoundaryWater(coord, salt, samples);
            }

            var baseTerrain = SummarizeBaseTerrain(samples);
            SetTerrain(fieldGrid, coord, baseTerrain);
            var subdivision = fieldSubdivisions.GetCell(coord);
            cells.Add(SummarizeCell(coord, subdivision, baseTerrain, microtriangleOffset, samples));
            for (var triangleIndex = 0; triangleIndex < samples.Length; triangleIndex++)
            {
                var sample = samples[triangleIndex];
                microtriangles.Add(ToDomainMicrotriangle(sample));
            }
        }

        var heartGrid = new HexGrid(
            PhysicalHeartRadius,
            catalog,
            IceRemembersTerrain.Wastes.ToDomainName());
        var heartSubdivisions = new MapSubdivisionMap();
        foreach (var heartCell in heartGrid.AllCells)
        {
            var source = fieldGrid.GetCell(heartCell.Coord) ?? throw new InvalidOperationException();
            heartGrid.SetCell(heartCell.Coord, source.Terrain.StableName);
            var subdivision = fieldSubdivisions.GetCell(heartCell.Coord);
            heartSubdivisions.AssignCell(heartCell.Coord, subdivision.SuperregionId, subdivision.ContinentId);
        }

        var blueprint = new DomainPhysicalGeographyBlueprint(
            GenerationRadius,
            PhysicalMaterialPalette.Select(terrain => terrain.ToDomainName()).ToArray(),
            cells.ToArray(),
            microtriangles.ToArray());
        blueprint.Validate();
        return new PhysicalGeographyBuild(heartGrid, heartSubdivisions, blueprint);
    }

    private static bool TouchesAnotherContinent(
        HexGrid grid,
        MapSubdivisionMap subdivisions,
        HexCoord coord)
    {
        var continentId = subdivisions.GetCell(coord).ContinentId;
        foreach (var neighbor in coord.Neighbors())
        {
            if (grid.ContainsCoord(neighbor) && subdivisions.GetCell(neighbor).ContinentId != continentId)
            {
                return true;
            }
        }

        return false;
    }

    private static void ApplyContinentBoundaryWater(
        HexCoord coord,
        int salt,
        global::System.Span<MicroSample> samples)
    {
        var reef = CellNoise(coord, salt ^ 0x4a17) >= 0.5f;
        for (var triangleIndex = 0; triangleIndex < 16; triangleIndex++)
        {
            var material = IceRemembersTerrain.Ocean;
            if (triangleIndex >= 8)
            {
                material = reef ? IceRemembersTerrain.Reef : IceRemembersTerrain.Shelf;
            }

            var water = material is IceRemembersTerrain.Ocean or IceRemembersTerrain.Reef
                ? PhysicalGeographyCodes.HydrologyWater
                : 0;
            samples[triangleIndex] = new MicroSample(
                (int)material,
                ScaleElevation(SeaLevel - 0.04d),
                250,
                900,
                0,
                water | PhysicalGeographyCodes.HydrologyCoast);
        }
    }

    private static void GenerateMicrotriangles(HexCoord coord, int salt, global::System.Span<MicroSample> samples)
    {
        var centerX = global::System.Math.Sqrt(3d) * (coord.Q + (coord.R * 0.5d));
        var centerY = 1.5d * coord.R;
        for (var sector = 0; sector < PhysicalGeographyCodes.HexSectorCount; sector++)
        {
            var angleA = ((sector * 60d) - 30d) * (global::System.Math.PI / 180d);
            var angleB = (((sector + 1) * 60d) - 30d) * (global::System.Math.PI / 180d);
            var ax = global::System.Math.Cos(angleA);
            var ay = global::System.Math.Sin(angleA);
            var bx = global::System.Math.Cos(angleB);
            var by = global::System.Math.Sin(angleB);
            var midpointAx = ax * 0.5d;
            var midpointAy = ay * 0.5d;
            var midpointBx = bx * 0.5d;
            var midpointBy = by * 0.5d;
            var midpointABx = (ax + bx) * 0.5d;
            var midpointABy = (ay + by) * 0.5d;
            WriteMicroSample(samples, sector, 0, centerX + ((midpointAx + midpointBx) / 3d), centerY + ((midpointAy + midpointBy) / 3d), salt);
            WriteMicroSample(samples, sector, 1, centerX + ((ax + midpointABx + midpointAx) / 3d), centerY + ((ay + midpointABy + midpointAy) / 3d), salt);
            WriteMicroSample(samples, sector, 2, centerX + ((bx + midpointBx + midpointABx) / 3d), centerY + ((by + midpointBy + midpointABy) / 3d), salt);
            WriteMicroSample(samples, sector, 3, centerX + ((midpointAx + midpointABx + midpointBx) / 3d), centerY + ((midpointAy + midpointABy + midpointBy) / 3d), salt);
        }
    }

    private static void WriteMicroSample(
        global::System.Span<MicroSample> samples,
        int sector,
        int localTriangle,
        double x,
        double y,
        int salt)
    {
        var elevation = ElevationAt(x, y, salt);
        var cold = UnitNoise(x, y, salt ^ 0x2f31, 16d, 3);
        var geothermal = UnitNoise(x, y, salt ^ 0x51d7, 10d, 3);
        var arid = UnitNoise(x, y, salt ^ 0x6bb1, 13d, 3);
        var material = MaterialAt(elevation, cold, geothermal, arid);
        var flags = HydrologyAt(x, y, elevation, material, salt);
        samples[(sector * PhysicalGeographyCodes.MicrotrianglesPerSector) + localTriangle] =
            new MicroSample(
                (int)material,
                ScaleElevation(elevation),
                ScaleUnit(1d - cold),
                ScaleUnit(1d - arid),
                ScaleUnit(geothermal),
                flags);
    }

    private static DomainPhysicalMicrotriangle ToDomainMicrotriangle(MicroSample sample)
    {
        var material = (IceRemembersTerrain)sample.MaterialIndex;
        var waterKind = LegacyWaterKind(material);
        return new DomainPhysicalMicrotriangle(
            sample.MaterialIndex,
            LegacyLandform(material),
            sample.Elevation,
            sample.Temperature,
            sample.Moisture,
            sample.Geothermal,
            waterKind,
            waterKind == PhysicalWaterKindCodes.None
                ? 0
                : global::System.Math.Max(1, ScaleElevation(SeaLevel) - sample.Elevation),
            waterKind == PhysicalWaterKindCodes.Ocean ? 1 : 0,
            0,
            PhysicalGeographyCodes.NoFlowEdge,
            (sample.HydrologyFlags & PhysicalGeographyCodes.HydrologyRiver) != 0 ? 1 : 0,
            (sample.HydrologyFlags & PhysicalGeographyCodes.HydrologyRiver) != 0 ? 1 : 0,
            LegacyFeatureFlags(material));
    }

    private static int LegacyWaterKind(IceRemembersTerrain material) =>
        material is IceRemembersTerrain.Ocean or IceRemembersTerrain.Reef or IceRemembersTerrain.Shelf
            ? PhysicalWaterKindCodes.Ocean
            : PhysicalWaterKindCodes.None;

    private static int LegacyLandform(IceRemembersTerrain material) =>
        material switch
        {
            IceRemembersTerrain.Ocean or IceRemembersTerrain.Reef or IceRemembersTerrain.Shelf =>
                PhysicalLandformCodes.Seabed,
            IceRemembersTerrain.Crags => PhysicalLandformCodes.Ridge,
            _ => PhysicalLandformCodes.Plain,
        };

    private static int LegacyFeatureFlags(IceRemembersTerrain material) =>
        material == IceRemembersTerrain.HotSprings
            ? PhysicalGeographyFeatureCodes.GeothermalVent
            : 0;

    private static double ElevationAt(double x, double y, int salt)
    {
        var macro = FractalNoise(x, y, salt ^ 0x1451, 24d, 4);
        var ridgeNoise = FractalNoise(x, y, salt ^ 0x3ac7, 8d, 3);
        var ridge = (1d - global::System.Math.Abs(ridgeNoise)) * 0.2d;
        var detail = FractalNoise(x, y, salt ^ 0x792d, 3d, 2) * 0.16d;
        var axialRadius = global::System.Math.Max(global::System.Math.Abs(x) / 1.73d, global::System.Math.Abs(y) / 1.5d);
        var edgeFalloff = global::System.Math.Max(0d, (axialRadius - 104d) / 24d);
        return (macro * 0.64d) + ridge + detail - (edgeFalloff * 0.85d);
    }

    private static IceRemembersTerrain MaterialAt(
        double elevation,
        double cold,
        double geothermal,
        double arid)
    {
        if (elevation < SeaLevel)
        {
            return cold > 0.78d ? IceRemembersTerrain.Ice : IceRemembersTerrain.Ocean;
        }

        if (elevation < SeaLevel + 0.1d)
        {
            return cold > 0.64d ? IceRemembersTerrain.Shelf : IceRemembersTerrain.Reef;
        }

        if (elevation > 0.26d)
        {
            return IceRemembersTerrain.Crags;
        }

        if (geothermal > 0.71d && elevation < 0.32d)
        {
            return IceRemembersTerrain.HotSprings;
        }

        if (cold > 0.66d)
        {
            return IceRemembersTerrain.Ice;
        }

        return arid > 0.63d ? IceRemembersTerrain.Wastes : IceRemembersTerrain.Tundra;
    }

    private static int HydrologyAt(
        double x,
        double y,
        double elevation,
        IceRemembersTerrain material,
        int salt)
    {
        var flags = 0;
        if (material is IceRemembersTerrain.Ocean or IceRemembersTerrain.Reef)
        {
            flags |= PhysicalGeographyCodes.HydrologyWater;
        }

        if (global::System.Math.Abs(elevation - SeaLevel) < 0.12d)
        {
            flags |= PhysicalGeographyCodes.HydrologyCoast;
        }

        var phase = HashUnit(salt, 17, 31) * global::System.Math.PI * 2d;
        var offsetA = (HashUnit(salt, 43, 71) - 0.5d) * 70d;
        var offsetB = (HashUnit(salt, 89, 113) - 0.5d) * 70d;
        var channelA = global::System.Math.Abs(y - (global::System.Math.Sin((x * 0.045d) + phase) * 11d) - offsetA);
        var channelB = global::System.Math.Abs((0.55d * x) + y - (global::System.Math.Sin((y * 0.038d) - phase) * 9d) - offsetB);
        if (elevation >= SeaLevel + 0.025d && elevation < 0.58d && global::System.Math.Min(channelA, channelB) < 0.5d)
        {
            flags |= PhysicalGeographyCodes.HydrologyRiver;
        }

        return flags;
    }

    private static DomainPhysicalGeographyCell SummarizeCell(
        HexCoord coord,
        MapSubdivisionCell subdivision,
        IceRemembersTerrain baseTerrain,
        int microtriangleOffset,
        global::System.ReadOnlySpan<MicroSample> samples)
        => LegacyMicroSummary.Create(coord, samples).ToCell(
            coord,
            subdivision,
            baseTerrain,
            microtriangleOffset,
            samples.Length);

    private struct LegacyMicroSummary
    {
        private int _dominantMaterialIndex;
        private int _elevationTotal;
        private int _temperatureTotal;
        private int _moistureTotal;
        private int _geothermalTotal;
        private int _minElevation;
        private int _maxElevation;
        private int _oceanCoverage;
        private int _permanentIceCoverage;
        private int _riverCoverage;
        private int _riverEdgeMask;
        private int _coastEdgeMask;
        private int _roughness;

        public static LegacyMicroSummary Create(
            HexCoord coord,
            global::System.ReadOnlySpan<MicroSample> samples)
        {
            var summary = new LegacyMicroSummary
            {
                _minElevation = int.MaxValue,
                _maxElevation = int.MinValue,
            };
            global::System.Span<int> materialCounts = stackalloc int[PhysicalMaterialPalette.Length];
            for (var index = 0; index < samples.Length; index++)
            {
                materialCounts[samples[index].MaterialIndex]++;
                summary.Add(samples[index], index);
            }

            summary._dominantMaterialIndex = (int)DominantTerrain(materialCounts, PhysicalMaterialPalette);
            summary._roughness = MeasureRoughness(coord, samples);
            return summary;
        }

        private void Add(MicroSample sample, int triangleIndex)
        {
            _elevationTotal += sample.Elevation;
            _temperatureTotal += sample.Temperature;
            _moistureTotal += sample.Moisture;
            _geothermalTotal += sample.Geothermal;
            _minElevation = global::System.Math.Min(_minElevation, sample.Elevation);
            _maxElevation = global::System.Math.Max(_maxElevation, sample.Elevation);
            var material = (IceRemembersTerrain)sample.MaterialIndex;
            if (LegacyWaterKind(material) == PhysicalWaterKindCodes.Ocean)
            {
                _oceanCoverage++;
            }

            if (material == IceRemembersTerrain.Ice)
            {
                _permanentIceCoverage++;
            }

            if ((sample.HydrologyFlags & PhysicalGeographyCodes.HydrologyRiver) != 0)
            {
                _riverCoverage++;
            }

            AddEdgeFlags(sample, triangleIndex);
        }

        private void AddEdgeFlags(MicroSample sample, int triangleIndex)
        {
            var local = triangleIndex % PhysicalGeographyCodes.MicrotrianglesPerSector;
            if (local is not (1 or 2))
            {
                return;
            }

            var edgeMask = 1 << (triangleIndex / PhysicalGeographyCodes.MicrotrianglesPerSector);
            if ((sample.HydrologyFlags & PhysicalGeographyCodes.HydrologyRiver) != 0)
            {
                _riverEdgeMask |= edgeMask;
            }

            if ((sample.HydrologyFlags & PhysicalGeographyCodes.HydrologyCoast) != 0)
            {
                _coastEdgeMask |= edgeMask;
            }
        }

        public readonly DomainPhysicalGeographyCell ToCell(
            HexCoord coord,
            MapSubdivisionCell subdivision,
            IceRemembersTerrain baseTerrain,
            int microtriangleOffset,
            int sampleCount) =>
            new(
                coord.Q,
                coord.R,
                baseTerrain.ToDomainName(),
                subdivision.SuperregionId,
                subdivision.ContinentId,
                microtriangleOffset,
                _dominantMaterialIndex,
                _elevationTotal / sampleCount,
                _minElevation,
                _maxElevation,
                _maxElevation - _minElevation,
                _roughness,
                _temperatureTotal / sampleCount,
                _moistureTotal / sampleCount,
                _geothermalTotal / sampleCount,
                sampleCount - _oceanCoverage,
                _oceanCoverage,
                _oceanCoverage,
                0,
                _permanentIceCoverage,
                _riverCoverage,
                0,
                _riverEdgeMask,
                _coastEdgeMask,
                0,
                0,
                0);

        private static int MeasureRoughness(
            HexCoord coord,
            global::System.ReadOnlySpan<MicroSample> samples)
        {
            var total = 0;
            var count = 0;
            for (var triangleIndex = 0; triangleIndex < samples.Length; triangleIndex++)
            {
                for (var edge = 0; edge < 3; edge++)
                {
                    PhysicalMicrotriangleTopology.ResolveNeighbor(
                        coord.Q,
                        coord.R,
                        triangleIndex,
                        edge,
                        out var neighborQ,
                        out var neighborR,
                        out var neighborTriangleIndex);
                    if (neighborQ == coord.Q && neighborR == coord.R && neighborTriangleIndex > triangleIndex)
                    {
                        total += global::System.Math.Abs(
                            samples[triangleIndex].Elevation - samples[neighborTriangleIndex].Elevation);
                        count++;
                    }
                }
            }

            return count == 0 ? 0 : total / count;
        }
    }

    private static IceRemembersTerrain SummarizeBaseTerrain(global::System.ReadOnlySpan<MicroSample> samples)
    {
        global::System.Span<int> counts = stackalloc int[PhysicalMaterialPalette.Length];
        for (var triangleIndex = 0; triangleIndex < samples.Length; triangleIndex++)
        {
            counts[samples[triangleIndex].MaterialIndex]++;
        }

        var wet = counts[(int)IceRemembersTerrain.Ocean] +
            counts[(int)IceRemembersTerrain.Reef] +
            counts[(int)IceRemembersTerrain.Shelf];
        if (wet >= 12)
        {
            return DominantTerrain(counts, IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf);
        }

        if (counts[(int)IceRemembersTerrain.Crags] >= 3)
        {
            return IceRemembersTerrain.Crags;
        }

        if (counts[(int)IceRemembersTerrain.HotSprings] >= 3)
        {
            return IceRemembersTerrain.HotSprings;
        }

        if (counts[(int)IceRemembersTerrain.Ice] >= 9)
        {
            return IceRemembersTerrain.Ice;
        }

        if (counts[(int)IceRemembersTerrain.Wastes] >= 8)
        {
            return IceRemembersTerrain.Wastes;
        }

        return DominantTerrain(counts, PhysicalMaterialPalette);
    }

    private static IceRemembersTerrain DominantTerrain(
        global::System.ReadOnlySpan<int> counts,
        params IceRemembersTerrain[] candidates)
    {
        var best = candidates[0];
        var bestCount = counts[(int)best];
        for (var i = 1; i < candidates.Length; i++)
        {
            var candidate = candidates[i];
            if (counts[(int)candidate] > bestCount)
            {
                best = candidate;
                bestCount = counts[(int)candidate];
            }
        }

        return best;
    }

    private static double UnitNoise(double x, double y, int salt, double scale, int octaves) =>
        (FractalNoise(x, y, salt, scale, octaves) + 1d) * 0.5d;

    private static double FractalNoise(double x, double y, int salt, double scale, int octaves)
    {
        var amplitude = 1d;
        var amplitudeTotal = 0d;
        var value = 0d;
        for (var octave = 0; octave < octaves; octave++)
        {
            value += ValueNoise(x / scale, y / scale, salt + (octave * 7919)) * amplitude;
            amplitudeTotal += amplitude;
            amplitude *= 0.5d;
            scale *= 0.5d;
        }

        return value / amplitudeTotal;
    }

    private static double ValueNoise(double x, double y, int salt)
    {
        var x0 = (int)global::System.Math.Floor(x);
        var y0 = (int)global::System.Math.Floor(y);
        var tx = Smooth(x - x0);
        var ty = Smooth(y - y0);
        var a = Lerp(HashSigned(salt, x0, y0), HashSigned(salt, x0 + 1, y0), tx);
        var b = Lerp(HashSigned(salt, x0, y0 + 1), HashSigned(salt, x0 + 1, y0 + 1), tx);
        return Lerp(a, b, ty);
    }

    private static double HashSigned(int salt, int x, int y) => (HashUnit(salt, x, y) * 2d) - 1d;

    private static double HashUnit(int salt, int x, int y)
    {
        unchecked
        {
            var hash = salt;
            hash = (hash * 397) ^ x;
            hash = (hash * 397) ^ y;
            hash ^= hash >> 13;
            hash *= 16777619;
            return (hash & 0x7fffffff) / (double)int.MaxValue;
        }
    }

    private static double Smooth(double value) => value * value * (3d - (2d * value));

    private static double Lerp(double a, double b, double amount) => a + ((b - a) * amount);

    private static int ScaleElevation(double elevation) =>
        (int)global::System.Math.Round(
            global::System.Math.Clamp(elevation, -2d, 2d) * ElevationScale,
            global::System.MidpointRounding.AwayFromZero);

    private static int ScaleUnit(double value) =>
        (int)global::System.Math.Round(
            global::System.Math.Clamp(value, 0d, 1d) * 1000d,
            global::System.MidpointRounding.AwayFromZero);

    private static int HexCellCount(int radius) => checked((3 * radius * radius) + (3 * radius) + 1);
}
