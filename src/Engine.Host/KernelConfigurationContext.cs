namespace Engine.Host;

using Engine.Constants;
using Engine.Domain;
using Engine.Kernel;

internal sealed class KernelConfigurationContext
{
    private readonly Simulation _kernel;

    internal KernelConfigurationContext(
        Simulation kernel,
        DomainIdRegistry domainIds,
        KernelSymbolContext symbols)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        ArgumentNullException.ThrowIfNull(domainIds);
        ArgumentNullException.ThrowIfNull(symbols);

        _kernel = kernel;
        DomainIds = domainIds;
        Symbols = symbols;
    }

    public DomainIdRegistry DomainIds { get; }

    public KernelSymbolContext Symbols { get; }

    public void SetMapRadius(int radius) =>
        _kernel.World.SetMapRadius(radius);

    public int RegisterTerrain(
        TerrainRuntimeId terrainId,
        int boundaryGroupId,
        float movementCost = 1f,
        float influenceMultiplier = 1f,
        float claimCostMultiplier = 1f,
        float claimChanceMultiplier = 1f,
        float roadBuildMultiplier = 1f,
        float roadSpeedMultiplier = 1f)
    {
        _kernel.RegisterTerrain(
            terrainId.Value,
            boundaryGroupId,
            movementCost,
            influenceMultiplier,
            claimCostMultiplier,
            claimChanceMultiplier,
            roadBuildMultiplier,
            roadSpeedMultiplier);
        return terrainId.Value;
    }

    public int RegisterTerrain(
        int terrainId,
        int boundaryGroupId,
        float movementCost = 1f,
        float influenceMultiplier = 1f,
        float claimCostMultiplier = 1f,
        float claimChanceMultiplier = 1f,
        float roadBuildMultiplier = 1f,
        float roadSpeedMultiplier = 1f)
    {
        _kernel.RegisterTerrain(
            terrainId,
            boundaryGroupId,
            movementCost,
            influenceMultiplier,
            claimCostMultiplier,
            claimChanceMultiplier,
            roadBuildMultiplier,
            roadSpeedMultiplier);
        return terrainId;
    }

    public void RegisterTerrainKindProfile(
        int terrainId,
        int kindId,
        int fieldFlags,
        float influenceMultiplier,
        float claimCostMultiplier,
        float claimChanceMultiplier) =>
        _kernel.RegisterTerrainKindProfile(
            terrainId,
            kindId,
            fieldFlags,
            influenceMultiplier,
            claimCostMultiplier,
            claimChanceMultiplier);

    public void RegisterTerrainMovementProfile(
        int terrainId,
        int movementProfileId,
        float movementCost) =>
        _kernel.RegisterTerrainMovementProfile(terrainId, movementProfileId, movementCost);

    public void RegisterDefaultMovementProfile(
        int kindId,
        int planeCode,
        int movementProfileId) =>
        _kernel.Movement.RegisterDefaultMovementProfile(kindId, planeCode, movementProfileId);

    public void RegisterSubtypeMovementProfile(
        int kindId,
        int subtypeId,
        int planeCode,
        int movementProfileId) =>
        _kernel.Movement.RegisterSubtypeMovementProfile(kindId, subtypeId, planeCode, movementProfileId);

    public void RegisterTraitMovementProfile(
        int kindId,
        int traitId,
        int planeCode,
        int movementProfileId) =>
        _kernel.Movement.RegisterTraitMovementProfile(kindId, traitId, planeCode, movementProfileId);

    public void RegisterTravelStructure(
        int travelStructureId,
        int planeCode) =>
        _kernel.Movement.RegisterTravelStructure(travelStructureId, planeCode);

    public void RegisterTravelStructureMovementProfile(
        int travelStructureId,
        int movementProfileId,
        float movementCost) =>
        _kernel.Movement.RegisterTravelStructureMovementProfile(travelStructureId, movementProfileId, movementCost);

    public int RegisterAttribute(
        int kindId,
        AttributeRuntimeId attributeId,
        float minimum,
        float maximum,
        float defaultValue,
        float defaultVariance = 0f) =>
        _kernel.RegisterAttribute(
            kindId,
            attributeId.Value,
            minimum,
            maximum,
            defaultValue,
            defaultVariance);

    public int RegisterTrait(
        int kindId,
        TraitRuntimeId traitId,
        float assignmentChance) =>
        _kernel.RegisterTrait(kindId, traitId.Value, assignmentChance);

    public int RegisterRelationshipKind(
        RelationshipKindRuntimeId kindId,
        float cullThreshold,
        bool targetFollowsSourcePhysical = false,
        bool isExclusive = false,
        bool isCultureAnchor = false) =>
        _kernel.Relationships.RegisterRelationshipKind(
            kindId.Value,
            cullThreshold,
            targetFollowsSourcePhysical,
            isExclusive,
            isCultureAnchor);

    public int AddRelationshipMaintenanceRule(
        RelationshipKindRuntimeId kindId,
        int conditionCode,
        int planeCode,
        int maxDistance,
        float threshold,
        float delta) =>
        _kernel.Relationships.AddMaintenanceRule(
            kindId.Value,
            conditionCode,
            planeCode,
            maxDistance,
            threshold,
            delta);

    public int RegisterLifecycleUpkeep(
        int kindId,
        AttributeRuntimeId attributeId,
        float baseRate,
        float ageCoefficient,
        float variance = 0.2f) =>
        _kernel.Upkeep.RegisterLifecycle(kindId, attributeId.Value, baseRate, ageCoefficient, variance);

    public int RegisterAttributeDeltaEffect(
        int kindId,
        AttributeRuntimeId attributeId,
        int conditionCode,
        int relationshipKindId,
        int relationshipDirectionCode,
        int planeCode,
        int maxDistance,
        float threshold,
        float minimumRelationshipStrength,
        float deltaPerTick,
        int requiredTargetTraitId = 0,
        int requiredRelatedTraitId = 0,
        int cultureId = 0,
        int cultureSlotId = 0,
        int cultureSlotPolarityCode = -1,
        float supportScale = 0f,
        int coverageOwnerKindId = 0,
        int coverageOwnerSubtypeId = 0) =>
        _kernel.Effects.RegisterAttributeDeltaEffect(
            kindId,
            attributeId.Value,
            conditionCode,
            relationshipKindId,
            relationshipDirectionCode,
            planeCode,
            maxDistance,
            threshold,
            minimumRelationshipStrength,
            deltaPerTick,
            requiredTargetTraitId,
            requiredRelatedTraitId,
            cultureId,
            cultureSlotId,
            cultureSlotPolarityCode,
            supportScale,
            coverageOwnerKindId,
            coverageOwnerSubtypeId);

    public void RegisterRelationshipTagRule(
        int kindId,
        int tagId,
        int relationshipKindId,
        int relationshipDirectionCode,
        float setAtOrAbove,
        float clearBelow,
        int insideCoverageOwnerKindId = 0,
        int insideCoverageOwnerSubtypeId = 0,
        int insideCoveragePlaneCode = PlaneCodes.Semantic) =>
        _kernel.Effects.RegisterRelationshipTagRule(
            kindId,
            tagId,
            relationshipKindId,
            relationshipDirectionCode,
            setAtOrAbove,
            clearBelow,
            insideCoverageOwnerKindId,
            insideCoverageOwnerSubtypeId,
            insideCoveragePlaneCode);

    public void RegisterCultureProduction(int cultureId, float physicalMultiplier, float semanticMultiplier) =>
        _kernel.RegisterCultureProduction(cultureId, physicalMultiplier, semanticMultiplier);

    public int RegisterCultureSlot(
        int cultureId,
        int slotId,
        int polarityCode,
        float minimumScore,
        int minimumTenureTicks,
        float displacementMargin,
        float entityInfluenceMultiplierDelta,
        float cultureInfluenceMultiplierDelta) =>
        _kernel.CultureSlots.RegisterSlot(
            cultureId,
            slotId,
            polarityCode,
            minimumScore,
            minimumTenureTicks,
            displacementMargin,
            entityInfluenceMultiplierDelta,
            cultureInfluenceMultiplierDelta);

    public void AddCultureSlotAllowedKind(int slot, int kindId) =>
        _kernel.CultureSlots.AddAllowedKind(slot, kindId);

    public void AddCultureSlotScoreRule(
        int slot,
        int signalCode,
        int relationshipKindId,
        float weight) =>
        _kernel.CultureSlots.AddScoreRule(slot, signalCode, relationshipKindId, weight);

    public int RegisterMinorActorEmitterRule(
        int sourceKindId,
        int sourceSubtypeId,
        int purposeCode,
        int actorKindCode,
        int actionCode,
        int planeCode,
        int ttlPulses,
        int emitCadencePulses,
        int maxActivePerSource,
        int spawnRadius,
        int moveChancePermille,
        int actionCadencePulses,
        float workDelta) =>
        _kernel.MinorActors.RegisterEmitterRule(
            sourceKindId,
            sourceSubtypeId,
            purposeCode,
            actorKindCode,
            actionCode,
            planeCode,
            ttlPulses,
            emitCadencePulses,
            maxActivePerSource,
            spawnRadius,
            moveChancePermille,
            actionCadencePulses,
            workDelta);

    public int RegisterMinorActorVerb(
        int sourceRuleSlot,
        int verbId,
        int actionCode,
        int targetingCode,
        int planeCode,
        int radius,
        int sampleCount,
        float weight,
        float workDeltaMultiplier) =>
        _kernel.MinorActors.RegisterVerb(
            sourceRuleSlot,
            verbId,
            actionCode,
            targetingCode,
            planeCode,
            radius,
            sampleCount,
            weight,
            workDeltaMultiplier);

    public int RegisterLowAltitudeVerb(
        int kindId,
        int verbId,
        int planeCode,
        int operationCode,
        int targetModeCode,
        int targetKindId,
        int relationshipKindId,
        int relationshipDirectionCode,
        int minCommitmentTicks,
        int maxCommitmentTicks,
        int dwellTicks,
        int targetRadius,
        int candidateSampleCount,
        float weight,
        float opportunityBonusOverride,
        float fitnessWeight,
        float distanceScaleOverride,
        int fitnessRelationshipKindId,
        int fitnessRelationshipDirectionCode,
        int fitnessPolarityCode,
        float fitnessThreshold,
        float influenceDelta,
        int influenceRadius,
        float coverageGrowthChance,
        float headquartersMoveChance,
        float requiredInfluence,
        int foreignInfluenceFilterEnabled = 0,
        int foreignInfluenceBlockPositiveRelationships = 0,
        int foreignInfluenceAllowNegativeRelationships = 0,
        float foreignInfluenceMinimumRelationshipStrength = 0f,
        float foreignInfluenceAllowUnrelatedInfluenceAtOrAbove = -1f,
        float relationshipStrengthDelta = 0f,
        int influenceTargetingCode = LowAltitudeInfluenceTargetingCodes.None) =>
        _kernel.LowAltitudeVerbs.RegisterVerb(
            kindId,
            verbId,
            planeCode,
            operationCode,
            targetModeCode,
            targetKindId,
            relationshipKindId,
            relationshipDirectionCode,
            minCommitmentTicks,
            maxCommitmentTicks,
            dwellTicks,
            targetRadius,
            candidateSampleCount,
            weight,
            opportunityBonusOverride,
            fitnessWeight,
            distanceScaleOverride,
            fitnessRelationshipKindId,
            fitnessRelationshipDirectionCode,
            fitnessPolarityCode,
            fitnessThreshold,
            influenceDelta,
            influenceRadius,
            coverageGrowthChance,
            headquartersMoveChance,
            requiredInfluence,
            foreignInfluenceFilterEnabled,
            foreignInfluenceBlockPositiveRelationships,
            foreignInfluenceAllowNegativeRelationships,
            foreignInfluenceMinimumRelationshipStrength,
            foreignInfluenceAllowUnrelatedInfluenceAtOrAbove,
            relationshipStrengthDelta,
            influenceTargetingCode);

    public void AddLowAltitudeVerbPrimitiveStep(
        int verbSlot,
        int opcode,
        int intArgument,
        float floatArgument) =>
        _kernel.LowAltitudeVerbs.AddPrimitiveStep(verbSlot, opcode, intArgument, floatArgument);

    public void AddLowAltitudeVerbTerrainFilter(int verbSlot, int terrainId) =>
        _kernel.LowAltitudeVerbs.AddTerrainFilter(verbSlot, terrainId);

    public int RegisterLowAltitudeInitialInfluence(
        int kindId,
        int planeCode,
        float influenceValue) =>
        _kernel.LowAltitudeVerbs.RegisterInitialInfluence(kindId, planeCode, influenceValue);

    public int RegisterLowAltitudeAffordance(
        int sourceKindCode,
        int sourceId,
        int shapeCode,
        int actorKindId,
        int planeCode,
        int candidateKindId,
        int relationshipKindId,
        int relationshipDirectionCode,
        int operationCode,
        int polarityCode,
        float threshold,
        float weight) =>
        _kernel.LowAltitudeVerbs.RegisterAffordance(
            sourceKindCode,
            sourceId,
            shapeCode,
            actorKindId,
            planeCode,
            candidateKindId,
            relationshipKindId,
            relationshipDirectionCode,
            operationCode,
            polarityCode,
            threshold,
            weight);

    public int RegisterLowAltitudeTraitWeightModifier(
        int kindId,
        int traitId,
        int verbId,
        float weightDelta,
        float opportunityBonusDelta,
        float fitnessWeightDelta) =>
        _kernel.LowAltitudeVerbs.RegisterTraitWeightModifier(
            kindId,
            traitId,
            verbId,
            weightDelta,
            opportunityBonusDelta,
            fitnessWeightDelta);

    public void ConfigureMapActivity(float presenceHeat, float heatDecayPerTick, float heatEmaAlpha)
    {
        _kernel.MapActivity.Data.PresenceHeat = presenceHeat;
        _kernel.MapActivity.Data.HeatDecayPerTick = heatDecayPerTick;
        _kernel.MapActivity.Data.HeatEmaAlpha = heatEmaAlpha;
    }

    public void ConfigureMapSignals(float semanticBaseCapacity, int crowdingRadius)
    {
        _kernel.MapSignals.Data.SemanticBaseCapacity = semanticBaseCapacity;
        _kernel.MapSignals.Data.CrowdingRadius = Math.Max(0, crowdingRadius);
    }

    public void RegisterTerrainMapSignal(int terrainId, float baseCapacity) =>
        _kernel.MapSignals.RegisterTerrainCapacity(terrainId, baseCapacity);

    public void RegisterKindMapSignal(
        int kindId,
        int signalRadius,
        float capacityEmission,
        float supportEmission,
        float threatEmission,
        float baseAttractorMass,
        float coverageAttractorScale,
        float influenceAttractorScale,
        float activityAttractorScale,
        int attractorRadius) =>
        _kernel.MapSignals.RegisterKindSignal(
            kindId,
            signalRadius,
            capacityEmission,
            supportEmission,
            threatEmission,
            baseAttractorMass,
            coverageAttractorScale,
            influenceAttractorScale,
            activityAttractorScale,
            attractorRadius);

    public void AddKindMapSignalAttractorScalar(int kindId, int attributeId, float attractorScale) =>
        _kernel.MapSignals.AddKindAttractorScalar(kindId, attributeId, attractorScale);

    public void RegisterRelationshipMapSignal(
        int relationshipKindId,
        int signalRadius,
        float supportEmission,
        float threatEmission) =>
        _kernel.MapSignals.RegisterRelationshipSignal(
            relationshipKindId,
            signalRadius,
            supportEmission,
            threatEmission);

    public int RegisterGambit(
        int kindId,
        int gambitId,
        int cooldownTicks = 0,
        int entityCooldownTicks = 0,
        int targetMode = BehaviorTargetModeCodes.Self,
        int targetKindId = 0,
        float successChance = 1f) =>
        _kernel.Gambits.RegisterGambit(
            kindId,
            gambitId,
            cooldownTicks,
            entityCooldownTicks,
            targetMode,
            targetKindId,
            successChance);

    public void MarkGambitChronicleGranted(int gambitSlot) =>
        _kernel.Gambits.MarkChronicleGranted(gambitSlot);

    public void AddGambitPrecondition(
        int gambitSlot,
        int preconditionCode,
        int a = 0,
        int b = 0,
        int c = 0,
        int d = 0,
        float x = 0f,
        float y = 0f) =>
        _kernel.Gambits.AddPrecondition(gambitSlot, preconditionCode, a, b, c, d, x, y);

    public void AddGambitScoreExpressionOp(int gambitSlot, int expressionCode, int a = 0, float x = 0f) =>
        _kernel.Gambits.AddExpressionOp(gambitSlot, expressionCode, a, x);

    public void AddGambitProviderSelector(
        int gambitSlot,
        int selectorCode,
        int kindId = 0,
        int valueId = 0,
        int relationshipKindId = 0,
        float minimumStrength = 0f) =>
        _kernel.Gambits.AddProviderSelector(
            gambitSlot,
            selectorCode,
            kindId,
            valueId,
            relationshipKindId,
            minimumStrength);

    public int AddGambitStep(
        int gambitSlot,
        int operationCode,
        int a = 0,
        int b = 0,
        int c = 0,
        int d = 0,
        float x = 0f,
        float y = 0f) =>
        _kernel.Gambits.AddStep(gambitSlot, operationCode, a, b, c, d, x, y);

    public void AddGambitStepExpressionOp(int stepSlot, int expressionCode, int a = 0, float x = 0f) =>
        _kernel.Gambits.AddStepExpressionOp(stepSlot, expressionCode, a, x);

    public int RegisterTraitGambitModifier(
        int gambitId,
        int traitId,
        int bearerCode,
        float scoreDelta,
        float successChanceDelta) =>
        _kernel.Gambits.RegisterTraitModifier(gambitId, traitId, bearerCode, scoreDelta, successChanceDelta);

    public int RegisterGenerator(
        int generatorId,
        int targetKindId,
        int pulseWeight,
        int maxRuns,
        int cadenceTicks = 0,
        int cadenceJitterTicks = 0,
        int targetRequiredTagBits = 0,
        int targetWithoutTagBits = 0) =>
        _kernel.Generators.RegisterGenerator(
            generatorId,
            targetKindId,
            pulseWeight,
            maxRuns,
            cadenceTicks,
            cadenceJitterTicks,
            targetRequiredTagBits,
            targetWithoutTagBits);

    public void AddGeneratorTargetSubtype(int generatorSlot, int subtypeId) =>
        _kernel.Generators.AddTargetSubtype(generatorSlot, subtypeId);

    public void AddGeneratorTargetRequiredOutgoingRelationship(
        int generatorSlot,
        int relationshipKindId,
        float minimumStrength = 0f) =>
        _kernel.Generators.AddTargetRequiredOutgoingRelationship(generatorSlot, relationshipKindId, minimumStrength);

    public void AddGeneratorTargetRequiredIncomingRelationship(
        int generatorSlot,
        int relationshipKindId,
        float minimumStrength = 0f) =>
        _kernel.Generators.AddTargetRequiredIncomingRelationship(generatorSlot, relationshipKindId, minimumStrength);

    public void AddGeneratorTargetRequiredRelationship(
        int generatorSlot,
        int relationshipKindId,
        float minimumStrength = 0f) =>
        _kernel.Generators.AddTargetRequiredRelationship(generatorSlot, relationshipKindId, minimumStrength);

    public void AddGeneratorTargetWithoutOutgoingRelationship(int generatorSlot, int relationshipKindId) =>
        _kernel.Generators.AddTargetWithoutOutgoingRelationship(generatorSlot, relationshipKindId);

    public void AddGeneratorTargetWithoutIncomingRelationship(int generatorSlot, int relationshipKindId) =>
        _kernel.Generators.AddTargetWithoutIncomingRelationship(generatorSlot, relationshipKindId);

    public void AddGeneratorTargetTerrainCountBelow(
        int generatorSlot,
        int terrainId,
        int radius,
        int maximumExclusive) =>
        _kernel.Generators.AddTargetTerrainCountBelow(generatorSlot, terrainId, radius, maximumExclusive);

    public void AddGeneratorTrigger(int generatorSlot, int kindId) =>
        _kernel.Generators.AddTrigger(generatorSlot, kindId);

    public int AddGeneratorSecondaryBinding(
        int generatorSlot,
        int refCode,
        int kindId,
        bool adjacentToTarget,
        int cultureMatchCode,
        bool samePhysicalCultureComponentAsTarget,
        int requiredTagBits = 0,
        int withoutTagBits = 0,
        int cultureId = 0,
        int relatedToTargetKindId = 0,
        bool relatedToTargetIncoming = false,
        float relatedToTargetMinStrength = 0f) =>
        _kernel.Generators.AddSecondaryBinding(
            generatorSlot,
            refCode,
            kindId,
            adjacentToTarget ? 1 : 0,
            cultureMatchCode,
            samePhysicalCultureComponentAsTarget ? 1 : 0,
            requiredTagBits,
            withoutTagBits,
            cultureId,
            relatedToTargetKindId,
            relatedToTargetIncoming ? 1 : 0,
            relatedToTargetMinStrength);

    public void AddGeneratorBindingSubtype(int bindingSlot, int subtypeId) =>
        _kernel.Generators.AddBindingSubtype(bindingSlot, subtypeId);

    public void AddGeneratorBindingWithoutOutgoingRelationship(int bindingSlot, int relationshipKindId) =>
        _kernel.Generators.AddBindingWithoutOutgoingRelationship(bindingSlot, relationshipKindId);

    public void AddGeneratorBindingWithoutIncomingRelationship(int bindingSlot, int relationshipKindId) =>
        _kernel.Generators.AddBindingWithoutIncomingRelationship(bindingSlot, relationshipKindId);

    public int AddGeneratorProduced(
        int generatorSlot,
        int refCode,
        int kindId,
        int subtypeId,
        bool createIfMissing,
        bool hasQuery,
        bool queryAdjacentToTarget,
        int queryCultureMatchCode,
        int physicalPlacementCode,
        int semanticPlacementCode,
        int placementRefCode,
        int cultureRefCode,
        int fixedCultureId,
        float createChance,
        int coverageModeCode,
        int coverageSourceRefCode,
        int coverageDesiredCellCount,
        int coverageMinimumCellCount,
        int coverageMinimumSourceRemainingCellCount,
        bool coverageRespectTerrainBoundaries,
        int coverageTransformTerrainId,
        int physicalPlacementMinRadius,
        int physicalPlacementMaxRadius,
        int semanticPlacementMinRadius,
        int semanticPlacementMaxRadius,
        int culturePlacementCode,
        bool querySamePhysicalCultureComponentAsTarget,
        int queryRequiredTagBits = 0,
        int queryWithoutTagBits = 0,
        int queryCultureId = 0,
        int queryRelatedToTargetKindId = 0,
        bool queryRelatedToTargetIncoming = false,
        float queryRelatedToTargetMinStrength = 0f) =>
        _kernel.Generators.AddProduced(
            generatorSlot,
            refCode,
            kindId,
            subtypeId,
            createIfMissing ? 1 : 0,
            hasQuery ? 1 : 0,
            queryAdjacentToTarget ? 1 : 0,
            queryCultureMatchCode,
            physicalPlacementCode,
            semanticPlacementCode,
            placementRefCode,
            cultureRefCode,
            createChance,
            coverageModeCode,
            coverageSourceRefCode,
            coverageDesiredCellCount,
            coverageMinimumCellCount,
            coverageMinimumSourceRemainingCellCount,
            coverageRespectTerrainBoundaries ? 1 : 0,
            coverageTransformTerrainId,
            physicalPlacementMinRadius,
            physicalPlacementMaxRadius,
            semanticPlacementMinRadius,
            semanticPlacementMaxRadius,
            culturePlacementCode,
            querySamePhysicalCultureComponentAsTarget ? 1 : 0,
            fixedCultureId,
            queryRequiredTagBits,
            queryWithoutTagBits,
            queryCultureId,
            queryRelatedToTargetKindId,
            queryRelatedToTargetIncoming ? 1 : 0,
            queryRelatedToTargetMinStrength);

    public void SetGeneratorTargetChronicleGates(int generatorSlot, float heatMin, int legendaryWithinTicks) =>
        _kernel.Generators.SetTargetChronicleGates(generatorSlot, heatMin, legendaryWithinTicks);

    public int AddWorldBeatAge(int ageId, int trackCode, int priority, int minOpenTicks, int cooldownTicks) =>
        _kernel.WorldBeats.AddAge(ageId, trackCode, priority, minOpenTicks, cooldownTicks);

    public void AddWorldBeatOpenCondition(int ageSlot, int factCode, int comparatorCode, float value, int paramId) =>
        _kernel.WorldBeats.AddOpenCondition(ageSlot, factCode, comparatorCode, value, paramId);

    public void AddWorldBeatCloseCondition(int ageSlot, int factCode, int comparatorCode, float value, int paramId) =>
        _kernel.WorldBeats.AddCloseCondition(ageSlot, factCode, comparatorCode, value, paramId);

    public void AddWorldBeatCharge(int ageSlot, int domainCode, int targetId, float multiplier) =>
        _kernel.WorldBeats.AddCharge(ageSlot, domainCode, targetId, multiplier);

    public void SetMinorActorCampaignRelationshipKind(int relationshipKindId) =>
        _kernel.MinorActors.SetCampaignRelationshipKindId(relationshipKindId);

    public void SetMovementCarrierRelationshipKind(int relationshipKindId) =>
        _kernel.Movement.SetCarrierRelationshipKindId(relationshipKindId);

    public void SetMapFrontierConfiguration(
        int thawAgeId,
        int refreezeAgeId,
        int thawTerrainId,
        int frozenTerrainId,
        int maxRadius,
        float frontierMassThreshold,
        float reclaimMassCeiling,
        int thawBudgetPerPulse,
        int refreezeBudgetPerPulse) =>
        _kernel.MapFrontier.SetAgeConfiguration(
            thawAgeId,
            refreezeAgeId,
            thawTerrainId,
            frozenTerrainId,
            maxRadius,
            frontierMassThreshold,
            reclaimMassCeiling,
            thawBudgetPerPulse,
            refreezeBudgetPerPulse);

    public void AddMapFrontierDriverAge(int thawAgeId, int refreezeAgeId) =>
        _kernel.MapFrontier.AddDriverAge(thawAgeId, refreezeAgeId);

    public int AddChronicleArchetype(int archetypeId, int maxActive, int castingCadenceTicks, int primaryKindId, int primaryRequiredRelKindId, float primaryRequiredRelMinStrength, int fadeOutcomeCodeId, int fadeGraceTicks, int fadeQuietTicks) =>
        _kernel.Chronicles.AddArchetype(archetypeId, maxActive, castingCadenceTicks, primaryKindId, primaryRequiredRelKindId, primaryRequiredRelMinStrength, fadeOutcomeCodeId, fadeGraceTicks, fadeQuietTicks);

    public void AddChronicleRole(int archetypeSlot, int kindId, int relatedKindId, bool relatedIncoming, float relatedMinStrength, bool open = false) =>
        _kernel.Chronicles.AddRole(archetypeSlot, kindId, relatedKindId, relatedIncoming ? 1 : 0, relatedMinStrength, open ? 1 : 0);

    public int AddChronicleBeat(
        int archetypeSlot,
        int beatCodeId,
        float heatGain,
        int terminalOutcomeCodeId,
        int openKind,
        int openRoleA,
        int openRoleB,
        int openRelOrAttrId,
        float openValue,
        int strikeKind,
        int strikeActorRole,
        int strikeTargetRole,
        int strikeGambitId,
        bool strikeAcceptFailure,
        int strikeJoinRole,
        int strikeFactKind,
        int strikeFactRoleA,
        int strikeFactRoleB,
        int strikeFactRelOrAttrId,
        float strikeFactValue) =>
        _kernel.Chronicles.AddBeat(
            archetypeSlot,
            beatCodeId,
            heatGain,
            terminalOutcomeCodeId,
            openKind,
            openRoleA,
            openRoleB,
            openRelOrAttrId,
            openValue,
            strikeKind,
            strikeActorRole,
            strikeTargetRole,
            strikeGambitId,
            strikeAcceptFailure ? 1 : 0,
            strikeJoinRole,
            strikeFactKind,
            strikeFactRoleA,
            strikeFactRoleB,
            strikeFactRelOrAttrId,
            strikeFactValue);

    public void AddChronicleBeatCharge(int beatSlot, int actorRole, int targetRole, int gambitId, float scoreBonus, bool heatScaled, float successBonus) =>
        _kernel.Chronicles.AddBeatCharge(beatSlot, actorRole, targetRole, gambitId, scoreBonus, heatScaled ? 1 : 0, successBonus);

    public void AddChronicleBeatGrant(int beatSlot, int role, int gambitId) =>
        _kernel.Chronicles.AddBeatGrant(beatSlot, role, gambitId);

    public void AddGeneratorProducedSubtypeChoice(int producedSlot, int subtypeId) =>
        _kernel.Generators.AddProducedSubtypeChoice(producedSlot, subtypeId);

    public void AddGeneratorProducedQuerySubtype(int producedSlot, int subtypeId) =>
        _kernel.Generators.AddProducedQuerySubtype(producedSlot, subtypeId);

    public void AddGeneratorProducedQueryWithoutOutgoingRelationship(int producedSlot, int relationshipKindId) =>
        _kernel.Generators.AddProducedQueryWithoutOutgoingRelationship(producedSlot, relationshipKindId);

    public void AddGeneratorProducedQueryWithoutIncomingRelationship(int producedSlot, int relationshipKindId) =>
        _kernel.Generators.AddProducedQueryWithoutIncomingRelationship(producedSlot, relationshipKindId);

    public void AddGeneratorProducedPlacementPreferredTerrain(int producedSlot, int planeCode, int terrainId) =>
        _kernel.Generators.AddProducedPlacementPreferredTerrain(producedSlot, planeCode, terrainId);

    public void AddGeneratorProducedPlacementAllowedTerrain(int producedSlot, int planeCode, int terrainId) =>
        _kernel.Generators.AddProducedPlacementAllowedTerrain(producedSlot, planeCode, terrainId);

    public void AddGeneratorProducedPlacementAvoidedTerrain(int producedSlot, int planeCode, int terrainId) =>
        _kernel.Generators.AddProducedPlacementAvoidedTerrain(producedSlot, planeCode, terrainId);

    public void SetGeneratorProducedPlacementFallbackTerrain(int producedSlot, int planeCode, int terrainId) =>
        _kernel.Generators.SetProducedPlacementFallbackTerrain(producedSlot, planeCode, terrainId);

    public void AddGeneratorProducedCoveragePreferredTerrain(int producedSlot, int terrainId) =>
        _kernel.Generators.AddProducedCoveragePreferredTerrain(producedSlot, terrainId);

    public void AddGeneratorProducedCoverageAllowedTerrain(int producedSlot, int terrainId) =>
        _kernel.Generators.AddProducedCoverageAllowedTerrain(producedSlot, terrainId);

    public void AddGeneratorRelationship(
        int generatorSlot,
        int sourceRefCode,
        int destinationRefCode,
        int relationshipKindId,
        float strength,
        bool replaceExistingOutgoing) =>
        _kernel.Generators.AddRelationship(
            generatorSlot,
            sourceRefCode,
            destinationRefCode,
            relationshipKindId,
            strength,
            replaceExistingOutgoing ? 1 : 0);

    public int AddGeneratorEffect(
        int generatorSlot,
        int effectTypeCode,
        int refOrKindId,
        int instanceSelectionCode,
        int optionCode,
        float value = 0f,
        int radius = 0,
        int maxCellCount = 0,
        int planeCode = PlaneCodes.Physical) =>
        _kernel.Generators.AddEffect(
            generatorSlot,
            effectTypeCode,
            refOrKindId,
            instanceSelectionCode,
            optionCode,
            value,
            radius,
            maxCellCount,
            planeCode);

    public void AddGeneratorEffectPreferredTerrain(int effectSlot, int terrainId) =>
        _kernel.Generators.AddEffectPreferredTerrain(effectSlot, terrainId);

    public void AddGeneratorEffectAllowedTerrain(int effectSlot, int terrainId) =>
        _kernel.Generators.AddEffectAllowedTerrain(effectSlot, terrainId);

    public void SetProgramEnabled(int programId, bool enabled) =>
        _kernel.SetProgramEnabled(programId, enabled);

    public bool ProgramIsEnabled(int programId) =>
        _kernel.ProgramIsEnabled(programId);
}
