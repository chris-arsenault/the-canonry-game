namespace Engine.Kernel.Driver.Tests;

using Engine.Eventing;
using Engine.Kernel;

public sealed class KernelAdvanceTests
{
    [Fact]
    public void AdvanceTicksAdvancesRequestedCount()
    {
        var simulation = new Simulation();

        var advanced = simulation.AdvanceTicks(5);

        Assert.Equal(5, advanced);
        Assert.Equal(5, simulation.CurrentTick);
        Assert.Equal(simulation.WorldVersion, simulation.CurrentVersion);
    }

    [Fact]
    public void AdvanceTicksRunsLargeBatchWithoutFrameClock()
    {
        var simulation = new Simulation();

        var advanced = simulation.AdvanceTicks(100);

        Assert.Equal(100, advanced);
        Assert.Equal(100, simulation.CurrentTick);
    }

    [Fact]
    public void AdvanceTicksRejectsNegativeCount()
    {
        var simulation = new Simulation();

        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.AdvanceTicks(-1));
    }

    [Fact]
    public void AdvanceTicksStopsBetweenTicksWhenPredicateFires()
    {
        var simulation = new Simulation();

        var advanced = simulation.AdvanceTicks(10, new AdvanceOptions(StopAtThree));

        Assert.Equal(3, advanced);
        Assert.Equal(3, simulation.CurrentTick);
    }

    [Fact]
    public void AdvanceTicksReturnsPartialCountWhenCancellationAlreadyRequested()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var simulation = new Simulation();

        var advanced = simulation.AdvanceTicks(10, new AdvanceOptions(Cancel: cancellation.Token));

        Assert.Equal(0, advanced);
        Assert.Equal(0, simulation.CurrentTick);
    }

    [Fact]
    public void AdvanceTicksEmitsTickCompletedEventForProjectedTime()
    {
        var simulation = new Simulation();

        _ = simulation.AdvanceTicks(1);

        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var found = false;
        var sequenced = default(SequencedKernelEvent);
        while (simulation.Events.TryRead(cursor, out var candidate))
        {
            if (candidate.Event.EventType != KernelEventType.TickCompleted)
            {
                continue;
            }

            found = true;
            sequenced = candidate;
            break;
        }

        Assert.True(found);
        var tick = sequenced.Event.AsTickCompleted();
        Assert.Equal(1, tick.Tick);
        Assert.Equal(simulation.WorldVersion, tick.WorldVersion);
    }

    private static bool StopAtThree(in KernelStopContext context) => context.CurrentTick >= 3;
}
