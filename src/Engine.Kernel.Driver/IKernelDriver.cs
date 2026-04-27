namespace Engine.Kernel.Driver;

internal interface IKernelDriver : IDisposable
{
    DriverState State { get; }

    Task RunAsync(CancellationToken cancellationToken);

    void RequestStop();
}

internal interface IDriverControl
{
    void SetTickRate(double ticksPerSecond);

    void RequestPause();

    void RequestResume();

    void RequestSingleStep(int tickCount);
}
