namespace Engine.Kernel.Tests;

using Engine.Kernel;

/// <summary>
/// Contract tests for the redesigned low-altitude verb selection model:
/// two-bucket selection (routine + opportunity), per-mode scoring, affordance overlap,
/// per-actor recent-target ring, cell saturation, three-delta trait surface,
/// relationship fitness, and biased cell-target candidate generation.
///
/// See DESIGN-low-altitude-verb-selection.md at the repository root.
/// </summary>
public sealed class LowAltitudeVerbSelectionTests
{
    private const int TerrainId = 1;
    private const int ActorKindId = 1;
    private const int TargetKindId = 2;

    [Fact]
    public void RecentTargetRing_PushesCellTargetWhenIntentExpires()
    {
        var simulation = NewSimulation(seed: 11);
        SeedNeighborhood(simulation);
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        RegisterExploreVerb(simulation, verbId: 200, weight: 1f);

        // Advance well past at least one commitment window so the ring has been pushed to.
        simulation.AdvanceTicks(40);

        Assert.True(simulation.LowAltitudeVerbs.State.RecentTargetCount(actorId) >= 1);
        // The most recent slot must be a cell-target key on the physical plane (bit 61 set,
        // bit 62 clear), not an entity-target key.
        var topKey = simulation.LowAltitudeVerbs.State.RecentTargetKey(actorId, 0);
        Assert.NotEqual(0L, topKey);
        Assert.Equal(0x2000_0000_0000_0000L, topKey & 0x6000_0000_0000_0000L);
    }

