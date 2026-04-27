namespace Engine.Kernel;

public sealed partial class KernelEntityData
{
    private readonly int[] _kindIds;
    private readonly int[] _attributeIds;
    private readonly float[] _minimums;
    private readonly float[] _maximums;
    private readonly float[] _defaultValues;
    private readonly float[] _defaultVariances;
    private readonly float[] _attributeMinimums;
    private readonly float[] _attributeMaximums;
    private readonly bool[] _attributeBoundsRegistered;
    private int _attributeCount;

    public KernelEntityData(int attributeDefinitionCapacity, int attributeIdCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attributeDefinitionCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attributeIdCapacity);
        _kindIds = new int[attributeDefinitionCapacity + 1];
        _attributeIds = new int[attributeDefinitionCapacity + 1];
        _minimums = new float[attributeDefinitionCapacity + 1];
        _maximums = new float[attributeDefinitionCapacity + 1];
        _defaultValues = new float[attributeDefinitionCapacity + 1];
        _defaultVariances = new float[attributeDefinitionCapacity + 1];
        _attributeMinimums = new float[attributeIdCapacity + 1];
        _attributeMaximums = new float[attributeIdCapacity + 1];
        _attributeBoundsRegistered = new bool[attributeIdCapacity + 1];
    }

    public int AttributeCount => _attributeCount;

    public int RegisterAttribute(
        int kindId,
        int attributeId,
        float minimum,
        float maximum,
        float defaultValue,
        float defaultVariance = 0f)
    {
        ValidatePositive(kindId);
        ValidatePositive(attributeId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(defaultVariance);
        if (maximum < minimum || _attributeCount + 1 >= _kindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        RegisterAttributeBounds(attributeId, minimum, maximum);
        _attributeCount++;
        var slot = _attributeCount;
        _kindIds[slot] = kindId;
        _attributeIds[slot] = attributeId;
        _minimums[slot] = minimum;
        _maximums[slot] = maximum;
        _defaultValues[slot] = defaultValue;
        _defaultVariances[slot] = defaultVariance;
        return slot;
    }

    public int AttributeKindId(int slot)
    {
        EnsureAttributeSlot(slot);
        return _kindIds[slot];
    }

    public int AttributeId(int slot)
    {
        EnsureAttributeSlot(slot);
        return _attributeIds[slot];
    }

    public float ClampAttribute(int attributeId, float value)
    {
        if ((uint)attributeId < (uint)_attributeBoundsRegistered.Length && _attributeBoundsRegistered[attributeId])
        {
            if (value < _attributeMinimums[attributeId])
            {
                return _attributeMinimums[attributeId];
            }

            if (value > _attributeMaximums[attributeId])
            {
                return _attributeMaximums[attributeId];
            }
        }

        return value;
    }

    public void InitializeEntity(Simulation simulation, int entityId, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var world = simulation.World;
        var kindId = world.EntityKindId(entityId);
        for (var slot = 1; slot <= _attributeCount; slot++)
        {
            if (_kindIds[slot] != kindId)
            {
                continue;
            }

            var value = _defaultValues[slot];
            var variance = _defaultVariances[slot];
            if (variance > 0f)
            {
                value *= 1f + (((simulation.NextFloat() * 2f) - 1f) * variance);
            }

            simulation.SetAttribute(entityId, _attributeIds[slot], value, tick);
        }
    }

    private void RegisterAttributeBounds(int attributeId, float minimum, float maximum)
    {
        if ((uint)attributeId >= (uint)_attributeMinimums.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _attributeMinimums[attributeId] = minimum;
        _attributeMaximums[attributeId] = maximum;
        _attributeBoundsRegistered[attributeId] = true;
    }

    private void EnsureAttributeSlot(int slot)
    {
        if ((uint)slot == 0u || slot > _attributeCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
