namespace Engine.Kernel;

internal sealed class MapLayerSet
{
    private readonly MapUniqueEntityLayer _coverage;
    private readonly MapScalarEntityLayer _influence;

    internal MapLayerSet(int entityCapacity, int mapCellCapacity, int mapInfluenceCapacity)
    {
        _coverage = new MapUniqueEntityLayer(entityCapacity, mapCellCapacity);
        _influence = new MapScalarEntityLayer(mapInfluenceCapacity);
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

    internal MapScalarEntityLayer(int capacity)
    {
        _values = new PrimitiveFloatTable(capacity);
    }

    internal int Count => _values.Count;

    internal float ValueAt(int entityId, int cellKey)
    {
        return _values.GetOrDefault(entityId, cellKey);
    }

    internal void SetValue(int entityId, int cellKey, float value)
    {
        _values.Set(entityId, cellKey, value);
    }

    internal void ForEach(global::System.Action<int, int, float> visitor)
    {
        _values.ForEach(visitor);
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
