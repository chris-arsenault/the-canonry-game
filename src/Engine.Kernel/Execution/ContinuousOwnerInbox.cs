namespace Engine.Kernel;

internal interface IContinuousOwnerInbox
{
    int PendingCount { get; }

    bool HasPending(int firstOwner, int ownerLimit);

    void AttachActivity(ContinuousOwnerActivity activity);
}

internal sealed class ContinuousOwnerActivity
{
    private readonly int[] _ownerPendingCounts;
    private int _pendingCount;

    internal ContinuousOwnerActivity(int ownerCount)
    {
        NumericGuard.ThrowIfNegativeOrZero(ownerCount);
        _ownerPendingCounts = new int[ownerCount];
    }

    internal int PendingCount =>
        global::System.Threading.Volatile.Read(ref _pendingCount);

    internal void Add(int ownerIndex, int count)
    {
        global::System.Threading.Interlocked.Add(
            ref _ownerPendingCounts[ownerIndex],
            count);
        global::System.Threading.Interlocked.Add(
            ref _pendingCount,
            count);
    }

    internal void Remove(int ownerIndex, int count)
    {
        global::System.Threading.Interlocked.Add(
            ref _ownerPendingCounts[ownerIndex],
            -count);
        global::System.Threading.Interlocked.Add(
            ref _pendingCount,
            -count);
    }

    internal bool HasPending(int firstOwner, int ownerLimit)
    {
        if ((uint)firstOwner >= (uint)_ownerPendingCounts.Length ||
            ownerLimit <= firstOwner ||
            ownerLimit > _ownerPendingCounts.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(firstOwner));
        }

