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
        float movementCost,
        float influenceMultiplier,
        float claimCostMultiplier,
        float claimChanceMultiplier) =>
        _kernel.RegisterTerrainKindProfile(
            terrainId,
            kindId,
            fieldFlags,
            movementCost,
            influenceMultiplier,
            claimCostMultiplier,
            claimChanceMultiplier);

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

    public int RegisterRelationshipKind(RelationshipKindRuntimeId kindId, float cullThreshold, bool targetFollowsSourcePhysical = false) =>
        _kernel.Relationships.RegisterRelationshipKind(kindId.Value, cullThreshold, targetFollowsSourcePhysical);

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
        int requiredRelatedTraitId = 0) =>
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
            requiredRelatedTraitId);

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
        float exploreWeight,
        float stalenessWeight,
        float hotspotWeight,
        float crowdingWeight,
        float affordanceWeight,
        int affinityRelationshipKindId,
        int affinityRelationshipDirectionCode,
        float relationshipAffinityWeight,
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
        float relationshipStrengthDelta = 0f) =>
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
            exploreWeight,
            stalenessWeight,
            hotspotWeight,
            crowdingWeight,
            affordanceWeight,
            affinityRelationshipKindId,
            affinityRelationshipDirectionCode,
            relationshipAffinityWeight,
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
            relationshipStrengthDelta);

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

    public void ConfigureGameplaySemanticBounds(int minQ, int maxQ, int minR, int maxR)
    {
        _kernel.LowAltitudeVerbs.ConfigureSemanticBounds(minQ, maxQ, minR, maxR);
        _kernel.Generators.ConfigureSemanticBounds(minQ, maxQ, minR, maxR);
    }

    public void ConfigureLowAltitudeSemanticBounds(int minQ, int maxQ, int minR, int maxR) =>
        ConfigureGameplaySemanticBounds(minQ, maxQ, minR, maxR);

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
        int terrainId,
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
            terrainId,
            polarityCode,
            threshold,
            weight);

    public int RegisterLowAltitudeTraitWeightModifier(
        int kindId,
        int traitId,
        int verbId,
        float weightDelta,
        float exploreWeightDelta,
        float stalenessWeightDelta,
        float hotspotWeightDelta,
        float crowdingWeightDelta,
        float affordanceWeightDelta,
        float relationshipAffinityWeightDelta) =>
        _kernel.LowAltitudeVerbs.RegisterTraitWeightModifier(
            kindId,
            traitId,
            verbId,
            weightDelta,
            exploreWeightDelta,
            stalenessWeightDelta,
            hotspotWeightDelta,
            crowdingWeightDelta,
            affordanceWeightDelta,
            relationshipAffinityWeightDelta);

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
        int cadenceTicks,
        int targetKindId,
        int pulseWeight,
        int maxRuns,
        bool targetAllowWilderness) =>
        _kernel.Generators.RegisterGenerator(
            generatorId,
            cadenceTicks,
            targetKindId,
            pulseWeight,
            maxRuns,
            targetAllowWilderness);

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
        int cultureMatchCode) =>
        _kernel.Generators.AddSecondaryBinding(
            generatorSlot,
            refCode,
            kindId,
            adjacentToTarget ? 1 : 0,
            cultureMatchCode);

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
        float createChance,
        int coverageModeCode,
        int coverageSourceRefCode,
        int coverageDesiredCellCount,
        int coverageMinimumCellCount,
        int coverageMinimumSourceRemainingCellCount,
        bool coverageRespectTerrainBoundaries,
        int coverageTransformTerrainId) =>
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
            coverageTransformTerrainId);

    public void AddGeneratorProducedSubtypeChoice(int producedSlot, int subtypeId) =>
        _kernel.Generators.AddProducedSubtypeChoice(producedSlot, subtypeId);

    public void AddGeneratorProducedQuerySubtype(int producedSlot, int subtypeId) =>
        _kernel.Generators.AddProducedQuerySubtype(producedSlot, subtypeId);

    public void AddGeneratorProducedQueryWithoutOutgoingRelationship(int producedSlot, int relationshipKindId) =>
        _kernel.Generators.AddProducedQueryWithoutOutgoingRelationship(producedSlot, relationshipKindId);

    public void AddGeneratorProducedQueryWithoutIncomingRelationship(int producedSlot, int relationshipKindId) =>
        _kernel.Generators.AddProducedQueryWithoutIncomingRelationship(producedSlot, relationshipKindId);

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

    public int RegisterDegeneracyDetector(
        DetectorRuntimeId detectorId,
        int metricCode,
        int thresholdDirectionCode,
        int noSubjectsSeverityCode,
        ReasonCodeId reasonCodeId,
        int primaryKindId,
        int secondaryKindId,
        AttributeRuntimeId attributeId,
        RelationshipKindRuntimeId relationshipKindId,
        int dominantRelationshipEndpointCode,
        float warningThreshold,
        float criticalThreshold) =>
        _kernel.Degeneracy.RegisterDetector(
            detectorId.Value,
            metricCode,
            thresholdDirectionCode,
            noSubjectsSeverityCode,
            reasonCodeId.Value,
            primaryKindId,
            secondaryKindId,
            attributeId.Value,
            relationshipKindId.Value,
            dominantRelationshipEndpointCode,
            warningThreshold,
            criticalThreshold);
}
