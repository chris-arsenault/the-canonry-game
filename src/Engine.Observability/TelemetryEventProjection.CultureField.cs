namespace Engine.Observability;

using Engine.Eventing;

internal sealed partial class TelemetryEventProjection
{
    private const float CultureFieldTraceRawMass = 2.5f;
    private const float CultureFieldSupportedRawMass = 12f;
    private const float CultureFieldStrongRawMass = 30f;
    private const float CultureFieldRingTwoConnectionWeight = 7f / 20f;
    private const float CultureFieldForeignContestShare = 25f;
    private const float CultureFieldDominantOwnShare = 65f;
    private const float CultureFieldMajorForeignContestShare = 35f;

    private readonly record struct CultureFieldComponentStats(
        int ComponentCount,
        int LargestComponentCellCount);

    private readonly record struct CultureFieldCellKindStats(
        int InteriorCellCount,
        int BorderCellCount,
        int IsolatedCellCount,
        int ContestedCellCount);

    private readonly record struct CultureFieldComponent(
        int ComponentId,
        int CellCount,
        float RawMass);

    private readonly record struct CultureFieldNeighborhood(
        float AdjacentRaw,
        float RingTwoRaw,
        int SupportedAdjacentCount);

    private readonly record struct CultureFieldComponentCacheKey(
        int CultureId,
        int PlaneCode,
        int Q,
        int R);

    private readonly Dictionary<CultureFieldComponentCacheKey, CultureFieldComponent> _cultureFieldComponentCache = [];
    private int _cultureFieldComponentCacheTick = -1;

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

    private void RecordCultureFieldComponentSnapshots(int tick)
    {
        foreach (var group in _cultureFields.Values
                     .Where(field => field.CurrentRawInfluence > 0f || field.CurrentShare > 0f)
                     .GroupBy(field => (field.Key.PlaneCode, field.Key.CultureId)))
        {
            var fields = group.ToDictionary(field => new ObservedMapCellKey(field.Key.PlaneCode, field.Key.Q, field.Key.R));
            var componentStats = CultureFieldComponentStatsFor(fields);
            var kindStats = CultureFieldCellKindStatsFor(group.Key.CultureId, group.Key.PlaneCode, fields.Keys);
            var influencedCellCount = fields.Count;
            _batch.CultureFieldComponentSnapshots.Add(new CultureFieldComponentSnapshotRow(
                _runId,
                tick,
                group.Key.PlaneCode,
                group.Key.CultureId,
                influencedCellCount,
                componentStats.ComponentCount,
                componentStats.LargestComponentCellCount,
                influencedCellCount == 0 ? 0f : componentStats.LargestComponentCellCount * 100f / influencedCellCount,
                kindStats.InteriorCellCount,
                kindStats.BorderCellCount,
                kindStats.IsolatedCellCount,
                kindStats.ContestedCellCount));
        }
    }

    private static CultureFieldComponentStats CultureFieldComponentStatsFor(
        Dictionary<ObservedMapCellKey, ObservedCultureFieldProjection> fields)
    {
        var visited = new HashSet<ObservedMapCellKey>();
        var componentCount = 0;
        var largestComponent = 0;
        foreach (var key in fields.Keys)
        {
            if (!visited.Add(key))
            {
                continue;
            }

            componentCount++;
            largestComponent = Math.Max(largestComponent, CultureFieldComponentSize(key, fields, visited));
        }

        return new CultureFieldComponentStats(componentCount, largestComponent);
    }

    private CultureFieldCellKindStats CultureFieldCellKindStatsFor(
        int cultureId,
        int planeCode,
        IEnumerable<ObservedMapCellKey> keys)
    {
        var interior = 0;
        var border = 0;
        var isolated = 0;
        var contested = 0;
        foreach (var key in keys)
        {
            var cellKindCode = CultureFieldAt(cultureId, planeCode, key.Q, key.R).CellKindCode;
            interior += cellKindCode == CultureFieldCellKindCodes.Interior ? 1 : 0;
            border += cellKindCode == CultureFieldCellKindCodes.Border ? 1 : 0;
            isolated += cellKindCode == CultureFieldCellKindCodes.Isolated ? 1 : 0;
            contested += cellKindCode == CultureFieldCellKindCodes.Contested ? 1 : 0;
        }

        return new CultureFieldCellKindStats(interior, border, isolated, contested);
    }

