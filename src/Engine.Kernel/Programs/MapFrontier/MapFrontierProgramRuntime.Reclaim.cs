namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MapFrontierProgramRuntime
{
    /// <summary>
    /// The Long Dark's reach: follow the retreating frontier. As culture
    /// abandons the edge, cells fall through the low-mass band; a weakly-held
    /// edge cell (raw at or below the reclaim ceiling) that no road crosses is
    /// taken back as impassable ice, and the freeze propagates inward pulse by
    /// pulse as the interior cools behind it. The dense heart core, held at
    /// full mass, is never touched; only weakly-held edges recede. Two-phase: collect from the culture cursor,
    /// then freeze, because freezing mutates the field the cursor walks.
    /// </summary>
    // Never reclaim within this radius of origin: a minimal core always
    // survives the Long Dark, so the map recedes but never fully dissolves.
    private const int ReclaimHeartFloor = 6;

    /// <summary>
    /// The dark presses in on every frontier. Scan the frontier box for cold
    /// edge cells - known, weakly-held (mass at or below the ceiling), no road,
    /// touching unknown or already-frozen ground - and take them back as ice.
    /// The global decay tide the reclaim age charges cools abandoned rim below
    /// the ceiling; the freeze propagates inward as the interior cools behind
    /// it. This scans a bounded box, but only while the reclaim age holds the
    /// Ice track (deep stagnation), never on the ordinary hot path.
    /// </summary>
    private void ReclaimRim(Simulation simulation, int tick)
    {
        var world = simulation.World;
        var count = CollectReclaimCandidates(simulation, world);
        for (var i = 0; i < count; i++)
        {
            simulation.SetCellTerrain(PlaneCodes.Physical, _candidateQ[i], _candidateR[i], Data.FrozenTerrainId, tick);
            State.TotalReclaimed++;
        }
    }

    private int CollectReclaimCandidates(Simulation simulation, WorldState world)
    {
        var budget = global::System.Math.Min(Data.RefreezeBudgetPerPulse, _candidateQ.Length);
        var extent = Data.MaxRadius;
        var count = 0;
        for (var q = -extent; q <= extent && count < budget; q++)
        {
            var minR = global::System.Math.Max(-extent, -q - extent);
            var maxR = global::System.Math.Min(extent, -q + extent);
            for (var r = minR; r <= maxR && count < budget; r++)
            {
                if (HexDistanceFromOrigin(q, r) > ReclaimHeartFloor && ShouldReclaim(simulation, world, q, r))
                {
                    _candidateQ[count] = q;
                    _candidateR[count] = r;
                    count++;
                }
            }
        }

        return count;
    }

    private bool ShouldReclaim(Simulation simulation, WorldState world, int q, int r)
    {
        if (!world.CellIsKnown(PlaneCodes.Physical, q, r) ||
            world.CellTerrainId(PlaneCodes.Physical, q, r) == Data.FrozenTerrainId ||
            world.CultureFieldAt(1, PlaneCodes.Physical, q, r).TotalRawMass > Data.ReclaimMassCeiling ||
            simulation.Movement.RoadTravelCount(PlaneCodes.Physical, q, r) > 0)
        {
            return false;
        }

        return IsEdgeCell(world, q, r);
    }

    private bool IsEdgeCell(WorldState world, int q, int r)
    {
        for (var direction = 0; direction < 6; direction++)
        {
            if (!world.CellIsKnown(PlaneCodes.Physical, q + NeighborDQ[direction], r + NeighborDR[direction]) ||
                world.CellTerrainId(PlaneCodes.Physical, q + NeighborDQ[direction], r + NeighborDR[direction]) == Data.FrozenTerrainId)
            {
                return true;
            }
        }

        return false;
    }

    public void SetAgeConfiguration(
        int thawAgeId,
        int refreezeAgeId,
        int thawTerrainId,
        int frozenTerrainId,
        int maxRadius,
        float frontierMassThreshold,
        float reclaimMassCeiling,
        int thawBudgetPerPulse,
        int refreezeBudgetPerPulse)
    {
        Data.ThawAgeId = thawAgeId;
        Data.RefreezeAgeId = refreezeAgeId;
        Data.ThawTerrainId = thawTerrainId;
        Data.FrozenTerrainId = frozenTerrainId;
        Data.MaxRadius = maxRadius;
        Data.FrontierMassThreshold = frontierMassThreshold;
        Data.ReclaimMassCeiling = reclaimMassCeiling;
        Data.ThawBudgetPerPulse = thawBudgetPerPulse;
        Data.RefreezeBudgetPerPulse = refreezeBudgetPerPulse;
    }
}
