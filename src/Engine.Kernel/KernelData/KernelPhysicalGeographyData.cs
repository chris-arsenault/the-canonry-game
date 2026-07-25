namespace Engine.Kernel;

/// <summary>
/// Fixed primitive reveal data for every potential physical surface hex.
/// Beside terrain and subdivision identity, each hex carries its immutable
/// gameplay projection: a directional crossing word, a band/flag field word,
/// and an optional initial transient cover terrain applied at reveal.
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
    private readonly ushort[] _crossingWords;
    private readonly ushort[] _fieldWords;
    private readonly int[] _initialCoverTerrainIds;
    private readonly byte[] _facts;
    private int _installedRadius;

    public KernelPhysicalGeographyData(int radiusCapacity)
    {
        NumericGuard.ThrowIfNegative(radiusCapacity);
        _radiusCapacity = radiusCapacity;
        _diameter = checked((radiusCapacity * 2) + 1);
        var cellCapacity = checked(_diameter * _diameter);
        _installedCells = new bool[cellCapacity];
        _baseTerrainIds = new int[cellCapacity];
        _superregionIds = new int[cellCapacity];
        _continentIds = new int[cellCapacity];
        _crossingWords = new ushort[cellCapacity];
        _fieldWords = new ushort[cellCapacity];
        _initialCoverTerrainIds = new int[cellCapacity];
        _facts = new byte[checked(cellCapacity * PhysicalHexFactCodes.Count)];
    }

    internal int Radius => _installedRadius;

    internal int RadiusCapacity => _radiusCapacity;

    internal void BeginInstall(int radius)
    {
        NumericGuard.ThrowIfNegative(radius);
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
        int crossingWord,
        int fieldWord,
        int initialCoverTerrainId,
        global::System.ReadOnlySpan<byte> facts)
    {
        ValidateInstall(q, r, baseTerrainId, superregionId, continentId);
        if ((crossingWord & ~PhysicalCrossingCodes.WordMask) != 0 ||
            (fieldWord & ~PhysicalHexFieldCodes.WordMask) != 0 ||
            initialCoverTerrainId < 0 ||
            facts.Length != PhysicalHexFactCodes.Count)
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
        _crossingWords[cellIndex] = (ushort)crossingWord;
        _fieldWords[cellIndex] = (ushort)fieldWord;
        _initialCoverTerrainIds[cellIndex] = initialCoverTerrainId;
        var factBase = cellIndex * PhysicalHexFactCodes.Count;
        for (var fact = 0; fact < PhysicalHexFactCodes.Count; fact++)
        {
            _facts[factBase + fact] = facts[fact];
        }
    }

    private void ValidateInstall(int q, int r, int baseTerrainId, int superregionId, int continentId)
    {
        if (_installedRadius <= 0 ||
            HexDistanceFromOrigin(q, r) > _installedRadius ||
            baseTerrainId <= 0 ||
            superregionId <= 0 ||
            continentId <= 0)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    internal bool TryGetCell(
        int q,
        int r,
        out int baseTerrainId,
        out int superregionId,
        out int continentId)
    {
        if (!CellInstalled(q, r, out var cellIndex))
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

    /// <summary>
    /// The immutable directional crossing word for a hex, or zero when the
    /// hex is outside the installed field. Zero also means every edge is
    /// open, so absent geography reads as unremarkable rather than special.
    /// </summary>
    internal int CrossingWord(int q, int r) =>
        CellInstalled(q, r, out var cellIndex) ? _crossingWords[cellIndex] : 0;

    /// <summary>
    /// The immutable band/flag field word for a hex, or zero outside the
    /// installed field.
    /// </summary>
    internal int FieldWord(int q, int r) =>
        CellInstalled(q, r, out var cellIndex) ? _fieldWords[cellIndex] : 0;

    /// <summary>
    /// The transient cover terrain a hex starts under when materialized
    /// (sea ice over frozen water), or zero for none.
    /// </summary>
    internal int InitialCoverTerrainId(int q, int r) =>
        CellInstalled(q, r, out var cellIndex) ? _initialCoverTerrainIds[cellIndex] : 0;

    /// <summary>
    /// A quantized geography fact (PhysicalHexFactCodes) for a hex, or zero
    /// outside the installed field - absent geography reads as cold, dry,
    /// flat, and unmagical rather than special.
    /// </summary>
    internal int Fact(int q, int r, int factCode) =>
        CellInstalled(q, r, out var cellIndex)
            ? _facts[(cellIndex * PhysicalHexFactCodes.Count) + factCode]
            : 0;

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer) =>
        KernelSnapshotBinary.WriteObject(writer, this);

    internal void ReadSnapshot(global::System.IO.BinaryReader reader) =>
        KernelSnapshotBinary.ReadObject(reader, this);

    private bool CellInstalled(int q, int r, out int cellIndex)
    {
        if (!CoordinateWithinCapacity(q, r))
        {
            cellIndex = 0;
            return false;
        }

        cellIndex = CellIndex(q, r);
        return _installedCells[cellIndex];
    }

    private bool CoordinateWithinCapacity(int q, int r) =>
        q >= -_radiusCapacity && q <= _radiusCapacity &&
        r >= -_radiusCapacity && r <= _radiusCapacity;

    private int CellIndex(int q, int r) =>
        checked(((q + _radiusCapacity) * _diameter) + r + _radiusCapacity);

    private static int HexDistanceFromOrigin(int q, int r) =>
        (global::System.Math.Abs(q) + global::System.Math.Abs(r) + global::System.Math.Abs(q + r)) / 2;
}
