namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;

public sealed partial class LowAltitudePhysicsProgramTests
{
    [Fact]
    public void LowAltitudeVerbProgram_CommitsSemanticTargetAndMovesWithVerbSource()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 13 });
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int terrainId = 1;
        const int verbId = 88;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(3);
        KernelTestSimulation.SeedSemanticCells(simulation, 3);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 0, 2, 0, -2, tick: 0);
        var verbSlot = simulation.LowAltitudeVerbs.RegisterVerb(
            actorKindId,
            verbId,
            PlaneCodes.Semantic,
            ProgramOpcodeCodes.MoveActor,
            TargetSelectorCodes.EntityKind,
            targetKindId,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 3,
            maxCommitmentTicks: 3,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: 1f,
            influenceDelta: 0f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.MoveActor);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        Assert.Equal((1, 0, -1), (
            simulation.World.EntitySemanticQ(actorId),
            simulation.World.EntitySemanticR(actorId),
            simulation.World.EntitySemanticS(actorId)));
        AssertMovedByProgram(
            simulation,
            cursor,
            actorId,
            KernelProgramCodes.LowAltitudeVerbs,
            simulation.LowAltitudeVerbSourceCodeId);
    }

    [Fact]
    public void LowAltitudeVerbProgram_MovementCanLeaveOwnedInfluenceOnTravelPath()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            Seed = 31,
            KernelTuning = CultureInfluenceActionScaleTuning(1f),
        });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 90;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
        var verbSlot = simulation.LowAltitudeVerbs.RegisterVerb(
            actorKindId,
            verbId,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.MoveActor,
            TargetSelectorCodes.EntityKind,
            targetKindId,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 3,
            maxCommitmentTicks: 3,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: 1f,
            influenceDelta: 2f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.MoveActor);

        simulation.AdvanceTicks(1);

        Assert.Equal((1, 0), (simulation.World.EntityPhysicalQ(actorId), simulation.World.EntityPhysicalR(actorId)));
        Assert.Equal(2f, simulation.World.GetCellInfluence(actorId, PlaneCodes.Physical, 1, 0));
    }

    [Fact]
    public void LowAltitudeVerbProgram_SemanticMovementUsesWorldMapRadius()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 29 });
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int terrainId = 1;
        const int verbId = 89;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(30);
        KernelTestSimulation.SeedSemanticCells(simulation, 24, 0, 3);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 24, 0, -24, tick: 0);
        simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 0, 25, 0, -25, tick: 0);
        var verbSlot = simulation.LowAltitudeVerbs.RegisterVerb(
            actorKindId,
            verbId,
            PlaneCodes.Semantic,
            ProgramOpcodeCodes.MoveActor,
            TargetSelectorCodes.EntityKind,
            targetKindId,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 8,
            maxCommitmentTicks: 8,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: 1f,
            influenceDelta: 0f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.MoveActor);

        simulation.AdvanceTicks(1);

        var q = simulation.World.EntitySemanticQ(actorId);
        var r = simulation.World.EntitySemanticR(actorId);
        Assert.Equal(25, q);
        Assert.Equal(0, r);
        Assert.True(HexDistance(0, 0, q, r) <= simulation.World.MapRadius);
        Assert.Equal(-q - r, simulation.World.EntitySemanticS(actorId));
    }

    [Fact]
    public void LowAltitudeVerbProgram_DoesNotRefreshDwellEveryTickAfterArrival()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 18 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 98;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        RegisterMovementVerb(
            simulation,
            actorKindId,
            verbId,
            PlaneCodes.Physical,
            TargetSelectorCodes.EntityKind,
            targetKindId,
            minCommitmentTicks: 40,
            maxCommitmentTicks: 40,
            dwellTicks: 4);

        simulation.AdvanceTicks(1);

        var firstIntentId = simulation.LowAltitudeVerbs.State.IntentId(actorId);

        simulation.AdvanceTicks(12);

        Assert.True(simulation.LowAltitudeVerbs.State.IntentId(actorId) > firstIntentId);
    }

    [Fact]
    public void LowAltitudeVerbProgram_WeightsEntityTargetsByConfiguredRelationshipFitness()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 23 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 96;
        const int fitnessRelationshipKindId = 9;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(2);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        _ = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
        var relatedTargetId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, -1, 0, 0, 0, 0, tick: 0);
        _ = simulation.CreateRelationship(
            fitnessRelationshipKindId,
            RelationshipPolarityCodes.Positive,
            actorId,
            relatedTargetId,
            1f,
            tick: 0);
        var verbSlot = simulation.LowAltitudeVerbs.RegisterVerb(
            actorKindId,
            verbId,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.MoveActor,
            TargetSelectorCodes.EntityKind,
            targetKindId,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 20,
            maxCommitmentTicks: 20,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 2,
            weight: 0.01f,
            fitnessWeight: 1000f,
            fitnessRelationshipKindId: fitnessRelationshipKindId,
            fitnessRelationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Outgoing,
            influenceDelta: 0f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.MoveActor);

        simulation.AdvanceTicks(1);

        Assert.Equal(relatedTargetId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
    }

    [Fact]
    public void LowAltitudeVerbProgram_WeightsForeignInfluenceTargetsByAffordance()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 24 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int favoredKindId = 2;
        const int otherKindId = 3;
        const int verbId = 95;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var favoredInfluencerId = simulation.CreateEntity(favoredKindId, subtypeId: 1, cultureId: 2, 0, 0, 0, 0, 0, tick: 0);
        var otherInfluencerId = simulation.CreateEntity(otherKindId, subtypeId: 1, cultureId: 2, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(actorId, PlaneCodes.Physical, 0, 0, 30f, tick: 0);
        simulation.SetCellInfluence(favoredInfluencerId, PlaneCodes.Physical, 1, 0, 100f, tick: 0);
        simulation.SetCellInfluence(otherInfluencerId, PlaneCodes.Physical, -1, 0, 100f, tick: 0);
        RegisterForeignInfluenceAffordance(simulation, actorKindId, favoredKindId);
        var verbSlot = simulation.LowAltitudeVerbs.RegisterVerb(
            actorKindId,
            verbId,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.SubtractOtherCellScalar,
            TargetSelectorCodes.OtherMapValue,
            targetKindId: 0,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 20,
            maxCommitmentTicks: 20,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 128,
            weight: 0.01f,
            opportunityBonusOverride: 1_000_000f,
            influenceDelta: 0f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.SubtractOtherCellScalar);

        simulation.AdvanceTicks(1);

        _ = favoredInfluencerId;
        Assert.Equal(0, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
        Assert.Equal((1, 0), (
            simulation.LowAltitudeVerbs.State.TargetQ(actorId),
            simulation.LowAltitudeVerbs.State.TargetR(actorId)));
    }

    [Fact]
    public void LowAltitudeVerbProgram_ReduceInfluenceAllowsNegativeRelationshipTargets()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 25 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 94;
        const int relationshipKindId = 9;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var hostileInfluencerId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 2, 0, 0, 0, 0, 0, tick: 0);
        var unrelatedInfluencerId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 2, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(actorId, PlaneCodes.Physical, 0, 0, 30f, tick: 0);
        simulation.SetCellInfluence(hostileInfluencerId, PlaneCodes.Physical, 1, 0, 20f, tick: 0);
        simulation.SetCellInfluence(unrelatedInfluencerId, PlaneCodes.Physical, -1, 0, 40f, tick: 0);
        _ = simulation.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Negative,
            actorId,
            hostileInfluencerId,
            0.5f,
            tick: 0);
        var hostileBefore = simulation.World.GetCellCultureInfluenceRaw(2, PlaneCodes.Physical, 1, 0);
        var unrelatedBefore = simulation.World.GetCellCultureInfluenceRaw(2, PlaneCodes.Physical, -1, 0);
        RegisterFilteredReduceVerb(simulation, actorKindId, verbId);

        simulation.AdvanceTicks(1);

        _ = unrelatedInfluencerId;
        Assert.Equal(0, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
        AssertSelectedForeignCultureCellReduced(
            simulation,
            actorId,
            cultureId: 2,
            (1, 0, hostileBefore),
            (-1, 0, unrelatedBefore));
    }

    [Fact]
    public void LowAltitudeVerbProgram_ReduceInfluenceReducesEveryOtherSourceAtTargetCell()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 28 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int foreignKindId = 2;
        const int ownKindId = 3;
        const int verbId = 91;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var foreignAId = simulation.CreateEntity(foreignKindId, subtypeId: 1, cultureId: 2, 0, 0, 0, 0, 0, tick: 0);
        var foreignBId = simulation.CreateEntity(foreignKindId, subtypeId: 1, cultureId: 3, 0, 0, 0, 0, 0, tick: 0);
        var ownSourceId = simulation.CreateEntity(ownKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(actorId, PlaneCodes.Physical, 0, 0, 30f, tick: 0);
        simulation.SetCellInfluence(foreignAId, PlaneCodes.Physical, 1, 0, 20f, tick: 0);
        simulation.SetCellInfluence(foreignBId, PlaneCodes.Physical, 1, 0, 18f, tick: 0);
        simulation.SetCellInfluence(ownSourceId, PlaneCodes.Physical, 1, 0, 16f, tick: 0);
        simulation.ModifyCellCultureLaborInfluenceRaw(2, PlaneCodes.Physical, 1, 0, 9f, tick: 0);
        simulation.ModifyCellCultureLaborInfluenceRaw(1, PlaneCodes.Physical, 1, 0, 8f, tick: 0);
        var foreignCultureTwoBefore = simulation.World.GetCellCultureInfluenceRaw(2, PlaneCodes.Physical, 1, 0);
        var foreignCultureThreeBefore = simulation.World.GetCellCultureInfluenceRaw(3, PlaneCodes.Physical, 1, 0);
        var ownCultureBefore = simulation.World.GetCellCultureInfluenceRaw(1, PlaneCodes.Physical, 1, 0);
        RegisterInfluenceVerb(
            simulation,
            actorKindId,
            verbId,
            ProgramOpcodeCodes.SubtractOtherCellScalar,
            TargetSelectorCodes.OtherMapValue,
            targetKindId: 0,
            influenceDelta: 5f,
            influenceRadius: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal((1, 0), (
            simulation.LowAltitudeVerbs.State.TargetQ(actorId),
            simulation.LowAltitudeVerbs.State.TargetR(actorId)));
        _ = foreignAId;
        _ = foreignBId;
        _ = ownSourceId;
        Assert.True(simulation.World.GetCellCultureInfluenceRaw(2, PlaneCodes.Physical, 1, 0) < foreignCultureTwoBefore);
        Assert.True(simulation.World.GetCellCultureInfluenceRaw(3, PlaneCodes.Physical, 1, 0) < foreignCultureThreeBefore);
        Assert.Equal(ownCultureBefore, simulation.World.GetCellCultureInfluenceRaw(1, PlaneCodes.Physical, 1, 0));
    }

    [Fact]
    public void LowAltitudeVerbProgram_ReduceInfluenceBlocksPositiveRelationshipTargets()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 26 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 93;
        const int relationshipKindId = 9;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var alliedInfluencerId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 2, 0, 0, 0, 0, 0, tick: 0);
        var overextendedInfluencerId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 2, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(actorId, PlaneCodes.Physical, 0, 0, 30f, tick: 0);
        simulation.SetCellInfluence(alliedInfluencerId, PlaneCodes.Physical, 1, 0, 100f, tick: 0);
        simulation.SetCellInfluence(overextendedInfluencerId, PlaneCodes.Physical, -1, 0, 60f, tick: 0);
        _ = simulation.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            actorId,
            alliedInfluencerId,
            0.5f,
            tick: 0);
        var alliedBefore = simulation.World.GetCellCultureInfluenceRaw(2, PlaneCodes.Physical, 1, 0);
        var overextendedBefore = simulation.World.GetCellCultureInfluenceRaw(2, PlaneCodes.Physical, -1, 0);
        RegisterFilteredReduceVerb(simulation, actorKindId, verbId);

        simulation.AdvanceTicks(1);

        _ = alliedInfluencerId;
        _ = overextendedInfluencerId;
        Assert.Equal(0, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
        AssertSelectedForeignCultureCellReduced(
            simulation,
            actorId,
            cultureId: 2,
            (1, 0, alliedBefore),
            (-1, 0, overextendedBefore));
    }

    [Fact]
    public void LowAltitudeVerbProgram_ReduceInfluencePositiveRelationshipWinsOverNegativeRelationship()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 27 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 92;
        const int relationshipKindId = 9;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var mixedInfluencerId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 2, 0, 0, 0, 0, 0, tick: 0);
        var overextendedInfluencerId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 2, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(actorId, PlaneCodes.Physical, 0, 0, 30f, tick: 0);
        simulation.SetCellInfluence(mixedInfluencerId, PlaneCodes.Physical, 1, 0, 100f, tick: 0);
        simulation.SetCellInfluence(overextendedInfluencerId, PlaneCodes.Physical, -1, 0, 60f, tick: 0);
        _ = simulation.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Negative,
            actorId,
            mixedInfluencerId,
            0.5f,
            tick: 0);
        _ = simulation.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            mixedInfluencerId,
            actorId,
            0.5f,
            tick: 0);
        var mixedBefore = simulation.World.GetCellCultureInfluenceRaw(2, PlaneCodes.Physical, 1, 0);
        var overextendedBefore = simulation.World.GetCellCultureInfluenceRaw(2, PlaneCodes.Physical, -1, 0);
        RegisterFilteredReduceVerb(simulation, actorKindId, verbId);

        simulation.AdvanceTicks(1);

        _ = mixedInfluencerId;
        _ = overextendedInfluencerId;
        Assert.Equal(0, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
        AssertSelectedForeignCultureCellReduced(
            simulation,
            actorId,
            cultureId: 2,
            (1, 0, mixedBefore),
            (-1, 0, overextendedBefore));
    }

    [Fact]
    public void LowAltitudeVerbProgram_AssignsDivergentMovementShapeToSameVerbIntents()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 21 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 97;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(2);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var firstActorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var secondActorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var thirdActorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
        RegisterMovementVerb(
            simulation,
            actorKindId,
            verbId,
            PlaneCodes.Physical,
            TargetSelectorCodes.EntityKind,
            targetKindId,
            minCommitmentTicks: 40,
            maxCommitmentTicks: 80,
            dwellTicks: 12,
            targetRadius: 1);

        simulation.AdvanceTicks(1);

        var firstShape = MovementShape(simulation, firstActorId);
        var secondShape = MovementShape(simulation, secondActorId);
        var thirdShape = MovementShape(simulation, thirdActorId);

        Assert.True(firstShape != secondShape || firstShape != thirdShape || secondShape != thirdShape);
    }

    [Fact]
    public void LowAltitudeVerbProgram_RoamsOneStepDuringDwellWithPullTowardTarget()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 22 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 96;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        RegisterMovementVerb(
            simulation,
            actorKindId,
            verbId,
            PlaneCodes.Physical,
            TargetSelectorCodes.EntityKind,
            targetKindId,
            minCommitmentTicks: 20,
            maxCommitmentTicks: 20,
            dwellTicks: 10);

        simulation.AdvanceTicks(1);

        var q = simulation.World.EntityPhysicalQ(actorId);
        var r = simulation.World.EntityPhysicalR(actorId);
        Assert.NotEqual((0, 0), (q, r));
        Assert.True(HexDistance(q, r, 0, 0) <= 1);
        Assert.True(simulation.LowAltitudeVerbs.State.ReachedTick(actorId) > 0);
    }

    [Fact]
    public void LowAltitudeVerbProgram_GrowsNonExclusiveCellInfluenceWithoutChangingOwner()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            Seed = 19,
            KernelTuning = CultureInfluenceActionScaleTuning(1f),
        });
        const int terrainId = 1;
        const int settlementKindId = 1;
        const int factionKindId = 2;
        const int verbId = 100;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(0);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        var settlementId = simulation.CreateEntity(settlementKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var factionId = simulation.CreateEntity(factionKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(factionId, PlaneCodes.Physical, 0, 0, 20f, tick: 0);
        simulation.LowAltitudeVerbs.RegisterInitialInfluence(settlementKindId, PlaneCodes.Physical, 35f);
        RegisterInfluenceVerb(
            simulation,
            settlementKindId,
            verbId,
            ProgramOpcodeCodes.AddOwnedCellScalar,
            TargetSelectorCodes.SelfMapValue,
            influenceDelta: 10f,
            influenceRadius: 0);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(2);

        Assert.True(simulation.World.GetCellInfluence(settlementId, PlaneCodes.Physical, 0, 0) >= 45f);
        _ = factionId;
        Assert.True(simulation.World.GetCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0) >= 65f);
        Assert.Equal(0, simulation.World.CoverageOwnerAt(0, 0));
        AssertCellInfluenceAction(simulation, cursor, settlementId, verbId);
    }

    [Fact]
    public void LowAltitudeVerbProgram_GrowsSemanticInfluenceWithoutKnownPhysicalCell()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 31 });
        const int kindId = 1;
        const int terrainId = 1;
        const int verbId = 103;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(3);
        KernelTestSimulation.SeedSemanticCells(simulation, 3);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 2, -1, -1, tick: 0);
        simulation.LowAltitudeVerbs.RegisterInitialInfluence(kindId, PlaneCodes.Semantic, 35f);
        RegisterInfluenceVerb(
            simulation,
            kindId,
            verbId,
            ProgramOpcodeCodes.AddOwnedCellScalar,
            TargetSelectorCodes.SelfMapValue,
            influenceDelta: 10f,
            influenceRadius: 0,
            planeCode: PlaneCodes.Semantic);

        simulation.AdvanceTicks(2);

        var targetQ = simulation.LowAltitudeVerbs.State.TargetQ(entityId);
        var targetR = simulation.LowAltitudeVerbs.State.TargetR(entityId);
        Assert.True(simulation.World.GetCellInfluence(entityId, PlaneCodes.Semantic, targetQ, targetR) > 0f);
        Assert.Equal(0f, simulation.World.GetCellInfluence(entityId, PlaneCodes.Physical, targetQ, targetR));
    }

    [Fact]
    public void LowAltitudeVerbProgram_RepeatsNonMoveWorkThroughCommitmentWindow()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            Seed = 32,
            KernelTuning = CultureInfluenceActionScaleTuning(1f),
        });
        const int terrainId = 1;
        const int kindId = 1;
        const int verbId = 104;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(0);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(entityId, PlaneCodes.Physical, 0, 0, 10f, tick: 0);
        var verbSlot = simulation.LowAltitudeVerbs.RegisterVerb(
            kindId,
            verbId,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.AddOwnedCellScalar,
            TargetSelectorCodes.SelfMapValue,
            targetKindId: 0,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 4,
            maxCommitmentTicks: 4,
            dwellTicks: 1,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: 1f,
            influenceDelta: 2f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.AddOwnedCellScalar);

        simulation.AdvanceTicks(1);

        var firstIntentId = simulation.LowAltitudeVerbs.State.IntentId(entityId);
        Assert.Equal(12f, simulation.World.GetCellInfluence(entityId, PlaneCodes.Physical, 0, 0));

        simulation.AdvanceTicks(2);

        Assert.Equal(firstIntentId, simulation.LowAltitudeVerbs.State.IntentId(entityId));
        Assert.Equal(16f, simulation.World.GetCellInfluence(entityId, PlaneCodes.Physical, 0, 0));

        simulation.AdvanceTicks(2);

        Assert.True(simulation.LowAltitudeVerbs.State.IntentId(entityId) > firstIntentId);
    }

    [Fact]
    public void LowAltitudeVerbProgram_AdjustsRelationshipStrengthForRelationshipTarget()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 34 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int relationshipKindId = 9;
        const int verbId = 105;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var targetId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
        var relationshipId = simulation.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            actorId,
            targetId,
            0.4f,
            tick: 0);
        var verbSlot = simulation.LowAltitudeVerbs.RegisterVerb(
            actorKindId,
            verbId,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.AdjustRelationshipStrength,
            TargetSelectorCodes.RelationshipTarget,
            targetKindId: 0,
            relationshipKindId,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Outgoing,
            minCommitmentTicks: 2,
            maxCommitmentTicks: 2,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: 1f,
            influenceDelta: 0f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f,
            relationshipStrengthDelta: 0.1f);
        simulation.LowAltitudeVerbs.AddPrimitiveStep(verbSlot, ProgramOpcodeCodes.AdjustRelationshipStrength, 0, 0f);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        Assert.Equal(0.5f, simulation.World.RelationshipStrength(relationshipId), precision: 4);
        AssertRelationshipChangedByProgram(simulation, cursor, relationshipId, verbId);
    }

    [Fact]
    public void MapActivityProgram_DecaysAbandonedHeatInsteadOfLeavingPermanentHotspots()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 33 });
        const int terrainId = 1;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var entityId = simulation.CreateEntity(kindId: 1, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        simulation.AdvanceTicks(80);

        var activeHeat = simulation.MapActivity.Heat(PlaneCodes.Physical, 0, 0, simulation.CurrentTick);
        simulation.MoveEntityPhysical(entityId, 1, 0, simulation.CurrentTick);
        simulation.AdvanceTicks(240);

        var abandonedHeat = simulation.MapActivity.Heat(PlaneCodes.Physical, 0, 0, simulation.CurrentTick);

        Assert.InRange(activeHeat, 30f, 100f);
        Assert.True(abandonedHeat < activeHeat * 0.25f);
    }

    [Fact]
    public void LowAltitudeVerbProgram_ExpandCoverageClaimsOnePhysicalCellWhenInfluenceIsEstablished()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            Seed = 23,
            KernelTuning = CultureInfluenceActionScaleTuning(1f),
        });
        const int terrainId = 1;
        const int settlementKindId = 1;
        const int verbId = 101;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(0);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        var settlementId = simulation.CreateEntity(settlementKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.LowAltitudeVerbs.RegisterInitialInfluence(settlementKindId, PlaneCodes.Physical, 35f);
        RegisterInfluenceVerb(
            simulation,
            settlementKindId,
            verbId,
            ProgramOpcodeCodes.TryClaimCell,
            TargetSelectorCodes.SelfMapValue,
            influenceDelta: 30f,
            influenceRadius: 1,
            coverageGrowthChance: 1f,
            requiredInfluence: 50f);

        simulation.AdvanceTicks(2);

        var targetQ = simulation.LowAltitudeVerbs.State.TargetQ(settlementId);
        var targetR = simulation.LowAltitudeVerbs.State.TargetR(settlementId);
        Assert.Equal(settlementId, simulation.World.CoverageOwnerAt(targetQ, targetR));
    }

    [Fact]
    public void LowAltitudeVerbProgram_MoveHeadquartersMovesFactionPhysicalAnchorToInfluencedTarget()
    {
        var simulation = KernelTestSimulation.Create(new SimulationOptions { Seed = 29 });
        const int terrainId = 1;
        const int factionKindId = 1;
        const int settlementKindId = 2;
        const int verbId = 102;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(2);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(1, 0, terrainId, ownerEntityId: 0);
        var factionId = simulation.CreateEntity(factionKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(settlementKindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(factionId, PlaneCodes.Physical, 1, 0, 80f, tick: 0);
        RegisterInfluenceVerb(
            simulation,
            factionKindId,
            verbId,
            ProgramOpcodeCodes.TryRelocateEntity,
            TargetSelectorCodes.EntityKind,
            targetKindId: settlementKindId,
            influenceDelta: 0f,
            influenceRadius: 0,
            headquartersMoveChance: 1f,
            requiredInfluence: 50f);

        simulation.AdvanceTicks(2);

        Assert.Equal((1, 0), (simulation.World.EntityPhysicalQ(factionId), simulation.World.EntityPhysicalR(factionId)));
    }

    [Fact]
    public void UpkeepProgram_OnlyLifecycleHealthChanges()
    {
        var simulation = KernelTestSimulation.Create();
        const int kindId = 1;
        const int healthAttributeId = 1;
        const int staticAttributeId = 2;
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: 0f, maximum: 100f, defaultValue: 10f);
        simulation.RegisterAttribute(kindId, staticAttributeId, minimum: 0f, maximum: 100f, defaultValue: 50f);
        simulation.Upkeep.RegisterLifecycle(kindId, healthAttributeId, baseRate: 1f, ageCoefficient: 0f, variance: 0f);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(9f, simulation.World.GetAttribute(entityId, healthAttributeId));
        Assert.Equal(50f, simulation.World.GetAttribute(entityId, staticAttributeId));
    }

    private static void SeedPhysicalNeighborhood(Simulation simulation, int terrainId)
    {
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        for (var direction = 0; direction < 6; direction++)
        {
            simulation.SeedCell(DirectionQ(direction), DirectionR(direction), terrainId, ownerEntityId: 0);
        }
    }

    private static KernelTuningOptions CultureInfluenceActionScaleTuning(float scale) =>
        new()
        {
            Programs = new KernelProgramTuningOptions
            {
                CultureInfluenceActionScale = scale,
            },
        };

    private static void RegisterInfluenceVerb(
        Simulation simulation,
        int kindId,
        int verbId,
        int operationCode,
        int targetModeCode,
        float influenceDelta,
        int influenceRadius,
        int targetKindId = 0,
        float coverageGrowthChance = 0f,
        float headquartersMoveChance = 0f,
        float requiredInfluence = 0f,
        int planeCode = PlaneCodes.Physical)
    {
        var verbSlot = simulation.LowAltitudeVerbs.RegisterVerb(
            kindId,
            verbId,
            planeCode,
            operationCode,
            targetModeCode,
            targetKindId,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 1,
            maxCommitmentTicks: 1,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 16,
            weight: 1f,
            influenceDelta: influenceDelta,
            influenceRadius: influenceRadius,
            coverageGrowthChance: coverageGrowthChance,
            headquartersMoveChance: headquartersMoveChance,
            requiredInfluence: requiredInfluence);
        AddPrimitiveSteps(simulation, verbSlot, operationCode);
    }

    private static void RegisterFilteredReduceVerb(
        Simulation simulation,
        int kindId,
        int verbId)
    {
        var verbSlot = simulation.LowAltitudeVerbs.RegisterVerb(
            kindId,
            verbId,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.SubtractOtherCellScalar,
            TargetSelectorCodes.OtherMapValue,
            targetKindId: 0,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 1,
            maxCommitmentTicks: 1,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 512,
            weight: 1f,
            influenceDelta: 5f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f,
            foreignInfluenceFilterEnabled: 1,
            foreignInfluenceBlockPositiveRelationships: 1,
            foreignInfluenceAllowNegativeRelationships: 1,
            foreignInfluenceMinimumRelationshipStrength: 0.05f,
            foreignInfluenceAllowUnrelatedInfluenceAtOrAbove: 50f);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.SubtractOtherCellScalar);
    }

    private static void RegisterMovementVerb(
        Simulation simulation,
        int kindId,
        int verbId,
        int planeCode,
        int targetModeCode,
        int targetKindId,
        int minCommitmentTicks,
        int maxCommitmentTicks,
        int dwellTicks,
        int targetRadius = 0)
    {
        var verbSlot = simulation.LowAltitudeVerbs.RegisterVerb(
            kindId,
            verbId,
            planeCode,
            ProgramOpcodeCodes.MoveActor,
            targetModeCode,
            targetKindId,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: minCommitmentTicks,
            maxCommitmentTicks: maxCommitmentTicks,
            dwellTicks: dwellTicks,
            targetRadius: targetRadius,
            candidateSampleCount: 4,
            weight: 1f,
            influenceDelta: 0f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.MoveActor);
    }

    private static void AddPrimitiveSteps(Simulation simulation, int verbSlot, int operationCode)
    {
        if (operationCode is ProgramOpcodeCodes.TryClaimCell or ProgramOpcodeCodes.TryRelocateEntity)
        {
            simulation.LowAltitudeVerbs.AddPrimitiveStep(verbSlot, ProgramOpcodeCodes.AddOwnedCellScalar, 0, 0f);
        }

        simulation.LowAltitudeVerbs.AddPrimitiveStep(verbSlot, operationCode, 0, 0f);
    }

    private static void AssertSelectedForeignCultureCellReduced(
        Simulation simulation,
        int actorId,
        int cultureId,
        params (int Q, int R, float Before)[] candidates)
    {
        var target = (simulation.LowAltitudeVerbs.State.TargetQ(actorId), simulation.LowAltitudeVerbs.State.TargetR(actorId));
        Assert.Contains(candidates, candidate => (candidate.Q, candidate.R) == target);
        var before = candidates.First(candidate => (candidate.Q, candidate.R) == target).Before;
        Assert.True(simulation.World.GetCellCultureInfluenceRaw(cultureId, PlaneCodes.Physical, target.Item1, target.Item2) < before);
    }

    private static (int Dwell, int Step, int Looseness, int Q, int R) MovementShape(Simulation simulation, int entityId) =>
        (
            simulation.LowAltitudeVerbs.State.IntentDwellTicks(entityId),
            simulation.LowAltitudeVerbs.State.StepIntervalTicks(entityId),
            simulation.LowAltitudeVerbs.State.PathLoosenessPermille(entityId),
            simulation.LowAltitudeVerbs.State.TargetQ(entityId),
            simulation.LowAltitudeVerbs.State.TargetR(entityId));

    private static void AssertMovedByProgram(
        Simulation simulation,
        KernelEventCursor cursor,
        int entityId,
        int programId,
        int sourceCodeId)
    {
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType != KernelEventType.EntityMoved)
            {
                continue;
            }

            var moved = read.Event.AsEntityMoved();
            if (moved.EntityId != entityId)
            {
                continue;
            }

            Assert.Equal(programId, read.Event.ProgramId);
            Assert.Equal(sourceCodeId, read.Event.SourceCodeId);
            return;
        }

        Assert.Fail("Expected entity movement event.");
    }

    private static void AssertCellInfluenceAction(
        Simulation simulation,
        KernelEventCursor cursor,
        int entityId,
        int actionId)
    {
        _ = actionId;
        var cultureId = simulation.World.EntityCultureId(entityId);
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType != KernelEventType.CellCultureInfluenceChanged)
            {
                continue;
            }

            var changed = read.Event.AsCellCultureInfluenceChanged();
            if (changed.CultureId != cultureId)
            {
                continue;
            }

            Assert.Equal(KernelProgramCodes.LowAltitudeVerbs, read.Event.ProgramId);
            Assert.Equal(simulation.LowAltitudeVerbSourceCodeId, read.Event.SourceCodeId);
            Assert.True(changed.CurrentRawInfluence > changed.PreviousRawInfluence);
            return;
        }

        Assert.Fail("Expected cell culture influence event attributed to the low-altitude verb program.");
    }

    private static void RegisterForeignInfluenceAffordance(Simulation simulation, int actorKindId, int favoredKindId) =>
        simulation.LowAltitudeVerbs.RegisterAffordance(
            sourceKindCode: AffordanceSourceKindCodes.Generator,
            sourceId: 1,
            shapeCode: LowAltitudeAffordanceShapeCodes.InfluenceChange,
            actorKindId: actorKindId,
            planeCode: PlaneCodes.Physical,
            candidateKindId: favoredKindId,
            relationshipKindId: 0,
            relationshipDirectionCode: 0,
            operationCode: ProgramOpcodeCodes.SubtractOtherCellScalar,
            polarityCode: AffordancePolarityCodes.Reduce,
            threshold: 0f,
            weight: 3f);

    private static void AssertRelationshipChangedByProgram(
        Simulation simulation,
        KernelEventCursor cursor,
        int relationshipId,
        int actionId)
    {
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType != KernelEventType.RelationshipChanged)
            {
                continue;
            }

            var changed = read.Event.AsRelationshipChanged();
            if (changed.RelationshipId != relationshipId)
            {
                continue;
            }

            Assert.Equal(KernelProgramCodes.LowAltitudeVerbs, read.Event.ProgramId);
            Assert.Equal(simulation.LowAltitudeVerbSourceCodeId, read.Event.SourceCodeId);
            Assert.Equal(actionId, changed.ActionId);
            Assert.True(changed.NewStrength > changed.PreviousStrength);
            return;
        }

        Assert.Fail("Expected relationship strength event attributed to the low-altitude verb program.");
    }

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var asCoord = -aq - ar;
        var bsCoord = -bq - br;
        return (Abs(aq - bq) + Abs(ar - br) + Abs(asCoord - bsCoord)) / 2;
    }

    private static int Abs(int value) => value < 0 ? -value : value;

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
