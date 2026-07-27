namespace Engine.Kernel;

internal sealed class KernelOwnerTopology
{
    internal const int DefaultPartitionCount = 64;

    private readonly int _entityCapacity;
    private readonly int _relationshipCapacity;

    internal KernelOwnerTopology(SimulationOptions options)
    {
        global::System.ArgumentNullException.ThrowIfNull(options);
        NumericGuard.ThrowIfNegativeOrZero(
            options.Execution.OwnerPartitionCount);
        _entityCapacity = options.EntityCapacity;
        _relationshipCapacity = options.RelationshipCapacity;
        PartitionCount = options.Execution.OwnerPartitionCount;
    }

    internal int PartitionCount { get; }

    internal int StructuralOwner => 0;

    internal int EntityOwner(int entityId) =>
        SlotOwner(entityId, _entityCapacity);

    internal int EntityOccurrenceOwner(int occurrenceHandle) =>
        SlotOwner(
            SlotFromOccurrence(occurrenceHandle, _entityCapacity),
            _entityCapacity);

    internal int RelationshipOwner(int relationshipId) =>
        SlotOwner(relationshipId, _relationshipCapacity);

    internal int RelationshipOccurrenceOwner(int occurrenceHandle) =>
        SlotOwner(
            SlotFromOccurrence(occurrenceHandle, _relationshipCapacity),
            _relationshipCapacity);

    internal int RelationshipCreationOwner(
        int sourceEntityId,
        int targetEntityId,
        int kindId)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            sourceEntityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            targetEntityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            kindId);
        var hash = unchecked((uint)sourceEntityId * 0x9E3779B1u);
        hash = unchecked((hash ^ (uint)targetEntityId) * 0x85EBCA77u);
        hash = unchecked((hash ^ (uint)kindId) * 0xC2B2AE3Du);
        hash ^= hash >> 16;
        return (int)(hash % (uint)PartitionCount);
    }

    internal int ProgramOwner(int programId)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            programId);
        var hash = unchecked((uint)programId * 0x9E3779B1u);
        hash ^= hash >> 16;
        return (int)(hash % (uint)PartitionCount);
    }

    internal int CultureOwner(int cultureId)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            cultureId);
        return (cultureId - 1) % PartitionCount;
    }

    internal int CellOwner(int cellId)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellId);
        return (cellId - 1) % PartitionCount;
    }

    internal int CellOwner(int planeCode, int q, int r, int z = 0)
    {
        var hash = unchecked((uint)planeCode * 0x9E3779B1u);
        hash = unchecked((hash ^ (uint)q) * 0x85EBCA77u);
        hash = unchecked((hash ^ (uint)r) * 0xC2B2AE3Du);
        hash = unchecked((hash ^ (uint)z) * 0x27D4EB2Fu);
        hash ^= hash >> 16;
        return (int)(hash % (uint)PartitionCount);
    }

    internal int FirstOwnerForWorker(int workerIndex, int workerCount)
    {
        ValidateWorker(workerIndex, workerCount);
        return (int)((long)PartitionCount * workerIndex / workerCount);
    }

    internal int OwnerLimitForWorker(int workerIndex, int workerCount)
    {
        ValidateWorker(workerIndex, workerCount);
        return (int)((long)PartitionCount * (workerIndex + 1) / workerCount);
    }

    private int SlotOwner(int slotId, int capacity)
    {
        if ((uint)slotId == 0u || slotId > capacity)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(slotId));
        }

        return (slotId - 1) % PartitionCount;
    }

    private static int SlotFromOccurrence(int occurrenceHandle, int capacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            occurrenceHandle);
        return ((occurrenceHandle - 1) % capacity) + 1;
    }

    private static void ValidateWorker(int workerIndex, int workerCount)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            workerCount);
        if ((uint)workerIndex >= (uint)workerCount)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(workerIndex));
        }
    }
}
