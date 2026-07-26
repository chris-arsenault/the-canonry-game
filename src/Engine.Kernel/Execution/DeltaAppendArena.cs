namespace Engine.Kernel;

internal sealed class DeltaAppendArena<T>
    where T : struct
{
    private const int BlockSize = 64;

    private readonly T[] _values;
    private readonly int[] _blockCounts;
    private readonly int[] _workerCurrentBlocks;
    private readonly int[] _workerCounts;
    private readonly int _logicalCapacity;
    private int _claimedBlockCount;

    internal DeltaAppendArena(int capacity, int workerCount)
    {
        NumericGuard.ThrowIfNegativeOrZero(capacity);
        NumericGuard.ThrowIfNegativeOrZero(workerCount);
        _logicalCapacity = capacity;
        // Partial worker blocks need bounded tail room so fragmentation cannot
        // reject a tick whose actual output still fits the logical capacity.
        var maximumConcurrentTails = global::System.Math.Min(workerCount, capacity);
        var storageCapacity = checked(
            capacity + ((maximumConcurrentTails - 1) * (BlockSize - 1)));
        _values = new T[storageCapacity];
        _blockCounts = new int[checked((storageCapacity + BlockSize - 1) / BlockSize)];
        _workerCurrentBlocks = new int[workerCount];
        _workerCounts = new int[workerCount];
        global::System.Array.Fill(_workerCurrentBlocks, -1);
    }

    internal T[] Values => _values;

    internal int Count
    {
        get
        {
            var count = 0;
            for (var workerIndex = 0; workerIndex < _workerCounts.Length; workerIndex++)
            {
                count = checked(count + _workerCounts[workerIndex]);
            }

            return count;
        }
    }

    internal void Add(int workerIndex, in T value)
    {
        if ((uint)workerIndex >= (uint)_workerCurrentBlocks.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(workerIndex));
        }

        if (_workerCurrentBlocks.Length == 1)
        {
            var count = _workerCounts[0];
            if (count >= _logicalCapacity)
            {
                throw new global::System.InvalidOperationException();
            }

            _values[count] = value;
            _workerCounts[0] = count + 1;
            return;
        }

        var block = _workerCurrentBlocks[workerIndex];
        if (block < 0 || _blockCounts[block] >= CapacityOfBlock(block))
        {
            block = global::System.Threading.Interlocked.Increment(
                ref _claimedBlockCount) - 1;
            if ((uint)block >= (uint)_blockCounts.Length)
            {
                throw new global::System.InvalidOperationException();
            }

            _workerCurrentBlocks[workerIndex] = block;
        }

        var blockOffset = _blockCounts[block];
        _values[checked(block * BlockSize + blockOffset)] = value;
        _blockCounts[block] = blockOffset + 1;
        _workerCounts[workerIndex]++;
    }

    internal int Compact()
    {
        if (_workerCurrentBlocks.Length == 1)
        {
            return _workerCounts[0];
        }

        var destination = 0;
        var claimedBlockCount = global::System.Threading.Volatile.Read(
            ref _claimedBlockCount);
        for (var block = 0; block < claimedBlockCount; block++)
        {
            var count = _blockCounts[block];
            if (count == 0)
            {
                continue;
            }

            if (destination > _logicalCapacity - count)
            {
                throw new global::System.InvalidOperationException();
            }

            var source = block * BlockSize;
            if (destination != source)
            {
                global::System.Array.Copy(
                    _values,
                    source,
                    _values,
                    destination,
                    count);
            }

            destination += count;
        }

        return destination;
    }

    internal void Clear()
    {
        if (_workerCurrentBlocks.Length == 1)
        {
            _workerCounts[0] = 0;
            return;
        }

        var claimedBlockCount = global::System.Math.Min(
            global::System.Threading.Volatile.Read(ref _claimedBlockCount),
            _blockCounts.Length);
        global::System.Array.Clear(_blockCounts, 0, claimedBlockCount);
        global::System.Array.Clear(_workerCounts);
        global::System.Array.Fill(_workerCurrentBlocks, -1);
        global::System.Threading.Volatile.Write(ref _claimedBlockCount, 0);
    }

    private int CapacityOfBlock(int block)
    {
        var start = block * BlockSize;
        return global::System.Math.Min(BlockSize, _values.Length - start);
    }
}
