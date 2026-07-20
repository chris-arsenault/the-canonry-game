namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

/// <summary>
/// Warmth bands shape carrying capacity and culture deposition through one
/// authored curve per system; unconfigured curves leave behavior
/// terrain-only.
/// </summary>
public sealed class WarmthCurveTests
{
    private const int OpenTerrain = 1;

    [Fact]
    public void WarmthCurve_TiltsCultureInfluenceOnPhysicalGround()
    {
        var simulation = CreateSimulation();
        InstallWarmAndColdCells(simulation);
        simulation.KernelData.Terrain.SetWarmthInfluenceCurve(0.5f, 1f, 1.2f, 1.5f);

        var warm = simulation.TerrainInfluenceMultiplier(PlaneCodes.Physical, kindId: 1, q: 1, r: 0);
        var cold = simulation.TerrainInfluenceMultiplier(PlaneCodes.Physical, kindId: 1, q: 0, r: 0);

        Assert.Equal(1.5f, warm, precision: 3);
        Assert.Equal(0.5f, cold, precision: 3);
    }

    [Fact]
    public void WarmthCurve_DefaultsToFlatWhenUnconfigured()
    {
        var simulation = CreateSimulation();
        InstallWarmAndColdCells(simulation);

        Assert.Equal(
            simulation.TerrainInfluenceMultiplier(PlaneCodes.Physical, kindId: 1, q: 0, r: 0),
            simulation.TerrainInfluenceMultiplier(PlaneCodes.Physical, kindId: 1, q: 1, r: 0),
            precision: 3);
        Assert.Equal(1f, simulation.MapSignals.Data.WarmthCapacityFactor(3), precision: 3);
    }

    [Fact]
    public void WarmthCapacityFactor_FollowsTheConfiguredCurve()
    {
        var simulation = CreateSimulation();
        simulation.MapSignals.Data.SetWarmthCapacityCurve(0.5f, 1f, 1.5f, 2.2f);

        Assert.Equal(0.5f, simulation.MapSignals.Data.WarmthCapacityFactor(0), precision: 3);
        Assert.Equal(2.2f, simulation.MapSignals.Data.WarmthCapacityFactor(3), precision: 3);
    }

    private static void InstallWarmAndColdCells(Simulation simulation)
    {
        simulation.RegisterTerrain(
            OpenTerrain, boundaryGroupId: 1, movementCost: 3f,
            influenceMultiplier: 1f, claimCostMultiplier: 1f, claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f, roadSpeedMultiplier: 1f);
        simulation.KernelData.PhysicalGeography.BeginInstall(4);
        simulation.KernelData.PhysicalGeography.InstallCell(
            0, 0, OpenTerrain, superregionId: 1, continentId: 1,
            crossingWord: 0, fieldWord: 0, initialCoverTerrainId: 0);
        simulation.KernelData.PhysicalGeography.InstallCell(
            1, 0, OpenTerrain, superregionId: 1, continentId: 1,
            crossingWord: 0,
            fieldWord: 3 << PhysicalHexFieldCodes.WarmthShift,
            initialCoverTerrainId: 0);
        simulation.World.SetMapRadius(4);
        simulation.SeedCell(0, 0, OpenTerrain, ownerEntityId: 0);
        simulation.SeedCell(1, 0, OpenTerrain, ownerEntityId: 0);
    }

    private static Simulation CreateSimulation() => KernelTestSimulation.Create(new SimulationOptions
    {
        EntityCapacity = 8,
        RelationshipKindIdCapacity = 16,
        TerrainIdCapacity = 8,
        EventCapacity = 4096,
        MapGenerationRadiusCapacity = 8,
    });
}
