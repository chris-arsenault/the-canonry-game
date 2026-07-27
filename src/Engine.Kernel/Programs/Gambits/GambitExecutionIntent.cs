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
    KernelEventState State);
