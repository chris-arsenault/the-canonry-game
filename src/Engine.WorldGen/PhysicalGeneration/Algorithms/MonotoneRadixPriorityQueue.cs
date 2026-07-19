namespace Engine.WorldGen;

using System.Numerics;

/// <summary>
/// Monotone radix heap for signed 64-bit priorities. It retains linear-size
/// storage when fixed-point elevation greatly expands the numeric key range.
/// Each item may be enqueued once and priorities may not precede the last
/// dequeued priority.
/// </summary>
internal sealed class MonotoneRadixPriorityQueue
{
    private readonly List<Entry>[] _buckets = Enumerable.Range(0, 65)
        .Select(static _ => new List<Entry>())
        .ToArray();
    private readonly Queue<Entry> _minimum = new();
    private ulong _lastKey;

    public int Count { get; private set; }

    public void Enqueue(int item, long priority)
    {
        var key = OrderedKey(priority);
        if (key < _lastKey)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        var entry = new Entry(item, key);
        var bucket = BucketIndex(key, _lastKey);
        if (bucket == 0)
        {
            _minimum.Enqueue(entry);
        }
        else
        {
            _buckets[bucket].Add(entry);
        }

        Count++;
    }

    public bool TryDequeue(out int item, out long priority)
    {
        if (Count == 0)
        {
            item = -1;
            priority = 0;
            return false;
        }

        if (_minimum.Count == 0)
        {
            RefillMinimum();
        }

        var entry = _minimum.Dequeue();
        Count--;
        item = entry.Item;
        priority = SignedPriority(entry.Key);
        return true;
    }

    private void RefillMinimum()
    {
        var sourceIndex = 1;
        while (_buckets[sourceIndex].Count == 0)
        {
            sourceIndex++;
        }

        var source = _buckets[sourceIndex];
        _lastKey = source.Min(static entry => entry.Key);
        foreach (var entry in source)
        {
            var destination = BucketIndex(entry.Key, _lastKey);
            if (destination == 0)
            {
                _minimum.Enqueue(entry);
            }
            else
            {
                _buckets[destination].Add(entry);
            }
        }

        source.Clear();
    }

    private static int BucketIndex(ulong key, ulong lastKey) => key == lastKey
        ? 0
        : 64 - BitOperations.LeadingZeroCount(key ^ lastKey);

    private static ulong OrderedKey(long priority) => unchecked((ulong)(priority ^ long.MinValue));

    private static long SignedPriority(ulong key) => unchecked((long)key ^ long.MinValue);

    private readonly record struct Entry(int Item, ulong Key);
}
