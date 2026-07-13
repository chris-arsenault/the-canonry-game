namespace Engine.Kernel;

internal sealed class InlineKernelWorkScheduler : IKernelWorkScheduler
{
    internal static readonly InlineKernelWorkScheduler Instance = new();

    private InlineKernelWorkScheduler()
    {
    }

    public int WorkerCount => 1;

    public void Run(
        IKernelParallelJob job,
        int startInclusive,
        int endExclusive,
        int minimumChunkSize)
    {
        global::System.ArgumentNullException.ThrowIfNull(job);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(startInclusive);
        global::System.ArgumentOutOfRangeException.ThrowIfLessThan(endExclusive, startInclusive);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumChunkSize);
        if (startInclusive < endExclusive)
        {
            job.Execute(0, startInclusive, endExclusive);
        }
    }
}
