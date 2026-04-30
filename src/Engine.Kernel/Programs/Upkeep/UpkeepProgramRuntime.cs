namespace Engine.Kernel;

internal sealed class UpkeepProgramRuntime : IKernelProgramRuntime<UpkeepProgramData, UpkeepProgramState>
{
    private int[] _lifecycleKindIds => Data.LifecycleKindIds;
    private int[] _lifecycleAttributeIds => Data.LifecycleAttributeIds;
    private float[] _lifecycleBaseRates => Data.LifecycleBaseRates;
    private float[] _lifecycleAgeCoefficients => Data.LifecycleAgeCoefficients;
    private float[] _lifecycleVariances => Data.LifecycleVariances;
    private ref int _lifecycleRuleCount => ref Data.LifecycleRuleCount;

    public UpkeepProgramRuntime(UpkeepProgramData data, UpkeepProgramState state)
        : this(data, state, new KernelProgramTuningOptions())
    {
    }

    public UpkeepProgramRuntime(
        UpkeepProgramData data,
        UpkeepProgramState state,
        KernelProgramTuningOptions tuning)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        Data = data;
        State = state;
        Tuning = tuning;
    }

    public UpkeepProgramData Data { get; }

    public UpkeepProgramState State { get; }

    public KernelProgramTuningOptions Tuning { get; }

    public int LifecycleRuleCount => _lifecycleRuleCount;

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
        var healthAttributeId = ApplyLifecycleDrainForKind(simulation, entityId, tick, kindId);
        if (healthAttributeId > 0 && world.GetAttribute(entityId, healthAttributeId) <= 0f)
        {
            simulation.SetEntityStatus(entityId, EntityStatusCodes.Historical, tick);
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

    private float LifecycleDrain(Simulation simulation, int entityId, int kindId, int attributeId, int tick)
    {
        for (var rule = 1; rule <= _lifecycleRuleCount; rule++)
        {
            if (_lifecycleKindIds[rule] != kindId || _lifecycleAttributeIds[rule] != attributeId)
            {
                continue;
            }

            var age = tick - simulation.World.EntityCreatedAt(entityId);
            var drain = LifecycleBaseDrain(rule, age);
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

    private float LifecycleBaseDrain(int rule, int age)
    {
        if (Tuning.Upkeep.AgingModelCode != UpkeepAgingModelCodes.LateLife)
        {
            return _lifecycleBaseRates[rule] + (age * _lifecycleAgeCoefficients[rule]);
        }

        var scaledAge = (age * age) / 500f;
        return (_lifecycleBaseRates[rule] * 0.5f) + (scaledAge * _lifecycleAgeCoefficients[rule]);
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
