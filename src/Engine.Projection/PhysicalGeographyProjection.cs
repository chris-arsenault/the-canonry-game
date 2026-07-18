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
    private readonly PhysicalGeographyCellProjection[] _cells;
    private readonly PhysicalMicrotriangleProjection[] _microtriangles;
    private readonly int[] _cellIndexes;
    private readonly int _diameter;

    private PhysicalGeographyProjection(
        int radius,
        string[] materialPalette,
        PhysicalGeographyCellProjection[] cells,
        PhysicalMicrotriangleProjection[] microtriangles)
    {
        Radius = radius;
        _materialPalette = materialPalette;
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
    }

    public static PhysicalGeographyProjection Empty { get; } =
        new(0, [], [], []);

    public int Radius { get; }

    public bool HasData => Radius > 0 && _cells.Length > 0;

    public IReadOnlyList<string> MaterialPalette => _materialPalette;

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
            blueprint.MaterialPalette.ToArray(),
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
                source.PeakCount,
                source.RiverEdgeMask,
                source.CoastEdgeMask,
                source.RidgeEdgeMask,
                source.CliffEdgeMask,
                source.PassEdgeMask);
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
                source.LandformCode,
                source.Elevation,
                source.Temperature,
                source.Moisture,
                source.Geothermal,
                source.WaterKindCode,
                source.WaterDepth,
                source.WaterBodyId,
                source.DrainageBasinId,
                source.FlowEdge,
                source.FlowAccumulation,
                source.RiverOrder,
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
    int PeakCount,
    int RiverEdgeMask,
    int CoastEdgeMask,
    int RidgeEdgeMask,
    int CliffEdgeMask,
    int PassEdgeMask);

public readonly record struct PhysicalMicrotriangleProjection(
    int MaterialIndex,
    int LandformCode,
    int Elevation,
    int Temperature,
    int Moisture,
    int Geothermal,
    int WaterKindCode,
    int WaterDepth,
    int WaterBodyId,
    int DrainageBasinId,
    int FlowEdge,
    int FlowAccumulation,
    int RiverOrder,
    int FeatureFlags);
