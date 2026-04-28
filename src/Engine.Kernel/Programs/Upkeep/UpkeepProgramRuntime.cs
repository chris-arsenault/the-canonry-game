namespace Engine.Kernel;

internal sealed class UpkeepProgramRuntime : IKernelProgramRuntime<UpkeepProgramData, UpkeepProgramState>
{
    private int[] _upkeepKindIds => Data.UpkeepKindIds;
    private int[] _upkeepAttributeIds => Data.UpkeepAttributeIds;
    private int[] _locationResourceIds => Data.LocationResourceIds;
    private int[] _criticalTargetAttributeIds => Data.CriticalTargetAttributeIds;
    private float[] _driftRates => Data.DriftRates;
    private float[] _locationResourceDriftPerUnit => Data.LocationResourceDriftPerUnit;
    private float[] _locationResourceDriftMinOffset => Data.LocationResourceDriftMinOffset;
    private float[] _locationResourceDriftMaxOffset => Data.LocationResourceDriftMaxOffset;
    private float[] _criticalThresholds => Data.CriticalThresholds;
    private float[] _criticalDeltas => Data.CriticalDeltas;
    private int[] _lifecycleKindIds => Data.LifecycleKindIds;
    private int[] _lifecycleAttributeIds => Data.LifecycleAttributeIds;
    private float[] _lifecycleBaseRates => Data.LifecycleBaseRates;
    private float[] _lifecycleAgeCoefficients => Data.LifecycleAgeCoefficients;
    private float[] _lifecycleVariances => Data.LifecycleVariances;
    private ref int _upkeepRuleCount => ref Data.UpkeepRuleCount;
    private ref int _lifecycleRuleCount => ref Data.LifecycleRuleCount;

