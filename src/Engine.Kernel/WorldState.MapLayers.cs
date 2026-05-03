namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class WorldState
{
    public int MapRadius => _mapRadius;

    public void SetMapRadius(int radius)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(radius);
        _mapRadius = radius;
    }

    public void SetCell(int q, int r, int terrainId)
    {
        ValidatePositive(terrainId);
        WriteCell(q, r, terrainId, CoverageOwnerAt(q, r), CellSuperregionId(q, r), CellContinentId(q, r));
    }

    public bool CellIsKnown(int q, int r)
    {
        return _mapCellTerrainIds.TryGet(CoordinateKey(q, r), 1, out _);
    }

    public int CellTerrainId(int q, int r)
    {
        return _mapCellTerrainIds.GetOrDefault(CoordinateKey(q, r), 1);
    }

    public void SetCellTerrain(int q, int r, int terrainId)
    {
        ValidatePositive(terrainId);
        WriteCell(q, r, terrainId, CoverageOwnerAt(q, r), CellSuperregionId(q, r), CellContinentId(q, r));
    }

    public void SetCellTerrain(int q, int r, int terrainId, int tick, in KernelExecutionContext context)
    {
        SetCellState(q, r, terrainId, CoverageOwnerAt(q, r), tick, context);
    }

    public void MaterializeCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId = 0,
        int continentId = 0)
    {
        ValidatePositive(terrainId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(ownerEntityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(superregionId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(continentId);
        WriteCell(q, r, terrainId, ownerEntityId, superregionId, continentId);
        AdvanceWorldVersion();
    }

    private void WriteCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId,
        int continentId)
    {
        var key = CoordinateKey(q, r);
        _mapCellTerrainIds.Set(key, 1, terrainId);
        _mapCellSuperregionIds.Set(key, 1, superregionId);
        _mapCellContinentIds.Set(key, 1, continentId);
        _mapLayers.Coverage.SetValue(key, q, r, ownerEntityId);
    }

    internal int CellSuperregionId(int q, int r)
    {
        return _mapCellSuperregionIds.GetOrDefault(CoordinateKey(q, r), 1);
    }

    internal int CellContinentId(int q, int r)
    {
        return _mapCellContinentIds.GetOrDefault(CoordinateKey(q, r), 1);
    }

    public int TerrainBoundaryGroupId(int terrainId)
    {
        return _kernelData.Terrain.BoundaryGroupId(terrainId);
    }

    internal int CoverageOwnerAt(int q, int r)
    {
        return _mapLayers.Coverage.ValueAt(CoordinateKey(q, r));
    }

    internal void SetCoverageOwner(int q, int r, int ownerEntityId)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(ownerEntityId);
        _mapLayers.Coverage.SetValue(CoordinateKey(q, r), q, r, ownerEntityId);
    }

    internal void SetCoverageOwner(int q, int r, int ownerEntityId, int tick, in KernelExecutionContext context)
    {
        SetCellState(q, r, CellTerrainId(q, r), ownerEntityId, tick, context);
    }

    internal void SetCellState(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int tick,
        in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        ValidatePositive(terrainId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(ownerEntityId);

        var previousTerrainId = CellTerrainId(q, r);
        var previousOwnerEntityId = CoverageOwnerAt(q, r);
        var superregionId = CellSuperregionId(q, r);
        var continentId = CellContinentId(q, r);
        if (previousTerrainId == terrainId && previousOwnerEntityId == ownerEntityId)
        {
            return;
        }

        EnsureProvenanceOutputCapacity(context);
        WriteCell(q, r, terrainId, ownerEntityId, superregionId, continentId);
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.CellChanged,
            CoordinateKey(q, r),
            ownerEntityId,
            0,
            terrainId,
            ProvenanceOutputKindCodes.CellChanged);
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

    internal int CoverageOwnerCellCount(int ownerEntityId)
    {
        EnsureEntity(ownerEntityId);
        return _mapLayers.Coverage.ValueCellCount(ownerEntityId);
    }

    internal bool CoverageOwnerHasCells(int ownerEntityId)
    {
        EnsureEntity(ownerEntityId);
        return _mapLayers.Coverage.ValueCellCount(ownerEntityId) > 0;
    }

    internal SpatialLayerCellCursor CoverageCells(int ownerEntityId)
    {
        EnsureEntity(ownerEntityId);
        return _mapLayers.Coverage.CellsOfValue(ownerEntityId);
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
}
