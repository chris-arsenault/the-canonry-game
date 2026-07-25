namespace Engine.Kernel;

internal sealed partial class PrimitiveLongTable
{
    private readonly int[] _firstKeys;
    private readonly int[] _secondKeys;
    private readonly long[] _values;
    private readonly int[] _occupiedSlots;
    private readonly int _slotMask;
    private int _count;

    internal PrimitiveLongTable(int capacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(capacity);
        _firstKeys = new int[capacity];
        _secondKeys = new int[capacity];
        _values = new long[capacity];
        _occupiedSlots = new int[capacity];
        _slotMask = (capacity & (capacity - 1)) == 0 ? capacity - 1 : -1;
    }

    internal long GetOrDefault(int firstKey, int secondKey)
    {
        ValidateKeys(firstKey);
        var slot = FindSlot(firstKey, secondKey, out var found);
        return slot >= 0 && found ? _values[slot] : 0L;
    }

    internal void AddChecked(int firstKey, int secondKey, long delta)
    {
        ValidateKeys(firstKey);
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

        _values[slot] = checked(_values[slot] + delta);
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

    private static void ValidateKeys(int firstKey)
    {
        NumericGuard.ThrowIfNegativeOrZero(firstKey);
    }

    private void RebuildOccupiedSlots()
    {
        _count = 0;
        for (var slot = 0; slot < _firstKeys.Length; slot++)
        {
            if (_firstKeys[slot] == 0)
            {
                continue;
            }

            _occupiedSlots[_count] = slot;
            _count++;
        }
    }
}
