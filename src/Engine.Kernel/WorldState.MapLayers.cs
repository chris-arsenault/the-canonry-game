namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class WorldState
{
    private const float MinimumCultureInfluenceRawTotal = 2f;
    private const float FullCultureInfluenceRawTotal = 30f;

    public int MapRadius => _mapRadius;

    public void SetMapRadius(int radius)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(radius);
        _mapRadius = radius;
    }

    public void SetCell(int q, int r, int terrainId)
    {
        SetCell(PlaneCodes.Physical, q, r, terrainId);
    }

    public void SetCell(int planeCode, int q, int r, int terrainId)
    {
        ValidatePositive(terrainId);
        WriteCell(planeCode, q, r, terrainId, CoverageOwnerAt(planeCode, q, r), CellSuperregionId(planeCode, q, r), CellContinentId(planeCode, q, r));
    }

    public bool CellIsKnown(int q, int r) => CellIsKnown(PlaneCodes.Physical, q, r);

    public bool CellIsKnown(int planeCode, int q, int r)
    {
        ValidateInfluencePlaneCode(planeCode);
        if (planeCode == PlaneCodes.Semantic)
        {
            return MaxAbs(q, r, -q - r) <= _mapRadius;
        }

        return _mapCellTerrainIds.TryGet(CoordinateKey(q, r), PlaneCodes.Physical, out _);
    }

    public int CellTerrainId(int q, int r) => CellTerrainId(PlaneCodes.Physical, q, r);

    public int CellTerrainId(int planeCode, int q, int r)
    {
        ValidateInfluencePlaneCode(planeCode);
        return _mapCellTerrainIds.TryGet(CoordinateKey(q, r), planeCode, out var terrainId) ? terrainId : 1;
    }

    public void SetCellTerrain(int q, int r, int terrainId)
    {
        SetCellTerrain(PlaneCodes.Physical, q, r, terrainId);
    }

    public void SetCellTerrain(int planeCode, int q, int r, int terrainId)
    {
        ValidatePositive(terrainId);
        WriteCell(planeCode, q, r, terrainId, CoverageOwnerAt(planeCode, q, r), CellSuperregionId(planeCode, q, r), CellContinentId(planeCode, q, r));
    }

    public void SetCellTerrain(int q, int r, int terrainId, int tick, in KernelExecutionContext context)
    {
        SetCellTerrain(PlaneCodes.Physical, q, r, terrainId, tick, context);
    }

    public void SetCellTerrain(int planeCode, int q, int r, int terrainId, int tick, in KernelExecutionContext context)
    {
        SetCellState(planeCode, q, r, terrainId, CoverageOwnerAt(planeCode, q, r), tick, context);
    }

    public void MaterializeCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId = 0,
        int continentId = 0)
    {
        MaterializeCell(PlaneCodes.Physical, q, r, terrainId, ownerEntityId, superregionId, continentId);
    }

    public void MaterializeCell(
        int planeCode,
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId = 0,
        int continentId = 0)
    {
        ValidatePositive(terrainId);
        ValidateInfluencePlaneCode(planeCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(ownerEntityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(superregionId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(continentId);
        WriteCell(planeCode, q, r, terrainId, ownerEntityId, superregionId, continentId);
        AdvanceWorldVersion();
    }

    private void WriteCell(
        int planeCode,
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId,
        int continentId)
    {
        ValidateInfluencePlaneCode(planeCode);
        var key = CoordinateKey(q, r);
        _mapCellTerrainIds.Set(key, planeCode, terrainId);
        _mapCellSuperregionIds.Set(key, planeCode, superregionId);
        _mapCellContinentIds.Set(key, planeCode, continentId);
        _mapLayers.Coverage.SetValue(planeCode, key, q, r, ownerEntityId);
    }

    internal int CellSuperregionId(int q, int r) => CellSuperregionId(PlaneCodes.Physical, q, r);

    internal int CellSuperregionId(int planeCode, int q, int r)
    {
        ValidateInfluencePlaneCode(planeCode);
        return _mapCellSuperregionIds.GetOrDefault(CoordinateKey(q, r), planeCode);
    }

    internal int CellContinentId(int q, int r) => CellContinentId(PlaneCodes.Physical, q, r);

    internal int CellContinentId(int planeCode, int q, int r)
    {
        ValidateInfluencePlaneCode(planeCode);
        return _mapCellContinentIds.GetOrDefault(CoordinateKey(q, r), planeCode);
    }

    public int TerrainBoundaryGroupId(int terrainId)
    {
        return _kernelData.Terrain.BoundaryGroupId(terrainId);
    }

    internal int CoverageOwnerAt(int q, int r) => CoverageOwnerAt(PlaneCodes.Physical, q, r);

    internal int CoverageOwnerAt(int planeCode, int q, int r)
    {
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.Coverage.ValueAt(planeCode, CoordinateKey(q, r));
    }

    internal void SetCoverageOwner(int q, int r, int ownerEntityId)
    {
        SetCoverageOwner(PlaneCodes.Physical, q, r, ownerEntityId);
    }

    internal void SetCoverageOwner(int planeCode, int q, int r, int ownerEntityId)
    {
        ValidateInfluencePlaneCode(planeCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(ownerEntityId);
        _mapLayers.Coverage.SetValue(planeCode, CoordinateKey(q, r), q, r, ownerEntityId);
    }

    internal void SetCoverageOwner(int q, int r, int ownerEntityId, int tick, in KernelExecutionContext context)
    {
        SetCoverageOwner(PlaneCodes.Physical, q, r, ownerEntityId, tick, context);
    }

    internal void SetCoverageOwner(int planeCode, int q, int r, int ownerEntityId, int tick, in KernelExecutionContext context)
    {
        SetCellState(planeCode, q, r, CellTerrainId(planeCode, q, r), ownerEntityId, tick, context);
    }

    internal void SetCellState(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int tick,
        in KernelExecutionContext context)
    {
        SetCellState(PlaneCodes.Physical, q, r, terrainId, ownerEntityId, tick, context);
    }

    internal void SetCellState(
        int planeCode,
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int tick,
        in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        ValidateInfluencePlaneCode(planeCode);
        ValidatePositive(terrainId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(ownerEntityId);

        var previousTerrainId = CellTerrainId(planeCode, q, r);
        var previousOwnerEntityId = CoverageOwnerAt(planeCode, q, r);
        var superregionId = CellSuperregionId(planeCode, q, r);
        var continentId = CellContinentId(planeCode, q, r);
        if (previousTerrainId == terrainId && previousOwnerEntityId == ownerEntityId)
        {
            return;
        }

        EnsureProvenanceOutputCapacity(context);
        WriteCell(planeCode, q, r, terrainId, ownerEntityId, superregionId, continentId);
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.CellChanged,
            CoordinateKey(q, r),
            ownerEntityId,
            0,
            terrainId,
            ProvenanceOutputKindCodes.CellChanged);
        Write(KernelEvent.From(new MapCellStateChangedKernelEvent(
            tick,
            WorldVersion,
            planeCode,
            q,
            r,
            context.SourceCodeId,
            previousTerrainId,
            terrainId,
            previousOwnerEntityId,
            ownerEntityId,
            superregionId,
            continentId)),
            context);
        if (planeCode == PlaneCodes.Physical)
        {
            Write(KernelEvent.From(new CellChangedKernelEvent(
                tick,
                WorldVersion,
                q,
                r,
                context.SourceCodeId,
                previousTerrainId,
                terrainId,
                previousOwnerEntityId,
                ownerEntityId,
                superregionId,
                continentId)),
                context);
        }
    }

    internal int CoverageOwnerCellCount(int ownerEntityId) => CoverageOwnerCellCount(ownerEntityId, PlaneCodes.Physical);

    internal int CoverageOwnerCellCount(int ownerEntityId, int planeCode)
    {
        EnsureEntity(ownerEntityId);
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.Coverage.ValueCellCount(ownerEntityId, planeCode);
    }

    internal bool CoverageOwnerHasCells(int ownerEntityId) => CoverageOwnerHasCells(ownerEntityId, PlaneCodes.Physical);

    internal bool CoverageOwnerHasCells(int ownerEntityId, int planeCode)
    {
        EnsureEntity(ownerEntityId);
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.Coverage.ValueCellCount(ownerEntityId, planeCode) > 0;
    }

    internal SpatialLayerCellCursor CoverageCells(int ownerEntityId) => CoverageCells(ownerEntityId, PlaneCodes.Physical);

    internal SpatialLayerCellCursor CoverageCells(int ownerEntityId, int planeCode)
    {
        EnsureEntity(ownerEntityId);
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.Coverage.CellsOfValue(ownerEntityId, planeCode);
    }

    internal bool MoveNextCoverageCell(ref SpatialLayerCellCursor cursor, out int q, out int r)
    {
        return _mapLayers.Coverage.MoveNextCell(ref cursor, out q, out r);
    }

    internal int InfluenceCellCount(int entityId, int planeCode)
    {
        EnsureEntity(entityId);
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.Influence.ValueCellCount(entityId, planeCode);
    }

    internal SpatialLayerCellCursor InfluenceCells(int entityId, int planeCode)
    {
        EnsureEntity(entityId);
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.Influence.CellsOfValue(entityId, planeCode);
    }

    internal bool MoveNextInfluenceCell(ref SpatialLayerCellCursor cursor, out int q, out int r, out float value)
    {
        return _mapLayers.Influence.MoveNextCell(ref cursor, out q, out r, out value);
    }

    public float GetCellInfluence(int entityId, int planeCode, int q, int r)
    {
        EnsureEntity(entityId);
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.Influence.ValueAt(entityId, planeCode, CoordinateKey(q, r));
    }

    internal int CultureInfluenceCellCount(int cultureId, int planeCode)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.CultureInfluence.ValueCellCount(cultureId, planeCode);
    }

    internal SpatialLayerCellCursor CultureInfluenceCells(int cultureId, int planeCode)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.CultureInfluence.CellsOfValue(cultureId, planeCode);
    }

    internal bool MoveNextCultureInfluenceCell(ref SpatialLayerCellCursor cursor, out int q, out int r, out float value)
    {
        return _mapLayers.CultureInfluence.MoveNextCell(ref cursor, out q, out r, out value);
    }

    public float GetCellCultureInfluence(int cultureId, int planeCode, int q, int r)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.CultureInfluence.ValueAt(cultureId, planeCode, CoordinateKey(q, r));
    }

    internal float GetCellCultureInfluenceRaw(int cultureId, int planeCode, int q, int r)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        return PositiveCultureInfluenceRaw(cultureId, planeCode, CoordinateKey(q, r));
    }

    public void SetCellInfluence(
        int entityId,
        int planeCode,
        int q,
        int r,
        float value,
        int tick,
        in KernelExecutionContext context)
    {
        EnsureEntity(entityId);
        ValidateInfluencePlaneCode(planeCode);
        ValidatePositive(context.SourceCodeId);
        var key = CoordinateKey(q, r);
        var clamped = ClampInfluence(value);
        var previous = _mapLayers.Influence.ValueAt(entityId, planeCode, key);
        if (global::System.MathF.Abs(clamped - previous) <= float.Epsilon)
        {
            return;
        }

        EnsureProvenanceOutputCapacity(context);
        _mapLayers.Influence.SetValue(entityId, planeCode, key, q, r, clamped);
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.CellInfluenceChanged,
            key,
            entityId,
            0,
            planeCode,
            ProvenanceOutputKindCodes.CellInfluenceChanged);
        Write(KernelEvent.From(new CellInfluenceChangedKernelEvent(
            tick,
            WorldVersion,
            q,
            r,
            context.SourceCodeId,
            planeCode,
            entityId,
            context.ActionId,
            previous,
            clamped)),
            context);
        UpdateCultureInfluenceForEntityInfluenceChange(entityId, planeCode, q, r, previous, clamped, tick, context);
    }

    public void ModifyCellInfluence(
        int entityId,
        int planeCode,
        int q,
        int r,
        float delta,
        int tick,
        in KernelExecutionContext context)
    {
        var previous = GetCellInfluence(entityId, planeCode, q, r);
        SetCellInfluence(entityId, planeCode, q, r, previous + delta, tick, context);
    }

    private static int MaxAbs(int a, int b, int c) =>
        global::System.Math.Max(global::System.Math.Abs(a), global::System.Math.Max(global::System.Math.Abs(b), global::System.Math.Abs(c)));

    internal void ModifyEntityCultureInfluenceMultiplierDelta(
        int entityId,
        float delta,
        int tick,
        in KernelExecutionContext context)
    {
        EnsureEntity(entityId);
        if (delta == 0f)
        {
            return;
        }

        var previous = EffectiveCultureInfluenceMultiplier(entityId);
        _entityCultureInfluenceMultiplierDeltas[entityId] += delta;
        var current = EffectiveCultureInfluenceMultiplier(entityId);
        UpdateCultureInfluenceForEntityMultiplierChange(entityId, previous, current, tick, context);
    }

    internal void ModifyCultureInfluenceMultiplierDelta(
        int cultureId,
        float delta,
        int tick,
        in KernelExecutionContext context)
    {
        ValidatePositive(cultureId);
        if ((uint)cultureId >= (uint)_cultureInfluenceMultiplierDeltas.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(cultureId));
        }

        if (delta == 0f)
        {
            return;
        }

        for (var entityId = FirstEntityOfCulture(cultureId);
             entityId > 0;
             entityId = NextEntityOfCulture(entityId))
        {
            if (EntityIsActive(entityId))
            {
                UpdateCultureInfluenceForEntityMultiplierChange(
                    entityId,
                    EffectiveCultureInfluenceMultiplier(entityId),
                    EffectiveCultureInfluenceMultiplierWithCultureDelta(entityId, delta),
                    tick,
                    context);
            }
        }

        _cultureInfluenceMultiplierDeltas[cultureId] += delta;
    }

    private void UpdateCultureInfluenceForEntityInfluenceChange(
        int entityId,
        int planeCode,
        int q,
        int r,
        float previous,
        float current,
        int tick,
        in KernelExecutionContext context)
    {
        if (!EntityIsActive(entityId))
        {
            return;
        }

        var delta = current - previous;
        if (global::System.MathF.Abs(delta) <= float.Epsilon)
        {
            return;
        }

        var cultureId = EntityCultureId(entityId);
        var key = CoordinateKey(q, r);
        var raw = _mapLayers.RawCultureInfluence.ValueAt(cultureId, planeCode, key);
        var weightedDelta = delta * EffectiveCultureInfluenceMultiplier(entityId);
        if (weightedDelta > 0f)
        {
            AddEntityInfluenceProduction(entityId, planeCode, weightedDelta);
        }

        _mapLayers.RawCultureInfluence.SetValue(
            cultureId,
            planeCode,
            key,
            q,
            r,
            global::System.MathF.Max(0f, raw + weightedDelta));
        RecalculateCultureInfluenceAtCell(
            planeCode,
            q,
            r,
            key,
            cultureId,
            raw,
            global::System.MathF.Max(0f, raw + weightedDelta),
            tick,
            context);
    }

    private void UpdateCultureInfluenceForEntityStatusChange(
        int entityId,
        bool isActive,
        int tick,
        in KernelExecutionContext context)
    {
        var sign = isActive ? 1f : -1f;
        var cultureId = EntityCultureId(entityId);
        UpdateCultureInfluenceForEntityStatusChange(entityId, cultureId, PlaneCodes.Physical, sign, tick, context);
        UpdateCultureInfluenceForEntityStatusChange(entityId, cultureId, PlaneCodes.Semantic, sign, tick, context);
    }

    private void UpdateCultureInfluenceForEntityStatusChange(
        int entityId,
        int cultureId,
        int planeCode,
        float sign,
        int tick,
        in KernelExecutionContext context)
    {
        var multiplier = EffectiveCultureInfluenceMultiplier(entityId);
        var cursor = _mapLayers.Influence.CellsOfValue(entityId, planeCode);
        while (_mapLayers.Influence.MoveNextCell(ref cursor, out var q, out var r, out var influence))
        {
            var key = CoordinateKey(q, r);
            var raw = _mapLayers.RawCultureInfluence.ValueAt(cultureId, planeCode, key);
            _mapLayers.RawCultureInfluence.SetValue(
                cultureId,
                planeCode,
                key,
                q,
                r,
                global::System.MathF.Max(0f, raw + (influence * multiplier * sign)));
            RecalculateCultureInfluenceAtCell(
                planeCode,
                q,
                r,
                key,
                cultureId,
                raw,
                global::System.MathF.Max(0f, raw + (influence * multiplier * sign)),
                tick,
                context);
        }
    }

    private void UpdateCultureInfluenceForEntityCultureChange(
        int entityId,
        int previousCultureId,
        float previousMultiplier,
        int currentCultureId,
        float currentMultiplier,
        int tick,
        in KernelExecutionContext context)
    {
        UpdateCultureInfluenceForEntityCultureChange(
            entityId,
            previousCultureId,
            PlaneCodes.Physical,
            -previousMultiplier,
            tick,
            context);
        UpdateCultureInfluenceForEntityCultureChange(
            entityId,
            previousCultureId,
            PlaneCodes.Semantic,
            -previousMultiplier,
            tick,
            context);
        UpdateCultureInfluenceForEntityCultureChange(
            entityId,
            currentCultureId,
            PlaneCodes.Physical,
            currentMultiplier,
            tick,
            context);
        UpdateCultureInfluenceForEntityCultureChange(
            entityId,
            currentCultureId,
            PlaneCodes.Semantic,
            currentMultiplier,
            tick,
            context);
    }

    private void UpdateCultureInfluenceForEntityCultureChange(
        int entityId,
        int cultureId,
        int planeCode,
        float multiplier,
        int tick,
        in KernelExecutionContext context)
    {
        var cursor = _mapLayers.Influence.CellsOfValue(entityId, planeCode);
        while (_mapLayers.Influence.MoveNextCell(ref cursor, out var q, out var r, out var influence))
        {
            var key = CoordinateKey(q, r);
            var raw = _mapLayers.RawCultureInfluence.ValueAt(cultureId, planeCode, key);
            _mapLayers.RawCultureInfluence.SetValue(
                cultureId,
                planeCode,
                key,
                q,
                r,
                global::System.MathF.Max(0f, raw + (influence * multiplier)));
            RecalculateCultureInfluenceAtCell(
                planeCode,
                q,
                r,
                key,
                cultureId,
                raw,
                global::System.MathF.Max(0f, raw + (influence * multiplier)),
                tick,
                context);
        }
    }

    private void UpdateCultureInfluenceForEntityMultiplierChange(
        int entityId,
        float previousMultiplier,
        float currentMultiplier,
        int tick,
        in KernelExecutionContext context)
    {
        if (!EntityIsActive(entityId))
        {
            return;
        }

        var deltaMultiplier = currentMultiplier - previousMultiplier;
        if (global::System.MathF.Abs(deltaMultiplier) <= 0.0001f)
        {
            return;
        }

        var cultureId = EntityCultureId(entityId);
        UpdateCultureInfluenceForEntityMultiplierChange(entityId, cultureId, PlaneCodes.Physical, deltaMultiplier, tick, context);
        UpdateCultureInfluenceForEntityMultiplierChange(entityId, cultureId, PlaneCodes.Semantic, deltaMultiplier, tick, context);
    }

    private void UpdateCultureInfluenceForEntityMultiplierChange(
        int entityId,
        int cultureId,
        int planeCode,
        float deltaMultiplier,
        int tick,
        in KernelExecutionContext context)
    {
        var cursor = _mapLayers.Influence.CellsOfValue(entityId, planeCode);
        while (_mapLayers.Influence.MoveNextCell(ref cursor, out var q, out var r, out var influence))
        {
            var key = CoordinateKey(q, r);
            var raw = _mapLayers.RawCultureInfluence.ValueAt(cultureId, planeCode, key);
            _mapLayers.RawCultureInfluence.SetValue(
                cultureId,
                planeCode,
                key,
                q,
                r,
                global::System.MathF.Max(0f, raw + (influence * deltaMultiplier)));
            RecalculateCultureInfluenceAtCell(
                planeCode,
                q,
                r,
                key,
                cultureId,
                raw,
                global::System.MathF.Max(0f, raw + (influence * deltaMultiplier)),
                tick,
                context);
        }
    }

    private void RecalculateCultureInfluenceAtCell(
        int planeCode,
        int q,
        int r,
        int key,
        int changedCultureId,
        float changedPreviousRaw,
        float changedCurrentRaw,
        int tick,
        in KernelExecutionContext context)
    {
        var total = 0f;
        for (var cultureId = 1; cultureId <= _cultureHighWatermark; cultureId++)
        {
            total += PositiveCultureInfluenceRaw(cultureId, planeCode, key);
        }

        for (var cultureId = 1; cultureId <= _cultureHighWatermark; cultureId++)
        {
            var previous = _mapLayers.CultureInfluence.ValueAt(cultureId, planeCode, key);
            var raw = PositiveCultureInfluenceRaw(cultureId, planeCode, key);
            var current = StrengthWeightedCultureShare(raw, total);
            if (global::System.MathF.Abs(current - previous) <= 0.0001f)
            {
                continue;
            }

            _mapLayers.CultureInfluence.SetValue(cultureId, planeCode, key, q, r, current);
            Write(KernelEvent.From(new CellCultureInfluenceChangedKernelEvent(
                tick,
                WorldVersion,
                q,
                r,
                context.SourceCodeId,
                planeCode,
                cultureId,
                previous,
                current,
                cultureId == changedCultureId ? changedPreviousRaw : raw,
                cultureId == changedCultureId ? changedCurrentRaw : raw,
                total)),
                context);
        }
    }

    private void AddEntityInfluenceProduction(int entityId, int planeCode, float delta)
    {
        if (planeCode == PlaneCodes.Semantic)
        {
            _entitySemanticInfluenceProduction[entityId] += delta;
            return;
        }

        _entityPhysicalInfluenceProduction[entityId] += delta;
    }

    private float PositiveCultureInfluenceRaw(int cultureId, int planeCode, int key)
    {
        var raw = _mapLayers.RawCultureInfluence.ValueAt(cultureId, planeCode, key);
        return raw > 0f ? raw : 0f;
    }

    private static float StrengthWeightedCultureShare(float raw, float total)
    {
        if (raw <= 0f || total < MinimumCultureInfluenceRawTotal)
        {
            return 0f;
        }

        var share = raw * 100f / total;
        var strength = global::System.MathF.Min(1f, total / FullCultureInfluenceRawTotal);
        return share * strength;
    }

    private float EffectiveCultureInfluenceMultiplier(int entityId)
    {
        var cultureId = _entityCultureId[entityId];
        var raw = 1f + _entityCultureInfluenceMultiplierDeltas[entityId] +
            (cultureId > 0 && cultureId < _cultureInfluenceMultiplierDeltas.Length
                ? _cultureInfluenceMultiplierDeltas[cultureId]
                : 0f);
        return global::System.Math.Clamp(raw, 0.1f, 3f);
    }

    private float EffectiveCultureInfluenceMultiplierWithCultureDelta(int entityId, float cultureDelta)
    {
        var cultureId = _entityCultureId[entityId];
        var raw = 1f + _entityCultureInfluenceMultiplierDeltas[entityId] +
            (cultureId > 0 && cultureId < _cultureInfluenceMultiplierDeltas.Length
                ? _cultureInfluenceMultiplierDeltas[cultureId] + cultureDelta
                : 0f);
        return global::System.Math.Clamp(raw, 0.1f, 3f);
    }
}