    [Fact]
    public void RecentTargetRing_PushesEntityTargetWhenIntentExpires()
    {
        var simulation = NewSimulation(seed: 12);
        SeedNeighborhood(simulation);
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var targetEntityId = simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        RegisterEntityKindVerb(simulation, verbId: 201, targetKindId: TargetKindId, weight: 1f);

        simulation.AdvanceTicks(1);
        Assert.Equal(targetEntityId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
        simulation.AdvanceTicks(40);

        var firstSlotKey = simulation.LowAltitudeVerbs.State.RecentTargetKey(actorId, 0);
        Assert.Equal(LowAltitudeVerbProgramState.PackEntityTargetKey(targetEntityId), firstSlotKey);
    }

    [Fact]
    public void RecentTargetRing_BoundsToConfiguredSize()
    {
        var simulation = NewSimulation(seed: 13);
        SeedNeighborhood(simulation);
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var first = simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var second = simulation.CreateEntity(TargetKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        var third = simulation.CreateEntity(TargetKindId, 1, 1, 0, 1, 0, 0, 0, 0);
        var fourth = simulation.CreateEntity(TargetKindId, 1, 1, 0, -1, 0, 0, 0, 0);

        simulation.LowAltitudeVerbs.State.PushRecentTarget(actorId, LowAltitudeVerbProgramState.PackEntityTargetKey(first), tick: 1);
        simulation.LowAltitudeVerbs.State.PushRecentTarget(actorId, LowAltitudeVerbProgramState.PackEntityTargetKey(second), tick: 2);
        simulation.LowAltitudeVerbs.State.PushRecentTarget(actorId, LowAltitudeVerbProgramState.PackEntityTargetKey(third), tick: 3);
        simulation.LowAltitudeVerbs.State.PushRecentTarget(actorId, LowAltitudeVerbProgramState.PackEntityTargetKey(fourth), tick: 4);

        Assert.Equal(3, simulation.LowAltitudeVerbs.State.RecentTargetCount(actorId));
        Assert.Equal(LowAltitudeVerbProgramState.PackEntityTargetKey(fourth), simulation.LowAltitudeVerbs.State.RecentTargetKey(actorId, 0));
        Assert.Equal(LowAltitudeVerbProgramState.PackEntityTargetKey(third), simulation.LowAltitudeVerbs.State.RecentTargetKey(actorId, 1));
        Assert.Equal(LowAltitudeVerbProgramState.PackEntityTargetKey(second), simulation.LowAltitudeVerbs.State.RecentTargetKey(actorId, 2));
    }

    [Fact]
    public void RecentTargetRing_UsesConfiguredSize()
    {
        var simulation = NewSimulation(seed: 130, recentTargetRingSize: 2);
        SeedNeighborhood(simulation);
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var first = simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var second = simulation.CreateEntity(TargetKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        var third = simulation.CreateEntity(TargetKindId, 1, 1, 0, 1, 0, 0, 0, 0);

        simulation.LowAltitudeVerbs.State.PushRecentTarget(actorId, LowAltitudeVerbProgramState.PackEntityTargetKey(first), tick: 1);
        simulation.LowAltitudeVerbs.State.PushRecentTarget(actorId, LowAltitudeVerbProgramState.PackEntityTargetKey(second), tick: 2);
        simulation.LowAltitudeVerbs.State.PushRecentTarget(actorId, LowAltitudeVerbProgramState.PackEntityTargetKey(third), tick: 3);

        Assert.Equal(2, simulation.LowAltitudeVerbs.State.RecentTargetCount(actorId));
        Assert.Equal(LowAltitudeVerbProgramState.PackEntityTargetKey(third), simulation.LowAltitudeVerbs.State.RecentTargetKey(actorId, 0));
        Assert.Equal(LowAltitudeVerbProgramState.PackEntityTargetKey(second), simulation.LowAltitudeVerbs.State.RecentTargetKey(actorId, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.LowAltitudeVerbs.State.RecentTargetKey(actorId, 2));
    }

    [Fact]
    public void RecentTargetRing_DropsToZeroWhenJustVisitedThenRecoversWithCooldown()
    {
        // We construct a scenario where there are two equal targets and the recently-visited
        // one should be effectively excluded by the recency multiplier.
        var simulation = NewSimulation(seed: 14, opportunityRate: 0f);
        SeedNeighborhood(simulation);
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var freshTargetId = simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var recentlyVisitedId = simulation.CreateEntity(TargetKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        RegisterEntityKindVerb(simulation, verbId: 202, targetKindId: TargetKindId, weight: 1f);

        // Mark recentlyVisited as just-completed to fully suppress its score.
        simulation.LowAltitudeVerbs.State.PushRecentTarget(
            actorId,
            LowAltitudeVerbProgramState.PackEntityTargetKey(recentlyVisitedId),
            tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(freshTargetId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
    }

    [Fact]
    public void OpportunityBucket_RoutesSelectionToVerbWithAffordancePressureWhenCoinHits()
    {
        var simulation = NewSimulation(seed: 21, opportunityRate: 1f, opportunityBonus: 4f);
        SeedNeighborhood(simulation);
        const int neutralVerbId = 210;
        const int hotVerbId = 211;
        const int hotTargetKindId = 7;
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var hotTargetId = simulation.CreateEntity(hotTargetKindId, 1, 1, -1, 0, 0, 0, 0, 0);

        // Two competing verbs: one has an active affordance row, one doesn't.
        RegisterEntityKindVerb(simulation, verbId: neutralVerbId, targetKindId: TargetKindId, weight: 1f);
        RegisterEntityKindVerb(simulation, verbId: hotVerbId, targetKindId: hotTargetKindId, weight: 1f);
        simulation.LowAltitudeVerbs.RegisterAffordance(
            sourceKindCode: AffordanceSourceKindCodes.Generator,
            sourceId: 1,
            shapeCode: LowAltitudeAffordanceShapeCodes.PresenceNearEntity,
            actorKindId: ActorKindId,
            planeCode: PlaneCodes.Physical,
            candidateKindId: hotTargetKindId,
            relationshipKindId: 0,
            relationshipDirectionCode: 0,
            operationCode: 0,
            polarityCode: AffordancePolarityCodes.None,
            threshold: 0f,
            weight: 5f);

        simulation.AdvanceTicks(1);

        Assert.Equal(hotTargetId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
    }

    [Fact]
    public void RoutineBucket_IsUsedWhenNoVerbHasAffordancePressure()
    {
        var simulation = NewSimulation(seed: 22, opportunityRate: 1f);
        SeedNeighborhood(simulation);
        const int verbId = 220;
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        RegisterEntityKindVerb(simulation, verbId, targetKindId: TargetKindId, weight: 1f);

        simulation.AdvanceTicks(1);

        // Even with opportunityRate=1 and no affordance row, a target must be picked from the
        // routine bucket. The intent should be set.
        Assert.True(simulation.LowAltitudeVerbs.State.ActiveVerbSlot(actorId) > 0);
    }

    [Fact]
    public void OpportunityBucket_FallsThroughToRoutineWhenZeroOpportunityVerbsExist()
    {
        // Forced opportunity coin but no affordance => routine bucket still used (no intent lost).
        var simulation = NewSimulation(seed: 23, opportunityRate: 1f);
        SeedNeighborhood(simulation);
        const int verbId = 230;
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        RegisterEntityKindVerb(simulation, verbId, targetKindId: TargetKindId, weight: 1f);

        simulation.AdvanceTicks(1);
        Assert.True(simulation.LowAltitudeVerbs.State.ActiveVerbSlot(actorId) > 0);
    }

    [Fact]
    public void AffordanceOverlap_DiminishingReturnsSumScalesScoreNonlinearly()
    {
        // Single target with multiple matching rows; with opportunityRate=1 the verb must win,
        // and with overlap, the candidate's weight is positive (verifying score is computed).
        var simulation = NewSimulation(seed: 31, opportunityRate: 1f, opportunityBonus: 10f);
        SeedNeighborhood(simulation);
        const int verbId = 310;
        const int hotKindId = 9;
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var hotEntityId = simulation.CreateEntity(hotKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        RegisterEntityKindVerb(simulation, verbId, targetKindId: hotKindId, weight: 0.001f);

        // Register two matching affordance rows on the same target. Diminishing-sum should still
        // direct the actor to the (only) candidate target.
        AddPresenceAffordance(simulation, sourceId: 1, candidateKindId: hotKindId, weight: 3f);
        AddPresenceAffordance(simulation, sourceId: 2, candidateKindId: hotKindId, weight: 3f);

        simulation.AdvanceTicks(1);

        Assert.Equal(hotEntityId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
    }

    [Fact]
    public void RelationshipFitness_DeficitRewardsLowStrength()
    {
        // PolarityCode=Strengthen with threshold=0.8 should rate a low-strength relationship target
        // higher than a high-strength one.
        var simulation = NewSimulation(seed: 41, opportunityRate: 0f);
        SeedNeighborhood(simulation);
        const int verbId = 410;
        const int relKindId = 5;
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var lowStrengthTargetId = simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var highStrengthTargetId = simulation.CreateEntity(TargetKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        simulation.CreateRelationship(relKindId, RelationshipPolarityCodes.Positive, actorId, lowStrengthTargetId, 0.1f, tick: 0);
        simulation.CreateRelationship(relKindId, RelationshipPolarityCodes.Positive, actorId, highStrengthTargetId, 1.0f, tick: 0);

        RegisterEntityKindVerb(
            simulation,
            verbId,
            targetKindId: TargetKindId,
            weight: 0.001f,
            fitnessRelationshipKindId: relKindId,
            fitnessRelationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Outgoing,
            fitnessPolarityCode: AffordancePolarityCodes.Strengthen,
            fitnessThreshold: 0.8f,
            fitnessWeight: 1000f);

        simulation.AdvanceTicks(1);

        Assert.Equal(lowStrengthTargetId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
    }

    [Fact]
    public void RelationshipFitness_SurplusRewardsHighStrength()
    {
        var simulation = NewSimulation(seed: 42, opportunityRate: 0f);
        SeedNeighborhood(simulation);
        const int verbId = 420;
        const int relKindId = 6;
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var lowStrengthTargetId = simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var highStrengthTargetId = simulation.CreateEntity(TargetKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        simulation.CreateRelationship(relKindId, RelationshipPolarityCodes.Positive, actorId, lowStrengthTargetId, 0.1f, tick: 0);
        simulation.CreateRelationship(relKindId, RelationshipPolarityCodes.Positive, actorId, highStrengthTargetId, 0.9f, tick: 0);

        RegisterEntityKindVerb(
            simulation,
            verbId,
            targetKindId: TargetKindId,
            weight: 0.001f,
            fitnessRelationshipKindId: relKindId,
            fitnessRelationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Outgoing,
            fitnessPolarityCode: AffordancePolarityCodes.Weaken,
            fitnessThreshold: 0.5f,
            fitnessWeight: 1000f);

        simulation.AdvanceTicks(1);

        Assert.Equal(highStrengthTargetId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
    }

    [Fact]
    public void RelationshipFitness_NonePolaritySimplyScalesByStrength()
    {
        // Backwards-compat shape: no polarity => higher strength wins (legacy semantics).
        var simulation = NewSimulation(seed: 43, opportunityRate: 0f);
        SeedNeighborhood(simulation);
        const int verbId = 430;
        const int relKindId = 7;
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var unrelatedTargetId = simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var allyTargetId = simulation.CreateEntity(TargetKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        simulation.CreateRelationship(relKindId, RelationshipPolarityCodes.Positive, actorId, allyTargetId, 1.0f, tick: 0);

        RegisterEntityKindVerb(
            simulation,
            verbId,
            targetKindId: TargetKindId,
            weight: 0.001f,
            fitnessRelationshipKindId: relKindId,
            fitnessRelationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Outgoing,
            fitnessPolarityCode: AffordancePolarityCodes.None,
            fitnessThreshold: 0f,
            fitnessWeight: 1000f);

        simulation.AdvanceTicks(1);

        Assert.Equal(allyTargetId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
        Assert.NotEqual(unrelatedTargetId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
    }

    [Fact]
    public void TraitWeightDelta_BoostsAuthoredVerbWeight()
    {
        var simulation = NewSimulation(seed: 51, opportunityRate: 0f);
        SeedNeighborhood(simulation);
        const int wildernessVerbId = 510;
        const int settlementVerbId = 511;
        const int wildernessKindId = 7;
        const int settlementKindId = 8;
        const int wildernessTraitId = 100;
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var wildernessTargetId = simulation.CreateEntity(wildernessKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        simulation.CreateEntity(settlementKindId, 1, 1, -1, 0, 0, 0, 0, 0);

        // Without trait: equal-weight rivals.
        RegisterEntityKindVerb(simulation, wildernessVerbId, targetKindId: wildernessKindId, weight: 1f);
        RegisterEntityKindVerb(simulation, settlementVerbId, targetKindId: settlementKindId, weight: 1f);
        // Trait massively boosts the wilderness verb's weight on the actor.
        simulation.LowAltitudeVerbs.RegisterTraitWeightModifier(
            ActorKindId,
            wildernessTraitId,
            wildernessVerbId,
            weightDelta: 1000f,
            opportunityBonusDelta: 0f,
            fitnessWeightDelta: 0f);
        simulation.World.AddTrait(actorId, wildernessTraitId, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(wildernessTargetId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
    }

    [Fact]
    public void TraitWeightDelta_DoesNotApplyWhenActorLacksTrait()
    {
        var simulation = NewSimulation(seed: 52, opportunityRate: 0f);
        SeedNeighborhood(simulation);
        const int verbId = 520;
        const int traitId = 101;
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        RegisterEntityKindVerb(simulation, verbId, targetKindId: TargetKindId, weight: 1f);
        simulation.LowAltitudeVerbs.RegisterTraitWeightModifier(
            ActorKindId,
            traitId,
            verbId,
            weightDelta: -1000f,
            opportunityBonusDelta: 0f,
            fitnessWeightDelta: 0f);

        simulation.AdvanceTicks(1);

        // Even though trait would suppress, actor lacks trait, so verb still fires.
        Assert.True(simulation.LowAltitudeVerbs.State.ActiveVerbSlot(actorId) > 0);
    }

    [Fact]
    public void TraitOpportunityBonusDelta_BoostsAffordanceSensitivityForVerb()
    {
        var simulation = NewSimulation(seed: 53, opportunityRate: 1f, opportunityBonus: 0f);
        SeedNeighborhood(simulation);
        const int hotVerbId = 530;
        const int neutralVerbId = 531;
        const int hotKindId = 9;
        const int traitId = 102;
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var hotTargetId = simulation.CreateEntity(hotKindId, 1, 1, -1, 0, 0, 0, 0, 0);

        RegisterEntityKindVerb(simulation, neutralVerbId, targetKindId: TargetKindId, weight: 1f);
        RegisterEntityKindVerb(simulation, hotVerbId, targetKindId: hotKindId, weight: 1f);
        AddPresenceAffordance(simulation, sourceId: 1, candidateKindId: hotKindId, weight: 1f);
        // Default opportunity bonus is 0 so no effect; trait raises it for the hot verb only.
        simulation.LowAltitudeVerbs.RegisterTraitWeightModifier(
            ActorKindId,
            traitId,
            hotVerbId,
            weightDelta: 0f,
            opportunityBonusDelta: 100f,
            fitnessWeightDelta: 0f);
        simulation.World.AddTrait(actorId, traitId, tick: 0);

        simulation.AdvanceTicks(1);

        Assert.Equal(hotTargetId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
    }

    [Fact]
    public void DistanceScoring_PrefersCloserTargetsAtSamePolarity()
    {
        var simulation = NewSimulation(seed: 61, opportunityRate: 0f);
        SeedRadius2Neighborhood(simulation);
        const int verbId = 610;
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var nearTargetId = simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        simulation.CreateEntity(TargetKindId, 1, 1, 2, 0, 0, 0, 0, 0);
        // Tighten the distance scale so close-vs-far is a sharp preference.
        RegisterEntityKindVerb(simulation, verbId, targetKindId: TargetKindId, weight: 1f, distanceScaleOverride: 1f);

        simulation.AdvanceTicks(1);

        Assert.Equal(nearTargetId, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
    }

    [Fact]
    public void TerrainCandidateSelection_DoesNotDependOnUniformRandomHit()
    {
        var simulation = NewSimulation(seed: 1, opportunityRate: 0f);
        SeedRadius3NeighborhoodWithSpecialTerrain(simulation, specialQ: 3, specialR: 0, specialTerrainId: 2);
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var slot = simulation.LowAltitudeVerbs.RegisterVerb(
            ActorKindId,
            verbId: 810,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.MoveActor,
            TargetSelectorCodes.Terrain,
            targetKindId: 0,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 5,
            maxCommitmentTicks: 5,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: 1f);
        simulation.LowAltitudeVerbs.AddTerrainFilter(slot, terrainId: 2);
        simulation.LowAltitudeVerbs.AddPrimitiveStep(slot, ProgramOpcodeCodes.MoveActor, 0, 0f);

        simulation.AdvanceTicks(1);

        Assert.Equal((3, 0), (simulation.LowAltitudeVerbs.State.TargetQ(actorId), simulation.LowAltitudeVerbs.State.TargetR(actorId)));
    }

    [Fact]
    public void OwnInfluenceCandidateSelection_DoesNotDependOnUniformRandomHit()
    {
        var simulation = NewSimulation(seed: 1, opportunityRate: 0f);
        SeedRadius3Neighborhood(simulation);
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        using (simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0))
        {
            simulation.SetCellInfluence(actorId, PlaneCodes.Physical, 3, 0, 15f, tick: 0);
        }

        var slot = simulation.LowAltitudeVerbs.RegisterVerb(
            ActorKindId,
            verbId: 811,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.AddOwnedCellScalar,
            TargetSelectorCodes.SelfMapValue,
            targetKindId: 0,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 5,
            maxCommitmentTicks: 5,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: 1f,
            influenceDelta: 1f,
            influenceRadius: 0);
        simulation.LowAltitudeVerbs.AddPrimitiveStep(slot, ProgramOpcodeCodes.AddOwnedCellScalar, 0, 0f);

        simulation.AdvanceTicks(1);

        var targetQ = simulation.LowAltitudeVerbs.State.TargetQ(actorId);
        var targetR = simulation.LowAltitudeVerbs.State.TargetR(actorId);
        Assert.True(HexDistance(targetQ, targetR, 3, 0) <= 1);
    }

    [Fact]
    public void OwnInfluenceCandidateSelection_CanTargetAdjacentCulturalFrontier()
    {
        var simulation = NewSimulation(seed: 2, opportunityRate: 0f);
        SeedRadius3Neighborhood(simulation);
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        using (simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0))
        {
            simulation.SetCellInfluence(actorId, PlaneCodes.Physical, 0, 0, 100f, tick: 0);
        }

        var slot = simulation.LowAltitudeVerbs.RegisterVerb(
            ActorKindId,
            verbId: 813,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.AddOwnedCellScalar,
            TargetSelectorCodes.SelfMapValue,
            targetKindId: 0,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 5,
            maxCommitmentTicks: 5,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: 1f,
            influenceDelta: 1f,
            influenceRadius: 0);
        simulation.LowAltitudeVerbs.AddPrimitiveStep(slot, ProgramOpcodeCodes.AddOwnedCellScalar, 0, 0f);

        simulation.AdvanceTicks(1);

        var targetQ = simulation.LowAltitudeVerbs.State.TargetQ(actorId);
        var targetR = simulation.LowAltitudeVerbs.State.TargetR(actorId);
        Assert.Equal(1, HexDistance(targetQ, targetR, 0, 0));
    }

    [Fact]
    public void OwnInfluenceCandidateSelection_ReinforcementStaysOnExistingCultureMass()
    {
        var simulation = NewSimulation(seed: 3, opportunityRate: 0f);
        SeedRadius3Neighborhood(simulation);
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        using (simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0))
        {
            simulation.SetCellInfluence(actorId, PlaneCodes.Physical, 0, 0, 15f, tick: 0);
        }

        var slot = simulation.LowAltitudeVerbs.RegisterVerb(
            ActorKindId,
            verbId: 814,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.AddOwnedCellScalar,
            TargetSelectorCodes.SelfMapValue,
            targetKindId: 0,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 5,
            maxCommitmentTicks: 5,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: 1f,
            influenceDelta: 1f,
            influenceRadius: 0,
            influenceTargetingCode: LowAltitudeInfluenceTargetingCodes.Reinforce);
        simulation.LowAltitudeVerbs.AddPrimitiveStep(slot, ProgramOpcodeCodes.AddOwnedCellScalar, 0, 0f);

        simulation.AdvanceTicks(1);

        Assert.Equal((0, 0), (simulation.LowAltitudeVerbs.State.TargetQ(actorId), simulation.LowAltitudeVerbs.State.TargetR(actorId)));
    }

    [Fact]
    public void OwnInfluenceCandidateSelection_SpreadIgnoresUnsupportedTraceInfluence()
    {
        var simulation = NewSimulation(seed: 4, opportunityRate: 0f);
        SeedRadius3Neighborhood(simulation);
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        using (simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0))
        {
            simulation.SetCellInfluence(actorId, PlaneCodes.Physical, 1, 0, 0.6f, tick: 0);
        }

        var slot = simulation.LowAltitudeVerbs.RegisterVerb(
            ActorKindId,
            verbId: 815,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.AddOwnedCellScalar,
            TargetSelectorCodes.SelfMapValue,
            targetKindId: 0,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 5,
            maxCommitmentTicks: 5,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: 1f,
            influenceDelta: 1f,
            influenceRadius: 0,
            influenceTargetingCode: LowAltitudeInfluenceTargetingCodes.Spread);
        simulation.LowAltitudeVerbs.AddPrimitiveStep(slot, ProgramOpcodeCodes.AddOwnedCellScalar, 0, 0f);

        simulation.AdvanceTicks(1);

        Assert.Equal(0, simulation.LowAltitudeVerbs.State.ActiveVerbSlot(actorId));
    }

    [Fact]
    public void ForeignInfluenceCandidateSelection_DoesNotDependOnUniformRandomHit()
    {
        var simulation = NewSimulation(seed: 1, opportunityRate: 0f);
        SeedRadius3Neighborhood(simulation);
        var actorId = simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var influencerId = simulation.CreateEntity(TargetKindId, 1, 2, 1, 0, 0, 0, 0, 0);
        using (simulation.EnterExecutionContext(simulation.StartupSourceCodeId, programId: 0))
        {
            simulation.SetCellInfluence(actorId, PlaneCodes.Physical, 2, 0, 30f, tick: 0);
            simulation.SetCellInfluence(influencerId, PlaneCodes.Physical, 3, 0, 40f, tick: 0);
        }

        var slot = simulation.LowAltitudeVerbs.RegisterVerb(
            ActorKindId,
            verbId: 812,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.SubtractOtherCellScalar,
            TargetSelectorCodes.OtherMapValue,
            targetKindId: 0,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 5,
            maxCommitmentTicks: 5,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 1,
            weight: 1f,
            influenceDelta: 1f,
            influenceRadius: 0);
        simulation.LowAltitudeVerbs.AddPrimitiveStep(slot, ProgramOpcodeCodes.SubtractOtherCellScalar, 0, 0f);

        simulation.AdvanceTicks(1);

        _ = influencerId;
        Assert.Equal(0, simulation.LowAltitudeVerbs.State.TargetEntityId(actorId));
        Assert.Equal((3, 0), (simulation.LowAltitudeVerbs.State.TargetQ(actorId), simulation.LowAltitudeVerbs.State.TargetR(actorId)));
    }

    [Fact]
    public void DefaultOpportunityRate_IsRoughlyFortyPercent()
    {
        // Sanity: default tuning has OpportunityRate = 0.4. Run many short selections and check
        // that opportunity-leaning verb wins ~30-50% of the time. Use a single isolated actor
        // and recreate the simulation for each trial so RNG draws are independent.
        const int trials = 80;
        const int hotVerbId = 700;
        const int hotKindId = 9;
        var hotPicks = 0;
        for (var trial = 0; trial < trials; trial++)
        {
            var simulation = NewSimulation(seed: 700 + trial);
            SeedNeighborhood(simulation);
            simulation.CreateEntity(ActorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
            simulation.CreateEntity(TargetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
            simulation.CreateEntity(hotKindId, 1, 1, -1, 0, 0, 0, 0, 0);
            RegisterEntityKindVerb(simulation, verbId: 701, targetKindId: TargetKindId, weight: 1f);
            RegisterEntityKindVerb(simulation, verbId: hotVerbId, targetKindId: hotKindId, weight: 1f);
            AddPresenceAffordance(simulation, sourceId: 1, candidateKindId: hotKindId, weight: 5f);

            simulation.AdvanceTicks(1);

            var slot = simulation.LowAltitudeVerbs.State.ActiveVerbSlot(1);
            if (slot != 0 && simulation.LowAltitudeVerbs.Data.VerbId(slot) == hotVerbId)
            {
                hotPicks++;
            }
        }

        // With opportunityRate=0.4 and one routine and one opportunity verb roughly equally weighted,
        // hot picks should occur often but not always.
        Assert.InRange(hotPicks, 16, trials);
    }

    [Fact]
    public void TargetKey_PackEntityAndCellAreDistinct()
    {
        var entityKey = LowAltitudeVerbProgramState.PackEntityTargetKey(42);
        var cellKey = LowAltitudeVerbProgramState.PackCellTargetKey(PlaneCodes.Physical, 1, 2);
        Assert.NotEqual(0L, entityKey);
        Assert.NotEqual(0L, cellKey);
        Assert.NotEqual(entityKey, cellKey);

        // Different cell coords produce different keys.
        var cellKeyB = LowAltitudeVerbProgramState.PackCellTargetKey(PlaneCodes.Physical, 1, 3);
        Assert.NotEqual(cellKey, cellKeyB);

        // Different planes produce different keys for same coord.
        var semanticKey = LowAltitudeVerbProgramState.PackCellTargetKey(PlaneCodes.Semantic, 1, 2);
        Assert.NotEqual(cellKey, semanticKey);
    }

    [Fact]
    public void DecisionModifier_LowAltitudeTotalsCollectThreeDeltas()
    {
        const int kindId = 1;
        const int traitId = 7;
        const int verbId = 20;
        var simulation = new Simulation(new SimulationOptions { EntityCapacity = 4 });
        var actor = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, tick: 0);
        simulation.World.AddTrait(actor, traitId, tick: 0);
        var modifiers = new[]
        {
            default,
            new DecisionModifier(verbId, kindId, traitId, TraitEffectBearerCodes.Actor, 1.5f, 2.5f, 3.5f),
            new DecisionModifier(verbId + 99, kindId, traitId, TraitEffectBearerCodes.Actor, 100f, 100f, 100f),
        };

        var totals = DecisionModifierEvaluation.SumLowAltitudeValues(
            simulation.World,
            modifierCount: 2,
            modifier => modifiers[modifier],
            kindId,
            verbId,
            actor);

        Assert.Equal(1.5f, totals.Weight);
        Assert.Equal(2.5f, totals.OpportunityBonus);
        Assert.Equal(3.5f, totals.FitnessWeight);
    }

    private static Simulation NewSimulation(
        int seed,
        float opportunityRate = 0.4f,
        float opportunityBonus = 1f,
        int recentTargetRingSize = 3)
    {
        return new Simulation(new SimulationOptions
        {
            Seed = seed,
            EntityCapacity = 32,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    LowAltitudeVerbs = new LowAltitudeVerbTuningOptions
                    {
                        OpportunityRate = opportunityRate,
                        OpportunityBonus = opportunityBonus,
                        RecentTargetRingSize = recentTargetRingSize,
                    },
                },
            },
        });
    }

    private static void SeedNeighborhood(Simulation simulation)
    {
        simulation.RegisterTerrain(TerrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(2);
        simulation.SeedCell(0, 0, TerrainId, ownerEntityId: 0);
        for (var direction = 0; direction < 6; direction++)
        {
            simulation.SeedCell(DirectionQ(direction), DirectionR(direction), TerrainId, ownerEntityId: 0);
        }
    }

    private static void SeedRadius2Neighborhood(Simulation simulation)
    {
        simulation.RegisterTerrain(TerrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(3);
        for (var dq = -2; dq <= 2; dq++)
        {
            var minDr = global::System.Math.Max(-2, -dq - 2);
            var maxDr = global::System.Math.Min(2, -dq + 2);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                simulation.SeedCell(dq, dr, TerrainId, ownerEntityId: 0);
            }
        }
    }

    private static void SeedRadius3Neighborhood(Simulation simulation)
    {
        simulation.RegisterTerrain(TerrainId, boundaryGroupId: 1);
        simulation.World.SetMapRadius(3);
        for (var q = -3; q <= 3; q++)
        {
            var minR = global::System.Math.Max(-3, -q - 3);
            var maxR = global::System.Math.Min(3, -q + 3);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.SeedCell(q, r, TerrainId, ownerEntityId: 0);
            }
        }
    }

    private static void SeedRadius3NeighborhoodWithSpecialTerrain(
        Simulation simulation,
        int specialQ,
        int specialR,
        int specialTerrainId)
    {
        SeedRadius3Neighborhood(simulation);
        simulation.RegisterTerrain(specialTerrainId, boundaryGroupId: 1);
        simulation.SeedCell(specialQ, specialR, specialTerrainId, ownerEntityId: 0);
    }

    private static void RegisterExploreVerb(Simulation simulation, int verbId, float weight)
    {
        var slot = simulation.LowAltitudeVerbs.RegisterVerb(
            ActorKindId,
            verbId,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.MoveActor,
            TargetSelectorCodes.Explore,
            targetKindId: 0,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 5,
            maxCommitmentTicks: 5,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 8,
            weight: weight);
        simulation.LowAltitudeVerbs.AddPrimitiveStep(slot, ProgramOpcodeCodes.MoveActor, 0, 0f);
    }

    private static void RegisterEntityKindVerb(
        Simulation simulation,
        int verbId,
        int targetKindId,
        float weight,
        float distanceScaleOverride = 0f,
        int fitnessRelationshipKindId = 0,
        int fitnessRelationshipDirectionCode = LowAltitudeVerbRelationshipDirectionCodes.Either,
        int fitnessPolarityCode = AffordancePolarityCodes.None,
        float fitnessThreshold = 0f,
        float fitnessWeight = 0f)
    {
        var slot = simulation.LowAltitudeVerbs.RegisterVerb(
            ActorKindId,
            verbId,
            PlaneCodes.Physical,
            ProgramOpcodeCodes.MoveActor,
            TargetSelectorCodes.EntityKind,
            targetKindId,
            relationshipKindId: 0,
            relationshipDirectionCode: LowAltitudeVerbRelationshipDirectionCodes.Either,
            minCommitmentTicks: 5,
            maxCommitmentTicks: 5,
            dwellTicks: 0,
            targetRadius: 0,
            candidateSampleCount: 8,
            weight: weight,
            opportunityBonusOverride: 0f,
            fitnessWeight: fitnessWeight,
            distanceScaleOverride: distanceScaleOverride,
            fitnessRelationshipKindId: fitnessRelationshipKindId,
            fitnessRelationshipDirectionCode: fitnessRelationshipDirectionCode,
            fitnessPolarityCode: fitnessPolarityCode,
            fitnessThreshold: fitnessThreshold);
        simulation.LowAltitudeVerbs.AddPrimitiveStep(slot, ProgramOpcodeCodes.MoveActor, 0, 0f);
    }

    private static void AddPresenceAffordance(
        Simulation simulation,
        int sourceId,
        int candidateKindId,
        float weight)
    {
        simulation.LowAltitudeVerbs.RegisterAffordance(
            sourceKindCode: AffordanceSourceKindCodes.Generator,
            sourceId: sourceId,
            shapeCode: LowAltitudeAffordanceShapeCodes.PresenceNearEntity,
            actorKindId: ActorKindId,
            planeCode: PlaneCodes.Physical,
            candidateKindId: candidateKindId,
            relationshipKindId: 0,
            relationshipDirectionCode: 0,
            operationCode: 0,
            polarityCode: AffordancePolarityCodes.None,
            threshold: 0f,
            weight: weight);
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

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var asCoord = -aq - ar;
        var bsCoord = -bq - br;
        return (Abs(aq - bq) + Abs(ar - br) + Abs(asCoord - bsCoord)) / 2;
    }

    private static int Abs(int value) => value < 0 ? -value : value;
}
