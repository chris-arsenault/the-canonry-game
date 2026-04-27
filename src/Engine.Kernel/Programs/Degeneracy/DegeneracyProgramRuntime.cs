namespace Engine.Kernel;

using Engine.Eventing;

public sealed class DegeneracyProgramRuntime : IKernelProgramRuntime<DegeneracyProgramData, DegeneracyProgramState>
{
    private int[] _detectorIds => Data.DetectorIds;
    private int[] _metricCodes => Data.MetricCodes;
    private int[] _thresholdDirectionCodes => Data.ThresholdDirectionCodes;
    private int[] _noSubjectsSeverityCodes => Data.NoSubjectsSeverityCodes;
    private int[] _reasonCodeIds => Data.ReasonCodeIds;
    private int[] _primaryKindIds => Data.PrimaryKindIds;
    private int[] _secondaryKindIds => Data.SecondaryKindIds;
    private int[] _attributeIds => Data.AttributeIds;
    private int[] _relationshipKindIds => Data.RelationshipKindIds;
    private int[] _dominantRelationshipEndpointCodes => Data.DominantRelationshipEndpointCodes;
    private float[] _warningThresholds => Data.WarningThresholds;
    private float[] _criticalThresholds => Data.CriticalThresholds;
    private int[] _previousSeverityCodes => State.PreviousSeverityCodes;
    private bool[] _enabled => State.Enabled;
    private PrimitiveFloatTable _previousAttributeValues => State.PreviousAttributeValues;
    private ref int _detectorCount => ref Data.DetectorCount;

