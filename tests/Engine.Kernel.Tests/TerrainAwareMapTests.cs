namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class TerrainAwareMapTests
{
    [Fact]
    public void MovementOverTerrainBuildsRoadQualityThatReducesEffectiveTravelCost()
    {
        var simulation = new Simulation();
        const int terrainId = 1;
        const int kindId = 1;
        simulation.RegisterTerrain(
            terrainId,
            boundaryGroupId: 1,
            movementCost: 2f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 20f,
            roadSpeedMultiplier: 2f);
        simulation.World.SetMapRadius(1);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(1, 0, terrainId, ownerEntityId: 0);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        var result = simulation.MoveEntityPhysical(entityId, 1, 0, tick: 0, MovementCauseCodes.Travel);
        simulation.AdvanceTicks(1);

        Assert.True(result.Moved);
        Assert.True(simulation.Movement.RoadQuality(PlaneCodes.Physical, 1, 0, simulation.CurrentTick) > 0f);
        Assert.True(simulation.Movement.PhysicalTravelCost(simulation, kindId, 1, 0, simulation.CurrentTick) < 2f);
    }

    [Fact]
    public void PlacementMovementDoesNotBuildRoadQuality()
    {
        var simulation = new Simulation();
        const int terrainId = 1;
        const int kindId = 1;
        simulation.RegisterTerrain(
            terrainId,
            boundaryGroupId: 1,
            movementCost: 2f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 20f,
            roadSpeedMultiplier: 2f);
        simulation.World.SetMapRadius(1);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(1, 0, terrainId, ownerEntityId: 0);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        var result = simulation.MoveEntityPhysical(entityId, 1, 0, tick: 0, MovementCauseCodes.Placement);

        Assert.True(result.Moved);
        Assert.Equal(0f, simulation.Movement.RoadQuality(PlaneCodes.Physical, 1, 0, simulation.CurrentTick));
    }

    [Fact]
    public void MovementRoadQualityRoundTripsThroughSnapshot()
    {
        var simulation = new Simulation();
        const int terrainId = 1;
        const int kindId = 1;
        simulation.RegisterTerrain(
            terrainId,
            boundaryGroupId: 1,
            movementCost: 2f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 20f,
            roadSpeedMultiplier: 2f);
        simulation.World.SetMapRadius(1);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(1, 0, terrainId, ownerEntityId: 0);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.MoveEntityPhysical(entityId, 1, 0, tick: 0, MovementCauseCodes.Travel);
        using var stream = new global::System.IO.MemoryStream();

        KernelSnapshot.SaveKernelState(simulation, stream);
        stream.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(stream);

        Assert.Equal(1, loaded.Movement.RoadTravelCount(PlaneCodes.Physical, 1, 0));
        Assert.Equal(
            simulation.Movement.RoadQuality(PlaneCodes.Physical, 1, 0, simulation.CurrentTick),
            loaded.Movement.RoadQuality(PlaneCodes.Physical, 1, 0, loaded.CurrentTick));
    }

    [Fact]
    public void SemanticMovementUsesTerrainCostAndBuildsCellRoadQuality()
    {
        var simulation = new Simulation();
        const int terrainId = 1;
        const int kindId = 1;
        simulation.RegisterTerrain(
            terrainId,
            boundaryGroupId: 1,
            movementCost: 3f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 20f,
            roadSpeedMultiplier: 2f);
        simulation.World.SetMapRadius(2);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        var result = simulation.Movement.TryMoveSemantic(
            simulation,
            entityId,
            q: 1,
            r: -1,
            s: 0,
            tick: 0,
            MovementCauseCodes.Travel);
        simulation.AdvanceTicks(1);

        Assert.True(result.Moved);
        Assert.Equal(3f, result.StepCost);
        Assert.Equal((1, -1, 0), (
            simulation.World.EntitySemanticQ(entityId),
            simulation.World.EntitySemanticR(entityId),
            simulation.World.EntitySemanticS(entityId)));
        Assert.True(simulation.Movement.RoadQuality(PlaneCodes.Semantic, 1, -1, simulation.CurrentTick) > 0f);
        Assert.True(simulation.Movement.TravelCost(simulation, PlaneCodes.Semantic, kindId, 1, -1, simulation.CurrentTick) < 3f);
        Assert.Equal(0f, simulation.Movement.RoadQuality(PlaneCodes.Physical, 1, -1, simulation.CurrentTick));
    }

    [Fact]
    public void SemanticCellStateAndCoverageAreIsolatedFromPhysicalCells()
    {
        var simulation = new Simulation();
        const int terrainId = 1;
        const int semanticTerrainId = 2;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.RegisterTerrain(semanticTerrainId, boundaryGroupId: 2);
        simulation.World.SetMapRadius(1);
        var owner = simulation.CreateEntity(1, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0, superregionId: 10, continentId: 20);

        simulation.SetCellState(PlaneCodes.Semantic, 0, 0, semanticTerrainId, owner, tick: 0);

        Assert.Equal(semanticTerrainId, simulation.World.CellTerrainId(PlaneCodes.Semantic, 0, 0));
        Assert.Equal(owner, simulation.World.CoverageOwnerAt(PlaneCodes.Semantic, 0, 0));
        Assert.Equal(terrainId, simulation.World.CellTerrainId(PlaneCodes.Physical, 0, 0));
        Assert.Equal(0, simulation.World.CoverageOwnerAt(PlaneCodes.Physical, 0, 0));
        Assert.Equal(10, simulation.World.CellSuperregionId(PlaneCodes.Physical, 0, 0));
        Assert.Equal(0, simulation.World.CellSuperregionId(PlaneCodes.Semantic, 0, 0));
    }

    [Fact]
    public void SemanticCellStateRoundTripsThroughSnapshot()
    {
        var simulation = new Simulation();
        const int terrainId = 1;
        const int semanticTerrainId = 2;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.RegisterTerrain(semanticTerrainId, boundaryGroupId: 2);
        simulation.World.SetMapRadius(1);
        var owner = simulation.CreateEntity(1, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellState(PlaneCodes.Semantic, 0, 0, semanticTerrainId, owner, tick: 0);
        using var stream = new global::System.IO.MemoryStream();

        KernelSnapshot.SaveKernelState(simulation, stream);
        stream.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(stream);

        Assert.Equal(semanticTerrainId, loaded.World.CellTerrainId(PlaneCodes.Semantic, 0, 0));
        Assert.Equal(owner, loaded.World.CoverageOwnerAt(PlaneCodes.Semantic, 0, 0));
        Assert.Equal(terrainId, loaded.World.CellTerrainId(PlaneCodes.Physical, 0, 0));
        Assert.Equal(0, loaded.World.CoverageOwnerAt(PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void GeneratorSetTerrainPatchEffectTransformsBoundedLocalPatch()
    {
        var simulation = new Simulation();
        const int originalTerrainId = 1;
        const int transformedTerrainId = 2;
        const int targetKindId = 1;
        const int generatorId = 100;
        simulation.RegisterTerrain(originalTerrainId, boundaryGroupId: 1);
        simulation.RegisterTerrain(transformedTerrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedRadiusOne(simulation, originalTerrainId);
        simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var slot = simulation.Generators.RegisterGenerator(
            generatorId,
            targetKindId,
            pulseWeight: 1,
            maxRuns: 1);
        simulation.Generators.AddTrigger(slot, targetKindId);
        simulation.Generators.AddEffect(
            slot,
            GeneratorEffectTypeCodes.SetTerrainPatch,
            GeneratorProgramConstants.TargetReferenceCode,
            instanceSelectionCode: 1,
            optionCode: transformedTerrainId,
            value: 1f,
            radius: 1,
            maxCellCount: 2);

        simulation.AdvanceTicks(1);

        var transformedCount = CountTerrain(simulation, transformedTerrainId);
        Assert.Equal(2, transformedCount);
        Assert.Equal(transformedTerrainId, simulation.World.CellTerrainId(0, 0));
    }

    [Fact]
    public void GeneratorSetTerrainPatchEffectCanTransformSemanticPatchWithoutChangingPhysicalCells()
    {
        var simulation = new Simulation();
        const int originalTerrainId = 1;
        const int transformedTerrainId = 2;
        const int targetKindId = 1;
        const int generatorId = 101;
        simulation.RegisterTerrain(originalTerrainId, boundaryGroupId: 1);
        simulation.RegisterTerrain(transformedTerrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedRadiusOne(simulation, originalTerrainId);
        simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 0, 1, -1, 0, tick: 0);
        var slot = simulation.Generators.RegisterGenerator(
            generatorId,
            targetKindId,
            pulseWeight: 1,
            maxRuns: 1);
        simulation.Generators.AddTrigger(slot, targetKindId);
        simulation.Generators.AddEffect(
            slot,
            GeneratorEffectTypeCodes.SetTerrainPatch,
            GeneratorProgramConstants.TargetReferenceCode,
            instanceSelectionCode: 1,
            optionCode: transformedTerrainId,
            value: 1f,
            radius: 0,
            maxCellCount: 1,
            planeCode: PlaneCodes.Semantic);

        simulation.AdvanceTicks(1);

        Assert.Equal(transformedTerrainId, simulation.World.CellTerrainId(PlaneCodes.Semantic, 1, -1));
        Assert.Equal(originalTerrainId, simulation.World.CellTerrainId(PlaneCodes.Physical, 1, -1));
    }

    private static void SeedRadiusOne(Simulation simulation, int terrainId)
    {
        for (var direction = 0; direction < 6; direction++)
        {
            simulation.SeedCell(DirectionQ(direction), DirectionR(direction), terrainId, ownerEntityId: 0);
        }

        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
    }

    private static int CountTerrain(Simulation simulation, int terrainId)
    {
        var count = 0;
        for (var q = -1; q <= 1; q++)
        {
            for (var r = -1; r <= 1; r++)
            {
                var s = -q - r;
                if (global::System.Math.Max(global::System.Math.Abs(q), global::System.Math.Max(global::System.Math.Abs(r), global::System.Math.Abs(s))) > 1)
                {
                    continue;
                }

                if (simulation.World.CellTerrainId(q, r) == terrainId)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int DirectionQ(int direction) => direction switch
    {
        0 => 1,
        1 => 1,
        2 => 0,
        3 => -1,
        4 => -1,
        _ => 0,
    };

    private static int DirectionR(int direction) => direction switch
    {
        0 => 0,
        1 => -1,
        2 => -1,
        3 => 0,
        4 => 1,
        _ => 1,
    };
}
