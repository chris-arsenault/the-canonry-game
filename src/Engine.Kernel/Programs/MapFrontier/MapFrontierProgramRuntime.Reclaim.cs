namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MapFrontierProgramRuntime
{
    /// <summary>
    /// The Long Dark's reach: revisit thawed ground and take back any rim
    /// cell no culture holds and no road crosses. Only cells this program
    /// opened are candidates, and only those still at the edge (touching
    /// unknown or already-frozen ground) - the interior stays warm.
    /// </summary>
    private void ReclaimRim(Simulation simulation, int tick)
    {
        var world = simulation.World;
        var budget = Data.RefreezeBudgetPerPulse;
        var reclaimed = 0;
        var visited = 0;
        var slot = (State.ThawedNext - 1 + State.Capacity) % State.Capacity;
        while (visited < State.ThawedCount && reclaimed < budget)
        {
            var q = State.ThawedQ[slot];
            var r = State.ThawedR[slot];
            if (ShouldReclaim(simulation, world, q, r))
            {
                simulation.SetCellTerrain(PlaneCodes.Physical, q, r, Data.FrozenTerrainId, tick);
                State.TotalReclaimed++;
                reclaimed++;
            }

            slot = (slot - 1 + State.Capacity) % State.Capacity;
            visited++;
        }
    }

    private bool ShouldReclaim(Simulation simulation, WorldState world, int q, int r)
    {
        if (!world.CellIsKnown(PlaneCodes.Physical, q, r) ||
            world.CellTerrainId(PlaneCodes.Physical, q, r) == Data.FrozenTerrainId)
        {
            return false;
        }

        if (world.CultureFieldAt(1, PlaneCodes.Physical, q, r).TotalRawMass > Data.ReclaimMassCeiling ||
            simulation.Movement.RoadTravelCount(PlaneCodes.Physical, q, r) > 0)
        {
            return false;
        }

        return IsRimCell(world, q, r);
    }

    private bool IsRimCell(WorldState world, int q, int r)
    {
        for (var direction = 0; direction < 6; direction++)
        {
            var nq = q + NeighborDQ[direction];
            var nr = r + NeighborDR[direction];
            if (!world.CellIsKnown(PlaneCodes.Physical, nq, nr) ||
                world.CellTerrainId(PlaneCodes.Physical, nq, nr) == Data.FrozenTerrainId)
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
