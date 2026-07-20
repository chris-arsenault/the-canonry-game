namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

/// <summary>
/// Family statements of the terrain layer: authored repaints never cross
/// the land/water divide, deep ice never replaces an existing cover, and
/// broken ground slows travel within a terrain.
/// </summary>
public sealed class TerrainFamilyRuleTests
{
    private const int LandTerrain = 1;
    private const int WaterTerrain = 2;
    private const int OtherLandTerrain = 3;
    private const int KindId = 1;

    [Fact]
    public void AuthoredRepaints_NeverCrossTheLandWaterDivide()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        simulation.World.SetMapRadius(4);
        simulation.SeedCell(0, 0, LandTerrain, ownerEntityId: 0);
        simulation.SeedCell(1, 0, WaterTerrain, ownerEntityId: 0);

        // Land to land applies; land to water and water to land are skipped.
        KernelEffectExecutor.SetCellTerrain(simulation, 0, 0, OtherLandTerrain);
        Assert.Equal(OtherLandTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 0, 0));

        KernelEffectExecutor.SetCellTerrain(simulation, 0, 0, WaterTerrain);
        Assert.Equal(OtherLandTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 0, 0));

        KernelEffectExecutor.SetCellTerrain(simulation, 1, 0, LandTerrain);
        Assert.Equal(WaterTerrain, simulation.World.CellTerrainId(PlaneCodes.Physical, 1, 0));
    }

    [Fact]
    public void Roughness_SlowsTravelWithinATerrain()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        simulation.Movement.Data.RoughnessCostWeight = 0.5f;
        simulation.KernelData.PhysicalGeography.BeginInstall(4);
        InstallWithRoughness(simulation, 0, 0, roughness: 0);
        InstallWithRoughness(simulation, 1, 0, roughness: 255);
        simulation.World.SetMapRadius(4);
        simulation.SeedCell(0, 0, LandTerrain, ownerEntityId: 0);
        simulation.SeedCell(1, 0, LandTerrain, ownerEntityId: 0);
        var actor = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);

        var smooth = simulation.Movement.TravelCost(simulation, PlaneCodes.Physical, actor, 0, 0, tick: 1);
        var rough = simulation.Movement.TravelCost(simulation, PlaneCodes.Physical, actor, 1, 0, tick: 1);

        Assert.Equal(smooth * 1.5f, rough, precision: 2);
    }

    private static void InstallWithRoughness(Simulation simulation, int q, int r, byte roughness)
    {
        var facts = new byte[PhysicalHexFactCodes.Count];
        facts[PhysicalHexFactCodes.Roughness] = roughness;
        simulation.KernelData.PhysicalGeography.InstallCell(
            q, r, LandTerrain, superregionId: 1, continentId: 1,
            crossingWord: 0, fieldWord: 0, initialCoverTerrainId: 0, facts);
    }

    private static void RegisterTerrains(Simulation simulation)
    {
        simulation.RegisterTerrain(
            LandTerrain, boundaryGroupId: 1, movementCost: 3f,
            influenceMultiplier: 1f, claimCostMultiplier: 1f, claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f, roadSpeedMultiplier: 1f);
        simulation.RegisterTerrain(
            WaterTerrain, boundaryGroupId: 2, movementCost: 4f,
            influenceMultiplier: 1f, claimCostMultiplier: 1f, claimChanceMultiplier: 1f,
            roadBuildMultiplier: 0f, roadSpeedMultiplier: 1f);
        simulation.KernelData.Terrain.SetWater(WaterTerrain);
        simulation.RegisterTerrain(
            OtherLandTerrain, boundaryGroupId: 1, movementCost: 5f,
            influenceMultiplier: 1f, claimCostMultiplier: 1f, claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f, roadSpeedMultiplier: 1f);
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
