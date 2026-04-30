namespace Engine.Host;

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

    public int RegisterTerrain(TerrainRuntimeId terrainId, int boundaryGroupId)
    {
        _kernel.RegisterTerrain(terrainId.Value, boundaryGroupId);
        return terrainId.Value;
    }

    public int RegisterTerrain(int terrainId, int boundaryGroupId)
    {
        _kernel.RegisterTerrain(terrainId, boundaryGroupId);
        return terrainId;
    }

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

    public int RegisterRelationshipKind(RelationshipKindRuntimeId kindId, float cullThreshold) =>
        _kernel.Relationships.RegisterRelationshipKind(kindId.Value, cullThreshold);

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

    public int RegisterPressure(
        PressureRuntimeId pressureId,
        int scopeCode,
        float baseline,
        float homeostasisRate,
        float smoothingFactor) =>
        _kernel.Pressures.RegisterPressure(
            pressureId.Value,
            scopeCode,
            baseline,
            homeostasisRate,
            smoothingFactor);

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
        float deltaPerTick) =>
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
            deltaPerTick);

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
        float generatorAffordanceWeight,
        int affinityRelationshipKindId,
        int affinityRelationshipDirectionCode,
        float relationshipAffinityWeight,
        float influenceDelta,
        int influenceRadius,
        float coverageGrowthChance,
        float headquartersMoveChance,
        float requiredInfluence) =>
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
            generatorAffordanceWeight,
            affinityRelationshipKindId,
            affinityRelationshipDirectionCode,
            relationshipAffinityWeight,
            influenceDelta,
            influenceRadius,
            coverageGrowthChance,
            headquartersMoveChance,
            requiredInfluence);

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

    public void ConfigureLowAltitudeSemanticBounds(int minQ, int maxQ, int minR, int maxR) =>
        _kernel.LowAltitudeVerbs.ConfigureSemanticBounds(minQ, maxQ, minR, maxR);

    public int RegisterLowAltitudeGeneratorAffordance(
        int actorKindId,
        int candidateKindId,
        int bindingKindId,
        float affordanceScore) =>
        _kernel.LowAltitudeVerbs.RegisterGeneratorAffordance(
            actorKindId,
            candidateKindId,
            bindingKindId,
            affordanceScore);

    public void ConfigureMapActivity(float presenceHeat)
    {
        _kernel.MapActivity.Data.PresenceHeat = presenceHeat;
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

    public void AddGambitRewardExpressionOp(int gambitSlot, int expressionCode, int a = 0, float x = 0f) =>
        _kernel.Gambits.AddRewardExpressionOp(gambitSlot, expressionCode, a, x);

    public void AddGambitGoalEffect(int gambitSlot, int scopeCode, int valueId, int kindId) =>
        _kernel.Gambits.AddGoalEffect(gambitSlot, scopeCode, valueId, kindId);

    public void AddGambitProviderSelector(
        int gambitSlot,
        int selectorCode,
        int kindId = 0,
        int valueId = 0,
        int relationshipKindId = 0,
        int factId = 0,
        float minimumStrength = 0f) =>
        _kernel.Gambits.AddProviderSelector(
            gambitSlot,
            selectorCode,
            kindId,
            valueId,
            relationshipKindId,
            factId,
            minimumStrength);

    public int AddGambitSupportRule(
        int gambitSlot,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float desiredMinimum,
        float urgencyScale = 1f) =>
        _kernel.Gambits.AddSupportRule(
            gambitSlot,
            effectScopeCode,
            effectValueId,
            effectKindId,
            desiredMinimum,
            urgencyScale);

    public void AddGambitSupportExpressionOp(int supportRuleSlot, int expressionCode, int a = 0, float x = 0f) =>
        _kernel.Gambits.AddSupportExpressionOp(supportRuleSlot, expressionCode, a, x);

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

    public void SetGeneratorTargetSaturation(
        int generatorSlot,
        int relationshipKindId,
        int requiredFactId,
        int directionCode,
        int maxExisting) =>
        _kernel.Generators.SetTargetSaturation(
            generatorSlot,
            relationshipKindId,
            requiredFactId,
            directionCode,
            maxExisting);

    public void AddGeneratorTrigger(
        int generatorSlot,
        int kindId,
        int attributeId,
        int directionCode,
        float threshold) =>
        _kernel.Generators.AddTrigger(generatorSlot, kindId, attributeId, directionCode, threshold);

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

    public void AddGeneratorBindingAttributeCondition(
        int bindingSlot,
        int attributeId,
        int directionCode,
        float threshold) =>
        _kernel.Generators.AddBindingAttributeCondition(bindingSlot, attributeId, directionCode, threshold);

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

    public void AddGeneratorProducedQueryAttributeCondition(
        int producedSlot,
        int attributeId,
        int directionCode,
        float threshold) =>
        _kernel.Generators.AddProducedQueryAttributeCondition(producedSlot, attributeId, directionCode, threshold);

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
        bool replaceExistingOutgoing,
        int factId0 = 0,
        int factId1 = 0,
        int factId2 = 0,
        int factId3 = 0,
        int factCount = 0) =>
        _kernel.Generators.AddRelationship(
            generatorSlot,
            sourceRefCode,
            destinationRefCode,
            relationshipKindId,
            strength,
            replaceExistingOutgoing ? 1 : 0,
            factId0,
            factId1,
            factId2,
            factId3,
            factCount);

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
