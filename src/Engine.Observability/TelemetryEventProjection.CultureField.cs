namespace Engine.Observability;

internal sealed partial class TelemetryEventProjection
{
    private void RecordCoverageSnapshots(
        int tick,
        Dictionary<ObservedMapCellKey, int> activeEntitiesByCell)
    {
        foreach (var group in _mapCells.Values
                     .Where(cell => cell.OwnerEntityId > 0)
                     .GroupBy(cell => (cell.Key.PlaneCode, cell.OwnerEntityId)))
        {
            var cells = group.ToList();
            _batch.CoverageSnapshots.Add(new CoverageSnapshotRow(
                _runId,
                tick,
                group.Key.PlaneCode,
                group.Key.OwnerEntityId,
                CoverageKindCode: 1,
                cells.Count,
                cells.Min(cell => cell.Key.Q),
                cells.Max(cell => cell.Key.Q),
                cells.Min(cell => cell.Key.R),
                cells.Max(cell => cell.Key.R),
                cells.Sum(cell => activeEntitiesByCell.GetValueOrDefault(cell.Key))));

            foreach (var terrain in cells.GroupBy(cell => cell.TerrainId))
            {
                _batch.CoverageTerrainSnapshots.Add(new CoverageTerrainSnapshotRow(
                    _runId,
                    tick,
                    group.Key.PlaneCode,
                    group.Key.OwnerEntityId,
                    terrain.Key,
                    terrain.Count()));
            }
        }
    }

    private Dictionary<ObservedMapCellKey, int> ActiveEntitiesByPlaneCell()
    {
        var activeEntities = _entities.Values.Where(entity => IsActiveStatus(entity.StatusCode)).ToList();
        return activeEntities
            .SelectMany(entity => new[]
            {
                new ObservedMapCellKey(PlaneCodes.Physical, entity.PhysicalQ, entity.PhysicalR),
                new ObservedMapCellKey(PlaneCodes.Semantic, entity.SemanticQ, entity.SemanticR),
            })
            .GroupBy(key => key)
            .ToDictionary(group => group.Key, group => group.Count());
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
