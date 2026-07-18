namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class WorldState
{
    internal const float CultureTraceRawMass = 5f;
    internal const float CultureClaimedRawMass = 30f;
    internal const float CultureControlledRawMass = 100f;
    internal const float CultureEntrenchedRawMass = 300f;
    internal const float CultureNexusRawMass = 700f;
    internal const float CultureFullRawMass = 2000f;
    internal const int MinCellZ = -3;
    internal const int MaxCellZ = 3;

    public int MapRadius => _physicalMapRadius;

    public int PhysicalMapRadius => _physicalMapRadius;

    public int SemanticMapRadius => _semanticMapRadius;

    public int MapRadiusForPlane(int planeCode)
    {
        ValidateInfluencePlaneCode(planeCode);
        return planeCode == PlaneCodes.Physical ? _physicalMapRadius : _semanticMapRadius;
    }

    internal int MapSegmentOriginRadiusForPlane(int planeCode)
    {
        ValidateInfluencePlaneCode(planeCode);
        return planeCode == PlaneCodes.Physical
            ? _physicalMapSegmentOriginRadius
            : _semanticMapSegmentOriginRadius;
    }

    public void SetMapRadius(int radius)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(radius);
        _physicalMapRadius = radius;
        _semanticMapRadius = radius;
        _physicalMapSegmentOriginRadius = radius;
        _semanticMapSegmentOriginRadius = radius;
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

    public bool CellIsKnown(int planeCode, int q, int r) => CellIsKnown(planeCode, q, r, z: 0);

    public bool CellIsKnown(int planeCode, int q, int r, int z)
    {
        ValidateInfluencePlaneCode(planeCode);
        if (planeCode == PlaneCodes.Semantic)
        {
            // Idea-space grows like the physical map: a semantic cell is known
            // only once it has been materialized (seeded heart or opened by the
            // frontier). Un-opened idea-space is genuine fog.
            return z == 0 && _mapCellTerrainIds.TryGet(CoordinateKey(q, r, z), PlaneCodes.Semantic, out _);
        }

        if (z is < MinCellZ or > MaxCellZ)
        {
            return false;
        }

        return _mapCellTerrainIds.TryGet(CoordinateKey(q, r, z), PlaneCodes.Physical, out _);
    }

    public int CellTerrainId(int q, int r) => CellTerrainId(PlaneCodes.Physical, q, r);

    public int CellTerrainId(int planeCode, int q, int r) => CellTerrainId(planeCode, q, r, z: 0);

    public int CellTerrainId(int planeCode, int q, int r, int z)
    {
        ValidateInfluencePlaneCode(planeCode);
        var key = CoordinateKey(q, r, z);
        if (planeCode == PlaneCodes.Physical && z == 0)
        {
            var coverTerrainId = _mapCellTerrainCoverIds.GetOrDefault(key, planeCode);
            if (coverTerrainId > 0)
            {
                return coverTerrainId;
            }
        }

        return _mapCellTerrainIds.TryGet(key, planeCode, out var terrainId) ? terrainId : 1;
    }

    internal int CellBaseTerrainId(int planeCode, int q, int r) =>
        _mapCellTerrainIds.TryGet(CoordinateKey(q, r), planeCode, out var terrainId) ? terrainId : 1;

    internal int CellTerrainCoverId(int planeCode, int q, int r)
    {
        ValidateInfluencePlaneCode(planeCode);
        return planeCode == PlaneCodes.Physical
            ? _mapCellTerrainCoverIds.GetOrDefault(CoordinateKey(q, r), planeCode)
            : 0;
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
        int continentId) =>
        WriteCell(planeCode, q, r, z: 0, terrainId, ownerEntityId, superregionId, continentId);

    private void WriteCell(
        int planeCode,
        int q,
        int r,
        int z,
        int terrainId,
        int ownerEntityId,
        int superregionId,
        int continentId)
    {
        ValidateInfluencePlaneCode(planeCode);
        ValidateCellZ(z);
        var key = CoordinateKey(q, r, z);
        _mapCellTerrainIds.Set(key, planeCode, terrainId);
        _mapCellSuperregionIds.Set(key, planeCode, superregionId);
        _mapCellContinentIds.Set(key, planeCode, continentId);
        _mapLayers.Coverage.SetValue(planeCode, key, q, r, z, ownerEntityId);
        if (z == 0)
        {
            AdvanceMapExtent(planeCode, q, r);
        }
    }

    private void AdvanceMapExtent(int planeCode, int q, int r)
    {
        var radius = HexDistanceFromOrigin(q, r);
        if (planeCode == PlaneCodes.Physical)
        {
            _physicalMapRadius = global::System.Math.Max(_physicalMapRadius, radius);
        }
        else
        {
            _semanticMapRadius = global::System.Math.Max(_semanticMapRadius, radius);
        }
    }

    private static int HexDistanceFromOrigin(int q, int r) =>
        (global::System.Math.Abs(q) + global::System.Math.Abs(r) + global::System.Math.Abs(q + r)) / 2;

    internal void WriteForgedCell(int planeCode, int q, int r, int z, int terrainId)
    {
        ValidatePositive(terrainId);
        WriteCell(planeCode, q, r, z, terrainId, ownerEntityId: 0, superregionId: 0, continentId: 0);
        AdvanceWorldVersion();
    }

    private static void ValidateCellZ(int z)
    {
        if (z is < MinCellZ or > MaxCellZ)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(z));
        }
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
        SetCellState(planeCode, q, r, CellBaseTerrainId(planeCode, q, r), ownerEntityId, tick, context);
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
        in KernelExecutionContext context) =>
        SetCellState(planeCode, q, r, z: 0, terrainId, ownerEntityId, tick, context);

    internal void SetCellState(
        int planeCode,
        int q,
        int r,
        int z,
        int terrainId,
        int ownerEntityId,
        int tick,
        in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        ValidateInfluencePlaneCode(planeCode);
        ValidatePositive(terrainId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(ownerEntityId);

        var key = CoordinateKey(q, r, z);
        var previousTerrainId = CellTerrainId(planeCode, q, r, z);
        var previousBaseTerrainId = _mapCellTerrainIds.TryGet(key, planeCode, out var baseTerrainId)
            ? baseTerrainId
            : 1;
        var previousOwnerEntityId = _mapLayers.Coverage.ValueAt(planeCode, CoordinateKey(q, r, z));
        var superregionId = _mapCellSuperregionIds.GetOrDefault(key, planeCode);
        var continentId = _mapCellContinentIds.GetOrDefault(key, planeCode);
        if (previousBaseTerrainId == terrainId && previousOwnerEntityId == ownerEntityId)
        {
            return;
        }

        EnsureProvenanceOutputCapacity(context);
        WriteCell(planeCode, q, r, z, terrainId, ownerEntityId, superregionId, continentId);
        var nextTerrainId = CellTerrainId(planeCode, q, r, z);
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.CellChanged,
            CoordinateKey(q, r, z),
            ownerEntityId,
            0,
            terrainId,
            ProvenanceOutputKindCodes.CellChanged);
        WriteCellStateEvents(
            planeCode, q, r, z, tick, previousTerrainId, nextTerrainId,
            previousOwnerEntityId, ownerEntityId, superregionId, continentId, context);
    }

    internal void SetTerrainCover(
        int q,
        int r,
        int coverTerrainId,
        int tick,
        in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        ValidatePositive(coverTerrainId);
        if (!CellIsKnown(PlaneCodes.Physical, q, r))
        {
            throw new global::System.InvalidOperationException();
        }

        SetTerrainCoverCore(q, r, coverTerrainId, tick, context);
    }

    internal void ClearTerrainCover(
        int q,
        int r,
        int tick,
        in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        if (!CellIsKnown(PlaneCodes.Physical, q, r))
        {
            throw new global::System.InvalidOperationException();
        }

        SetTerrainCoverCore(q, r, coverTerrainId: 0, tick, context);
    }

    private void SetTerrainCoverCore(
        int q,
        int r,
        int coverTerrainId,
        int tick,
        in KernelExecutionContext context)
    {
        var key = CoordinateKey(q, r);
        var previousCoverTerrainId = _mapCellTerrainCoverIds.GetOrDefault(key, PlaneCodes.Physical);
        if (previousCoverTerrainId == coverTerrainId)
        {
            return;
        }

        var previousTerrainId = CellTerrainId(PlaneCodes.Physical, q, r);
        var ownerEntityId = _mapLayers.Coverage.ValueAt(PlaneCodes.Physical, key);
        var superregionId = _mapCellSuperregionIds.GetOrDefault(key, PlaneCodes.Physical);
        var continentId = _mapCellContinentIds.GetOrDefault(key, PlaneCodes.Physical);
        EnsureProvenanceOutputCapacity(context);
        _mapCellTerrainCoverIds.Set(key, PlaneCodes.Physical, coverTerrainId);
        var nextTerrainId = CellTerrainId(PlaneCodes.Physical, q, r);
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.CellChanged,
            key,
            ownerEntityId,
            0,
            nextTerrainId,
            ProvenanceOutputKindCodes.CellChanged);
        WriteCellStateEvents(
            PlaneCodes.Physical, q, r, z: 0, tick, previousTerrainId, nextTerrainId,
            ownerEntityId, ownerEntityId, superregionId, continentId, context);
    }

    private void WriteCellStateEvents(
        int planeCode,
        int q,
        int r,
        int z,
        int tick,
        int previousTerrainId,
        int nextTerrainId,
        int previousOwnerEntityId,
        int ownerEntityId,
        int superregionId,
        int continentId,
        in KernelExecutionContext context)
    {
        Write(KernelEvent.From(new MapCellStateChangedKernelEvent(
            tick, WorldVersion, planeCode, q, r, context.SourceCodeId,
            previousTerrainId, nextTerrainId, previousOwnerEntityId, ownerEntityId,
            superregionId, continentId, z)),
            context);
        if (planeCode == PlaneCodes.Physical)
        {
            Write(KernelEvent.From(new CellChangedKernelEvent(
                tick, WorldVersion, q, r, context.SourceCodeId,
                previousTerrainId, nextTerrainId, previousOwnerEntityId, ownerEntityId,
                superregionId, continentId, z)),
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

    public float GetCellCultureInfluence(int cultureId, int planeCode, int q, int r) =>
        GetCellCultureInfluence(cultureId, planeCode, q, r, z: 0);

    public float GetCellCultureInfluence(int cultureId, int planeCode, int q, int r, int z)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.CultureInfluence.ValueAt(cultureId, planeCode, CoordinateKey(q, r, z));
    }

    internal float GetCellCultureInfluenceRaw(int cultureId, int planeCode, int q, int r) =>
        GetCellCultureInfluenceRaw(cultureId, planeCode, q, r, z: 0);

    internal float GetCellCultureInfluenceRaw(int cultureId, int planeCode, int q, int r, int z)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        return PositiveCultureInfluenceRaw(cultureId, planeCode, CoordinateKey(q, r, z));
    }

    internal float GetCellCultureMaturity(int cultureId, int planeCode, int q, int r)
    {
        return CultureMaturity(GetCellCultureInfluenceRaw(cultureId, planeCode, q, r));
    }

    internal SpatialLayerCellCursor RawCultureInfluenceCellsOf(int cultureId, int planeCode)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.RawCultureInfluence.CellsOfValue(cultureId, planeCode);
    }

    internal bool MoveNextRawCultureInfluenceCell(
        ref SpatialLayerCellCursor cursor,
        out int q,
        out int r,
        out float raw)
    {
        return _mapLayers.RawCultureInfluence.MoveNextCell(ref cursor, out q, out r, out raw);
    }

    internal bool MoveNextRawCultureInfluenceCell(
        ref SpatialLayerCellCursor cursor,
        out int q,
        out int r,
        out int z,
        out float raw)
    {
        return _mapLayers.RawCultureInfluence.MoveNextCell(ref cursor, out q, out r, out z, out raw);
    }

    internal float ModifyCellCultureInfluenceRaw(
        int cultureId,
        int planeCode,
        int q,
        int r,
        float delta,
        int tick,
        in KernelExecutionContext context) =>
        ModifyCellCultureInfluenceRaw(cultureId, planeCode, q, r, z: 0, delta, tick, context);

    internal float ModifyCellCultureInfluenceRaw(
        int cultureId,
        int planeCode,
        int q,
        int r,
        int z,
        float delta,
        int tick,
        in KernelExecutionContext context)
    {
        EnsureCultureIndex(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        ValidateCellZ(z);
        ValidatePositive(context.SourceCodeId);
        if (global::System.MathF.Abs(delta) <= float.Epsilon)
        {
            return 0f;
        }

        var key = CoordinateKey(q, r, z);
        var previous = PositiveCultureInfluenceRaw(cultureId, planeCode, key);
        var current = ClampCultureRaw(previous + delta);
        if (global::System.MathF.Abs(current - previous) <= float.Epsilon)
        {
            return 0f;
        }

        EnsureProvenanceOutputCapacity(context);
        _cultureHighWatermark = global::System.Math.Max(_cultureHighWatermark, cultureId);
        _mapLayers.RawCultureInfluence.SetValue(cultureId, planeCode, key, q, r, z, current);
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.CellCultureInfluenceChanged,
            key,
            cultureId,
            0,
            planeCode,
            ProvenanceOutputKindCodes.CellCultureInfluenceChanged);
        RecalculateCultureInfluenceAtCell(
            planeCode,
            q,
            r,
            z,
            key,
            cultureId,
            previous,
            current,
            tick,
            context);
        return current - previous;
    }

    internal float ModifyCellCultureInfluenceRawFromEntity(
        int entityId,
        int planeCode,
        int q,
        int r,
        float delta,
        int tick,
        in KernelExecutionContext context) =>
        ModifyCellCultureInfluenceRawFromEntity(entityId, planeCode, q, r, z: 0, delta, tick, context);

    internal float ModifyCellCultureInfluenceRawFromEntity(
        int entityId,
        int planeCode,
        int q,
        int r,
        int z,
        float delta,
        int tick,
        in KernelExecutionContext context)
    {
        EnsureEntity(entityId);
        var cultureId = EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return 0f;
        }

        var weightedDelta = delta * EffectiveCultureInfluenceMultiplier(entityId);
        var applied = ModifyCellCultureInfluenceRaw(
            cultureId,
            planeCode,
            q,
            r,
            z,
            weightedDelta,
            tick,
            context);
        if (applied > 0f)
        {
            AddEntityInfluenceProduction(entityId, planeCode, applied);
        }

        return applied;
    }

    internal float ReduceOtherCultureSourcesAtCell(
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        float delta,
        int tick,
        in KernelExecutionContext context,
        global::System.Span<int> changedCultureIds,
        out int changedCultureCount) =>
        ReduceOtherCultureSourcesAtCell(
            ownCultureId,
            planeCode,
            q,
            r,
            z: 0,
            delta,
            tick,
            context,
            changedCultureIds,
            out changedCultureCount);

    internal float ReduceOtherCultureSourcesAtCell(
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        int z,
        float delta,
        int tick,
        in KernelExecutionContext context,
        global::System.Span<int> changedCultureIds,
        out int changedCultureCount)
    {
        ValidatePositive(ownCultureId);
        ValidateInfluencePlaneCode(planeCode);
        ValidatePositive(context.SourceCodeId);
        changedCultureCount = 0;
        if (delta <= 0f)
        {
            return 0f;
        }

        var removed = 0f;
        for (var cultureId = 1; cultureId <= CultureHighWatermark; cultureId++)
        {
            if (cultureId <= 0 || cultureId == ownCultureId)
            {
                continue;
            }

            var influence = GetCellCultureInfluenceRaw(cultureId, planeCode, q, r, z);
            if (influence <= 0f)
            {
                continue;
            }

            var appliedDelta = global::System.MathF.Min(delta, influence);
            var applied = ModifyCellCultureInfluenceRaw(cultureId, planeCode, q, r, z, -appliedDelta, tick, context);
            if (applied == 0f)
            {
                continue;
            }

            changedCultureIds[changedCultureCount] = cultureId;
            changedCultureCount++;
            removed -= applied;
        }

        return removed;
    }


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

        _entityCultureInfluenceMultiplierDeltas[entityId] += delta;
        _ = tick;
        _ = context;
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

        _cultureInfluenceMultiplierDeltas[cultureId] += delta;
        _ = tick;
        _ = context;
    }

    private void RecalculateCultureInfluenceAtCell(
        int planeCode,
        int q,
        int r,
        int z,
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
            var current = CultureDominanceShare(raw, total);
            var previousRaw = cultureId == changedCultureId ? changedPreviousRaw : raw;
            var currentRaw = cultureId == changedCultureId ? changedCurrentRaw : raw;
            var shareChanged = global::System.MathF.Abs(current - previous) > 0.0001f;
            var rawChanged = global::System.MathF.Abs(currentRaw - previousRaw) > 0.0001f;
            if (!shareChanged && !rawChanged)
            {
                continue;
            }

            if (shareChanged)
            {
                _mapLayers.CultureInfluence.SetValue(cultureId, planeCode, key, q, r, z, current);
            }

            Write(KernelEvent.From(new CellCultureInfluenceChangedKernelEvent(
                tick,
                WorldVersion,
                q,
                r,
                context.SourceCodeId,
                planeCode,
                cultureId,
                z,
                previous,
                current,
                previousRaw,
                currentRaw,
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

    private static float CultureDominanceShare(float raw, float total)
    {
        if (raw <= 0f || total <= 0f)
        {
            return 0f;
        }

        return raw * 100f / total;
    }

    private static float CultureMaturity(float raw) =>
        raw <= 0f ? 0f : global::System.MathF.Min(100f, raw * 100f / CultureFullRawMass);

    private static float ClampCultureRaw(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > CultureFullRawMass ? CultureFullRawMass : value;
    }

    private float EffectiveCultureInfluenceMultiplier(int entityId)
    {
        var cultureId = _entityCultureId[entityId];
        var favorDelta = CurrentTick < _entityFavorExpiresAtTick[entityId]
            ? _entityFavorMultiplierDelta[entityId]
            : 0f;
        var raw = 1f + _entityCultureInfluenceMultiplierDeltas[entityId] + favorDelta +
            (cultureId > 0 && cultureId < _cultureInfluenceMultiplierDeltas.Length
                ? _cultureInfluenceMultiplierDeltas[cultureId]
                : 0f);
        return global::System.Math.Clamp(raw, 0.1f, 3f);
    }

}
