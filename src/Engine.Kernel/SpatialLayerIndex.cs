namespace Engine.Kernel;

internal sealed partial class SpatialLayerIndex
{
    private const int PageSize = 16;

    private readonly int[] _valuesByCell;
    private readonly int[] _cellPages;
    private readonly int[] _cellOffsets;
    private readonly int[] _valueCounts;
    private readonly int[] _firstPageByValue;
    private readonly int[] _lastPageByValue;
    private readonly int[] _pageValues;
    private readonly int[] _pageCounts;
    private readonly int[] _nextPage;
    private readonly int[] _previousPage;
    private readonly int[] _pageCells;
    private int _pageHighWatermark;
    private int _freePage;

    internal SpatialLayerIndex(int valueCapacity, int cellCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(valueCapacity);
        NumericGuard.ThrowIfNegativeOrZero(cellCapacity);
        _valuesByCell = new int[cellCapacity + 1];
        _cellPages = new int[cellCapacity + 1];
        _cellOffsets = new int[cellCapacity + 1];
        _valueCounts = new int[valueCapacity + 1];
        _firstPageByValue = new int[valueCapacity + 1];
        _lastPageByValue = new int[valueCapacity + 1];
        _pageValues = new int[cellCapacity + 1];
        _pageCounts = new int[cellCapacity + 1];
        _nextPage = new int[cellCapacity + 1];
        _previousPage = new int[cellCapacity + 1];
        _pageCells = new int[checked((cellCapacity + 1) * PageSize)];
    }

    internal int ValueCapacity => _valueCounts.Length - 1;

    internal int CellCapacity => _valuesByCell.Length - 1;

    internal int ValueAt(int cellId)
    {
        ValidateCellId(cellId);
        return _valuesByCell[cellId];
    }

    internal void SetValue(int cellId, int value)
    {
        var previousValue = SetDirectValue(cellId, value);
        if (previousValue == value)
        {
            return;
        }

        ReconcileValueChange(cellId, previousValue, value);
    }

    internal int SetDirectValue(int cellId, int value)
    {
        ValidateCellId(cellId);
        ValidateValue(value);
        var previousValue = _valuesByCell[cellId];
        _valuesByCell[cellId] = value;
        return previousValue;
    }

    internal void ReconcileValueChange(
        int cellId,
        int previousValue,
        int value)
    {
        ValidateCellId(cellId);
        ValidateValue(previousValue);
        ValidateValue(value);
        if (previousValue == value)
        {
            return;
        }

        if (previousValue > 0)
        {
            RemoveCellFromValue(cellId, previousValue);
        }

        if (value > 0)
        {
            AddCellToValue(cellId, value);
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
            Value = value,
        };
    }

    internal bool MoveNextCell(ref SpatialLayerCellCursor cursor, out int cellId)
    {
        while (cursor.Page > 0)
        {
            if (cursor.Offset < _pageCounts[cursor.Page])
            {
                cellId = _pageCells[PageCellIndex(cursor.Page, cursor.Offset)];
                cursor.Offset++;
                return true;
            }

            cursor.Page = _nextPage[cursor.Page];
            cursor.Offset = 0;
        }

        cellId = 0;
        return false;
    }

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        var count = 0;
        for (var cellId = 1; cellId < _valuesByCell.Length; cellId++)
        {
            if (_valuesByCell[cellId] > 0)
            {
                count++;
            }
        }

        writer.Write(count);
        for (var cellId = 1; cellId < _valuesByCell.Length; cellId++)
        {
            if (_valuesByCell[cellId] <= 0)
            {
                continue;
            }

            writer.Write(cellId);
            writer.Write(_valuesByCell[cellId]);
        }
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        Clear();
        var count = reader.ReadInt32();
        for (var index = 0; index < count; index++)
        {
            SetValue(reader.ReadInt32(), reader.ReadInt32());
        }
    }

    private void Clear()
    {
        global::System.Array.Clear(_valuesByCell);
        global::System.Array.Clear(_cellPages);
        global::System.Array.Clear(_cellOffsets);
        global::System.Array.Clear(_valueCounts);
        global::System.Array.Clear(_firstPageByValue);
        global::System.Array.Clear(_lastPageByValue);
        global::System.Array.Clear(_pageValues);
        global::System.Array.Clear(_pageCounts);
        global::System.Array.Clear(_nextPage);
        global::System.Array.Clear(_previousPage);
        global::System.Array.Clear(_pageCells);
        _pageHighWatermark = 0;
        _freePage = 0;
    }

    private void AddCellToValue(int cellId, int value)
    {
        var page = _lastPageByValue[value];
        if (page == 0 || _pageCounts[page] == PageSize)
        {
            page = AllocatePage(value);
            LinkPage(value, page);
        }

        var offset = _pageCounts[page]++;
        _pageCells[PageCellIndex(page, offset)] = cellId;
        _cellPages[cellId] = page;
        _cellOffsets[cellId] = offset;
        _valueCounts[value]++;
    }

    private void RemoveCellFromValue(int cellId, int value)
    {
        var page = _cellPages[cellId];
        var offset = _cellOffsets[cellId];
        var lastPage = _lastPageByValue[value];
        var lastOffset = _pageCounts[lastPage] - 1;
        var movedCellId = _pageCells[PageCellIndex(lastPage, lastOffset)];
        if (movedCellId != cellId)
        {
            _pageCells[PageCellIndex(page, offset)] = movedCellId;
            _cellPages[movedCellId] = page;
            _cellOffsets[movedCellId] = offset;
        }

        _pageCells[PageCellIndex(lastPage, lastOffset)] = 0;
        _pageCounts[lastPage]--;
        _valueCounts[value]--;
        _cellPages[cellId] = 0;
        _cellOffsets[cellId] = 0;
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
            if (_pageHighWatermark >= CellCapacity)
            {
                throw new global::System.InvalidOperationException();
            }

            page = ++_pageHighWatermark;
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

    private static int PageCellIndex(int page, int offset) => (page * PageSize) + offset;

    private void ValidateValue(int value)
    {
        NumericGuard.ThrowIfNegative(value);
        NumericGuard.ThrowIfGreaterThanOrEqual(value, _valueCounts.Length);
    }

    private void ValidateCellId(int cellId)
    {
        if ((uint)cellId == 0u || cellId >= _valuesByCell.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(cellId));
        }
    }
}

