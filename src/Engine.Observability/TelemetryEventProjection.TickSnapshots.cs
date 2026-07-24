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

    private readonly record struct KindSnapshotKey(int KindId, int SubtypeId);

    private readonly record struct MapSnapshotKey(int PlaneCode, int TerrainId);

    private readonly record struct CoverageSnapshotKey(int PlaneCode, int OwnerEntityId);

    private readonly record struct CoverageTerrainSnapshotKey(int PlaneCode, int OwnerEntityId, int TerrainId);

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

    /// <summary>
    /// Cheap counters for mid-run progress reporting: active entities and
    /// physically covered cells as currently projected.
    /// </summary>
    public (int ActiveEntities, int CoveredCells) ProgressCounts()
    {
        var entityStats = TickEntityStatsFor();
        var mapStats = PhysicalMapStatsFor();
        return (entityStats.ActiveCount, mapStats.CoveredCells);
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

    private struct KindSnapshotAccumulator
    {
        private double _healthTotal;

        public int ActiveCount { get; private set; }

        public int HistoricalCount { get; private set; }

        public int HealthCount { get; private set; }

        public float MinimumHealth { get; private set; }

        public float MaximumHealth { get; private set; }

        public float AverageHealth => HealthCount == 0 ? 0f : (float)(_healthTotal / HealthCount);

        public void Add(int statusCode, float health)
        {
            ActiveCount += IsActiveStatus(statusCode) ? 1 : 0;
            HistoricalCount += IsHistoricalStatus(statusCode) ? 1 : 0;
            if (health <= 0f)
            {
                return;
            }

            if (HealthCount == 0)
            {
                MinimumHealth = health;
                MaximumHealth = health;
            }
            else
            {
                MinimumHealth = Math.Min(MinimumHealth, health);
                MaximumHealth = Math.Max(MaximumHealth, health);
            }

            HealthCount++;
            _healthTotal += health;
        }
    }

    private struct RelationshipSnapshotAccumulator
    {
        private double _total;

        public int Count { get; private set; }

        public float Minimum { get; private set; }

        public float Maximum { get; private set; }

        public float Average => Count == 0 ? 0f : (float)(_total / Count);

        public void Add(float strength)
        {
            if (Count == 0)
            {
                Minimum = strength;
                Maximum = strength;
            }
            else
            {
                Minimum = Math.Min(Minimum, strength);
                Maximum = Math.Max(Maximum, strength);
            }

            Count++;
            _total += strength;
        }
    }

    private struct MapSnapshotAccumulator
    {
        public int CellCount { get; private set; }

        public int CoveredCellCount { get; private set; }

        public int ActiveEntityCount { get; private set; }

        public void Add(int ownerEntityId, int activeEntityCount)
        {
            CellCount++;
            CoveredCellCount += ownerEntityId > 0 ? 1 : 0;
            ActiveEntityCount += activeEntityCount;
        }
    }

    private struct CoverageSnapshotAccumulator
    {
        public int CellCount { get; private set; }

        public int MinimumQ { get; private set; }

        public int MaximumQ { get; private set; }

        public int MinimumR { get; private set; }

        public int MaximumR { get; private set; }

        public int ActiveEntityCount { get; private set; }

        public void Add(int q, int r, int activeEntityCount)
        {
            if (CellCount == 0)
            {
                MinimumQ = q;
                MaximumQ = q;
                MinimumR = r;
                MaximumR = r;
            }
            else
            {
                MinimumQ = Math.Min(MinimumQ, q);
                MaximumQ = Math.Max(MaximumQ, q);
                MinimumR = Math.Min(MinimumR, r);
                MaximumR = Math.Max(MaximumR, r);
            }

            CellCount++;
            ActiveEntityCount += activeEntityCount;
        }
    }
}
