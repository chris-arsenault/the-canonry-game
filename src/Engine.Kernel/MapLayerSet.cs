namespace Engine.Kernel;

using Engine.Constants;

internal sealed class MapLayerSet
{
    private readonly MapUniqueEntityLayer _coverage;
    private readonly MapFixedScalarEntityLayer _rawCultureInfluence;
    private readonly long[] _totalRawCultureInfluence;

    internal MapLayerSet(
        MapCellRegistry cells,
        int entityCapacity,
        int cultureIdCapacity,
        int mapInfluenceCapacity)
    {
        _coverage = new MapUniqueEntityLayer(cells, entityCapacity);
        _rawCultureInfluence = new MapFixedScalarEntityLayer(
            cells,
            cultureIdCapacity,
            mapInfluenceCapacity);
        _totalRawCultureInfluence = new long[cells.Capacity + 1];
    }

    internal MapUniqueEntityLayer Coverage => _coverage;

    internal MapFixedScalarEntityLayer RawCultureInfluence => _rawCultureInfluence;

    internal long TotalRawCultureInfluenceAt(int cellId) => _totalRawCultureInfluence[cellId];

    internal void AddTotalRawCultureInfluence(int cellId, int deltaUnits)
    {
        _totalRawCultureInfluence[cellId] = checked(_totalRawCultureInfluence[cellId] + deltaUnits);
    }

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        _coverage.WriteSnapshot(writer);
        _rawCultureInfluence.WriteSnapshot(writer);
        var count = 0;
        for (var cellId = 1; cellId < _totalRawCultureInfluence.Length; cellId++)
        {
            if (_totalRawCultureInfluence[cellId] != 0)
            {
                count++;
            }
        }

        writer.Write(count);
        for (var cellId = 1; cellId < _totalRawCultureInfluence.Length; cellId++)
        {
            if (_totalRawCultureInfluence[cellId] == 0)
            {
                continue;
            }

            writer.Write(cellId);
            writer.Write(_totalRawCultureInfluence[cellId]);
        }
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        _coverage.ReadSnapshot(reader);
        _rawCultureInfluence.ReadSnapshot(reader);
        global::System.Array.Clear(_totalRawCultureInfluence);
        var count = reader.ReadInt32();
        for (var index = 0; index < count; index++)
        {
            _totalRawCultureInfluence[reader.ReadInt32()] = reader.ReadInt64();
        }
    }
}

internal sealed class MapUniqueEntityLayer
{
    private readonly MapCellRegistry _cells;
    private readonly SpatialLayerIndex _values;

    internal MapUniqueEntityLayer(MapCellRegistry cells, int entityCapacity)
    {
        _cells = cells;
        _values = new SpatialLayerIndex(
            checked((entityCapacity << 2) + PlaneCodes.Semantic),
            cells.Capacity);
    }

    internal int ValueAt(int cellId)
    {
        var value = _values.ValueAt(cellId);
        return value >> 2;
    }

    internal void SetValue(int cellId, int value)
    {
        var planeCode = _cells.PlaneCode(cellId);
        _values.SetValue(cellId, value == 0 ? 0 : checked((value << 2) | planeCode));
    }

    internal int ValueCellCount(int value, int planeCode)
    {
        return _values.ValueCellCount(checked((value << 2) | planeCode));
    }

    internal SpatialLayerCellCursor CellsOfValue(int value, int planeCode)
    {
        return _values.CellsOfValue(checked((value << 2) | planeCode));
    }

    internal bool MoveNextCell(ref SpatialLayerCellCursor cursor, out int q, out int r)
    {
        if (!_values.MoveNextCell(ref cursor, out var cellId))
        {
            q = 0;
            r = 0;
            return false;
        }

        q = _cells.Q(cellId);
        r = _cells.R(cellId);
        return true;
    }

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer) => _values.WriteSnapshot(writer);

    internal void ReadSnapshot(global::System.IO.BinaryReader reader) => _values.ReadSnapshot(reader);
}

internal sealed class MapFixedScalarEntityLayer
{
    private readonly MapCellRegistry _cells;
    private readonly int[] _rowByCultureCell;
    private readonly int[] _cultureIds;
    private readonly int[] _cellIds;
    private readonly int[] _values;
    private readonly int[] _nextByCulturePlane;
    private readonly int[] _previousByCulturePlane;
    private readonly int[] _firstByCulturePlane;
    private readonly int[] _lastByCulturePlane;
    private readonly int[] _countsByCulturePlane;
    private readonly int[] _freeRows;
    private readonly int _cellStride;
    private readonly int _cultureCapacity;
    private readonly int _rowCapacity;
    private int _rowHighWatermark;
    private int _freeRowCount;
    private int _count;

    internal MapFixedScalarEntityLayer(
        MapCellRegistry cells,
        int cultureCapacity,
        int rowCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(cultureCapacity);
        NumericGuard.ThrowIfNegativeOrZero(rowCapacity);
        _cells = cells;
        _cellStride = cells.Capacity + 1;
        _cultureCapacity = cultureCapacity;
        _rowByCultureCell = new int[checked((cultureCapacity + 1) * _cellStride)];
        _cultureIds = new int[rowCapacity + 1];
        _cellIds = new int[rowCapacity + 1];
        _values = new int[rowCapacity + 1];
        _nextByCulturePlane = new int[rowCapacity + 1];
        _previousByCulturePlane = new int[rowCapacity + 1];
        var culturePlaneCapacity = checked((cultureCapacity << 2) + PlaneCodes.Semantic);
        _firstByCulturePlane = new int[culturePlaneCapacity + 1];
        _lastByCulturePlane = new int[culturePlaneCapacity + 1];
        _countsByCulturePlane = new int[culturePlaneCapacity + 1];
        _freeRows = new int[rowCapacity];
        _rowCapacity = rowCapacity;
    }

