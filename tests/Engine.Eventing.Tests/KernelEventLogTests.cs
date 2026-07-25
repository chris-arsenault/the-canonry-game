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

        var kernelEvent = KernelEvent.From(new TraceKernelEvent(
            Tick: 7,
            WorldVersion: 13,
            IntentId: 17,
            EntityId: 19,
            TraceSubjectCode: TraceSubjectCodes.ActionSummary,
            CandidateIndex: 23,
            ParentCandidateIndex: 0,
            ActionFamilyCode: ActionFamilyCodes.LowAltitudeVerb,
            ActionId: 29,
            EffectScopeCode: 31,
            EffectValueId: 37,
            EffectKindId: 41,
            TargetEntityId: 43,
            StatusCode: TraceStatusCodes.Selected,
            AffordanceSourceKindCode: 0,
            AffordanceSourceId: 0,
            AffordanceShapeCode: 0,
            AffordanceSourceKindMask: 0,
            AffordanceShapeMask: 0,
            AffordanceContributionCount: 0,
            GeneratorAffordanceCount: 0,
            GambitAffordanceCount: 0,
            HiddenAffordanceCount: 0,
            SelectionBucketCode: 0,
            UrgencyUnits: FixedScalar.FromSingle(3.5f).Units,
            RewardOrBaseScoreUnits: FixedScalar.FromSingle(8.25f).Units,
            RawWeightUnits: FixedScalar.FromSingle(1.5f).Units,
            AdjustedWeightUnits: FixedScalar.FromSingle(2.5f).Units,
            AffordanceScoreUnits: FixedScalar.Zero.Units,
            AffordanceBonusUnits: FixedScalar.Zero.Units));

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

        Assert.True(log.TryWrite(KernelEvent.From(new RelationshipChangedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            ChangeCodeId: 1,
            SourceCodeId: 10,
            RelationshipKindId: 20,
            PolarityCode: RelationshipPolarityCodes.Positive,
            SourceEntityId: 30,
            TargetEntityId: 40,
            RelationshipId: 50,
            PreviousStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0f),
            NewStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0.5f)))));
        Assert.True(log.TryWrite(KernelEvent.From(new TraceKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            IntentId: 10,
            EntityId: 20,
            TraceSubjectCode: TraceSubjectCodes.Goal,
            CandidateIndex: 0,
            ParentCandidateIndex: 0,
            ActionFamilyCode: ActionFamilyCodes.LowAltitudeVerb,
            ActionId: 30,
            EffectScopeCode: 0,
            EffectValueId: 0,
            EffectKindId: 0,
            TargetEntityId: 40,
            StatusCode: TraceStatusCodes.Selected,
            AffordanceSourceKindCode: 0,
            AffordanceSourceId: 0,
            AffordanceShapeCode: 0,
            AffordanceSourceKindMask: 0,
            AffordanceShapeMask: 0,
            AffordanceContributionCount: 0,
            GeneratorAffordanceCount: 0,
            GambitAffordanceCount: 0,
            HiddenAffordanceCount: 0,
            SelectionBucketCode: 0,
            UrgencyUnits: FixedScalar.Zero.Units,
            RewardOrBaseScoreUnits: FixedScalar.FromInt(5).Units,
            RawWeightUnits: FixedScalar.FromInt(5).Units,
            AdjustedWeightUnits: FixedScalar.FromInt(5).Units,
            AffordanceScoreUnits: FixedScalar.Zero.Units,
            AffordanceBonusUnits: FixedScalar.Zero.Units))));

        Span<SequencedKernelEvent> destination = stackalloc SequencedKernelEvent[1];
        var result = log.ReadWithResult(cursor, destination);

        Assert.Equal(1, result.Count);
        Assert.Equal(1, result.DroppedEventCount);
        Assert.Equal(2, result.NextSequence);
        Assert.Equal(1, cursor.DroppedEvents);
        Assert.Equal(1, destination[0].Sequence);
        Assert.Equal(KernelEventType.Trace, destination[0].Event.EventType);
    }
}
