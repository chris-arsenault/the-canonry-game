namespace TheCanonryGame.Game.Tests;

using Engine.Kernel.Driver;

using TheCanonryGame.Game.Runtime;

public sealed class GameRuntimeControlsTests
{
    [Fact]
    public void Properties_ReflectCurrentDriverTimeState()
    {
        using var time = new RecordingTimeControl
        {
            IsPausedValue = true,
            TicksPerSecondValue = 3.5d,
            CurrentTickValue = 27,
        };
        var controls = new GameRuntimeControls(time);

        Assert.True(controls.IsPaused);
        Assert.Equal(3.5d, controls.TicksPerSecond);
        Assert.Equal(27, controls.CurrentTick);
    }

    [Fact]
    public void PauseAndResume_ForwardToDriverTimeControl()
    {
        using var time = new RecordingTimeControl();
        var controls = new GameRuntimeControls(time);

        controls.Pause();
        controls.Resume();

        Assert.Equal(1, time.PauseRequests);
        Assert.Equal(1, time.ResumeRequests);
    }

    [Fact]
    public void Step_ForwardsRequestedTickCount()
    {
        using var time = new RecordingTimeControl();
        var controls = new GameRuntimeControls(time);

        controls.Step(12);

        Assert.Equal([12], time.SingleStepRequests);
    }

    [Fact]
    public void SetTickRate_ForwardsRequestedRate()
    {
        using var time = new RecordingTimeControl();
        var controls = new GameRuntimeControls(time);

        controls.SetTickRate(0.25d);
        controls.SetTickRate(8d);

        Assert.Equal([0.25d, 8d], time.TickRates);
    }

    private sealed class RecordingTimeControl : IKernelDriverTimeControl
    {
        public DriverState State => DriverState.Running;

        public double TicksPerSecond => TicksPerSecondValue;

        public bool IsPaused => IsPausedValue;

        public int CurrentTick => CurrentTickValue;

        public bool IsPausedValue { get; init; }

        public double TicksPerSecondValue { get; init; }

        public int CurrentTickValue { get; init; }

        public int PauseRequests { get; private set; }

        public int ResumeRequests { get; private set; }

        public List<int> SingleStepRequests { get; } = [];

        public List<double> TickRates { get; } = [];

        public void SetTickRate(double ticksPerSecond) => TickRates.Add(ticksPerSecond);

        public void RequestPause() => PauseRequests++;

        public void RequestResume() => ResumeRequests++;

        public void RequestSingleStep(int tickCount) => SingleStepRequests.Add(tickCount);

        public int AdvanceSingleStep(int tickCount, CancellationToken cancellationToken = default) => tickCount;

        public void Dispose()
        {
        }
    }
}
