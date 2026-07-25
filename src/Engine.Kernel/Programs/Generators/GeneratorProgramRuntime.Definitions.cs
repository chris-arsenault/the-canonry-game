namespace Engine.Kernel;

internal sealed partial class GeneratorProgramRuntime
{
    public void AddRelationship(
        int generatorSlot,
        int sourceRefCode,
        int destinationRefCode,
        int relationshipKindId,
        FixedScalar strength,
        int replaceExistingOutgoing)
    {
        EnsureOpenGenerator(generatorSlot);
        ValidatePositive(sourceRefCode);
        ValidatePositive(destinationRefCode);
        ValidatePositive(relationshipKindId);
        if (_relationshipCount + 1 >= _relationshipKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _relationshipCount++;
        _relationshipSourceRefCodes[_relationshipCount] = sourceRefCode;
        _relationshipDestinationRefCodes[_relationshipCount] = destinationRefCode;
        _relationshipKindIds[_relationshipCount] = relationshipKindId;
        _relationshipStrengths[_relationshipCount] = strength;
        _relationshipReplaceExistingOutgoing[_relationshipCount] = replaceExistingOutgoing;
        _relationshipCounts[generatorSlot]++;
    }

    public int AddSecondaryBinding(
        int generatorSlot,
        int refCode,
        int kindId,
        int adjacentToTarget,
        int cultureMatchCode,
        int requireTargetCultureRoute = 0,
        int requiredTagBits = 0,
        int withoutTagBits = 0,
        int cultureId = 0,
        int relatedToTargetKindId = 0,
        int relatedToTargetIncoming = 0,
        FixedScalar relatedToTargetMinStrength = default)
    {
        EnsureOpenGenerator(generatorSlot);
        ValidatePositive(refCode);
        ValidatePositive(kindId);
        ValidatePositive(cultureMatchCode);
        if (_bindingCount + 1 >= _bindingRefCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _bindingCount++;
        _bindingRefCodes[_bindingCount] = refCode;
        _bindingKindIds[_bindingCount] = kindId;
        _bindingAdjacentToTarget[_bindingCount] = adjacentToTarget;
        _bindingCultureMatchCodes[_bindingCount] = cultureMatchCode;
        _bindingRequireTargetCultureRoute[_bindingCount] = requireTargetCultureRoute;
        _bindingRequiredTagBits[_bindingCount] = requiredTagBits;
        _bindingWithoutTagBits[_bindingCount] = withoutTagBits;
        _bindingCultureIds[_bindingCount] = cultureId;
        _bindingRelatedToTargetKindIds[_bindingCount] = relatedToTargetKindId;
        _bindingRelatedToTargetIncoming[_bindingCount] = relatedToTargetIncoming;
        _bindingRelatedToTargetMinStrengths[_bindingCount] = relatedToTargetMinStrength;
        _bindingSubtypeStarts[_bindingCount] = _subtypeCount + 1;
        _bindingWithoutOutgoingStarts[_bindingCount] = _relationshipKindFilterCount + 1;
        _bindingWithoutIncomingStarts[_bindingCount] = _relationshipKindFilterCount + 1;
        _bindingCounts[generatorSlot]++;
        _openBindingSlot = _bindingCount;
        return _bindingCount;
    }

    public int AddEffect(
        int generatorSlot,
        int effectTypeCode,
        int refOrKindId,
        int instanceSelectionCode = GeneratorRelationshipInstanceSelectionCodes.AllTargetRelationships,
        int optionCode = 0,
        FixedScalar value = default,
        int radius = 0,
        int maxCellCount = 0,
        int planeCode = PlaneCodes.Physical)
    {
        EnsureOpenGenerator(generatorSlot);
        ValidatePositive(effectTypeCode);
        ValidatePositive(refOrKindId);
        ValidateEffectInstanceSelection(effectTypeCode, instanceSelectionCode);
        ValidatePlaneCode(planeCode);
        NumericGuard.ThrowIfNegative(optionCode);
        NumericGuard.ThrowIfNegative(radius);
        NumericGuard.ThrowIfNegative(maxCellCount);
        if (_effectCount + 1 >= _effectTypeCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _effectCount++;
        _effectTypeCodes[_effectCount] = effectTypeCode;
        _effectRefOrKindIds[_effectCount] = refOrKindId;
        _effectInstanceSelectionCodes[_effectCount] = instanceSelectionCode;
        _effectOptionCodes[_effectCount] = optionCode;
        _effectPlaneCodes[_effectCount] = planeCode;
        _effectValues[_effectCount] = value;
        _effectRadii[_effectCount] = radius;
        _effectMaxCellCounts[_effectCount] = maxCellCount;
        _effectPreferredTerrainStarts[_effectCount] = _terrainFilterCount + 1;
        _effectAllowedTerrainStarts[_effectCount] = _terrainFilterCount + 1;
        _effectCounts[generatorSlot]++;
        _openEffectSlot = _effectCount;
        return _effectCount;
    }

    private static void ValidateEffectInstanceSelection(int effectTypeCode, int instanceSelectionCode)
    {
        if (effectTypeCode == GeneratorEffectTypeCodes.RemoveTargetRelationships ||
            effectTypeCode == GeneratorEffectTypeCodes.AdjustTargetRelationshipStrength)
        {
            ValidateRelationshipInstanceSelection(instanceSelectionCode);
            return;
        }

        NumericGuard.ThrowIfNegative(instanceSelectionCode);
    }

    private static void ValidatePlaneCode(int planeCode)
    {
        if (planeCode is not PlaneCodes.Physical and not PlaneCodes.Semantic)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }
    }

    private static void ValidateRelationshipInstanceSelection(int instanceSelectionCode)
    {
        if (instanceSelectionCode is not (GeneratorRelationshipInstanceSelectionCodes.AllTargetRelationships or
            GeneratorRelationshipInstanceSelectionCodes.TriggerRelationship))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(instanceSelectionCode));
        }
    }

