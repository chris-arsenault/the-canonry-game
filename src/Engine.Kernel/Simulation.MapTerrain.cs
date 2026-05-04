namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class Simulation
{
    internal bool PhysicalTerrainCanEnter(int kindId, int q, int r)
    {
        return TerrainCanEnter(PlaneCodes.Physical, kindId, q, r);
    }

    internal bool TerrainCanEnter(int planeCode, int kindId, int q, int r)
    {
        return Movement.CanEnter(this, planeCode, kindId, q, r);
    }

    internal float PhysicalTravelCost(int kindId, int q, int r, int tick)
    {
        return TravelCost(PlaneCodes.Physical, kindId, q, r, tick);
    }

    internal float TravelCost(int planeCode, int kindId, int q, int r, int tick)
    {
        return Movement.TravelCost(this, planeCode, kindId, q, r, tick);
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
                continentId)));
        }
    }

    internal void SetCellState(int q, int r, int terrainId, int ownerEntityId, int tick)
    {
        SetCellState(PlaneCodes.Physical, q, r, terrainId, ownerEntityId, tick);
    }

    internal void SetCellState(int planeCode, int q, int r, int terrainId, int ownerEntityId, int tick)
    {
        World.SetCellState(planeCode, q, r, terrainId, ownerEntityId, tick, _executionContext);
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
        World.SetCellTerrain(planeCode, q, r, terrainId, tick, _executionContext);
    }
}
