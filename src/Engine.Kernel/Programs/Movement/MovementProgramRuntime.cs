namespace Engine.Kernel;

internal sealed class MovementProgramRuntime :
    IKernelProgramRuntime<MovementProgramData, MovementProgramState>
{
    private const int DirectionCount = 6;

    public MovementProgramRuntime(MovementProgramData data, MovementProgramState state)
        : this(data, state, new KernelProgramTuningOptions())
    {
    }

    public MovementProgramRuntime(
        MovementProgramData data,
        MovementProgramState state,
        KernelProgramTuningOptions tuning)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        Data = data;
        State = state;
        Tuning = tuning;
    }

    public MovementProgramData Data { get; }

    public MovementProgramState State { get; }

    public KernelProgramTuningOptions Tuning { get; }

    public int RegisterRule(
        int kindId,
        int planeCode,
        float moveChance,
        float exploreWeight,
        float stalenessWeight,
        float crowdingWeight) =>
        Data.RegisterRule(kindId, planeCode, moveChance, exploreWeight, stalenessWeight, crowdingWeight);

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

            var rule = Data.RuleForKind(world.EntityKindId(entityId));
            if (rule == 0 || simulation.NextFloat() > Data.MoveChance(rule))
            {
                continue;
            }

            if (Data.PlaneCode(rule) == PlaneCodes.Physical)
            {
                MovePhysical(simulation, world, entityId, rule, tick);
            }
            else
            {
                MoveSemantic(simulation, world, entityId, rule, tick);
            }
        }
    }

    private void MovePhysical(Simulation simulation, WorldState world, int entityId, int rule, int tick)
    {
        var currentQ = world.EntityPhysicalQ(entityId);
        var currentR = world.EntityPhysicalR(entityId);
        var totalWeight = 0f;
        var selectedQ = currentQ;
        var selectedR = currentR;
        for (var direction = 0; direction < DirectionCount; direction++)
        {
            var q = currentQ + DirectionQ(direction);
            var r = currentR + DirectionR(direction);
            if (!world.CellIsKnown(q, r))
            {
                continue;
            }

            var weight = CandidateWeight(simulation, PlaneCodes.Physical, q, r, rule, tick);
            totalWeight += weight;
            if (simulation.NextFloat() * totalWeight <= weight)
            {
                selectedQ = q;
                selectedR = r;
            }
        }

        if (selectedQ != currentQ || selectedR != currentR)
        {
            simulation.MoveEntityPhysical(entityId, selectedQ, selectedR, tick);
        }
    }

    private void MoveSemantic(Simulation simulation, WorldState world, int entityId, int rule, int tick)
    {
        var currentQ = world.EntitySemanticQ(entityId);
        var currentR = world.EntitySemanticR(entityId);
        var totalWeight = 0f;
        var selectedQ = currentQ;
        var selectedR = currentR;
        for (var direction = 0; direction < DirectionCount; direction++)
        {
            var q = currentQ + DirectionQ(direction);
            var r = currentR + DirectionR(direction);
            var weight = CandidateWeight(simulation, PlaneCodes.Semantic, q, r, rule, tick);
            totalWeight += weight;
            if (simulation.NextFloat() * totalWeight <= weight)
            {
                selectedQ = q;
                selectedR = r;
            }
        }

        if (selectedQ != currentQ || selectedR != currentR)
        {
            simulation.MoveEntitySemantic(entityId, selectedQ, selectedR, -selectedQ - selectedR, tick);
        }
    }

    private float CandidateWeight(Simulation simulation, int planeCode, int q, int r, int rule, int tick)
    {
        var visits = simulation.MapActivity.VisitCount(planeCode, q, r);
        var lastVisited = simulation.MapActivity.LastVisitedTick(planeCode, q, r);
        var occupancy = simulation.MapActivity.CurrentOccupancy(planeCode, q, r);
        var staleness = lastVisited <= 0 ? 1f : Clamp01((tick - lastVisited) / 100f);
        var weight = 1f;
        weight += Data.ExploreWeight(rule) / (1f + visits);
        weight += Data.StalenessWeight(rule) * staleness;
        weight -= Data.CrowdingWeight(rule) * occupancy;
        return weight <= 0.01f ? 0.01f : weight;
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private static int DirectionQ(int direction) => direction switch
    {
        0 => 1,
        1 => 1,
        2 => 0,
        3 => -1,
        4 => -1,
        _ => 0,
    };

    private static int DirectionR(int direction) => direction switch
    {
        0 => 0,
        1 => -1,
        2 => -1,
        3 => 0,
        4 => 1,
        _ => 1,
    };
}

