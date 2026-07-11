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

    internal void SetCellState(int q, int r, int terrainId, int ownerEntityId, int tick)
    {
        SetCellState(PlaneCodes.Physical, q, r, terrainId, ownerEntityId, tick);
    }

    internal void SetCellState(int planeCode, int q, int r, int terrainId, int ownerEntityId, int tick)
    {
        var wasKnown = World.CellIsKnown(planeCode, q, r);
        var previousTerrainId = wasKnown ? World.CellTerrainId(planeCode, q, r) : 0;
        World.SetCellState(planeCode, q, r, terrainId, ownerEntityId, tick, _executionContext);
        if (!wasKnown || previousTerrainId != terrainId)
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
        if (!wasKnown || previousTerrainId != terrainId)
        {
            Movement.RecordTopologyChanged();
        }
    }
}
