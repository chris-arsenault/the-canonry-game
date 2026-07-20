namespace Engine.Projection;

using Engine.Constants;
using Engine.Domain.Startup;

/// <summary>
/// Immutable presentation snapshot of the complete latent physical geography.
/// It is built from startup data before the simulation begins and never reads
/// live kernel state.
/// </summary>
public sealed class PhysicalGeographyProjection
{
    private readonly string[] _materialPalette;
    private readonly string[] _surfacePalette;
    private readonly string[] _featurePalette;
    private readonly string[] _scalarFieldNames;
    private readonly int[][] _scalarFieldValues;
    private readonly Dictionary<string, int> _scalarFieldIndexes;
    private readonly PhysicalGeographyCellProjection[] _cells;
    private readonly PhysicalMicrotriangleProjection[] _microtriangles;
    private readonly int[] _cellIndexes;
    private readonly int _diameter;

    private const int AreaClassNone = 0;
    private const int AreaClassOcean = 1;
    private const int AreaClassLake = 2;
    private const int AreaClassFloodplain = 3;

    private PhysicalGeographyProjection(
        int radius,
        int generationEpochCount,
        string[] materialPalette,
        string[] surfacePalette,
        string[] featurePalette,
        string[] scalarFieldNames,
        int[][] scalarFieldValues,
        PhysicalGeographyCellProjection[] cells,
        PhysicalMicrotriangleProjection[] microtriangles)
    {
        Radius = radius;
        GenerationEpochCount = generationEpochCount;
        _materialPalette = materialPalette;
        _surfacePalette = surfacePalette;
        _featurePalette = featurePalette;
        _scalarFieldNames = scalarFieldNames;
        _scalarFieldValues = scalarFieldValues;
        _scalarFieldIndexes = new Dictionary<string, int>(scalarFieldNames.Length, StringComparer.Ordinal);
        for (var index = 0; index < scalarFieldNames.Length; index++)
        {
            _scalarFieldIndexes.Add(scalarFieldNames[index], index);
        }

        _cells = cells;
        _microtriangles = microtriangles;
        _diameter = checked((radius * 2) + 1);
        _cellIndexes = new int[checked(_diameter * _diameter)];
        Array.Fill(_cellIndexes, -1);
        for (var index = 0; index < cells.Length; index++)
        {
            var coord = cells[index].Coord;
            _cellIndexes[SquareIndex(coord.Q, coord.R)] = index;
        }

        BuildAreaOutlineEdgeMasks();
    }

