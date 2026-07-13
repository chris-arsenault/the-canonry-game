namespace Engine.Kernel.Driver.Tests;

using Engine.Kernel;
using Engine.Kernel.Driver;

public sealed class FixedKernelWorkerPoolTests
{
    [Fact]
    public void RunProcessesEveryIndexAcrossBoundedWorkers()
    {
        using var pool = new FixedKernelWorkerPool(4);
        var values = new int[257];
        var job = new MarkingJob(values);

        pool.Run(job, 0, values.Length, minimumChunkSize: 7);

        Assert.All(values, value => Assert.InRange(value, 1, 4));
        Assert.True(values.Distinct().Count() > 1);
    }

    [Fact]
    public void RunPropagatesWorkerFailureAndRemainsReusable()
    {
        using var pool = new FixedKernelWorkerPool(4);
        var failing = new FailingJob(failingIndex: 63);

        Assert.Throws<InvalidOperationException>(() =>
            pool.Run(failing, 0, 128, minimumChunkSize: 8));

        var values = new int[128];
        pool.Run(new MarkingJob(values), 0, values.Length, minimumChunkSize: 8);
        Assert.All(values, value => Assert.InRange(value, 1, 4));
    }

    [Fact]
    public void WorkerCountCannotExceedKernelLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FixedKernelWorkerPool(SimulationOptions.MaximumParallelWorkerCount + 1));
    }

    private sealed class MarkingJob(int[] values) : IKernelParallelJob
    {
        public void Execute(int workerIndex, int startInclusive, int endExclusive)
        {
            for (var index = startInclusive; index < endExclusive; index++)
            {
                values[index] = workerIndex + 1;
            }
        }
    }

    private sealed class FailingJob(int failingIndex) : IKernelParallelJob
    {
        public void Execute(int workerIndex, int startInclusive, int endExclusive)
        {
            _ = workerIndex;
            for (var index = startInclusive; index < endExclusive; index++)
            {
                if (index == failingIndex)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
