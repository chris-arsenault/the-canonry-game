namespace Engine.Kernel;

using Engine.Eventing;

internal readonly record struct GambitExecutionIntent(
    int EntityOccurrenceHandle,
    int TargetOccurrenceHandle,
    int EntityId,
    int TargetId,
    int GambitSlot,
    int KindId,
    int CandidateCount,
    KernelEventState State) : IDeterministicOrder<GambitExecutionIntent>
{
    public int CompareTo(GambitExecutionIntent other)
    {
        var comparison =
            EntityOccurrenceHandle.CompareTo(other.EntityOccurrenceHandle);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = GambitSlot.CompareTo(other.GambitSlot);
        return comparison != 0
            ? comparison
            : TargetOccurrenceHandle.CompareTo(other.TargetOccurrenceHandle);
    }
}
