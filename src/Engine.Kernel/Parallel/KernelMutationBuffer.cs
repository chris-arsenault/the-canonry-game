namespace Engine.Kernel;

internal sealed class KernelMutationBuffer
{
    private readonly KernelMutation[] _mutations;
    private readonly int[] _counts;
    private readonly int[] _overflowed;
    private readonly int _capacityPerWorker;

    internal KernelMutationBuffer(int workerCount, int capacityPerWorker)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfLessThan(workerCount, 1);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThan(
            workerCount,
            SimulationOptions.MaximumParallelWorkerCount);
        global::System.ArgumentOutOfRangeException.ThrowIfLessThan(capacityPerWorker, 1);
        _capacityPerWorker = capacityPerWorker;
        _mutations = new KernelMutation[checked(workerCount * capacityPerWorker)];
        _counts = new int[workerCount];
        _overflowed = new int[workerCount];
    }

    internal int WorkerCount => _counts.Length;

    internal void Reset()
    {
        global::System.Array.Clear(_counts);
        global::System.Array.Clear(_overflowed);
    }

    internal bool TryAppend(int workerIndex, in KernelMutation mutation)
    {
        EnsureWorker(workerIndex);
        var count = _counts[workerIndex];
        if (count >= _capacityPerWorker)
        {
            _overflowed[workerIndex] = 1;
            return false;
        }

        _mutations[(workerIndex * _capacityPerWorker) + count] = mutation;
        _counts[workerIndex] = count + 1;
        return true;
    }

    internal int Count(int workerIndex)
    {
        EnsureWorker(workerIndex);
        return _counts[workerIndex];
    }

    internal bool Overflowed(int workerIndex)
    {
        EnsureWorker(workerIndex);
        return _overflowed[workerIndex] != 0;
    }

    internal KernelMutation At(int workerIndex, int index)
    {
        EnsureWorker(workerIndex);
        if ((uint)index >= (uint)_counts[workerIndex])
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(index));
        }

        return _mutations[(workerIndex * _capacityPerWorker) + index];
    }

    private void EnsureWorker(int workerIndex)
    {
        if ((uint)workerIndex >= (uint)_counts.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(workerIndex));
        }
    }
}
