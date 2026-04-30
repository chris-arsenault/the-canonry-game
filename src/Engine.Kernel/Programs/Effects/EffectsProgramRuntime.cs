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
    private ref int _attributeDeltaRuleCount => ref Data.AttributeDeltaRuleCount;

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
        float deltaPerTick)
    {
        ValidatePositive(kindId);
        ValidatePositive(attributeId);
        ValidatePositive(conditionCode);
        ValidatePositive(relationshipKindId);
        ValidatePositive(relationshipDirectionCode);
        ValidatePositive(planeCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(maxDistance);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(threshold);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(minimumRelationshipStrength);
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
        return slot;
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var world = simulation.World;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!world.EntityIsActive(entityId))
            {
                continue;
            }

            ApplyAttributeDeltaEffectsForKind(simulation, entityId, tick, world.EntityKindId(entityId));
        }
    }

    private void ApplyAttributeDeltaEffectsForKind(Simulation simulation, int entityId, int tick, int kindId)
    {
        for (var rule = 1; rule <= _attributeDeltaRuleCount; rule++)
        {
            if (_attributeDeltaKindIds[rule] != kindId)
            {
                continue;
            }

            var evidence = EffectEvidence(simulation.World, entityId, rule);
            if (evidence <= 0f)
            {
                continue;
            }

            simulation.ModifyAttribute(
                entityId,
                _attributeDeltaAttributeIds[rule],
                _attributeDeltaDeltas[rule] * evidence,
                tick);
        }
    }

    private float EffectEvidence(WorldState world, int entityId, int rule)
    {
        var conditionCode = _attributeDeltaConditionCodes[rule];
        if (conditionCode == EffectConditionCodes.RelatedInfluenceAtPhysicalLocation)
        {
            return RelatedInfluenceEvidence(world, entityId, rule);
        }

        return conditionCode == EffectConditionCodes.RelatedEntityDistance
            ? RelatedDistanceEvidence(world, entityId, rule)
            : 0f;
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

        return Clamp01(total);
    }

    private float RelatedInfluenceEvidenceForDirection(
        WorldState world,
        int entityId,
        int rule,
        bool outgoing)
    {
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKindAbove(
                entityId,
                _attributeDeltaRelationshipKindIds[rule],
                _attributeDeltaMinimumRelationshipStrengths[rule])
            : world.IncomingRelationshipsOfKindAbove(
                entityId,
                _attributeDeltaRelationshipKindIds[rule],
                _attributeDeltaMinimumRelationshipStrengths[rule]);
        var q = world.EntityPhysicalQ(entityId);
        var r = world.EntityPhysicalR(entityId);
        var total = 0f;
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var otherEntityId = outgoing
                ? world.RelationshipTargetId(relationshipId)
                : world.RelationshipSourceId(relationshipId);
            if (!world.EntityIsActive(otherEntityId))
            {
                continue;
            }

            var influence = world.GetCellInfluence(otherEntityId, q, r);
            if (influence <= 0f)
            {
                continue;
            }

            total += ThresholdEvidence(influence, _attributeDeltaThresholds[rule]) *
                world.RelationshipStrength(relationshipId);
            if (total >= 1f)
            {
                return 1f;
            }
        }

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

        return Clamp01(total);
    }

    private float RelatedDistanceEvidenceForDirection(
        WorldState world,
        int entityId,
        int rule,
        bool outgoing)
    {
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKindAbove(
                entityId,
                _attributeDeltaRelationshipKindIds[rule],
                _attributeDeltaMinimumRelationshipStrengths[rule])
            : world.IncomingRelationshipsOfKindAbove(
                entityId,
                _attributeDeltaRelationshipKindIds[rule],
                _attributeDeltaMinimumRelationshipStrengths[rule]);
        var total = 0f;
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var otherEntityId = outgoing
                ? world.RelationshipTargetId(relationshipId)
                : world.RelationshipSourceId(relationshipId);
            if (!world.EntityIsActive(otherEntityId))
            {
                continue;
            }

            var distance = Distance(world, entityId, otherEntityId, _attributeDeltaPlaneCodes[rule]);
            var evidence = DistanceEvidence(distance, _attributeDeltaMaxDistances[rule]) *
                world.RelationshipStrength(relationshipId);
            total += evidence;
            if (total >= 1f)
            {
                return 1f;
            }
        }

        return total;
    }

    private static float ThresholdEvidence(float value, float threshold)
    {
        if (threshold <= 0f)
        {
            return value > 0f ? 1f : 0f;
        }

        return Clamp01(value / threshold);
    }

    private static float DistanceEvidence(int distance, int maxDistance)
    {
        if (maxDistance <= 0)
        {
            return distance == 0 ? 1f : 0f;
        }

        if (distance > maxDistance)
        {
            return 0f;
        }

        return 1f - (distance / (float)(maxDistance + 1));
    }

    private static int Distance(WorldState world, int sourceId, int targetId, int planeCode) =>
        planeCode == PlaneCodes.Semantic
            ? SemanticDistance(world, sourceId, targetId)
            : PhysicalDistance(world, sourceId, targetId);

    private static int PhysicalDistance(WorldState world, int sourceId, int targetId) =>
        HexDistance(
            world.EntityPhysicalQ(sourceId),
            world.EntityPhysicalR(sourceId),
            world.EntityPhysicalQ(targetId),
            world.EntityPhysicalR(targetId));

    private static int SemanticDistance(WorldState world, int sourceId, int targetId)
    {
        var q = Abs(world.EntitySemanticQ(sourceId) - world.EntitySemanticQ(targetId));
        var r = Abs(world.EntitySemanticR(sourceId) - world.EntitySemanticR(targetId));
        var s = Abs(world.EntitySemanticS(sourceId) - world.EntitySemanticS(targetId));
        return Max(q, Max(r, s));
    }

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var aS = -aq - ar;
        var bS = -bq - br;
        return (Abs(aq - bq) + Abs(ar - br) + Abs(aS - bS)) / 2;
    }

    private static int Abs(int value)
    {
        return value < 0 ? -value : value;
    }

    private static int Max(int left, int right) => left >= right ? left : right;

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
