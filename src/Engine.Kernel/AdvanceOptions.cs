namespace Engine.Kernel;

internal delegate bool StopPredicate(in KernelStopContext context);

internal readonly record struct AdvanceOptions(StopPredicate? StopAt = null, global::System.Threading.CancellationToken Cancel = default);

internal readonly record struct KernelStopContext(int CurrentTick, long CurrentVersion, int AdvancedTicks);
