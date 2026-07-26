namespace Engine.Kernel;

internal sealed class MapCellRegistry
{
    private readonly int[] _cellIdsByHashSlot;
    private readonly int[] _planeCodes;
    private readonly int[] _q;
    private readonly int[] _r;
    private readonly int[] _z;
    private readonly int _hashMask;

    internal MapCellRegistry(int capacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(capacity);
        var hashCapacity = 1;
        while (hashCapacity < checked(capacity * 2))
        {
            hashCapacity <<= 1;
        }

        _cellIdsByHashSlot = new int[hashCapacity];
        _planeCodes = new int[capacity + 1];
        _q = new int[capacity + 1];
        _r = new int[capacity + 1];
        _z = new int[capacity + 1];
        _hashMask = hashCapacity - 1;
    }

    internal int Capacity => _planeCodes.Length - 1;

    internal int Count { get; private set; }

    internal bool TryGet(int planeCode, int q, int r, int z, out int cellId)
    {
        var slot = HashSlot(planeCode, q, r, z);
        for (var probe = 0; probe < _cellIdsByHashSlot.Length; probe++)
        {
            cellId = _cellIdsByHashSlot[slot];
            if (cellId == 0)
            {
                return false;
            }

            if (_planeCodes[cellId] == planeCode &&
                _q[cellId] == q &&
                _r[cellId] == r &&
                _z[cellId] == z)
            {
                return true;
            }

            slot = (slot + 1) & _hashMask;
        }

        cellId = 0;
        return false;
    }

    internal int GetOrAdd(int planeCode, int q, int r, int z)
    {
        if (TryGet(planeCode, q, r, z, out var cellId))
        {
            return cellId;
        }

        if (Count >= Capacity)
        {
            throw new global::System.InvalidOperationException();
        }

        cellId = ++Count;
        _planeCodes[cellId] = planeCode;
        _q[cellId] = q;
        _r[cellId] = r;
        _z[cellId] = z;
        InsertHash(cellId);
        return cellId;
    }

    internal int PlaneCode(int cellId)
    {
        EnsureCell(cellId);
        return _planeCodes[cellId];
    }

    internal int Q(int cellId)
    {
        EnsureCell(cellId);
        return _q[cellId];
    }

    internal int R(int cellId)
    {
        EnsureCell(cellId);
        return _r[cellId];
    }

    internal int Z(int cellId)
    {
        EnsureCell(cellId);
        return _z[cellId];
    }

    internal void WriteSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(Count);
        for (var cellId = 1; cellId <= Count; cellId++)
        {
            writer.Write(_planeCodes[cellId]);
            writer.Write(_q[cellId]);
            writer.Write(_r[cellId]);
            writer.Write(_z[cellId]);
        }
    }

    internal void ReadSnapshot(global::System.IO.BinaryReader reader)
    {
        global::System.Array.Clear(_cellIdsByHashSlot);
        global::System.Array.Clear(_planeCodes);
        global::System.Array.Clear(_q);
        global::System.Array.Clear(_r);
        global::System.Array.Clear(_z);
        Count = 0;
        var count = reader.ReadInt32();
        if ((uint)count > (uint)Capacity)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var expectedCellId = 1; expectedCellId <= count; expectedCellId++)
        {
            var cellId = GetOrAdd(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32());
            if (cellId != expectedCellId)
            {
                throw new global::System.InvalidOperationException();
            }
        }
    }

    private void InsertHash(int cellId)
    {
        var slot = HashSlot(_planeCodes[cellId], _q[cellId], _r[cellId], _z[cellId]);
        while (_cellIdsByHashSlot[slot] != 0)
        {
            slot = (slot + 1) & _hashMask;
        }

        _cellIdsByHashSlot[slot] = cellId;
    }

    private int HashSlot(int planeCode, int q, int r, int z)
    {
        unchecked
        {
            var hash = (uint)planeCode * 2246822519u;
            hash ^= (uint)q * 2654435761u;
            hash ^= (uint)r * 3266489917u;
            hash ^= (uint)z * 668265263u;
            hash ^= hash >> 16;
            return (int)(hash & (uint)_hashMask);
        }
    }

    private void EnsureCell(int cellId)
    {
        if ((uint)cellId == 0u || cellId > Count)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(cellId));
        }
    }
}
