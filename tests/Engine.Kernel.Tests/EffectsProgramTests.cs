namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;

public sealed class EffectsProgramTests
{
    [Fact]
    public void AttributeDeltaEffectEmitsAttributeChangedWithEffectsSourceCode()
    {
        const int kindId = 1;
        const int attributeId = 1;
        const int relationshipKindId = 7;
        const int effectsSourceCodeId = 42;
        var simulation = new Simulation(new SimulationOptions
        {
            RuntimeCodes = new KernelRuntimeCodes(
                RelationshipCreatedCodeId: 1,
                RelationshipCulledCodeId: 2,
                RelationshipStrengthChangedCodeId: 3,
                GambitSourceCodeId: 11,
                UpkeepSourceCodeId: 12,
                StartupSourceCodeId: 13,
                RelationshipSourceCodeId: 14,
                GeneratorSourceCodeId: 15,
                DegeneracySourceCodeId: 17,
                PlanningSourceCodeId: 18,
                MapActivitySourceCodeId: 20,
                MapSignalsSourceCodeId: 21,
                LowAltitudeVerbSourceCodeId: 22,
                EffectsSourceCodeId: effectsSourceCodeId),
        });

        simulation.RegisterAttribute(kindId, attributeId, minimum: 0f, maximum: 100f, defaultValue: 50f);
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            attributeId,
            EffectConditionCodes.RelatedEntityDistance,
            relationshipKindId,
            RelationshipMatchDirectionCodes.Outgoing,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: 0f,
            minimumRelationshipStrength: 0.1f,
            deltaPerTick: -1f);
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var related = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Negative,
            entityId,
            related,
            strength: 1f,
            tick: 0);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        Assert.True(simulation.Events.TryRead(cursor, out var read));
        var changed = read.Event.AsAttributeChanged();
        Assert.Equal(entityId, changed.EntityId);
        Assert.Equal(attributeId, changed.AttributeId);
        Assert.Equal(effectsSourceCodeId, changed.SourceCodeId);
        Assert.Equal(50f, changed.OldValue);
        Assert.Equal(49f, changed.NewValue);
    }

    [Fact]
    public void AttributeDeltaEffect_RelatedInfluenceAtLocationAddsHealth()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        const int relationshipKindId = 7;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: 0f, maximum: 100f, defaultValue: 50f);
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.RelationshipMapValueAtLocation,
            relationshipKindId,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: 50f,
            minimumRelationshipStrength: 0.1f,
            deltaPerTick: 2f);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var influencer = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            entity,
            influencer,
            strength: 1f,
            tick: 0);
        simulation.SetCellInfluence(influencer, PlaneCodes.Physical, 0, 0, 50f, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(52f, simulation.World.GetAttribute(entity, healthAttributeId));
    }

    [Fact]
    public void AttributeDeltaEffect_RelatedInfluenceAtLocationDrainsHealth()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        const int relationshipKindId = 8;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: 0f, maximum: 100f, defaultValue: 50f);
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.RelationshipMapValueAtLocation,
            relationshipKindId,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: 50f,
            minimumRelationshipStrength: 0.1f,
            deltaPerTick: -3f);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var influencer = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Negative,
            entity,
            influencer,
            strength: 1f,
            tick: 0);
        simulation.SetCellInfluence(influencer, PlaneCodes.Physical, 0, 0, 50f, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(47f, simulation.World.GetAttribute(entity, healthAttributeId));
    }

    [Fact]
    public void AttributeDeltaEffect_RelatedInfluenceAtLocationUsesConfiguredSemanticPlane()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        const int relationshipKindId = 8;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: 0f, maximum: 100f, defaultValue: 50f);
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.RelationshipMapValueAtLocation,
            relationshipKindId,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Semantic,
            maxDistance: 0,
            threshold: 50f,
            minimumRelationshipStrength: 0.1f,
            deltaPerTick: 2f);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 2, -1, -1, tick: 0);
        var influencer = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 1, 0, -3, 1, 2, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            entity,
            influencer,
            strength: 1f,
            tick: 0);
        simulation.SetCellInfluence(influencer, PlaneCodes.Physical, 1, 0, 50f, tick: 0);
        simulation.SetCellInfluence(influencer, PlaneCodes.Semantic, 2, -1, 50f, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(52f, simulation.World.GetAttribute(entity, healthAttributeId));
    }

    [Fact]
    public void AttributeDeltaEffect_RelatedEntityDistanceUsesRelationshipStrengthAndDistance()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        const int relationshipKindId = 9;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: 0f, maximum: 100f, defaultValue: 50f);
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.RelatedEntityDistance,
            relationshipKindId,
            RelationshipMatchDirectionCodes.Outgoing,
            PlaneCodes.Physical,
            maxDistance: 1,
            threshold: 0f,
            minimumRelationshipStrength: 0.1f,
            deltaPerTick: 4f);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var related = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            entity,
            related,
            strength: 0.5f,
            tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(51f, simulation.World.GetAttribute(entity, healthAttributeId));
    }

    [Fact]
    public void AttributeDeltaEffect_CanRequireTraitOnRelatedEntity()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        const int relationshipKindId = 9;
        const int traitId = 11;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: 0f, maximum: 100f, defaultValue: 50f);
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.RelatedEntityDistance,
            relationshipKindId,
            RelationshipMatchDirectionCodes.Outgoing,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: 0f,
            minimumRelationshipStrength: 0.1f,
            deltaPerTick: 2f,
            requiredRelatedTraitId: traitId);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var related = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            entity,
            related,
            strength: 1f,
            tick: 0);

        simulation.AdvanceTicks(1);
        simulation.World.AddTrait(related, traitId, tick: 1);
        simulation.AdvanceTicks(1);

        Assert.Equal(52f, simulation.World.GetAttribute(entity, healthAttributeId));
    }

    private static Simulation CreateSimulation() =>
        new(new SimulationOptions
        {
            EntityCapacity = 8,
            RelationshipCapacity = 16,
            AttributeCapacity = 16,
            ResourceCapacity = 16,
            EventCapacity = 128,
            LifecycleRuleCapacity = 4,
            EffectRuleCapacity = 4,
            Seed = 1,
        });
}
