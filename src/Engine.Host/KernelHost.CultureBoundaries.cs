namespace Engine.Host;

using Engine.Constants;
using Engine.Kernel;
using Engine.Observability;
using Engine.Projection;

public sealed partial class KernelHost
{
    private const int CultureBoundaryIncrementalRefreshRadius = 2;

    private static readonly int[] CultureBoundaryPlaneCodes =
    [
        PlaneCodes.Physical,
        PlaneCodes.Semantic,
    ];

    private readonly HashSet<(int PlaneCode, int Q, int R)> _cultureBoundaryRefreshCellSet = [];
    private readonly List<(int PlaneCode, int Q, int R)> _cultureBoundaryRefreshCells = [];

    public long CultureBoundaryRevision => _kernel.CultureBoundaries.Revision;

    public void RefreshCultureBoundaries(HexMapProjection map, bool incremental = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(map);

        var world = _kernel.World;
        var influences = new List<HexCultureInfluenceProjection>(Math.Max(1, world.CultureHighWatermark));
        foreach (var planeCode in CultureBoundaryPlaneCodes)
        {
            if (incremental)
            {
                RefreshCultureBoundaryPlaneIncremental(map, world, influences, planeCode);
            }
            else
            {
                foreach (var coord in KnownCells(world, planeCode))
                {
                    RefreshCultureBoundaryCell(map, world, influences, planeCode, coord.Q, coord.R);
                }
            }
        }
    }

    private void RefreshCultureBoundaryPlaneIncremental(
        HexMapProjection map,
        WorldState world,
        List<HexCultureInfluenceProjection> influences,
        int planeCode)
    {
        _cultureBoundaryRefreshCellSet.Clear();
        _cultureBoundaryRefreshCells.Clear();
        foreach (var hexId in map.DirtyCellIds(planeCode))
        {
            if (!map.TryGetHex(planeCode, hexId, out var hex))
            {
                continue;
            }

            AddCultureBoundaryRefreshCells(world, planeCode, hex.Coord.Q, hex.Coord.R);
        }

        for (var i = 0; i < _cultureBoundaryRefreshCells.Count; i++)
        {
            var cell = _cultureBoundaryRefreshCells[i];
            RefreshCultureBoundaryCell(map, world, influences, cell.PlaneCode, cell.Q, cell.R);
        }
    }

    private void AddCultureBoundaryRefreshCells(WorldState world, int planeCode, int centerQ, int centerR)
    {
        for (var dq = -CultureBoundaryIncrementalRefreshRadius; dq <= CultureBoundaryIncrementalRefreshRadius; dq++)
        {
            var minDr = Math.Max(
                -CultureBoundaryIncrementalRefreshRadius,
                -dq - CultureBoundaryIncrementalRefreshRadius);
            var maxDr = Math.Min(
                CultureBoundaryIncrementalRefreshRadius,
                -dq + CultureBoundaryIncrementalRefreshRadius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = centerQ + dq;
                var r = centerR + dr;
                if (!world.CellIsKnown(planeCode, q, r) ||
                    !_cultureBoundaryRefreshCellSet.Add((planeCode, q, r)))
                {
                    continue;
                }

                _cultureBoundaryRefreshCells.Add((planeCode, q, r));
            }
        }
    }

    private void RefreshCultureBoundaryCell(
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
            var boundary = _kernel.CultureBoundaries.CellAt(world, cultureId, planeCode, q, r);
            if (boundary.RawMass <= 0f && boundary.Share <= 0f)
            {
                continue;
            }

            influences.Add(ToHexCultureInfluence(boundary, CurrentTick, CurrentVersion));
        }

