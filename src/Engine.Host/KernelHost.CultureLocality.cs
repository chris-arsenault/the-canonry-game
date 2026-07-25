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
        int tick,
        bool includeEntitySnapshots)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);
        RecordCultureTopologySnapshots(batch, runId, tick);
        if (includeEntitySnapshots)
        {
            RecordCultureEntityLocalitySnapshots(batch, runId, tick);
        }
    }

    public void RecordCultureLocalityBirthTelemetry(
        TelemetryBatch batch,
        int runId,
        ISet<int> recordedEntityIds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);
        ArgumentNullException.ThrowIfNull(recordedEntityIds);
        var world = _kernel.World;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!world.EntityExists(entityId) ||
                recordedEntityIds.Contains(entityId) ||
                world.EntityCreatedAt(entityId) > CurrentTick)
            {
                continue;
            }

            recordedEntityIds.Add(entityId);
            var cultureId = world.EntityCultureId(entityId);
            if (cultureId <= 0)
            {
                continue;
            }

            var physical = _kernel.CultureLocality.CellAt(
                world,
                cultureId,
                PlaneCodes.Physical,
                world.EntityPhysicalQ(entityId),
                world.EntityPhysicalR(entityId));
            var semantic = _kernel.CultureLocality.CellAt(
                world,
                cultureId,
                PlaneCodes.Semantic,
                world.EntitySemanticQ(entityId),
                world.EntitySemanticR(entityId));
            batch.CultureEntityBirthFieldEvents.Add(new CultureEntityBirthFieldEventRow(
                runId,
                world.EntityCreatedAt(entityId),
                entityId,
                cultureId,
                physical.StrengthBandCode,
                physical.CellKindCode,
                physical.LocalRawMass.ToSingle(),
                physical.ConnectedRawMass.ToSingle(),
                semantic.StrengthBandCode,
                semantic.CellKindCode,
                semantic.LocalRawMass.ToSingle(),
                semantic.ConnectedRawMass.ToSingle()));
        }
    }

    // Topology is sampled in the host at telemetry cadence; it is not cached
    // or rebuilt by the deterministic kernel.
    private void RecordCultureTopologySnapshots(TelemetryBatch batch, int runId, int tick)
    {
        var world = _kernel.World;
        foreach (var planeCode in CultureLocalityPlaneCodes)
        {
            for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
            {
                RecordCultureTopologySnapshot(batch, runId, tick, world, cultureId, planeCode);
            }
        }
    }

    private void RecordCultureTopologySnapshot(
        TelemetryBatch batch,
        int runId,
        int tick,
        WorldState world,
        int cultureId,
        int planeCode)
    {
        RecordCultureFieldCellSnapshots(batch, runId, tick, world, cultureId, planeCode);
        var supported = new HashSet<HexCoord>();
        var stats = MeasureCultureField(world, cultureId, planeCode, supported);
        MeasureComponents(supported, ref stats);
        if (stats.InfluencedCellCount == 0 && stats.ComponentCount == 0 && stats.HoleCellCount == 0)
        {
            return;
        }

        batch.CultureFieldComponentSnapshots.Add(new CultureFieldComponentSnapshotRow(
            runId,
            tick,
            planeCode,
            cultureId,
            stats.InfluencedCellCount,
            stats.ComponentCount,
            stats.LargestComponentCellCount,
            stats.LargestComponentShare,
            stats.InteriorCellCount,
            stats.BorderCellCount,
            stats.IsolatedCellCount,
            stats.ContestedCellCount,
            stats.HoleCellCount));
    }

    private CultureTopologyStats MeasureCultureField(
        WorldState world,
        int cultureId,
        int planeCode,
        HashSet<HexCoord> supported)
    {
        var stats = new CultureTopologyStats();
        var cursor = world.CultureInfluenceCells(cultureId, planeCode);
        while (world.MoveNextCultureInfluenceCell(ref cursor, out var q, out var r, out _))
        {
            AddCultureFieldCell(world, cultureId, planeCode, q, r, supported, ref stats);
        }

        foreach (var coord in KnownCells(world, planeCode))
        {
            stats.HoleCellCount += _kernel.CultureLocality.IsInteriorHoleAt(
                world,
                cultureId,
                planeCode,
                coord.Q,
                coord.R)
                ? 1
                : 0;
        }

        return stats;
    }

    private void AddCultureFieldCell(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r,
        HashSet<HexCoord> supported,
        ref CultureTopologyStats stats)
    {
        var locality = _kernel.CultureLocality.CellAt(world, cultureId, planeCode, q, r);
        if (locality.RawMass <= FixedScalar.Zero && locality.Share <= FixedScalar.Zero)
        {
            return;
        }

        stats.AddInfluencedCell(locality.CellKindCode);
        if (locality.RawMass >= WorldState.CultureClaimedRawMass)
        {
            supported.Add(new HexCoord(q, r));
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

    private static void MeasureComponents(HashSet<HexCoord> supported, ref CultureTopologyStats stats)
    {
        var remaining = new HashSet<HexCoord>(supported);
        var pending = new Queue<HexCoord>();
        while (remaining.Count > 0)
        {
            var start = remaining.First();
            remaining.Remove(start);
            pending.Enqueue(start);
            var cellCount = 0;
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                cellCount++;
                for (var direction = 0; direction < 6; direction++)
                {
                    var neighbor = new HexCoord(
                        current.Q + DirectionQ(direction),
                        current.R + DirectionR(direction));
                    if (remaining.Remove(neighbor))
                    {
                        pending.Enqueue(neighbor);
                    }
                }
            }

            stats.ComponentCount++;
            stats.LargestComponentCellCount = Math.Max(stats.LargestComponentCellCount, cellCount);
        }
    }

    private void RecordCultureEntityLocalitySnapshots(TelemetryBatch batch, int runId, int tick)
    {
        var world = _kernel.World;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!world.EntityExists(entityId))
            {
                continue;
            }

            var cultureId = world.EntityCultureId(entityId);
            if (cultureId <= 0)
            {
                continue;
            }

            RecordCultureEntityLocalitySnapshot(batch, runId, tick, entityId, cultureId, PlaneCodes.Physical);
            RecordCultureEntityLocalitySnapshot(batch, runId, tick, entityId, cultureId, PlaneCodes.Semantic);
        }
    }

    private void RecordCultureEntityLocalitySnapshot(
        TelemetryBatch batch,
        int runId,
        int tick,
        int entityId,
        int cultureId,
        int planeCode)
    {
        var world = _kernel.World;
        var q = planeCode == PlaneCodes.Physical
            ? world.EntityPhysicalQ(entityId)
            : world.EntitySemanticQ(entityId);
        var r = planeCode == PlaneCodes.Physical
            ? world.EntityPhysicalR(entityId)
            : world.EntitySemanticR(entityId);
        var locality = _kernel.CultureLocality.CellAt(world, cultureId, planeCode, q, r);
        batch.CultureEntityFieldSnapshots.Add(new CultureEntityFieldSnapshotRow(
            runId,
            tick,
            entityId,
            cultureId,
            planeCode,
            locality.CellKindCode,
            _kernel.CultureSlots.CausalSpendLimitMultiplier(entityId) > 1,
            locality.LocalRawMass.ToSingle(),
            locality.ConnectedRawMass.ToSingle(),
            world.EntityInfluenceProduction(entityId, planeCode).ToSingle(),
            locality.StrengthBandCode,
            locality.RawMass.ToSingle(),
            locality.TotalRawMass.ToSingle(),
            locality.ForeignCultureId,
            locality.ForeignShare.ToSingle(),
            locality.ForeignRawMass.ToSingle()));
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

    private static int DirectionQ(int direction) => direction switch
    {
        0 => 1,
        1 => 1,
        2 => 0,
        3 => -1,
        4 => -1,
        _ => 0,
    };

    private static int DirectionR(int direction) => direction switch
    {
        0 => 0,
        1 => -1,
        2 => -1,
        3 => 0,
        4 => 1,
        _ => 1,
    };

    private struct CultureTopologyStats
    {
        public int InfluencedCellCount;
        public int ComponentCount;
        public int LargestComponentCellCount;
        public int InteriorCellCount;
        public int BorderCellCount;
        public int IsolatedCellCount;
        public int ContestedCellCount;
        public int HoleCellCount;

        public readonly float LargestComponentShare =>
            InfluencedCellCount == 0 ? 0f : LargestComponentCellCount * 100f / InfluencedCellCount;

        public void AddInfluencedCell(int cellKindCode)
        {
            InfluencedCellCount++;
            InteriorCellCount += cellKindCode == CultureFieldCellKindCodes.Interior ? 1 : 0;
            BorderCellCount += cellKindCode == CultureFieldCellKindCodes.Border ? 1 : 0;
            IsolatedCellCount += cellKindCode == CultureFieldCellKindCodes.Isolated ? 1 : 0;
            ContestedCellCount += cellKindCode == CultureFieldCellKindCodes.Contested ? 1 : 0;
        }
    }
}
