namespace Engine.Kernel;

internal sealed partial class PrimitiveFloatTable
{
    private readonly int[] _firstKeys;
    private readonly int[] _secondKeys;
    private readonly float[] _values;
    private readonly int[] _occupiedSlots;
    private int _count;

    public PrimitiveFloatTable(int capacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _firstKeys = new int[capacity];
        _secondKeys = new int[capacity];
        _values = new float[capacity];
        _occupiedSlots = new int[capacity];
    }

    public int Capacity => _firstKeys.Length;

    public int Count => _count;

    public float GetOrDefault(int firstKey, int secondKey)
    {
        return TryGet(firstKey, secondKey, out var value) ? value : 0f;
    }

    public float GetOrDefault(int firstKey, int secondKey, float fallback)
    {
        return TryGet(firstKey, secondKey, out var value) ? value : fallback;
    }

    public bool TryGet(int firstKey, int secondKey, out float value)
    {
        ValidateKeys(firstKey, secondKey);
        var slot = FindSlot(firstKey, secondKey, out var found);
        if (found)
        {
            value = _values[slot];
            return true;
        }

        value = 0f;
        return false;
    }

    public void Set(int firstKey, int secondKey, float value)
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

    public void Add(int firstKey, int secondKey, float delta)
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
            _values[slot] = 0f;
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
        ValidateFirstKey(firstKey);
    }

    private static void ValidateFirstKey(int firstKey)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(firstKey);
    }
}
