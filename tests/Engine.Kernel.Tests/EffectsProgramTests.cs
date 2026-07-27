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
        var simulation = KernelTestSimulation.Create(new SimulationOptions
        {
            RuntimeCodes = new KernelRuntimeCodes(
                RelationshipCreatedCodeId: 1,
                RelationshipCulledCodeId: 2,
                RelationshipStrengthChangedCodeId: 3,
                GambitSourceCodeId: 11,
                StartupSourceCodeId: 13,
                RelationshipSourceCodeId: 14,
                GeneratorSourceCodeId: 15,
                MapSourceCodeId: 20,
                LowAltitudeVerbSourceCodeId: 22,
                EffectsSourceCodeId: effectsSourceCodeId,
                PlayerSourceCodeId: 23,
                ChronicleSourceCodeId: 24),
        });

        simulation.RegisterAttribute(kindId, attributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(50f));
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            attributeId,
            EffectConditionCodes.RelatedEntityDistance,
            relationshipKindId,
            RelationshipMatchDirectionCodes.Outgoing,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: FixedScalar.FromSingle(0f),
            minimumRelationshipStrength: FixedScalar.FromSingle(0.1f),
            deltaPerTick: FixedScalar.FromSingle(-1f));
        var entityId = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var related = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Negative,
            entityId,
            related,
            strength: FixedScalar.FromSingle(1f),
            tick: 0);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var read = ReadNextAttributeChanged(simulation, cursor);
        var changed = read.Event.AsAttributeChanged();
        Assert.Equal(entityId, changed.EntityId);
        Assert.Equal(attributeId, changed.AttributeId);
        Assert.Equal(effectsSourceCodeId, changed.SourceCodeId);
        Assert.Equal(KernelFixedPoint.AttributeUnits(50f), changed.OldValueUnits);
        Assert.Equal(KernelFixedPoint.AttributeUnits(49f), changed.NewValueUnits);
    }

    [Fact]
    public void AttributeDeltaEffect_RelatedInfluenceAtLocationAddsHealth()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        const int relationshipKindId = 7;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(50f));
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.RelationshipMapValueAtLocation,
            relationshipKindId,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: FixedScalar.FromSingle(50f),
            minimumRelationshipStrength: FixedScalar.FromSingle(0.1f),
            deltaPerTick: FixedScalar.FromSingle(2f));
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var influencer = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            entity,
            influencer,
            strength: FixedScalar.FromSingle(1f),
            tick: 0);
        simulation.SetCellInfluence(influencer, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(50f), tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(52f), simulation.World.GetAttribute(entity, healthAttributeId));
    }

    [Fact]
    public void AttributeDeltaEffect_RelatedInfluenceAtLocationDrainsHealth()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        const int relationshipKindId = 8;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(50f));
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.RelationshipMapValueAtLocation,
            relationshipKindId,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: FixedScalar.FromSingle(50f),
            minimumRelationshipStrength: FixedScalar.FromSingle(0.1f),
            deltaPerTick: FixedScalar.FromSingle(-3f));
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var influencer = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Negative,
            entity,
            influencer,
            strength: FixedScalar.FromSingle(1f),
            tick: 0);
        simulation.SetCellInfluence(influencer, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(50f), tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(47f), simulation.World.GetAttribute(entity, healthAttributeId));
    }

    [Fact]
    public void AttributeDeltaEffect_RelatedInfluenceAtLocationUsesConfiguredSemanticPlane()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        const int relationshipKindId = 8;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(50f));
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.RelationshipMapValueAtLocation,
            relationshipKindId,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Semantic,
            maxDistance: 0,
            threshold: FixedScalar.FromSingle(50f),
            minimumRelationshipStrength: FixedScalar.FromSingle(0.1f),
            deltaPerTick: FixedScalar.FromSingle(2f));
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 2, -1, -1, tick: 0);
        var influencer = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 1, 0, -3, 1, 2, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            entity,
            influencer,
            strength: FixedScalar.FromSingle(1f),
            tick: 0);
        simulation.SetCellInfluence(influencer, PlaneCodes.Physical, 1, 0, FixedScalar.FromSingle(50f), tick: 0);
        simulation.SetCellInfluence(influencer, PlaneCodes.Semantic, 2, -1, FixedScalar.FromSingle(50f), tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(52f), simulation.World.GetAttribute(entity, healthAttributeId));
    }

    [Fact]
    public void AttributeDeltaEffect_RelatedEntityDistanceUsesRelationshipStrengthAndDistance()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        const int relationshipKindId = 9;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(50f));
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.RelatedEntityDistance,
            relationshipKindId,
            RelationshipMatchDirectionCodes.Outgoing,
            PlaneCodes.Physical,
            maxDistance: 1,
            threshold: FixedScalar.FromSingle(0f),
            minimumRelationshipStrength: FixedScalar.FromSingle(0.1f),
            deltaPerTick: FixedScalar.FromSingle(4f));
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var related = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 1, 0, 0, 0, 0, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            entity,
            related,
            strength: FixedScalar.FromSingle(0.5f),
            tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(51f), simulation.World.GetAttribute(entity, healthAttributeId));
    }

    [Fact]
    public void AttributeDeltaEffect_CanRequireTraitOnRelatedEntity()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        const int relationshipKindId = 9;
        const int traitId = 11;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(50f));
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.RelatedEntityDistance,
            relationshipKindId,
            RelationshipMatchDirectionCodes.Outgoing,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: FixedScalar.FromSingle(0f),
            minimumRelationshipStrength: FixedScalar.FromSingle(0.1f),
            deltaPerTick: FixedScalar.FromSingle(2f),
            requiredRelatedTraitId: traitId);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var related = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            entity,
            related,
            strength: FixedScalar.FromSingle(1f),
            tick: 0);

        simulation.AdvanceTicks(1);
        simulation.World.AddTrait(related, traitId, tick: 1);
        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(52f), simulation.World.GetAttribute(entity, healthAttributeId));
    }

    [Fact]
    public void AttributeDeltaEffect_CanRequireTraitOnTargetEntity()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        const int relationshipKindId = 9;
        const int traitId = 12;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(50f));
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.RelatedEntityDistance,
            relationshipKindId,
            RelationshipMatchDirectionCodes.Outgoing,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: FixedScalar.FromSingle(0f),
            minimumRelationshipStrength: FixedScalar.FromSingle(0.1f),
            deltaPerTick: FixedScalar.FromSingle(2f),
            requiredTargetTraitId: traitId);
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        var related = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.World.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            entity,
            related,
            strength: FixedScalar.FromSingle(1f),
            tick: 0);

        simulation.AdvanceTicks(1);
        simulation.World.AddTrait(entity, traitId, tick: 1);
        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(52f), simulation.World.GetAttribute(entity, healthAttributeId));
    }

    [Fact]
    public void AttributeDeltaEffect_OwnCultureInfluenceAtLocationAddsHealth()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(50f));
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.LocalCultureMapValue,
            0,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: FixedScalar.FromSingle(50f),
            minimumRelationshipStrength: FixedScalar.FromSingle(0f),
            deltaPerTick: FixedScalar.FromSingle(2f));
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 2, 0, 0, 0, 0, 0, tick: 0);
        simulation.SetCellInfluence(entity, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(40f), tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(52f), simulation.World.GetAttribute(entity, healthAttributeId));
    }

    [Fact]
    public void AttributeDeltaEffect_BeyondCultureSupportDrainsOnUnheldGroundOnly()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(50f));
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.BeyondCultureSupport,
            0,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: FixedScalar.FromSingle(10f),
            minimumRelationshipStrength: FixedScalar.FromSingle(0f),
            deltaPerTick: FixedScalar.FromSingle(-2f));
        var farWalker = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 2, 0, 0, 0, 0, 0, tick: 0);
        var homebody = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 2, 1, 0, 0, 0, 0, tick: 0);
        _ = simulation.ModifyCellCultureInfluenceRaw(2, PlaneCodes.Physical, 1, 0, FixedScalar.FromSingle(40f), tick: 0);

        simulation.AdvanceTicks(1);

        // Full drain with zero support, none at or above the threshold.
        Assert.Equal(FixedScalar.FromSingle(48f), simulation.World.GetAttribute(farWalker, healthAttributeId));
        Assert.Equal(FixedScalar.FromSingle(50f), simulation.World.GetAttribute(homebody, healthAttributeId));
    }

    [Fact]
    public void AttributeDeltaEffect_CultureSlotOccupancyAddsHealth()
    {
        const int kindId = 1;
        const int healthAttributeId = 1;
        const int cultureId = 2;
        const int slotId = 1;
        var simulation = CreateSimulation();
        simulation.RegisterAttribute(kindId, healthAttributeId, minimum: FixedScalar.FromSingle(0f), maximum: FixedScalar.FromSingle(100f), defaultValue: FixedScalar.FromSingle(50f));
        simulation.Effects.RegisterAttributeDeltaEffect(
            kindId,
            healthAttributeId,
            EffectConditionCodes.CultureSlotOccupancy,
            0,
            RelationshipMatchDirectionCodes.Either,
            PlaneCodes.Physical,
            maxDistance: 0,
            threshold: FixedScalar.FromSingle(1f),
            minimumRelationshipStrength: FixedScalar.FromSingle(0f),
            deltaPerTick: FixedScalar.FromSingle(2f),
            cultureSlotPolarityCode: RelationshipPolarityCodes.Positive);
        var slot = simulation.CultureSlots.RegisterSlot(
            cultureId,
            slotId,
            RelationshipPolarityCodes.Positive,
            minimumScore: FixedScalar.FromSingle(0.5f),
            minimumTenureTicks: 0,
            displacementMargin: FixedScalar.FromSingle(0f),
            entityInfluenceMultiplierDelta: FixedScalar.FromSingle(0f),
            cultureInfluenceMultiplierDelta: FixedScalar.FromSingle(0f));
        simulation.CultureSlots.AddAllowedKind(slot, kindId);
        simulation.CultureSlots.AddScoreRule(slot, CultureSlotScoreSignalCodes.ActiveEntity, relationshipKindId: 0, weight: FixedScalar.FromSingle(1f));
        var entity = simulation.CreateEntity(kindId, subtypeId: 1, cultureId, 0, 0, 0, 0, 0, tick: 0);

        simulation.AdvanceTicks(11);

        // The strategic slot owner publishes at the tick-10 convergence cut.
        // Local entity owners consume that published occupancy on their next
        // bounded window rather than retroactively changing tick 10.
        Assert.Equal(FixedScalar.FromSingle(50f), simulation.World.GetAttribute(entity, healthAttributeId));
        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(52f), simulation.World.GetAttribute(entity, healthAttributeId));
    }

    private static Simulation CreateSimulation() =>
        KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 8,
            RelationshipCapacity = 16,
            AttributeCapacity = 16,
            EventCapacity = 128,
            LifecycleRuleCapacity = 4,
            EffectRuleCapacity = 4,
            Seed = 1,
        });

    private static SequencedKernelEvent ReadNextAttributeChanged(
        Simulation simulation,
        KernelEventCursor cursor)
    {
        while (simulation.Events.TryRead(cursor, out var candidate))
        {
            if (candidate.Event.EventType == KernelEventType.AttributeChanged)
            {
                return candidate;
            }
        }

        throw new Xunit.Sdk.XunitException("No attribute change event was published.");
    }
}
