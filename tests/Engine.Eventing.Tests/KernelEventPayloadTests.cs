namespace Engine.Eventing.Tests;

using Engine.Constants;
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
            PreviousStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0.25f),
            NewStrengthUnits: KernelFixedPoint.RelationshipStrengthUnits(0.75f),
            ActionId: 23,
            IsActive: 1));

        Assert.Equal(7, kernelEvent.GetIntSlot(0));
        Assert.Equal(41, kernelEvent.GetIntSlot(1));
        Assert.Equal(11, kernelEvent.GetIntSlot(2));
        Assert.Equal(InvalidPolarityCode, kernelEvent.GetIntSlot(3));
        Assert.Equal(13, kernelEvent.GetIntSlot(4));
        Assert.Equal(17, kernelEvent.GetIntSlot(5));
        Assert.Equal(19, kernelEvent.GetIntSlot(6));
        Assert.Equal(23, kernelEvent.GetIntSlot(7));
        Assert.Equal(KernelFixedPoint.RelationshipStrengthUnits(0.25f), kernelEvent.GetIntSlot(8));
        Assert.Equal(KernelFixedPoint.RelationshipStrengthUnits(0.75f), kernelEvent.GetIntSlot(9));
        Assert.Equal(1, kernelEvent.GetIntSlot(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => kernelEvent.GetIntSlot(KernelEvent.IntSlotCount));
        Assert.Throws<ArgumentOutOfRangeException>(() => kernelEvent.GetLongSlot(KernelEvent.LongSlotCount));
    }

    [Fact]
    public void GeneratedPackAndViewRoundTripEveryRegisteredEvent()
    {
        AssertEntityAndChangeEventsRoundTrip();
        AssertRuntimeEventsRoundTrip();
    }

    private static void AssertEntityAndChangeEventsRoundTrip()
    {
        var entityCreated = new EntityCreatedKernelEvent(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, -21, 3);
        Assert.Equal(entityCreated, KernelEvent.From(entityCreated).AsEntityCreated());

        var entityStatusChanged = new EntityStatusChangedKernelEvent(2, 3, 4, 5, 6, 7, 8, 9);
        Assert.Equal(entityStatusChanged, KernelEvent.From(entityStatusChanged).AsEntityStatusChanged());

        var entityMoved = new EntityMovedKernelEvent(3, 4, 5, 6, PlaneCodes.Physical, 7, 8, -15, 9, 10, -19, 0, 1);
        Assert.Equal(entityMoved, KernelEvent.From(entityMoved).AsEntityMoved());

        var attributeChanged = new AttributeChangedKernelEvent(
            4,
            5,
            6,
            7,
            8,
            KernelFixedPoint.AttributeUnits(1.25f),
            KernelFixedPoint.AttributeUnits(2.5f));
        Assert.Equal(attributeChanged, KernelEvent.From(attributeChanged).AsAttributeChanged());

        var entityCultureChanged = new EntityCultureChangedKernelEvent(5, 6, 7, 8, 9, 10, 11, 12);
        Assert.Equal(entityCultureChanged, KernelEvent.From(entityCultureChanged).AsEntityCultureChanged());

        var relationshipChanged = new RelationshipChangedKernelEvent(
            6,
            7,
            8,
            9,
            10,
            InvalidPolarityCode,
            11,
            12,
            13,
            KernelFixedPoint.RelationshipStrengthUnits(0.125f),
            KernelFixedPoint.RelationshipStrengthUnits(0.875f),
            0,
            1);
        Assert.Equal(relationshipChanged, KernelEvent.From(relationshipChanged).AsRelationshipChanged());
    }

    private static void AssertRuntimeEventsRoundTrip()
    {
        AssertBehaviorRuntimeEventsRoundTrip();
        AssertWorldRuntimeEventsRoundTrip();
        AssertInfrastructureRuntimeEventsRoundTrip();
    }

    private static void AssertBehaviorRuntimeEventsRoundTrip()
    {
        var goalGenerated = new GoalGeneratedKernelEvent(9, 10, 11, 12, 13, 14, KernelEventState.Truncated);
        Assert.Equal(goalGenerated, KernelEvent.From(goalGenerated).AsGoalGenerated());

        var generatorEvaluated = new GeneratorEvaluatedKernelEvent(11, 12, 13, 14, 15, 16, 17);
        Assert.Equal(generatorEvaluated, KernelEvent.From(generatorEvaluated).AsGeneratorEvaluated());

        var generatorFired = new GeneratorFiredKernelEvent(12, 13, 14, 15, 16, 17, 18, 19);
        Assert.Equal(generatorFired, KernelEvent.From(generatorFired).AsGeneratorFired());

        AssertGeneratorTargetEvaluatedRoundTrips();

        var gambitEvaluated = new GambitEvaluatedKernelEvent(13, 14, 15, 16, 17, 18, KernelEventState.Succeeded);
        Assert.Equal(gambitEvaluated, KernelEvent.From(gambitEvaluated).AsGambitEvaluated());

        var gambitExecuted = new GambitExecutedKernelEvent(13, 14, 15, 16, 17, 18, 19, 20, KernelEventState.Succeeded);
        Assert.Equal(gambitExecuted, KernelEvent.From(gambitExecuted).AsGambitExecuted());

        var commandApplied = new CommandAppliedKernelEvent(14, 15, 16, 17, 18);
        Assert.Equal(commandApplied, KernelEvent.From(commandApplied).AsCommandApplied());

        var commandRejected = new CommandRejectedKernelEvent(15, 16, 17, 18, 19, 20);
        Assert.Equal(commandRejected, KernelEvent.From(commandRejected).AsCommandRejected());
    }

    private static void AssertWorldRuntimeEventsRoundTrip()
    {
        AssertWorldCellEventsRoundTrip();
        AssertWorldCultureEventsRoundTrip();
        AssertWorldActorAndTravelEventsRoundTrip();
    }

    private static void AssertWorldCellEventsRoundTrip()
    {
        var tickCompleted = new TickCompletedKernelEvent(16, 17);
        Assert.Equal(tickCompleted, KernelEvent.From(tickCompleted).AsTickCompleted());

        var watermarkAdvanced =
            new WatermarkAdvancedKernelEvent(16, 17, 16, 18, 64, 19);
        Assert.Equal(
            watermarkAdvanced,
            KernelEvent.From(watermarkAdvanced).AsWatermarkAdvanced());

        var generatorEnabledChanged = new GeneratorEnabledChangedKernelEvent(17, 18, 19, 1);
        Assert.Equal(generatorEnabledChanged, KernelEvent.From(generatorEnabledChanged).AsGeneratorEnabledChanged());

        var cellInfluenceChanged = new CellInfluenceChangedKernelEvent(
            20,
            21,
            -3,
            3,
            4,
            PlaneCodes.Physical,
            5,
            6,
            FixedScalar.FromSingle(10f).Units,
            FixedScalar.FromSingle(25f).Units);
        Assert.Equal(cellInfluenceChanged, KernelEvent.From(cellInfluenceChanged).AsCellInfluenceChanged());
    }

    private static void AssertWorldCultureEventsRoundTrip()
    {
        var cellCultureInfluenceChanged = new CellCultureInfluenceChangedKernelEvent(
            20,
            21,
            -3,
            3,
            4,
            PlaneCodes.Physical,
            5,
            -1,
            FixedScalar.FromSingle(10f).Units,
            FixedScalar.FromSingle(25f).Units);
        Assert.Equal(cellCultureInfluenceChanged, KernelEvent.From(cellCultureInfluenceChanged).AsCellCultureInfluenceChanged());

        var cultureSlotOccupancyChanged = new CultureSlotOccupancyChangedKernelEvent(
            20,
            21,
            22,
            23,
            24,
            25,
            26,
            27,
            FixedScalar.FromSingle(1.25f).Units,
            FixedScalar.FromSingle(2.5f).Units);
        Assert.Equal(cultureSlotOccupancyChanged, KernelEvent.From(cultureSlotOccupancyChanged).AsCultureSlotOccupancyChanged());
    }

    private static void AssertWorldActorAndTravelEventsRoundTrip()
    {
        var minorActorCreated = new MinorActorCreatedKernelEvent(
            20, 21, 22, 23, 24, 0, 25, MinorActorRoleCodes.CulturalFront,
            PlaneCodes.Physical, -3, 3, MinorActorPostureCodes.Traveling,
            FixedScalar.FromSingle(3.75f).Units);
        Assert.Equal(minorActorCreated, KernelEvent.From(minorActorCreated).AsMinorActorCreated());

        var minorActorAssigned = new MinorActorAssignedKernelEvent(
            20, 21, 22, 23, MinorActorRoleCodes.CulturalFront, PlaneCodes.Physical,
            -3, 3, 4, -2, MinorActorTargetingCodes.ExtendBorder,
            MinorActorPostureCodes.Traveling, 30, 25, FixedScalar.FromSingle(3.75f).Units);
        Assert.Equal(minorActorAssigned, KernelEvent.From(minorActorAssigned).AsMinorActorAssigned());

        var minorActorMoved = new MinorActorMovedKernelEvent(
            20, 21, 22, 23, 25, MinorActorRoleCodes.CulturalFront,
            PlaneCodes.Physical, -3, 3, -2, 3, 4, -2,
            MinorActorPostureCodes.Traveling, FixedScalar.FromSingle(3.5f).Units);
        Assert.Equal(minorActorMoved, KernelEvent.From(minorActorMoved).AsMinorActorMoved());

        var minorActorWorked = new MinorActorWorkedKernelEvent(
            20, 21, 22, 23, 24, 2, 5, 25, MinorActorRoleCodes.CulturalFront,
            MinorActorActionCodes.BuildCulture, MinorActorTargetingCodes.ExtendBorder,
            PlaneCodes.Physical, -2, 3, CultureFieldCellKindCodes.Border,
            FixedScalar.FromSingle(1.25f).Units, FixedScalar.FromSingle(2.5f).Units,
            FixedScalar.FromSingle(3.75f).Units, FixedScalar.FromSingle(5f).Units);
        Assert.Equal(minorActorWorked, KernelEvent.From(minorActorWorked).AsMinorActorWorked());

        var minorActorEncountered = new MinorActorEncounteredKernelEvent(
            20, 21, 22, 26, 25, 27, MinorActorRoleCodes.CulturalFront,
            MinorActorRoleCodes.CampaignColumn, PlaneCodes.Physical, -2, 3,
            MinorActorEncounterCodes.CampaignDisruption,
            MinorActorPostureCodes.Working, MinorActorPostureCodes.Disrupted,
            FixedScalar.FromSingle(2f).Units, FixedScalar.FromSingle(1f).Units,
            FixedScalar.FromSingle(0.5f).Units);
        Assert.Equal(minorActorEncountered, KernelEvent.From(minorActorEncountered).AsMinorActorEncountered());

        var minorActorRetired = new MinorActorRetiredKernelEvent(
            20, 21, 22, 23, 24, 0, 25, MinorActorRoleCodes.CulturalFront,
            PlaneCodes.Physical, -2, 3, MinorActorRetirementReasonCodes.NoAssignment,
            MinorActorPostureCodes.Working, FixedScalar.FromSingle(2f).Units);
        Assert.Equal(minorActorRetired, KernelEvent.From(minorActorRetired).AsMinorActorRetired());

        var mapCellStateChanged = new MapCellStateChangedKernelEvent(20, 21, PlaneCodes.Semantic, -3, 3, 4, 5, 6, 7, 8, 9, 10, 0);
        Assert.Equal(mapCellStateChanged, KernelEvent.From(mapCellStateChanged).AsMapCellStateChanged());

        var cellRoadChanged = new CellRoadChangedKernelEvent(
            20,
            21,
            PlaneCodes.Physical,
            -3,
            3,
            4,
            5,
            KernelFixedPoint.RoadQualityUnits(0.25f),
            KernelFixedPoint.RoadQualityUnits(0.75f));
        Assert.Equal(cellRoadChanged, KernelEvent.From(cellRoadChanged).AsCellRoadChanged());

        var cellTravelStructureChanged = new CellTravelStructureChangedKernelEvent(20, 21, PlaneCodes.Physical, -3, 3, 4, 5, 6);
        Assert.Equal(cellTravelStructureChanged, KernelEvent.From(cellTravelStructureChanged).AsCellTravelStructureChanged());
    }

    private static void AssertInfrastructureRuntimeEventsRoundTrip()
    {
        var provenanceInvocationRecorded = new ProvenanceInvocationRecordedKernelEvent(21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31);
        Assert.Equal(provenanceInvocationRecorded, KernelEvent.From(provenanceInvocationRecorded).AsProvenanceInvocationRecorded());

        var trace = new TraceKernelEvent(
            23,
            24,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9,
            10,
            11,
            12,
            0,
            0,
            0,
            3,
            8,
            2,
            1,
            1,
            1,
            2,
            FixedScalar.FromSingle(1.25f).Units,
            FixedScalar.FromSingle(2.5f).Units,
            FixedScalar.FromSingle(3.75f).Units,
            FixedScalar.FromSingle(5f).Units,
            FixedScalar.Zero.Units,
            FixedScalar.Zero.Units);
        Assert.Equal(trace, KernelEvent.From(trace).AsTrace());
    }

    private static void AssertGeneratorTargetEvaluatedRoundTrips()
    {
        var evt = new GeneratorTargetEvaluatedKernelEvent(
            12,
            13,
            14,
            15,
            16,
            17,
            18,
            19,
            20,
            21,
            22,
            23,
            24,
            25,
            26,
            27,
            28,
            29,
            FixedScalar.FromSingle(1.25f).Units,
            FixedScalar.FromSingle(2.5f).Units,
            FixedScalar.FromSingle(3.75f).Units,
            FixedScalar.FromSingle(5f).Units);
        Assert.Equal(evt, KernelEvent.From(evt).AsGeneratorTargetEvaluated());
    }
}
