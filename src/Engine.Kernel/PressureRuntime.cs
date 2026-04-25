namespace Engine.Kernel;

public sealed class PressureRuntime : ISimulationSystem
{
    private readonly int[] _pressureIds;
    private readonly int[] _scopeCodes;
    private readonly float[] _baselines;
    private readonly float[] _homeostasisRates;
    private readonly float[] _smoothingFactors;
    private readonly bool[] _enabled;
    private readonly PrimitiveFloatTable _values;
    private readonly PrimitiveFloatTable _pendingDeltas;
    private int _pressureCount;

    public PressureRuntime(int pressureCapacity, int valueCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pressureCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valueCapacity);
        _pressureIds = new int[pressureCapacity + 1];
        _scopeCodes = new int[pressureCapacity + 1];
        _baselines = new float[pressureCapacity + 1];
        _homeostasisRates = new float[pressureCapacity + 1];
        _smoothingFactors = new float[pressureCapacity + 1];
        _enabled = new bool[pressureCapacity + 1];
        _values = new PrimitiveFloatTable(valueCapacity);
        _pendingDeltas = new PrimitiveFloatTable(valueCapacity);
    }

    public int Count => _pressureCount;

    public int RegisterPressure(
        int pressureId,
        int scopeCode,
        float baseline,
        float homeostasisRate,
        float smoothingFactor)
    {
        ValidatePositive(pressureId);
        ValidatePositive(scopeCode);
        if (homeostasisRate < 0f || homeostasisRate > 1f ||
            smoothingFactor < 0f || smoothingFactor > 1f)
        {
            throw new global::System.InvalidOperationException();
        }

        if (_pressureCount + 1 >= _pressureIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _pressureCount++;
        var slot = _pressureCount;
        _pressureIds[slot] = pressureId;
        _scopeCodes[slot] = scopeCode;
        _baselines[slot] = baseline;
        _homeostasisRates[slot] = homeostasisRate;
        _smoothingFactors[slot] = smoothingFactor;
        _enabled[slot] = true;
        return slot;
    }

    public float GetValue(int pressureId, int scopeEntityId)
    {
        ValidatePositive(pressureId);
        ValidatePositive(scopeEntityId);
        return _values.GetOrDefault(pressureId, scopeEntityId);
    }

    public void ApplyDelta(int pressureId, int scopeEntityId, float delta)
    {
        ValidatePositive(pressureId);
        ValidatePositive(scopeEntityId);
        _pendingDeltas.Add(pressureId, scopeEntityId, delta);
    }

    public void DisablePressure(int pressureSlot)
    {
        EnsurePressure(pressureSlot);
        _enabled[pressureSlot] = false;
    }

    public void EnablePressure(int pressureSlot)
    {
        EnsurePressure(pressureSlot);
        _enabled[pressureSlot] = true;
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        _ = tick;
        for (var slot = 1; slot <= _pressureCount; slot++)
        {
            if (!_enabled[slot])
            {
                continue;
            }

            ComputePressure(simulation.World, slot);
        }

        _pendingDeltas.Clear();
    }

    private void ComputePressure(WorldState world, int slot)
    {
        if (_scopeCodes[slot] == PressureScopeCodes.Entity)
        {
            for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
            {
                if (world.EntityIsActive(entityId))
                {
                    ComputeForScope(slot, entityId, rawContribution: 0f);
                }
            }

            return;
        }

        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (world.EntityIsActive(entityId) && world.CoverageCellCount(entityId) > 0)
            {
                ComputeForScope(slot, entityId, rawContribution: 0f);
            }
        }
    }

    private void ComputeForScope(int slot, int scopeEntityId, float rawContribution)
    {
        var pressureId = _pressureIds[slot];
        var baseline = _baselines[slot];
        var previousValue = _values.GetOrDefault(pressureId, scopeEntityId, baseline);
        rawContribution += _pendingDeltas.GetOrDefault(pressureId, scopeEntityId);
        var targetValue = baseline + rawContribution;
        targetValue += (baseline - targetValue) * _homeostasisRates[slot];
        var newValue = previousValue + ((targetValue - previousValue) * _smoothingFactors[slot]);
        _values.Set(pressureId, scopeEntityId, Clamp(newValue, -100f, 100f));
    }

    private void EnsurePressure(int pressureSlot)
    {
        if ((uint)pressureSlot == 0u || pressureSlot > _pressureCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static float Clamp(float value, float minimum, float maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
