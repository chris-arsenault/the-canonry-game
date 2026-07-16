namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class MapFrontierProgramRuntimeTests
{
    private const int TundraTerrain = 1;
    private const int DeepIceTerrain = 2;
    private const int ThawAgeId = 900;
    private const int ReclaimAgeId = 901;

    [Fact]
    public void MapFrontier_ThawsGroundWhereCultureMassPressesTheEdge()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        simulation.World.SetMapRadius(6);
        simulation.SeedCell(0, 0, TundraTerrain, ownerEntityId: 0);
        OpenIceAge(simulation, ThawAgeId);
        ConfigureFrontier(simulation, ThawAgeId, ReclaimAgeId);

        // No mass at the edge: nothing thaws.
        simulation.MapFrontier.Tick(simulation, tick: 10);
        Assert.False(simulation.World.CellIsKnown(PlaneCodes.Physical, 1, 0));

        // Mass at the edge cell opens its unknown neighbours.
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0, 40f, tick: 11);
        simulation.MapFrontier.Tick(simulation, tick: 12);

        Assert.True(simulation.World.CellIsKnown(PlaneCodes.Physical, 1, 0));
        Assert.Equal(TundraTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 1, 0));
        Assert.True(simulation.MapFrontier.State.TotalThawed > 0);
    }

    [Fact]
    public void MapFrontier_DoesNotThawBeyondMaxRadius()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        simulation.World.SetMapRadius(2);
        simulation.SeedCell(1, 0, TundraTerrain, ownerEntityId: 0);
        OpenIceAge(simulation, ThawAgeId);
        // Max radius 1: the cell at distance 1 may press inward but not past the cap.
        simulation.MapFrontier.SetAgeConfiguration(
            ThawAgeId, ReclaimAgeId, TundraTerrain, DeepIceTerrain,
            maxRadius: 1, frontierMassThreshold: 10f, reclaimMassCeiling: 1f,
            thawBudgetPerPulse: 24, refreezeBudgetPerPulse: 24);
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 1, 0, 40f, tick: 11);

        simulation.MapFrontier.Tick(simulation, tick: 12);

        // The outward neighbour (2, 0) is beyond the max radius and stays closed.
        Assert.False(simulation.World.CellIsKnown(PlaneCodes.Physical, 2, 0));
    }

    [Fact]
    public void MapFrontier_ReclaimsUntendedRimAsDeepIce()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        simulation.World.SetMapRadius(10);
        simulation.SeedCell(7, 0, TundraTerrain, ownerEntityId: 0);
        OpenIceAge(simulation, ReclaimAgeId);
        ConfigureFrontier(simulation, ThawAgeId, ReclaimAgeId);
        // (7,0) is a weakly-held edge cell beyond the heart floor (trace mass,
        // outward neighbour unknown), no road: the Long Dark takes it back.
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 7, 0, 0.5f, tick: 5);

        simulation.MapFrontier.Tick(simulation, tick: 20);

        Assert.Equal(DeepIceTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 7, 0));
        Assert.True(simulation.MapFrontier.State.TotalReclaimed > 0);
    }

    [Fact]
    public void MapFrontier_DoesNotReclaimCellsHoldingCultureMass()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        simulation.World.SetMapRadius(10);
        simulation.SeedCell(7, 0, TundraTerrain, ownerEntityId: 0);
        OpenIceAge(simulation, ReclaimAgeId);
        ConfigureFrontier(simulation, ThawAgeId, ReclaimAgeId);
        _ = simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, 7, 0, 20f, tick: 15);

        simulation.MapFrontier.Tick(simulation, tick: 20);

        Assert.Equal(TundraTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 7, 0));
    }

    private static void RegisterTerrains(Simulation simulation)
    {
        simulation.RegisterTerrain(
            TundraTerrain, boundaryGroupId: 1, movementCost: 3f,
            influenceMultiplier: 1f, claimCostMultiplier: 1f, claimChanceMultiplier: 1f,
            roadBuildMultiplier: 4f, roadSpeedMultiplier: 2f);
        simulation.RegisterTerrain(
            DeepIceTerrain, boundaryGroupId: 1, movementCost: 9f,
            influenceMultiplier: 0.2f, claimCostMultiplier: 3f, claimChanceMultiplier: 0.05f,
            roadBuildMultiplier: 0f, roadSpeedMultiplier: 1f);
    }

    private static void OpenIceAge(Simulation simulation, int ageId)
    {
        // An unconditional Ice-track age so OpenAgeId(Ice) equals ageId.
        _ = simulation.WorldBeats.AddAge(ageId, WorldBeatTrackCodes.World, priority: 10, minOpenTicks: 0, cooldownTicks: 0);
        simulation.WorldBeats.Tick(simulation, tick: 1);
    }

    private static void ConfigureFrontier(Simulation simulation, int thawAgeId, int reclaimAgeId) =>
        simulation.MapFrontier.SetAgeConfiguration(
            thawAgeId, reclaimAgeId, TundraTerrain, DeepIceTerrain,
            maxRadius: 40, frontierMassThreshold: 10f, reclaimMassCeiling: 1f,
            thawBudgetPerPulse: 24, refreezeBudgetPerPulse: 24);

    private static Simulation CreateSimulation() => KernelTestSimulation.Create(new SimulationOptions
    {
        EntityCapacity = 8,
        RelationshipCapacity = 8,
        RelationshipKindIdCapacity = 16,
        TerrainIdCapacity = 8,
        EventCapacity = 4096,
    });
}