        for (var owner = firstOwner; owner < ownerLimit; owner++)
        {
            if (global::System.Threading.Volatile.Read(
                    ref _ownerPendingCounts[owner]) != 0)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Fixed-capacity multi-producer, single-logical-owner inbox. Producers append
/// under the destination owner's short gate. The destination detaches whole
/// blocks and consumes them without holding that gate.
/// </summary>
internal sealed class ContinuousOwnerInbox<T> : IContinuousOwnerInbox
    where T : struct
{
    private const int BlockSize = 64;

    private readonly T[] _values;
    private readonly int[] _blockCounts;
    private readonly int[] _blockNext;
    private readonly int[] _ownerHeads;
    private readonly int[] _ownerTails;
    private readonly int[] _ownerPendingCounts;
    private readonly object[] _ownerGates;
    private readonly int[] _freeBlocks;
    private readonly object _freeGate = new();
    private ContinuousOwnerActivity _activity;
    private int _freeBlockCount;
    private int _pendingCount;

    internal ContinuousOwnerInbox(int capacity, int ownerCount)
    {
        NumericGuard.ThrowIfNegativeOrZero(capacity);
        NumericGuard.ThrowIfNegativeOrZero(ownerCount);
        var blockCount = checked(
            ((capacity + BlockSize - 1) / BlockSize) + ownerCount);
        _values = new T[checked(blockCount * BlockSize)];
        _blockCounts = new int[blockCount];
        _blockNext = new int[blockCount];
        _ownerHeads = new int[ownerCount];
        _ownerTails = new int[ownerCount];
        _ownerPendingCounts = new int[ownerCount];
        _ownerGates = new object[ownerCount];
        _freeBlocks = new int[blockCount];
        _activity = new ContinuousOwnerActivity(ownerCount);
        global::System.Array.Fill(_ownerHeads, -1);
        global::System.Array.Fill(_ownerTails, -1);
        global::System.Array.Fill(_blockNext, -1);
        for (var owner = 0; owner < ownerCount; owner++)
        {
            _ownerGates[owner] = new object();
        }

        for (var block = 0; block < blockCount; block++)
        {
            _freeBlocks[block] = block;
        }

        _freeBlockCount = blockCount;
    }

    internal int PendingCount =>
        global::System.Threading.Volatile.Read(ref _pendingCount);

    int IContinuousOwnerInbox.PendingCount => PendingCount;

    bool IContinuousOwnerInbox.HasPending(
        int firstOwner,
        int ownerLimit) =>
        HasPending(firstOwner, ownerLimit);

    void IContinuousOwnerInbox.AttachActivity(
        ContinuousOwnerActivity activity)
    {
        global::System.ArgumentNullException.ThrowIfNull(activity);
        if (PendingCount != 0)
        {
            throw new global::System.InvalidOperationException();
        }

        _activity = activity;
    }

    internal bool HasPending(int firstOwner, int ownerLimit)
    {
        if ((uint)firstOwner >= (uint)_ownerHeads.Length ||
            ownerLimit <= firstOwner ||
            ownerLimit > _ownerHeads.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(firstOwner));
        }

        for (var owner = firstOwner; owner < ownerLimit; owner++)
        {
            if (global::System.Threading.Volatile.Read(
                    ref _ownerPendingCounts[owner]) != 0)
            {
                return true;
            }
        }

        return false;
    }

    internal void Add(int ownerIndex, in T value)
    {
        ValidateOwner(ownerIndex);
        lock (_ownerGates[ownerIndex])
        {
            var tail = _ownerTails[ownerIndex];
            if (tail < 0 || _blockCounts[tail] == BlockSize)
            {
                tail = ClaimBlock();
                var previousTail = _ownerTails[ownerIndex];
                if (previousTail < 0)
                {
                    _ownerHeads[ownerIndex] = tail;
                }
                else
                {
                    _blockNext[previousTail] = tail;
                }

                _ownerTails[ownerIndex] = tail;
            }

            var offset = _blockCounts[tail];
            _values[checked((tail * BlockSize) + offset)] = value;
            _blockCounts[tail] = offset + 1;
            global::System.Threading.Volatile.Write(
                ref _ownerPendingCounts[ownerIndex],
                _ownerPendingCounts[ownerIndex] + 1);
            _activity.Add(ownerIndex, 1);
            global::System.Threading.Interlocked.Increment(
                ref _pendingCount);
        }
    }

    internal DrainCursor BeginDrain(int ownerIndex)
    {
        ValidateOwner(ownerIndex);
        lock (_ownerGates[ownerIndex])
        {
            var head = _ownerHeads[ownerIndex];
            if (head < 0)
            {
                return default;
            }

            _ownerHeads[ownerIndex] = -1;
            _ownerTails[ownerIndex] = -1;
            var count = 0;
            for (var block = head; block >= 0; block = _blockNext[block])
            {
                count = checked(count + _blockCounts[block]);
            }

            global::System.Threading.Interlocked.Add(
                ref _pendingCount,
                -count);
            global::System.Threading.Volatile.Write(
                ref _ownerPendingCounts[ownerIndex],
                0);
            _activity.Remove(ownerIndex, count);
            return new DrainCursor(head, count);
        }
    }

    internal bool MoveNext(ref DrainCursor cursor, out T value)
    {
        if (cursor.Remaining == 0)
        {
            value = default;
            return false;
        }

        var block = cursor.Block;
        value = _values[checked(
            (block * BlockSize) + cursor.BlockOffset)];
        cursor.BlockOffset++;
        cursor.Remaining--;
        if (cursor.BlockOffset == _blockCounts[block])
        {
            var next = _blockNext[block];
            ReleaseBlock(block);
            cursor.Block = next;
            cursor.BlockOffset = 0;
        }

        return true;
    }

    private int ClaimBlock()
    {
        lock (_freeGate)
        {
            if (_freeBlockCount == 0)
            {
                throw new global::System.InvalidOperationException();
            }

            var block = _freeBlocks[--_freeBlockCount];
            _blockCounts[block] = 0;
            _blockNext[block] = -1;
            return block;
        }
    }

    private void ReleaseBlock(int block)
    {
        _blockCounts[block] = 0;
        _blockNext[block] = -1;
        lock (_freeGate)
        {
            _freeBlocks[_freeBlockCount++] = block;
        }
    }

    private void ValidateOwner(int ownerIndex)
    {
        if ((uint)ownerIndex >= (uint)_ownerHeads.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(ownerIndex));
        }
    }

    internal struct DrainCursor
    {
        internal DrainCursor(int block, int remaining)
        {
            Block = block;
            Remaining = remaining;
        }

        internal int Block;
        internal int BlockOffset;
        internal int Remaining;
    }
}
