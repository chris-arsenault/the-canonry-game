namespace Engine.Kernel.Driver;

public sealed record RealtimeDriverConfig(
    double TicksPerSecond,
    int MaxTicksPerSlice,
    TimeSpan SliceInterval);
