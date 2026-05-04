namespace Engine.Kernel;

internal sealed class MovementProgramRuntime :
    IKernelProgramRuntime<MovementProgramData, MovementProgramState>
{
    public MovementProgramRuntime(MovementProgramData data, MovementProgramState state)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        Data = data;
        State = state;
    }

    public MovementProgramData Data { get; }

    public MovementProgramState State { get; }

    public bool CanEnter(Simulation simulation, int kindId, int q, int r)
    {
        return CanEnter(simulation, PlaneCodes.Physical, kindId, q, r);
    }

    public bool CanEnter(Simulation simulation, int planeCode, int kindId, int q, int r)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        return simulation.ProgramIsEnabled(KernelProgramCodes.Movement) &&
            simulation.World.CellIsKnown(planeCode, q, r) &&
            simulation.KernelData.Terrain.CanEnter(simulation.World.CellTerrainId(planeCode, q, r), kindId);
    }

    public float PhysicalTravelCost(Simulation simulation, int kindId, int q, int r, int tick)
    {
        return TravelCost(simulation, PlaneCodes.Physical, kindId, q, r, tick);
    }

    public float TravelCost(Simulation simulation, int planeCode, int kindId, int q, int r, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        if (!simulation.ProgramIsEnabled(KernelProgramCodes.Movement) ||
            !simulation.World.CellIsKnown(planeCode, q, r))
        {
            return float.PositiveInfinity;
        }

        return simulation.KernelData.Terrain.RoadAdjustedTravelCost(
            simulation.World.CellTerrainId(planeCode, q, r),
            kindId,
            RoadQuality(planeCode, q, r, tick));
    }

    public float RoadQuality(int planeCode, int q, int r, int tick) =>
        State.RoadQuality(planeCode, q, r, tick, Data.RoadDecayPerTick);

    public int RoadTravelCount(int planeCode, int q, int r) => State.RoadTravelCount(planeCode, q, r);

    public MovementMoveResult TryMovePhysical(
        Simulation simulation,
        int entityId,
        int q,
        int r,
        int tick,
        int causeCode)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        ValidateCauseCode(causeCode);
        var fromQ = simulation.World.EntityPhysicalQ(entityId);
        var fromR = simulation.World.EntityPhysicalR(entityId);
        var stepCost = StepCostForMove(simulation, PlaneCodes.Physical, entityId, q, r, tick, causeCode);
        if (!MoveAllowed(simulation, PlaneCodes.Physical, entityId, q, r, stepCost, causeCode))
        {
            return new MovementMoveResult(false, stepCost, fromQ, fromR, q, r, causeCode);
        }

        simulation.MoveEntityPhysicalUnchecked(entityId, q, r, tick);
        if ((fromQ != q || fromR != r) && causeCode == MovementCauseCodes.Travel)
        {
            ApplyTravelSurface(simulation, PlaneCodes.Physical, q, r, tick);
        }

        return new MovementMoveResult(fromQ != q || fromR != r, stepCost, fromQ, fromR, q, r, causeCode);
    }

    public MovementSemanticMoveResult TryMoveSemantic(
        Simulation simulation,
        int entityId,
        int q,
        int r,
        int s,
        int tick,
        int causeCode)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        ValidateCauseCode(causeCode);
        var fromQ = simulation.World.EntitySemanticQ(entityId);
        var fromR = simulation.World.EntitySemanticR(entityId);
        var fromS = simulation.World.EntitySemanticS(entityId);
        var stepCost = StepCostForMove(simulation, PlaneCodes.Semantic, entityId, q, r, tick, causeCode);
        if (!MoveAllowed(simulation, PlaneCodes.Semantic, entityId, q, r, stepCost, causeCode))
        {
            return new MovementSemanticMoveResult(false, stepCost, fromQ, fromR, fromS, q, r, s, causeCode);
        }

        simulation.MoveEntitySemanticUnchecked(entityId, q, r, s, tick);
        if ((fromQ != q || fromR != r || fromS != s) && causeCode == MovementCauseCodes.Travel)
        {
            ApplyTravelSurface(simulation, PlaneCodes.Semantic, q, r, tick);
        }

        return new MovementSemanticMoveResult(
            fromQ != q || fromR != r || fromS != s,
            stepCost,
            fromQ,
            fromR,
            fromS,
            q,
            r,
            s,
            causeCode);
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        _ = tick;
    }

    private float StepCostForMove(
        Simulation simulation,
        int planeCode,
        int entityId,
        int q,
        int r,
        int tick,
        int causeCode)
    {
        if (causeCode != MovementCauseCodes.Travel)
        {
            return 1f;
        }

        return TravelCost(simulation, planeCode, simulation.World.EntityKindId(entityId), q, r, tick);
    }

    private bool MoveAllowed(
        Simulation simulation,
        int planeCode,
        int entityId,
        int q,
        int r,
        float stepCost,
        int causeCode)
    {
        if (!simulation.ProgramIsEnabled(KernelProgramCodes.Movement))
        {
            return false;
        }

        if (causeCode != MovementCauseCodes.Travel)
        {
            return true;
        }

        return !float.IsInfinity(stepCost) &&
            stepCost > 0f &&
            CanEnter(simulation, planeCode, simulation.World.EntityKindId(entityId), q, r);
    }

    private void ApplyTravelSurface(Simulation simulation, int planeCode, int q, int r, int tick)
    {
        if (!simulation.World.CellIsKnown(planeCode, q, r))
        {
            return;
        }

        var terrainId = simulation.World.CellTerrainId(planeCode, q, r);
        State.RecordTravel(
            planeCode,
            q,
            r,
            tick,
            Data.RoadBuildPerTravel * simulation.KernelData.Terrain.RoadBuildMultiplier(terrainId),
            Data.RoadDecayPerTick);
    }

    private static void ValidateCauseCode(int causeCode)
    {
        if (causeCode is
            MovementCauseCodes.Travel or
            MovementCauseCodes.Placement or
            MovementCauseCodes.Forced or
            MovementCauseCodes.Relocation or
            MovementCauseCodes.Follow)
        {
            return;
        }

        throw new global::System.InvalidOperationException();
    }
}
