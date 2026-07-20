namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

/// <summary>
/// Physical carrying capacity derives from the hex's real facts through one
/// authored formula: warmth sets suitability, rivers and floodplains enrich
/// land, broken relief suppresses it, and open water carries its own
/// temperature-driven marine capacity.
/// </summary>
public sealed class PhysicalCapacityFormulaTests
{
    private const int OpenTerrain = 1;

    [Fact]
    public void WarmWetFlatLand_OutcarriesColdBrokenLand()
    {
        var simulation = CreateSimulation();
        InstallCells(simulation);

        var verdant = simulation.MapSignals.PhysicalBaseCapacity(simulation.World, 0, 0);
        var frozenWaste = simulation.MapSignals.PhysicalBaseCapacity(simulation.World, 1, 0);
        var warmSea = simulation.MapSignals.PhysicalBaseCapacity(simulation.World, 2, 0);

        Assert.True(verdant > 3f, $"warm river valley should be rich, got {verdant}");
        Assert.True(frozenWaste < 0.5f, $"cold broken ground should be nearly barren, got {frozenWaste}");
        Assert.True(warmSea is > 0.2f and < 2f, $"warm open water should carry modest marine life, got {warmSea}");
        Assert.True(verdant > warmSea);
        Assert.True(warmSea > frozenWaste);
    }

    [Fact]
    public void GeothermalHeat_CreatesRefugesInColdGround()
    {
        var simulation = CreateSimulation();
        InstallCells(simulation);

        var coldPlain = simulation.MapSignals.PhysicalBaseCapacity(simulation.World, 0, 1);
        var geothermalRefuge = simulation.MapSignals.PhysicalBaseCapacity(simulation.World, 1, 1);

        Assert.True(
            geothermalRefuge > coldPlain + 0.5f,
            $"geothermal heat should open a refuge: cold {coldPlain}, refuge {geothermalRefuge}");
    }

    private static void InstallCells(Simulation simulation)
    {
        simulation.RegisterTerrain(
            OpenTerrain, boundaryGroupId: 1, movementCost: 3f,
            influenceMultiplier: 1f, claimCostMultiplier: 1f, claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f, roadSpeedMultiplier: 1f);
        simulation.MapSignals.Data.SetCapacityFormula(
            coldFloor: 46f,
            warmPeak: 127f,
            geothermalBlend: 0.5f,
            freshwaterWeight: 0.6f,
            fertileWeight: 0.8f,
            reliefComfort: 0.25f,
            reliefPenalty: 1.2f,
            roughnessPenalty: 0.5f,
            landScale: 4f,
            marineScale: 1.6f);
        simulation.KernelData.PhysicalGeography.BeginInstall(4);

        // (0,0) warm, wet, flat river valley - fully land.
        Install(simulation, 0, 0, temperature: 130, land: 255, river: 90, floodplain: 60, relief: 20, roughness: 20, geothermal: 0);

        // (1,0) cold, broken ground.
        Install(simulation, 1, 0, temperature: 40, land: 255, river: 0, floodplain: 0, relief: 200, roughness: 160, geothermal: 0);

        // (2,0) warm open water.
        Install(simulation, 2, 0, temperature: 130, land: 0, river: 0, floodplain: 0, relief: 0, roughness: 0, geothermal: 0);

        // (0,1) cold flat plain; (1,1) the same plain over a geothermal field.
        Install(simulation, 0, 1, temperature: 50, land: 255, river: 0, floodplain: 0, relief: 20, roughness: 20, geothermal: 0);
        Install(simulation, 1, 1, temperature: 50, land: 255, river: 0, floodplain: 0, relief: 20, roughness: 20, geothermal: 180);
    }

    private static void Install(
        Simulation simulation,
        int q,
        int r,
        byte temperature,
        byte land,
        byte river,
        byte floodplain,
        byte relief,
        byte roughness,
        byte geothermal)
    {
        var facts = new byte[PhysicalHexFactCodes.Count];
        facts[PhysicalHexFactCodes.Temperature] = temperature;
        facts[PhysicalHexFactCodes.LandFraction] = land;
        facts[PhysicalHexFactCodes.RiverCoverage] = river;
        facts[PhysicalHexFactCodes.FloodplainCoverage] = floodplain;
        facts[PhysicalHexFactCodes.Relief] = relief;
        facts[PhysicalHexFactCodes.Roughness] = roughness;
        facts[PhysicalHexFactCodes.Geothermal] = geothermal;
        simulation.KernelData.PhysicalGeography.InstallCell(
            q, r, OpenTerrain, superregionId: 1, continentId: 1,
            crossingWord: 0, fieldWord: 0, initialCoverTerrainId: 0, facts);
        simulation.World.SetMapRadius(4);
        simulation.SeedCell(q, r, OpenTerrain, ownerEntityId: 0);
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
