namespace Engine.Kernel;

internal sealed class PressureProgramRuntime : IKernelProgramRuntime<PressureProgramData, PressureProgramState>
{
    private int[] _pressureIds => Data.PressureIds;
    private int[] _scopeCodes => Data.ScopeCodes;
    private float[] _baselines => Data.Baselines;
    private float[] _homeostasisRates => Data.HomeostasisRates;
    private float[] _smoothingFactors => Data.SmoothingFactors;
    private bool[] _enabled => State.Enabled;
    private PrimitiveFloatTable _values => State.Values;
    private PrimitiveFloatTable _pendingDeltas => State.PendingDeltas;
    private ref int _pressureCount => ref Data.PressureCount;

    public PressureProgramRuntime(PressureProgramData data, PressureProgramState state)
        : this(data, state, new KernelProgramTuningOptions())
    {
    }

    public PressureProgramRuntime(
        PressureProgramData data,
        PressureProgramState state,
        KernelProgramTuningOptions tuning)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        Data = data;
        State = state;
        Tuning = tuning;
    }

    public PressureProgramData Data { get; }

    public PressureProgramState State { get; }

    public KernelProgramTuningOptions Tuning { get; }

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
        if (!Tuning.Pressures.Enabled)
        {
            return 0f;
        }

        return _values.GetOrDefault(pressureId, scopeEntityId);
    }

    public void ApplyDelta(int pressureId, int scopeEntityId, float delta)
    {
        ValidatePositive(pressureId);
        ValidatePositive(scopeEntityId);
        if (!Tuning.Pressures.Enabled)
        {
            return;
        }

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
        if (!Tuning.Pressures.Enabled)
        {
            _pendingDeltas.Clear();
            return;
        }

        for (var slot = 1; slot <= _pressureCount; slot++)
        {
            if (!_enabled[slot])
            {
                continue;
            }

            ComputePressure(simulation, tick, slot);
        }

        _pendingDeltas.Clear();
    }

    private void ComputePressure(Simulation simulation, int tick, int slot)
    {
        var world = simulation.World;
        if (_scopeCodes[slot] == PressureScopeCodes.Entity)
        {
            for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
            {
                if (world.EntityIsActive(entityId))
                {
                    ComputeForScope(simulation, tick, slot, entityId, rawContribution: 0f);
                }
            }

            return;
        }

        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (world.EntityIsActive(entityId) && world.CoverageOwnerHasCells(entityId))
            {
                ComputeForScope(simulation, tick, slot, entityId, rawContribution: 0f);
            }
        }
    }

    private void ComputeForScope(Simulation simulation, int tick, int slot, int scopeEntityId, float rawContribution)
    {
        var pressureId = _pressureIds[slot];
        var baseline = _baselines[slot];
        var previousValue = _values.GetOrDefault(pressureId, scopeEntityId, baseline);
        rawContribution += _pendingDeltas.GetOrDefault(pressureId, scopeEntityId);
        var targetValue = baseline + rawContribution;
        targetValue += (baseline - targetValue) * _homeostasisRates[slot];
        var newValue = previousValue + ((targetValue - previousValue) * _smoothingFactors[slot]);
        var clamped = Clamp(newValue, -100f, 100f);
        _values.Set(pressureId, scopeEntityId, clamped);
        simulation.EmitPressureChanged(tick, pressureId, _scopeCodes[slot], scopeEntityId, previousValue, clamped);
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
