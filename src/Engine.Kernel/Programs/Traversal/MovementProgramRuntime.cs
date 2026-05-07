namespace Engine.Kernel;

internal sealed partial class MovementProgramRuntime :
    IKernelProgramRuntime<MovementProgramData, MovementProgramState>
{
    public MovementProgramRuntime(
        MovementProgramData data,
        MovementProgramState state,
        MovementProgramScratch scratch)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(scratch);
        Data = data;
        State = state;
        Scratch = scratch;
    }

    public MovementProgramData Data { get; }

    public MovementProgramState State { get; }

    public MovementProgramScratch Scratch { get; }

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

    public int SnapshotEventCount() => State.RoadCellCount;

    public void EmitSnapshotEvents(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        State.ForEachRoadCell((planeCode, coordinateKey, quality, lastTick, travelCount) =>
        {
            _ = quality;
            _ = lastTick;
            if (!simulation.World.TryResolveCoordinateKey(
                    coordinateKey,
                    planeCode == PlaneCodes.Semantic ? 4 : 0,
                    out var q,
                    out var r))
            {
                return;
            }

            simulation.EmitCellRoadChanged(
                tick,
                planeCode,
                q,
                r,
                0f,
                RoadQuality(planeCode, q, r, tick),
                travelCount);
        });
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        _ = tick;
    }

    public MovementMoveResult Navigate(
        Simulation simulation,
        int entityId,
        int planeCode,
        int targetQ,
        int targetR,
        int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        ValidatePlane(planeCode);
        var world = simulation.World;
        var fromQ = CurrentQ(world, entityId, planeCode);
        var fromR = CurrentR(world, entityId, planeCode);
        var fromS = planeCode == PlaneCodes.Semantic ? -fromQ - fromR : 0;
        if (fromQ == targetQ && fromR == targetR)
        {
            return new MovementMoveResult(
                false, 0f, planeCode, fromQ, fromR, fromS, fromQ, fromR, fromS, MovementCauseCodes.Travel);
        }

        if (!TryPlanNextStep(simulation, entityId, planeCode, fromQ, fromR, targetQ, targetR, tick, out var nextQ, out var nextR))
        {
            return new MovementMoveResult(
                false, float.PositiveInfinity, planeCode, fromQ, fromR, fromS, fromQ, fromR, fromS, MovementCauseCodes.Travel);
        }

        return StepUnchecked(simulation, entityId, planeCode, nextQ, nextR, tick, MovementCauseCodes.Travel);
    }

    public MovementMoveResult Wander(
        Simulation simulation,
        int entityId,
        int planeCode,
        int tick) =>
        Wander(simulation, entityId, planeCode, tick, anchorQ: 0, anchorR: 0, maxDistanceFromAnchor: -1);

    public MovementMoveResult Wander(
        Simulation simulation,
        int entityId,
        int planeCode,
        int tick,
        int anchorQ,
        int anchorR,
        int maxDistanceFromAnchor)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        ValidatePlane(planeCode);
        var world = simulation.World;
        var actorKindId = world.EntityKindId(entityId);
        var fromQ = CurrentQ(world, entityId, planeCode);
        var fromR = CurrentR(world, entityId, planeCode);
        var fromS = planeCode == PlaneCodes.Semantic ? -fromQ - fromR : 0;

        var totalWeight = 0f;
        var selectedDirection = -1;
        for (var direction = 0; direction < 6; direction++)
        {
            var q = fromQ + DirectionQ(direction);
            var r = fromR + DirectionR(direction);
            if (!CanEnter(simulation, planeCode, actorKindId, q, r))
            {
                continue;
            }

            if (maxDistanceFromAnchor >= 0 &&
                KernelQuery.HexDistance(q, r, anchorQ, anchorR) > maxDistanceFromAnchor)
            {
                continue;
            }

            var cost = TravelCost(simulation, planeCode, actorKindId, q, r, tick);
            if (float.IsInfinity(cost) || cost <= 0f)
            {
                continue;
            }

            var weight = 1f / global::System.MathF.Max(0.25f, cost);
            totalWeight += weight;
            if (simulation.NextFloat() * totalWeight <= weight)
            {
                selectedDirection = direction;
            }
        }

        if (selectedDirection < 0)
        {
            return new MovementMoveResult(
                false, 0f, planeCode, fromQ, fromR, fromS, fromQ, fromR, fromS, MovementCauseCodes.Travel);
        }

        var toQ = fromQ + DirectionQ(selectedDirection);
        var toR = fromR + DirectionR(selectedDirection);
        return StepUnchecked(simulation, entityId, planeCode, toQ, toR, tick, MovementCauseCodes.Travel);
    }

    public MovementMoveResult Place(
        Simulation simulation,
        int entityId,
        int planeCode,
        int q,
        int r,
        int tick,
        int causeCode)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        ValidatePlane(planeCode);
        if (causeCode == MovementCauseCodes.Travel)
        {
            throw new global::System.InvalidOperationException();
        }

        ValidateCauseCode(causeCode);
        return StepUnchecked(simulation, entityId, planeCode, q, r, tick, causeCode);
    }

    private MovementMoveResult StepUnchecked(
        Simulation simulation,
        int entityId,
        int planeCode,
        int q,
        int r,
        int tick,
        int causeCode)
    {
        var world = simulation.World;
        var fromQ = CurrentQ(world, entityId, planeCode);
        var fromR = CurrentR(world, entityId, planeCode);
        var fromS = planeCode == PlaneCodes.Semantic ? -fromQ - fromR : 0;
        var toS = planeCode == PlaneCodes.Semantic ? -q - r : 0;
        var stepCost = StepCostForMove(simulation, planeCode, entityId, q, r, tick, causeCode);
        if (!MoveAllowed(simulation, planeCode, entityId, q, r, stepCost, causeCode))
        {
            return new MovementMoveResult(false, stepCost, planeCode, fromQ, fromR, fromS, q, r, toS, causeCode);
        }

        if (planeCode == PlaneCodes.Physical)
        {
            simulation.MoveEntityPhysicalUnchecked(entityId, q, r, tick);
        }
        else
        {
            simulation.MoveEntitySemanticUnchecked(entityId, q, r, toS, tick);
        }

        var moved = fromQ != q || fromR != r || (planeCode == PlaneCodes.Semantic && fromS != toS);
        if (moved && causeCode == MovementCauseCodes.Travel)
        {
            ApplyTravelSurface(simulation, planeCode, q, r, tick);
        }

        return new MovementMoveResult(moved, stepCost, planeCode, fromQ, fromR, fromS, q, r, toS, causeCode);
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
        var previousQuality = RoadQuality(planeCode, q, r, tick);
        State.RecordTravel(
            planeCode,
            q,
            r,
            tick,
            Data.RoadBuildPerTravel * simulation.KernelData.Terrain.RoadBuildMultiplier(terrainId),
            Data.RoadDecayPerTick);
        var currentQuality = RoadQuality(planeCode, q, r, tick);
        simulation.EmitCellRoadChanged(
            tick,
            planeCode,
            q,
            r,
            previousQuality,
            currentQuality,
            RoadTravelCount(planeCode, q, r));
    }

    private static int CurrentQ(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticQ(entityId) : world.EntityPhysicalQ(entityId);

    private static int CurrentR(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticR(entityId) : world.EntityPhysicalR(entityId);

    private static void ValidatePlane(int planeCode)
    {
        if (planeCode is not PlaneCodes.Physical and not PlaneCodes.Semantic)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }
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
