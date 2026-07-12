namespace Engine.Kernel;

internal sealed class EffectsProgramRuntime : IKernelProgramRuntime<EffectsProgramData, EffectsProgramState>
{
    private int[] _attributeDeltaKindIds => Data.AttributeDeltaKindIds;
    private int[] _attributeDeltaAttributeIds => Data.AttributeDeltaAttributeIds;
    private int[] _attributeDeltaConditionCodes => Data.AttributeDeltaConditionCodes;
    private int[] _attributeDeltaRelationshipKindIds => Data.AttributeDeltaRelationshipKindIds;
    private int[] _attributeDeltaRelationshipDirectionCodes => Data.AttributeDeltaRelationshipDirectionCodes;
    private int[] _attributeDeltaPlaneCodes => Data.AttributeDeltaPlaneCodes;
    private int[] _attributeDeltaMaxDistances => Data.AttributeDeltaMaxDistances;
    private float[] _attributeDeltaThresholds => Data.AttributeDeltaThresholds;
    private float[] _attributeDeltaMinimumRelationshipStrengths => Data.AttributeDeltaMinimumRelationshipStrengths;
    private float[] _attributeDeltaDeltas => Data.AttributeDeltaDeltas;
    private int[] _attributeDeltaRequiredTargetTraitIds => Data.AttributeDeltaRequiredTargetTraitIds;
    private int[] _attributeDeltaRequiredRelatedTraitIds => Data.AttributeDeltaRequiredRelatedTraitIds;
    private int[] _attributeDeltaCultureIds => Data.AttributeDeltaCultureIds;
    private int[] _attributeDeltaCultureSlotIds => Data.AttributeDeltaCultureSlotIds;
    private int[] _attributeDeltaCultureSlotPolarityCodes => Data.AttributeDeltaCultureSlotPolarityCodes;
    private ref int _attributeDeltaRuleCount => ref Data.AttributeDeltaRuleCount;
    private int[] _tagRuleKindIds => Data.TagRuleKindIds;
    private int[] _tagRuleTagIds => Data.TagRuleTagIds;
    private int[] _tagRuleRelationshipKindIds => Data.TagRuleRelationshipKindIds;
    private int[] _tagRuleDirectionCodes => Data.TagRuleDirectionCodes;
    private float[] _tagRuleSetThresholds => Data.TagRuleSetThresholds;
    private float[] _tagRuleClearThresholds => Data.TagRuleClearThresholds;
    private ref int _tagRuleCount => ref Data.TagRuleCount;

