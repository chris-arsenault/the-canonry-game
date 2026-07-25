namespace Engine.Kernel;

internal struct EpochStampedArray<T>
{
    private readonly int[] _epochs;
    private readonly T[] _values;
    private int _epoch;

    public EpochStampedArray(int capacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(capacity);
        _epochs = new int[capacity];
        _values = new T[capacity];
        _epoch = 0;
    }

    public int Length => _values.Length;

    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void BeginEpoch()
    {
        if (_epoch == int.MaxValue)
        {
            global::System.Array.Clear(_epochs);
            _epoch = 1;
            return;
        }

        _epoch++;
    }

    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public T GetOrDefault(int index, T fallback)
    {
        return _epochs[index] == _epoch ? _values[index] : fallback;
    }

    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void Set(int index, T value)
    {
        _epochs[index] = _epoch;
        _values[index] = value;
    }
}

internal struct EpochStampSet
{
    private readonly int[] _epochs;
    private int _epoch;

    public EpochStampSet(int capacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(capacity);
        _epochs = new int[capacity];
        _epoch = 0;
    }

    public int Length => _epochs.Length;

    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void BeginEpoch()
    {
        if (_epoch == int.MaxValue)
        {
            global::System.Array.Clear(_epochs);
            _epoch = 1;
            return;
        }

        _epoch++;
    }

    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public bool Contains(int index)
    {
        return _epochs[index] == _epoch;
    }

    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void Add(int index)
    {
        _epochs[index] = _epoch;
    }

    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void Remove(int index)
    {
        if (_epochs[index] == _epoch)
        {
            _epochs[index] = 0;
        }
    }
}
