namespace Engine.Host;

using Engine.Constants;
using Engine.Kernel;
using Engine.Observability;
using Engine.Projection;

public sealed partial class KernelHost
{
    private static readonly int[] CultureLocalityPlaneCodes =
    [
        PlaneCodes.Physical,
        PlaneCodes.Semantic,
    ];

    public long CultureLocalityRevision => _kernel.CultureLocality.Revision;

    public void RefreshCultureLocality(HexMapProjection map, bool incremental = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(map);
        var world = _kernel.World;
        var influences = new List<HexCultureInfluenceProjection>(Math.Max(1, world.CultureHighWatermark));
        foreach (var planeCode in CultureLocalityPlaneCodes)
        {
            if (incremental)
            {
                var count = _kernel.CultureLocality.DrainDirtyCells(
                    world,
                    planeCode,
                    _cultureLocalityRefreshCells);
                for (var i = 0; i < count; i++)
                {
                    var cell = _cultureLocalityRefreshCells[i];
                    RefreshCultureLocalityCell(map, world, influences, cell.PlaneCode, cell.Q, cell.R);
                }
            }
            else
            {
                foreach (var coord in KnownCells(world, planeCode))
                {
                    RefreshCultureLocalityCell(map, world, influences, planeCode, coord.Q, coord.R);
                }
            }
        }
    }

    private void RefreshCultureLocalityCell(
        HexMapProjection map,
        WorldState world,
        List<HexCultureInfluenceProjection> influences,
        int planeCode,
        int q,
        int r)
    {
        influences.Clear();
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
        {
            var locality = _kernel.CultureLocality.CellAt(world, cultureId, planeCode, q, r);
            if (locality.RawMass <= FixedScalar.Zero && locality.Share <= FixedScalar.Zero)
            {
                continue;
            }

            influences.Add(ToHexCultureInfluence(locality, CurrentTick, CurrentVersion));
        }

        map.SetCultureInfluencesFromLocality(
            planeCode,
            new HexCoord(q, r),
            influences,
            CurrentTick,
            CurrentVersion);
    }

    public void RecordCultureLocalityTelemetry(
        TelemetryBatch batch,
        int runId,
        int tick)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);
        RecordCultureFieldCellSnapshots(batch, runId, tick);
    }

    private void RecordCultureFieldCellSnapshots(TelemetryBatch batch, int runId, int tick)
    {
        var world = _kernel.World;
        foreach (var planeCode in CultureLocalityPlaneCodes)
        {
            for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
            {
                RecordCultureFieldCellSnapshots(batch, runId, tick, world, cultureId, planeCode);
            }
        }
    }

    private static void RecordCultureFieldCellSnapshots(
        TelemetryBatch batch,
        int runId,
        int tick,
        WorldState world,
        int cultureId,
        int planeCode)
    {
        var cursor = world.RawCultureInfluenceCellsOf(cultureId, planeCode);
        while (world.MoveNextRawCultureInfluenceCell(ref cursor, out var q, out var r, out var z, out var raw))
        {
            var total = FixedScalar.Zero;
            for (var otherCultureId = 1; otherCultureId <= world.CultureHighWatermark; otherCultureId++)
            {
                total += world.GetCellCultureInfluenceRaw(otherCultureId, planeCode, q, r, z);
            }

            batch.CultureFieldCellSnapshots.Add(new CultureFieldCellSnapshotRow(
                runId,
                tick,
                cultureId,
                planeCode,
                q,
                r,
                world.GetCellCultureInfluence(cultureId, planeCode, q, r, z).ToSingle(),
                raw.ToSingle(),
                total.ToSingle(),
                z));
        }
    }

    private static HexCultureInfluenceProjection ToHexCultureInfluence(
        in CultureLocalityCell locality,
        int tick,
        long worldVersion) =>
        new(
            locality.CultureId,
            locality.Share.ToSingle(),
            tick,
            worldVersion,
            locality.RawMass.ToSingle(),
            locality.TotalRawMass.ToSingle(),
            locality.ForeignCultureId,
            locality.ForeignShare.ToSingle(),
            locality.ForeignRawMass.ToSingle(),
            locality.CellKindCode,
            locality.StrengthBandCode);

    private static IEnumerable<(int Q, int R)> KnownCells(WorldState world, int planeCode)
    {
        var radius = world.MapRadiusForPlane(planeCode);
        for (var q = -radius; q <= radius; q++)
        {
            var minR = Math.Max(-radius, -q - radius);
            var maxR = Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++)
            {
                if (world.CellIsKnown(planeCode, q, r))
                {
                    yield return (q, r);
                }
            }
        }
    }

}
