namespace Engine.Kernel;

internal sealed partial class MapFrontierProgramRuntime
{
    // Cells examined per run while hunting freeze/melt work. Bounds the
    // scan so a large live map costs the same per run as a small one.
    private const int SeaIceScanVisitBudget = 512;

    /// <summary>
    /// The frozen sea breathes with the Ice-track ages. Re-freeze ages
    /// extend sea-ice cover over eligible water from coasts and existing
    /// ice; thaw ages melt the cover back from its fringe. A persistent
    /// rotating cursor over the live disk keeps each run bounded and free
    /// of directional bias.
    /// </summary>
    private void UpdateSeaIce(Simulation simulation, int tick, bool freeze)
    {
        var world = simulation.World;
        var radius = world.PhysicalMapRadius;
        var budget = freeze ? Data.SeaIceFreezeBudgetPerRun : Data.SeaIceMeltBudgetPerRun;
        if (Data.SeaIceCoverTerrainId <= 0 || budget <= 0 || radius <= 0)
        {
            return;
        }

        ScanSeaIce(simulation, world, radius, budget, freeze, tick);
    }

    private void ScanSeaIce(
        Simulation simulation,
        WorldState world,
        int radius,
        int budget,
        bool freeze,
        int tick)
    {
        var diameter = (radius * 2) + 1;
        var cellCount = diameter * diameter;
        var visited = 0;
        var changed = 0;
        while (visited < SeaIceScanVisitBudget && visited < cellCount && changed < budget)
        {
            if (State.SeaIceCursor >= cellCount)
            {
                State.SeaIceCursor = 0;
            }

            var index = State.SeaIceCursor++;
            visited++;
            var q = (index / diameter) - radius;
            var r = (index % diameter) - radius;
            var updated = freeze
                ? TryFreezeSeaIce(simulation, world, q, r, tick)
                : TryMeltSeaIce(simulation, world, q, r, tick);
            changed += updated ? 1 : 0;
        }
    }

    // Freezing spreads inward from shores and existing ice: open eligible
    // water freezes only beside land, cover, or the unrevealed frontier.
    private bool TryFreezeSeaIce(Simulation simulation, WorldState world, int q, int r, int tick)
    {
        if (!world.CellIsKnown(PlaneCodes.Physical, q, r) ||
            world.CellTerrainCoverId(PlaneCodes.Physical, q, r) != 0 ||
            !Data.SeaIceEligibleTerrain(world.CellTerrainId(PlaneCodes.Physical, q, r)) ||
            !SeaIceCanSpreadTo(world, q, r))
        {
            return false;
        }

        simulation.SetTerrainCover(q, r, Data.SeaIceCoverTerrainId, tick);
        return true;
    }

    // Melt works the fringe: covered water beside any uncovered known cell
    // (open water or shore) thaws first, so lanes reopen from their edges.
    private bool TryMeltSeaIce(Simulation simulation, WorldState world, int q, int r, int tick)
    {
        if (!world.CellIsKnown(PlaneCodes.Physical, q, r) ||
            world.CellTerrainCoverId(PlaneCodes.Physical, q, r) != Data.SeaIceCoverTerrainId ||
            !SeaIceHasOpenNeighbor(world, q, r))
        {
            return false;
        }

        simulation.ClearTerrainCover(q, r, tick);
        return true;
    }

    private bool SeaIceCanSpreadTo(WorldState world, int q, int r)
    {
        for (var direction = 0; direction < 6; direction++)
        {
            var nq = q + NeighborDQ[direction];
            var nr = r + NeighborDR[direction];
            if (!world.CellIsKnown(PlaneCodes.Physical, nq, nr) ||
                world.CellTerrainCoverId(PlaneCodes.Physical, nq, nr) != 0 ||
                !Data.SeaIceEligibleTerrain(world.CellTerrainId(PlaneCodes.Physical, nq, nr)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SeaIceHasOpenNeighbor(WorldState world, int q, int r)
    {
        for (var direction = 0; direction < 6; direction++)
        {
            var nq = q + NeighborDQ[direction];
            var nr = r + NeighborDR[direction];
            if (world.CellIsKnown(PlaneCodes.Physical, nq, nr) &&
                world.CellTerrainCoverId(PlaneCodes.Physical, nq, nr) == 0)
            {
                return true;
            }
        }

        return false;
    }

    public void SetSeaIceConfiguration(
        int coverTerrainId,
        int freezeBudgetPerRun,
        int meltBudgetPerRun)
    {
        NumericGuard.ThrowIfNegative(coverTerrainId);
        NumericGuard.ThrowIfNegative(freezeBudgetPerRun);
        NumericGuard.ThrowIfNegative(meltBudgetPerRun);
        Data.SeaIceCoverTerrainId = coverTerrainId;
        Data.SeaIceFreezeBudgetPerRun = freezeBudgetPerRun;
        Data.SeaIceMeltBudgetPerRun = meltBudgetPerRun;
    }

    public void AddSeaIceEligibleTerrain(int terrainId) =>
        Data.AddSeaIceEligibleTerrain(terrainId);
}
