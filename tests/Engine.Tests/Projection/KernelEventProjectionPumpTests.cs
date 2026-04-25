namespace Engine.Tests.Projection;

using Engine.Eventing;
using Engine.Projection;

public sealed class KernelEventProjectionPumpTests
{
    [Fact]
    public void DrainAvailable_ProjectsEventsToEveryRegisteredProjection()
    {
        var log = new KernelEventLog(capacity: 8);
        var first = new RecordingProjection();
        var second = new RecordingProjection();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            [first, second],
            batchSize: 2);

        Assert.True(log.TryWrite(KernelEvent.From(new GoalGeneratedKernelEvent(
            Tick: 1,
            WorldVersion: 2,
            EntityId: 3,
            KindId: 4,
            GoalCount: 5,
            ActionableGoalCount: 6,
            State: KernelEventState.Succeeded))));
        Assert.True(log.TryWrite(KernelEvent.From(new GeneratorFiredKernelEvent(
            Tick: 2,
            WorldVersion: 3,
            GeneratorId: 10,
            TargetEntityId: 20,
            CreatedEntityCount: 1,
            CreatedRelationshipCount: 2))));

        pump.DrainAvailable();

        Assert.Equal([KernelEventType.GoalGenerated, KernelEventType.GeneratorFired], first.EventTypes);
        Assert.Equal([0L, 1L], first.Sequences);
        Assert.Equal(first.EventTypes, second.EventTypes);
        Assert.Equal(2, pump.NextSequence);
    }

    [Fact]
    public void DrainAvailable_NotifiesProjectionWhenCursorDropsEvents()
    {
        var log = new KernelEventLog(capacity: 1);
        var projection = new RecordingProjection();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new GeneratorEvaluatedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            GeneratorId: 2,
            StatusCode: 3,
            SelectedTargetEntityId: 4,
            CandidateCount: 5,
            FailedFireCount: 6))));
        Assert.True(log.TryWrite(KernelEvent.From(new GeneratorFiredKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            GeneratorId: 2,
            TargetEntityId: 4,
            CreatedEntityCount: 1,
            CreatedRelationshipCount: 0))));

        pump.DrainAvailable();

        Assert.Equal(1, projection.DroppedEvents);
        Assert.Equal([KernelEventType.GeneratorFired], projection.EventTypes);
        Assert.Equal(1, pump.DroppedEvents);
    }

    private sealed class RecordingProjection : IKernelEventProjection
    {
        private readonly List<KernelEventType> _eventTypes = [];
        private readonly List<long> _sequences = [];

        public IReadOnlyList<KernelEventType> EventTypes => _eventTypes;

        public IReadOnlyList<long> Sequences => _sequences;

        public long DroppedEvents { get; private set; }

        public void Project(in SequencedKernelEvent kernelEvent)
        {
            _sequences.Add(kernelEvent.Sequence);
            _eventTypes.Add(kernelEvent.Event.EventType);
        }

        public void OnEventsDropped(long droppedEventCount, long nextSequence)
        {
            DroppedEvents += droppedEventCount;
        }
    }
}