    // Rivers are lines and read as lines; oceans, lakes, and floodplains are
    // areas and need to read as areas. Marking every edge where a triangle
    // leaves its own area class gives each body a continuous outline that
    // closes across hex seams, so a floodplain reads as one shape rather than
    // as the river veins that happen to cross it. Derived here rather than
    // carried through worldgen and the seed cache because it is a display
    // fact recomputable from water kind and floodplain intensity.
    private void BuildAreaOutlineEdgeMasks()
    {
        for (var cellIndex = 0; cellIndex < _cells.Length; cellIndex++)
        {
            var cell = _cells[cellIndex];
            var offset = cell.MicrotriangleOffset;
            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                var triangleIndex = offset + local;
                var own = AreaClassOf(_microtriangles[triangleIndex]);
                if (own == AreaClassNone)
                {
                    continue;
                }

                var mask = AreaBoundaryEdges(cell.Coord, local, own);
                if (mask != 0)
                {
                    _microtriangles[triangleIndex] =
                        _microtriangles[triangleIndex] with { AreaOutlineEdgeMask = mask };
                }
            }
        }
    }

    private int AreaBoundaryEdges(HexCoord coord, int localTriangle, int ownClass)
    {
        var mask = 0;
        for (var edge = 0; edge < 3; edge++)
        {
            PhysicalMicrotriangleTopology.ResolveNeighbor(
                coord.Q,
                coord.R,
                localTriangle,
                edge,
                out var neighborQ,
                out var neighborR,
                out var neighborLocal);

            if (AreaClassAt(neighborQ, neighborR, neighborLocal) != ownClass)
            {
                mask |= 1 << edge;
            }
        }

        return mask;
    }

    private int AreaClassAt(int q, int r, int localTriangle)
    {
        if (!TryGetCellIndex(new HexCoord(q, r), out var cellIndex))
        {
            // Off-map neighbours end the shape, so the rim of the world
            // outlines like any other boundary.
            return AreaClassNone;
        }

        return AreaClassOf(
            _microtriangles[_cells[cellIndex].MicrotriangleOffset + localTriangle]);
    }

    private static int AreaClassOf(in PhysicalMicrotriangleProjection triangle) =>
        triangle.WaterKindCode switch
        {
            PhysicalWaterKindCodes.Ocean => AreaClassOcean,
            PhysicalWaterKindCodes.Lake => AreaClassLake,
            _ => triangle.FloodplainIntensity > 0 ? AreaClassFloodplain : AreaClassNone,
        };

    public static PhysicalGeographyProjection Empty { get; } =
        new(0, 0, [], [], [], [], [], [], []);

    public int Radius { get; }

    public int GenerationEpochCount { get; }

    public bool HasData => Radius > 0 && _cells.Length > 0;

    public IReadOnlyList<string> MaterialPalette => _materialPalette;

    public IReadOnlyList<string> SurfacePalette => _surfacePalette;

    public IReadOnlyList<string> FeaturePalette => _featurePalette;

    public IReadOnlyList<string> ScalarFieldNames => _scalarFieldNames;

    public IReadOnlyList<PhysicalGeographyCellProjection> Cells => _cells;

    public static PhysicalGeographyProjection FromBlueprint(
        DomainPhysicalGeographyBlueprint? blueprint)
    {
        if (blueprint is null || blueprint.Radius == 0)
        {
            return Empty;
        }

        blueprint.Validate();
        return new PhysicalGeographyProjection(
            blueprint.Radius,
            blueprint.GenerationEpochCount,
            blueprint.MaterialPalette.ToArray(),
            blueprint.SurfacePalette.ToArray(),
            blueprint.FeaturePalette.ToArray(),
            blueprint.ScalarFields.Select(static field => field.StableName).ToArray(),
            blueprint.ScalarFields.Select(static field => field.Values.ToArray()).ToArray(),
            CreateCells(blueprint.Cells),
            CreateMicrotriangles(blueprint.Microtriangles));
    }

    private static PhysicalGeographyCellProjection[] CreateCells(
        IReadOnlyList<DomainPhysicalGeographyCell> sourceCells)
    {
        var cells = new PhysicalGeographyCellProjection[sourceCells.Count];
        for (var index = 0; index < cells.Length; index++)
        {
            var source = sourceCells[index];
            cells[index] = new PhysicalGeographyCellProjection(
                index,
                new HexCoord(source.Q, source.R),
                source.BaseTerrain,
                source.SuperregionId,
                source.ContinentId,
                source.MicrotriangleOffset,
                source.DominantMaterialIndex,
                source.PrimarySurfaceIndex,
                source.SignificantFeatureFlags,
                source.MeanElevation,
                source.MinElevation,
                source.MaxElevation,
                source.Relief,
                source.Roughness,
                source.MeanTemperature,
                source.MeanMoisture,
                source.MeanGeothermal,
                source.LandCoverage,
                source.WaterCoverage,
                source.OceanCoverage,
                source.LakeCoverage,
                source.PermanentIceCoverage,
                source.RiverCoverage,
                source.FloodplainCoverage,
                source.DeltaCoverage,
                source.WideRiverCoverage,
                source.PeakCount,
                source.MeanIceThickness,
                source.MaxIceThickness,
                source.MeanFireIntensity,
                source.MaxFireIntensity,
                source.MeanAuroraIntensity,
                source.MaxAuroraIntensity,
                source.MeanPressureIntensity,
                source.MaxPressureIntensity,
                source.RiverEdgeMask,
                source.CoastEdgeMask,
                source.RidgeEdgeMask,
                source.CliffEdgeMask,
                source.PassEdgeMask,
                source.FractureEdgeMask);
        }

        return cells;
    }

    private static PhysicalMicrotriangleProjection[] CreateMicrotriangles(
        IReadOnlyList<DomainPhysicalMicrotriangle> sourceTriangles)
    {
        var triangles = new PhysicalMicrotriangleProjection[sourceTriangles.Count];
        for (var index = 0; index < triangles.Length; index++)
        {
            var source = sourceTriangles[index];
            triangles[index] = new PhysicalMicrotriangleProjection(
                source.MaterialIndex,
                source.BaseSurfaceIndex,
                source.SurfaceIndex,
                source.SignificantFeatureFlags,
                source.LandformCode,
                source.Elevation,
                source.Slope,
                source.Temperature,
                source.Moisture,
                source.Geothermal,
                source.LandmassId,
                source.LandmassClassCode,
                source.ContinentalGroupId,
                source.BathymetricZoneCode,
                source.WaterKindCode,
                source.WaterDepth,
                source.WaterBodyId,
                source.DrainageBasinId,
                source.FlowEdge,
                source.FlowAccumulation,
                source.RiverOrder,
                source.RiverSpineEdgeMask,
                0,
                source.RiverChannelStrength,
                source.RiverChannelOrder,
                source.FloodplainIntensity,
                source.DeltaIntensity,
                source.IceThickness,
                source.IceFlux,
                source.FractureConductance,
                source.HeatSourceDensity,
                source.FeatureFlags);
        }

        return triangles;
    }

    public bool TryGetCell(HexCoord coord, out PhysicalGeographyCellProjection cell)
    {
        if (!TryGetCellIndex(coord, out var index))
        {
            cell = default;
            return false;
        }

        cell = _cells[index];
        return true;
    }

    public ReadOnlySpan<PhysicalMicrotriangleProjection> MicrotrianglesFor(
        in PhysicalGeographyCellProjection cell) =>
        new(
            _microtriangles,
            cell.MicrotriangleOffset,
            PhysicalGeographyCodes.MicrotrianglesPerHex);

    public bool TryGetMicrotriangle(
        HexCoord coord,
        int triangleIndex,
        out PhysicalMicrotriangleProjection triangle)
    {
        if ((uint)triangleIndex >= PhysicalGeographyCodes.MicrotrianglesPerHex ||
            !TryGetCell(coord, out var cell))
        {
            triangle = default;
            return false;
        }

        triangle = _microtriangles[cell.MicrotriangleOffset + triangleIndex];
        return true;
    }

    public string MaterialName(int materialIndex) =>
        (uint)materialIndex < (uint)_materialPalette.Length
            ? _materialPalette[materialIndex]
            : $"material-{materialIndex}";

    public string SurfaceName(int surfaceIndex) =>
        (uint)surfaceIndex < (uint)_surfacePalette.Length
            ? _surfacePalette[surfaceIndex]
            : $"surface-{surfaceIndex}";

    public bool TryGetScalarValue(string stableName, int microtriangleIndex, out int value)
    {
        if (!_scalarFieldIndexes.TryGetValue(stableName, out var fieldIndex) ||
            (uint)microtriangleIndex >= (uint)_microtriangles.Length)
        {
            value = 0;
            return false;
        }

        value = _scalarFieldValues[fieldIndex][microtriangleIndex];
        return true;
    }

    public IEnumerable<string> SignificantFeatureNames(ulong flags)
    {
        for (var index = 0; index < _featurePalette.Length; index++)
        {
            if ((flags & (1UL << index)) != 0)
            {
                yield return _featurePalette[index];
            }
        }
    }

    private bool TryGetCellIndex(HexCoord coord, out int index)
    {
        if (Radius == 0 ||
            coord.Q < -Radius || coord.Q > Radius ||
            coord.R < -Radius || coord.R > Radius ||
            coord.DistanceFromOrigin > Radius)
        {
            index = -1;
            return false;
        }

        index = _cellIndexes[SquareIndex(coord.Q, coord.R)];
        return index >= 0;
    }

    private int SquareIndex(int q, int r) =>
        checked(((r + Radius) * _diameter) + q + Radius);
}

