namespace Engine.Observability;

using Engine.Constants;

internal sealed partial class TelemetryEventProjection
{
    private readonly record struct TickEntityStats(
        int ActiveCount,
        int HistoricalCount,
        float PhysicalSpread,
        float SemanticSpread);

    private readonly record struct PhysicalMapStats(
        int CoverageCount,
        int CoveredCells,
        int UnknownCells);

    public void RecordTickSnapshot(int tick)
    {
        var entityStats = TickEntityStatsFor();
        var mapStats = PhysicalMapStatsFor();

        _batch.TickSnapshots.Add(new TickSnapshotRow(
            _runId,
            tick,
            entityStats.ActiveCount,
            entityStats.HistoricalCount,
            _totalGeneratorFires,
            ActiveRelationshipCount(),
            mapStats.CoverageCount,
            mapStats.CoveredCells,
            mapStats.UnknownCells,
            entityStats.PhysicalSpread,
            entityStats.SemanticSpread));
    }

    private TickEntityStats TickEntityStatsFor()
    {
        var activeCount = 0;
        var historicalCount = 0;
        var physicalSpread = new CubeSpreadBounds();
        var semanticSpread = new CubeSpreadBounds();
        foreach (var entity in _entities.Values)
        {
            if (IsHistoricalStatus(entity.StatusCode))
            {
                historicalCount++;
                continue;
            }

            if (!IsActiveStatus(entity.StatusCode))
            {
                continue;
            }

            activeCount++;
            physicalSpread.Add(entity.PhysicalQ, entity.PhysicalR, -entity.PhysicalQ - entity.PhysicalR);
            semanticSpread.Add(entity.SemanticQ, entity.SemanticR, entity.SemanticS);
        }

        return new TickEntityStats(
            activeCount,
            historicalCount,
            physicalSpread.Spread,
            semanticSpread.Spread);
    }

    private int ActiveRelationshipCount()
    {
        var count = 0;
        foreach (var relationship in _relationships.Values)
        {
            count += RelationshipIsHotActive(relationship) ? 1 : 0;
        }

        return count;
    }

    private PhysicalMapStats PhysicalMapStatsFor()
    {
        var coverageOwners = new HashSet<int>();
        var physicalCellCount = 0;
        var coveredCells = 0;
        foreach (var cell in _mapCells.Values)
        {
            if (cell.Key.PlaneCode != PlaneCodes.Physical)
            {
                continue;
            }

            physicalCellCount++;
            if (cell.OwnerEntityId <= 0)
            {
                continue;
            }

            coveredCells++;
            coverageOwners.Add(cell.OwnerEntityId);
        }

        return new PhysicalMapStats(coverageOwners.Count, coveredCells, physicalCellCount - coveredCells);
    }

    private struct CubeSpreadBounds
    {
        private bool _hasValue;
        private int _minQ;
        private int _maxQ;
        private int _minR;
        private int _maxR;
        private int _minS;
        private int _maxS;

        public int Spread =>
            !_hasValue
                ? 0
                : Math.Max(Math.Max(_maxQ - _minQ, _maxR - _minR), _maxS - _minS);

        public void Add(int q, int r, int s)
        {
            if (!_hasValue)
            {
                _hasValue = true;
                _minQ = q;
                _maxQ = q;
                _minR = r;
                _maxR = r;
                _minS = s;
                _maxS = s;
                return;
            }

            _minQ = Math.Min(_minQ, q);
            _maxQ = Math.Max(_maxQ, q);
            _minR = Math.Min(_minR, r);
            _maxR = Math.Max(_maxR, r);
            _minS = Math.Min(_minS, s);
            _maxS = Math.Max(_maxS, s);
        }
    }
}
