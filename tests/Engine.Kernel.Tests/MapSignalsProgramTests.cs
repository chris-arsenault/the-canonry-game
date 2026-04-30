namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class MapSignalsProgramTests
{
    [Fact]
    public void MapSignalsProgram_ComputesCrowdingPressureAndComfortFromDynamicCapacity()
    {
        var simulation = new Simulation();
        const int terrainId = 1;
        const int kindId = 1;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.MapSignals.RegisterTerrainCapacity(terrainId, 3f);
        simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var third = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(3f, simulation.MapSignals.EntityCapacity(third, PlaneCodes.Physical));
        Assert.Equal(0f, simulation.MapSignals.EntityCrowdingPressure(third, PlaneCodes.Physical));
        Assert.Equal(1f, simulation.MapSignals.EntityCrowdingComfort(third, PlaneCodes.Physical));
    }

    [Fact]
    public void MapSignalsProgram_InfluenceAndCoverageRaiseCapacityAndAttractorMass()
    {
        var simulation = new Simulation();
        const int terrainId = 1;
        const int kindId = 1;
        const int healthAttributeId = 1;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: 0f, maximum: 100f, defaultValue: 80f);
        simulation.World.SetMapRadius(1);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.MapSignals.RegisterTerrainCapacity(terrainId, 1f);
        simulation.MapSignals.RegisterKindSignal(
            kindId,
            healthAttributeId,
            signalRadius: 0,
            capacityEmission: 4f,
            supportEmission: 0f,
            threatEmission: 0f,
            baseAttractorMass: 2f,
            coverageAttractorScale: 1f,
            influenceAttractorScale: 0.5f,
            activityAttractorScale: 0f,
            healthAttractorScale: 0.1f,
            attractorRadius: 0);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.World.SetCoverageOwner(0, 0, entity);
        simulation.SetCellInfluence(entity, PlaneCodes.Physical, 0, 0, 50f, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(11f, simulation.MapSignals.EntityCapacity(entity, PlaneCodes.Physical));
        Assert.Equal(36f, simulation.MapSignals.EntityAttractorMass(entity, PlaneCodes.Physical));
    }

    [Fact]
    public void MapSignalsProgram_SemanticInfluenceRaisesSemanticCapacityAndAttractorMass()
    {
        var simulation = new Simulation();
        const int kindId = 1;
        simulation.MapSignals.RegisterKindSignal(
            kindId,
            healthAttributeId: 0,
            signalRadius: 0,
            capacityEmission: 4f,
            supportEmission: 0f,
            threatEmission: 0f,
            baseAttractorMass: 2f,
            coverageAttractorScale: 1f,
            influenceAttractorScale: 0.5f,
            activityAttractorScale: 0f,
            healthAttractorScale: 0f,
            attractorRadius: 0);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 2, -1, -1, tick: 0);
        simulation.SetCellInfluence(entity, PlaneCodes.Physical, 0, 0, 50f, tick: 0);
        simulation.SetCellInfluence(entity, PlaneCodes.Semantic, 2, -1, 50f, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(8f, simulation.MapSignals.EntityCapacity(entity, PlaneCodes.Semantic));
        Assert.Equal(27f, simulation.MapSignals.EntityAttractorMass(entity, PlaneCodes.Semantic));
    }

    [Fact]
    public void EffectsProgram_CanUseMapSupportAndCrowdingSignalsForHealth()
    {
        var simulation = new Simulation();
        const int terrainId = 1;
        const int targetKindId = 1;
        const int supportKindId = 2;
        const int healthAttributeId = 1;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.RegisterAttribute(targetKindId, healthAttributeId, minimum: 0f, maximum: 100f, defaultValue: 50f);
        simulation.World.SetMapRadius(1);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.MapSignals.RegisterTerrainCapacity(terrainId, 1f);
        simulation.MapSignals.RegisterKindSignal(
            supportKindId,
            healthAttributeId: 0,
            signalRadius: 0,
            capacityEmission: 0f,
            supportEmission: 5f,
            threatEmission: 0f,
            baseAttractorMass: 0f,
            coverageAttractorScale: 0f,
            influenceAttractorScale: 0f,
            activityAttractorScale: 0f,
            healthAttractorScale: 0f,
            attractorRadius: 0);
        simulation.Effects.RegisterAttributeDeltaEffect(
            targetKindId,
            healthAttributeId,
            EffectConditionCodes.LocalPositiveSignal,
            relationshipKindId: 0,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: 5f,
            minimumRelationshipStrength: 0f,
            deltaPerTick: 2f);
        simulation.Effects.RegisterAttributeDeltaEffect(
            targetKindId,
            healthAttributeId,
            EffectConditionCodes.LocalPressureSignal,
            relationshipKindId: 0,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: 1f,
            minimumRelationshipStrength: 0f,
            deltaPerTick: -1f);
        var target = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(supportKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(supportKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(51f, simulation.World.GetAttribute(target, healthAttributeId));
    }

    [Fact]
    public void LowAltitudeVerbProgram_UsesAttractorMassWhenChoosingEntityTargets()
    {
        var simulation = new Simulation(new SimulationOptions { Seed = 1 });
        const int terrainId = 1;
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int healthAttributeId = 1;
        const int verbId = 10;
        SeedAttractorTargetScenario(simulation, terrainId, targetKindId, healthAttributeId);
        var actor = simulation.CreateEntity(actorKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var highAttractor = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 3, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 1, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(highAttractor, PlaneCodes.Physical, 3, 0, 100f, tick: 0);
        RegisterAttractorSeekingVerb(simulation, actorKindId, targetKindId, verbId);
        simulation.SetProgramEnabled(KernelProgramCodes.LowAltitudeVerbs, enabled: false);
        simulation.AdvanceTicks(1);
        Assert.Equal(200f, simulation.MapSignals.EntityAttractorMass(highAttractor, PlaneCodes.Physical));
        simulation.SetProgramEnabled(KernelProgramCodes.LowAltitudeVerbs, enabled: true);

        simulation.AdvanceTicks(1);

        Assert.Equal(highAttractor, simulation.LowAltitudeVerbs.State.TargetEntityId(actor));
    }

    private static void SeedAttractorTargetScenario(
        Simulation simulation,
        int terrainId,
        int targetKindId,
        int healthAttributeId)
    {
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.RegisterAttribute(targetKindId, healthAttributeId, minimum: 0f, maximum: 100f, defaultValue: 100f);
        simulation.World.SetMapRadius(4);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(0, 1, terrainId, ownerEntityId: 0);
        simulation.SeedCell(1, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(2, 0, terrainId, ownerEntityId: 0);
        simulation.SeedCell(3, 0, terrainId, ownerEntityId: 0);
        simulation.MapSignals.RegisterTerrainCapacity(terrainId, 2f);
        simulation.MapSignals.RegisterKindSignal(
            targetKindId,
            healthAttributeId,
            signalRadius: 0,
            capacityEmission: 0f,
            supportEmission: 0f,
            threatEmission: 0f,
            baseAttractorMass: 0f,
            coverageAttractorScale: 0f,
            influenceAttractorScale: 2f,
            activityAttractorScale: 0f,
            healthAttractorScale: 0f,
            attractorRadius: 0);
    }

    private static void RegisterAttractorSeekingVerb(
        Simulation simulation,
        int actorKindId,
        int targetKindId,
        int verbId)
    {
        simulation.LowAltitudeVerbs.RegisterVerb(
            actorKindId,
            verbId,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.MoveActor,
            TargetSelectorCodes.EntityKind,
            targetKindId,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 10,
            maxCommitmentTicks: 10,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: 0.01f,
            exploreWeight: 0f,
            stalenessWeight: 0f,
            hotspotWeight: 100000f,
            crowdingWeight: 0f,
            generatorAffordanceWeight: 0f,
            influenceDelta: 0f,
            influenceRadius: 0,
            coverageGrowthChance: 0f,
            headquartersMoveChance: 0f,
            requiredInfluence: 0f);
    }
}
