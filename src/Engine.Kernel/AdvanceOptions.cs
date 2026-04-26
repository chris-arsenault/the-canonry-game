namespace Engine.Kernel;

public delegate bool StopPredicate(in KernelStopContext context);

public readonly record struct AdvanceOptions(StopPredicate? StopAt = null, global::System.Threading.CancellationToken Cancel = default);

public readonly record struct KernelStopContext(int CurrentTick, long CurrentVersion, int AdvancedTicks);
