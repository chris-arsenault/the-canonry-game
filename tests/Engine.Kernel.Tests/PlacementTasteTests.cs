namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

/// <summary>
/// Placement taste: kind and culture weights over the hex-fact axes sum
/// into one score, so identity reads geography - an ocean culture prices a
/// coast differently from an ice culture without either naming a terrain.
/// </summary>
public sealed class PlacementTasteTests
{
    private const int LandTerrain = 1;
    private const int WaterTerrain = 2;
    private const int KindId = 3;
    private const int OceanCultureId = 1;
    private const int IceCultureId = 2;

    [Fact]
    public void CultureTaste_ReordersTheSameKindsHexPreferences()
    {
        var simulation = CreateSimulation();
        InstallWorld(simulation);

        // The ocean culture loves water and pressure; the ice culture loves
        // ice and shuns aurora. The kind itself only needs capacity.
        simulation.RegisterKindPlacementTaste(KindId, PlacementTasteAxisCodes.Capacity, 0.4f);
        simulation.RegisterCulturePlacementTaste(OceanCultureId, PlacementTasteAxisCodes.WaterFraction, 1f);
        simulation.RegisterCulturePlacementTaste(OceanCultureId, PlacementTasteAxisCodes.PressureMean, 0.5f);
        simulation.RegisterCulturePlacementTaste(IceCultureId, PlacementTasteAxisCodes.IceThickness, 1f);
        simulation.RegisterCulturePlacementTaste(IceCultureId, PlacementTasteAxisCodes.AuroraMean, -0.8f);

        var oceanAtSea = simulation.PlacementTasteScore(KindId, OceanCultureId, 1, 0);
        var oceanOnIce = simulation.PlacementTasteScore(KindId, OceanCultureId, 0, 1);
        var iceAtSea = simulation.PlacementTasteScore(KindId, IceCultureId, 1, 0);
        var iceOnIce = simulation.PlacementTasteScore(KindId, IceCultureId, 0, 1);

        Assert.True(oceanAtSea > oceanOnIce, $"ocean culture should prefer the sea: {oceanAtSea} vs {oceanOnIce}");
        Assert.True(iceOnIce > iceAtSea, $"ice culture should prefer the ice: {iceOnIce} vs {iceAtSea}");
    }

    [Fact]
    public void CoastAdjacency_DerivesFromTheLandWaterDivide()
    {
        var simulation = CreateSimulation();
        InstallWorld(simulation);
        simulation.RegisterKindPlacementTaste(KindId, PlacementTasteAxisCodes.CoastAdjacency, 1f);

        // (0,0) land beside water at (1,0) is coastal; (0,2) sits fully
        // inside the icefield with only land neighbours.
        var coastal = simulation.PlacementTasteScore(KindId, cultureId: 0, 0, 0);
        var interior = simulation.PlacementTasteScore(KindId, cultureId: 0, 0, 2);

        Assert.True(coastal > interior, $"coast should score above interior: {coastal} vs {interior}");
        Assert.Equal(0f, interior, precision: 2);
    }

    [Fact]
    public void UnregisteredTaste_ScoresZeroEverywhere()
    {
        var simulation = CreateSimulation();
        InstallWorld(simulation);

        Assert.Equal(0f, simulation.PlacementTasteScore(KindId, OceanCultureId, 0, 0), precision: 3);
    }

    private static void InstallWorld(Simulation simulation)
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
        simulation.KernelData.PhysicalGeography.BeginInstall(4);

        // (0,0) coastal land; (1,0) deep pressured sea; (0,1) aurora ice.
        Install(simulation, 0, 0, LandTerrain, land: 255, ice: 0, aurora: 0, pressure: 0);
        Install(simulation, 1, 0, WaterTerrain, land: 0, ice: 0, aurora: 0, pressure: 200);
        Install(simulation, 0, 1, LandTerrain, land: 255, ice: 220, aurora: 200, pressure: 0);
        Install(simulation, -1, 1, LandTerrain, land: 255, ice: 220, aurora: 200, pressure: 0);
        Install(simulation, 1, 1, LandTerrain, land: 255, ice: 220, aurora: 200, pressure: 0);
        Install(simulation, -1, 0, LandTerrain, land: 255, ice: 0, aurora: 0, pressure: 0);
        Install(simulation, 0, -1, LandTerrain, land: 255, ice: 0, aurora: 0, pressure: 0);
        Install(simulation, 1, -1, LandTerrain, land: 255, ice: 0, aurora: 0, pressure: 0);
        Install(simulation, 0, 2, LandTerrain, land: 255, ice: 220, aurora: 200, pressure: 0);
        Install(simulation, -1, 2, LandTerrain, land: 255, ice: 220, aurora: 200, pressure: 0);
        Install(simulation, 1, 2, LandTerrain, land: 255, ice: 220, aurora: 200, pressure: 0);
        simulation.World.SetMapRadius(4);
    }

    private static void Install(
        Simulation simulation,
        int q,
        int r,
        int terrainId,
        byte land,
        byte ice,
        byte aurora,
        byte pressure)
    {
        var facts = new byte[PhysicalHexFactCodes.Count];
        facts[PhysicalHexFactCodes.LandFraction] = land;
        facts[PhysicalHexFactCodes.IceThickness] = ice;
        facts[PhysicalHexFactCodes.AuroraMean] = aurora;
        facts[PhysicalHexFactCodes.PressureMean] = pressure;
        simulation.KernelData.PhysicalGeography.InstallCell(
            q, r, terrainId, superregionId: 1, continentId: 1,
            crossingWord: 0, fieldWord: 0, initialCoverTerrainId: 0, facts);
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
