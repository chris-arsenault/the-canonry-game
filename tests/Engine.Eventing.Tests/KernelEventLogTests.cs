namespace Engine.Eventing.Tests;

using Engine.Eventing;
using Engine.Eventing.Schema;
using Engine.Kernel;

public sealed class KernelEventLogTests
{
    [Fact]
    public void ValidatedLogRejectsEventsWithoutRegisteredSchema()
    {
        var log = new KernelEventLog(
            capacity: 4,
            schemaValidator: KernelEventSchemaRegistry.Validator);

        Assert.False(log.TryWrite(default));
        Assert.Equal(1, log.RejectedWriteCount);
        Assert.Equal(0, log.NextSequence);
    }

    [Fact]
    public void KernelSimulationPassesConfiguredSchemaValidatorToEventLog()
    {
        var simulation = new Simulation(new SimulationOptions
        {
            EventCapacity = 4,
            EventSchemaValidator = KernelEventSchemaRegistry.Validator,
        });

        Assert.False(simulation.Events.TryWrite(default));
        Assert.Equal(1, simulation.Events.RejectedWriteCount);
        Assert.Equal(0, simulation.Events.NextSequence);
    }

    [Fact]
    public void ValidatedLogAcceptsRegisteredEvents()
    {
        var log = new KernelEventLog(
            capacity: 4,
            schemaValidator: KernelEventSchemaRegistry.Validator);
        var cursor = log.CreateCursor(KernelEventCursorStart.EarliestAvailable);

        var kernelEvent = KernelEvent.From(new GoalScoredKernelEvent(
            Tick: 7,
            WorldVersion: 13,
            PlanId: 17,
            EntityId: 19,
            GoalId: 23,
            ReasonCodeId: 29,
            ScoringPassIndex: 31,
            ScoreDelta: 3.5f,
            RunningTotal: 8.25f));

        Assert.True(log.TryWrite(kernelEvent));
        Span<SequencedKernelEvent> destination = stackalloc SequencedKernelEvent[1];
        var result = log.ReadWithResult(cursor, destination);

        Assert.Equal(1, result.Count);
        Assert.Equal(0, result.DroppedEventCount);
        Assert.Equal(1, result.NextSequence);
        Assert.Equal(kernelEvent, destination[0].Event);
    }

    [Fact]
    public void ReadWithResultReportsCursorDropDeltaAndNextSequence()
    {
        var log = new KernelEventLog(capacity: 1);
        var cursor = log.CreateCursor(KernelEventCursorStart.EarliestAvailable);

        Assert.True(log.TryWrite(KernelEvent.From(new PlanStartedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            PlanId: 10,
            EntityId: 20,
            ReasonCodeId: 30,
            CandidateCount: 2))));
        Assert.True(log.TryWrite(KernelEvent.From(new PlanCommittedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            PlanId: 10,
            EntityId: 20,
            StepCount: 3,
            ReasonCodeId: 40,
            TotalScore: 5f))));

        Span<SequencedKernelEvent> destination = stackalloc SequencedKernelEvent[1];
        var result = log.ReadWithResult(cursor, destination);

        Assert.Equal(1, result.Count);
        Assert.Equal(1, result.DroppedEventCount);
        Assert.Equal(2, result.NextSequence);
        Assert.Equal(1, cursor.DroppedEvents);
        Assert.Equal(1, destination[0].Sequence);
        Assert.Equal(KernelEventType.PlanCommitted, destination[0].Event.EventType);
    }
}
