namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class TravelReachCostTests
{
    private const int OpenTerrain = 1;
    private const int DeepIceTerrain = 2;
    private const int KindId = 1;

    [Fact]
    public void TravelReachCost_SumsTerrainCostAlongTheLine()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        simulation.World.SetMapRadius(4);
        for (var q = 0; q <= 3; q++)
        {
            simulation.SeedCell(q, 0, OpenTerrain, ownerEntityId: 0);
        }

        var actor = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        var target = simulation.CreateEntity(KindId, 1, 1, 3, 0, 0, 0, 0, tick: 0);

        // Three steps of open terrain (cost 3 each) between the endpoints.
        var cost = simulation.TravelReachCost(actor, target, PlaneCodes.Physical, tick: 1);
        Assert.Equal(9f, cost, precision: 2);
    }

    [Fact]
    public void TravelReachCost_IsInfiniteAcrossDeepIce()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        simulation.World.SetMapRadius(4);
        simulation.SeedCell(0, 0, OpenTerrain, ownerEntityId: 0);
        simulation.SeedCell(1, 0, DeepIceTerrain, ownerEntityId: 0);
        simulation.SeedCell(2, 0, OpenTerrain, ownerEntityId: 0);

        var actor = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        var target = simulation.CreateEntity(KindId, 1, 1, 2, 0, 0, 0, 0, tick: 0);

        var cost = simulation.TravelReachCost(actor, target, PlaneCodes.Physical, tick: 1);
        Assert.True(float.IsPositiveInfinity(cost));
    }

    [Fact]
    public void TravelReachCost_IsInfiniteAcrossUnthawedGround()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        simulation.World.SetMapRadius(4);
        simulation.SeedCell(0, 0, OpenTerrain, ownerEntityId: 0);
        // (1,0) is never materialized - the frontier has not thawed there.
        simulation.SeedCell(2, 0, OpenTerrain, ownerEntityId: 0);

        var actor = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        var target = simulation.CreateEntity(KindId, 1, 1, 2, 0, 0, 0, 0, tick: 0);

        Assert.True(float.IsPositiveInfinity(
            simulation.TravelReachCost(actor, target, PlaneCodes.Physical, tick: 1)));
    }

    [Fact]
    public void TravelReachCost_RoadsShrinkTheCost()
    {
        var simulation = CreateSimulation();
        RegisterTerrains(simulation);
        simulation.World.SetMapRadius(4);
        for (var q = 0; q <= 2; q++)
        {
            simulation.SeedCell(q, 0, OpenTerrain, ownerEntityId: 0);
        }

        var actor = simulation.CreateEntity(KindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        var target = simulation.CreateEntity(KindId, 1, 1, 2, 0, 0, 0, 0, tick: 0);
        var before = simulation.TravelReachCost(actor, target, PlaneCodes.Physical, tick: 1);

        // Walking the line lays road quality, which discounts the cost.
        for (var pass = 0; pass < 6; pass++)
        {
            _ = simulation.MoveEntityPhysical(actor, 1, 0, tick: 1, MovementCauseCodes.Travel);
            _ = simulation.MoveEntityPhysical(actor, 2, 0, tick: 1, MovementCauseCodes.Travel);
            _ = simulation.MoveEntityPhysical(actor, 0, 0, tick: 1, MovementCauseCodes.Travel);
        }

        var after = simulation.TravelReachCost(actor, target, PlaneCodes.Physical, tick: 1);
        Assert.True(after < before, $"expected roads to shrink reach cost: before {before}, after {after}");
    }

    private static void RegisterTerrains(Simulation simulation)
    {
        simulation.RegisterTerrain(
            OpenTerrain, boundaryGroupId: 1, movementCost: 3f,
            influenceMultiplier: 1f, claimCostMultiplier: 1f, claimChanceMultiplier: 1f,
            roadBuildMultiplier: 20f, roadSpeedMultiplier: 2f);
        // Deep ice is impassable: base travel cost 0 makes the surface cost
        // infinite, exactly as the domain's all-zero movement profiles do.
        simulation.RegisterTerrain(
            DeepIceTerrain, boundaryGroupId: 1, movementCost: 0f,
            influenceMultiplier: 0.2f, claimCostMultiplier: 3f, claimChanceMultiplier: 0.05f,
            roadBuildMultiplier: 0f, roadSpeedMultiplier: 1f);
    }

    private static Simulation CreateSimulation() => KernelTestSimulation.Create(new SimulationOptions
    {
        EntityCapacity = 8,
        RelationshipKindIdCapacity = 16,
        TerrainIdCapacity = 8,
        EventCapacity = 4096,
    });
}
