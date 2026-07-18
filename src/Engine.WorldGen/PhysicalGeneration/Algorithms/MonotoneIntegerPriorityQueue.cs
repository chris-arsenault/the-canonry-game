namespace Engine.WorldGen;

/// <summary>
/// Linear-time priority queue for flood algorithms whose integer priorities
/// never decrease after dequeue. Each item may be enqueued once.
/// </summary>
internal sealed class MonotoneIntegerPriorityQueue
{
    private readonly int[] _bucketHeads;
    private readonly int[] _bucketTails;
    private readonly int[] _nextItems;
    private readonly int _minimumPriority;
    private int _currentBucket;

    public MonotoneIntegerPriorityQueue(int itemCapacity, int minimumPriority, int maximumPriority)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(itemCapacity);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumPriority, minimumPriority);

        _minimumPriority = minimumPriority;
        _bucketHeads = new int[checked(maximumPriority - minimumPriority + 1)];
        _bucketTails = new int[_bucketHeads.Length];
        _nextItems = new int[itemCapacity];
        Array.Fill(_bucketHeads, -1);
        Array.Fill(_bucketTails, -1);
        Array.Fill(_nextItems, -1);
    }

    public int Count { get; private set; }

    public void Enqueue(int item, int priority)
    {
        var bucket = checked(priority - _minimumPriority);
        if ((uint)item >= (uint)_nextItems.Length || (uint)bucket >= (uint)_bucketHeads.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        if (_bucketTails[bucket] < 0)
        {
            _bucketHeads[bucket] = item;
        }
        else
        {
            _nextItems[_bucketTails[bucket]] = item;
        }

        _bucketTails[bucket] = item;
        Count++;
    }

    public bool TryDequeue(out int item, out int priority)
    {
        while (_currentBucket < _bucketHeads.Length && _bucketHeads[_currentBucket] < 0)
        {
            _currentBucket++;
        }

        if (_currentBucket >= _bucketHeads.Length)
        {
            item = -1;
            priority = 0;
            return false;
        }

        item = _bucketHeads[_currentBucket];
        _bucketHeads[_currentBucket] = _nextItems[item];
        _nextItems[item] = -1;
        if (_bucketHeads[_currentBucket] < 0)
        {
            _bucketTails[_currentBucket] = -1;
        }

        Count--;
        priority = _currentBucket + _minimumPriority;
        return true;
    }
}
