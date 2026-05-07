namespace Engine.Kernel;

internal sealed partial class MovementProgramRuntime
{
    private const int LocalPathSegmentSize = 5;
    private const int RegionalPathSegmentSize = 10;
    private const int LocalRoadMarkCadence = 10;
    private const int RegionalRoadMarkCadence = 25;
    private const float LowRoadMarkQuality = 25f;
    private const float HighRoadMarkQuality = 65f;

    private void ReconcileRoadMarks(Simulation simulation, int tick, int segmentSize)
    {
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

        var mark = RoadMarkForQuality(RoadQuality(planeCode, q, r, tick));
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

    private static int RoadMarkForQuality(float roadQuality)
    {
        if (roadQuality >= HighRoadMarkQuality)
        {
            return MovementProgramState.RoadMarkHigh;
        }

        return roadQuality >= LowRoadMarkQuality
            ? MovementProgramState.RoadMarkLow
            : MovementProgramState.RoadMarkNone;
    }
}