    public EffectsProgramRuntime(EffectsProgramData data, EffectsProgramState state)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        Data = data;
        State = state;
    }

    public EffectsProgramData Data { get; }

    public EffectsProgramState State { get; }

    public int AttributeDeltaRuleCount => _attributeDeltaRuleCount;

    public int RegisterAttributeDeltaEffect(
        int kindId,
        int attributeId,
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
        int cultureSlotPolarityCode = -1)
    {
        ValidatePositive(kindId);
        ValidatePositive(attributeId);
        ValidatePositive(conditionCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(relationshipKindId);
        ValidatePositive(relationshipDirectionCode);
        ValidatePositive(planeCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(maxDistance);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(threshold);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(minimumRelationshipStrength);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(requiredTargetTraitId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(requiredRelatedTraitId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(cultureId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(cultureSlotId);
        global::System.ArgumentOutOfRangeException.ThrowIfLessThan(cultureSlotPolarityCode, -1);
        if (_attributeDeltaRuleCount + 1 >= _attributeDeltaKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _attributeDeltaRuleCount++;
        var slot = _attributeDeltaRuleCount;
        _attributeDeltaKindIds[slot] = kindId;
        _attributeDeltaAttributeIds[slot] = attributeId;
        _attributeDeltaConditionCodes[slot] = conditionCode;
        _attributeDeltaRelationshipKindIds[slot] = relationshipKindId;
        _attributeDeltaRelationshipDirectionCodes[slot] = relationshipDirectionCode;
        _attributeDeltaPlaneCodes[slot] = planeCode;
        _attributeDeltaMaxDistances[slot] = maxDistance;
        _attributeDeltaThresholds[slot] = threshold;
        _attributeDeltaMinimumRelationshipStrengths[slot] = minimumRelationshipStrength;
        _attributeDeltaDeltas[slot] = deltaPerTick;
        _attributeDeltaRequiredTargetTraitIds[slot] = requiredTargetTraitId;
        _attributeDeltaRequiredRelatedTraitIds[slot] = requiredRelatedTraitId;
        _attributeDeltaCultureIds[slot] = cultureId;
        _attributeDeltaCultureSlotIds[slot] = cultureSlotId;
        _attributeDeltaCultureSlotPolarityCodes[slot] = cultureSlotPolarityCode;
        return slot;
    }

    public int RegisterRelationshipTagRule(
        int kindId,
        int tagId,
        int relationshipKindId,
        int relationshipDirectionCode,
        float setAtOrAbove,
        float clearBelow)
    {
        ValidatePositive(kindId);
        ValidatePositive(tagId);
        ValidatePositive(relationshipKindId);
        ValidatePositive(relationshipDirectionCode);
        if (clearBelow > setAtOrAbove || _tagRuleCount + 1 >= _tagRuleKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _tagRuleCount++;
        var slot = _tagRuleCount;
        _tagRuleKindIds[slot] = kindId;
        _tagRuleTagIds[slot] = tagId;
        _tagRuleRelationshipKindIds[slot] = relationshipKindId;
        _tagRuleDirectionCodes[slot] = relationshipDirectionCode;
        _tagRuleSetThresholds[slot] = setAtOrAbove;
        _tagRuleClearThresholds[slot] = clearBelow;
        return slot;
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var world = simulation.World;
        KernelQuery.ForEachActiveEntity(
            world,
            entityId =>
            {
                var kindId = world.EntityKindId(entityId);
                ApplyAttributeDeltaEffectsForKind(simulation, entityId, tick, kindId);
                ApplyTagRulesForKind(simulation, entityId, tick, kindId);
                return true;
            });
    }

    private void ApplyTagRulesForKind(Simulation simulation, int entityId, int tick, int kindId)
    {
        for (var rule = 1; rule <= _tagRuleCount; rule++)
        {
            if (_tagRuleKindIds[rule] != kindId)
            {
                continue;
            }

            var sum = SummedRelationshipStrength(
                simulation.World,
                entityId,
                _tagRuleRelationshipKindIds[rule],
                _tagRuleDirectionCodes[rule]);
            if (sum >= _tagRuleSetThresholds[rule])
            {
                _ = simulation.SetEntityTag(entityId, _tagRuleTagIds[rule], assigned: true, tick);
            }
            else if (sum < _tagRuleClearThresholds[rule])
            {
                _ = simulation.SetEntityTag(entityId, _tagRuleTagIds[rule], assigned: false, tick);
            }
        }
    }

    private static float SummedRelationshipStrength(
        WorldState world,
        int entityId,
        int relationshipKindId,
        int directionCode)
    {
        var sum = 0f;
        if (directionCode is RelationshipMatchDirectionCodes.Outgoing or RelationshipMatchDirectionCodes.Either)
        {
            KernelQuery.ForEachRelatedEndpoint(
                world,
                entityId,
                relationshipKindId,
                minimumStrength: 0f,
                outgoing: true,
                (relationshipId, _) =>
                {
                    sum += world.RelationshipStrength(relationshipId);
                    return true;
                });
        }

        if (directionCode is RelationshipMatchDirectionCodes.Incoming or RelationshipMatchDirectionCodes.Either)
        {
            KernelQuery.ForEachRelatedEndpoint(
                world,
                entityId,
                relationshipKindId,
                minimumStrength: 0f,
                outgoing: false,
                (relationshipId, _) =>
                {
                    sum += world.RelationshipStrength(relationshipId);
                    return true;
                });
        }

        return sum;
    }

    private void ApplyAttributeDeltaEffectsForKind(Simulation simulation, int entityId, int tick, int kindId)
    {
        for (var rule = 1; rule <= _attributeDeltaRuleCount; rule++)
        {
            if (_attributeDeltaKindIds[rule] != kindId)
            {
                continue;
            }

            if (!KernelQuery.TraitFilterPasses(simulation.World, entityId, _attributeDeltaRequiredTargetTraitIds[rule]))
            {
                continue;
            }

            var evidence = EffectEvidence(simulation, entityId, rule);
            if (evidence <= 0f)
            {
                continue;
            }

            KernelEffectExecutor.ModifyAttribute(
                simulation,
                entityId,
                _attributeDeltaAttributeIds[rule],
                _attributeDeltaDeltas[rule] * evidence);
        }
    }

    private float EffectEvidence(Simulation simulation, int entityId, int rule)
    {
        var conditionCode = _attributeDeltaConditionCodes[rule];
        if (conditionCode == EffectConditionCodes.RelationshipMapValueAtLocation)
        {
            return RelatedInfluenceEvidence(simulation.World, entityId, rule);
        }

        if (conditionCode == EffectConditionCodes.RelatedEntityDistance)
        {
            return RelatedDistanceEvidence(simulation.World, entityId, rule);
        }

        if (conditionCode == EffectConditionCodes.LocalPositiveSignal)
        {
            return KernelQuery.ThresholdEvidence(
                simulation.MapSignals.EntitySupport(entityId, _attributeDeltaPlaneCodes[rule]),
                _attributeDeltaThresholds[rule]);
        }

        if (conditionCode == EffectConditionCodes.LocalNegativeSignal)
        {
            return KernelQuery.ThresholdEvidence(
                simulation.MapSignals.EntityThreat(entityId, _attributeDeltaPlaneCodes[rule]),
                _attributeDeltaThresholds[rule]);
        }

        if (conditionCode == EffectConditionCodes.LocalCrowdingLoadSignal)
        {
            return KernelQuery.ThresholdEvidence(
                simulation.MapSignals.EntityCrowdingLoad(entityId, _attributeDeltaPlaneCodes[rule]),
                _attributeDeltaThresholds[rule]);
        }

        if (conditionCode == EffectConditionCodes.LocalComfortSignal)
        {
            return KernelQuery.ThresholdEvidence(
                simulation.MapSignals.EntityCrowdingComfort(entityId, _attributeDeltaPlaneCodes[rule]),
                _attributeDeltaThresholds[rule]);
        }

        if (conditionCode == EffectConditionCodes.LocalCultureMapValue)
        {
            return OwnCultureInfluenceEvidence(simulation.World, entityId, rule);
        }

        return conditionCode == EffectConditionCodes.CultureSlotOccupancy
            ? CultureSlotOccupancyEvidence(simulation, entityId, rule)
            : 0f;
    }

    private float OwnCultureInfluenceEvidence(WorldState world, int entityId, int rule)
    {
        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return 0f;
        }

        var planeCode = _attributeDeltaPlaneCodes[rule];
        var q = KernelQuery.CurrentQ(world, entityId, planeCode);
        var r = KernelQuery.CurrentR(world, entityId, planeCode);
        return KernelQuery.ThresholdEvidence(
            world.GetCellCultureInfluence(cultureId, planeCode, q, r),
            _attributeDeltaThresholds[rule]);
    }

    private float CultureSlotOccupancyEvidence(Simulation simulation, int entityId, int rule)
    {
        return simulation.CultureSlots.OccupancyEvidence(
            entityId,
            _attributeDeltaCultureIds[rule],
            _attributeDeltaCultureSlotIds[rule],
            _attributeDeltaCultureSlotPolarityCodes[rule],
            _attributeDeltaThresholds[rule]);
    }

    private float RelatedInfluenceEvidence(WorldState world, int entityId, int rule)
    {
        var direction = _attributeDeltaRelationshipDirectionCodes[rule];
        var total = 0f;
        if (direction is RelationshipMatchDirectionCodes.Outgoing or RelationshipMatchDirectionCodes.Either)
        {
            total += RelatedInfluenceEvidenceForDirection(world, entityId, rule, outgoing: true);
        }

        if (total >= 1f)
        {
            return 1f;
        }

        if (direction is RelationshipMatchDirectionCodes.Incoming or RelationshipMatchDirectionCodes.Either)
        {
            total += RelatedInfluenceEvidenceForDirection(world, entityId, rule, outgoing: false);
        }

        return KernelQuery.Clamp01(total);
    }

    private float RelatedInfluenceEvidenceForDirection(
        WorldState world,
        int entityId,
        int rule,
        bool outgoing)
    {
        var planeCode = _attributeDeltaPlaneCodes[rule];
        var q = KernelQuery.CurrentQ(world, entityId, planeCode);
        var r = KernelQuery.CurrentR(world, entityId, planeCode);
        var total = 0f;
        KernelQuery.ForEachRelatedEndpoint(
            world,
            entityId,
            _attributeDeltaRelationshipKindIds[rule],
            _attributeDeltaMinimumRelationshipStrengths[rule],
            outgoing,
            (relationshipId, otherEntityId) =>
            {
                if (!world.EntityIsActive(otherEntityId))
                {
                    return true;
                }

                if (!KernelQuery.TraitFilterPasses(world, otherEntityId, _attributeDeltaRequiredRelatedTraitIds[rule]))
                {
                    return true;
                }

                var otherCultureId = world.EntityCultureId(otherEntityId);
                var influence = otherCultureId <= 0
                    ? 0f
                    : world.GetCellCultureInfluenceRaw(otherCultureId, planeCode, q, r);
                if (influence <= 0f)
                {
                    return true;
                }

                total += KernelQuery.ThresholdEvidence(influence, _attributeDeltaThresholds[rule]) *
                    KernelQuery.RelationshipStrength(world, relationshipId);
                return total < 1f;
            });

        return total;
    }

    private float RelatedDistanceEvidence(WorldState world, int entityId, int rule)
    {
        var direction = _attributeDeltaRelationshipDirectionCodes[rule];
        var total = 0f;
        if (direction is RelationshipMatchDirectionCodes.Outgoing or RelationshipMatchDirectionCodes.Either)
        {
            total += RelatedDistanceEvidenceForDirection(world, entityId, rule, outgoing: true);
        }

        if (total >= 1f)
        {
            return 1f;
        }

        if (direction is RelationshipMatchDirectionCodes.Incoming or RelationshipMatchDirectionCodes.Either)
        {
            total += RelatedDistanceEvidenceForDirection(world, entityId, rule, outgoing: false);
        }

        return KernelQuery.Clamp01(total);
    }

    private float RelatedDistanceEvidenceForDirection(
        WorldState world,
        int entityId,
        int rule,
        bool outgoing)
    {
        var total = 0f;
        KernelQuery.ForEachRelatedEndpoint(
            world,
            entityId,
            _attributeDeltaRelationshipKindIds[rule],
            _attributeDeltaMinimumRelationshipStrengths[rule],
            outgoing,
            (relationshipId, otherEntityId) =>
            {
                if (!world.EntityIsActive(otherEntityId))
                {
                    return true;
                }

                if (!KernelQuery.TraitFilterPasses(world, otherEntityId, _attributeDeltaRequiredRelatedTraitIds[rule]))
                {
                    return true;
                }

                var distance = KernelQuery.PlaneDistance(world, entityId, otherEntityId, _attributeDeltaPlaneCodes[rule]);
                var evidence = KernelQuery.DistanceEvidence(distance, _attributeDeltaMaxDistances[rule]) *
                    KernelQuery.RelationshipStrength(world, relationshipId);
                total += evidence;
                return total < 1f;
            });

        return total;
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