internal readonly struct EntitySpatialMembershipIndex
{
    private readonly int[] _firstPhysicalEntityByCell;
    private readonly int[] _firstSemanticEntityByCell;
    private readonly int[] _nextPhysicalEntity;
    private readonly int[] _nextSemanticEntity;

    internal EntitySpatialMembershipIndex(int entityCapacity, int cellCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(entityCapacity);
        NumericGuard.ThrowIfNegativeOrZero(cellCapacity);
        _firstPhysicalEntityByCell = new int[cellCapacity + 1];
        _firstSemanticEntityByCell = new int[cellCapacity + 1];
        _nextPhysicalEntity = new int[entityCapacity + 1];
        _nextSemanticEntity = new int[entityCapacity + 1];
    }

    internal void AttachPhysical(int entityId, int cellId) =>
        Attach(
            entityId,
            cellId,
            _firstPhysicalEntityByCell,
            _nextPhysicalEntity);

    internal void AttachSemantic(int entityId, int cellId) =>
        Attach(
            entityId,
            cellId,
            _firstSemanticEntityByCell,
            _nextSemanticEntity);

    internal void DetachPhysical(int entityId, int cellId) =>
        Detach(
            entityId,
            cellId,
            _firstPhysicalEntityByCell,
            _nextPhysicalEntity);

    internal void DetachSemantic(int entityId, int cellId) =>
        Detach(
            entityId,
            cellId,
            _firstSemanticEntityByCell,
            _nextSemanticEntity);

    internal void MovePhysical(int entityId, int fromCellId, int toCellId)
    {
        DetachPhysical(entityId, fromCellId);
        AttachPhysical(entityId, toCellId);
    }

    internal void MoveSemantic(int entityId, int fromCellId, int toCellId)
    {
        DetachSemantic(entityId, fromCellId);
        AttachSemantic(entityId, toCellId);
    }

    internal int FirstPhysicalEntity(int cellId) => _firstPhysicalEntityByCell[cellId];

    internal int FirstSemanticEntity(int cellId) => _firstSemanticEntityByCell[cellId];

    internal int NextPhysicalEntity(int entityId) => _nextPhysicalEntity[entityId];

    internal int NextSemanticEntity(int entityId) => _nextSemanticEntity[entityId];

    private static void Attach(
        int entityId,
        int cellId,
        int[] firstEntityByCell,
        int[] nextEntity)
    {
        ValidateIds(entityId, cellId, firstEntityByCell, nextEntity);
        nextEntity[entityId] = firstEntityByCell[cellId];
        firstEntityByCell[cellId] = entityId;
    }

    private static void Detach(
        int entityId,
        int cellId,
        int[] firstEntityByCell,
        int[] nextEntity)
    {
        ValidateIds(entityId, cellId, firstEntityByCell, nextEntity);
        var current = firstEntityByCell[cellId];
        if (current == entityId)
        {
            firstEntityByCell[cellId] = nextEntity[entityId];
            nextEntity[entityId] = 0;
            return;
        }

        while (current > 0)
        {
            var next = nextEntity[current];
            if (next == entityId)
            {
                nextEntity[current] = nextEntity[entityId];
                nextEntity[entityId] = 0;
                return;
            }

            current = next;
        }

        throw new global::System.InvalidOperationException();
    }

    private static void ValidateIds(
        int entityId,
        int cellId,
        int[] firstEntityByCell,
        int[] nextEntity)
    {
        if ((uint)entityId == 0u || entityId >= nextEntity.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(entityId));
        }

        if ((uint)cellId == 0u || cellId >= firstEntityByCell.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(cellId));
        }
    }
}
