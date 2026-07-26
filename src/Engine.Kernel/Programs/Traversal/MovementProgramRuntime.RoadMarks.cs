namespace Engine.Kernel;

internal sealed partial class MovementProgramRuntime
{
    private const int LocalPathSegmentSize = 5;
    private const int RegionalPathSegmentSize = 10;
    private const int LocalRoadMarkCadence = 10;
    private const int RegionalRoadMarkCadence = 25;

    private void ReconcileRoadMarks(Simulation simulation, int tick, int segmentSize)
    {
        if (!State.RoadMarksDirty(segmentSize))
        {
            return;
        }

        var world = simulation.World;
        State.BeginRoadMarkReconciliation(segmentSize);
        for (var index = 0; index < State.RoadCellCount; index++)
        {
            State.GetRoadCell(index, out var cellId, out var quality, out var lastTick, out var travelCount);
            _ = quality;
            _ = lastTick;
            _ = travelCount;
            ReconcileRoadMarkCell(world, cellId, tick, segmentSize);
        }

        State.CommitRoadMarkReconciliation(segmentSize);
        State.MarkRoadMarksClean(segmentSize);
    }

    private void ReconcileRoadMarkCell(
        WorldState world,
        int cellId,
        int tick,
        int segmentSize)
    {
        var planeCode = world.CellPlaneCode(cellId);
        var q = world.CellQ(cellId);
        var r = world.CellR(cellId);

        var mark = MovementProgramState.RoadMarkForQuality(RoadQuality(planeCode, q, r, tick));
        if (mark == MovementProgramState.RoadMarkNone)
        {
            return;
        }

        var radius = SegmentFrameRadius(
            world.MapRadiusForPlane(planeCode),
            world.MapSegmentOriginRadiusForPlane(planeCode),
            segmentSize);
        var segmentGridWidth = SegmentGridWidth(radius, segmentSize);
        var segment = SegmentIndex(radius, segmentSize, segmentGridWidth, q, r);
        if (segment >= 0)
        {
            State.AccumulateRoadMark(planeCode, segmentSize, segment, mark);
        }
    }
}
