namespace Engine.Kernel.Driver.Tests;

using Engine.Kernel;

public sealed class KernelSchedulingTests
{
    [Fact]
    public void AdvanceTicksWithZeroCountDoesNotAdvanceOrEmitTicks()
    {
        var simulation = new Simulation();

        var advanced = simulation.AdvanceTicks(0);

        Assert.Equal(0, advanced);
        Assert.Equal(0, simulation.CurrentTick);
        using var cursor = simulation.Events.CreateCursor(Engine.Eventing.KernelEventCursorStart.EarliestAvailable);
        Assert.False(simulation.Events.TryRead(cursor, out _));
    }

    [Fact]
    public void CadenceSystemRunsEveryNTicks()
    {
        var simulation = new Simulation();
        var ticks = new List<int>();
        _ = simulation.RegisterSystem(
            cadence: 5,
            new RecordingSystem((_, tick) => ticks.Add(tick)));

        var advanced = simulation.AdvanceTicks(12);

        Assert.Equal(12, advanced);
        Assert.Equal([5, 10], ticks);
    }

    [Fact]
    public void CadenceOneRunsEveryTick()
    {
        var simulation = new Simulation();
        var ticks = new List<int>();
        _ = simulation.RegisterSystem(
            cadence: 1,
            new RecordingSystem((_, tick) => ticks.Add(tick)));

        _ = simulation.AdvanceTicks(5);

        Assert.Equal([1, 2, 3, 4, 5], ticks);
    }

    [Fact]
    public void RegisterSystemRejectsCadenceZero()
    {
        var simulation = new Simulation();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            simulation.RegisterSystem(cadence: 0, new RecordingSystem((_, _) => { })));
    }

    [Fact]
    public void RegisteredSystemsRunInRegistrationOrder()
    {
        var simulation = new Simulation();
        var executionOrder = new List<int>();
        _ = simulation.RegisterSystem(
            cadence: 1,
            new RecordingSystem((_, _) => executionOrder.Add(1)));
        _ = simulation.RegisterSystem(
            cadence: 1,
            new RecordingSystem((_, _) => executionOrder.Add(2)));
        _ = simulation.RegisterSystem(
            cadence: 1,
            new RecordingSystem((_, _) => executionOrder.Add(3)));
        _ = simulation.RegisterSystem(
            cadence: 1,
            new RecordingSystem((_, _) => executionOrder.Add(4)));
        _ = simulation.RegisterSystem(
            cadence: 1,
            new RecordingSystem((_, _) => executionOrder.Add(5)));

        _ = simulation.AdvanceTicks(1);

        Assert.Equal([1, 2, 3, 4, 5], executionOrder);
    }

    [Fact]
    public void EarlierRegisteredWritesAreVisibleToLaterRegisteredSystemsInSameTick()
    {
        var simulation = new Simulation();
        var value = 0;
        var laterSaw = -1;
        _ = simulation.RegisterSystem(
            cadence: 1,
            new RecordingSystem((_, _) => value = 42));
        _ = simulation.RegisterSystem(
            cadence: 1,
            new RecordingSystem((_, _) => laterSaw = value));

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(42, laterSaw);
    }

    [Fact]
    public void LaterRegisteredWritesAreNotVisibleToEarlierRegisteredSystemsUntilNextTick()
    {
        var simulation = new Simulation();
        var value = 0;
        var earlierSaw = new List<int>();
        var laterSaw = new List<int>();
        _ = simulation.RegisterSystem(
            cadence: 1,
            new RecordingSystem((_, _) =>
            {
                earlierSaw.Add(value);
                value = 42;
            }));
        _ = simulation.RegisterSystem(
            cadence: 1,
            new RecordingSystem((_, _) =>
            {
                laterSaw.Add(value);
                value = 99;
            }));

        _ = simulation.AdvanceTicks(2);

        Assert.Equal([0, 99], earlierSaw);
        Assert.Equal([42, 42], laterSaw);
    }

    [Fact]
    public void UnregisterSystemStopsFutureExecution()
    {
        var simulation = new Simulation();
        var runCount = 0;
        var handle = simulation.RegisterSystem(
            cadence: 1,
            new RecordingSystem((_, _) => runCount++));

        _ = simulation.AdvanceTicks(1);
        simulation.UnregisterSystem(handle);
        _ = simulation.AdvanceTicks(2);

        Assert.Equal(1, runCount);
    }

    [Fact]
    public void ProgramCanBeDisabledAndReEnabled()
    {
        var simulation = new Simulation();
        const int testProgram = KernelProgramCodes.Planning;
        var runCount = 0;
        _ = simulation.RegisterProgram(
            testProgram,
            cadence: 1,
            new RecordingSystem((_, _) => runCount++));

        _ = simulation.AdvanceTicks(1);
        simulation.SetProgramEnabled(testProgram, enabled: false);
        _ = simulation.AdvanceTicks(2);
        simulation.SetProgramEnabled(testProgram, enabled: true);
        _ = simulation.AdvanceTicks(1);

        Assert.Equal(2, runCount);
    }

    [Fact]
    public void SystemCallbackReceivesCurrentTick()
    {
        var simulation = new Simulation();
        var receivedTicks = new List<int>();
        _ = simulation.RegisterSystem(
            cadence: 1,
            new RecordingSystem((_, tick) => receivedTicks.Add(tick)));

        _ = simulation.AdvanceTicks(3);

        Assert.Equal([1, 2, 3], receivedTicks);
    }

    private sealed class RecordingSystem(Action<Simulation, int> onTick) : ISimulationSystem
    {
        public void Tick(Simulation simulation, int tick) => onTick(simulation, tick);
    }
}
