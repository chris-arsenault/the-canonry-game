namespace Engine.Observability.Tests.Observability;

using Engine.Constants;
using Engine.Eventing;
using Engine.Observability;
using Engine.Projection;

public sealed class KernelTelemetryRecorderTests
{
    [Fact]
    public void Recorder_ProjectsPrimitiveRowsFromEventLogWithSymbolCatalog()
    {
        var log = new KernelEventLog(capacity: 64);
        var symbols = CreateTestSymbols();
        using var recorder = new KernelTelemetryRecorder(log, "run-1", symbols, seed: 42, domain: "test");

        WriteTelemetryEvents(log);
        recorder.RecordSnapshots(tick: 3);

        AssertTelemetryBatch(recorder.Batch);
    }

    [Fact]
    public void Recorder_ProjectsCultureFieldEmergenceMetrics()
    {
        var log = new KernelEventLog(capacity: 64);
        var symbols = CreateTestSymbols();
        using var recorder = new KernelTelemetryRecorder(log, "run-culture-field", symbols, seed: 44, domain: "test");

        WriteCultureFieldMetricEvents(log);

        recorder.RecordSnapshots(tick: 3);

        AssertCultureFieldMetricRows(recorder.Batch);
    }

    private static void WriteCultureFieldMetricEvents(KernelEventLog log)
    {
        WriteCultureFieldMetricEntity(log);
        WriteCultureFieldMetricCells(log);
        WriteCultureSlotOccupancyChanged(log);
        WriteCultureFieldMetricAction(log);
    }

    private static void AssertCultureFieldMetricRows(TelemetryBatch batch)
    {
        var component = Assert.Single(batch.CultureFieldComponentSnapshots);
        Assert.Equal(3, component.CultureId);
        Assert.Equal(2, component.InfluencedCellCount);
        Assert.Equal(1, component.ComponentCount);
        Assert.Equal(2, component.LargestComponentCellCount);
        Assert.Equal(100f, component.LargestComponentShare);

        var birth = Assert.Single(batch.CultureEntityBirthFieldEvents);
        Assert.Equal(100, birth.EntityId);
        Assert.Equal(CultureFieldStrengthBandCodes.None, birth.PhysicalFieldBandCode);

        var action = Assert.Single(batch.CultureInfluenceActionFieldEvents);
        Assert.Equal(100, action.EntityId);
        Assert.True(action.SlotOccupied);
        Assert.True(action.LocalRawMass > 0f);
        Assert.Equal(25f, action.Delta);

        var entityField = Assert.Single(batch.CultureEntityFieldSnapshots, row => row.EntityId == 100 && row.PlaneCode == PlaneCodes.Physical);
        Assert.True(entityField.SlotOccupied);
        Assert.True(entityField.LocalRawMass > 0f);
    }

