namespace Engine.Kernel.Driver;

internal sealed record RealtimeDriverConfig(
    double TicksPerSecond,
    int MaxTicksPerSlice,
    TimeSpan SliceInterval);