    public int AddProduced(
        int generatorSlot,
        int refCode,
        int kindId,
        int subtypeId,
        int createIfMissing,
        int hasQuery,
        int queryAdjacentToTarget,
        int queryCultureMatchCode,
        int physicalPlacementCode,
        int semanticPlacementCode,
        int placementRefCode,
        int cultureRefCode,
        FixedScalar createChance,
        int coverageModeCode,
        int coverageSourceRefCode,
        int coverageDesiredCellCount,
        int coverageMinimumCellCount,
        int coverageMinimumSourceRemainingCellCount,
        int coverageRespectTerrainBoundaries,
        int coverageTransformTerrainId,
        int physicalPlacementMinRadius = GeneratorProgramConstants.DefaultPlacementMinRadius,
        int physicalPlacementMaxRadius = GeneratorProgramConstants.DefaultPlacementMaxRadius,
        int semanticPlacementMinRadius = GeneratorProgramConstants.DefaultPlacementMinRadius,
        int semanticPlacementMaxRadius = GeneratorProgramConstants.DefaultPlacementMaxRadius,
        int culturePlacementCode = GeneratorCulturePlacementCodes.PreferInheritedCulture,
        int queryRequireTargetCultureRoute = 0,
        int fixedCultureId = 0,
        int queryRequiredTagBits = 0,
        int queryWithoutTagBits = 0,
        int queryCultureId = 0,
        int queryRelatedToTargetKindId = 0,
        int queryRelatedToTargetIncoming = 0,
        FixedScalar queryRelatedToTargetMinStrength = default)
    {
        EnsureOpenGenerator(generatorSlot);
        ValidateProducedIdentity(refCode, kindId, subtypeId, queryCultureMatchCode);
        ValidateProducedPlacement(
            physicalPlacementCode,
            semanticPlacementCode,
            placementRefCode,
            culturePlacementCode,
            physicalPlacementMinRadius,
            physicalPlacementMaxRadius,
            semanticPlacementMinRadius,
            semanticPlacementMaxRadius);
        ValidateProducedCultureSource(cultureRefCode, fixedCultureId);
        ValidateProducedCreateChance(createChance);
        if (_producedCount + 1 >= _producedRefCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _producedCount++;
        StoreProducedDefinition(
            _producedCount,
            refCode,
            kindId,
            subtypeId,
            createIfMissing,
            hasQuery,
            queryAdjacentToTarget,
            queryCultureMatchCode,
            physicalPlacementCode,
            semanticPlacementCode,
            placementRefCode,
            cultureRefCode,
            fixedCultureId,
            createChance,
            coverageModeCode,
            coverageSourceRefCode,
            coverageDesiredCellCount,
            coverageMinimumCellCount,
            coverageMinimumSourceRemainingCellCount,
            coverageRespectTerrainBoundaries,
            coverageTransformTerrainId,
            physicalPlacementMinRadius,
            physicalPlacementMaxRadius,
            semanticPlacementMinRadius,
            semanticPlacementMaxRadius,
            culturePlacementCode,
            queryRequireTargetCultureRoute);
        StoreProducedQueryFilters(
            _producedCount,
            queryRequiredTagBits,
            queryWithoutTagBits,
            queryCultureId,
            queryRelatedToTargetKindId,
            queryRelatedToTargetIncoming,
            queryRelatedToTargetMinStrength);
        _producedCounts[generatorSlot]++;
        _openProducedSlot = _producedCount;
        return _producedCount;
    }

    private void StoreProducedQueryFilters(
        int producedSlot,
        int queryRequiredTagBits,
        int queryWithoutTagBits,
        int queryCultureId,
        int queryRelatedToTargetKindId,
        int queryRelatedToTargetIncoming,
        FixedScalar queryRelatedToTargetMinStrength)
    {
        _producedQueryRequiredTagBits[producedSlot] = queryRequiredTagBits;
        _producedQueryWithoutTagBits[producedSlot] = queryWithoutTagBits;
        _producedQueryCultureIds[producedSlot] = queryCultureId;
        _producedQueryRelatedToTargetKindIds[producedSlot] = queryRelatedToTargetKindId;
        _producedQueryRelatedToTargetIncoming[producedSlot] = queryRelatedToTargetIncoming;
        _producedQueryRelatedToTargetMinStrengths[producedSlot] = queryRelatedToTargetMinStrength;
    }

    private static void ValidateProducedIdentity(int refCode, int kindId, int subtypeId, int queryCultureMatchCode)
    {
        ValidatePositive(refCode);
        ValidatePositive(kindId);
        NumericGuard.ThrowIfNegative(subtypeId);
        ValidatePositive(queryCultureMatchCode);
    }

    private static void ValidateProducedPlacement(
        int physicalPlacementCode,
        int semanticPlacementCode,
        int placementRefCode,
        int culturePlacementCode,
        int physicalPlacementMinRadius,
        int physicalPlacementMaxRadius,
        int semanticPlacementMinRadius,
        int semanticPlacementMaxRadius)
    {
        ValidatePositive(physicalPlacementCode);
        ValidatePositive(semanticPlacementCode);
        ValidatePositive(placementRefCode);
        ValidatePositive(culturePlacementCode);
        ValidateProducedPlacementRadii(
            physicalPlacementMinRadius,
            physicalPlacementMaxRadius,
            semanticPlacementMinRadius,
            semanticPlacementMaxRadius);
    }

    private static void ValidateProducedCreateChance(FixedScalar createChance)
    {
        if (createChance < 0 || createChance > 1)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidateProducedCultureSource(int cultureRefCode, int fixedCultureId)
    {
        if (cultureRefCode <= 0 && fixedCultureId <= 0)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidateProducedPlacementRadii(
        int physicalPlacementMinRadius,
        int physicalPlacementMaxRadius,
        int semanticPlacementMinRadius,
        int semanticPlacementMaxRadius)
    {
        NumericGuard.ThrowIfNegative(physicalPlacementMinRadius);
        NumericGuard.ThrowIfNegative(physicalPlacementMaxRadius);
        NumericGuard.ThrowIfNegative(semanticPlacementMinRadius);
        NumericGuard.ThrowIfNegative(semanticPlacementMaxRadius);
        if (physicalPlacementMaxRadius < physicalPlacementMinRadius ||
            semanticPlacementMaxRadius < semanticPlacementMinRadius)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void StoreProducedDefinition(
        int producedSlot,
        int refCode,
        int kindId,
        int subtypeId,
        int createIfMissing,
        int hasQuery,
        int queryAdjacentToTarget,
        int queryCultureMatchCode,
        int physicalPlacementCode,
        int semanticPlacementCode,
        int placementRefCode,
        int cultureRefCode,
        int fixedCultureId,
        FixedScalar createChance,
        int coverageModeCode,
        int coverageSourceRefCode,
        int coverageDesiredCellCount,
        int coverageMinimumCellCount,
        int coverageMinimumSourceRemainingCellCount,
        int coverageRespectTerrainBoundaries,
        int coverageTransformTerrainId,
        int physicalPlacementMinRadius,
        int physicalPlacementMaxRadius,
        int semanticPlacementMinRadius,
        int semanticPlacementMaxRadius,
        int culturePlacementCode,
        int queryRequireTargetCultureRoute)
    {
        _producedRefCodes[producedSlot] = refCode;
        _producedKindIds[producedSlot] = kindId;
        _producedSubtypeIds[producedSlot] = subtypeId;
        _producedCreateIfMissing[producedSlot] = createIfMissing;
        _producedHasQuery[producedSlot] = hasQuery;
        _producedPhysicalPlacementCodes[producedSlot] = physicalPlacementCode;
        _producedSemanticPlacementCodes[producedSlot] = semanticPlacementCode;
        _producedPhysicalPlacementMinRadii[producedSlot] = physicalPlacementMinRadius;
        _producedPhysicalPlacementMaxRadii[producedSlot] = physicalPlacementMaxRadius;
        _producedSemanticPlacementMinRadii[producedSlot] = semanticPlacementMinRadius;
        _producedSemanticPlacementMaxRadii[producedSlot] = semanticPlacementMaxRadius;
        _producedPlacementRefCodes[producedSlot] = placementRefCode;
        _producedCultureRefCodes[producedSlot] = cultureRefCode;
        _producedFixedCultureIds[producedSlot] = fixedCultureId;
        _producedCulturePlacementCodes[producedSlot] = culturePlacementCode;
        _producedCreateChances[producedSlot] = createChance;
        _producedQueryAdjacentToTarget[producedSlot] = queryAdjacentToTarget;
        _producedQueryCultureMatchCodes[producedSlot] = queryCultureMatchCode;
        _producedQueryRequireTargetCultureRoute[producedSlot] = queryRequireTargetCultureRoute;
        _producedCoverageModeCodes[producedSlot] = coverageModeCode;
        _producedCoverageSourceRefCodes[producedSlot] = coverageSourceRefCode;
        _producedCoverageDesiredCellCounts[producedSlot] = coverageDesiredCellCount;
        _producedCoverageMinimumCellCounts[producedSlot] = coverageMinimumCellCount;
        _producedCoverageMinimumSourceRemainingCellCounts[producedSlot] = coverageMinimumSourceRemainingCellCount;
        _producedCoverageRespectTerrainBoundaries[producedSlot] = coverageRespectTerrainBoundaries;
        _producedCoverageTransformTerrainIds[producedSlot] = coverageTransformTerrainId;
        _producedSubtypeChoiceStarts[producedSlot] = _subtypeCount + 1;
        _producedQuerySubtypeStarts[producedSlot] = _subtypeCount + 1;
        _producedQueryWithoutOutgoingStarts[producedSlot] = _relationshipKindFilterCount + 1;
        _producedQueryWithoutIncomingStarts[producedSlot] = _relationshipKindFilterCount + 1;
        InitializeProducedPlacementTerrainFilters(producedSlot);
        _producedCoveragePreferredTerrainStarts[producedSlot] = _terrainFilterCount + 1;
        _producedCoverageAllowedTerrainStarts[producedSlot] = _terrainFilterCount + 1;
    }
}
