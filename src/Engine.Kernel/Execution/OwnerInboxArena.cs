namespace Engine.Kernel;

internal sealed class OwnerInboxArena<T>
    where T : struct
{
    private const int BlockSize = 64;

    private readonly T[] _appendValues;
    private readonly T[] _values;
    private readonly int[] _blockCounts;
    private readonly int[] _blockOwners;
    private readonly int[] _channelCurrentBlocks;
    private readonly int[] _channelCounts;
    private readonly int[] _ownerCounts;
    private readonly int[] _ownerStarts;
    private readonly int[] _ownerCursors;
    private readonly int _logicalCapacity;
    private readonly int _ownerCount;
    private int _claimedBlockCount;

    internal OwnerInboxArena(
        int capacity,
        int workerCount,
        int ownerCount)
    {
        NumericGuard.ThrowIfNegativeOrZero(capacity);
        NumericGuard.ThrowIfNegativeOrZero(workerCount);
        NumericGuard.ThrowIfNegativeOrZero(ownerCount);

        _logicalCapacity = capacity;
        _ownerCount = ownerCount;
        var channelCount = checked(workerCount * ownerCount);
        var maximumConcurrentTails = global::System.Math.Min(
            channelCount,
            capacity);
        var storageCapacity = checked(
            capacity + ((maximumConcurrentTails - 1) * (BlockSize - 1)));
        var blockCapacity = checked(
            (storageCapacity + BlockSize - 1) / BlockSize);

        _appendValues = new T[storageCapacity];
        _values = new T[capacity];
        _blockCounts = new int[blockCapacity];
        _blockOwners = new int[blockCapacity];
        _channelCurrentBlocks = new int[channelCount];
        _channelCounts = new int[channelCount];
        _ownerCounts = new int[ownerCount];
        _ownerStarts = new int[ownerCount + 1];
        _ownerCursors = new int[ownerCount];
        global::System.Array.Fill(_channelCurrentBlocks, -1);
    }

    internal T[] Values => _values;

    internal int OwnerCount => _ownerCount;

    internal int Count
    {
        get
        {
            var count = 0;
            for (var channel = 0;
                 channel < _channelCounts.Length;
                 channel++)
            {
                count = checked(count + _channelCounts[channel]);
            }

            return count;
        }
    }

    internal void Add(
        int workerIndex,
        int ownerIndex,
        in T value)
    {
        if ((uint)ownerIndex >= (uint)_ownerCount)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(ownerIndex));
        }

        var channel = checked((workerIndex * _ownerCount) + ownerIndex);
        if ((uint)channel >= (uint)_channelCurrentBlocks.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(workerIndex));
        }

        var block = _channelCurrentBlocks[channel];
        if (block < 0 || _blockCounts[block] >= CapacityOfBlock(block))
        {
            block = global::System.Threading.Interlocked.Increment(
                ref _claimedBlockCount) - 1;
            if ((uint)block >= (uint)_blockCounts.Length)
            {
                throw new global::System.InvalidOperationException();
            }

            _blockOwners[block] = ownerIndex;
            _channelCurrentBlocks[channel] = block;
        }

        var blockOffset = _blockCounts[block];
        _appendValues[checked((block * BlockSize) + blockOffset)] = value;
        _blockCounts[block] = blockOffset + 1;
        _channelCounts[channel]++;
    }

    internal int Seal()
    {
        global::System.Array.Clear(_ownerCounts);
        global::System.Array.Clear(_ownerStarts);
        global::System.Array.Clear(_ownerCursors);

        var total = 0;
        var claimedBlockCount = global::System.Threading.Volatile.Read(
            ref _claimedBlockCount);
        for (var block = 0; block < claimedBlockCount; block++)
        {
            var count = _blockCounts[block];
            if (count == 0)
            {
                continue;
            }

            total = checked(total + count);
            if (total > _logicalCapacity)
            {
                throw new global::System.InvalidOperationException();
            }

            _ownerCounts[_blockOwners[block]] = checked(
                _ownerCounts[_blockOwners[block]] + count);
        }

        for (var owner = 0; owner < _ownerCount; owner++)
        {
            _ownerStarts[owner + 1] = checked(
                _ownerStarts[owner] + _ownerCounts[owner]);
            _ownerCursors[owner] = _ownerStarts[owner];
        }

        for (var block = 0; block < claimedBlockCount; block++)
        {
            var count = _blockCounts[block];
            if (count == 0)
            {
                continue;
            }

            var owner = _blockOwners[block];
            var destination = _ownerCursors[owner];
            global::System.Array.Copy(
                _appendValues,
                block * BlockSize,
                _values,
                destination,
                count);
            _ownerCursors[owner] = destination + count;
        }

        return total;
    }

    internal int OwnerStart(int ownerIndex)
    {
        if ((uint)ownerIndex > (uint)_ownerCount)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(ownerIndex));
        }

        return _ownerStarts[ownerIndex];
    }

    internal int OwnerLimit(int ownerIndex)
    {
        if ((uint)ownerIndex >= (uint)_ownerCount)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(ownerIndex));
        }

        return _ownerStarts[ownerIndex + 1];
    }

    internal int ReplaceSealedOwnerCounts(int[] ownerCounts)
    {
        global::System.ArgumentNullException.ThrowIfNull(ownerCounts);
        if (ownerCounts.Length != _ownerCount)
        {
            throw new global::System.InvalidOperationException();
        }

        var total = 0;
        _ownerStarts[0] = 0;
        for (var owner = 0; owner < _ownerCount; owner++)
        {
            if (ownerCounts[owner] < 0)
            {
                throw new global::System.InvalidOperationException();
            }
            total = checked(total + ownerCounts[owner]);
            if (total > _logicalCapacity)
            {
                throw new global::System.InvalidOperationException();
            }

            _ownerCounts[owner] = ownerCounts[owner];
            _ownerStarts[owner + 1] = total;
        }

        return total;
    }

    internal void Clear()
    {
        var claimedBlockCount = global::System.Math.Min(
            global::System.Threading.Volatile.Read(ref _claimedBlockCount),
            _blockCounts.Length);
        global::System.Array.Clear(_blockCounts, 0, claimedBlockCount);
        global::System.Array.Clear(_channelCounts);
        global::System.Array.Fill(_channelCurrentBlocks, -1);
        global::System.Array.Clear(_ownerCounts);
        global::System.Array.Clear(_ownerStarts);
        global::System.Array.Clear(_ownerCursors);
        global::System.Threading.Volatile.Write(ref _claimedBlockCount, 0);
    }

    private int CapacityOfBlock(int block)
    {
        var start = block * BlockSize;
        return global::System.Math.Min(
            BlockSize,
            _appendValues.Length - start);
    }
}