        map.SetCultureInfluencesFromBoundaries(
            planeCode,
            new HexCoord(q, r),
            influences,
            CurrentTick,
            CurrentVersion);
    }

    public void RecordCultureBoundaryTelemetry(
        TelemetryBatch batch,
        int runId,
        int tick,
        bool includeEntitySnapshots)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);

        RecordCultureBoundaryComponentSnapshots(batch, runId, tick);
        if (includeEntitySnapshots)
        {
            RecordCultureEntityBoundarySnapshots(batch, runId, tick);
        }
    }

    public void RecordCultureBoundaryBirthTelemetry(
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

            var physical = _kernel.CultureBoundaries.CellAt(
                world,
                cultureId,
                PlaneCodes.Physical,
                world.EntityPhysicalQ(entityId),
                world.EntityPhysicalR(entityId));
            var semantic = _kernel.CultureBoundaries.CellAt(
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
                physical.LocalRawMass,
                physical.ConnectedRawMass,
                semantic.StrengthBandCode,
                semantic.CellKindCode,
                semantic.LocalRawMass,
                semantic.ConnectedRawMass));
        }
    }

    private void RecordCultureBoundaryComponentSnapshots(TelemetryBatch batch, int runId, int tick)
    {
        var world = _kernel.World;
        foreach (var planeCode in CultureBoundaryPlaneCodes)
        {
            for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
            {
                RecordCultureBoundaryComponentSnapshot(batch, runId, tick, cultureId, planeCode);
            }
        }
    }

    private void RecordCultureBoundaryComponentSnapshot(
        TelemetryBatch batch,
        int runId,
        int tick,
        int cultureId,
        int planeCode)
    {
        var stats = CultureBoundaryComponentTelemetryStatsFor(cultureId, planeCode);
        var componentCount = _kernel.CultureBoundaries.ComponentCount(_kernel.World, cultureId, planeCode);
        if (stats.InfluencedCellCount == 0 && componentCount == 0 && stats.HoleCellCount == 0)
        {
            return;
        }

        batch.CultureFieldComponentSnapshots.Add(new CultureFieldComponentSnapshotRow(
            runId,
            tick,
            planeCode,
            cultureId,
            stats.InfluencedCellCount,
            componentCount,
            stats.LargestComponentCellCount,
            stats.LargestComponentShare,
            stats.InteriorCellCount,
            stats.BorderCellCount,
            stats.IsolatedCellCount,
            stats.ContestedCellCount,
            stats.HoleCellCount));
    }

    private CultureBoundaryComponentTelemetryStats CultureBoundaryComponentTelemetryStatsFor(
        int cultureId,
        int planeCode)
    {
        var stats = new CultureBoundaryComponentTelemetryStats();
        AddInfluencedCellStats(cultureId, planeCode, ref stats);
        AddInteriorHoleStats(cultureId, planeCode, ref stats);
        return stats;
    }

    private void AddInfluencedCellStats(
        int cultureId,
        int planeCode,
        ref CultureBoundaryComponentTelemetryStats stats)
    {
        var world = _kernel.World;
        var cursor = world.CultureInfluenceCells(cultureId, planeCode);
        while (world.MoveNextCultureInfluenceCell(ref cursor, out var q, out var r, out _))
        {
            var boundary = _kernel.CultureBoundaries.CellAt(world, cultureId, planeCode, q, r);
            if (boundary.RawMass > 0f || boundary.Share > 0f)
            {
                stats.AddInfluencedCell(boundary);
            }
        }
    }

    private void AddInteriorHoleStats(
        int cultureId,
        int planeCode,
        ref CultureBoundaryComponentTelemetryStats stats)
    {
        var world = _kernel.World;
        foreach (var coord in KnownCells(world, planeCode))
        {
            if (_kernel.CultureBoundaries.IsInteriorHoleAt(world, cultureId, planeCode, coord.Q, coord.R))
            {
                stats.AddInteriorHole();
            }
        }
    }

    private void RecordCultureEntityBoundarySnapshots(TelemetryBatch batch, int runId, int tick)
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

            RecordCultureEntityBoundarySnapshot(
                batch,
                runId,
                tick,
                entityId,
                cultureId,
                PlaneCodes.Physical,
                world.EntityPhysicalQ(entityId),
                world.EntityPhysicalR(entityId));
            RecordCultureEntityBoundarySnapshot(
                batch,
                runId,
                tick,
                entityId,
                cultureId,
                PlaneCodes.Semantic,
                world.EntitySemanticQ(entityId),
                world.EntitySemanticR(entityId));
        }
    }

    private void RecordCultureEntityBoundarySnapshot(
        TelemetryBatch batch,
        int runId,
        int tick,
        int entityId,
        int cultureId,
        int planeCode,
        int q,
        int r)
    {
        var boundary = _kernel.CultureBoundaries.CellAt(_kernel.World, cultureId, planeCode, q, r);
        batch.CultureEntityFieldSnapshots.Add(new CultureEntityFieldSnapshotRow(
            runId,
            tick,
            entityId,
            cultureId,
            planeCode,
            boundary.CellKindCode,
            _kernel.CultureSlots.CausalSpendLimitMultiplier(entityId) > 1,
            boundary.LocalRawMass,
            boundary.ConnectedRawMass,
            _kernel.World.EntityInfluenceProduction(entityId, planeCode),
            boundary.StrengthBandCode,
            boundary.RawMass,
            boundary.TotalRawMass,
            boundary.ComponentId,
            boundary.ComponentCellCount,
            boundary.ComponentRawMass,
            boundary.ForeignCultureId,
            boundary.ForeignShare,
            boundary.ForeignRawMass));
    }

    private static HexCultureInfluenceProjection ToHexCultureInfluence(
        in CultureBoundaryCell boundary,
        int tick,
        long worldVersion) =>
        new(
            boundary.CultureId,
            boundary.Share,
            tick,
            worldVersion,
            boundary.RawMass,
            boundary.TotalRawMass,
            boundary.ForeignCultureId,
            boundary.ForeignShare,
            boundary.ForeignRawMass,
            boundary.CellKindCode,
            boundary.StrengthBandCode,
            boundary.ComponentId,
            boundary.ComponentCellCount,
            boundary.ComponentRawMass);

    private static IEnumerable<(int Q, int R)> KnownCells(WorldState world, int planeCode)
    {
        var radius = world.MapRadius;
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

    private struct CultureBoundaryComponentTelemetryStats
    {
        public int InfluencedCellCount { get; private set; }

        public int LargestComponentCellCount { get; private set; }

        public int InteriorCellCount { get; private set; }

        public int BorderCellCount { get; private set; }

        public int IsolatedCellCount { get; private set; }

        public int ContestedCellCount { get; private set; }

        public int HoleCellCount { get; private set; }

        public float LargestComponentShare =>
            InfluencedCellCount == 0 ? 0f : LargestComponentCellCount * 100f / InfluencedCellCount;

        public void AddInfluencedCell(in CultureBoundaryCell boundary)
        {
            InfluencedCellCount++;
            if (boundary.IsLargestComponent)
            {
                LargestComponentCellCount = Math.Max(LargestComponentCellCount, boundary.ComponentCellCount);
            }

            AddCellKind(boundary.CellKindCode);
        }

        public void AddInteriorHole()
        {
            HoleCellCount++;
        }

        private void AddCellKind(int cellKindCode)
        {
            InteriorCellCount += cellKindCode == CultureFieldCellKindCodes.Interior ? 1 : 0;
            BorderCellCount += cellKindCode == CultureFieldCellKindCodes.Border ? 1 : 0;
            IsolatedCellCount += cellKindCode == CultureFieldCellKindCodes.Isolated ? 1 : 0;
            ContestedCellCount += cellKindCode == CultureFieldCellKindCodes.Contested ? 1 : 0;
        }
    }
}
