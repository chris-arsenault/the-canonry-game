namespace Engine.Kernel.Driver;

public interface IKernelDriverTimeControl : IDriverControl, IDisposable
{
    DriverState State { get; }

    double TicksPerSecond { get; }

    bool IsPaused { get; }

    int CurrentTick { get; }

    int AdvanceSingleStep(int tickCount, CancellationToken cancellationToken = default);
}
