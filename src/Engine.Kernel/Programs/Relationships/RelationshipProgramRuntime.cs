namespace Engine.Kernel;

internal sealed class RelationshipProgramRuntime :
    IKernelProgramRuntime<RelationshipProgramData, RelationshipProgramState>
{
    public RelationshipProgramRuntime(RelationshipProgramData data, RelationshipProgramState state)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        Data = data;
        State = state;
    }

    public RelationshipProgramData Data { get; }

    public RelationshipProgramState State { get; }

    public int Count => Data.Count;

    public int ProximityMaxDistance
    {
        get => Data.ProximityMaxDistance;
        set => Data.ProximityMaxDistance = value;
    }

    public float ProximityMaxDelta
    {
        get => Data.ProximityMaxDelta;
        set => Data.ProximityMaxDelta = value;
    }

    public float SharedCultureMatchDelta
    {
        get => Data.SharedCultureMatchDelta;
        set => Data.SharedCultureMatchDelta = value;
    }

    public float SharedCultureMismatchDelta
    {
        get => Data.SharedCultureMismatchDelta;
        set => Data.SharedCultureMismatchDelta = value;
    }

    public float NeglectDelta
    {
        get => Data.NeglectDelta;
        set => Data.NeglectDelta = value;
    }

    public int RegisterRelationshipKind(int kindId, float cullThreshold) =>
        Data.RegisterRelationshipKind(kindId, cullThreshold);

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

            TickRelationship(simulation, world, relationshipId, tick);
        }
    }

    private void TickRelationship(Simulation simulation, WorldState world, int relationshipId, int tick)
    {
        var sourceId = world.RelationshipSourceId(relationshipId);
        var targetId = world.RelationshipTargetId(relationshipId);
        var delta = Data.NeglectDelta;
        delta += ComputeProximityDelta(world, sourceId, targetId);
        delta += world.EntityCultureId(sourceId) == world.EntityCultureId(targetId)
            ? Data.SharedCultureMatchDelta
            : Data.SharedCultureMismatchDelta;

        if (delta != 0f)
        {
            simulation.SetRelationshipStrength(relationshipId, world.RelationshipStrength(relationshipId) + delta, tick);
        }

        if (world.RelationshipStrength(relationshipId) <= Data.CullThreshold(world.RelationshipKindId(relationshipId)))
        {
            simulation.CullRelationship(relationshipId, tick);
        }
    }

    private float ComputeProximityDelta(WorldState world, int sourceId, int targetId)
    {
        if (Data.ProximityMaxDistance <= 0)
        {
            return 0f;
        }

        var distance = HexDistance(
            world.EntityPhysicalQ(sourceId),
            world.EntityPhysicalR(sourceId),
            world.EntityPhysicalQ(targetId),
            world.EntityPhysicalR(targetId));
        if (distance >= Data.ProximityMaxDistance)
        {
            return 0f;
        }

        var factor = 1f - (distance / (float)Data.ProximityMaxDistance);
        return Data.ProximityMaxDelta * factor;
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

}
