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
            TickPhaseCodes.Low,
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
            TickPhaseCodes.Low,
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
            simulation.RegisterSystem(TickPhaseCodes.Low, cadence: 0, new RecordingSystem((_, _) => { })));
    }

    [Fact]
    public void RegisteredSystemsRunInStrictPhaseOrder()
    {
        var simulation = new Simulation();
        var executionOrder = new List<int>();
        _ = simulation.RegisterSystem(
            TickPhaseCodes.Generator,
            cadence: 1,
            new RecordingSystem((_, _) => executionOrder.Add(TickPhaseCodes.Generator)));
        _ = simulation.RegisterSystem(
            TickPhaseCodes.Low,
            cadence: 1,
            new RecordingSystem((_, _) => executionOrder.Add(TickPhaseCodes.Low)));
        _ = simulation.RegisterSystem(
            TickPhaseCodes.Event,
            cadence: 1,
            new RecordingSystem((_, _) => executionOrder.Add(TickPhaseCodes.Event)));
        _ = simulation.RegisterSystem(
            TickPhaseCodes.Pressure,
            cadence: 1,
            new RecordingSystem((_, _) => executionOrder.Add(TickPhaseCodes.Pressure)));
        _ = simulation.RegisterSystem(
            TickPhaseCodes.Process,
            cadence: 1,
            new RecordingSystem((_, _) => executionOrder.Add(TickPhaseCodes.Process)));

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(
        [
            TickPhaseCodes.Low,
            TickPhaseCodes.Process,
            TickPhaseCodes.Pressure,
            TickPhaseCodes.Generator,
            TickPhaseCodes.Event,
        ], executionOrder);
    }

    [Fact]
    public void LowerPhaseWritesAreVisibleToLaterPhasesInSameTick()
    {
        var simulation = new Simulation();
        var value = 0;
        var pressureSaw = -1;
        _ = simulation.RegisterSystem(
            TickPhaseCodes.Low,
            cadence: 1,
            new RecordingSystem((_, _) => value = 42));
        _ = simulation.RegisterSystem(
            TickPhaseCodes.Pressure,
            cadence: 1,
            new RecordingSystem((_, _) => pressureSaw = value));

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(42, pressureSaw);
    }

    [Fact]
    public void LaterPhaseWritesAreNotVisibleToLowerPhaseUntilNextTick()
    {
        var simulation = new Simulation();
        var value = 0;
        var lowSaw = new List<int>();
        var generatorSaw = new List<int>();
        _ = simulation.RegisterSystem(
            TickPhaseCodes.Generator,
            cadence: 1,
            new RecordingSystem((_, _) =>
            {
                generatorSaw.Add(value);
                value = 99;
            }));
        _ = simulation.RegisterSystem(
            TickPhaseCodes.Low,
            cadence: 1,
            new RecordingSystem((_, _) =>
            {
                lowSaw.Add(value);
                value = 42;
            }));

        _ = simulation.AdvanceTicks(2);

        Assert.Equal([0, 99], lowSaw);
        Assert.Equal([42, 42], generatorSaw);
    }

    [Fact]
    public void UnregisterSystemStopsFutureExecution()
    {
        var simulation = new Simulation();
        var runCount = 0;
        var handle = simulation.RegisterSystem(
            TickPhaseCodes.Low,
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
        const int testProgram = 99;
        var runCount = 0;
        _ = simulation.RegisterProgram(
            testProgram,
            TickPhaseCodes.Low,
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
            TickPhaseCodes.Low,
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
