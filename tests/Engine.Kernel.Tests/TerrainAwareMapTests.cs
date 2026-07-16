namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class TerrainAwareMapTests
{
    [Fact]
    public void MovementOverTerrainBuildsRoadQualityThatReducesEffectiveTravelCost()
    {
        var simulation = KernelTestSimulation.Create();
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
        Assert.True(simulation.Movement.PhysicalTravelCost(simulation, entityId, 1, 0, simulation.CurrentTick) < 2f);
    }

    [Fact]
    public void PlacementMovementDoesNotBuildRoadQuality()
    {
        var simulation = KernelTestSimulation.Create();
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
    public void WanderUsesEnterableNeighborsWithoutRoadAdjustedPacing()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 11 });
        const int terrainId = 1;
        const int kindId = 1;
        simulation.RegisterTerrain(
            terrainId,
            boundaryGroupId: 1,
            movementCost: 6f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 20f,
            roadSpeedMultiplier: 2f);
        simulation.World.SetMapRadius(1);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(1, 0, terrainId, ownerEntityId: 0);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        var result = simulation.Movement.Wander(
            simulation,
            entityId,
            PlaneCodes.Physical,
            tick: 0,
            anchorQ: 0,
            anchorR: 0,
            maxDistanceFromAnchor: 1);

        Assert.True(result.Moved);
        Assert.Equal(6f, result.StepCost);
        Assert.Equal((1, 0), (simulation.World.EntityPhysicalQ(entityId), simulation.World.EntityPhysicalR(entityId)));
        Assert.True(simulation.Movement.RoadQuality(PlaneCodes.Physical, 1, 0, simulation.CurrentTick) > 0f);
        Assert.True(simulation.Movement.PhysicalTravelCost(simulation, entityId, 1, 0, simulation.CurrentTick) < result.StepCost);
    }

    [Fact]
    public void MovementRoadQualityRoundTripsThroughSnapshot()
    {
        var simulation = KernelTestSimulation.Create();
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

    [Theory]
    [InlineData(30f, 25f)]
    [InlineData(80f, 75f)]
    public void RoadQualityDoesNotDecayBelowReachedCheckpoint(float roadBuildPerTravel, float expectedFloor)
    {
        var simulation = KernelTestSimulation.Create();
        const int terrainId = 1;
        const int kindId = 1;
        simulation.Movement.Data.RoadBuildPerTravel = roadBuildPerTravel;
        simulation.RegisterTerrain(
            terrainId,
            boundaryGroupId: 1,
            movementCost: 1f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f,
            roadSpeedMultiplier: 2f);
        simulation.World.SetMapRadius(1);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(1, 0, terrainId, ownerEntityId: 0);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        var result = simulation.MoveEntityPhysical(entityId, 1, 0, tick: 0, MovementCauseCodes.Travel);
        var agedQuality = simulation.Movement.RoadQuality(PlaneCodes.Physical, 1, 0, tick: 100_000);

        Assert.True(result.Moved);
        Assert.InRange(agedQuality, expectedFloor, expectedFloor + 0.001f);
    }

    [Fact]
    public void SemanticMovementUsesTerrainCostAndBuildsCellRoadQuality()
    {
        var simulation = KernelTestSimulation.Create();
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
        KernelTestSimulation.SeedSemanticCells(simulation, 2);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        var result = simulation.Movement.Navigate(
            simulation,
            entityId,
            PlaneCodes.Semantic,
            targetQ: 1,
            targetR: -1,
            tick: 0);
        simulation.AdvanceTicks(1);

        Assert.True(result.Moved);
        Assert.Equal(3f, result.StepCost);
        Assert.Equal((1, -1, 0), (
            simulation.World.EntitySemanticQ(entityId),
            simulation.World.EntitySemanticR(entityId),
            simulation.World.EntitySemanticS(entityId)));
        Assert.True(simulation.Movement.RoadQuality(PlaneCodes.Semantic, 1, -1, simulation.CurrentTick) > 0f);
        Assert.True(simulation.Movement.TravelCost(simulation, PlaneCodes.Semantic, entityId, 1, -1, simulation.CurrentTick) < 3f);
        Assert.Equal(0f, simulation.Movement.RoadQuality(PlaneCodes.Physical, 1, -1, simulation.CurrentTick));
    }

    [Fact]
    public void HierarchicalMovementPlansWhenFullGridPathScratchWouldOverflow()
    {
        var (simulation, entityId) = CreateLargePathingSimulation(roadBuildPerTravel: 1f);

        var result = simulation.Movement.Navigate(
            simulation,
            entityId,
            PlaneCodes.Physical,
            targetQ: 12,
            targetR: 0,
            tick: 0);

        Assert.True(result.Moved);
        Assert.Equal(-11, simulation.World.EntityPhysicalQ(entityId));
        Assert.Equal(0, simulation.World.EntityPhysicalR(entityId));
        Assert.True(simulation.Movement.State.RouteCacheCount >= 2);
    }

    [Fact]
    public void SameRegionalSegmentMovementUsesConcretePathToTarget()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 4,
            MapCellCapacity = 1024,
            TerrainIdCapacity = 4,
            MovementCellCapacity = 1024,
        });
        const int landTerrainId = 1;
        const int waterTerrainId = 2;
        const int kindId = 1;
        simulation.RegisterTerrain(
            landTerrainId,
            boundaryGroupId: 1,
            movementCost: 1f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f,
            roadSpeedMultiplier: 1f);
        simulation.RegisterTerrain(
            waterTerrainId,
            boundaryGroupId: 2,
            movementCost: 0f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f,
            roadSpeedMultiplier: 1f);
        simulation.World.SetMapRadius(12);
        SeedHexMap(simulation, radius: 12, waterTerrainId);
        simulation.SeedCell(-6, -10, landTerrainId, ownerEntityId: 0);
        simulation.SeedCell(-5, -11, landTerrainId, ownerEntityId: 0);
        simulation.SeedCell(-7, -9, landTerrainId, ownerEntityId: 0);
        simulation.SeedCell(-8, -8, landTerrainId, ownerEntityId: 0);
        simulation.SeedCell(-8, -7, landTerrainId, ownerEntityId: 0);
        simulation.SeedCell(-8, -6, landTerrainId, ownerEntityId: 0);
        simulation.SeedCell(-8, -5, landTerrainId, ownerEntityId: 0);
        simulation.SeedCell(-8, -4, landTerrainId, ownerEntityId: 0);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, -6, -10, 0, 0, 0, tick: 0);

        var result = simulation.Movement.Navigate(
            simulation,
            entityId,
            PlaneCodes.Physical,
            targetQ: -8,
            targetR: -4,
            tick: 0);

        Assert.True(result.Moved);
        Assert.Equal((-7, -9), (simulation.World.EntityPhysicalQ(entityId), simulation.World.EntityPhysicalR(entityId)));
    }

    [Fact]
    public void HierarchicalRouteCacheRoundTripsThroughSnapshot()
    {
        var (simulation, entityId) = CreateLargePathingSimulation(roadBuildPerTravel: 1f);
        simulation.Movement.Navigate(
            simulation,
            entityId,
            PlaneCodes.Physical,
            targetQ: 12,
            targetR: 0,
            tick: 0);
        Assert.True(simulation.Movement.State.RouteCacheCount >= 2);
        using var stream = new global::System.IO.MemoryStream();

        KernelSnapshot.SaveKernelState(simulation, stream);
        stream.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(stream);

        Assert.Equal(simulation.Movement.State.RouteCacheCount, loaded.Movement.State.RouteCacheCount);
        var result = loaded.Movement.Navigate(
            loaded,
            entityId,
            PlaneCodes.Physical,
            targetQ: 12,
            targetR: 0,
            tick: loaded.CurrentTick);
        Assert.True(result.Moved);
    }

    [Fact]
    public void TerrainTopologyChangeClearsHierarchicalRouteCache()
    {
        var (simulation, entityId) = CreateLargePathingSimulation(roadBuildPerTravel: 1f);
        simulation.Movement.Navigate(
            simulation,
            entityId,
            PlaneCodes.Physical,
            targetQ: 12,
            targetR: 0,
            tick: 0);
        var topologyEpoch = simulation.Movement.State.PathingTopologyEpoch;
        Assert.True(simulation.Movement.State.RouteCacheCount >= 2);

        simulation.SetCellTerrain(PlaneCodes.Physical, q: 0, r: 0, terrainId: 2, tick: 0);

        Assert.True(simulation.Movement.State.PathingTopologyEpoch > topologyEpoch);
        Assert.Equal(0, simulation.Movement.State.RouteCacheCount);
    }

    [Fact]
    public void RoadMarkReconciliationOnlyClearsRouteCacheWhenMarksChange()
    {
        var (simulation, entityId) = CreateLargePathingSimulation(roadBuildPerTravel: 70f);
        simulation.Movement.Navigate(
            simulation,
            entityId,
            PlaneCodes.Physical,
            targetQ: 12,
            targetR: 0,
            tick: 0);
        var roadMarkEpoch = simulation.Movement.State.RoadMarkEpoch;
        Assert.True(simulation.Movement.State.RouteCacheCount >= 2);

        simulation.AdvanceTicks(10);

        Assert.True(simulation.Movement.State.RoadMarkEpoch > roadMarkEpoch);
        Assert.Equal(0, simulation.Movement.State.RouteCacheCount);
        roadMarkEpoch = simulation.Movement.State.RoadMarkEpoch;
        simulation.Movement.Navigate(
            simulation,
            entityId,
            PlaneCodes.Physical,
            targetQ: 12,
            targetR: 0,
            tick: simulation.CurrentTick);
        Assert.True(simulation.Movement.State.RouteCacheCount >= 2);

        simulation.AdvanceTicks(10);

        Assert.Equal(roadMarkEpoch, simulation.Movement.State.RoadMarkEpoch);
        Assert.True(simulation.Movement.State.RouteCacheCount >= 2);
    }

    [Fact]
    public void MovementProfilesResolveKindDefaultSubtypeAndTraitRules()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 8,
            MapCellCapacity = 16,
            TerrainIdCapacity = 4,
            MovementProfileRuleCapacity = 8,
        });
        const int terrainId = 1;
        const int waterTerrainId = 2;
        const int kindId = 1;
        const int defaultSubtypeId = 1;
        const int shoreSubtypeId = 2;
        const int swimTraitId = 3;
        const int trailProfileId = 1;
        const int shoreProfileId = 2;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.RegisterTerrain(waterTerrainId, boundaryGroupId: 1);
        simulation.RegisterTerrainMovementProfile(waterTerrainId, trailProfileId, movementCost: 0f);
        simulation.RegisterTerrainMovementProfile(waterTerrainId, shoreProfileId, movementCost: 6f);
        simulation.Movement.RegisterDefaultMovementProfile(kindId, PlaneCodes.Physical, trailProfileId);
        simulation.Movement.RegisterSubtypeMovementProfile(kindId, shoreSubtypeId, PlaneCodes.Physical, shoreProfileId);
        simulation.Movement.RegisterTraitMovementProfile(kindId, swimTraitId, PlaneCodes.Physical, shoreProfileId);
        simulation.World.SetMapRadius(1);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(1, 0, waterTerrainId, ownerEntityId: 0);
        var trailEntityId = simulation.CreateEntity(kindId, defaultSubtypeId, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var shoreEntityId = simulation.CreateEntity(kindId, shoreSubtypeId, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var swimmerEntityId = simulation.CreateEntity(kindId, defaultSubtypeId, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.World.AddTrait(swimmerEntityId, swimTraitId, tick: 0);

        Assert.False(simulation.Movement.CanEnter(simulation, trailEntityId, 1, 0));
        Assert.True(simulation.Movement.CanEnter(simulation, shoreEntityId, 1, 0));
        Assert.True(simulation.Movement.CanEnter(simulation, swimmerEntityId, 1, 0));
        Assert.Equal(6f, simulation.Movement.PhysicalTravelCost(simulation, shoreEntityId, 1, 0, tick: 0));
    }

    [Fact]
    public void MovementAccessDoesNotRejectForeignCulturalInterior()
    {
        var simulation = KernelTestSimulation.Create();
        const int terrainId = 1;
        const int kindId = 1;
        const int actorCultureId = 1;
        const int foreignCultureId = 2;
        simulation.RegisterTerrain(
            terrainId,
            boundaryGroupId: 1,
            movementCost: 1f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f,
            roadSpeedMultiplier: 1f);
        simulation.World.SetMapRadius(2);
        SeedHexMap(simulation, radius: 2, terrainId);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, actorCultureId, 0, 0, 0, 0, 0, tick: 0);
        simulation.ModifyCellCultureInfluenceRaw(
            foreignCultureId,
            PlaneCodes.Physical,
            1,
            0,
            WorldState.CultureNexusRawMass,
            tick: 0);
        for (var direction = 0; direction < 6; direction++)
        {
            simulation.ModifyCellCultureInfluenceRaw(
                foreignCultureId,
                PlaneCodes.Physical,
                1 + DirectionQ(direction),
                DirectionR(direction),
                WorldState.CultureControlledRawMass,
                tick: 0);
        }

        var boundary = simulation.CultureBoundaries.CellAt(simulation.World, foreignCultureId, PlaneCodes.Physical, 1, 0);
        var canEnter = simulation.Movement.CanEnter(simulation, entityId, 1, 0);
        var canReach = simulation.Movement.CanReach(simulation, entityId, PlaneCodes.Physical, 1, 0, tick: 0);
        var result = simulation.MoveEntityPhysical(entityId, 1, 0, tick: 0, MovementCauseCodes.Travel);

        Assert.Equal(CultureFieldCellKindCodes.Interior, boundary.CellKindCode);
        Assert.Equal(CultureFieldStrengthBandCodes.Nexus, boundary.StrengthBandCode);
        Assert.True(canEnter);
        Assert.True(canReach);
        Assert.True(result.Moved);
    }

    [Fact]
    public void MovementReachabilityRejectsLandTargetAcrossImpassableWaterBarrier()
    {
        var simulation = CreateWaterBarrierSimulation(out var entityId, out var targetQ, out var targetR);

        Assert.False(simulation.Movement.CanReach(simulation, entityId, PlaneCodes.Physical, targetQ, targetR, tick: 0));
    }

    [Fact]
    public void TravelStructureAllowsMatchingProfileToCrossBlockedTerrain()
    {
        var simulation = CreateWaterBarrierSimulation(out var entityId, out var targetQ, out var targetR);
        const int travelStructureId = 1;
        const int movementProfileId = 1;
        simulation.Movement.RegisterTravelStructure(travelStructureId, PlaneCodes.Physical);
        simulation.Movement.RegisterTravelStructureMovementProfile(travelStructureId, movementProfileId, movementCost: 2.5f);

        var changed = simulation.Movement.SetTravelStructure(
            simulation,
            PlaneCodes.Physical,
            q: 0,
            r: 0,
            travelStructureId,
            tick: 0);
        var result = simulation.Movement.Navigate(
            simulation,
            entityId,
            PlaneCodes.Physical,
            targetQ,
            targetR,
            tick: 0);

        Assert.True(changed);
        Assert.True(simulation.Movement.CanEnter(simulation, entityId, 0, 0));
        Assert.Equal(2.5f, simulation.Movement.PhysicalTravelCost(simulation, entityId, 0, 0, tick: 0));
        Assert.True(simulation.Movement.CanReach(simulation, entityId, PlaneCodes.Physical, targetQ, targetR, tick: 0));
        Assert.True(result.Moved);
    }

    [Fact]
    public void TravelStructureStateRoundTripsThroughSnapshot()
    {
        var simulation = CreateWaterBarrierSimulation(out _, out _, out _);
        const int travelStructureId = 1;
        simulation.Movement.RegisterTravelStructure(travelStructureId, PlaneCodes.Physical);
        simulation.Movement.RegisterTravelStructureMovementProfile(travelStructureId, movementProfileId: 1, movementCost: 2.5f);
        simulation.Movement.SetTravelStructure(
            simulation,
            PlaneCodes.Physical,
            q: 0,
            r: 0,
            travelStructureId,
            tick: 0);
        using var stream = new global::System.IO.MemoryStream();

        KernelSnapshot.SaveKernelState(simulation, stream);
        stream.Position = 0;
        var loaded = KernelSnapshot.LoadKernelState(stream);

        Assert.Equal(travelStructureId, loaded.Movement.TravelStructureId(PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void SemanticCellStateAndCoverageAreIsolatedFromPhysicalCells()
    {
        var simulation = KernelTestSimulation.Create();
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
        var simulation = KernelTestSimulation.Create();
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
        var simulation = KernelTestSimulation.Create();
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
        var simulation = KernelTestSimulation.Create();
        const int originalTerrainId = 1;
        const int transformedTerrainId = 2;
        const int targetKindId = 1;
        const int generatorId = 101;
        simulation.RegisterTerrain(originalTerrainId, boundaryGroupId: 1);
        simulation.RegisterTerrain(transformedTerrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedRadiusOne(simulation, originalTerrainId);
        KernelTestSimulation.SeedSemanticCells(simulation, 1);
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

    private static (Simulation Simulation, int EntityId) CreateLargePathingSimulation(float roadBuildPerTravel)
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 4,
            MapCellCapacity = 512,
            TerrainIdCapacity = 4,
            MovementCellCapacity = 512,
        });
        const int terrainId = 1;
        const int alternateTerrainId = 2;
        const int kindId = 1;
        simulation.Movement.Data.RoadBuildPerTravel = roadBuildPerTravel;
        simulation.RegisterTerrain(
            terrainId,
            boundaryGroupId: 1,
            movementCost: 1f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f,
            roadSpeedMultiplier: 1f);
        simulation.RegisterTerrain(
            alternateTerrainId,
            boundaryGroupId: 1,
            movementCost: 1f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f,
            roadSpeedMultiplier: 1f);
        simulation.World.SetMapRadius(12);
        SeedHexMap(simulation, radius: 12, terrainId);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, -12, 0, 0, 0, 0, tick: 0);
        return (simulation, entityId);
    }

    private static Simulation CreateWaterBarrierSimulation(out int entityId, out int targetQ, out int targetR)
    {
        var simulation = KernelTestSimulation.Create();
        const int landTerrainId = 1;
        const int waterTerrainId = 2;
        const int kindId = 1;
        simulation.RegisterTerrain(
            landTerrainId,
            boundaryGroupId: 1,
            movementCost: 1f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f,
            roadSpeedMultiplier: 1f);
        simulation.RegisterTerrain(
            waterTerrainId,
            boundaryGroupId: 2,
            movementCost: 0f,
            influenceMultiplier: 1f,
            claimCostMultiplier: 1f,
            claimChanceMultiplier: 1f,
            roadBuildMultiplier: 1f,
            roadSpeedMultiplier: 1f);
        simulation.World.SetMapRadius(2);
        for (var q = -2; q <= 2; q++)
        {
            var minR = global::System.Math.Max(-2, -q - 2);
            var maxR = global::System.Math.Min(2, -q + 2);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.SeedCell(q, r, q == 0 ? waterTerrainId : landTerrainId, ownerEntityId: 0);
            }
        }

        entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, -2, 0, 0, 0, 0, tick: 0);
        targetQ = 2;
        targetR = 0;
        return simulation;
    }

    private static void SeedHexMap(Simulation simulation, int radius, int terrainId)
    {
        for (var q = -radius; q <= radius; q++)
        {
            var minR = global::System.Math.Max(-radius, -q - radius);
            var maxR = global::System.Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.SeedCell(q, r, terrainId, ownerEntityId: 0);
            }
        }
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
