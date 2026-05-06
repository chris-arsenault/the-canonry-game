namespace Engine.Kernel;

internal sealed partial class GeneratorProgramRuntime
{
    public void AddRelationship(
        int generatorSlot,
        int sourceRefCode,
        int destinationRefCode,
        int relationshipKindId,
        float strength,
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

    public int AddEffect(
        int generatorSlot,
        int effectTypeCode,
        int refOrKindId,
        int instanceSelectionCode = GeneratorRelationshipInstanceSelectionCodes.AllTargetRelationships,
        int optionCode = 0,
        float value = 0f,
        int radius = 0,
        int maxCellCount = 0,
        int planeCode = PlaneCodes.Physical)
    {
        EnsureOpenGenerator(generatorSlot);
        ValidatePositive(effectTypeCode);
        ValidatePositive(refOrKindId);
        ValidateEffectInstanceSelection(effectTypeCode, instanceSelectionCode);
        ValidatePlaneCode(planeCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(optionCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(radius);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(maxCellCount);
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

        global::System.ArgumentOutOfRangeException.ThrowIfNegative(instanceSelectionCode);
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
}