    public UpkeepProgramRuntime(UpkeepProgramData data, UpkeepProgramState state)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        Data = data;
        State = state;
    }

    public UpkeepProgramData Data { get; }

    public UpkeepProgramState State { get; }

    public int UpkeepRuleCount => _upkeepRuleCount;

    public int LifecycleRuleCount => _lifecycleRuleCount;

    public int RegisterAttributeUpkeep(
        int kindId,
        int attributeId,
        float driftRate = 0f,
        int locationResourceId = 0,
        float locationResourceDriftPerUnit = 0f,
        float locationResourceDriftMinOffset = 0f,
        float locationResourceDriftMaxOffset = 0f,
        int criticalTargetAttributeId = 0,
        float criticalThreshold = 15f,
        float criticalDelta = 0f)
    {
        ValidatePositive(kindId);
        ValidatePositive(attributeId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(locationResourceId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(criticalTargetAttributeId);
        if (_upkeepRuleCount + 1 >= _upkeepKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _upkeepRuleCount++;
        var slot = _upkeepRuleCount;
        _upkeepKindIds[slot] = kindId;
        _upkeepAttributeIds[slot] = attributeId;
        _driftRates[slot] = driftRate;
        _locationResourceIds[slot] = locationResourceId;
        _locationResourceDriftPerUnit[slot] = locationResourceDriftPerUnit;
        _locationResourceDriftMinOffset[slot] = locationResourceDriftMinOffset;
        _locationResourceDriftMaxOffset[slot] = locationResourceDriftMaxOffset;
        _criticalTargetAttributeIds[slot] = criticalTargetAttributeId;
        _criticalThresholds[slot] = criticalThreshold;
        _criticalDeltas[slot] = criticalDelta;
        return slot;
    }

    public int RegisterLifecycle(
        int kindId,
        int healthAttributeId,
        float baseRate,
        float ageCoefficient,
        float variance = 0.2f)
    {
        ValidatePositive(kindId);
        ValidatePositive(healthAttributeId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(variance);
        if (_lifecycleRuleCount + 1 >= _lifecycleKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _lifecycleRuleCount++;
        var slot = _lifecycleRuleCount;
        _lifecycleKindIds[slot] = kindId;
        _lifecycleAttributeIds[slot] = healthAttributeId;
        _lifecycleBaseRates[slot] = baseRate;
        _lifecycleAgeCoefficients[slot] = ageCoefficient;
        _lifecycleVariances[slot] = variance;
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

            TickEntity(simulation, entityId, tick);
        }
    }

    private void TickEntity(Simulation simulation, int entityId, int tick)
    {
        var world = simulation.World;
        var kindId = world.EntityKindId(entityId);
        ApplyCriticalEffectsForKind(simulation, entityId, tick, kindId);
        ApplyDriftRulesForKind(simulation, entityId, tick, kindId);
        var healthAttributeId = ApplyLifecycleDrainForKind(simulation, entityId, tick, kindId);
        if (healthAttributeId > 0 && world.GetAttribute(entityId, healthAttributeId) <= 0f)
        {
            simulation.SetEntityStatus(entityId, EntityStatusCodes.Historical, tick);
        }
    }

    private void ApplyCriticalEffectsForKind(Simulation simulation, int entityId, int tick, int kindId)
    {
        for (var rule = 1; rule <= _upkeepRuleCount; rule++)
        {
            if (_upkeepKindIds[rule] == kindId && _criticalTargetAttributeIds[rule] > 0)
            {
                ApplyCriticalEffect(simulation, entityId, tick, rule);
            }
        }
    }

    private void ApplyDriftRulesForKind(Simulation simulation, int entityId, int tick, int kindId)
    {
        var world = simulation.World;
        for (var rule = 1; rule <= _upkeepRuleCount; rule++)
        {
            if (_upkeepKindIds[rule] != kindId)
            {
                continue;
            }

            var delta = _driftRates[rule];
            delta += LocationResourceOffset(world, entityId, rule);
            if (delta != 0f)
            {
                simulation.ModifyAttribute(entityId, _upkeepAttributeIds[rule], delta, tick);
            }
        }
    }

    private int ApplyLifecycleDrainForKind(Simulation simulation, int entityId, int tick, int kindId)
    {
        var healthAttributeId = 0;
        var entityData = simulation.KernelData.Entities;
        for (var rule = 1; rule <= entityData.AttributeCount; rule++)
        {
            if (entityData.AttributeKindId(rule) != kindId)
            {
                continue;
            }

            var attributeId = entityData.AttributeId(rule);
            var delta = -LifecycleDrain(simulation, entityId, kindId, attributeId, tick);
            if (delta != 0f)
            {
                simulation.ModifyAttribute(entityId, attributeId, delta, tick);
            }

            if (IsLifecycleAttribute(kindId, attributeId))
            {
                healthAttributeId = attributeId;
            }
        }

        return healthAttributeId;
    }

    private void ApplyCriticalEffect(Simulation simulation, int entityId, int tick, int rule)
    {
        var world = simulation.World;
        if (world.GetAttribute(entityId, _upkeepAttributeIds[rule]) > _criticalThresholds[rule])
        {
            return;
        }

        var delta = _criticalDeltas[rule];
        if (delta != 0f)
        {
            simulation.ModifyAttribute(
                entityId,
                _criticalTargetAttributeIds[rule],
                delta,
                tick);
        }
    }

    private float LocationResourceOffset(WorldState world, int entityId, int rule)
    {
        var resourceId = _locationResourceIds[rule];
        if (resourceId <= 0)
        {
            return 0f;
        }

        var value = world.GetResource(
            resourceId,
            world.EntityPhysicalQ(entityId),
            world.EntityPhysicalR(entityId));
        var offset = value * _locationResourceDriftPerUnit[rule];
        if (offset < _locationResourceDriftMinOffset[rule])
        {
            return _locationResourceDriftMinOffset[rule];
        }

        if (offset > _locationResourceDriftMaxOffset[rule])
        {
            return _locationResourceDriftMaxOffset[rule];
        }

        return offset;
    }

    private float LifecycleDrain(Simulation simulation, int entityId, int kindId, int attributeId, int tick)
    {
        for (var rule = 1; rule <= _lifecycleRuleCount; rule++)
        {
            if (_lifecycleKindIds[rule] != kindId || _lifecycleAttributeIds[rule] != attributeId)
            {
                continue;
            }

            var age = tick - simulation.World.EntityCreatedAt(entityId);
            var drain = _lifecycleBaseRates[rule] + (age * _lifecycleAgeCoefficients[rule]);
            var variance = _lifecycleVariances[rule];
            if (variance > 0f)
            {
                var factor = 1f + (((simulation.NextFloat() * 2f) - 1f) * variance);
                if (factor < 0f)
                {
                    factor = 0f;
                }

                drain *= factor;
            }

            return drain;
        }

        return 0f;
    }

    private bool IsLifecycleAttribute(int kindId, int attributeId)
    {
        for (var rule = 1; rule <= _lifecycleRuleCount; rule++)
        {
            if (_lifecycleKindIds[rule] == kindId && _lifecycleAttributeIds[rule] == attributeId)
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
