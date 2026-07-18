namespace Engine.Kernel;

/// <summary>
/// Fixed primitive reveal data for every potential physical surface hex.
/// Generation-time triangle fields remain outside the simulation kernel.
/// </summary>
internal sealed class KernelPhysicalGeographyData
{
    private readonly int _radiusCapacity;
    private readonly int _diameter;
    private readonly bool[] _installedCells;
    private readonly int[] _baseTerrainIds;
    private readonly int[] _superregionIds;
    private readonly int[] _continentIds;
    private int _installedRadius;

    public KernelPhysicalGeographyData(int radiusCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(radiusCapacity);
        _radiusCapacity = radiusCapacity;
        _diameter = checked((radiusCapacity * 2) + 1);
        var cellCapacity = checked(_diameter * _diameter);
        _installedCells = new bool[cellCapacity];
        _baseTerrainIds = new int[cellCapacity];
        _superregionIds = new int[cellCapacity];
        _continentIds = new int[cellCapacity];
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
        int continentId)
    {
        if (_installedRadius <= 0 ||
            HexDistanceFromOrigin(q, r) > _installedRadius ||
            baseTerrainId <= 0 ||
            superregionId <= 0 ||
            continentId <= 0)
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

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer) =>
        KernelSnapshotBinary.WriteObject(writer, this);

    internal void ReadSnapshot(global::System.IO.BinaryReader reader) =>
        KernelSnapshotBinary.ReadObject(reader, this);

    private bool CoordinateWithinCapacity(int q, int r) =>
        q >= -_radiusCapacity && q <= _radiusCapacity &&
        r >= -_radiusCapacity && r <= _radiusCapacity;

    private int CellIndex(int q, int r) =>
        checked(((q + _radiusCapacity) * _diameter) + r + _radiusCapacity);

    private static int HexDistanceFromOrigin(int q, int r) =>
        (global::System.Math.Abs(q) + global::System.Math.Abs(r) + global::System.Math.Abs(q + r)) / 2;
}
