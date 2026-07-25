namespace Engine.Eventing.Tests.Eventing;

using Engine.Eventing;

public sealed class KernelEventStreamTests
{
    [Fact]
    public void CursorReadsEventsInWriteOrder()
    {
        var stream = new KernelEventLog(capacity: 4);
        var cursor = stream.CreateCursor(KernelEventCursorStart.EarliestAvailable);

        Assert.True(stream.TryWrite(KernelEvent.From(new GambitExecutedKernelEvent(
            Tick: 1,
            WorldVersion: 101,
            ActorId: 10,
            ActorKindId: 2,
            GambitId: 20,
            AffectedEntityId0: 30,
            AffectedEntityId1: 31,
            AffectedEntityCount: 2,
            State: KernelEventState.Succeeded))));
        Assert.True(stream.TryWrite(KernelEvent.From(new AttributeChangedKernelEvent(
            Tick: 2,
            WorldVersion: 102,
            EntityId: 11,
            AttributeId: 30,
            SourceCodeId: 7,
            OldValueUnits: KernelFixedPoint.AttributeUnits(1.5f),
            NewValueUnits: KernelFixedPoint.AttributeUnits(2.5f)))));

        Assert.True(stream.TryRead(cursor, out var first));
        Assert.Equal(0, first.Sequence);
        Assert.Equal(KernelEventType.GambitExecuted, first.Event.EventType);
        var firstPayload = first.Event.AsGambitExecuted();
        Assert.Equal(1, firstPayload.Tick);
        Assert.Equal(101, firstPayload.WorldVersion);
        Assert.Equal(10, firstPayload.ActorId);
        Assert.Equal(20, firstPayload.GambitId);
        Assert.Equal(2, firstPayload.AffectedEntityCount);
        Assert.Equal(KernelEventState.Succeeded, firstPayload.State);

        Assert.True(stream.TryRead(cursor, out var second));
        Assert.Equal(1, second.Sequence);
        Assert.Equal(KernelEventType.AttributeChanged, second.Event.EventType);
        var secondPayload = second.Event.AsAttributeChanged();
        Assert.Equal(2, secondPayload.Tick);
        Assert.Equal(102, secondPayload.WorldVersion);
        Assert.Equal(11, secondPayload.EntityId);
        Assert.Equal(30, secondPayload.AttributeId);
        Assert.Equal(KernelFixedPoint.AttributeUnits(1.5f), secondPayload.OldValueUnits);
        Assert.Equal(KernelFixedPoint.AttributeUnits(2.5f), secondPayload.NewValueUnits);

        Assert.False(stream.TryRead(cursor, out _));
    }

