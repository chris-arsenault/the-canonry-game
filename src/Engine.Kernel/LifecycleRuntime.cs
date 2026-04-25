namespace Engine.Kernel;

public sealed class LifecycleRuntime : ISimulationSystem
{
    private readonly int[] _kindIds;
    private readonly int[] _attributeIds;
    private readonly float[] _perTickDelta;
    private readonly float[] _deathThreshold;
    private int _ruleCount;

    public LifecycleRuntime(int capacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _kindIds = new int[capacity];
        _attributeIds = new int[capacity];
        _perTickDelta = new float[capacity];
        _deathThreshold = new float[capacity];
    }

    public int Count => _ruleCount;

    public int RegisterRule(int kindId, int attributeId, float perTickDelta, float deathThreshold)
    {
        ValidatePositive(kindId);
        ValidatePositive(attributeId);
        if (_ruleCount >= _kindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var slot = _ruleCount;
        _kindIds[slot] = kindId;
        _attributeIds[slot] = attributeId;
        _perTickDelta[slot] = perTickDelta;
        _deathThreshold[slot] = deathThreshold;
        _ruleCount++;
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

            var kindId = world.EntityKindId(entityId);
            for (var rule = 0; rule < _ruleCount; rule++)
            {
                if (_kindIds[rule] != kindId)
                {
                    continue;
                }

                if (_perTickDelta[rule] != 0f)
                {
                    world.ModifyAttribute(
                        entityId,
                        _attributeIds[rule],
                        simulation.ProcessSourceCodeId,
                        _perTickDelta[rule],
                        tick);
                }

                if (world.GetAttribute(entityId, _attributeIds[rule]) <= _deathThreshold[rule])
                {
                    world.SetEntityStatus(entityId, EntityStatusCodes.Historical, tick);
                    break;
                }
            }
        }
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
