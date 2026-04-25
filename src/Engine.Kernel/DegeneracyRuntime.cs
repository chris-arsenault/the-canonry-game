namespace Engine.Kernel;

using Engine.Eventing;

public sealed class DegeneracyRuntime : ISimulationSystem
{
    private readonly int[] _detectorIds;
    private readonly int[] _operationCodes;
    private readonly int[] _reasonCodeIds;
    private readonly int[] _primaryKindIds;
    private readonly int[] _secondaryKindIds;
    private readonly int[] _attributeIds;
    private readonly int[] _relationshipKindIds;
    private readonly float[] _warningThresholds;
    private readonly float[] _criticalThresholds;
    private readonly int[] _previousSeverityCodes;
    private readonly bool[] _enabled;
    private readonly PrimitiveFloatTable _previousAttributeValues;
    private int _detectorCount;

    public DegeneracyRuntime(int detectorCapacity, int stateCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(detectorCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stateCapacity);
        _detectorIds = new int[detectorCapacity + 1];
        _operationCodes = new int[detectorCapacity + 1];
        _reasonCodeIds = new int[detectorCapacity + 1];
        _primaryKindIds = new int[detectorCapacity + 1];
        _secondaryKindIds = new int[detectorCapacity + 1];
        _attributeIds = new int[detectorCapacity + 1];
        _relationshipKindIds = new int[detectorCapacity + 1];
        _warningThresholds = new float[detectorCapacity + 1];
        _criticalThresholds = new float[detectorCapacity + 1];
        _previousSeverityCodes = new int[detectorCapacity + 1];
        _enabled = new bool[detectorCapacity + 1];
        _previousAttributeValues = new PrimitiveFloatTable(stateCapacity);
    }

    public int Count => _detectorCount;

    public int RegisterDetector(
        int detectorId,
        int operationCode,
        int reasonCodeId,
        int primaryKindId,
        int secondaryKindId,
        int attributeId,
        int relationshipKindId,
        float warningThreshold,
        float criticalThreshold)
    {
        ValidatePositive(detectorId);
        ValidatePositive(operationCode);
        ValidatePositive(reasonCodeId);
        ValidatePositive(primaryKindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(secondaryKindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(attributeId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(relationshipKindId);
        if (_detectorCount + 1 >= _detectorIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _detectorCount++;
        var slot = _detectorCount;
        _detectorIds[slot] = detectorId;
        _operationCodes[slot] = operationCode;
        _reasonCodeIds[slot] = reasonCodeId;
        _primaryKindIds[slot] = primaryKindId;
        _secondaryKindIds[slot] = secondaryKindId;
        _attributeIds[slot] = attributeId;
        _relationshipKindIds[slot] = relationshipKindId;
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
        return _operationCodes[slot] switch
        {
            DegeneracyOperationCodes.DecliningAttributeRatio => EvaluateDecliningAttributeRatio(world, slot),
            DegeneracyOperationCodes.RelationshipDominanceRatio => EvaluateRelationshipDominanceRatio(world, slot),
            DegeneracyOperationCodes.ActiveKindCountBelow => EvaluateActiveKindCountBelow(world, slot),
            _ => throw new global::System.InvalidOperationException(),
        };
    }

    private int EvaluateDecliningAttributeRatio(WorldState world, int slot)
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
            if (_previousAttributeValues.TryGet(entityId, _attributeIds[slot], out var previous) && current < previous)
            {
                decliningCount++;
            }

            _previousAttributeValues.Set(entityId, _attributeIds[slot], current);
        }

        if (activeCount == 0)
        {
            return DegeneracySeverityCodes.Critical;
        }

        var ratio = (float)decliningCount / activeCount;
        return SeverityFromHighRatio(ratio, _warningThresholds[slot], _criticalThresholds[slot]);
    }

    private int EvaluateRelationshipDominanceRatio(WorldState world, int slot)
    {
        var totalControlledCount = CountActiveKind(world, _primaryKindIds[slot]);
        if (totalControlledCount == 0)
        {
            return DegeneracySeverityCodes.Normal;
        }

        var maxControlledByOne = 0;
        for (var controllerId = 1; controllerId <= world.EntityHighWatermark; controllerId++)
        {
            if (!world.EntityIsActive(controllerId) || world.EntityKindId(controllerId) != _secondaryKindIds[slot])
            {
                continue;
            }

            var controlledCount = 0;
            var relationships = world.IncomingRelationshipsOfKind(controllerId, _relationshipKindIds[slot]);
            while (world.MoveNextRelationship(ref relationships, out var relationshipId))
            {
                var sourceId = world.RelationshipSourceId(relationshipId);
                if (world.EntityIsActive(sourceId) && world.EntityKindId(sourceId) == _primaryKindIds[slot])
                {
                    controlledCount++;
                }
            }

            if (controlledCount > maxControlledByOne)
            {
                maxControlledByOne = controlledCount;
            }
        }

        var ratio = (float)maxControlledByOne / totalControlledCount;
        return SeverityFromHighRatio(ratio, _warningThresholds[slot], _criticalThresholds[slot]);
    }

    private int EvaluateActiveKindCountBelow(WorldState world, int slot)
    {
        var count = CountActiveKind(world, _primaryKindIds[slot]);
        if (count <= _criticalThresholds[slot])
        {
            return DegeneracySeverityCodes.Critical;
        }

        return count <= _warningThresholds[slot]
            ? DegeneracySeverityCodes.Warning
            : DegeneracySeverityCodes.Normal;
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

    private static int SeverityFromHighRatio(float ratio, float warningThreshold, float criticalThreshold)
    {
        if (ratio >= criticalThreshold)
        {
            return DegeneracySeverityCodes.Critical;
        }

        return ratio >= warningThreshold
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
}
