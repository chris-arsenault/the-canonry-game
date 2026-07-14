namespace Engine.Projection.Tests.Projection;

using Engine.Eventing;
using Engine.Projection;

public sealed class PrimitiveEventProjectionTests
{
    [Fact]
    public void UiChronicleProjection_UsesSelectedTargetSlotForGambitTarget()
    {
        var log = new KernelEventLog(capacity: 4);
        var projection = new UiChronicleProjection(symbols: null);
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new GambitExecutedKernelEvent(
            Tick: 7,
            WorldVersion: 8,
            ActorId: 10,
            ActorKindId: 2,
            GambitId: 3,
            AffectedEntityId0: 10,
            AffectedEntityId1: 20,
            AffectedEntityCount: 2,
            State: KernelEventState.Succeeded))));

        pump.DrainAvailable();

        var line = Assert.Single(projection.Lines);
        Assert.Contains("[color=#E7E3D5]E10[/color]", line, StringComparison.Ordinal);
        Assert.EndsWith("[color=#E7E3D5]E20[/color]", line, StringComparison.Ordinal);
    }

    [Fact]
    public void UiChronicleProjection_CapturesDurableBeatsForActiveTales()
    {
        var log = new KernelEventLog(capacity: 512);
        var projection = new UiChronicleProjection(
            symbols: null,
            relationshipCreatedCodeId: 91,
            relationshipCulledCodeId: 92);
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new ChronicleChangedKernelEvent(
            Tick: 10,
            WorldVersion: 1,
            ChronicleSequence: 1,
            ArchetypeId: 5,
            EventTypeCode: ChronicleEventTypeCodes.Cast,
            ActIndex: 0,
            OutcomeCodeId: 0,
            RoleEntityId0: 100,
            RoleEntityId1: 200,
            RoleEntityId2: 0,
            RoleEntityId3: 0))));

        WriteGambit(log, tick: 20, actorId: 100, targetId: 200);
        // A cast member's routine solo deed elsewhere is not this story.
        WriteGambit(log, tick: 25, actorId: 100, targetId: 999);
        // The world acting ON a cast member is.
        WriteGambit(log, tick: 30, actorId: 998, targetId: 200);
        Assert.True(log.TryWrite(KernelEvent.From(new RelationshipChangedKernelEvent(
            Tick: 40,
            WorldVersion: 5,
            ChangeCodeId: 92,
            SourceCodeId: 3,
            RelationshipKindId: 7,
            PolarityCode: RelationshipPolarityCodes.Negative,
            SourceEntityId: 200,
            TargetEntityId: 100,
            RelationshipId: 12,
            PreviousStrength: 0.4f,
            NewStrength: 0f))));

        for (var i = 0; i < 200; i++)
        {
            WriteGambit(log, tick: 50 + i, actorId: 900, targetId: 901);
        }

        pump.DrainAvailable();

        var beats = projection.BeatsFor(1);
        Assert.Equal(3, beats.Count);
        Assert.Equal(UiChronicleEntryType.Gambit, beats[0].EntryType);
        Assert.Equal(UiChronicleEntryType.Gambit, beats[1].EntryType);
        Assert.Equal(UiChronicleEntryType.Relationship, beats[2].EntryType);
        Assert.Contains("severed", beats[2].Text, StringComparison.Ordinal);
    }

    private static void WriteGambit(KernelEventLog log, int tick, int actorId, int targetId)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new GambitExecutedKernelEvent(
            Tick: tick,
            WorldVersion: tick,
            ActorId: actorId,
            ActorKindId: 2,
            GambitId: 3,
            AffectedEntityId0: actorId,
            AffectedEntityId1: targetId,
            AffectedEntityCount: 2,
            State: KernelEventState.Succeeded))));
    }

    [Fact]
    public void PrimitiveProjectionPump_ReplacesObjectMessageSubscription()
    {
        var log = new KernelEventLog(capacity: 5);
        var projection = new UiProjectionState();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new GambitExecutedKernelEvent(
            Tick: 10,
            WorldVersion: 11,
            ActorId: 1,
            ActorKindId: 2,
            GambitId: 3,
            AffectedEntityId0: 4,
            AffectedEntityId1: 0,
            AffectedEntityCount: 1,
            State: KernelEventState.Succeeded))));

        pump.DrainAvailable();

        var entity = projection.Entities[1];
        Assert.Equal(3, entity.LastGambitExecution?.GambitId);
        Assert.Equal(4, entity.LastGambitExecution?.AffectedEntityId0);
    }

    [Fact]
    public void HexMapProjection_AppliesCellChangedAsRuntimeCellState()
    {
        var log = new KernelEventLog(capacity: 4);
        var projection = new HexMapProjection();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new CellCreatedKernelEvent(
            Tick: 0,
            WorldVersion: 1,
            Q: 1,
            R: -1,
            SourceCodeId: 5,
            TerrainId: 10,
            OwnerEntityId: 100,
            SuperregionId: 2,
            ContinentId: 1,
            Z: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new CellChangedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            Q: 1,
            R: -1,
            SourceCodeId: 6,
            PreviousTerrainId: 10,
            CurrentTerrainId: 11,
            PreviousOwnerEntityId: 100,
            CurrentOwnerEntityId: 200,
            SuperregionId: 2,
            ContinentId: 1,
            Z: 0))));

        pump.DrainAvailable();

        Assert.True(projection.TryGetHex(PlaneCodes.Physical, new HexCoord(1, -1), out var cell));
        Assert.Equal(11, cell.TerrainId);
        Assert.Equal(200, cell.OwnerEntityId);
        Assert.Equal(2, cell.SuperregionId);
        Assert.Equal(1, cell.ContinentId);
        Assert.Equal(2, cell.Tick);
        Assert.Equal(2, cell.WorldVersion);
    }

    [Fact]
    public void HexMapProjection_AppliesCellInfluenceAsNonExclusiveCellState()
    {
        var log = new KernelEventLog(capacity: 4);
        var projection = new HexMapProjection();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new CellCreatedKernelEvent(
            Tick: 0,
            WorldVersion: 1,
            Q: 0,
            R: 0,
            SourceCodeId: 5,
            TerrainId: 10,
            OwnerEntityId: 100,
            SuperregionId: 3,
            ContinentId: 1,
            Z: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new CellInfluenceChangedKernelEvent(
            Tick: 1,
            WorldVersion: 2,
            Q: 0,
            R: 0,
            SourceCodeId: 6,
            PlaneCode: PlaneCodes.Physical,
            EntityId: 200,
            ActionId: 0,
            PreviousInfluence: 0f,
            CurrentInfluence: 35f))));
        Assert.True(log.TryWrite(KernelEvent.From(new CellInfluenceChangedKernelEvent(
            Tick: 1,
            WorldVersion: 3,
            Q: 0,
            R: 0,
            SourceCodeId: 6,
            PlaneCode: PlaneCodes.Physical,
            EntityId: 300,
            ActionId: 0,
            PreviousInfluence: 0f,
            CurrentInfluence: 20f))));

        pump.DrainAvailable();

        Assert.True(projection.TryGetHex(PlaneCodes.Physical, new HexCoord(0, 0), out var cell));
        Assert.Equal(100, cell.OwnerEntityId);
        Assert.Equal([200, 300], cell.Influences.Select(influence => influence.EntityId).ToArray());
        Assert.Equal([35f, 20f], cell.Influences.Select(influence => influence.Value).ToArray());
    }

    [Fact]
    public void HexMapProjection_AppliesCultureInfluenceAsProportionalCellState()
    {
        var log = new KernelEventLog(capacity: 4);
        var projection = new HexMapProjection();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new CellCultureInfluenceChangedKernelEvent(
            Tick: 1,
            WorldVersion: 2,
            Q: 0,
            R: 0,
            SourceCodeId: 6,
            PlaneCode: PlaneCodes.Physical,
            CultureId: 10,
            Z: 0,
            PreviousShare: 0f,
            CurrentShare: 66.667f))));
        Assert.True(log.TryWrite(KernelEvent.From(new CellCultureInfluenceChangedKernelEvent(
            Tick: 1,
            WorldVersion: 2,
            Q: 0,
            R: 0,
            SourceCodeId: 6,
            PlaneCode: PlaneCodes.Physical,
            CultureId: 20,
            Z: 0,
            PreviousShare: 0f,
            CurrentShare: 33.333f))));

        pump.DrainAvailable();

        Assert.True(projection.TryGetHex(PlaneCodes.Physical, new HexCoord(0, 0), out var cell));
        Assert.Equal([10, 20], cell.CultureInfluences.Select(influence => influence.CultureId).ToArray());
        Assert.Equal([66.667f, 33.333f], cell.CultureInfluences.Select(influence => influence.Share).ToArray());
    }

    [Fact]
    public void HexMapProjection_TracksOverlaySpecificVersions()
    {
        var log = new KernelEventLog(capacity: 8);
        var projection = new HexMapProjection();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        WritePhysicalCellCreated(log, ownerEntityId: 100);
        pump.DrainAvailable();

        var frameVersion = projection.FrameVersion;
        AssertOverlayVersions(projection, culture: 0, influence: 0, road: 0, route: 0);

        WritePhysicalInfluenceChanged(log, entityId: 200, value: 35f, worldVersion: 2);
        pump.DrainAvailable();

        Assert.Equal(frameVersion, projection.FrameVersion);
        AssertOverlayVersions(projection, culture: 0, influence: 1, road: 0, route: 0);

        WritePhysicalCultureChanged(log);
        pump.DrainAvailable();

        AssertOverlayVersions(projection, culture: 1, influence: 1, road: 0, route: 0);

        WritePhysicalRoadChanged(log);
        WritePhysicalTravelStructureChanged(log);
        pump.DrainAvailable();

        AssertOverlayVersions(projection, culture: 1, influence: 1, road: 1, route: 1);
    }

    [Fact]
    public void HexMapProjection_ProjectsSemanticCellsInfluenceAndRoads()
    {
        var log = new KernelEventLog(capacity: 4);
        var projection = new HexMapProjection();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        WriteSemanticSurfaceEvents(log);

        pump.DrainAvailable();

        AssertSemanticSurfaceProjection(projection);
    }

    private static void WriteSemanticSurfaceEvents(KernelEventLog log)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new MapCellStateChangedKernelEvent(
            Tick: 0,
            WorldVersion: 1,
            PlaneCode: PlaneCodes.Semantic,
            Q: -1,
            R: 1,
            SourceCodeId: 5,
            PreviousTerrainId: 0,
            CurrentTerrainId: 10,
            PreviousOwnerEntityId: 0,
            CurrentOwnerEntityId: 100,
            SuperregionId: 3,
            ContinentId: 1,
            Z: 0))));
        Assert.True(log.TryWrite(KernelEvent.From(new CellInfluenceChangedKernelEvent(
            Tick: 1,
            WorldVersion: 2,
            Q: -1,
            R: 1,
            SourceCodeId: 6,
            PlaneCode: PlaneCodes.Semantic,
            EntityId: 200,
            ActionId: 0,
            PreviousInfluence: 0f,
            CurrentInfluence: 35f))));
        Assert.True(log.TryWrite(KernelEvent.From(new CellRoadChangedKernelEvent(
            Tick: 2,
            WorldVersion: 3,
            PlaneCode: PlaneCodes.Semantic,
            Q: -1,
            R: 1,
            SourceCodeId: 7,
            TravelCount: 4,
            PreviousQuality: 20f,
            CurrentQuality: 80f))));
        Assert.True(log.TryWrite(KernelEvent.From(new CellTravelStructureChangedKernelEvent(
            Tick: 3,
            WorldVersion: 4,
            PlaneCode: PlaneCodes.Semantic,
            Q: -1,
            R: 1,
            SourceCodeId: 8,
            PreviousTravelStructureId: 0,
            CurrentTravelStructureId: 12))));
    }

    private static void AssertSemanticSurfaceProjection(HexMapProjection projection)
    {
        Assert.True(projection.TryGetHex(PlaneCodes.Semantic, new HexCoord(-1, 1), out var cell));
        Assert.Equal(PlaneCodes.Semantic, cell.PlaneCode);
        Assert.Equal(10, cell.TerrainId);
        Assert.Equal(100, cell.OwnerEntityId);
        Assert.Equal([200], cell.Influences.Select(influence => influence.EntityId).ToArray());
        Assert.Equal(80f, cell.RoadQuality);
        Assert.Equal(4, cell.RoadTravelCount);
        Assert.Equal(12, cell.TravelStructureId);
    }

    [Fact]
    public void HexMapProjection_TracksDirtyCellsAndOwnerColorInputs()
    {
        var log = new KernelEventLog(capacity: 8);
        var projection = new HexMapProjection();
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        WritePhysicalCellCreated(log, ownerEntityId: 100);
        pump.DrainAvailable();

        Assert.True(projection.TryGetHex(PlaneCodes.Physical, new HexCoord(0, 0), out var cell));
        Assert.Equal([cell.HexId], projection.DirtyCellIds(PlaneCodes.Physical).ToArray());
        Assert.Equal([100], projection.OwnerColorEntityIds.ToArray());
        projection.ClearDirtyCells(PlaneCodes.Physical);

        WritePhysicalInfluenceChanged(log, entityId: 200, value: 35f, worldVersion: 2);
        pump.DrainAvailable();

        Assert.Equal([cell.HexId], projection.DirtyCellIds(PlaneCodes.Physical).ToArray());
        Assert.Equal([100, 200], projection.OwnerColorEntityIds.ToArray());
        var ownerColorVersion = projection.OwnerColorVersion;
        projection.ClearDirtyCells(PlaneCodes.Physical);

        WritePhysicalInfluenceChanged(log, entityId: 200, value: 60f, worldVersion: 3);
        pump.DrainAvailable();

        Assert.Equal([cell.HexId], projection.DirtyCellIds(PlaneCodes.Physical).ToArray());
        Assert.Equal(ownerColorVersion, projection.OwnerColorVersion);
    }

    [Fact]
    public void PrimitiveProjectionPump_DisposeReleasesCursor()
    {
        var log = new KernelEventLog(capacity: 1, KernelEventOverflowPolicy.RejectWrite);
        var projection = new UiProjectionState();
        var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new GoalGeneratedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            EntityId: 1,
            KindId: 2,
            GoalCount: 3,
            ActionableGoalCount: 1,
            State: KernelEventState.None))));
        Assert.False(log.TryWrite(KernelEvent.From(new GoalGeneratedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            EntityId: 1,
            KindId: 2,
            GoalCount: 4,
            ActionableGoalCount: 2,
            State: KernelEventState.None))));

        pump.Dispose();

        Assert.True(log.TryWrite(KernelEvent.From(new GoalGeneratedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            EntityId: 1,
            KindId: 2,
            GoalCount: 4,
            ActionableGoalCount: 2,
            State: KernelEventState.None))));
    }

    private static void WritePhysicalCellCreated(KernelEventLog log, int ownerEntityId)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new CellCreatedKernelEvent(
            Tick: 0,
            WorldVersion: 1,
            Q: 0,
            R: 0,
            SourceCodeId: 5,
            TerrainId: 10,
            OwnerEntityId: ownerEntityId,
            SuperregionId: 3,
            ContinentId: 1,
            Z: 0))));
    }

    private static void WritePhysicalInfluenceChanged(
        KernelEventLog log,
        int entityId,
        float value,
        long worldVersion)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new CellInfluenceChangedKernelEvent(
            Tick: (int)worldVersion,
            WorldVersion: worldVersion,
            Q: 0,
            R: 0,
            SourceCodeId: 6,
            PlaneCode: PlaneCodes.Physical,
            EntityId: entityId,
            ActionId: 0,
            PreviousInfluence: 0f,
            CurrentInfluence: value))));
    }

    private static void WritePhysicalCultureChanged(KernelEventLog log)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new CellCultureInfluenceChangedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            Q: 0,
            R: 0,
            SourceCodeId: 6,
            PlaneCode: PlaneCodes.Physical,
            CultureId: 10,
            Z: 0,
            PreviousShare: 0f,
            CurrentShare: 80f))));
    }

    private static void WritePhysicalRoadChanged(KernelEventLog log)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new CellRoadChangedKernelEvent(
            Tick: 4,
            WorldVersion: 4,
            PlaneCode: PlaneCodes.Physical,
            Q: 0,
            R: 0,
            SourceCodeId: 7,
            TravelCount: 1,
            PreviousQuality: 0f,
            CurrentQuality: 60f))));
    }

    private static void WritePhysicalTravelStructureChanged(KernelEventLog log)
    {
        Assert.True(log.TryWrite(KernelEvent.From(new CellTravelStructureChangedKernelEvent(
            Tick: 5,
            WorldVersion: 5,
            PlaneCode: PlaneCodes.Physical,
            Q: 0,
            R: 0,
            SourceCodeId: 8,
            PreviousTravelStructureId: 0,
            CurrentTravelStructureId: 12))));
    }

    private static void AssertOverlayVersions(
        HexMapProjection projection,
        long culture,
        long influence,
        long road,
        long route)
    {
        Assert.Equal(culture, projection.CultureOverlayVersion);
        Assert.Equal(influence, projection.InfluenceOverlayVersion);
        Assert.Equal(road, projection.RoadNetworkVersion);
        Assert.Equal(route, projection.RouteNetworkVersion);
    }
}
