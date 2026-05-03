namespace Engine.Kernel;

#pragma warning disable CA1032
internal sealed class KernelCapacityExceededException : global::System.InvalidOperationException
#pragma warning restore CA1032
{
    public KernelCapacityExceededException(int capacityCode, int requestedIndex, int capacity)
    {
        CapacityCode = capacityCode;
        RequestedIndex = requestedIndex;
        Capacity = capacity;
    }

    public int CapacityCode { get; }

    public int RequestedIndex { get; }

    public int Capacity { get; }
}
