namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class MapSignalsProgramTests
{
    [Fact]
    public void MapSignalsProgram_ComputesCrowdingLoadAndComfortFromDynamicCapacity()
    {
        var simulation = KernelTestSimulation.Create();
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
        Assert.Equal(0f, simulation.MapSignals.EntityCrowdingLoad(third, PlaneCodes.Physical));
        Assert.Equal(1f, simulation.MapSignals.EntityCrowdingComfort(third, PlaneCodes.Physical));
    }

    [Fact]
    public void MapSignalsProgram_InfluenceAndCoverageRaiseCapacityAndAttractorMass()
    {
        var simulation = KernelTestSimulation.Create();
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
            signalRadius: 0,
            capacityEmission: 4f,
            supportEmission: 0f,
            threatEmission: 0f,
            baseAttractorMass: 2f,
            coverageAttractorScale: 1f,
            influenceAttractorScale: 0.5f,
            activityAttractorScale: 0f,
            attractorRadius: 0);
        simulation.MapSignals.AddKindAttractorScalar(kindId, healthAttributeId, attractorScale: 0.1f);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.World.SetCoverageOwner(0, 0, entity);
        simulation.SetCellInfluence(entity, PlaneCodes.Physical, 0, 0, 50f, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(
            9f + (0.4f * CultureMaturityOf(50f)),
            simulation.MapSignals.EntityCapacity(entity, PlaneCodes.Physical));
        Assert.Equal(
            11f + (0.5f * CultureMaturityOf(50f)),
            simulation.MapSignals.EntityAttractorMass(entity, PlaneCodes.Physical));
    }

    private static float CultureMaturityOf(float raw) =>
        raw * 100f / WorldState.CultureFullRawMass;

    [Fact]
    public void MapSignalsProgram_SemanticInfluenceRaisesSemanticCapacityAndAttractorMass()
    {
        var simulation = KernelTestSimulation.Create();
        const int terrainId = 1;
        const int kindId = 1;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(3);
        simulation.MapSignals.RegisterKindSignal(
            kindId,
            signalRadius: 0,
            capacityEmission: 4f,
            supportEmission: 0f,
            threatEmission: 0f,
            baseAttractorMass: 2f,
            coverageAttractorScale: 1f,
            influenceAttractorScale: 0.5f,
            activityAttractorScale: 0f,
            attractorRadius: 0);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 2, -1, -1, tick: 0);
        simulation.SetCellInfluence(entity, PlaneCodes.Physical, 0, 0, 50f, tick: 0);
        simulation.SetCellInfluence(entity, PlaneCodes.Semantic, 2, -1, 50f, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(
            6f + (0.4f * CultureMaturityOf(50f)),
            simulation.MapSignals.EntityCapacity(entity, PlaneCodes.Semantic));
        Assert.Equal(
            2f + (0.5f * CultureMaturityOf(50f)),
            simulation.MapSignals.EntityAttractorMass(entity, PlaneCodes.Semantic));
    }

    [Fact]
    public void EffectsProgram_CanUseMapSupportAndCrowdingSignalsForHealth()
    {
        var simulation = KernelTestSimulation.Create();
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
            signalRadius: 0,
            capacityEmission: 0f,
            supportEmission: 5f,
            threatEmission: 0f,
            baseAttractorMass: 0f,
            coverageAttractorScale: 0f,
            influenceAttractorScale: 0f,
            activityAttractorScale: 0f,
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
            EffectConditionCodes.LocalCrowdingLoadSignal,
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

}
