namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// Fixed primitive geography for every potential physical surface hex. The
/// blueprint is installed once; materialization only exposes a precomputed
/// coordinate to the live world.
/// </summary>
internal sealed class KernelPhysicalGeographyData
{
    private readonly int _radiusCapacity;
    private readonly int _diameter;
    private readonly bool[] _installedCells;
    private readonly int[] _baseTerrainIds;
    private readonly int[] _superregionIds;
    private readonly int[] _continentIds;
    private readonly int[] _meanElevations;
    private readonly int[] _maxElevations;
    private readonly int[] _roughness;
    private readonly int[] _waterCoverage;
    private readonly int[] _riverEdgeMasks;
    private readonly int[] _coastEdgeMasks;
    private readonly int[] _microtriangleMaterialIds;
    private readonly int[] _microtriangleElevations;
    private readonly int[] _microtriangleHydrologyFlags;
    private int _installedRadius;

    public KernelPhysicalGeographyData(int radiusCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(radiusCapacity);
        _radiusCapacity = radiusCapacity;
        _diameter = checked((radiusCapacity * 2) + 1);
        var cellCapacity = checked(_diameter * _diameter);
        var microtriangleCapacity = checked(cellCapacity * PhysicalGeographyCodes.MicrotrianglesPerHex);
        _installedCells = new bool[cellCapacity];
        _baseTerrainIds = new int[cellCapacity];
        _superregionIds = new int[cellCapacity];
        _continentIds = new int[cellCapacity];
        _meanElevations = new int[cellCapacity];
        _maxElevations = new int[cellCapacity];
        _roughness = new int[cellCapacity];
        _waterCoverage = new int[cellCapacity];
        _riverEdgeMasks = new int[cellCapacity];
        _coastEdgeMasks = new int[cellCapacity];
        _microtriangleMaterialIds = new int[microtriangleCapacity];
        _microtriangleElevations = new int[microtriangleCapacity];
        _microtriangleHydrologyFlags = new int[microtriangleCapacity];
    }

    internal int Radius => _installedRadius;

    internal int RadiusCapacity => _radiusCapacity;

    internal void BeginInstall(int radius)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(radius);
        if (radius > _radiusCapacity || _installedRadius != 0)
        {
            throw new global::System.InvalidOperationException();
        }

        _installedRadius = radius;
    }

    internal void InstallCell(
        int q,
        int r,
        int baseTerrainId,
        int superregionId,
        int continentId,
        int meanElevation,
        int maxElevation,
        int roughness,
        int waterCoverage,
        int riverEdgeMask,
        int coastEdgeMask,
        global::System.ReadOnlySpan<int> materialIds,
        global::System.ReadOnlySpan<int> elevations,
        global::System.ReadOnlySpan<int> hydrologyFlags)
    {
        if (_installedRadius <= 0 ||
            HexDistanceFromOrigin(q, r) > _installedRadius ||
            baseTerrainId <= 0 ||
            superregionId <= 0 ||
            continentId <= 0 ||
            materialIds.Length != PhysicalGeographyCodes.MicrotrianglesPerHex ||
            elevations.Length != PhysicalGeographyCodes.MicrotrianglesPerHex ||
            hydrologyFlags.Length != PhysicalGeographyCodes.MicrotrianglesPerHex)
        {
            throw new global::System.InvalidOperationException();
        }

        var cellIndex = CellIndex(q, r);
        if (_installedCells[cellIndex])
        {
            throw new global::System.InvalidOperationException();
        }

        _installedCells[cellIndex] = true;
        _baseTerrainIds[cellIndex] = baseTerrainId;
        _superregionIds[cellIndex] = superregionId;
        _continentIds[cellIndex] = continentId;
        _meanElevations[cellIndex] = meanElevation;
        _maxElevations[cellIndex] = maxElevation;
        _roughness[cellIndex] = roughness;
        _waterCoverage[cellIndex] = waterCoverage;
        _riverEdgeMasks[cellIndex] = riverEdgeMask;
        _coastEdgeMasks[cellIndex] = coastEdgeMask;

        var microtriangleOffset = checked(cellIndex * PhysicalGeographyCodes.MicrotrianglesPerHex);
        materialIds.CopyTo(new global::System.Span<int>(_microtriangleMaterialIds, microtriangleOffset, materialIds.Length));
        elevations.CopyTo(new global::System.Span<int>(_microtriangleElevations, microtriangleOffset, elevations.Length));
        hydrologyFlags.CopyTo(new global::System.Span<int>(_microtriangleHydrologyFlags, microtriangleOffset, hydrologyFlags.Length));
    }

    internal bool TryGetCell(
        int q,
        int r,
        out int baseTerrainId,
        out int superregionId,
        out int continentId)
    {
        if (!CoordinateWithinCapacity(q, r))
        {
            baseTerrainId = 0;
            superregionId = 0;
            continentId = 0;
            return false;
        }

        var cellIndex = CellIndex(q, r);
        if (!_installedCells[cellIndex])
        {
            baseTerrainId = 0;
            superregionId = 0;
            continentId = 0;
            return false;
        }

        baseTerrainId = _baseTerrainIds[cellIndex];
        superregionId = _superregionIds[cellIndex];
        continentId = _continentIds[cellIndex];
        return true;
    }

    internal int MeanElevation(int q, int r) => CellValue(_meanElevations, q, r);

    internal int MaxElevation(int q, int r) => CellValue(_maxElevations, q, r);

    internal int Roughness(int q, int r) => CellValue(_roughness, q, r);

    internal int WaterCoverage(int q, int r) => CellValue(_waterCoverage, q, r);

    internal int RiverEdgeMask(int q, int r) => CellValue(_riverEdgeMasks, q, r);

    internal int CoastEdgeMask(int q, int r) => CellValue(_coastEdgeMasks, q, r);

    internal int MicrotriangleMaterialId(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleMaterialIds, q, r, triangleIndex);

    internal int MicrotriangleElevation(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleElevations, q, r, triangleIndex);

    internal int MicrotriangleHydrologyFlags(int q, int r, int triangleIndex) =>
        MicrotriangleValue(_microtriangleHydrologyFlags, q, r, triangleIndex);

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer) =>
        KernelSnapshotBinary.WriteObject(writer, this);

    internal void ReadSnapshot(global::System.IO.BinaryReader reader) =>
        KernelSnapshotBinary.ReadObject(reader, this);

    private int CellValue(int[] values, int q, int r)
    {
        var cellIndex = RequiredCellIndex(q, r);
        return values[cellIndex];
    }

    private int MicrotriangleValue(int[] values, int q, int r, int triangleIndex)
    {
        if ((uint)triangleIndex >= PhysicalGeographyCodes.MicrotrianglesPerHex)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(triangleIndex));
        }

        var cellIndex = RequiredCellIndex(q, r);
        return values[(cellIndex * PhysicalGeographyCodes.MicrotrianglesPerHex) + triangleIndex];
    }

    private int RequiredCellIndex(int q, int r)
    {
        if (!CoordinateWithinCapacity(q, r))
        {
            throw new global::System.InvalidOperationException();
        }

        var cellIndex = CellIndex(q, r);
        if (!_installedCells[cellIndex])
        {
            throw new global::System.InvalidOperationException();
        }

        return cellIndex;
    }

    private bool CoordinateWithinCapacity(int q, int r) =>
        q >= -_radiusCapacity && q <= _radiusCapacity &&
        r >= -_radiusCapacity && r <= _radiusCapacity;

    private int CellIndex(int q, int r) =>
        checked(((q + _radiusCapacity) * _diameter) + r + _radiusCapacity);

    private static int HexDistanceFromOrigin(int q, int r) =>
        (global::System.Math.Abs(q) + global::System.Math.Abs(r) + global::System.Math.Abs(q + r)) / 2;
}

