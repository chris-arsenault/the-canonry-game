namespace Engine.Kernel;

using Engine.Constants;

internal sealed class MapLayerSet
{
    private readonly MapUniqueEntityLayer _coverage;
    private readonly MapScalarEntityLayer _rawCultureInfluence;
    private readonly MapScalarEntityLayer _cultureInfluence;

    internal MapLayerSet(int entityCapacity, int cultureIdCapacity, int mapCellCapacity, int mapInfluenceCapacity)
    {
        _coverage = new MapUniqueEntityLayer(entityCapacity, mapCellCapacity);
        _rawCultureInfluence = new MapScalarEntityLayer(cultureIdCapacity, mapInfluenceCapacity);
        _cultureInfluence = new MapScalarEntityLayer(cultureIdCapacity, mapInfluenceCapacity);
    }

    internal MapUniqueEntityLayer Coverage => _coverage;

    internal MapScalarEntityLayer RawCultureInfluence => _rawCultureInfluence;

    internal MapScalarEntityLayer CultureInfluence => _cultureInfluence;

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        _coverage.WriteSnapshot(writer);
        _rawCultureInfluence.WriteSnapshot(writer);
        _cultureInfluence.WriteSnapshot(writer);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        _coverage.ReadSnapshot(reader);
        _rawCultureInfluence.ReadSnapshot(reader);
        _cultureInfluence.ReadSnapshot(reader);
    }
}

internal sealed class MapUniqueEntityLayer
{
    private readonly SpatialLayerIndex _values;

    internal MapUniqueEntityLayer(int entityCapacity, int cellCapacity)
    {
        _values = new SpatialLayerIndex((entityCapacity << 2) + PlaneCodes.Semantic, cellCapacity);
    }

    internal int ValueAt(int planeCode, int cellKey)
    {
        return EntityIdFromPlaneValue(_values.ValueAt(PlaneCellKey(planeCode, cellKey)));
    }

    internal void SetValue(int planeCode, int cellKey, int q, int r, int value) =>
        SetValue(planeCode, cellKey, q, r, z: 0, value);

    internal void SetValue(int planeCode, int cellKey, int q, int r, int z, int value)
    {
        _values.SetValue(PlaneCellKey(planeCode, cellKey), q, r, z, EntityPlaneValue(value, planeCode));
    }

    internal int ValueCellCount(int value, int planeCode)
    {
        return _values.ValueCellCount(EntityPlaneValue(value, planeCode));
    }

    internal SpatialLayerCellCursor CellsOfValue(int value, int planeCode)
    {
        return _values.CellsOfValue(EntityPlaneValue(value, planeCode));
    }

    internal bool MoveNextCell(ref SpatialLayerCellCursor cursor, out int q, out int r)
    {
        return _values.MoveNextCell(ref cursor, out q, out r);
    }

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        _values.WriteSnapshot(writer);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        _values.ReadSnapshot(reader);
    }

    private static int EntityPlaneValue(int entityId, int planeCode)
    {
        if (entityId == 0)
        {
            return 0;
        }

        global::System.ArgumentOutOfRangeException.ThrowIfNegative(entityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(planeCode);
        return (entityId << 2) | planeCode;
    }

    private static int EntityIdFromPlaneValue(int value) => value >> 2;

    private static int PlaneCellKey(int planeCode, int cellKey)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(planeCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellKey);
        unchecked
        {
            var hash = (uint)cellKey * 2654435761u;
            hash ^= (uint)planeCode * 2246822519u;
            var value = (int)(hash & 2147483647u);
            return value == 0 ? 1 : value;
        }
    }
}

internal sealed class MapScalarEntityLayer
{
    private readonly PrimitiveFloatTable _values;
    private readonly PrimitiveIntTable _rowKeysByEntityPlaneCell;
    private readonly SpatialLayerIndex _cellsByEntityPlane;
    private readonly int[] _cellKeysByRowKey;
    private readonly int _rowCapacity;
    private int _rowHighWatermark;

