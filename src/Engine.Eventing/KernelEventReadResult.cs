namespace Engine.Eventing;

public readonly record struct KernelEventReadResult(
    int Count,
    long DroppedEventCount,
    long NextSequence);
