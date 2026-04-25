namespace Engine.Kernel;

public sealed class AttributeRuntime : ISimulationSystem
{
    private readonly int[] _kindIds;
    private readonly int[] _attributeIds;
    private readonly int[] _locationResourceIds;
    private readonly int[] _criticalTargetAttributeIds;
    private readonly float[] _minimums;
    private readonly float[] _maximums;
    private readonly float[] _defaultValues;
    private readonly float[] _defaultVariances;
    private readonly float[] _driftRates;
    private readonly float[] _locationResourceDriftPerUnit;
    private readonly float[] _locationResourceDriftMinOffset;
    private readonly float[] _locationResourceDriftMaxOffset;
    private readonly float[] _criticalThresholds;
    private readonly float[] _criticalDeltas;
    private readonly int[] _lifecycleKindIds;
    private readonly int[] _lifecycleAttributeIds;
    private readonly float[] _lifecycleBaseRates;
    private readonly float[] _lifecycleAgeCoefficients;
    private readonly float[] _lifecycleVariances;
    private int _attributeRuleCount;
    private int _lifecycleRuleCount;

    public AttributeRuntime(int attributeRuleCapacity, int lifecycleRuleCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attributeRuleCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lifecycleRuleCapacity);
        _kindIds = new int[attributeRuleCapacity + 1];
        _attributeIds = new int[attributeRuleCapacity + 1];
        _locationResourceIds = new int[attributeRuleCapacity + 1];
        _criticalTargetAttributeIds = new int[attributeRuleCapacity + 1];
        _minimums = new float[attributeRuleCapacity + 1];
        _maximums = new float[attributeRuleCapacity + 1];
        _defaultValues = new float[attributeRuleCapacity + 1];
        _defaultVariances = new float[attributeRuleCapacity + 1];
        _driftRates = new float[attributeRuleCapacity + 1];
        _locationResourceDriftPerUnit = new float[attributeRuleCapacity + 1];
        _locationResourceDriftMinOffset = new float[attributeRuleCapacity + 1];
        _locationResourceDriftMaxOffset = new float[attributeRuleCapacity + 1];
        _criticalThresholds = new float[attributeRuleCapacity + 1];
        _criticalDeltas = new float[attributeRuleCapacity + 1];
        _lifecycleKindIds = new int[lifecycleRuleCapacity + 1];
        _lifecycleAttributeIds = new int[lifecycleRuleCapacity + 1];
        _lifecycleBaseRates = new float[lifecycleRuleCapacity + 1];
        _lifecycleAgeCoefficients = new float[lifecycleRuleCapacity + 1];
        _lifecycleVariances = new float[lifecycleRuleCapacity + 1];
    }

    public int AttributeRuleCount => _attributeRuleCount;

    public int LifecycleRuleCount => _lifecycleRuleCount;

    public int RegisterAttribute(
        WorldState world,
        int kindId,
        int attributeId,
        float minimum,
        float maximum,
        float defaultValue,
        float defaultVariance = 0f,
        float driftRate = 0f,
        int locationResourceId = 0,
        float locationResourceDriftPerUnit = 0f,
        float locationResourceDriftMinOffset = 0f,
        float locationResourceDriftMaxOffset = 0f,
        int criticalTargetAttributeId = 0,
        float criticalThreshold = 15f,
        float criticalDelta = 0f)
    {
        global::System.ArgumentNullException.ThrowIfNull(world);
        ValidatePositive(kindId);
        ValidatePositive(attributeId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(defaultVariance);
        if (maximum < minimum || _attributeRuleCount + 1 >= _kindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _attributeRuleCount++;
        var slot = _attributeRuleCount;
        _kindIds[slot] = kindId;
        _attributeIds[slot] = attributeId;
        _minimums[slot] = minimum;
        _maximums[slot] = maximum;
        _defaultValues[slot] = defaultValue;
        _defaultVariances[slot] = defaultVariance;
        _driftRates[slot] = driftRate;
        _locationResourceIds[slot] = locationResourceId;
        _locationResourceDriftPerUnit[slot] = locationResourceDriftPerUnit;
        _locationResourceDriftMinOffset[slot] = locationResourceDriftMinOffset;
        _locationResourceDriftMaxOffset[slot] = locationResourceDriftMaxOffset;
        _criticalTargetAttributeIds[slot] = criticalTargetAttributeId;
        _criticalThresholds[slot] = criticalThreshold;
        _criticalDeltas[slot] = criticalDelta;
        world.RegisterAttributeBounds(attributeId, minimum, maximum);
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

    public void InitializeEntity(Simulation simulation, int entityId, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var world = simulation.World;
        var kindId = world.EntityKindId(entityId);
        for (var rule = 1; rule <= _attributeRuleCount; rule++)
        {
            if (_kindIds[rule] != kindId)
            {
                continue;
            }

            var value = _defaultValues[rule];
            var variance = _defaultVariances[rule];
            if (variance > 0f)
            {
                value *= 1f + (((simulation.NextFloat() * 2f) - 1f) * variance);
            }

            world.SetAttribute(entityId, _attributeIds[rule], simulation.ProcessSourceCodeId, value, tick);
        }
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
        for (var rule = 1; rule <= _attributeRuleCount; rule++)
        {
            if (_kindIds[rule] == kindId && _criticalTargetAttributeIds[rule] > 0)
            {
                ApplyCriticalEffect(simulation, entityId, tick, rule);
            }
        }

        var healthAttributeId = 0;
        for (var rule = 1; rule <= _attributeRuleCount; rule++)
        {
            if (_kindIds[rule] != kindId)
            {
                continue;
            }

            var delta = _driftRates[rule];
            delta += LocationResourceOffset(world, entityId, rule);
            delta -= LifecycleDrain(simulation, entityId, kindId, _attributeIds[rule], tick);
            if (delta != 0f)
            {
                world.ModifyAttribute(entityId, _attributeIds[rule], simulation.ProcessSourceCodeId, delta, tick);
            }

            if (IsLifecycleAttribute(kindId, _attributeIds[rule]))
            {
                healthAttributeId = _attributeIds[rule];
            }
        }

        if (healthAttributeId > 0 && world.GetAttribute(entityId, healthAttributeId) <= 0f)
        {
            world.SetEntityStatus(entityId, EntityStatusCodes.Historical, tick);
        }
    }

    private void ApplyCriticalEffect(Simulation simulation, int entityId, int tick, int rule)
    {
        var world = simulation.World;
        if (world.GetAttribute(entityId, _attributeIds[rule]) > _criticalThresholds[rule])
        {
            return;
        }

        var delta = _criticalDeltas[rule];
        if (delta != 0f)
        {
            world.ModifyAttribute(
                entityId,
                _criticalTargetAttributeIds[rule],
                simulation.ProcessSourceCodeId,
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