internal static class PhysicalMicrotriangleTopology
{
    private static readonly int[] NeighborDQ = [1, 1, 0, -1, -1, 0];
    private static readonly int[] NeighborDR = [0, -1, -1, 0, 1, 1];

    internal static int TriangleIndex(int sector, int localTriangle)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(sector);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(
            sector,
            PhysicalGeographyCodes.HexSectorCount);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(localTriangle);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(
            localTriangle,
            PhysicalGeographyCodes.MicrotrianglesPerSector);

        return (sector * PhysicalGeographyCodes.MicrotrianglesPerSector) + localTriangle;
    }

    internal static void ResolveNeighbor(
        int q,
        int r,
        int triangleIndex,
        int edge,
        out int neighborQ,
        out int neighborR,
        out int neighborTriangleIndex)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(triangleIndex);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(
            triangleIndex,
            PhysicalGeographyCodes.MicrotrianglesPerHex);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(edge);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(edge, 3);

        var sector = triangleIndex / PhysicalGeographyCodes.MicrotrianglesPerSector;
        var local = triangleIndex % PhysicalGeographyCodes.MicrotrianglesPerSector;
        neighborQ = q;
        neighborR = r;
        if (local == 3)
        {
            neighborTriangleIndex = TriangleIndex(sector, edge);
            return;
        }

        if (edge == 0)
        {
            neighborTriangleIndex = TriangleIndex(sector, 3);
            return;
        }

        if (local == 0)
        {
            var adjacentSector = edge == 1 ? (sector + 5) % 6 : (sector + 1) % 6;
            neighborTriangleIndex = TriangleIndex(adjacentSector, 0);
            return;
        }

        ResolveOuterNeighbor(
            sector,
            local,
            edge,
            ref neighborQ,
            ref neighborR,
            out neighborTriangleIndex);
    }

    private static void ResolveOuterNeighbor(
        int sector,
        int local,
        int edge,
        ref int neighborQ,
        ref int neighborR,
        out int neighborTriangleIndex)
    {
        var firstHalf = local == 1;
        if (edge == 1)
        {
            var adjacentSector = firstHalf ? (sector + 5) % 6 : (sector + 1) % 6;
            neighborTriangleIndex = TriangleIndex(adjacentSector, firstHalf ? 2 : 1);
            return;
        }

        neighborQ += NeighborDQ[sector];
        neighborR += NeighborDR[sector];
        neighborTriangleIndex = TriangleIndex((sector + 3) % 6, firstHalf ? 2 : 1);
    }
}
