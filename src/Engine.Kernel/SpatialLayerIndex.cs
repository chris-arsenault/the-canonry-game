namespace Engine.Kernel;

internal sealed partial class SpatialLayerIndex
{
    private const int PageSize = 16;

    private readonly PrimitiveIntTable _entryByCell;
    private readonly int[] _entryCellKeys;
    private readonly int[] _entryQ;
    private readonly int[] _entryR;
    private readonly int[] _entryZ;
    private readonly int[] _entryValues;
    private readonly int[] _entryPages;
    private readonly int[] _entryOffsets;
    private readonly int[] _valueCounts;
    private readonly int[] _firstPageByValue;
    private readonly int[] _lastPageByValue;
    private readonly int[] _pageValues;
    private readonly int[] _pageCounts;
    private readonly int[] _nextPage;
    private readonly int[] _previousPage;
    private readonly int[] _pageEntries;
    private int _entryCount;
    private int _pageCount;
    private int _freePage;

    internal SpatialLayerIndex(int valueCapacity, int cellCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valueCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCapacity);
        _entryByCell = new PrimitiveIntTable(cellCapacity);
        _entryCellKeys = new int[cellCapacity + 1];
        _entryQ = new int[cellCapacity + 1];
        _entryR = new int[cellCapacity + 1];
        _entryZ = new int[cellCapacity + 1];
        _entryValues = new int[cellCapacity + 1];
        _entryPages = new int[cellCapacity + 1];
        _entryOffsets = new int[cellCapacity + 1];
        _valueCounts = new int[valueCapacity + 1];
        _firstPageByValue = new int[valueCapacity + 1];
        _lastPageByValue = new int[valueCapacity + 1];
        _pageValues = new int[cellCapacity + 1];
        _pageCounts = new int[cellCapacity + 1];
        _nextPage = new int[cellCapacity + 1];
        _previousPage = new int[cellCapacity + 1];
        _pageEntries = new int[(cellCapacity + 1) * PageSize];
    }

    internal int ValueCapacity => _valueCounts.Length - 1;

    internal int CellCapacity => _entryCellKeys.Length - 1;

    internal int ValueAt(int cellKey)
    {
        ValidateCellKey(cellKey);
        return _entryByCell.TryGet(cellKey, 1, out var entry) && entry > 0
            ? _entryValues[entry]
            : 0;
    }

    internal void SetValue(int cellKey, int q, int r, int value) =>
        SetValue(cellKey, q, r, z: 0, value);

    internal void SetValue(int cellKey, int q, int r, int z, int value)
    {
        ValidateCellKey(cellKey);
        ValidateValue(value);
        if (!_entryByCell.TryGet(cellKey, 1, out var entry) || entry == 0)
        {
            if (value == 0)
            {
                return;
            }

            if (_entryCount + 1 >= _entryCellKeys.Length)
            {
                throw new global::System.InvalidOperationException();
            }

            _entryCount++;
            entry = _entryCount;
            _entryCellKeys[entry] = cellKey;
            _entryQ[entry] = q;
            _entryR[entry] = r;
            _entryZ[entry] = z;
            _entryByCell.Set(cellKey, 1, entry);
            _entryValues[entry] = value;
            AddEntryToValue(entry, value);
            return;
        }

        _entryQ[entry] = q;
        _entryR[entry] = r;
        _entryZ[entry] = z;
        var previousValue = _entryValues[entry];
        if (previousValue == value)
        {
            return;
        }

        if (previousValue > 0)
        {
            RemoveEntryFromValue(entry, previousValue);
        }

        _entryValues[entry] = value;
        if (value > 0)
        {
            AddEntryToValue(entry, value);
        }
    }

    internal int ValueCellCount(int value)
    {
        ValidateValue(value);
        return _valueCounts[value];
    }

    internal SpatialLayerCellCursor CellsOfValue(int value)
    {
        ValidateValue(value);
        return new SpatialLayerCellCursor
        {
            Page = _firstPageByValue[value],
            Offset = 0,
            Value = value,
        };
    }

    internal bool MoveNextCell(ref SpatialLayerCellCursor cursor, out int q, out int r)
    {
        return MoveNextCell(ref cursor, out _, out q, out r, out _);
    }

    internal bool MoveNextCell(ref SpatialLayerCellCursor cursor, out int cellKey, out int q, out int r)
    {
        return MoveNextCell(ref cursor, out cellKey, out q, out r, out _);
    }

    internal bool MoveNextCell(ref SpatialLayerCellCursor cursor, out int cellKey, out int q, out int r, out int z)
    {
        while (cursor.Page > 0)
        {
            if (cursor.Offset < _pageCounts[cursor.Page])
            {
                var entry = _pageEntries[PageEntryIndex(cursor.Page, cursor.Offset)];
                cursor.Offset++;
                cellKey = _entryCellKeys[entry];
                q = _entryQ[entry];
                r = _entryR[entry];
                z = _entryZ[entry];
                return true;
            }

            cursor.Page = _nextPage[cursor.Page];
            cursor.Offset = 0;
        }

        cellKey = 0;
        q = 0;
        r = 0;
        z = 0;
        return false;
    }

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(ValueCapacity);
        writer.Write(CellCapacity);
        var count = 0;
        for (var entry = 1; entry <= _entryCount; entry++)
        {
            if (_entryValues[entry] > 0)
            {
                count++;
            }
        }

        writer.Write(count);
        for (var entry = 1; entry <= _entryCount; entry++)
        {
            if (_entryValues[entry] == 0)
            {
                continue;
            }

            writer.Write(_entryCellKeys[entry]);
            writer.Write(_entryQ[entry]);
            writer.Write(_entryR[entry]);
            writer.Write(_entryZ[entry]);
            writer.Write(_entryValues[entry]);
        }
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        if (reader.ReadInt32() != ValueCapacity ||
            reader.ReadInt32() != CellCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        Clear();
        var count = reader.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            var cellKey = reader.ReadInt32();
            var q = reader.ReadInt32();
            var r = reader.ReadInt32();
            var z = reader.ReadInt32();
            var value = reader.ReadInt32();
            SetValue(cellKey, q, r, z, value);
        }
    }

    private void Clear()
    {
        _entryByCell.Clear();
        global::System.Array.Clear(_entryCellKeys);
        global::System.Array.Clear(_entryQ);
        global::System.Array.Clear(_entryR);
        global::System.Array.Clear(_entryZ);
        global::System.Array.Clear(_entryValues);
        global::System.Array.Clear(_entryPages);
        global::System.Array.Clear(_entryOffsets);
        global::System.Array.Clear(_valueCounts);
        global::System.Array.Clear(_firstPageByValue);
        global::System.Array.Clear(_lastPageByValue);
        global::System.Array.Clear(_pageValues);
        global::System.Array.Clear(_pageCounts);
        global::System.Array.Clear(_nextPage);
        global::System.Array.Clear(_previousPage);
        global::System.Array.Clear(_pageEntries);
        _entryCount = 0;
        _pageCount = 0;
        _freePage = 0;
    }

    private void AddEntryToValue(int entry, int value)
    {
        var page = _lastPageByValue[value];
        if (page == 0 || _pageCounts[page] == PageSize)
        {
            page = AllocatePage(value);
            LinkPage(value, page);
        }

        var offset = _pageCounts[page];
        _pageEntries[PageEntryIndex(page, offset)] = entry;
        _pageCounts[page]++;
        _entryPages[entry] = page;
        _entryOffsets[entry] = offset;
        _valueCounts[value]++;
    }

    private void RemoveEntryFromValue(int entry, int value)
    {
        var page = _entryPages[entry];
        var offset = _entryOffsets[entry];
        var lastPage = _lastPageByValue[value];
        var lastOffset = _pageCounts[lastPage] - 1;
        var lastEntry = _pageEntries[PageEntryIndex(lastPage, lastOffset)];
        if (lastEntry != entry)
        {
            _pageEntries[PageEntryIndex(page, offset)] = lastEntry;
            _entryPages[lastEntry] = page;
            _entryOffsets[lastEntry] = offset;
        }

        _pageEntries[PageEntryIndex(lastPage, lastOffset)] = 0;
        _pageCounts[lastPage]--;
        _valueCounts[value]--;
        _entryPages[entry] = 0;
        _entryOffsets[entry] = 0;
        if (_pageCounts[lastPage] == 0)
        {
            ReleasePage(value, lastPage);
        }
    }

    private int AllocatePage(int value)
    {
        int page;
        if (_freePage > 0)
        {
            page = _freePage;
            _freePage = _nextPage[page];
        }
        else
        {
            if (_pageCount + 1 >= _pageCounts.Length)
            {
                throw new global::System.InvalidOperationException();
            }

            _pageCount++;
            page = _pageCount;
        }

        _pageValues[page] = value;
        _pageCounts[page] = 0;
        _nextPage[page] = 0;
        _previousPage[page] = 0;
        return page;
    }

    private void LinkPage(int value, int page)
    {
        var previous = _lastPageByValue[value];
        if (previous == 0)
        {
            _firstPageByValue[value] = page;
        }
        else
        {
            _nextPage[previous] = page;
            _previousPage[page] = previous;
        }

        _lastPageByValue[value] = page;
    }

    private void ReleasePage(int value, int page)
    {
        var previous = _previousPage[page];
        var next = _nextPage[page];
        if (previous == 0)
        {
            _firstPageByValue[value] = next;
        }
        else
        {
            _nextPage[previous] = next;
        }

        if (next == 0)
        {
            _lastPageByValue[value] = previous;
        }
        else
        {
            _previousPage[next] = previous;
        }

        _pageValues[page] = 0;
        _pageCounts[page] = 0;
        _previousPage[page] = 0;
        _nextPage[page] = _freePage;
        _freePage = page;
    }

    private static int PageEntryIndex(int page, int offset) => (page * PageSize) + offset;

    private void ValidateValue(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(value);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, _valueCounts.Length);
    }

    private static void ValidateCellKey(int cellKey)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellKey);
    }
}