public readonly record struct PhysicalGeographyCellProjection(
    int CellIndex,
    HexCoord Coord,
    string BaseTerrainStableName,
    int SuperregionId,
    int ContinentId,
    int MicrotriangleOffset,
    int DominantMaterialIndex,
    int PrimarySurfaceIndex,
    ulong SignificantFeatureFlags,
    int MeanElevation,
    int MinElevation,
    int MaxElevation,
    int Relief,
    int Roughness,
    int MeanTemperature,
    int MeanMoisture,
    int MeanGeothermal,
    int LandCoverage,
    int WaterCoverage,
    int OceanCoverage,
    int LakeCoverage,
    int PermanentIceCoverage,
    int RiverCoverage,
    int FloodplainCoverage,
    int DeltaCoverage,
    int WideRiverCoverage,
    int PeakCount,
    int MeanIceThickness,
    int MaxIceThickness,
    int MeanFireIntensity,
    int MaxFireIntensity,
    int MeanAuroraIntensity,
    int MaxAuroraIntensity,
    int MeanPressureIntensity,
    int MaxPressureIntensity,
    int RiverEdgeMask,
    int CoastEdgeMask,
    int RidgeEdgeMask,
    int CliffEdgeMask,
    int PassEdgeMask,
    int FractureEdgeMask);

public readonly record struct PhysicalMicrotriangleProjection(
    int MaterialIndex,
    int BaseSurfaceIndex,
    int SurfaceIndex,
    ulong SignificantFeatureFlags,
    int LandformCode,
    int Elevation,
    int Slope,
    int Temperature,
    int Moisture,
    int Geothermal,
    int LandmassId,
    int LandmassClassCode,
    int ContinentalGroupId,
    int BathymetricZoneCode,
    int WaterKindCode,
    int WaterDepth,
    int WaterBodyId,
    int DrainageBasinId,
    int FlowEdge,
    int FlowAccumulation,
    int RiverOrder,
    int RiverSpineEdgeMask,
    int AreaOutlineEdgeMask,
    int RiverChannelStrength,
    int RiverChannelOrder,
    int FloodplainIntensity,
    int DeltaIntensity,
    int IceThickness,
    int IceFlux,
    int FractureConductance,
    int HeatSourceDensity,
    int FeatureFlags);
