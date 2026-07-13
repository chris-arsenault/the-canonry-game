namespace Engine.Kernel;

internal sealed partial class Simulation
{
    [global::System.ThreadStatic]
    private static KernelRandomCursor _parallelRandom;
    [global::System.ThreadStatic]
    private static bool _parallelRandomActive;
    private IKernelWorkScheduler _workScheduler = InlineKernelWorkScheduler.Instance;

    internal IKernelWorkScheduler WorkScheduler => _workScheduler;

    internal void AttachWorkScheduler(IKernelWorkScheduler workScheduler)
    {
        global::System.ArgumentNullException.ThrowIfNull(workScheduler);
        if (workScheduler.WorkerCount > Options.ParallelWorkerCount)
        {
            throw new global::System.InvalidOperationException();
        }

        _workScheduler = workScheduler;
    }

    internal KernelRandomCursor CreateRandomCursor(
        int phase,
        int programId,
        int subjectId,
        int purposeId) =>
        KernelRandomCursor.Create(
            Options.Seed,
            CurrentTick,
            phase,
            programId,
            subjectId,
            purposeId);

    internal static void BeginParallelRandom(
        int seed,
        int tick,
        int phase,
        int programId,
        int subjectId,
        int purposeId)
    {
        if (_parallelRandomActive)
        {
            throw new global::System.InvalidOperationException();
        }

        _parallelRandom = KernelRandomCursor.Create(
            seed,
            tick,
            phase,
            programId,
            subjectId,
            purposeId);
        _parallelRandomActive = true;
    }

    internal static void EndParallelRandom()
    {
        _parallelRandomActive = false;
        _parallelRandom = default;
    }
}
