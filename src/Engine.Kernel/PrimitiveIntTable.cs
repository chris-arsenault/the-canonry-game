namespace Engine.Kernel;

internal sealed partial class PrimitiveIntTable
{
    private readonly int[] _firstKeys;
    private readonly int[] _secondKeys;
    private readonly int[] _values;
    private readonly int[] _occupiedSlots;
    private int _count;

    public PrimitiveIntTable(int capacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _firstKeys = new int[capacity];
        _secondKeys = new int[capacity];
        _values = new int[capacity];
        _occupiedSlots = new int[capacity];
    }

    public int Capacity => _firstKeys.Length;

    public int Count => _count;

    public int GetOrDefault(int firstKey, int secondKey)
    {
        return TryGet(firstKey, secondKey, out var value) ? value : 0;
    }

    public bool TryGet(int firstKey, int secondKey, out int value)
    {
        ValidateKeys(firstKey, secondKey);
        var slot = FindSlot(firstKey, secondKey, out var found);
        if (found)
        {
            value = _values[slot];
            return true;
        }

        value = 0;
        return false;
    }

    public void Set(int firstKey, int secondKey, int value)
    {
        ValidateKeys(firstKey, secondKey);
        var slot = FindSlot(firstKey, secondKey, out var found);
        if (!found)
        {
            _firstKeys[slot] = firstKey;
            _secondKeys[slot] = secondKey;
            _occupiedSlots[_count] = slot;
            _count++;
        }

        _values[slot] = value;
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

        throw new global::System.InvalidOperationException();
    }

    private int SlotIndex(int firstKey, int secondKey)
    {
        unchecked
        {
            var hash = (uint)firstKey * 2654435761u;
            hash ^= (uint)secondKey * 2246822519u;
            return (int)(hash % (uint)_firstKeys.Length);
        }
    }

    private static void ValidateKeys(int firstKey, int secondKey)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(firstKey);
    }
}
