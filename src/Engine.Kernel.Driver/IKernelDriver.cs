namespace Engine.Kernel.Driver;

public interface IKernelDriver : IDisposable
{
    DriverState State { get; }

    Task RunAsync(CancellationToken cancellationToken);

    void RequestStop();
}

public interface IDriverControl
{
    void SetTickRate(double ticksPerSecond);

    void RequestPause();

    void RequestResume();

    void RequestSingleStep(int tickCount);
}
