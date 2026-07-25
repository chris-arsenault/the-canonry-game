namespace Engine.Kernel;

internal sealed partial class PrimitiveScalarTable
{
    private readonly int[] _firstKeys;
    private readonly int[] _secondKeys;
    private readonly FixedScalar[] _values;
    private readonly int[] _occupiedSlots;
    private readonly int _slotMask;
    private int _count;

    public PrimitiveScalarTable(int capacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(capacity);
        _firstKeys = new int[capacity];
        _secondKeys = new int[capacity];
        _values = new FixedScalar[capacity];
        _occupiedSlots = new int[capacity];
        _slotMask = (capacity & (capacity - 1)) == 0 ? capacity - 1 : -1;
    }

    public int Capacity => _firstKeys.Length;

    public int Count => _count;

    public FixedScalar GetOrDefault(int firstKey, int secondKey)
    {
        return TryGet(firstKey, secondKey, out var value) ? value : 0;
    }

    public FixedScalar GetOrDefault(int firstKey, int secondKey, FixedScalar fallback)
    {
        return TryGet(firstKey, secondKey, out var value) ? value : fallback;
    }

    public bool TryGet(int firstKey, int secondKey, out FixedScalar value)
    {
        ValidateKeys(firstKey, secondKey);
        var slot = FindSlot(firstKey, secondKey, out var found);
        if (slot < 0)
        {
            value = 0;
            return false;
        }

        if (found)
        {
            value = _values[slot];
            return true;
        }

        value = 0;
        return false;
    }

    public void Set(int firstKey, int secondKey, FixedScalar value)
    {
        ValidateKeys(firstKey, secondKey);
        var slot = FindSlot(firstKey, secondKey, out var found);
        if (slot < 0)
        {
            throw new global::System.InvalidOperationException();
        }

        if (!found)
        {
            _firstKeys[slot] = firstKey;
            _secondKeys[slot] = secondKey;
            _occupiedSlots[_count] = slot;
            _count++;
        }

        _values[slot] = value;
    }

    public void Add(int firstKey, int secondKey, FixedScalar delta)
    {
        ValidateKeys(firstKey, secondKey);
        var slot = FindSlot(firstKey, secondKey, out var found);
        if (slot < 0)
        {
            throw new global::System.InvalidOperationException();
        }

        if (!found)
        {
            _firstKeys[slot] = firstKey;
            _secondKeys[slot] = secondKey;
            _occupiedSlots[_count] = slot;
            _count++;
        }

        _values[slot] += delta;
    }

    public int CountSecondKeysForFirst(int firstKey)
    {
        ValidateFirstKey(firstKey);
        var count = 0;
        for (var i = 0; i < _count; i++)
        {
            if (_firstKeys[_occupiedSlots[i]] == firstKey)
            {
                count++;
            }
        }

        return count;
    }

    public int CopySecondKeysForFirst(int firstKey, global::System.Span<int> destination)
    {
        ValidateFirstKey(firstKey);
        var count = 0;
        for (var i = 0; i < _count && count < destination.Length; i++)
        {
            var slot = _occupiedSlots[i];
            if (_firstKeys[slot] == firstKey)
            {
                destination[count] = _secondKeys[slot];
                count++;
            }
        }

        return count;
    }

    public void Clear()
    {
        for (var i = 0; i < _count; i++)
        {
            var slot = _occupiedSlots[i];
            _firstKeys[slot] = 0;
            _secondKeys[slot] = 0;
            _values[slot] = 0;
        }

        _count = 0;
    }

    internal void GetEntry(int index, out int firstKey, out int secondKey, out FixedScalar value)
    {
        if ((uint)index >= (uint)_count)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(index));
        }

        var slot = _occupiedSlots[index];
        firstKey = _firstKeys[slot];
        secondKey = _secondKeys[slot];
        value = _values[slot];
    }

    private void RebuildOccupiedSlots()
    {
        _count = 0;
        for (var slot = 0; slot < _firstKeys.Length; slot++)
        {
            if (_firstKeys[slot] != 0)
            {
                _occupiedSlots[_count] = slot;
                _count++;
            }
        }
    }

    private int FindSlot(int firstKey, int secondKey, out bool found)
    {
        var index = SlotIndex(firstKey, secondKey);
        for (var probe = 0; probe < _firstKeys.Length; probe++)
        {
            var existingFirst = _firstKeys[index];
            if (existingFirst == 0)
            {
                found = false;
                return index;
            }

            if (existingFirst == firstKey && _secondKeys[index] == secondKey)
            {
                found = true;
                return index;
            }

            index++;
            if (index == _firstKeys.Length)
            {
                index = 0;
            }
        }

        found = false;
        return -1;
    }

    private int SlotIndex(int firstKey, int secondKey)
    {
        unchecked
        {
            var hash = (uint)firstKey * 2654435761u;
            hash ^= (uint)secondKey * 2246822519u;
            return _slotMask >= 0
                ? (int)(hash & (uint)_slotMask)
                : (int)(hash % (uint)_firstKeys.Length);
        }
    }

    private static void ValidateKeys(int firstKey, int secondKey)
    {
        ValidateFirstKey(firstKey);
    }

    private static void ValidateFirstKey(int firstKey)
    {
        NumericGuard.ThrowIfNegativeOrZero(firstKey);
    }
}
