namespace TheCanonryGame.Game.Runtime;

using Engine.Kernel.Driver;

internal sealed class GameRuntimeControls
{
    private readonly IKernelDriverTimeControl _time;

    internal GameRuntimeControls(IKernelDriverTimeControl time)
    {
        ArgumentNullException.ThrowIfNull(time);
        _time = time;
    }

    public bool IsPaused => _time.IsPaused;

    public double TicksPerSecond => _time.TicksPerSecond;

    public int CurrentTick => _time.PublishedLowWatermark;

    public void Pause() => _time.RequestPause();

    public void Resume() => _time.RequestResume();

    public void Step(int ticks) => _time.RequestSingleStep(ticks);

    public void SetTickRate(double ticksPerSecond) => _time.SetTickRate(ticksPerSecond);
}
