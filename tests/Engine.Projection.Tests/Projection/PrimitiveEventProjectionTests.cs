namespace Engine.Projection.Tests.Projection;

using Engine.Eventing;
using Engine.Projection;

public sealed class PrimitiveEventProjectionTests
{
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
            ContinentId: 1))));
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
            ContinentId: 1))));

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
            ContinentId: 1))));
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
            PreviousShare: 0f,
            CurrentShare: 33.333f))));

        pump.DrainAvailable();

        Assert.True(projection.TryGetHex(PlaneCodes.Physical, new HexCoord(0, 0), out var cell));
        Assert.Equal([10, 20], cell.CultureInfluences.Select(influence => influence.CultureId).ToArray());
        Assert.Equal([66.667f, 33.333f], cell.CultureInfluences.Select(influence => influence.Share).ToArray());
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
            ContinentId: 1))));
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
            ContinentId: 1))));
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
}
