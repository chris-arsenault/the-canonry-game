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
        return _mapLayers.Coverage.ValueAt(planeCode, CoordinateKey(q, r));
    }

    internal void SetCoverageOwner(int q, int r, int ownerEntityId)
    {
        SetCoverageOwner(PlaneCodes.Physical, q, r, ownerEntityId);
    }

    internal void SetCoverageOwner(int planeCode, int q, int r, int ownerEntityId)
    {
        ValidateInfluencePlaneCode(planeCode);
        NumericGuard.ThrowIfNegative(ownerEntityId);
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
        NumericGuard.ThrowIfNegative(ownerEntityId);

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
        _mapCellTerrainCoverIds.Set(key, PlaneCodes.Physical, coverTerrainId);
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
            previousTerrainId, nextTerrainId, previousOwnerEntityId, ownerEntityId,
            superregionId, continentId, z)),
            context);
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
        value = CultureDominanceShare(
            rawUnits,
            _mapLayers.TotalRawCultureInfluenceAt(planeCode, CoordinateKey(q, r)));
        return true;
    }

    public FixedScalar GetCellCultureInfluence(int cultureId, int planeCode, int q, int r) =>
        GetCellCultureInfluence(cultureId, planeCode, q, r, z: 0);

    public FixedScalar GetCellCultureInfluence(int cultureId, int planeCode, int q, int r, int z)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        var key = CoordinateKey(q, r, z);
        return CultureDominanceShare(
            PositiveCultureInfluenceRawUnits(cultureId, planeCode, key),
            _mapLayers.TotalRawCultureInfluenceAt(planeCode, key));
    }

    internal FixedScalar GetCellCultureInfluenceRaw(int cultureId, int planeCode, int q, int r) =>
        GetCellCultureInfluenceRaw(cultureId, planeCode, q, r, z: 0);

    internal FixedScalar GetCellCultureInfluenceRaw(int cultureId, int planeCode, int q, int r, int z)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        return PositiveCultureInfluenceRaw(cultureId, planeCode, CoordinateKey(q, r, z));
    }

    internal int GetCellCultureInfluenceRawUnits(int cultureId, int planeCode, int q, int r, int z)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        return PositiveCultureInfluenceRawUnits(cultureId, planeCode, CoordinateKey(q, r, z));
    }

    internal long GetCellTotalCultureInfluenceRawUnits(int planeCode, int q, int r, int z)
    {
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.TotalRawCultureInfluenceAt(planeCode, CoordinateKey(q, r, z));
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

        var key = CoordinateKey(q, r, z);
        var previous = PositiveCultureInfluenceRawUnits(cultureId, planeCode, key);
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
        _mapLayers.RawCultureInfluence.SetValue(cultureId, planeCode, key, q, r, z, current);
        _mapLayers.AddTotalRawCultureInfluence(planeCode, key, current - previous);
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
            AddEntityInfluenceProduction(
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
        EnsureEntity(entityId);
        if (delta == 0)
        {
            return;
        }

        _entityCultureInfluenceMultiplierDeltas[entityId] = KernelFixedPoint.SaturatingAdd(
            _entityCultureInfluenceMultiplierDeltas[entityId],
            KernelFixedPoint.ModifierUnits(delta),
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
        ValidatePositive(cultureId);
        if ((uint)cultureId >= (uint)_cultureInfluenceMultiplierDeltas.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(cultureId));
        }

        if (delta == 0)
        {
            return;
        }

        _cultureInfluenceMultiplierDeltas[cultureId] = KernelFixedPoint.SaturatingAdd(
            _cultureInfluenceMultiplierDeltas[cultureId],
            KernelFixedPoint.ModifierUnits(delta),
            KernelFixedPoint.ModifierMinimumUnits,
            KernelFixedPoint.ModifierMaximumUnits);
        MarkCultureWorkDirty(cultureId);
        _ = tick;
        _ = context;
    }

    private void AddEntityInfluenceProduction(int entityId, int planeCode, int deltaUnits)
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

    private FixedScalar PositiveCultureInfluenceRaw(int cultureId, int planeCode, int key)
    {
        return KernelFixedPoint.CultureMassScalar(
            PositiveCultureInfluenceRawUnits(cultureId, planeCode, key));
    }

    private int PositiveCultureInfluenceRawUnits(int cultureId, int planeCode, int key)
    {
        var raw = _mapLayers.RawCultureInfluence.ValueAt(cultureId, planeCode, key);
        return raw > 0 ? raw : 0;
    }

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

    private FixedScalar EffectiveCultureInfluenceMultiplier(int entityId)
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