    [Fact]
    public void CursorsMaintainIndependentReadHeads()
    {
        var stream = new KernelEventLog(capacity: 4);
        var firstCursor = stream.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var secondCursor = stream.CreateCursor(KernelEventCursorStart.EarliestAvailable);

        stream.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            EntityId: 1,
            SourceCodeId: 9,
            KindId: 2,
            SubtypeId: 3,
            CultureId: 4,
            PhysicalQ: 5,
            PhysicalR: 6,
            SemanticQ: 7,
            SemanticR: 8,
            SemanticS: -15)));
        stream.TryWrite(KernelEvent.From(new EntityMovedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            EntityId: 1,
            SourceCodeId: 9,
            PlaneCode: PlaneCodes.Physical,
            FromQ: 5,
            FromR: 6,
            FromS: -11,
            ToQ: 6,
            ToR: 7,
            ToS: -13,
            FromZ: 0,
            ToZ: 0)));

        Assert.True(stream.TryRead(firstCursor, out var firstRead));
        Assert.Equal(0, firstRead.Sequence);

        Assert.True(stream.TryRead(secondCursor, out var secondRead));
        Assert.Equal(0, secondRead.Sequence);

        Assert.True(stream.TryRead(firstCursor, out var firstNext));
        Assert.Equal(1, firstNext.Sequence);

        Assert.True(stream.TryRead(secondCursor, out var secondNext));
        Assert.Equal(1, secondNext.Sequence);
    }

    [Fact]
    public void SlowCursorRecordsDroppedEvents()
    {
        var stream = new KernelEventLog(capacity: 2);
        var cursor = stream.CreateCursor(KernelEventCursorStart.EarliestAvailable);

        stream.TryWrite(KernelEvent.From(new GoalGeneratedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            EntityId: 1,
            KindId: 2,
            GoalCount: 3,
            ActionableGoalCount: 1,
            State: KernelEventState.None)));
        stream.TryWrite(KernelEvent.From(new EntityMovedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            EntityId: 1,
            SourceCodeId: 5,
            PlaneCode: PlaneCodes.Physical,
            FromQ: 0,
            FromR: 0,
            FromS: 0,
            ToQ: 1,
            ToR: 0,
            ToS: -1,
            FromZ: 0,
            ToZ: 0)));
        stream.TryWrite(KernelEvent.From(new AttributeChangedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            EntityId: 1,
            AttributeId: 4,
            SourceCodeId: 5,
            OldValueUnits: KernelFixedPoint.AttributeUnits(6f),
            NewValueUnits: KernelFixedPoint.AttributeUnits(7f))));

        Assert.True(stream.TryRead(cursor, out var firstAvailable));
        Assert.Equal(1, firstAvailable.Sequence);
        Assert.Equal(KernelEventType.EntityMoved, firstAvailable.Event.EventType);
        Assert.Equal(1, cursor.DroppedEvents);
        Assert.Equal(1, stream.OverwrittenSlotCount);

        Assert.True(stream.TryRead(cursor, out var secondAvailable));
        Assert.Equal(2, secondAvailable.Sequence);
        Assert.Equal(KernelEventType.AttributeChanged, secondAvailable.Event.EventType);
    }

    [Fact]
    public void LatestCursorStartsAfterExistingEvents()
    {
        var stream = new KernelEventLog(capacity: 4);
        stream.TryWrite(KernelEvent.From(new GeneratorFiredKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            GeneratorId: 2,
            TargetEntityId: 3,
            CreatedEntityCount: 4,
            CreatedRelationshipCount: 5)));

        var cursor = stream.CreateCursor(KernelEventCursorStart.Latest);
        Assert.False(stream.TryRead(cursor, out _));

        stream.TryWrite(KernelEvent.From(new EntityMovedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            EntityId: 1,
            SourceCodeId: 5,
            PlaneCode: PlaneCodes.Physical,
            FromQ: 0,
            FromR: 0,
            FromS: 0,
            ToQ: 1,
            ToR: 0,
            ToS: -1,
            FromZ: 0,
            ToZ: 0)));

        Assert.True(stream.TryRead(cursor, out var next));
        Assert.Equal(1, next.Sequence);
        Assert.Equal(KernelEventType.EntityMoved, next.Event.EventType);
    }

    [Fact]
    public void ReadCopiesAvailableBatchWithoutAllocatingDestination()
    {
        var stream = new KernelEventLog(capacity: 4);
        var cursor = stream.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        Span<SequencedKernelEvent> destination = stackalloc SequencedKernelEvent[2];

        stream.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            EntityId: 1,
            SourceCodeId: 9,
            KindId: 2,
            SubtypeId: 3,
            CultureId: 4,
            PhysicalQ: 5,
            PhysicalR: 6,
            SemanticQ: 7,
            SemanticR: 8,
            SemanticS: -15)));
        stream.TryWrite(KernelEvent.From(new EntityMovedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            EntityId: 1,
            SourceCodeId: 9,
            PlaneCode: PlaneCodes.Physical,
            FromQ: 5,
            FromR: 6,
            FromS: -11,
            ToQ: 6,
            ToR: 7,
            ToS: -13,
            FromZ: 0,
            ToZ: 0)));
        stream.TryWrite(KernelEvent.From(new AttributeChangedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            EntityId: 1,
            AttributeId: 4,
            SourceCodeId: 5,
            OldValueUnits: KernelFixedPoint.AttributeUnits(6f),
            NewValueUnits: KernelFixedPoint.AttributeUnits(7f))));

        var read = stream.Read(cursor, destination);

        Assert.Equal(2, read);
        Assert.Equal(0, destination[0].Sequence);
        Assert.Equal(1, destination[1].Sequence);
        Assert.Equal(2, cursor.NextSequence);
    }

    [Fact]
    public void TypedAccessorForWrongEventFails()
    {
        var kernelEvent = KernelEvent.From(new AttributeChangedKernelEvent(
            Tick: 1,
            WorldVersion: 2,
            EntityId: 3,
            AttributeId: 4,
            SourceCodeId: 6,
            OldValueUnits: KernelFixedPoint.AttributeUnits(7f),
            NewValueUnits: KernelFixedPoint.AttributeUnits(8f)));

        Assert.Throws<InvalidOperationException>(() => kernelEvent.AsRelationshipChanged());
    }

    [Fact]
    public void GambitExecutedEventRoundTripsPrimitivePayload()
    {
        var kernelEvent = KernelEvent.From(new GambitExecutedKernelEvent(
            Tick: 9,
            WorldVersion: 10,
            ActorId: 11,
            ActorKindId: 12,
            GambitId: 13,
            AffectedEntityId0: 14,
            AffectedEntityId1: 0,
            AffectedEntityCount: 1,
            State: KernelEventState.Succeeded));

        Assert.Equal(KernelEventType.GambitExecuted, kernelEvent.EventType);
        var payload = kernelEvent.AsGambitExecuted();
        Assert.Equal(9, payload.Tick);
        Assert.Equal(10, payload.WorldVersion);
        Assert.Equal(11, payload.ActorId);
        Assert.Equal(12, payload.ActorKindId);
        Assert.Equal(13, payload.GambitId);
        Assert.Equal(14, payload.AffectedEntityId0);
        Assert.Equal(1, payload.AffectedEntityCount);
        Assert.Equal(KernelEventState.Succeeded, payload.State);
    }

    [Fact]
    public void RejectWritePolicyFailsInsteadOfOverwritingUnreadCursor()
    {
        var stream = new KernelEventLog(capacity: 2, KernelEventOverflowPolicy.RejectWrite);
        var cursor = stream.CreateCursor(KernelEventCursorStart.EarliestAvailable);

        Assert.True(stream.TryWrite(KernelEvent.From(new GoalGeneratedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            EntityId: 1,
            KindId: 2,
            GoalCount: 3,
            ActionableGoalCount: 1,
            State: KernelEventState.None))));
        Assert.True(stream.TryWrite(KernelEvent.From(new GoalGeneratedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            EntityId: 1,
            KindId: 2,
            GoalCount: 4,
            ActionableGoalCount: 2,
            State: KernelEventState.None))));

        Assert.False(stream.TryWrite(KernelEvent.From(new GoalGeneratedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            EntityId: 1,
            KindId: 2,
            GoalCount: 5,
            ActionableGoalCount: 3,
            State: KernelEventState.None))));
        Assert.Equal(1, stream.RejectedWriteCount);
        Assert.Equal(2, stream.NextSequence);

        Assert.True(stream.TryRead(cursor, out var first));
        Assert.Equal(0, first.Sequence);
        Assert.Equal(1, first.Event.AsGoalGenerated().Tick);

        Assert.True(stream.TryWrite(KernelEvent.From(new GoalGeneratedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            EntityId: 1,
            KindId: 2,
            GoalCount: 5,
            ActionableGoalCount: 3,
            State: KernelEventState.None))));
        Assert.Equal(3, stream.NextSequence);
    }

    [Fact]
    public void DisposedCursorNoLongerParticipatesInRejectWriteBackpressure()
    {
        var stream = new KernelEventLog(capacity: 1, KernelEventOverflowPolicy.RejectWrite);
        var cursor = stream.CreateCursor(KernelEventCursorStart.EarliestAvailable);

        Assert.True(stream.TryWrite(KernelEvent.From(new GeneratorEvaluatedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            GeneratorId: 2,
            StatusCode: GeneratorStatusCodes.EligibleNotSelected,
            SelectedTargetEntityId: 4,
            CandidateCount: 5,
            FailedFireCount: 6))));
        Assert.False(stream.TryWrite(KernelEvent.From(new GeneratorEvaluatedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            GeneratorId: 2,
            StatusCode: GeneratorStatusCodes.EligibleNotSelected,
            SelectedTargetEntityId: 4,
            CandidateCount: 5,
            FailedFireCount: 6))));

        cursor.Dispose();

        Assert.True(stream.TryWrite(KernelEvent.From(new GeneratorEvaluatedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            GeneratorId: 2,
            StatusCode: GeneratorStatusCodes.EligibleNotSelected,
            SelectedTargetEntityId: 4,
            CandidateCount: 5,
            FailedFireCount: 6))));
        Assert.Throws<ObjectDisposedException>(() => stream.TryRead(cursor, out _));
    }
}