    private static int CultureFieldComponentSize(
        ObservedMapCellKey start,
        Dictionary<ObservedMapCellKey, ObservedCultureFieldProjection> fields,
        HashSet<ObservedMapCellKey> visited)
    {
        var size = 0;
        var queue = new Queue<ObservedMapCellKey>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            size++;
            foreach (var neighbor in Neighbors(current))
            {
                if (fields.ContainsKey(neighbor) && visited.Add(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        return size;
    }

    private CultureFieldComponent CultureFieldComponentAt(int cultureId, int planeCode, int q, int r, int tick)
    {
        if (cultureId <= 0)
        {
            return default;
        }

        if (_cultureFieldComponentCacheTick != tick)
        {
            _cultureFieldComponentCache.Clear();
            _cultureFieldComponentCacheTick = tick;
        }

        var cacheKey = new CultureFieldComponentCacheKey(cultureId, planeCode, q, r);
        if (_cultureFieldComponentCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var start = new ObservedMapCellKey(planeCode, q, r);
        var startFieldKey = new ObservedCultureFieldKey(cultureId, planeCode, q, r);
        if (!_cultureFields.ContainsKey(startFieldKey))
        {
            return default;
        }

        return BuildCultureFieldComponent(cultureId, start);
    }

    private CultureFieldComponent BuildCultureFieldComponent(int cultureId, ObservedMapCellKey start)
    {
        var visited = new HashSet<ObservedMapCellKey>();
        var queue = new Queue<ObservedMapCellKey>();
        var cellCount = 0;
        var rawMass = 0f;
        var minQ = start.Q;
        var minR = start.R;
        visited.Add(start);
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            AddCultureFieldComponentCell(cultureId, current, ref cellCount, ref rawMass, ref minQ, ref minR);
            foreach (var neighbor in Neighbors(current))
            {
                var fieldKey = new ObservedCultureFieldKey(cultureId, neighbor.PlaneCode, neighbor.Q, neighbor.R);
                if (_cultureFields.ContainsKey(fieldKey) && visited.Add(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        var component = new CultureFieldComponent(BuildComponentId(minQ, minR), cellCount, rawMass);
        foreach (var key in visited)
        {
            _cultureFieldComponentCache[new CultureFieldComponentCacheKey(cultureId, key.PlaneCode, key.Q, key.R)] =
                component;
        }

        return component;
    }

    private void AddCultureFieldComponentCell(
        int cultureId,
        ObservedMapCellKey key,
        ref int cellCount,
        ref float rawMass,
        ref int minQ,
        ref int minR)
    {
        cellCount++;
        var fieldKey = new ObservedCultureFieldKey(cultureId, key.PlaneCode, key.Q, key.R);
        rawMass += _cultureFields.TryGetValue(fieldKey, out var field) ? field.CurrentRawInfluence : 0f;
        if (key.Q < minQ || (key.Q == minQ && key.R < minR))
        {
            minQ = key.Q;
            minR = key.R;
        }
    }

    private static IEnumerable<ObservedMapCellKey> Neighbors(ObservedMapCellKey key)
    {
        yield return new ObservedMapCellKey(key.PlaneCode, key.Q + 1, key.R);
        yield return new ObservedMapCellKey(key.PlaneCode, key.Q + 1, key.R - 1);
        yield return new ObservedMapCellKey(key.PlaneCode, key.Q, key.R - 1);
        yield return new ObservedMapCellKey(key.PlaneCode, key.Q - 1, key.R);
        yield return new ObservedMapCellKey(key.PlaneCode, key.Q - 1, key.R + 1);
        yield return new ObservedMapCellKey(key.PlaneCode, key.Q, key.R + 1);
    }

    private static int BuildComponentId(int q, int r)
    {
        unchecked
        {
            var hash = (uint)q * 2654435761u;
            hash ^= (uint)r * 2246822519u;
            var value = (int)(hash & 2147483647u);
            return value == 0 ? 1 : value;
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

    private void RecordCultureEntityBirthField(int tick, ObservedEntityProjection entity)
    {
        var physical = CultureFieldAt(entity.CultureId, PlaneCodes.Physical, entity.PhysicalQ, entity.PhysicalR);
        var semantic = CultureFieldAt(entity.CultureId, PlaneCodes.Semantic, entity.SemanticQ, entity.SemanticR);
        _batch.CultureEntityBirthFieldEvents.Add(new CultureEntityBirthFieldEventRow(
            _runId,
            tick,
            entity.EntityId,
            entity.CultureId,
            CultureFieldStrengthBand(physical.RawMass),
            physical.CellKindCode,
            physical.LocalRawMass,
            physical.ConnectedRawMass,
            CultureFieldStrengthBand(semantic.RawMass),
            semantic.CellKindCode,
            semantic.LocalRawMass,
            semantic.ConnectedRawMass));
    }

    private void RecordCultureEntityFieldSnapshot(int tick, ObservedEntityProjection entity, int planeCode)
    {
        var q = planeCode == PlaneCodes.Semantic ? entity.SemanticQ : entity.PhysicalQ;
        var r = planeCode == PlaneCodes.Semantic ? entity.SemanticR : entity.PhysicalR;
        var field = CultureFieldAt(entity.CultureId, planeCode, q, r);
        _batch.CultureEntityFieldSnapshots.Add(new CultureEntityFieldSnapshotRow(
            _runId,
            tick,
            entity.EntityId,
            entity.CultureId,
            planeCode,
            field.CellKindCode,
            EntityOccupiesCultureSlot(entity.EntityId),
            field.LocalRawMass,
            field.ConnectedRawMass,
            EntityInfluenceProduction(entity.EntityId, planeCode)));
    }

    private void RecordCultureInfluenceActionField(in CellInfluenceChangedKernelEvent evt)
    {
        if (!_entities.TryGetValue(evt.EntityId, out var entity))
        {
            return;
        }

        var delta = evt.CurrentInfluence - evt.PreviousInfluence;
        if (Math.Abs(delta) <= float.Epsilon)
        {
            return;
        }

        AddEntityInfluenceProduction(evt.EntityId, evt.PlaneCode, delta);
        var field = CultureFieldAt(entity.CultureId, evt.PlaneCode, evt.Q, evt.R);
        _batch.CultureInfluenceActionFieldEvents.Add(new CultureInfluenceActionFieldEventRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            entity.CultureId,
            evt.ActionId,
            evt.PlaneCode,
            field.CellKindCode,
            EntityOccupiesCultureSlot(evt.EntityId),
            field.LocalRawMass,
            field.ConnectedRawMass,
            evt.PreviousInfluence,
            evt.CurrentInfluence,
            delta));
    }

    private void AddEntityInfluenceProduction(int entityId, int planeCode, float delta)
    {
        if (delta <= 0f)
        {
            return;
        }

        var key = new ObservedEntityPlaneKey(entityId, planeCode);
        _entityInfluenceProduction[key] = _entityInfluenceProduction.GetValueOrDefault(key) + delta;
    }

    private void UpdateCultureField(in CellCultureInfluenceChangedKernelEvent evt)
    {
        _cultureFieldComponentCache.Clear();
        _cultureFieldComponentCacheTick = -1;

        var key = new ObservedCultureFieldKey(evt.CultureId, evt.PlaneCode, evt.Q, evt.R);
        var cellKey = new ObservedMapCellKey(evt.PlaneCode, evt.Q, evt.R);
        if (evt.CurrentShare <= 0f && evt.CurrentRawInfluence <= 0f)
        {
            _cultureFields.Remove(key);
            RemoveCultureFieldCellKey(cellKey, key);
            return;
        }

        _cultureFields[key] = new ObservedCultureFieldProjection(
            key,
            evt.CurrentShare,
            evt.CurrentRawInfluence,
            evt.CurrentTotalRawInfluence);
        if (!_cultureFieldKeysByCell.TryGetValue(cellKey, out var keys))
        {
            keys = [];
            _cultureFieldKeysByCell[cellKey] = keys;
        }

        keys.Add(key);
    }

    private void RemoveCultureFieldCellKey(ObservedMapCellKey cellKey, ObservedCultureFieldKey key)
    {
        if (!_cultureFieldKeysByCell.TryGetValue(cellKey, out var keys))
        {
            return;
        }

        keys.Remove(key);
        if (keys.Count == 0)
        {
            _cultureFieldKeysByCell.Remove(cellKey);
        }
    }

    private void AdjustCultureSlotOccupancy(int entityId, int delta)
    {
        if (entityId <= 0)
        {
            return;
        }

        var current = _occupiedCultureSlotCountByEntity.GetValueOrDefault(entityId);
        var next = current + delta;
        if (next <= 0)
        {
            _occupiedCultureSlotCountByEntity.Remove(entityId);
            return;
        }

        _occupiedCultureSlotCountByEntity[entityId] = next;
    }

    private bool EntityOccupiesCultureSlot(int entityId) =>
        _occupiedCultureSlotCountByEntity.GetValueOrDefault(entityId) > 0;

    private float EntityInfluenceProduction(int entityId, int planeCode) =>
        _entityInfluenceProduction.GetValueOrDefault(new ObservedEntityPlaneKey(entityId, planeCode));

    private ObservedCultureFieldCell CultureFieldAt(int cultureId, int planeCode, int q, int r)
    {
        var key = new ObservedCultureFieldKey(cultureId, planeCode, q, r);
        _cultureFields.TryGetValue(key, out var field);
        var raw = field?.CurrentRawInfluence ?? 0f;
        var share = field?.CurrentShare ?? 0f;
        var totalRaw = field?.CurrentTotalRawInfluence ?? CultureRawTotalAt(planeCode, q, r);
        var foreignShare = StrongestForeignCultureShare(cultureId, planeCode, q, r);
        var neighborhood = CultureFieldNeighborhoodAt(cultureId, planeCode, q, r);
        var localRaw = raw + neighborhood.AdjacentRaw;
        var connectedRaw = localRaw + (neighborhood.RingTwoRaw * CultureFieldRingTwoConnectionWeight);
        return new ObservedCultureFieldCell(
            share,
            raw,
            totalRaw,
            foreignShare,
            localRaw,
            connectedRaw,
            CultureFieldCellKind(raw, share, foreignShare, neighborhood.SupportedAdjacentCount, connectedRaw));
    }

    private CultureFieldNeighborhood CultureFieldNeighborhoodAt(int cultureId, int planeCode, int q, int r)
    {
        var adjacentRaw = 0f;
        var ringTwoRaw = 0f;
        var supportedAdjacentCount = 0;
        for (var dq = -2; dq <= 2; dq++)
        {
            var minDr = Math.Max(-2, -dq - 2);
            var maxDr = Math.Min(2, -dq + 2);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                AddCultureFieldNeighbor(cultureId, planeCode, q, r, dq, dr, ref adjacentRaw, ref ringTwoRaw, ref supportedAdjacentCount);
            }
        }

        return new CultureFieldNeighborhood(adjacentRaw, ringTwoRaw, supportedAdjacentCount);
    }

    private void AddCultureFieldNeighbor(
        int cultureId,
        int planeCode,
        int q,
        int r,
        int dq,
        int dr,
        ref float adjacentRaw,
        ref float ringTwoRaw,
        ref int supportedAdjacentCount)
    {
        if (dq == 0 && dr == 0)
        {
            return;
        }

        var neighborRaw = CultureRawAt(cultureId, planeCode, q + dq, r + dr);
        if (HexDistance(0, 0, dq, dr) == 1)
        {
            adjacentRaw += neighborRaw;
            supportedAdjacentCount += CultureFieldSupportedCellCount(neighborRaw);
            return;
        }

        ringTwoRaw += neighborRaw;
    }

    private float CultureRawAt(int cultureId, int planeCode, int q, int r) =>
        _cultureFields.TryGetValue(new ObservedCultureFieldKey(cultureId, planeCode, q, r), out var field)
            ? field.CurrentRawInfluence
            : 0f;

    private float CultureRawTotalAt(int planeCode, int q, int r)
    {
        var total = 0f;
        foreach (var field in CultureFieldsAtCell(planeCode, q, r))
        {
            total += field.CurrentRawInfluence;
        }

        return total;
    }

    private float StrongestForeignCultureShare(int cultureId, int planeCode, int q, int r)
    {
        var strongest = 0f;
        foreach (var field in CultureFieldsAtCell(planeCode, q, r))
        {
            if (field.Key.CultureId != cultureId)
            {
                strongest = Math.Max(strongest, field.CurrentShare);
            }
        }

        return strongest;
    }

    private IEnumerable<ObservedCultureFieldProjection> CultureFieldsAtCell(int planeCode, int q, int r)
    {
        var cellKey = new ObservedMapCellKey(planeCode, q, r);
        if (!_cultureFieldKeysByCell.TryGetValue(cellKey, out var keys))
        {
            yield break;
        }

        foreach (var key in keys)
        {
            if (_cultureFields.TryGetValue(key, out var field))
            {
                yield return field;
            }
        }
    }

    private static int CultureFieldStrengthBand(float rawMass)
    {
        if (rawMass >= CultureFieldStrongRawMass)
        {
            return CultureFieldStrengthBandCodes.Strong;
        }

        return rawMass >= CultureFieldTraceRawMass
            ? CultureFieldStrengthBandCodes.Trace
            : CultureFieldStrengthBandCodes.None;
    }

    private static int CultureFieldCellKind(
        float raw,
        float share,
        float foreignShare,
        int supportedAdjacentCount,
        float connectedRaw)
    {
        if (CultureFieldIsEmpty(raw, share, connectedRaw))
        {
            return CultureFieldCellKindCodes.None;
        }

        if (CultureFieldIsContested(raw, share, foreignShare))
        {
            return CultureFieldCellKindCodes.Contested;
        }

        if (CultureFieldIsInterior(raw, supportedAdjacentCount))
        {
            return CultureFieldCellKindCodes.Interior;
        }

        if (CultureFieldIsIsolated(raw, supportedAdjacentCount))
        {
            return CultureFieldCellKindCodes.Isolated;
        }

        return CultureFieldIsBorder(raw, supportedAdjacentCount)
            ? CultureFieldCellKindCodes.Border
            : CultureFieldCellKindCodes.None;
    }

    private static bool CultureFieldIsEmpty(float raw, float share, float connectedRaw) =>
        raw < CultureFieldTraceRawMass && share <= 0f && connectedRaw < CultureFieldSupportedRawMass;

    private static bool CultureFieldIsContested(float raw, float share, float foreignShare) =>
        raw >= CultureFieldTraceRawMass &&
        foreignShare >= CultureFieldForeignContestShare &&
        (share < CultureFieldDominantOwnShare || foreignShare >= CultureFieldMajorForeignContestShare);

    private static bool CultureFieldIsInterior(float raw, int supportedAdjacentCount) =>
        raw >= CultureFieldStrongRawMass && supportedAdjacentCount >= 5;

    private static bool CultureFieldIsIsolated(float raw, int supportedAdjacentCount) =>
        raw >= CultureFieldTraceRawMass && supportedAdjacentCount == 0;

    private static bool CultureFieldIsBorder(float raw, int supportedAdjacentCount) =>
        raw >= CultureFieldTraceRawMass || supportedAdjacentCount > 0;

    private static int CultureFieldSupportedCellCount(float raw) =>
        raw >= CultureFieldSupportedRawMass ? 1 : 0;

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var asCoord = -aq - ar;
        var bsCoord = -bq - br;
        return (Math.Abs(aq - bq) + Math.Abs(ar - br) + Math.Abs(asCoord - bsCoord)) / 2;
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
