namespace Engine.Kernel;

using Engine.Constants;
using Engine.Eventing;

internal sealed partial class WorldState
{
    internal static readonly FixedScalar CultureTraceRawMass = 5;
    internal static readonly FixedScalar CultureClaimedRawMass = 30;
    internal static readonly FixedScalar CultureControlledRawMass = 100;
    internal static readonly FixedScalar CultureEntrenchedRawMass = 300;
    internal static readonly FixedScalar CultureNexusRawMass = 700;
    internal static readonly FixedScalar CultureFullRawMass = 2000;
    internal const int CultureFullRawMassUnits = 2000 * KernelFixedPoint.CultureMassScale;
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
        NumericGuard.ThrowIfNegative(radius);
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
            return z == 0 &&
                TryGetCellId(planeCode, q, r, z, out var semanticCellId) &&
                _mapCellKnown[semanticCellId];
        }

        if (z is < MinCellZ or > MaxCellZ)
        {
            return false;
        }

        return TryGetCellId(planeCode, q, r, z, out var physicalCellId) &&
            _mapCellKnown[physicalCellId];
    }

    public int CellTerrainId(int q, int r) => CellTerrainId(PlaneCodes.Physical, q, r);

    public int CellTerrainId(int planeCode, int q, int r) => CellTerrainId(planeCode, q, r, z: 0);

    public int CellTerrainId(int planeCode, int q, int r, int z)
    {
        ValidateInfluencePlaneCode(planeCode);
        if (!TryGetCellId(planeCode, q, r, z, out var cellId))
        {
            return 1;
        }

        if (planeCode == PlaneCodes.Physical && z == 0)
        {
            var coverTerrainId = _mapCellTerrainCoverIds[cellId];
            if (coverTerrainId > 0)
            {
                return coverTerrainId;
            }
        }

        var terrainId = _mapCellTerrainIds[cellId];
        return terrainId > 0 ? terrainId : 1;
    }

    internal int CellBaseTerrainId(int planeCode, int q, int r)
    {
        if (!TryGetCellId(planeCode, q, r, z: 0, out var cellId))
        {
            return 1;
        }

        var terrainId = _mapCellTerrainIds[cellId];
        return terrainId > 0 ? terrainId : 1;
    }

    internal int CellTerrainCoverId(int planeCode, int q, int r)
    {
        ValidateInfluencePlaneCode(planeCode);
        return planeCode == PlaneCodes.Physical &&
            TryGetCellId(planeCode, q, r, z: 0, out var cellId)
            ? _mapCellTerrainCoverIds[cellId]
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
        NumericGuard.ThrowIfNegative(ownerEntityId);
        NumericGuard.ThrowIfNegative(superregionId);
        NumericGuard.ThrowIfNegative(continentId);
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
        var cellId = _cells.GetOrAdd(planeCode, q, r, z);
        _mapCellKnown[cellId] = true;
        _mapCellTerrainIds[cellId] = terrainId;
        _mapCellSuperregionIds[cellId] = superregionId;
        _mapCellContinentIds[cellId] = continentId;
        _mapLayers.Coverage.SetValue(cellId, ownerEntityId);
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
        return TryGetCellId(planeCode, q, r, z: 0, out var cellId)
            ? _mapCellSuperregionIds[cellId]
            : 0;
    }

    internal int CellContinentId(int q, int r) => CellContinentId(PlaneCodes.Physical, q, r);

    internal int CellContinentId(int planeCode, int q, int r)
    {
        ValidateInfluencePlaneCode(planeCode);
        return TryGetCellId(planeCode, q, r, z: 0, out var cellId)
            ? _mapCellContinentIds[cellId]
            : 0;
    }

    public int TerrainBoundaryGroupId(int terrainId)
    {
        return _kernelData.Terrain.BoundaryGroupId(terrainId);
    }

    /// <summary>
    /// A quantized geography fact (PhysicalHexFactCodes) of a physical
    /// hex's latent geography; 0 outside the installed field.
    /// </summary>
    public int PhysicalHexFact(int q, int r, int factCode) =>
        _kernelData.PhysicalGeography.Fact(q, r, factCode);

    internal int CoverageOwnerAt(int q, int r) => CoverageOwnerAt(PlaneCodes.Physical, q, r);

    internal int CoverageOwnerAt(int planeCode, int q, int r)
    {
        ValidateInfluencePlaneCode(planeCode);
        return TryGetCellId(planeCode, q, r, z: 0, out var cellId)
            ? _mapLayers.Coverage.ValueAt(cellId)
            : 0;
    }

    internal void SetCoverageOwner(int q, int r, int ownerEntityId)
    {
        SetCoverageOwner(PlaneCodes.Physical, q, r, ownerEntityId);
    }

    internal void SetCoverageOwner(int planeCode, int q, int r, int ownerEntityId)
    {
        ValidateInfluencePlaneCode(planeCode);
        NumericGuard.ThrowIfNegative(ownerEntityId);
        _mapLayers.Coverage.SetValue(RequireCellId(planeCode, q, r, z: 0), ownerEntityId);
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
        NumericGuard.ThrowIfNegative(ownerEntityId);

        var cellId = _cells.GetOrAdd(planeCode, q, r, z);
        var previousTerrainId = CellTerrainId(planeCode, q, r, z);
        var previousBaseTerrainId = _mapCellTerrainIds[cellId] > 0 ? _mapCellTerrainIds[cellId] : 1;
        var previousOwnerEntityId = _mapLayers.Coverage.ValueAt(cellId);
        var superregionId = _mapCellSuperregionIds[cellId];
        var continentId = _mapCellContinentIds[cellId];
        if (previousBaseTerrainId == terrainId && previousOwnerEntityId == ownerEntityId)
        {
            return;
        }

        WriteCell(planeCode, q, r, z, terrainId, ownerEntityId, superregionId, continentId);
        if (previousOwnerEntityId > 0)
        {
            MarkEntityCultureWorkDirty(previousOwnerEntityId);
        }
        if (ownerEntityId > 0)
        {
            MarkEntityCultureWorkDirty(ownerEntityId);
        }
        var nextTerrainId = CellTerrainId(planeCode, q, r, z);
        AdvanceWorldVersion();
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
        var cellId = RequireCellId(PlaneCodes.Physical, q, r, z: 0);
        var previousCoverTerrainId = _mapCellTerrainCoverIds[cellId];
        if (previousCoverTerrainId == coverTerrainId)
        {
            return;
        }

        var previousTerrainId = CellTerrainId(PlaneCodes.Physical, q, r);
        var ownerEntityId = _mapLayers.Coverage.ValueAt(cellId);
        var superregionId = _mapCellSuperregionIds[cellId];
        var continentId = _mapCellContinentIds[cellId];
        _mapCellTerrainCoverIds[cellId] = coverTerrainId;
        var nextTerrainId = CellTerrainId(PlaneCodes.Physical, q, r);
        AdvanceWorldVersion();
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
            previousTerrainId, nextTerrainId,
            EntityOccurrenceHandleOrZero(previousOwnerEntityId),
            EntityOccurrenceHandleOrZero(ownerEntityId),
            superregionId, continentId, z)),
            context);
    }

    internal int PrepareOwnedCellState(int planeCode, int q, int r)
    {
        ValidateInfluencePlaneCode(planeCode);
        return _cells.GetOrAdd(planeCode, q, r, z: 0);
    }

    internal bool ApplyOwnedCellState(
        int cellId,
        int planeCode,
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int tick,
        in KernelExecutionContext context,
        out int previousOwnerEntityId,
        out bool terrainChanged)
    {
        ValidatePositive(context.SourceCodeId);
        ValidateInfluencePlaneCode(planeCode);
        ValidatePositive(terrainId);
        NumericGuard.ThrowIfNegative(ownerEntityId);
        var previousTerrainId = CellTerrainId(planeCode, q, r);
        var previousBaseTerrainId =
            _mapCellTerrainIds[cellId] > 0 ? _mapCellTerrainIds[cellId] : 1;
        previousOwnerEntityId = _mapLayers.Coverage.ValueAt(cellId);
        terrainChanged = previousTerrainId != terrainId;
        if (previousBaseTerrainId == terrainId &&
            previousOwnerEntityId == ownerEntityId)
        {
            return false;
        }

        var superregionId = _mapCellSuperregionIds[cellId];
        var continentId = _mapCellContinentIds[cellId];
        _mapCellKnown[cellId] = true;
        _mapCellTerrainIds[cellId] = terrainId;
        _ = _mapLayers.Coverage.SetDirectValue(cellId, ownerEntityId);
        if (previousOwnerEntityId > 0)
        {
            MarkEntityCultureWorkDirty(previousOwnerEntityId);
        }
        if (ownerEntityId > 0)
        {
            MarkEntityCultureWorkDirty(ownerEntityId);
        }

        var nextTerrainId = CellTerrainId(planeCode, q, r);
        terrainChanged = previousTerrainId != nextTerrainId;
        AdvanceWorldVersion();
        WriteCellStateEvents(
            planeCode,
            q,
            r,
            z: 0,
            tick,
            previousTerrainId,
            nextTerrainId,
            previousOwnerEntityId,
            ownerEntityId,
            superregionId,
            continentId,
            context);
        return true;
    }

    internal void ReconcileOwnedCellState(
        int cellId,
        int planeCode,
        int q,
        int r,
        int previousOwnerEntityId,
        int ownerEntityId)
    {
        _mapLayers.Coverage.ReconcileValueChange(
            cellId,
            previousOwnerEntityId,
            ownerEntityId);
        AdvanceMapExtent(planeCode, q, r);
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

    internal void ReleaseCoverageOwner(
        int ownerEntityId,
        int tick,
        in KernelExecutionContext context)
    {
        ReleaseCoverageOwner(ownerEntityId, PlaneCodes.Physical, tick, context);
        ReleaseCoverageOwner(ownerEntityId, PlaneCodes.Semantic, tick, context);
    }

    private void ReleaseCoverageOwner(
        int ownerEntityId,
        int planeCode,
        int tick,
        in KernelExecutionContext context)
    {
        while (_mapLayers.Coverage.ValueCellCount(ownerEntityId, planeCode) > 0)
        {
            var cursor = _mapLayers.Coverage.CellsOfValue(ownerEntityId, planeCode);
            if (!_mapLayers.Coverage.MoveNextCell(ref cursor, out var q, out var r))
            {
                throw new global::System.InvalidOperationException();
            }

            SetCellState(
                planeCode,
                q,
                r,
                CellBaseTerrainId(planeCode, q, r),
                ownerEntityId: 0,
                tick,
                context);
        }
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
        return _mapLayers.RawCultureInfluence.ValueCellCount(cultureId, planeCode);
    }

    internal SpatialLayerCellCursor CultureInfluenceCells(int cultureId, int planeCode)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.RawCultureInfluence.CellsOfValue(cultureId, planeCode);
    }

    internal bool MoveNextCultureInfluenceCell(ref SpatialLayerCellCursor cursor, out int q, out int r, out FixedScalar value)
    {
        if (!_mapLayers.RawCultureInfluence.MoveNextCell(ref cursor, out q, out r, out var rawUnits))
        {
            value = 0;
            return false;
        }

        var planeCode = cursor.Value & 3;
        var cellId = RequireCellId(planeCode, q, r, z: 0);
        value = CultureDominanceShare(
            rawUnits,
            _mapLayers.TotalRawCultureInfluenceAt(cellId));
        return true;
    }

    public FixedScalar GetCellCultureInfluence(int cultureId, int planeCode, int q, int r) =>
        GetCellCultureInfluence(cultureId, planeCode, q, r, z: 0);

    public FixedScalar GetCellCultureInfluence(int cultureId, int planeCode, int q, int r, int z)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        if (!TryGetCellId(planeCode, q, r, z, out var cellId))
        {
            return 0;
        }

        return CultureDominanceShare(
            PositiveCultureInfluenceRawUnits(cultureId, cellId),
            _mapLayers.TotalRawCultureInfluenceAt(cellId));
    }

    internal FixedScalar GetCellCultureInfluenceRaw(int cultureId, int planeCode, int q, int r) =>
        GetCellCultureInfluenceRaw(cultureId, planeCode, q, r, z: 0);

    internal FixedScalar GetCellCultureInfluenceRaw(int cultureId, int planeCode, int q, int r, int z)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        return TryGetCellId(planeCode, q, r, z, out var cellId)
            ? PositiveCultureInfluenceRaw(cultureId, cellId)
            : 0;
    }

    internal int GetCellCultureInfluenceRawUnits(int cultureId, int planeCode, int q, int r, int z)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        return TryGetCellId(planeCode, q, r, z, out var cellId)
            ? PositiveCultureInfluenceRawUnits(cultureId, cellId)
            : 0;
    }

    internal long GetCellTotalCultureInfluenceRawUnits(int planeCode, int q, int r, int z)
    {
        ValidateInfluencePlaneCode(planeCode);
        return TryGetCellId(planeCode, q, r, z, out var cellId)
            ? _mapLayers.TotalRawCultureInfluenceAt(cellId)
            : 0;
    }

    internal FixedScalar GetCellCultureMaturity(int cultureId, int planeCode, int q, int r)
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
        out FixedScalar raw)
    {
        var moved = _mapLayers.RawCultureInfluence.MoveNextCell(ref cursor, out q, out r, out var units);
        raw = KernelFixedPoint.CultureMassScalar(units);
        return moved;
    }

    internal bool MoveNextRawCultureInfluenceCell(
        ref SpatialLayerCellCursor cursor,
        out int q,
        out int r,
        out int z,
        out FixedScalar raw)
    {
        var moved = _mapLayers.RawCultureInfluence.MoveNextCell(ref cursor, out q, out r, out z, out var units);
        raw = KernelFixedPoint.CultureMassScalar(units);
        return moved;
    }

    internal bool MoveNextRawCultureInfluenceCellUnits(
        ref SpatialLayerCellCursor cursor,
        out int q,
        out int r,
        out int z,
        out int rawUnits)
    {
        return _mapLayers.RawCultureInfluence.MoveNextCell(
            ref cursor,
            out q,
            out r,
            out z,
            out rawUnits);
    }

    internal FixedScalar ModifyCellCultureInfluenceRaw(
        int cultureId,
        int planeCode,
        int q,
        int r,
        FixedScalar delta,
        int tick,
        in KernelExecutionContext context) =>
        ModifyCellCultureInfluenceRaw(cultureId, planeCode, q, r, z: 0, delta, tick, context);

    internal FixedScalar ModifyCellCultureInfluenceRaw(
        int cultureId,
        int planeCode,
        int q,
        int r,
        int z,
        FixedScalar delta,
        int tick,
        in KernelExecutionContext context)
    {
        return KernelFixedPoint.CultureMassScalar(
            ModifyCellCultureInfluenceRawUnits(
                cultureId,
                planeCode,
                q,
                r,
                z,
                KernelFixedPoint.CultureMassUnits(delta),
                tick,
                context));
    }

    internal int ModifyCellCultureInfluenceRawUnits(
        int cultureId,
        int planeCode,
        int q,
        int r,
        int z,
        int deltaUnits,
        int tick,
        in KernelExecutionContext context)
    {
        EnsureCultureIndex(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        ValidateCellZ(z);
        ValidatePositive(context.SourceCodeId);
        if (deltaUnits == 0)
        {
            return 0;
        }

        var cellId = _cells.GetOrAdd(planeCode, q, r, z);
        var previous = PositiveCultureInfluenceRawUnits(cultureId, cellId);
        var current = KernelFixedPoint.SaturatingAdd(
            previous,
            deltaUnits,
            0,
            CultureFullRawMassUnits);
        if (current == previous)
        {
            return 0;
        }

        _cultureHighWatermark = global::System.Math.Max(_cultureHighWatermark, cultureId);
        _mapLayers.RawCultureInfluence.SetValue(cultureId, cellId, current);
        _mapLayers.AddTotalRawCultureInfluence(cellId, current - previous);
        if (planeCode == PlaneCodes.Physical)
        {
            _totalPhysicalCultureRawMass = checked(
                _totalPhysicalCultureRawMass + current - previous);
        }

        MarkCultureFieldDirty(cultureId);
        _ = tick;
        _ = context;
        return current - previous;
    }

    internal FixedScalar ModifyCellCultureInfluenceRawFromEntity(
        int entityId,
        int planeCode,
        int q,
        int r,
        FixedScalar delta,
        int tick,
        in KernelExecutionContext context) =>
        ModifyCellCultureInfluenceRawFromEntity(entityId, planeCode, q, r, z: 0, delta, tick, context);

    internal FixedScalar ModifyCellCultureInfluenceRawFromEntity(
        int entityId,
        int planeCode,
        int q,
        int r,
        int z,
        FixedScalar delta,
        int tick,
        in KernelExecutionContext context)
    {
        EnsureEntity(entityId);
        var cultureId = EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return 0;
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
        if (applied > 0)
        {
            NoteEntityInfluenceProduction(
                entityId,
                planeCode,
                KernelFixedPoint.CultureMassUnits(applied));
        }

        return applied;
    }

    internal FixedScalar ReduceOtherCultureSourcesAtCell(
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        FixedScalar delta,
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

    internal FixedScalar ReduceOtherCultureSourcesAtCell(
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        int z,
        FixedScalar delta,
        int tick,
        in KernelExecutionContext context,
        global::System.Span<int> changedCultureIds,
        out int changedCultureCount)
    {
        ValidatePositive(ownCultureId);
        ValidateInfluencePlaneCode(planeCode);
        ValidatePositive(context.SourceCodeId);
        changedCultureCount = 0;
        if (delta <= 0)
        {
            return 0;
        }

        var removed = FixedScalar.Zero;
        for (var cultureId = 1; cultureId <= CultureHighWatermark; cultureId++)
        {
            if (cultureId <= 0 || cultureId == ownCultureId)
            {
                continue;
            }

            var influence = GetCellCultureInfluenceRaw(cultureId, planeCode, q, r, z);
            if (influence <= 0)
            {
                continue;
            }

            var appliedDelta = FixedScalar.Min(delta, influence);
            var applied = ModifyCellCultureInfluenceRaw(cultureId, planeCode, q, r, z, -appliedDelta, tick, context);
            if (applied == 0)
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
        FixedScalar delta,
        int tick,
        in KernelExecutionContext context)
    {
        ModifyEntityCultureInfluenceMultiplierDeltaUnits(
            entityId,
            KernelFixedPoint.ModifierUnits(delta),
            tick,
            context);
    }

    internal void ModifyEntityCultureInfluenceMultiplierDeltaUnits(
        int entityId,
        int deltaUnits,
        int tick,
        in KernelExecutionContext context)
    {
        EnsureEntity(entityId);
        if (deltaUnits == 0)
        {
            return;
        }

        _entityCultureInfluenceMultiplierDeltas[entityId] = KernelFixedPoint.SaturatingAdd(
            _entityCultureInfluenceMultiplierDeltas[entityId],
            deltaUnits,
            KernelFixedPoint.ModifierMinimumUnits,
            KernelFixedPoint.ModifierMaximumUnits);
        MarkEntityCultureWorkDirty(entityId);
        _ = tick;
        _ = context;
    }

    internal void ModifyCultureInfluenceMultiplierDelta(
        int cultureId,
        FixedScalar delta,
        int tick,
        in KernelExecutionContext context)
    {
        ModifyCultureInfluenceMultiplierDeltaUnits(
            cultureId,
            KernelFixedPoint.ModifierUnits(delta),
            tick,
            context);
    }

    internal void ModifyCultureInfluenceMultiplierDeltaUnits(
        int cultureId,
        int deltaUnits,
        int tick,
        in KernelExecutionContext context)
    {
        ValidatePositive(cultureId);
        if ((uint)cultureId >= (uint)_cultureInfluenceMultiplierDeltas.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(cultureId));
        }

        if (deltaUnits == 0)
        {
            return;
        }

        _cultureInfluenceMultiplierDeltas[cultureId] = KernelFixedPoint.SaturatingAdd(
            _cultureInfluenceMultiplierDeltas[cultureId],
            deltaUnits,
            KernelFixedPoint.ModifierMinimumUnits,
            KernelFixedPoint.ModifierMaximumUnits);
        MarkCultureWorkDirty(cultureId);
        _ = tick;
        _ = context;
    }

    internal void NoteEntityInfluenceProduction(int entityId, int planeCode, int deltaUnits)
    {
        if (planeCode == PlaneCodes.Semantic)
        {
            _entitySemanticInfluenceProduction[entityId] = KernelFixedPoint.SaturatingAdd(
                _entitySemanticInfluenceProduction[entityId],
                deltaUnits,
                0,
                KernelFixedPoint.CultureMassMaximumUnits);
            MarkEntityCultureWorkDirty(entityId);
            return;
        }

        _entityPhysicalInfluenceProduction[entityId] = KernelFixedPoint.SaturatingAdd(
            _entityPhysicalInfluenceProduction[entityId],
            deltaUnits,
            0,
            KernelFixedPoint.CultureMassMaximumUnits);
        MarkEntityCultureWorkDirty(entityId);
    }

    private FixedScalar PositiveCultureInfluenceRaw(int cultureId, int cellId)
    {
        return KernelFixedPoint.CultureMassScalar(
            PositiveCultureInfluenceRawUnits(cultureId, cellId));
    }

    internal int PositiveCultureInfluenceRawUnits(int cultureId, int cellId)
    {
        var raw = _mapLayers.RawCultureInfluence.ValueAt(cultureId, cellId);
        return raw > 0 ? raw : 0;
    }

    internal bool TryGetCellId(int planeCode, int q, int r, int z, out int cellId)
    {
        ValidateInfluencePlaneCode(planeCode);
        return _cells.TryGet(planeCode, q, r, z, out cellId);
    }

    internal int RequireCellId(int planeCode, int q, int r, int z)
    {
        ValidateInfluencePlaneCode(planeCode);
        return _cells.Require(planeCode, q, r, z);
    }

    internal int CellPlaneCode(int cellId) => _cells.PlaneCode(cellId);

    internal int CellQ(int cellId) => _cells.Q(cellId);

    internal int CellR(int cellId) => _cells.R(cellId);

    internal int CellZ(int cellId) => _cells.Z(cellId);

    private static FixedScalar CultureDominanceShare(int rawUnits, long totalUnits)
    {
        if (rawUnits <= 0 || totalUnits <= 0)
        {
            return 0;
        }

        return FixedScalar.FromRatio((long)rawUnits * 100, totalUnits);
    }

    private static FixedScalar CultureMaturity(FixedScalar raw) =>
        raw <= 0 ? 0 : FixedScalar.Min(100, raw * 100 / CultureFullRawMass);

    internal FixedScalar EffectiveCultureInfluenceMultiplier(int entityId)
    {
        var cultureId = _entityCultureId[entityId];
        var favorDelta = CurrentTick < _entityFavorExpiresAtTick[entityId]
            ? _entityFavorMultiplierDelta[entityId]
            : 0;
        var rawUnits = (long)KernelFixedPoint.ModifierScale +
            _entityCultureInfluenceMultiplierDeltas[entityId] + favorDelta +
            (cultureId > 0 && cultureId < _cultureInfluenceMultiplierDeltas.Length
                ? _cultureInfluenceMultiplierDeltas[cultureId]
                : 0);
        var raw = (FixedScalar)rawUnits / KernelFixedPoint.ModifierScale;
        return FixedScalar.Clamp(raw, FixedScalar.FromUnits(100000L), 3);
    }

}