    private static void WriteCultureFieldMetricEntity(KernelEventLog log)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            EntityId: 100,
            SourceCodeId: 5,
            KindId: 1,
            SubtypeId: 2,
            CultureId: 3,
            PhysicalQ: 0,
            PhysicalR: 0,
            SemanticQ: 0,
            SemanticR: 0,
            SemanticS: 0))));
    }

    private static void WriteCultureFieldMetricCells(KernelEventLog log)
    {
        WriteCultureInfluenceChanged(log, worldVersion: 2, q: 0, currentShare: 80f, currentRaw: 60f);
        WriteCultureInfluenceChanged(log, worldVersion: 3, q: 1, currentShare: 75f, currentRaw: 40f);
    }

    private static void WriteCultureInfluenceChanged(
        KernelEventLog log,
        long worldVersion,
        int q,
        float currentShare,
        float currentRaw)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new CellCultureInfluenceChangedKernelEvent(
            Tick: 1,
            WorldVersion: worldVersion,
            Q: q,
            R: 0,
            SourceCodeId: 5,
            PlaneCode: PlaneCodes.Physical,
            CultureId: 3,
            PreviousShare: 0f,
            CurrentShare: currentShare,
            PreviousRawInfluence: 0f,
            CurrentRawInfluence: currentRaw,
            CurrentTotalRawInfluence: currentRaw))));
    }

    private static void WriteCultureFieldMetricAction(KernelEventLog log)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new CellInfluenceChangedKernelEvent(
            Tick: 2,
            WorldVersion: 4,
            Q: 1,
            R: 0,
            SourceCodeId: 5,
            PlaneCode: PlaneCodes.Physical,
            EntityId: 100,
            ActionId: 11,
            PreviousInfluence: 10f,
            CurrentInfluence: 35f))));
    }

    private static ProjectionSymbolCatalog CreateTestSymbols() =>
        new ProjectionSymbolCatalog()
            .RegisterSymbol(ProjectionSymbolKind.Kind, 1, "settlement", "Settlement")
            .RegisterSymbol(ProjectionSymbolKind.Subtype, 2, "village", "Village")
            .RegisterSymbol(ProjectionSymbolKind.Culture, 3, "north", "North")
            .RegisterSymbol(ProjectionSymbolKind.Attribute, 4, "health", "Health")
            .RegisterSymbol(ProjectionSymbolKind.SourceCode, 5, "low_altitude", "Low Altitude")
            .RegisterSymbol(ProjectionSymbolKind.EventCode, 7, "relationship_added", "Relationship Added")
            .RegisterSymbol(ProjectionSymbolKind.RelationshipKind, 8, "allied_with", "Allied With")
            .RegisterSymbol(ProjectionSymbolKind.Generator, 10, "found_settlement", "Found Settlement")
            .RegisterSymbol(ProjectionSymbolKind.Verb, 11, "forage", "Forage")
            .RegisterEntityName(100, "Hearthhome")
            .RegisterEntityName(101, "New Hall")
            .RegisterEntityName(200, "Waystone");

    private static void WriteTelemetryEvents(KernelEventLog log)
    {
        WriteEntityCreationEvents(log);
        WriteStateChangeEvents(log);
        WriteActionAndGeneratorEvents(log);
    }

    private static void WriteEntityCreationEvents(KernelEventLog log)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            EntityId: 100,
            SourceCodeId: 5,
            KindId: 1,
            SubtypeId: 2,
            CultureId: 3,
            PhysicalQ: 0,
            PhysicalR: 0,
            SemanticQ: 0,
            SemanticR: 1,
            SemanticS: -1))));
        Assert.True(log.TryWrite(KernelEvent.From(new EntityCreatedKernelEvent(
            Tick: 3,
            WorldVersion: 7,
            EntityId: 101,
            SourceCodeId: 5,
            KindId: 1,
            SubtypeId: 2,
            CultureId: 3,
            PhysicalQ: 3,
            PhysicalR: -1,
            SemanticQ: 1,
            SemanticR: 0,
            SemanticS: -1))));
    }

    private static void WriteStateChangeEvents(KernelEventLog log)
    {
        WriteMapStateEvents(log);
        Assert.True(log.TryWrite(KernelEvent.From(new AttributeChangedKernelEvent(
            Tick: 1,
            WorldVersion: 3,
            EntityId: 100,
            AttributeId: 4,
            SourceCodeId: 5,
            OldValue: 5f,
            NewValue: 7f))));
        Assert.True(log.TryWrite(KernelEvent.From(new EntityMovedKernelEvent(
            Tick: 2,
            WorldVersion: 4,
            EntityId: 100,
            SourceCodeId: 5,
            PlaneCode: PlaneCodes.Physical,
            FromQ: 0,
            FromR: 0,
            FromS: 0,
            ToQ: 2,
            ToR: -1,
            ToS: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new RelationshipChangedKernelEvent(
            Tick: 2,
            WorldVersion: 6,
            ChangeCodeId: 7,
            SourceCodeId: 5,
            RelationshipKindId: 8,
            PolarityCode: RelationshipPolarityCodes.Neutral,
            SourceEntityId: 100,
            TargetEntityId: 101,
            RelationshipId: 300,
            PreviousStrength: 0f,
            NewStrength: 0.75f,
            ActionId: 12))));
    }

    private static void WriteMapStateEvents(KernelEventLog log)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new CellCreatedKernelEvent(
            Tick: 1,
            WorldVersion: 2,
            Q: 2,
            R: -1,
            SourceCodeId: 5,
            TerrainId: 12,
            OwnerEntityId: 100,
            SuperregionId: 4,
            ContinentId: 2))));
        WriteMapCellStateChanged(log, tick: 1, planeCode: PlaneCodes.Physical, q: 2, r: -1, previousTerrainId: 0, currentTerrainId: 12, previousOwnerEntityId: 0, currentOwnerEntityId: 100, superregionId: 4, continentId: 2);
        WriteMapCellStateChanged(log, tick: 1, planeCode: PlaneCodes.Semantic, q: 4, r: -2, previousTerrainId: 0, currentTerrainId: 14, previousOwnerEntityId: 0, currentOwnerEntityId: 100, superregionId: 0, continentId: 0);
        Assert.True(log.TryWrite(KernelEvent.From(new CellRoadChangedKernelEvent(
            Tick: 2,
            WorldVersion: 5,
            PlaneCode: PlaneCodes.Physical,
            Q: 2,
            R: -1,
            SourceCodeId: 5,
            TravelCount: 2,
            PreviousQuality: 30f,
            CurrentQuality: 45f))));
        Assert.True(log.TryWrite(KernelEvent.From(new CellInfluenceChangedKernelEvent(
            Tick: 2,
            WorldVersion: 5,
            Q: 2,
            R: -1,
            SourceCodeId: 5,
            PlaneCode: PlaneCodes.Physical,
            EntityId: 100,
            ActionId: 11,
            PreviousInfluence: 10f,
            CurrentInfluence: 30f))));
        Assert.True(log.TryWrite(KernelEvent.From(new CellCultureInfluenceChangedKernelEvent(
            Tick: 2,
            WorldVersion: 5,
            Q: 2,
            R: -1,
            SourceCodeId: 5,
            PlaneCode: PlaneCodes.Physical,
            CultureId: 3,
            PreviousShare: 25f,
            CurrentShare: 75f))));
        WriteCultureSlotOccupancyChanged(log);
        Assert.True(log.TryWrite(KernelEvent.From(new CellChangedKernelEvent(
            Tick: 3,
            WorldVersion: 8,
            Q: 2,
            R: -1,
            SourceCodeId: 5,
            PreviousTerrainId: 12,
            CurrentTerrainId: 13,
            PreviousOwnerEntityId: 100,
            CurrentOwnerEntityId: 101,
            SuperregionId: 4,
            ContinentId: 2))));
        WriteMapCellStateChanged(log, tick: 3, planeCode: PlaneCodes.Physical, q: 2, r: -1, previousTerrainId: 12, currentTerrainId: 13, previousOwnerEntityId: 100, currentOwnerEntityId: 101, superregionId: 4, continentId: 2);
    }

    private static void WriteMapCellStateChanged(
        KernelEventLog log,
        int tick,
        int planeCode,
        int q,
        int r,
        int previousTerrainId,
        int currentTerrainId,
        int previousOwnerEntityId,
        int currentOwnerEntityId,
        int superregionId,
        int continentId)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new MapCellStateChangedKernelEvent(
            Tick: tick,
            WorldVersion: tick == 1 ? 2 : 8,
            PlaneCode: planeCode,
            Q: q,
            R: r,
            SourceCodeId: 5,
            PreviousTerrainId: previousTerrainId,
            CurrentTerrainId: currentTerrainId,
            PreviousOwnerEntityId: previousOwnerEntityId,
            CurrentOwnerEntityId: currentOwnerEntityId,
            SuperregionId: superregionId,
            ContinentId: continentId))));
    }

    private static void WriteCultureSlotOccupancyChanged(KernelEventLog log)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new CultureSlotOccupancyChangedKernelEvent(
            Tick: 2,
            WorldVersion: 5,
            SourceCodeId: 5,
            CultureId: 3,
            SlotId: 9,
            PolarityCode: RelationshipPolarityCodes.Positive,
            PreviousEntityId: 0,
            CurrentEntityId: 100,
            PreviousScore: 0f,
            CurrentScore: 4.25f))));
    }

    private static void WriteActionAndGeneratorEvents(KernelEventLog log)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new TraceKernelEvent(
            Tick: 2,
            WorldVersion: 5,
            IntentId: ActionFamilyCodes.LowAltitudeVerb,
            EntityId: 100,
            TraceSubjectCode: TraceSubjectCodes.TargetDetail,
            CandidateIndex: 1,
            ParentCandidateIndex: 0,
            ActionFamilyCode: ActionFamilyCodes.LowAltitudeVerb,
            ActionId: 11,
            EffectScopeCode: BehaviorEffectScopeCodes.SelfAttribute,
            EffectValueId: 4,
            EffectKindId: 1,
            TargetEntityId: 100,
            StatusCode: TraceStatusCodes.Actionable,
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
            Urgency: 0.5f,
            RewardOrBaseScore: 0.75f,
            RawWeight: 1.25f,
            AdjustedWeight: 1.5f,
            AffordanceScore: 0f,
            AffordanceBonus: 0f))));
        Assert.True(log.TryWrite(KernelEvent.From(new GeneratorFiredKernelEvent(
            Tick: 3,
            WorldVersion: 6,
            GeneratorId: 10,
            TargetEntityId: 200,
            CreatedEntityCount: 1,
            CreatedRelationshipCount: 2))));
    }

    private static void AssertTelemetryBatch(TelemetryBatch batch)
    {
        Assert.Equal(42, Assert.Single(batch.Runs).RunId);
        AssertEntityRows(batch);
        AssertPrimitiveEventRows(batch);
        AssertGeneratedAndTickRows(batch);
    }

    private static void AssertEntityRows(TelemetryBatch batch)
    {
        var created = Assert.Single(batch.EntityEvents, row => row.EntityId == 100);
        Assert.Equal((int)KernelEventType.EntityCreated, created.EventTypeCode);
        Assert.Equal(5, created.SourceCodeId);
        Assert.Equal(1, created.KindId);
        Assert.Equal(2, created.SubtypeId);
        Assert.Equal(3, created.CultureId);
        Assert.Equal(EntityStatusCodes.Active, created.CurrentStatusCode);
        Assert.Contains(batch.EntityNameSymbols, row =>
            row.EntityId == 100 &&
            row.DisplayName == "Hearthhome");
    }

    private static void AssertPrimitiveEventRows(TelemetryBatch batch)
    {
        var attribute = Assert.Single(batch.AttributeEvents);
        Assert.Equal(4, attribute.AttributeId);
        Assert.Equal(2f, attribute.Delta);
        Assert.Equal(5, attribute.SourceCodeId);

        var movement = Assert.Single(batch.MovementEvents);
        Assert.Equal(5, movement.SourceCodeId);
        Assert.Equal(PlaneCodes.Physical, movement.PlaneCode);
        Assert.Equal(2, movement.Distance);

        var road = Assert.Single(batch.CellRoadEvents);
        Assert.Equal(5, road.SourceCodeId);
        Assert.Equal(PlaneCodes.Physical, road.PlaneCode);
        Assert.Equal(2, road.TravelCount);
        Assert.Equal(30f, road.PreviousQuality);
        Assert.Equal(45f, road.CurrentQuality);
        Assert.Equal(15f, road.Delta);

        var influence = Assert.Single(batch.CellInfluenceEvents);
        Assert.Equal(100, influence.EntityId);
        Assert.Equal(11, influence.ActionId);
        Assert.Equal(20f, influence.Delta);

        var cultureInfluence = Assert.Single(batch.CellCultureInfluenceEvents);
        Assert.Equal(3, cultureInfluence.CultureId);
        Assert.Equal(50f, cultureInfluence.Delta);

        AssertCultureSlotOccupancyRows(batch);

        AssertMapCellRows(batch);

        var relationship = Assert.Single(batch.RelationshipEvents);
        Assert.Equal(7, relationship.ChangeCodeId);
        Assert.Equal(5, relationship.SourceCodeId);
        Assert.Equal(8, relationship.RelationshipKindId);
        Assert.Equal(RelationshipPolarityCodes.Neutral, relationship.PolarityCode);
        Assert.Equal(12, relationship.ActionId);

        var trace = Assert.Single(batch.TraceEvents);
        Assert.Equal(TraceSubjectCodes.TargetDetail, trace.TraceSubjectCode);
        Assert.Equal(TraceStatusCodes.Actionable, trace.StatusCode);
        Assert.Equal(11, trace.ActionId);
        Assert.Equal(1.5f, trace.AdjustedWeight);

        AssertMapPlacementRows(batch);
    }

    private static void AssertMapCellRows(TelemetryBatch batch)
    {
        var createdCell = Assert.Single(
            batch.MapCellEvents,
            row => row.EventTypeCode == (int)KernelEventType.MapCellStateChanged &&
                   row.PlaneCode == PlaneCodes.Physical &&
                   row.PreviousTerrainId == 0);
        Assert.Equal(12, createdCell.CurrentTerrainId);
        Assert.Equal(100, createdCell.CurrentOwnerEntityId);
        Assert.Equal(4, createdCell.SuperregionId);
        Assert.Equal(2, createdCell.ContinentId);

        var semanticCreatedCell = Assert.Single(
            batch.MapCellEvents,
            row => row.EventTypeCode == (int)KernelEventType.MapCellStateChanged &&
                   row.PlaneCode == PlaneCodes.Semantic);
        Assert.Equal(14, semanticCreatedCell.CurrentTerrainId);
        Assert.Equal(100, semanticCreatedCell.CurrentOwnerEntityId);

        var changedCell = Assert.Single(
            batch.MapCellEvents,
            row => row.EventTypeCode == (int)KernelEventType.MapCellStateChanged &&
                   row.PlaneCode == PlaneCodes.Physical &&
                   row.PreviousTerrainId == 12);
        Assert.Equal(12, changedCell.PreviousTerrainId);
        Assert.Equal(13, changedCell.CurrentTerrainId);
        Assert.Equal(100, changedCell.PreviousOwnerEntityId);
        Assert.Equal(101, changedCell.CurrentOwnerEntityId);
        Assert.Equal(4, changedCell.SuperregionId);
        Assert.Equal(2, changedCell.ContinentId);
    }

    private static void AssertMapPlacementRows(TelemetryBatch batch)
    {
        Assert.Contains(batch.PlacementEvents, row =>
            row.Selected &&
            row.PlaneCode == PlaneCodes.Physical &&
            row.Q == 2 &&
            row.R == -1);
        Assert.Contains(batch.PlacementEvents, row =>
            row.Selected &&
            row.PlaneCode == PlaneCodes.Semantic &&
            row.Q == 4 &&
            row.R == -2);
    }

    private static void AssertCultureSlotOccupancyRows(TelemetryBatch batch)
    {
        var cultureSlot = Assert.Single(batch.CultureSlotOccupancyEvents);
        Assert.Equal(3, cultureSlot.CultureId);
        Assert.Equal(9, cultureSlot.SlotId);
        Assert.Equal(RelationshipPolarityCodes.Positive, cultureSlot.PolarityCode);
        Assert.Equal(0, cultureSlot.PreviousEntityId);
        Assert.Equal(100, cultureSlot.CurrentEntityId);
        Assert.Equal(4.25f, cultureSlot.CurrentScore);
    }

    private static void AssertGeneratedAndTickRows(TelemetryBatch batch)
    {
        var generator = Assert.Single(batch.GeneratorEvents);
        Assert.Equal(10, generator.GeneratorId);
        Assert.Equal(1, generator.CreatedEntityCount);

        var generated = Assert.Single(batch.GeneratedEntities);
        Assert.Equal(101, generated.EntityId);
        Assert.Equal(10, generated.GeneratorId);
        Assert.Contains(batch.PlacementEvents, row =>
            row.Selected &&
            row.AnchorEntityId == 101 &&
            row.PlaneCode == PlaneCodes.Physical &&
            row.Q == 3 &&
            row.R == -1);
        Assert.Contains(batch.PlacementEvents, row =>
            row.Selected &&
            row.AnchorEntityId == 101 &&
            row.PlaneCode == PlaneCodes.Semantic &&
            row.Q == 1 &&
            row.R == 0 &&
            row.S == -1);

        var tick = Assert.Single(batch.TickSnapshots);
        Assert.Equal(2, tick.ActiveEntities);
        Assert.Equal(1, tick.TotalGeneratorFires);
        Assert.Equal(1, tick.ActiveRelationships);
        Assert.Equal(1, tick.CoverageCount);
        Assert.Equal(1, tick.CoveredCells);
        Assert.Equal(0, tick.UncoveredCells);

        var physicalMap = Assert.Single(batch.MapSnapshots, row => row.PlaneCode == PlaneCodes.Physical);
        Assert.Equal(13, physicalMap.TerrainId);
        Assert.Equal(1, physicalMap.CellCount);
        Assert.Equal(1, physicalMap.CoveredCellCount);
        Assert.Equal(1, physicalMap.ActiveEntityCount);

        var semanticMap = Assert.Single(batch.MapSnapshots, row => row.PlaneCode == PlaneCodes.Semantic);
        Assert.Equal(14, semanticMap.TerrainId);
        Assert.Equal(1, semanticMap.CellCount);
        Assert.Equal(1, semanticMap.CoveredCellCount);
        Assert.Equal(0, semanticMap.ActiveEntityCount);
    }

}
