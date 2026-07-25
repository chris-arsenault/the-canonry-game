namespace Engine.Kernel;

internal sealed partial class KernelEntityData
{
    private readonly int[] _kindIds;
    private readonly int[] _attributeIds;
    private readonly FixedScalar[] _minimums;
    private readonly FixedScalar[] _maximums;
    private readonly FixedScalar[] _defaultValues;
    private readonly FixedScalar[] _defaultVariances;
    private readonly int[] _traitKindIds;
    private readonly int[] _traitIds;
    private readonly FixedScalar[] _traitAssignmentChances;
    private readonly FixedScalar[] _attributeMinimums;
    private readonly FixedScalar[] _attributeMaximums;
    private readonly bool[] _attributeBoundsRegistered;
    private int _attributeCount;
    private int _traitCount;

    public KernelEntityData(int attributeDefinitionCapacity, int attributeIdCapacity, int traitDefinitionCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(attributeDefinitionCapacity);
        NumericGuard.ThrowIfNegativeOrZero(attributeIdCapacity);
        NumericGuard.ThrowIfNegativeOrZero(traitDefinitionCapacity);
        _kindIds = new int[attributeDefinitionCapacity + 1];
        _attributeIds = new int[attributeDefinitionCapacity + 1];
        _minimums = new FixedScalar[attributeDefinitionCapacity + 1];
        _maximums = new FixedScalar[attributeDefinitionCapacity + 1];
        _defaultValues = new FixedScalar[attributeDefinitionCapacity + 1];
        _defaultVariances = new FixedScalar[attributeDefinitionCapacity + 1];
        _traitKindIds = new int[traitDefinitionCapacity + 1];
        _traitIds = new int[traitDefinitionCapacity + 1];
        _traitAssignmentChances = new FixedScalar[traitDefinitionCapacity + 1];
        _attributeMinimums = new FixedScalar[attributeIdCapacity + 1];
        _attributeMaximums = new FixedScalar[attributeIdCapacity + 1];
        _attributeBoundsRegistered = new bool[attributeIdCapacity + 1];
    }

    public int AttributeCount => _attributeCount;
    public int TraitCount => _traitCount;

    public int RegisterAttribute(
        int kindId,
        int attributeId,
        FixedScalar minimum,
        FixedScalar maximum,
        FixedScalar defaultValue,
        FixedScalar defaultVariance = default)
    {
        ValidatePositive(kindId);
        ValidatePositive(attributeId);
        NumericGuard.ThrowIfNegative(defaultVariance);
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

    public int RegisterTrait(int kindId, int traitId, FixedScalar assignmentChance)
    {
        ValidatePositive(kindId);
        ValidatePositive(traitId);
        if (!FixedScalar.IsFinite(assignmentChance) ||
            assignmentChance < 0 ||
            assignmentChance > 1 ||
            _traitCount + 1 >= _traitKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _traitCount++;
        var slot = _traitCount;
        _traitKindIds[slot] = kindId;
        _traitIds[slot] = traitId;
        _traitAssignmentChances[slot] = assignmentChance;
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

    public FixedScalar ClampAttribute(int attributeId, FixedScalar value)
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

    public FixedScalar AttributeMaximum(int attributeId)
    {
        ValidatePositive(attributeId);
        return (uint)attributeId < (uint)_attributeBoundsRegistered.Length && _attributeBoundsRegistered[attributeId]
            ? _attributeMaximums[attributeId]
            : 0;
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
            if (variance > 0)
            {
                value *= 1 + (((simulation.NextScalar() * 2) - 1) * variance);
            }

            simulation.SetAttribute(entityId, _attributeIds[slot], value, tick);
        }

        for (var slot = 1; slot <= _traitCount; slot++)
        {
            if (_traitKindIds[slot] != kindId)
            {
                continue;
            }

            var chance = _traitAssignmentChances[slot];
            if (chance >= 1 || simulation.NextScalar() < chance)
            {
                world.AddTrait(entityId, _traitIds[slot], tick);
            }
        }
    }

    private void RegisterAttributeBounds(int attributeId, FixedScalar minimum, FixedScalar maximum)
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
        NumericGuard.ThrowIfNegativeOrZero(value);
    }
}
