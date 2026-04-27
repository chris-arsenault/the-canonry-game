namespace Engine.Kernel.Driver;

public interface IKernelDriverTimeControl : IDisposable
{
    DriverState State { get; }

    double TicksPerSecond { get; }

    bool IsPaused { get; }

    int CurrentTick { get; }

    void SetTickRate(double ticksPerSecond);

    void RequestPause();

    void RequestResume();

    void RequestSingleStep(int tickCount);

    int AdvanceSingleStep(int tickCount, CancellationToken cancellationToken = default);
}
