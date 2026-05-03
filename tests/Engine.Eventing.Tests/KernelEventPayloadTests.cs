namespace Engine.Eventing.Tests;

using Engine.Eventing;

public sealed class KernelEventPayloadTests
{
    private const int InvalidPolarityCode = -1;

    [Fact]
    public void RawSlotAccessExposesPrimitiveEventStorage()
    {
        var kernelEvent = KernelEvent.From(new RelationshipChangedKernelEvent(
            Tick: 3,
            WorldVersion: 5,
            ChangeCodeId: 7,
            SourceCodeId: 41,
            RelationshipKindId: 11,
            PolarityCode: InvalidPolarityCode,
            SourceEntityId: 13,
            TargetEntityId: 17,
            RelationshipId: 19,
            PreviousStrength: 0.25f,
            NewStrength: 0.75f));

        Assert.Equal(7, kernelEvent.GetIntSlot(0));
        Assert.Equal(41, kernelEvent.GetIntSlot(1));
        Assert.Equal(11, kernelEvent.GetIntSlot(2));
        Assert.Equal(InvalidPolarityCode, kernelEvent.GetIntSlot(3));
        Assert.Equal(13, kernelEvent.GetIntSlot(4));
        Assert.Equal(17, kernelEvent.GetIntSlot(5));
        Assert.Equal(19, kernelEvent.GetIntSlot(6));
        Assert.Equal(0.25f, kernelEvent.GetFloatSlot(0));
        Assert.Equal(0.75f, kernelEvent.GetFloatSlot(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => kernelEvent.GetIntSlot(KernelEvent.IntSlotCount));
        Assert.Throws<ArgumentOutOfRangeException>(() => kernelEvent.GetFloatSlot(KernelEvent.FloatSlotCount));
    }

    [Fact]
    public void GeneratedPackAndViewRoundTripEveryRegisteredEvent()
    {
        AssertEntityAndChangeEventsRoundTrip();
        AssertRuntimeEventsRoundTrip();
        AssertPlanningEventsRoundTrip();
    }

    private static void AssertEntityAndChangeEventsRoundTrip()
    {
        var entityCreated = new EntityCreatedKernelEvent(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, -21);
        Assert.Equal(entityCreated, KernelEvent.From(entityCreated).AsEntityCreated());

        var entityStatusChanged = new EntityStatusChangedKernelEvent(2, 3, 4, 5, 6, 7, 8, 9);
        Assert.Equal(entityStatusChanged, KernelEvent.From(entityStatusChanged).AsEntityStatusChanged());

        var entityMoved = new EntityMovedKernelEvent(3, 4, 5, 6, PlaneCodes.Physical, 7, 8, -15, 9, 10, -19);
        Assert.Equal(entityMoved, KernelEvent.From(entityMoved).AsEntityMoved());

        var attributeChanged = new AttributeChangedKernelEvent(4, 5, 6, 7, 8, 1.25f, 2.5f);
        Assert.Equal(attributeChanged, KernelEvent.From(attributeChanged).AsAttributeChanged());

        var relationshipChanged = new RelationshipChangedKernelEvent(
            6, 7, 8, 9, 10, InvalidPolarityCode, 11, 12, 13, 0.125f, 0.875f);
        Assert.Equal(relationshipChanged, KernelEvent.From(relationshipChanged).AsRelationshipChanged());
    }

    private static void AssertRuntimeEventsRoundTrip()
    {
        var goalGenerated = new GoalGeneratedKernelEvent(9, 10, 11, 12, 13, 14, KernelEventState.Truncated);
        Assert.Equal(goalGenerated, KernelEvent.From(goalGenerated).AsGoalGenerated());

        var degeneracyDetected = new DegeneracyDetectedKernelEvent(10, 11, 12, 13, 14);
        Assert.Equal(degeneracyDetected, KernelEvent.From(degeneracyDetected).AsDegeneracyDetected());

        var generatorEvaluated = new GeneratorEvaluatedKernelEvent(11, 12, 13, 14, 15, 16, 17);
        Assert.Equal(generatorEvaluated, KernelEvent.From(generatorEvaluated).AsGeneratorEvaluated());

        var generatorFired = new GeneratorFiredKernelEvent(12, 13, 14, 15, 16, 17);
        Assert.Equal(generatorFired, KernelEvent.From(generatorFired).AsGeneratorFired());

        var gambitEvaluated = new GambitEvaluatedKernelEvent(13, 14, 15, 16, 17, 18, KernelEventState.Succeeded);
        Assert.Equal(gambitEvaluated, KernelEvent.From(gambitEvaluated).AsGambitEvaluated());

        var gambitExecuted = new GambitExecutedKernelEvent(13, 14, 15, 16, 17, 18, 19, 20, KernelEventState.Succeeded);
        Assert.Equal(gambitExecuted, KernelEvent.From(gambitExecuted).AsGambitExecuted());

        var commandApplied = new CommandAppliedKernelEvent(14, 15, 16, 17, 18);
        Assert.Equal(commandApplied, KernelEvent.From(commandApplied).AsCommandApplied());

        var commandRejected = new CommandRejectedKernelEvent(15, 16, 17, 18, 19, 20);
        Assert.Equal(commandRejected, KernelEvent.From(commandRejected).AsCommandRejected());

        var tickCompleted = new TickCompletedKernelEvent(16, 17);
        Assert.Equal(tickCompleted, KernelEvent.From(tickCompleted).AsTickCompleted());

        var generatorEnabledChanged = new GeneratorEnabledChangedKernelEvent(17, 18, 19, 1);
        Assert.Equal(generatorEnabledChanged, KernelEvent.From(generatorEnabledChanged).AsGeneratorEnabledChanged());

        var cellCreated = new CellCreatedKernelEvent(18, 19, 1, -1, 2, 3, 4, 5, 6);
        Assert.Equal(cellCreated, KernelEvent.From(cellCreated).AsCellCreated());

        var cellChanged = new CellChangedKernelEvent(19, 20, 2, -2, 3, 4, 5, 6, 7, 8, 9);
        Assert.Equal(cellChanged, KernelEvent.From(cellChanged).AsCellChanged());

        var cellInfluenceChanged = new CellInfluenceChangedKernelEvent(20, 21, -3, 3, 4, PlaneCodes.Physical, 5, 6, 10f, 25f);
        Assert.Equal(cellInfluenceChanged, KernelEvent.From(cellInfluenceChanged).AsCellInfluenceChanged());

        var provenanceInvocationRecorded = new ProvenanceInvocationRecordedKernelEvent(21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31);
        Assert.Equal(provenanceInvocationRecorded, KernelEvent.From(provenanceInvocationRecorded).AsProvenanceInvocationRecorded());

        var kernelStorageGrown = new KernelStorageGrownKernelEvent(22, 23, 24, 25, 26, 27, 28, 29);
        Assert.Equal(kernelStorageGrown, KernelEvent.From(kernelStorageGrown).AsKernelStorageGrown());
    }

    private static void AssertPlanningEventsRoundTrip()
    {
        var planStarted = new PlanStartedKernelEvent(18, 19, 20, 21, 22, 23);
        Assert.Equal(planStarted, KernelEvent.From(planStarted).AsPlanStarted());

        var goalConsidered = new GoalConsideredKernelEvent(19, 20, 21, 22, 23, 24, 1.5f);
        Assert.Equal(goalConsidered, KernelEvent.From(goalConsidered).AsGoalConsidered());

        var goalScored = new GoalScoredKernelEvent(20, 21, 22, 23, 24, 25, 26, -0.5f, 4.75f);
        Assert.Equal(goalScored, KernelEvent.From(goalScored).AsGoalScored());

        var goalSelected = new GoalSelectedKernelEvent(21, 22, 23, 24, 25, 8.25f);
        Assert.Equal(goalSelected, KernelEvent.From(goalSelected).AsGoalSelected());

        var planStepEmitted = new PlanStepEmittedKernelEvent(22, 23, 24, 25, 26, 27, 28, 29, 30);
        Assert.Equal(planStepEmitted, KernelEvent.From(planStepEmitted).AsPlanStepEmitted());

        var planCommitted = new PlanCommittedKernelEvent(23, 24, 25, 26, 27, 28, 9.5f);
        Assert.Equal(planCommitted, KernelEvent.From(planCommitted).AsPlanCommitted());

        var planAbandoned = new PlanAbandonedKernelEvent(24, 25, 26, 27, 28);
        Assert.Equal(planAbandoned, KernelEvent.From(planAbandoned).AsPlanAbandoned());
    }
}
