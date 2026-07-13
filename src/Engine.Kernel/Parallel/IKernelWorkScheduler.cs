namespace Engine.Kernel;

internal interface IKernelWorkScheduler
{
    int WorkerCount { get; }

    void Run(
        IKernelParallelJob job,
        int startInclusive,
        int endExclusive,
        int minimumChunkSize);
}
