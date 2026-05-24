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
        var segmentGridWidth = SegmentGridWidth(world.MapRadius, segmentSize);
        if (segmentGridWidth <= 0)
        {
            return;
        }

        State.BeginRoadMarkReconciliation(segmentSize);
        for (var index = 0; index < State.RoadCellCount; index++)
        {
            State.GetRoadCell(index, out var planeCode, out var coordinateKey, out var quality, out var lastTick, out var travelCount);
            _ = quality;
            _ = lastTick;
            _ = travelCount;
            ReconcileRoadMarkCell(world, planeCode, coordinateKey, tick, segmentSize, segmentGridWidth);
        }

        State.CommitRoadMarkReconciliation(segmentSize);
        State.MarkRoadMarksClean(segmentSize);
    }

    private void ReconcileRoadMarkCell(
        WorldState world,
        int planeCode,
        int coordinateKey,
        int tick,
        int segmentSize,
        int segmentGridWidth)
    {
        if (!world.TryResolveCoordinateKey(
                coordinateKey,
                planeCode == PlaneCodes.Semantic ? 4 : 0,
                out var q,
                out var r))
        {
            return;
        }

        var mark = MovementProgramState.RoadMarkForQuality(RoadQuality(planeCode, q, r, tick));
        if (mark == MovementProgramState.RoadMarkNone)
        {
            return;
        }

        var segment = SegmentIndex(world.MapRadius, segmentSize, segmentGridWidth, q, r);
        if (segment >= 0)
        {
            State.AccumulateRoadMark(planeCode, segmentSize, segment, mark);
        }
    }
}