    internal int Count => _count;

    internal int ValueAt(int cultureId, int cellId)
    {
        var row = _rowByCultureCell[CultureCellIndex(cultureId, cellId)];
        return row == 0 ? 0 : _values[row];
    }

    internal void SetValue(int cultureId, int cellId, int value)
    {
        var directIndex = CultureCellIndex(cultureId, cellId);
        var row = _rowByCultureCell[directIndex];
        if (value <= 0)
        {
            if (row > 0)
            {
                ReleaseRow(row, directIndex);
            }

            return;
        }

        if (row == 0)
        {
            row = AllocateRow();
            _rowByCultureCell[directIndex] = row;
            _cultureIds[row] = cultureId;
            _cellIds[row] = cellId;
            LinkRow(row, CulturePlaneKey(cultureId, _cells.PlaneCode(cellId)));
            _count++;
        }

        _values[row] = value;
    }

    internal int ValueCellCount(int cultureId, int planeCode)
    {
        return _countsByCulturePlane[CulturePlaneKey(cultureId, planeCode)];
    }

    internal SpatialLayerCellCursor CellsOfValue(int cultureId, int planeCode)
    {
        var value = CulturePlaneKey(cultureId, planeCode);
        return new SpatialLayerCellCursor
        {
            Page = _firstByCulturePlane[value],
            Value = value,
        };
    }

    internal bool MoveNextCell(
        ref SpatialLayerCellCursor cursor,
        out int q,
        out int r,
        out int value) =>
        MoveNextCell(ref cursor, out q, out r, out _, out value);

    internal bool MoveNextCell(
        ref SpatialLayerCellCursor cursor,
        out int q,
        out int r,
        out int z,
        out int value)
    {
        var row = cursor.Page;
        if (row == 0)
        {
            q = 0;
            r = 0;
            z = 0;
            value = 0;
            return false;
        }

        cursor.Page = _nextByCulturePlane[row];
        var cellId = _cellIds[row];
        q = _cells.Q(cellId);
        r = _cells.R(cellId);
        z = _cells.Z(cellId);
        value = _values[row];
        return true;
    }

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(_count);
        for (var culturePlane = 1; culturePlane < _firstByCulturePlane.Length; culturePlane++)
        {
            for (var row = _firstByCulturePlane[culturePlane]; row > 0; row = _nextByCulturePlane[row])
            {
                writer.Write(_cultureIds[row]);
                writer.Write(_cellIds[row]);
                writer.Write(_values[row]);
            }
        }
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        Clear();
        var count = reader.ReadInt32();
        for (var index = 0; index < count; index++)
        {
            SetValue(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        }
    }

    private int AllocateRow()
    {
        if (_freeRowCount > 0)
        {
            return _freeRows[--_freeRowCount];
        }

        if (_rowHighWatermark >= _rowCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        return ++_rowHighWatermark;
    }

    private void ReleaseRow(int row, int directIndex)
    {
        var culturePlane = CulturePlaneKey(_cultureIds[row], _cells.PlaneCode(_cellIds[row]));
        var previous = _previousByCulturePlane[row];
        var next = _nextByCulturePlane[row];
        if (previous > 0)
        {
            _nextByCulturePlane[previous] = next;
        }
        else
        {
            _firstByCulturePlane[culturePlane] = next;
        }

        if (next > 0)
        {
            _previousByCulturePlane[next] = previous;
        }
        else
        {
            _lastByCulturePlane[culturePlane] = previous;
        }

        _countsByCulturePlane[culturePlane]--;
        _rowByCultureCell[directIndex] = 0;
        _cultureIds[row] = 0;
        _cellIds[row] = 0;
        _values[row] = 0;
        _nextByCulturePlane[row] = 0;
        _previousByCulturePlane[row] = 0;
        _freeRows[_freeRowCount++] = row;
        _count--;
    }

    private void LinkRow(int row, int culturePlane)
    {
        var previous = _lastByCulturePlane[culturePlane];
        _previousByCulturePlane[row] = previous;
        if (previous > 0)
        {
            _nextByCulturePlane[previous] = row;
        }
        else
        {
            _firstByCulturePlane[culturePlane] = row;
        }

        _lastByCulturePlane[culturePlane] = row;
        _countsByCulturePlane[culturePlane]++;
    }

    private int CultureCellIndex(int cultureId, int cellId)
    {
        if ((uint)cultureId == 0u || cultureId > _cultureCapacity)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(cultureId));
        }

        if ((uint)cellId == 0u || cellId >= _cellStride)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(cellId));
        }

        return checked((cultureId * _cellStride) + cellId);
    }

    private static int CulturePlaneKey(int cultureId, int planeCode) =>
        checked((cultureId << 2) | planeCode);

    private void Clear()
    {
        global::System.Array.Clear(_rowByCultureCell);
        global::System.Array.Clear(_cultureIds);
        global::System.Array.Clear(_cellIds);
        global::System.Array.Clear(_values);
        global::System.Array.Clear(_nextByCulturePlane);
        global::System.Array.Clear(_previousByCulturePlane);
        global::System.Array.Clear(_firstByCulturePlane);
        global::System.Array.Clear(_lastByCulturePlane);
        global::System.Array.Clear(_countsByCulturePlane);
        global::System.Array.Clear(_freeRows);
        _rowHighWatermark = 0;
        _freeRowCount = 0;
        _count = 0;
    }
}
