namespace Engine.Kernel;

internal interface IKernelParallelJob
{
    void Execute(int workerIndex, int startInclusive, int endExclusive);
}
