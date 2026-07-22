namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class Simulation
{
    internal bool PhysicalTerrainCanEnterProfile(int movementProfileId, int q, int r)
    {
        return TerrainCanEnterProfile(PlaneCodes.Physical, movementProfileId, q, r);
    }

    internal bool TerrainCanEnterProfile(int planeCode, int movementProfileId, int q, int r)
    {
        return Movement.CanEnterProfile(this, planeCode, movementProfileId, q, r);
    }

    internal float PhysicalTravelCostProfile(int movementProfileId, int q, int r, int tick)
    {
        return TravelCostProfile(PlaneCodes.Physical, movementProfileId, q, r, tick);
    }

    internal float TravelCostProfile(int planeCode, int movementProfileId, int q, int r, int tick)
    {
        return Movement.TravelCostProfile(this, planeCode, movementProfileId, q, r, tick);
    }

    private const float CultureGrowthFloor = 0.25f;
    private const float CultureGrowthTasteWeight = 0.75f;
    private const float CultureGrowthMinimum = 0.1f;
    private const float CultureGrowthMaximum = 2.5f;

    /// <summary>
    /// How readily culture takes root on a physical hex: the land's derived
    /// capacity sets the base and the culture's taste bends it, so identity
    /// reads geography when territory grows, not just when entities place.
    /// Replaces the authored per-terrain influence table on the physical
    /// plane; semantic ground keeps its authored multiplier - ideas have no
    /// facts to read.
    /// </summary>
    internal float CultureGrowthMultiplier(int cultureId, int q, int r)
    {
        // No latent facts, no opinion: worlds without installed physical
        // geography keep neutral growth everywhere.
        if (!KernelData.PhysicalGeography.TryGetCell(q, r, out _, out _, out _))
        {
            return 1f;
        }

        var multiplier = CultureGrowthFloor + TasteCapacityValue(q, r);
        if (cultureId > 0)
        {
            multiplier += CultureGrowthTasteWeight * PlacementTasteScore(0, cultureId, q, r) / TasteScoreScale;
        }

        if (multiplier < CultureGrowthMinimum)
        {
            return CultureGrowthMinimum;
        }

        return multiplier > CultureGrowthMaximum ? CultureGrowthMaximum : multiplier;
    }

    internal float PhysicalInfluenceMultiplier(int kindId, int q, int r)
    {
        return TerrainInfluenceMultiplier(PlaneCodes.Physical, kindId, q, r);
    }

    internal float TerrainInfluenceMultiplier(int planeCode, int kindId, int q, int r)
    {
        if (!World.CellIsKnown(planeCode, q, r))
        {
            return 1f;
        }

        return KernelData.Terrain.InfluenceMultiplier(World.CellTerrainId(planeCode, q, r), kindId);
    }

    internal float PhysicalClaimCostMultiplier(int kindId, int q, int r)
    {
        return TerrainClaimCostMultiplier(PlaneCodes.Physical, kindId, q, r);
    }

    internal float TerrainClaimCostMultiplier(int planeCode, int kindId, int q, int r)
    {
        if (!World.CellIsKnown(planeCode, q, r))
        {
            return 1f;
        }

        return KernelData.Terrain.ClaimCostMultiplier(World.CellTerrainId(planeCode, q, r), kindId);
    }

    internal float PhysicalClaimChanceMultiplier(int kindId, int q, int r)
    {
        return TerrainClaimChanceMultiplier(PlaneCodes.Physical, kindId, q, r);
    }

    internal float TerrainClaimChanceMultiplier(int planeCode, int kindId, int q, int r)
    {
        if (!World.CellIsKnown(planeCode, q, r))
        {
            return 1f;
        }

        return KernelData.Terrain.ClaimChanceMultiplier(World.CellTerrainId(planeCode, q, r), kindId);
    }

    internal void SeedCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId = 0,
        int continentId = 0)
    {
        SeedCell(PlaneCodes.Physical, q, r, terrainId, ownerEntityId, superregionId, continentId);
    }

    internal void SeedCell(
        int planeCode,
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId = 0,
        int continentId = 0)
    {
        AssertStartupSeedTick();
        CreateCell(planeCode, q, r, terrainId, ownerEntityId, superregionId, continentId);
        if (planeCode == PlaneCodes.Physical)
        {
            ApplyInitialTerrainCover(q, r, CurrentTick);
        }
    }

    /// <summary>
    /// Applies the transient cover a hex starts under, if its latent
    /// geography declares one (sea ice over frozen water). Called once when
    /// a physical cell first materializes - at startup seed or frontier
    /// reveal - never on later terrain changes.
    /// </summary>
    internal void ApplyInitialTerrainCover(int q, int r, int tick)
    {
        var coverTerrainId = KernelData.PhysicalGeography.InitialCoverTerrainId(q, r);
        if (coverTerrainId > 0)
        {
            SetTerrainCover(q, r, coverTerrainId, tick);
        }
    }

    internal void CreateCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId = 0,
        int continentId = 0)
    {
        CreateCell(PlaneCodes.Physical, q, r, terrainId, ownerEntityId, superregionId, continentId);
    }

    internal void CreateCell(
        int planeCode,
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId = 0,
        int continentId = 0)
    {
        AssertExecutionSource();
        World.MaterializeCell(planeCode, q, r, terrainId, ownerEntityId, superregionId, continentId);
        Movement.RecordTopologyChanged();
        WriteKernelEvent(KernelEvent.From(new MapCellStateChangedKernelEvent(
            CurrentTick,
            WorldVersion,
            planeCode,
            q,
            r,
            _executionContext.SourceCodeId,
            0,
            terrainId,
            0,
            ownerEntityId,
            superregionId,
            continentId,
            0)));
        if (planeCode == PlaneCodes.Physical)
        {
            WriteKernelEvent(KernelEvent.From(new CellCreatedKernelEvent(
                CurrentTick,
                WorldVersion,
                q,
                r,
                _executionContext.SourceCodeId,
                terrainId,
                ownerEntityId,
                superregionId,
                continentId,
                0)));
        }
    }

    internal bool TryForgeCellVertical(int entityId, int direction, int terrainId, int tick)
    {
        if (direction is not (1 or -1) || terrainId <= 0)
        {
            return false;
        }

        var world = World;
        var q = world.EntityPhysicalQ(entityId);
        var r = world.EntityPhysicalR(entityId);
        if (!world.CellIsKnown(PlaneCodes.Physical, q, r))
        {
            return false;
        }

        if (TryForgeColumnExtension(entityId, q, r, direction, terrainId, tick))
        {
            return true;
        }

        return TryForgeNeighborColumn(entityId, q, r, direction, terrainId, tick);
    }

    private bool TryForgeColumnExtension(int entityId, int q, int r, int direction, int terrainId, int tick)
    {
        var world = World;
        var z = direction;
        while (z >= WorldState.MinCellZ && z <= WorldState.MaxCellZ)
        {
            if (!world.CellIsKnown(PlaneCodes.Physical, q, r, z))
            {
                ForgeCell(entityId, q, r, z, terrainId, tick);
                return true;
            }

            z += direction;
        }

        return false;
    }

    private bool TryForgeNeighborColumn(int entityId, int q, int r, int direction, int terrainId, int tick)
    {
        // The actor's own column is fully forged in this direction; creep the
        // first forged layer into an adjacent known column so galleries and
        // spire clusters spread instead of stalling.
        var world = World;
        var start = NextInt(6);
        for (var i = 0; i < 6; i++)
        {
            var directionIndex = (start + i) % 6;
            var neighborQ = q + HexDirectionQ(directionIndex);
            var neighborR = r + HexDirectionR(directionIndex);
            if (!world.CellIsKnown(PlaneCodes.Physical, neighborQ, neighborR) ||
                world.CellIsKnown(PlaneCodes.Physical, neighborQ, neighborR, direction))
            {
                continue;
            }

            ForgeCell(entityId, neighborQ, neighborR, direction, terrainId, tick);
            return true;
        }

        return false;
    }

    private void ForgeCell(int entityId, int q, int r, int z, int terrainId, int tick)
    {
        World.WriteForgedCell(PlaneCodes.Physical, q, r, z, terrainId);
        Movement.RecordTopologyChanged();
        WriteKernelEvent(KernelEvent.From(new CellCreatedKernelEvent(
            CurrentTick,
            WorldVersion,
            q,
            r,
            _executionContext.SourceCodeId,
            terrainId,
            0,
            0,
            0,
            z)));
        var cultureId = World.EntityCultureId(entityId);
        if (cultureId > 0)
        {
            _ = ModifyCellCultureInfluenceRaw(
                cultureId,
                PlaneCodes.Physical,
                q,
                r,
                z,
                WorldState.CultureClaimedRawMass,
                tick);
        }
    }

    private static int HexDirectionQ(int direction) => direction switch
    {
        0 => 1,
        1 => 1,
        2 => 0,
        3 => -1,
        4 => -1,
        _ => 0,
    };

    private static int HexDirectionR(int direction) => direction switch
    {
        0 => 0,
        1 => -1,
        2 => -1,
        3 => 0,
        4 => 1,
        _ => 1,
    };

    internal void SetCellState(int q, int r, int terrainId, int ownerEntityId, int tick)
    {
        SetCellState(PlaneCodes.Physical, q, r, terrainId, ownerEntityId, tick);
    }

    internal void SetCellState(int planeCode, int q, int r, int terrainId, int ownerEntityId, int tick)
    {
        var wasKnown = World.CellIsKnown(planeCode, q, r);
        var previousTerrainId = wasKnown ? World.CellTerrainId(planeCode, q, r) : 0;
        World.SetCellState(planeCode, q, r, terrainId, ownerEntityId, tick, _executionContext);
        if (!wasKnown || previousTerrainId != World.CellTerrainId(planeCode, q, r))
        {
            Movement.RecordTopologyChanged();
        }
    }

    internal void SetCoverageOwner(int q, int r, int ownerEntityId, int tick)
    {
        SetCoverageOwner(PlaneCodes.Physical, q, r, ownerEntityId, tick);
    }

    internal void SetCoverageOwner(int planeCode, int q, int r, int ownerEntityId, int tick)
    {
        World.SetCoverageOwner(planeCode, q, r, ownerEntityId, tick, _executionContext);
    }

    internal void SetCellTerrain(int q, int r, int terrainId, int tick)
    {
        SetCellTerrain(PlaneCodes.Physical, q, r, terrainId, tick);
    }

    internal void SetCellTerrain(int planeCode, int q, int r, int terrainId, int tick)
    {
        var wasKnown = World.CellIsKnown(planeCode, q, r);
        var previousTerrainId = wasKnown ? World.CellTerrainId(planeCode, q, r) : 0;
        World.SetCellTerrain(planeCode, q, r, terrainId, tick, _executionContext);
        if (!wasKnown || previousTerrainId != World.CellTerrainId(planeCode, q, r))
        {
            Movement.RecordTopologyChanged();
        }
    }

    internal void SetTerrainCover(int q, int r, int coverTerrainId, int tick)
    {
        var previousTerrainId = World.CellTerrainId(PlaneCodes.Physical, q, r);
        World.SetTerrainCover(q, r, coverTerrainId, tick, _executionContext);
        if (previousTerrainId != World.CellTerrainId(PlaneCodes.Physical, q, r))
        {
            Movement.RecordTopologyChanged();
        }
    }

    internal void ClearTerrainCover(int q, int r, int tick)
    {
        var previousTerrainId = World.CellTerrainId(PlaneCodes.Physical, q, r);
        World.ClearTerrainCover(q, r, tick, _executionContext);
        if (previousTerrainId != World.CellTerrainId(PlaneCodes.Physical, q, r))
        {
            Movement.RecordTopologyChanged();
        }
    }

    private const float TasteScoreScale = 600f;
    private const float TasteCapacityNormal = 4f;

    internal void RegisterKindPlacementTaste(int kindId, int axisCode, float weight) =>
        KernelData.PlacementTaste.RegisterKindWeight(kindId, axisCode, weight);

    internal void RegisterCulturePlacementTaste(int cultureId, int axisCode, float weight) =>
        KernelData.PlacementTaste.RegisterCultureWeight(cultureId, axisCode, weight);

    /// <summary>
    /// The placement taste score of a physical hex for a kind placed under
    /// a culture: each signed taste weight times its normalized axis value,
    /// summed. Identity reads geography here - a kind's needs and a
    /// culture's relationship to the world, stated once, price every hex.
    /// </summary>
    internal float PlacementTasteScore(int kindId, int cultureId, int q, int r)
    {
        var taste = KernelData.PlacementTaste;
        var score = 0f;
        for (var axis = 0; axis < PlacementTasteAxisCodes.Count; axis++)
        {
            var weight = taste.CombinedWeight(kindId, cultureId, axis);
            if (weight != 0f)
            {
                score += weight * TasteAxisValue(axis, q, r);
            }
        }

        return score * TasteScoreScale;
    }

    // Fact-backed axes map straight to their fact column; the three derived
    // axes (capacity, water fraction, coast) compute below. Indexed by axis.
    private static readonly int[] TasteAxisFactCodes =
    [
        -1,
        PhysicalHexFactCodes.Temperature,
        PhysicalHexFactCodes.Moisture,
        PhysicalHexFactCodes.Elevation,
        PhysicalHexFactCodes.Relief,
        PhysicalHexFactCodes.IceThickness,
        PhysicalHexFactCodes.RiverCoverage,
        PhysicalHexFactCodes.FireMean,
        PhysicalHexFactCodes.AuroraMean,
        PhysicalHexFactCodes.PressureMean,
        -1,
        -1,
    ];

    private float TasteAxisValue(int axis, int q, int r)
    {
        var factCode = TasteAxisFactCodes[axis];
        if (factCode >= 0)
        {
            return FactValue(q, r, factCode);
        }

        if (axis == PlacementTasteAxisCodes.Capacity)
        {
            return TasteCapacityValue(q, r);
        }

        return axis == PlacementTasteAxisCodes.WaterFraction
            ? 1f - FactValue(q, r, PhysicalHexFactCodes.LandFraction)
            : TasteCoastAdjacency(q, r);
    }

    private float FactValue(int q, int r, int factCode) =>
        KernelData.PhysicalGeography.Fact(q, r, factCode) / 255f;

    private float TasteCapacityValue(int q, int r)
    {
        var capacity = MapSignals.Data.HasCapacityFormula
            ? MapSignals.PhysicalBaseCapacity(World, q, r)
            : TasteFallbackCapacity(q, r);
        var normalized = capacity / TasteCapacityNormal;
        if (normalized < 0f)
        {
            return 0f;
        }

        return normalized > 1f ? 1f : normalized;
    }

    private float TasteFallbackCapacity(int q, int r) =>
        KernelData.PhysicalGeography.TryGetCell(q, r, out var terrainId, out _, out _)
            ? MapSignals.Data.TerrainBaseCapacity(terrainId)
            : 0f;

    // Coast is derived, never stored: a hex whose latent terrain sits on
    // the opposite side of the land/water divide from any neighbour.
    private float TasteCoastAdjacency(int q, int r)
    {
        if (!KernelData.PhysicalGeography.TryGetCell(q, r, out var terrainId, out _, out _))
        {
            return 0f;
        }

        var water = KernelData.Terrain.IsWater(terrainId);
        for (var direction = 0; direction < 6; direction++)
        {
            if (KernelData.PhysicalGeography.TryGetCell(
                    q + HexDirectionQ(direction),
                    r + HexDirectionR(direction),
                    out var neighborTerrainId,
                    out _,
                    out _) &&
                KernelData.Terrain.IsWater(neighborTerrainId) != water)
            {
                return 1f;
            }
        }

        return 0f;
    }

    /// <summary>
    /// Travel-cost reach between two entities on a plane: the summed
    /// road-discounted terrain cost of the cells along the hex line between
    /// them. Impassable or un-thawed ground on the line (deep ice, unknown
    /// cells) makes the route infinite, so a growing map and its frozen
    /// frontier gate reach the way real geography does; roads shrink it.
    /// </summary>
    internal float TravelReachCost(int actorId, int targetId, int planeCode, int tick)
    {
        if (actorId <= 0 || targetId <= 0 ||
            !World.EntityIsActive(actorId) || !World.EntityIsActive(targetId))
        {
            return float.PositiveInfinity;
        }

        var (fromQ, fromR) = PlaneCoords(actorId, planeCode);
        var (toQ, toR) = PlaneCoords(targetId, planeCode);
        var steps = KernelQuery.HexDistance(fromQ, fromR, toQ, toR);
        if (steps == 0)
        {
            return 0f;
        }

        var total = 0f;
        var previousQ = fromQ;
        var previousR = fromR;
        for (var step = 1; step <= steps; step++)
        {
            var t = step / (float)steps;
            var (q, r) = HexRound(
                fromQ + ((toQ - fromQ) * t),
                fromR + ((toR - fromR) * t));
            var cellCost = Movement.DirectedTravelCost(this, planeCode, actorId, previousQ, previousR, q, r, tick);
            if (float.IsPositiveInfinity(cellCost))
            {
                return float.PositiveInfinity;
            }

            total += cellCost;
            previousQ = q;
            previousR = r;
        }

        return total;
    }

    private (int Q, int R) PlaneCoords(int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic
            ? (World.EntitySemanticQ(entityId), World.EntitySemanticR(entityId))
            : (World.EntityPhysicalQ(entityId), World.EntityPhysicalR(entityId));

    private static (int Q, int R) HexRound(float q, float r)
    {
        var s = -q - r;
        var rq = global::System.MathF.Round(q, global::System.MidpointRounding.AwayFromZero);
        var rr = global::System.MathF.Round(r, global::System.MidpointRounding.AwayFromZero);
        var rs = global::System.MathF.Round(s, global::System.MidpointRounding.AwayFromZero);
        var dq = global::System.MathF.Abs(rq - q);
        var dr = global::System.MathF.Abs(rr - r);
        var ds = global::System.MathF.Abs(rs - s);
        if (dq > dr && dq > ds)
        {
            rq = -rr - rs;
        }
        else if (dr > ds)
        {
            rr = -rq - rs;
        }

        return ((int)rq, (int)rr);
    }
}
