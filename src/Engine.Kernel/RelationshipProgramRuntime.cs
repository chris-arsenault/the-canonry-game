namespace Engine.Kernel;

public sealed class RelationshipProgramRuntime : ISimulationSystem
{
    private readonly int[] _kindIds;
    private readonly float[] _cullThresholds;
    private int _ruleCount;

    public RelationshipProgramRuntime(int relationshipRuleCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(relationshipRuleCapacity);
        _kindIds = new int[relationshipRuleCapacity + 1];
        _cullThresholds = new float[relationshipRuleCapacity + 1];
        ProximityMaxDistance = 10;
        ProximityMaxDelta = 0.005f;
        SharedCultureMatchDelta = 0.003f;
        SharedCultureMismatchDelta = -0.001f;
        NeglectDelta = -0.001f;
    }

    public int Count => _ruleCount;

    public int ProximityMaxDistance { get; set; }

    public float ProximityMaxDelta { get; set; }

    public float SharedCultureMatchDelta { get; set; }

    public float SharedCultureMismatchDelta { get; set; }

    public float NeglectDelta { get; set; }

    public int RegisterRelationshipKind(int kindId, float cullThreshold)
    {
        ValidatePositive(kindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(cullThreshold);
        if (_ruleCount + 1 >= _kindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _ruleCount++;
        var slot = _ruleCount;
        _kindIds[slot] = kindId;
        _cullThresholds[slot] = cullThreshold;
        return slot;
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var world = simulation.World;
        for (var relationshipId = 1; relationshipId <= world.RelationshipHighWatermark; relationshipId++)
        {
            if (!world.RelationshipIsActive(relationshipId))
            {
                continue;
            }

            TickRelationship(world, relationshipId, tick);
        }
    }

    private void TickRelationship(WorldState world, int relationshipId, int tick)
    {
        var sourceId = world.RelationshipSourceId(relationshipId);
        var targetId = world.RelationshipTargetId(relationshipId);
        var delta = NeglectDelta;
        delta += ComputeProximityDelta(world, sourceId, targetId);
        delta += world.EntityCultureId(sourceId) == world.EntityCultureId(targetId)
            ? SharedCultureMatchDelta
            : SharedCultureMismatchDelta;

        if (delta != 0f)
        {
            world.SetRelationshipStrength(relationshipId, world.RelationshipStrength(relationshipId) + delta, tick);
        }

        if (world.RelationshipStrength(relationshipId) <= CullThreshold(world.RelationshipKindId(relationshipId)))
        {
            world.CullRelationship(relationshipId, tick);
        }
    }

    private float ComputeProximityDelta(WorldState world, int sourceId, int targetId)
    {
        if (ProximityMaxDistance <= 0)
        {
            return 0f;
        }

        var distance = HexDistance(
            world.EntityPhysicalQ(sourceId),
            world.EntityPhysicalR(sourceId),
            world.EntityPhysicalQ(targetId),
            world.EntityPhysicalR(targetId));
        if (distance >= ProximityMaxDistance)
        {
            return 0f;
        }

        var factor = 1f - (distance / (float)ProximityMaxDistance);
        return ProximityMaxDelta * factor;
    }

    private float CullThreshold(int kindId)
    {
        for (var rule = 1; rule <= _ruleCount; rule++)
        {
            if (_kindIds[rule] == kindId)
            {
                return _cullThresholds[rule];
            }
        }

        return 0.05f;
    }

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var aS = -aq - ar;
        var bS = -bq - br;
        return (Abs(aq - bq) + Abs(ar - br) + Abs(aS - bS)) / 2;
    }

    private static int Abs(int value)
    {
        return value < 0 ? -value : value;
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
