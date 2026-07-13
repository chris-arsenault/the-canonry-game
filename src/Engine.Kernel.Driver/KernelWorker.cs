namespace Engine.Kernel.Driver;

internal sealed class KernelWorker
{
    private readonly Thread _thread;

    internal KernelWorker(int workerIndex, Action<int> run)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workerIndex);
        ArgumentNullException.ThrowIfNull(run);
        _thread = new Thread(() => run(workerIndex))
        {
            IsBackground = true,
            Name = $"kernel-worker-{workerIndex}",
        };
        _thread.Start();
    }

    internal void Join() => _thread.Join();
}
