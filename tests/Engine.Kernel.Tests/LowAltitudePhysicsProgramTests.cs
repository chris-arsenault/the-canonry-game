namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;

public sealed class LowAltitudePhysicsProgramTests
{
    [Fact]
    public void LowAltitudeVerbProgram_CommitsPhysicalTargetAndMovesWithVerbSource()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 11 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 77;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(3);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(1, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(2, 0, terrainId, ownerEntityId: 0);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 2, 0, 0, 0, 0, tick: 0);
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
            exploreWeight: 0f,
            stalenessWeight: 0f,
            hotspotWeight: 0f,
            crowdingWeight: 0f,
            generatorAffordanceWeight: 0f,
            influenceDelta: 0f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.MoveActor);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        Assert.Equal((1, 0), (simulation.World.EntityPhysicalQ(actorId), simulation.World.EntityPhysicalR(actorId)));
        AssertMovedByProgram(
            simulation,
            cursor,
            actorId,
            KernelProgramCodes.LowAltitudeVerbs,
            simulation.LowAltitudeVerbSourceCodeId);
    }

    [Fact]
    public void LowAltitudeVerbProgram_CommitsSemanticTargetAndMovesWithVerbSource()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 13 });
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 88;
        simulation.World.SetMapRadius(3);
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
            exploreWeight: 0f,
            stalenessWeight: 0f,
            hotspotWeight: 0f,
            crowdingWeight: 0f,
            generatorAffordanceWeight: 0f,
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
    public void LowAltitudeVerbProgram_SemanticMovementDoesNotLeaveVisualMapRadius()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 29 });
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 89;
        simulation.World.SetMapRadius(1);
        simulation.LowAltitudeVerbs.ConfigureSemanticBounds(-4, 4, -4, 4);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 0, 1, 1, -2, tick: 0);
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
            exploreWeight: 0f,
            stalenessWeight: 0f,
            hotspotWeight: 0f,
            crowdingWeight: 0f,
            generatorAffordanceWeight: 0f,
            influenceDelta: 0f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.MoveActor);

        simulation.AdvanceTicks(8);

        var q = simulation.World.EntitySemanticQ(actorId);
        var r = simulation.World.EntitySemanticR(actorId);
        Assert.True(HexDistance(0, 0, q, r) <= simulation.World.MapRadius);
        Assert.Equal(-q - r, simulation.World.EntitySemanticS(actorId));
    }

    [Fact]
    public void LowAltitudeVerbProgram_EmitsMovementIntentTraceWithVerbIdWhenTraceEnabled()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 17, TraceEnabled = true });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 99;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var targetId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
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
            exploreWeight: 0f,
            stalenessWeight: 0f,
            hotspotWeight: 0f,
            crowdingWeight: 0f,
            generatorAffordanceWeight: 0f,
            influenceDelta: 0f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.MoveActor);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var trace = AssertTrace(simulation, cursor, actorId);
        Assert.Equal(TraceSubjectCodes.MovementIntent, trace.TraceSubjectCode);
        Assert.Equal(ActionFamilyCodes.LowAltitudeVerb, trace.ActionFamilyCode);
        Assert.Equal(verbId, trace.ActionId);
        Assert.Equal(targetId, trace.TargetEntityId);
        Assert.Equal(TraceStatusCodes.Selected, trace.StatusCode);
    }

    [Fact]
    public void LowAltitudeVerbProgram_DoesNotRefreshDwellEveryTickAfterArrival()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 18 });
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
    public void LowAltitudeVerbProgram_WeightsEntityTargetsByConfiguredRelationshipAffinity()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 23 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 96;
        const int affinityRelationshipKindId = 9;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(2);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        _ = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
        var relatedTargetId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, -1, 0, 0, 0, 0, tick: 0);
        _ = simulation.CreateRelationship(
            affinityRelationshipKindId,
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
            exploreWeight: 0f,
            stalenessWeight: 0f,
            hotspotWeight: 0f,
            crowdingWeight: 0f,
            generatorAffordanceWeight: 0f,
            affinityRelationshipKindId: affinityRelationshipKindId,
            affinityRelationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Outgoing,
            relationshipAffinityWeight: 1000f,
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
    public void LowAltitudeVerbProgram_WeightsForeignInfluenceTargetsByGeneratorAffordance()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 24 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int favoredKindId = 2;
        const int otherKindId = 3;
        const int verbId = 95;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var favoredInfluencerId = simulation.CreateEntity(favoredKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var otherInfluencerId = simulation.CreateEntity(otherKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(favoredInfluencerId, PlaneCodes.Physical, 1, 0, 100f, tick: 0);
        simulation.SetCellInfluence(otherInfluencerId, PlaneCodes.Physical, -1, 0, 100f, tick: 0);
        simulation.LowAltitudeVerbs.RegisterGeneratorAffordance(actorKindId, favoredKindId, favoredKindId, 3f);
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
            exploreWeight: 0f,
            stalenessWeight: 0f,
            hotspotWeight: 0f,
            crowdingWeight: 0f,
            generatorAffordanceWeight: 1_000_000f,
            influenceDelta: 0f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f);
        AddPrimitiveSteps(simulation, verbSlot, ProgramOpcodeCodes.SubtractOtherCellScalar);

        simulation.AdvanceTicks(1);

        Assert.Equal(favoredInfluencerId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
        Assert.Equal((1, 0), (
            simulation.LowAltitudeVerbs.State.TargetQ(actorId),
            simulation.LowAltitudeVerbs.State.TargetR(actorId)));
    }

    [Fact]
    public void LowAltitudeVerbProgram_ReduceInfluenceAllowsNegativeRelationshipTargets()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 25 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 94;
        const int relationshipKindId = 9;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var hostileInfluencerId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var unrelatedInfluencerId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(hostileInfluencerId, PlaneCodes.Physical, 1, 0, 20f, tick: 0);
        simulation.SetCellInfluence(unrelatedInfluencerId, PlaneCodes.Physical, -1, 0, 40f, tick: 0);
        _ = simulation.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Negative,
            actorId,
            hostileInfluencerId,
            0.5f,
            tick: 0);
        RegisterFilteredReduceVerb(simulation, actorKindId, verbId);

        simulation.AdvanceTicks(1);

        Assert.Equal(hostileInfluencerId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
        Assert.True(simulation.World.GetCellInfluence(hostileInfluencerId, PlaneCodes.Physical, 1, 0) < 20f);
        Assert.Equal(40f, simulation.World.GetCellInfluence(unrelatedInfluencerId, PlaneCodes.Physical, -1, 0));
    }

    [Fact]
    public void LowAltitudeVerbProgram_ReduceInfluenceBlocksPositiveRelationshipTargets()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 26 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int verbId = 93;
        const int relationshipKindId = 9;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
        var actorId = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var alliedInfluencerId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var overextendedInfluencerId = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(alliedInfluencerId, PlaneCodes.Physical, 1, 0, 100f, tick: 0);
        simulation.SetCellInfluence(overextendedInfluencerId, PlaneCodes.Physical, -1, 0, 60f, tick: 0);
        _ = simulation.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            actorId,
            alliedInfluencerId,
            0.5f,
            tick: 0);
        RegisterFilteredReduceVerb(simulation, actorKindId, verbId);

        simulation.AdvanceTicks(1);

        Assert.Equal(overextendedInfluencerId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
        Assert.Equal(100f, simulation.World.GetCellInfluence(alliedInfluencerId, PlaneCodes.Physical, 1, 0));
        Assert.True(simulation.World.GetCellInfluence(overextendedInfluencerId, PlaneCodes.Physical, -1, 0) < 60f);
    }

    [Fact]
    public void LowAltitudeVerbProgram_AssignsDivergentMovementShapeToSameVerbIntents()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 21 });
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
        var simulation = new Simulation(new SimulationOptions { Seed = 22 });
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
        var simulation = new Simulation(new SimulationOptions { Seed = 19 });
        const int terrainId = 1;
        const int settlementKindId = 1;
        const int factionKindId = 2;
        const int verbId = 100;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
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
        Assert.Equal(20f, simulation.World.GetCellInfluence(factionId, PlaneCodes.Physical, 0, 0));
        Assert.Equal(0, simulation.World.CoverageOwnerAt(0, 0));
        AssertCellInfluenceAction(simulation, cursor, settlementId, verbId);
    }

    [Fact]
    public void LowAltitudeVerbProgram_GrowsSemanticInfluenceWithoutKnownPhysicalCell()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 31 });
        const int kindId = 1;
        const int verbId = 103;
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

        Assert.True(simulation.World.GetCellInfluence(entityId, PlaneCodes.Semantic, 2, -1) >= 45f);
        Assert.Equal(0f, simulation.World.GetCellInfluence(entityId, PlaneCodes.Physical, 2, -1));
    }

    [Fact]
    public void LowAltitudeVerbProgram_RepeatsNonMoveWorkThroughCommitmentWindow()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 32 });
        const int terrainId = 1;
        const int kindId = 1;
        const int verbId = 104;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
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
            exploreWeight: 0f,
            stalenessWeight: 0f,
            hotspotWeight: 0f,
            crowdingWeight: 0f,
            generatorAffordanceWeight: 0f,
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
    public void MapActivityProgram_DecaysAbandonedHeatInsteadOfLeavingPermanentHotspots()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 33 });
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
        var simulation = new Simulation(new SimulationOptions { Seed = 23 });
        const int terrainId = 1;
        const int settlementKindId = 1;
        const int verbId = 101;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        SeedPhysicalNeighborhood(simulation, terrainId);
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

        Assert.Equal(settlementId, simulation.World.CoverageOwnerAt(0, 0));
    }

    [Fact]
    public void LowAltitudeVerbProgram_MoveHeadquartersMovesFactionPhysicalAnchorToInfluencedTarget()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 29 });
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
        var simulation = new Simulation();
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
            exploreWeight: 0f,
            stalenessWeight: 0f,
            hotspotWeight: 0f,
            crowdingWeight: 0f,
            generatorAffordanceWeight: 0f,
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
            exploreWeight: 0f,
            stalenessWeight: 0f,
            hotspotWeight: 0f,
            crowdingWeight: 0f,
            generatorAffordanceWeight: 0f,
            affinityRelationshipKindId: 0,
            affinityRelationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            relationshipAffinityWeight: 0f,
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
            exploreWeight: 0f,
            stalenessWeight: 0f,
            hotspotWeight: 0f,
            crowdingWeight: 0f,
            generatorAffordanceWeight: 0f,
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
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType != KernelEventType.CellInfluenceChanged)
            {
                continue;
            }

            var changed = read.Event.AsCellInfluenceChanged();
            if (changed.EntityId != entityId || changed.ActionId != actionId)
            {
                continue;
            }

            Assert.Equal(KernelProgramCodes.LowAltitudeVerbs, read.Event.ProgramId);
            Assert.Equal(simulation.LowAltitudeVerbSourceCodeId, read.Event.SourceCodeId);
            Assert.True(changed.CurrentInfluence > changed.PreviousInfluence);
            return;
        }

        Assert.Fail("Expected cell influence event attributed to the low-altitude verb action.");
    }

    private static TraceKernelEvent AssertTrace(
        Simulation simulation,
        KernelEventCursor cursor,
        int entityId)
    {
        var found = false;
        var selected = default(TraceKernelEvent);
        while (simulation.Events.TryRead(cursor, out var read))
        {
            if (read.Event.EventType != KernelEventType.Trace)
            {
                continue;
            }

            var trace = read.Event.AsTrace();
            if (trace.EntityId == entityId)
            {
                found = true;
                if (trace.StatusCode == TraceStatusCodes.Selected)
                {
                    selected = trace;
                }
            }
        }

        if (found)
        {
            return selected;
        }

        Assert.Fail("Expected movement intent trace event.");
        return selected;
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
