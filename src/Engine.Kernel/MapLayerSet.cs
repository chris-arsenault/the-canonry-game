namespace Engine.Kernel;

using Engine.Constants;

internal sealed class MapLayerSet
{
    private readonly MapUniqueEntityLayer _coverage;
    private readonly MapScalarEntityLayer _influence;

    internal MapLayerSet(int entityCapacity, int mapCellCapacity, int mapInfluenceCapacity)
    {
        _coverage = new MapUniqueEntityLayer(entityCapacity, mapCellCapacity);
        _influence = new MapScalarEntityLayer(entityCapacity, mapInfluenceCapacity);
    }

    internal MapUniqueEntityLayer Coverage => _coverage;

    internal MapScalarEntityLayer Influence => _influence;

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        _coverage.WriteSnapshot(writer);
        _influence.WriteSnapshot(writer);
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        _coverage.ReadSnapshot(reader);
        _influence.ReadSnapshot(reader);
    }
}

internal sealed class MapUniqueEntityLayer
{
    private readonly SpatialLayerIndex _values;

    internal MapUniqueEntityLayer(int valueCapacity, int cellCapacity)
    {
        _values = new SpatialLayerIndex(valueCapacity, cellCapacity);
    }

    internal int ValueAt(int cellKey)
    {
        return _values.ValueAt(cellKey);
    }

    internal void SetValue(int cellKey, int q, int r, int value)
    {
        _values.SetValue(cellKey, q, r, value);
    }

    internal int ValueCellCount(int value)
    {
        return _values.ValueCellCount(value);
    }

    internal SpatialLayerCellCursor CellsOfValue(int value)
    {
        return _values.CellsOfValue(value);
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

    internal void SetValue(int entityId, int planeCode, int cellKey, int q, int r, float value)
    {
        var entityPlaneKey = EntityPlaneKey(entityId, planeCode);
        _values.Set(entityPlaneKey, cellKey, value);

        var rowKey = _rowKeysByEntityPlaneCell.GetOrDefault(entityPlaneKey, cellKey);
        if (value > 0f)
        {
            if (rowKey == 0)
            {
                rowKey = AllocateRowKey();
                _rowKeysByEntityPlaneCell.Set(entityPlaneKey, cellKey, rowKey);
                _cellKeysByRowKey[rowKey] = cellKey;
            }

            _cellsByEntityPlane.SetValue(rowKey, q, r, entityPlaneKey);
            return;
        }

        if (rowKey > 0)
        {
            _cellsByEntityPlane.SetValue(rowKey, q, r, value: 0);
        }
    }

    internal int ValueCellCount(int entityId, int planeCode)
    {
        return _cellsByEntityPlane.ValueCellCount(EntityPlaneKey(entityId, planeCode));
    }

    internal SpatialLayerCellCursor CellsOfValue(int entityId, int planeCode)
    {
        return _cellsByEntityPlane.CellsOfValue(EntityPlaneKey(entityId, planeCode));
    }

    internal bool MoveNextCell(ref SpatialLayerCellCursor cursor, out int q, out int r, out float value)
    {
        if (!_cellsByEntityPlane.MoveNextCell(ref cursor, out var rowKey, out q, out r))
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
