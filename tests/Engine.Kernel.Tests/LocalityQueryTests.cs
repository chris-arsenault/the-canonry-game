namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed class LocalityQueryTests
{
    [Fact]
    public void ComputesCrowdingLoadAndComfortFromCurrentLocalCapacity()
    {
        var simulation = KernelTestSimulation.Create();
        const int terrainId = 1;
        const int kindId = 1;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(1);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.Locality.RegisterTerrainCapacity(terrainId, FixedScalar.FromSingle(3f));
        simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var third = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(3f), simulation.Locality.EntityCapacity(simulation, third, PlaneCodes.Physical));
        Assert.Equal(FixedScalar.FromSingle(0f), simulation.Locality.EntityCrowdingLoad(simulation, third, PlaneCodes.Physical));
        Assert.Equal(FixedScalar.FromSingle(1f), simulation.Locality.EntityCrowdingComfort(simulation, third, PlaneCodes.Physical));
    }

    [Fact]
    public void InfluenceAndCoverageRaiseCurrentCapacityAndAttractorMass()
    {
        var simulation = KernelTestSimulation.Create();
        const int terrainId = 1;
        const int kindId = 1;
        const int healthAttributeId = 1;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(80f));
        simulation.World.SetMapRadius(1);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.Locality.RegisterTerrainCapacity(terrainId, FixedScalar.FromSingle(1f));
        simulation.Locality.RegisterKind(
            kindId,
            localityRadius: 0,
            capacity: FixedScalar.FromSingle(4f),
            support: FixedScalar.FromSingle(0f),
            threat: FixedScalar.FromSingle(0f),
            baseAttractorMass: FixedScalar.FromSingle(2f),
            coverageAttractorScale: FixedScalar.FromSingle(1f),
            influenceAttractorScale: FixedScalar.FromSingle(0.5f),
            attractorRadius: 0);
        simulation.Locality.AddKindAttractorScalar(kindId, healthAttributeId, attractorScale: FixedScalar.FromSingle(0.1f));
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.World.SetCoverageOwner(0, 0, entity);
        simulation.SetCellInfluence(entity, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(50f), tick: 0);

        simulation.AdvanceTicks(1);

        Assert.True(simulation.Locality.EntityCapacity(simulation, entity, PlaneCodes.Physical) > FixedScalar.FromSingle(9f));
        Assert.True(simulation.Locality.EntityAttractorMass(simulation, entity, PlaneCodes.Physical) > FixedScalar.FromSingle(11f));
    }

    [Fact]
    public void SemanticInfluenceRaisesCurrentSemanticCapacityAndAttractorMass()
    {
        var simulation = KernelTestSimulation.Create();
        const int terrainId = 1;
        const int kindId = 1;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(3);
        KernelTestSimulation.SeedSemanticCells(simulation, 3);
        simulation.Locality.RegisterKind(
            kindId,
            localityRadius: 0,
            capacity: FixedScalar.FromSingle(4f),
            support: FixedScalar.FromSingle(0f),
            threat: FixedScalar.FromSingle(0f),
            baseAttractorMass: FixedScalar.FromSingle(2f),
            coverageAttractorScale: FixedScalar.FromSingle(1f),
            influenceAttractorScale: FixedScalar.FromSingle(0.5f),
            attractorRadius: 0);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 2, -1, -1, tick: 0);
        simulation.SetCellInfluence(entity, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(50f), tick: 0);
        simulation.SetCellInfluence(entity, PlaneCodes.Semantic, 2, -1, FixedScalar.FromSingle(50f), tick: 0);

        simulation.AdvanceTicks(1);

        Assert.True(simulation.Locality.EntityCapacity(simulation, entity, PlaneCodes.Semantic) > FixedScalar.FromSingle(6f));
        Assert.True(simulation.Locality.EntityAttractorMass(simulation, entity, PlaneCodes.Semantic) > FixedScalar.FromSingle(2f));
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
        simulation.RegisterAttribute(targetKindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(50f));
        simulation.World.SetMapRadius(1);
        simulation.SeedCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.Locality.RegisterTerrainCapacity(terrainId, FixedScalar.FromSingle(1f));
        simulation.Locality.RegisterKind(
            supportKindId,
            localityRadius: 0,
            capacity: FixedScalar.FromSingle(0f),
            support: FixedScalar.FromSingle(5f),
            threat: FixedScalar.FromSingle(0f),
            baseAttractorMass: FixedScalar.FromSingle(0f),
            coverageAttractorScale: FixedScalar.FromSingle(0f),
            influenceAttractorScale: FixedScalar.FromSingle(0f),
            attractorRadius: 0);
        simulation.Effects.RegisterAttributeDeltaEffect(
            targetKindId,
            healthAttributeId,
            EffectConditionCodes.LocalPositiveSignal,
            relationshipKindId: 0,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: FixedScalar.FromSingle(5f),
            minimumRelationshipStrength: FixedScalar.FromSingle(0f),
            deltaPerTick: FixedScalar.FromSingle(2f));
        simulation.Effects.RegisterAttributeDeltaEffect(
            targetKindId,
            healthAttributeId,
            EffectConditionCodes.LocalCrowdingLoadSignal,
            relationshipKindId: 0,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: FixedScalar.FromSingle(1f),
            minimumRelationshipStrength: FixedScalar.FromSingle(0f),
            deltaPerTick: FixedScalar.FromSingle(-1f));
        var target = simulation.CreateEntity(targetKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(supportKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.CreateEntity(supportKindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(51f), simulation.World.GetAttribute(target, healthAttributeId));
    }

}