    public DegeneracyProgramRuntime(DegeneracyProgramData data, DegeneracyProgramState state)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        Data = data;
        State = state;
    }

    public DegeneracyProgramData Data { get; }

    public DegeneracyProgramState State { get; }

    public int Count => _detectorCount;

    public int RegisterDetector(
        int detectorId,
        int metricCode,
        int thresholdDirectionCode,
        int noSubjectsSeverityCode,
        int reasonCodeId,
        int primaryKindId,
        int secondaryKindId,
        int attributeId,
        int relationshipKindId,
        int dominantRelationshipEndpointCode,
        float warningThreshold,
        float criticalThreshold)
    {
        ValidatePositive(detectorId);
        ValidatePositive(metricCode);
        ValidateThresholdDirectionCode(thresholdDirectionCode);
        ValidateSeverityCode(noSubjectsSeverityCode);
        ValidatePositive(reasonCodeId);
        ValidatePositive(primaryKindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(secondaryKindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(attributeId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(relationshipKindId);
        ValidateRelationshipEndpointCode(dominantRelationshipEndpointCode);
        if (_detectorCount + 1 >= _detectorIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _detectorCount++;
        var slot = _detectorCount;
        _detectorIds[slot] = detectorId;
        _metricCodes[slot] = metricCode;
        _thresholdDirectionCodes[slot] = thresholdDirectionCode;
        _noSubjectsSeverityCodes[slot] = noSubjectsSeverityCode;
        _reasonCodeIds[slot] = reasonCodeId;
        _primaryKindIds[slot] = primaryKindId;
        _secondaryKindIds[slot] = secondaryKindId;
        _attributeIds[slot] = attributeId;
        _relationshipKindIds[slot] = relationshipKindId;
        _dominantRelationshipEndpointCodes[slot] = dominantRelationshipEndpointCode;
        _warningThresholds[slot] = warningThreshold;
        _criticalThresholds[slot] = criticalThreshold;
        _previousSeverityCodes[slot] = DegeneracySeverityCodes.Normal;
        _enabled[slot] = true;
        return slot;
    }

    public void DisableDetector(int detectorSlot)
    {
        EnsureDetector(detectorSlot);
        _enabled[detectorSlot] = false;
    }

    public void EnableDetector(int detectorSlot)
    {
        EnsureDetector(detectorSlot);
        _enabled[detectorSlot] = true;
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        for (var slot = 1; slot <= _detectorCount; slot++)
        {
            if (!_enabled[slot])
            {
                continue;
            }

            var severity = Evaluate(simulation.World, slot);
            if (severity != _previousSeverityCodes[slot])
            {
                WriteDegeneracyDetected(simulation, tick, slot, severity);
            }

            _previousSeverityCodes[slot] = severity;
        }
    }

    private int Evaluate(WorldState world, int slot)
    {
        return _metricCodes[slot] switch
        {
            DegeneracyMetricCodes.AttributeDeclineRatio => EvaluateAttributeDeclineRatio(world, slot),
            DegeneracyMetricCodes.RelationshipEndpointDominanceRatio =>
                EvaluateRelationshipEndpointDominanceRatio(world, slot),
            DegeneracyMetricCodes.ActiveKindCount => EvaluateActiveKindCount(world, slot),
            _ => throw new global::System.InvalidOperationException(),
        };
    }

    private int EvaluateAttributeDeclineRatio(WorldState world, int slot)
    {
        var activeCount = 0;
        var decliningCount = 0;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!world.EntityIsActive(entityId) || world.EntityKindId(entityId) != _primaryKindIds[slot])
            {
                continue;
            }

            activeCount++;
            var current = world.GetAttribute(entityId, _attributeIds[slot]);
            if (_previousAttributeValues.TryGet(slot, entityId, out var previous) && current < previous)
            {
                decliningCount++;
            }

            _previousAttributeValues.Set(slot, entityId, current);
        }

        if (activeCount == 0)
        {
            return _noSubjectsSeverityCodes[slot];
        }

        var ratio = (float)decliningCount / activeCount;
        return SeverityFromValue(ratio, slot);
    }

    private int EvaluateRelationshipEndpointDominanceRatio(WorldState world, int slot)
    {
        var totalControlledCount = CountActiveKind(world, _primaryKindIds[slot]);
        if (totalControlledCount == 0)
        {
            return _noSubjectsSeverityCodes[slot];
        }

        var maxControlledByOne = 0;
        for (var controllerId = 1; controllerId <= world.EntityHighWatermark; controllerId++)
        {
            if (!world.EntityIsActive(controllerId) || world.EntityKindId(controllerId) != _secondaryKindIds[slot])
            {
                continue;
            }

            var controlledCount = _dominantRelationshipEndpointCodes[slot] == DegeneracyRelationshipEndpointCodes.Source
                ? CountOutgoingDominatedRelationships(world, slot, controllerId)
                : CountIncomingDominatedRelationships(world, slot, controllerId);

            if (controlledCount > maxControlledByOne)
            {
                maxControlledByOne = controlledCount;
            }
        }

        var ratio = (float)maxControlledByOne / totalControlledCount;
        return SeverityFromValue(ratio, slot);
    }

    private int EvaluateActiveKindCount(WorldState world, int slot)
    {
        var count = CountActiveKind(world, _primaryKindIds[slot]);
        return SeverityFromValue(count, slot);
    }

    private int CountIncomingDominatedRelationships(WorldState world, int slot, int controllerId)
    {
        var count = 0;
        var relationships = world.IncomingRelationshipsOfKind(controllerId, _relationshipKindIds[slot]);
        while (world.MoveNextRelationship(ref relationships, out var relationshipId))
        {
            var sourceId = world.RelationshipSourceId(relationshipId);
            if (world.EntityIsActive(sourceId) && world.EntityKindId(sourceId) == _primaryKindIds[slot])
            {
                count++;
            }
        }

        return count;
    }

    private int CountOutgoingDominatedRelationships(WorldState world, int slot, int controllerId)
    {
        var count = 0;
        var relationships = world.OutgoingRelationshipsOfKind(controllerId, _relationshipKindIds[slot]);
        while (world.MoveNextRelationship(ref relationships, out var relationshipId))
        {
            var targetId = world.RelationshipTargetId(relationshipId);
            if (world.EntityIsActive(targetId) && world.EntityKindId(targetId) == _primaryKindIds[slot])
            {
                count++;
            }
        }

        return count;
    }

    private static int CountActiveKind(WorldState world, int kindId)
    {
        var count = 0;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (world.EntityIsActive(entityId) && world.EntityKindId(entityId) == kindId)
            {
                count++;
            }
        }

        return count;
    }

    private int SeverityFromValue(float value, int slot)
    {
        if (_thresholdDirectionCodes[slot] == DegeneracyThresholdDirectionCodes.LowIsBad)
        {
            if (value <= _criticalThresholds[slot])
            {
                return DegeneracySeverityCodes.Critical;
            }

            return value <= _warningThresholds[slot]
                ? DegeneracySeverityCodes.Warning
                : DegeneracySeverityCodes.Normal;
        }

        if (value >= _criticalThresholds[slot])
        {
            return DegeneracySeverityCodes.Critical;
        }

        return value >= _warningThresholds[slot]
            ? DegeneracySeverityCodes.Warning
            : DegeneracySeverityCodes.Normal;
    }

    private static void WriteDegeneracyDetected(Simulation simulation, int tick, int slot, int severityCode)
    {
        if (!simulation.Events.TryWrite(KernelEvent.From(new DegeneracyDetectedKernelEvent(
            tick,
            simulation.WorldVersion,
            simulation.Degeneracy._detectorIds[slot],
            severityCode,
            simulation.Degeneracy._reasonCodeIds[slot]))))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureDetector(int detectorSlot)
    {
        if ((uint)detectorSlot == 0u || detectorSlot > _detectorCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }

    private static void ValidateThresholdDirectionCode(int value)
    {
        if (value is not (
            DegeneracyThresholdDirectionCodes.HighIsBad or
            DegeneracyThresholdDirectionCodes.LowIsBad))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidateRelationshipEndpointCode(int value)
    {
        if (value is not (
            DegeneracyRelationshipEndpointCodes.Source or
            DegeneracyRelationshipEndpointCodes.Target))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidateSeverityCode(int value)
    {
        if (value is not (
            DegeneracySeverityCodes.Normal or
            DegeneracySeverityCodes.Warning or
            DegeneracySeverityCodes.Critical))
        {
            throw new global::System.InvalidOperationException();
        }
    }
}
