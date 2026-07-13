namespace Engine.Observability;

internal sealed partial class TelemetryEventProjection
{
    private void RecordCoverageSnapshots(
        int tick,
        Dictionary<ObservedMapCellKey, int> activeEntitiesByCell)
    {
        AggregateCoverageSnapshots(activeEntitiesByCell);
        WriteCoverageSnapshots(tick);
    }

    private void AggregateCoverageSnapshots(Dictionary<ObservedMapCellKey, int> activeEntitiesByCell)
    {
        _coverageSnapshotAccumulators.Clear();
        _coverageSnapshotKeys.Clear();
        _coverageTerrainSnapshotCounts.Clear();
        _coverageTerrainSnapshotKeys.Clear();
        foreach (var cell in _mapCells.Values)
        {
            if (cell.OwnerEntityId <= 0)
            {
                continue;
            }

            var key = new CoverageSnapshotKey(cell.Key.PlaneCode, cell.OwnerEntityId);
            if (!_coverageSnapshotAccumulators.TryGetValue(key, out var aggregate))
            {
                _coverageSnapshotKeys.Add(key);
            }

            aggregate.Add(cell.Key.Q, cell.Key.R, activeEntitiesByCell.GetValueOrDefault(cell.Key));
            _coverageSnapshotAccumulators[key] = aggregate;

            var terrainKey = new CoverageTerrainSnapshotKey(
                cell.Key.PlaneCode,
                cell.OwnerEntityId,
                cell.TerrainId);
            if (!_coverageTerrainSnapshotCounts.TryGetValue(terrainKey, out var terrainCount))
            {
                _coverageTerrainSnapshotKeys.Add(terrainKey);
            }

            _coverageTerrainSnapshotCounts[terrainKey] = terrainCount + 1;
        }
    }

    private void WriteCoverageSnapshots(int tick)
    {
        for (var i = 0; i < _coverageSnapshotKeys.Count; i++)
        {
            var key = _coverageSnapshotKeys[i];
            var aggregate = _coverageSnapshotAccumulators[key];
            _batch.CoverageSnapshots.Add(new CoverageSnapshotRow(
                _runId,
                tick,
                key.PlaneCode,
                key.OwnerEntityId,
                CoverageKindCode: 1,
                aggregate.CellCount,
                aggregate.MinimumQ,
                aggregate.MaximumQ,
                aggregate.MinimumR,
                aggregate.MaximumR,
                aggregate.ActiveEntityCount));

            for (var terrainIndex = 0; terrainIndex < _coverageTerrainSnapshotKeys.Count; terrainIndex++)
            {
                var terrainKey = _coverageTerrainSnapshotKeys[terrainIndex];
                if (terrainKey.PlaneCode != key.PlaneCode || terrainKey.OwnerEntityId != key.OwnerEntityId)
                {
                    continue;
                }

                _batch.CoverageTerrainSnapshots.Add(new CoverageTerrainSnapshotRow(
                    _runId,
                    tick,
                    terrainKey.PlaneCode,
                    terrainKey.OwnerEntityId,
                    terrainKey.TerrainId,
                    _coverageTerrainSnapshotCounts[terrainKey]));
            }
        }
    }

    private Dictionary<ObservedMapCellKey, int> ActiveEntitiesByPlaneCell()
    {
        _activeEntitiesByCell.Clear();
        foreach (var entity in _entities.Values)
        {
            if (!IsActiveStatus(entity.StatusCode))
            {
                continue;
            }

            AddActiveEntityAt(new ObservedMapCellKey(PlaneCodes.Physical, entity.PhysicalQ, entity.PhysicalR));
            AddActiveEntityAt(new ObservedMapCellKey(PlaneCodes.Semantic, entity.SemanticQ, entity.SemanticR));
        }

        return _activeEntitiesByCell;
    }

    private void AddActiveEntityAt(ObservedMapCellKey key)
    {
        _activeEntitiesByCell.TryGetValue(key, out var count);
        _activeEntitiesByCell[key] = count + 1;
    }

    public void RecordGeneratorStateSnapshots(int tick)
    {
        if (!_recordedGeneratorSnapshotTicks.Add(tick))
        {
            return;
        }

        foreach (var state in _generatorStates.Values)
        {
            _batch.GeneratorStateSnapshots.Add(new GeneratorStateSnapshotRow(
                _runId,
                tick,
                state.GeneratorId,
                state.Enabled,
                Cadence: 0,
                CooldownRemaining: 0,
                state.RunCount,
                state.LastRunTick));
        }
    }
}