    internal MapScalarEntityLayer(int entityCapacity, int capacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityCapacity);
        _values = new PrimitiveFloatTable(capacity);
        _rowKeysByEntityPlaneCell = new PrimitiveIntTable(capacity);
        _cellsByEntityPlane = new SpatialLayerIndex((entityCapacity << 2) + PlaneCodes.Semantic, capacity);
        _cellKeysByRowKey = new int[capacity + 1];
        _rowCapacity = capacity;
    }

    internal int Count => _values.Count;

    internal float ValueAt(int entityId, int planeCode, int cellKey)
    {
        return _values.GetOrDefault(EntityPlaneKey(entityId, planeCode), cellKey);
    }

    internal void SetValue(int entityId, int planeCode, int cellKey, int q, int r, float value) =>
        SetValue(entityId, planeCode, cellKey, q, r, z: 0, value);

    internal void SetValue(int entityId, int planeCode, int cellKey, int q, int r, int z, float value)
    {
        var entityPlaneKey = EntityPlaneKey(entityId, planeCode);
        var rowKey = _rowKeysByEntityPlaneCell.GetOrDefault(entityPlaneKey, cellKey);
        if (value <= 0f)
        {
            if (rowKey > 0)
            {
                _values.Set(entityPlaneKey, cellKey, value);
                _cellsByEntityPlane.SetValue(rowKey, q, r, z, value: 0);
            }

            return;
        }

        _values.Set(entityPlaneKey, cellKey, value);
        if (rowKey == 0)
        {
            rowKey = AllocateRowKey();
            _rowKeysByEntityPlaneCell.Set(entityPlaneKey, cellKey, rowKey);
            _cellKeysByRowKey[rowKey] = cellKey;
        }

        _cellsByEntityPlane.SetValue(rowKey, q, r, z, entityPlaneKey);
    }

    internal int ValueCellCount(int entityId, int planeCode)
    {
        return _cellsByEntityPlane.ValueCellCount(EntityPlaneKey(entityId, planeCode));
    }

    internal SpatialLayerCellCursor CellsOfValue(int entityId, int planeCode)
    {
        return _cellsByEntityPlane.CellsOfValue(EntityPlaneKey(entityId, planeCode));
    }

    internal bool MoveNextCell(ref SpatialLayerCellCursor cursor, out int q, out int r, out float value) =>
        MoveNextCell(ref cursor, out q, out r, out _, out value);

    internal bool MoveNextCell(ref SpatialLayerCellCursor cursor, out int q, out int r, out int z, out float value)
    {
        if (!_cellsByEntityPlane.MoveNextCell(ref cursor, out var rowKey, out q, out r, out z))
        {
            value = 0f;
            return false;
        }

        value = _values.GetOrDefault(cursor.Value, _cellKeysByRowKey[rowKey]);
        return true;
    }

    internal void ForEach(global::System.Action<int, int, int, float> visitor)
    {
        _values.ForEach((entityPlaneKey, cellKey, value) =>
        {
            visitor(EntityIdFromKey(entityPlaneKey), PlaneCodeFromKey(entityPlaneKey), cellKey, value);
        });
    }

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        _values.WriteSnapshot(writer);
        _rowKeysByEntityPlaneCell.WriteSnapshot(writer);
        _cellsByEntityPlane.WriteSnapshot(writer);
        KernelSnapshotBinary.Write(writer, _cellKeysByRowKey);
        writer.Write(_rowHighWatermark);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        _values.ReadSnapshot(reader);
        _rowKeysByEntityPlaneCell.ReadSnapshot(reader);
        _cellsByEntityPlane.ReadSnapshot(reader);
        KernelSnapshotBinary.Read(reader, _cellKeysByRowKey);
        _rowHighWatermark = reader.ReadInt32();
    }

    private static int EntityPlaneKey(int entityId, int planeCode)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(planeCode);
        return (entityId << 2) | planeCode;
    }

    private static int EntityIdFromKey(int key) => key >> 2;

    private static int PlaneCodeFromKey(int key) => key & 3;

    private int AllocateRowKey()
    {
        if (_rowHighWatermark >= _rowCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        _rowHighWatermark++;
        return _rowHighWatermark;
    }
}
