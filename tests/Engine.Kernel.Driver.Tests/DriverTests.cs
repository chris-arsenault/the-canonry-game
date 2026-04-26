namespace Engine.Kernel.Driver.Tests;

using Engine.Kernel;
using Engine.Kernel.Driver;

public sealed class DriverTests
{
    [Fact]
    public async Task UnpacedDriverAdvancesUntilStopPredicate()
    {
        var simulation = new Simulation();
        using var driver = new UnpacedKernelDriver(simulation, batchSize: 8, stopAt: StopAtTwentyFive);

        var run = driver.RunAsync(CancellationToken.None);
        await run.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(true);

        Assert.Equal(DriverState.Stopped, driver.State);
        Assert.Equal(25, simulation.CurrentTick);
    }

    [Fact]
    public void SteppedDriverAdvancesOnlyWhenStepIsRequested()
    {
        var simulation = new Simulation();
        using var driver = new SteppedKernelDriver(simulation);

        Assert.Equal(0, simulation.CurrentTick);

        var advanced = driver.Step(3);

        Assert.Equal(3, advanced);
        Assert.Equal(3, simulation.CurrentTick);
        Assert.Equal(DriverState.Stopped, driver.State);
    }

    [Fact]
    public void RealtimeAccumulatorCapsCatchUpAndDropsDebt()
    {
        var accumulator = new RealtimeTickAccumulator(maxTicksPerSlice: 5);

        var first = accumulator.ConsumeTicks(TimeSpan.FromMilliseconds(100), ticksPerSecond: 10d);
        var second = accumulator.ConsumeTicks(TimeSpan.FromSeconds(10), ticksPerSecond: 10d);
        var third = accumulator.ConsumeTicks(TimeSpan.FromMilliseconds(100), ticksPerSecond: 10d);

        Assert.Equal(1, first);
        Assert.Equal(5, second);
        Assert.Equal(1, third);
        Assert.Equal(1, accumulator.OverrunCount);
    }

    [Fact]
    public void TimeControllerSingleStepAdvancesWhilePaused()
    {
        var kernel = new CountingKernel();
        using var time = new KernelDriverTimeController(
            kernel,
            new RealtimeDriverConfig(
                TicksPerSecond: 0d,
                MaxTicksPerSlice: 4,
                SliceInterval: TimeSpan.FromMilliseconds(1)));

        var advanced = time.AdvanceSingleStep(3);

        Assert.Equal(3, advanced);
        Assert.Equal(3, kernel.CurrentTick);
        Assert.True(time.IsPaused);
        Assert.Equal(DriverState.Stopped, time.State);
    }

    [Fact]
    public void TimeControllerRateStartsRealtimeDriverAndPauseStopsIt()
    {
        var kernel = new CountingKernel();
        using var time = new KernelDriverTimeController(
            kernel,
            new RealtimeDriverConfig(
                TicksPerSecond: 0d,
                MaxTicksPerSlice: 8,
                SliceInterval: TimeSpan.FromMilliseconds(1)));

        time.SetTickRate(1000d);

        Assert.True(SpinWait.SpinUntil(() => kernel.CurrentTick > 0, TimeSpan.FromSeconds(2)));
        Assert.False(time.IsPaused);
        Assert.Equal(1000d, time.TicksPerSecond);

        time.RequestPause();
        var pausedTick = kernel.CurrentTick;
        Thread.Sleep(TimeSpan.FromMilliseconds(20));

        Assert.True(time.IsPaused);
        Assert.Equal(DriverState.Stopped, time.State);
        Assert.Equal(pausedTick, kernel.CurrentTick);
    }

    [Fact]
    public void TimeControllerZeroRatePausesAndSingleStepStillAdvances()
    {
        var kernel = new CountingKernel();
        using var time = new KernelDriverTimeController(
            kernel,
            new RealtimeDriverConfig(
                TicksPerSecond: 0d,
                MaxTicksPerSlice: 8,
                SliceInterval: TimeSpan.FromMilliseconds(1)));

        time.SetTickRate(1000d);
        Assert.True(SpinWait.SpinUntil(() => kernel.CurrentTick > 0, TimeSpan.FromSeconds(2)));

        time.SetTickRate(0d);
        var pausedTick = kernel.CurrentTick;
        Thread.Sleep(TimeSpan.FromMilliseconds(20));
        var stepped = time.AdvanceSingleStep(2);

        Assert.Equal(0d, time.TicksPerSecond);
        Assert.True(time.IsPaused);
        Assert.Equal(2, stepped);
        Assert.Equal(pausedTick + 2, kernel.CurrentTick);
    }

    [Fact]
    public void TimeControllerResumeRestartsWithRememberedRate()
    {
        var kernel = new CountingKernel();
        using var time = new KernelDriverTimeController(
            kernel,
            new RealtimeDriverConfig(
                TicksPerSecond: 0d,
                MaxTicksPerSlice: 8,
                SliceInterval: TimeSpan.FromMilliseconds(1)));

        time.SetTickRate(250d);
        Assert.True(SpinWait.SpinUntil(() => kernel.CurrentTick > 0, TimeSpan.FromSeconds(2)));
        time.RequestPause();
        var pausedTick = kernel.CurrentTick;

        time.RequestResume();

        Assert.True(SpinWait.SpinUntil(() => kernel.CurrentTick > pausedTick, TimeSpan.FromSeconds(2)));
        Assert.False(time.IsPaused);
        Assert.Equal(250d, time.TicksPerSecond);
    }

    private static bool StopAtTwentyFive(in KernelStopContext context) => context.CurrentTick >= 25;

    private sealed class CountingKernel : IKernel
    {
        private int _currentTick;

        public int CurrentTick => Volatile.Read(ref _currentTick);

        public long CurrentVersion => CurrentTick;

        public int AdvanceTicks(int count, in AdvanceOptions options = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            var advanced = 0;
            while (advanced < count && !options.Cancel.IsCancellationRequested)
            {
                var nextTick = Interlocked.Increment(ref _currentTick);
                advanced++;
                var context = new KernelStopContext(nextTick, CurrentVersion, advanced);
                if (options.StopAt is not null && options.StopAt(in context))
                {
                    break;
                }
            }

            return advanced;
        }
    }
}
