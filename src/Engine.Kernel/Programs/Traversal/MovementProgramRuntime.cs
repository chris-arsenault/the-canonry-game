namespace Engine.Kernel;

internal sealed partial class MovementProgramRuntime :
    IKernelProgramRuntime<MovementProgramData, MovementProgramState>
{
    public MovementProgramRuntime(
        MovementProgramData data,
        MovementProgramState state,
        MovementProgramScratch scratch)
        : this(data, state, scratch, new KernelProgramTuningOptions())
    {
    }

    public MovementProgramRuntime(
        MovementProgramData data,
        MovementProgramState state,
        MovementProgramScratch scratch,
        KernelProgramTuningOptions tuning)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(scratch);
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        Data = data;
        State = state;
        Scratch = scratch;
        Tuning = tuning;
    }

    public MovementProgramData Data { get; }

    public MovementProgramState State { get; }

    public MovementProgramScratch Scratch { get; }

    public KernelProgramTuningOptions Tuning { get; }

    public void RegisterDefaultMovementProfile(
        int kindId,
        int planeCode,
        int movementProfileId) =>
        Data.RegisterDefaultMovementProfile(kindId, planeCode, movementProfileId);

    public void RegisterSubtypeMovementProfile(
        int kindId,
        int subtypeId,
        int planeCode,
        int movementProfileId) =>
        Data.RegisterSubtypeMovementProfile(kindId, subtypeId, planeCode, movementProfileId);

    public void RegisterTraitMovementProfile(
        int kindId,
        int traitId,
        int planeCode,
        int movementProfileId) =>
        Data.RegisterTraitMovementProfile(kindId, traitId, planeCode, movementProfileId);

    public void RegisterTravelStructure(
        int travelStructureId,
        int planeCode) =>
        Data.RegisterTravelStructure(travelStructureId, planeCode);

    public void RegisterTravelStructureMovementProfile(
        int travelStructureId,
        int movementProfileId,
        float movementCost) =>
        Data.RegisterTravelStructureMovementProfile(travelStructureId, movementProfileId, movementCost);

    public int MovementProfileFor(Simulation simulation, int entityId, int planeCode)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        return Data.MovementProfileFor(simulation.World, entityId, planeCode);
    }

    public int MovementProfileFor(int kindId, int subtypeId, int planeCode) =>
        Data.MovementProfileFor(kindId, subtypeId, planeCode);

    public bool CanEnter(Simulation simulation, int entityId, int q, int r)
    {
        return CanEnter(simulation, PlaneCodes.Physical, entityId, q, r);
    }

    public bool CanEnter(Simulation simulation, int planeCode, int entityId, int q, int r) =>
        CanEnter(simulation, planeCode, entityId, q, r, z: 0);

    public bool CanEnter(Simulation simulation, int planeCode, int entityId, int q, int r, int z)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var movementProfileId = Data.MovementProfileFor(simulation.World, entityId, planeCode);
        return z == 0
            ? CanEnterProfile(simulation, planeCode, movementProfileId, q, r)
            : CanEnterForgedCell(simulation, planeCode, movementProfileId, q, r, z);
    }

    private bool CanEnterForgedCell(Simulation simulation, int planeCode, int movementProfileId, int q, int r, int z)
    {
        if (!simulation.ProgramIsEnabled(KernelProgramCodes.Movement) ||
            planeCode != PlaneCodes.Physical ||
            !simulation.World.CellIsKnown(planeCode, q, r, z))
        {
            return false;
        }

        var terrainId = simulation.World.CellTerrainId(planeCode, q, r, z);
        return simulation.KernelData.Terrain.TravelCost(terrainId, movementProfileId) > 0f;
    }

    public bool CanEnterProfile(Simulation simulation, int planeCode, int movementProfileId, int q, int r)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        return simulation.ProgramIsEnabled(KernelProgramCodes.Movement) &&
            TrySurfaceBaseTravelCost(
                simulation,
                simulation.World,
                planeCode,
                movementProfileId,
                q,
                r,
                out _,
                out _);
    }

    public float PhysicalTravelCost(Simulation simulation, int entityId, int q, int r, int tick)
    {
        return TravelCost(simulation, PlaneCodes.Physical, entityId, q, r, tick);
    }

    public float TravelCost(Simulation simulation, int planeCode, int entityId, int q, int r, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var movementProfileId = Data.MovementProfileFor(simulation.World, entityId, planeCode);
        return TravelCostProfile(simulation, planeCode, movementProfileId, q, r, tick);
    }

    public float TravelCostProfile(Simulation simulation, int planeCode, int movementProfileId, int q, int r, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        if (!simulation.ProgramIsEnabled(KernelProgramCodes.Movement) ||
            !simulation.World.CellIsKnown(planeCode, q, r))
        {
            return float.PositiveInfinity;
        }

        return SurfaceTravelCost(simulation, planeCode, movementProfileId, q, r, tick);
    }

    public float RoadQuality(int planeCode, int q, int r, int tick) =>
        State.RoadQuality(planeCode, q, r, tick, Data.RoadDecayPerTick, Tuning.MinorActorMainTickRatio);

    public int RoadTravelCount(int planeCode, int q, int r) => State.RoadTravelCount(planeCode, q, r);

    public int TravelStructureId(int planeCode, int q, int r) => State.TravelStructureId(planeCode, q, r);

    internal bool ReinforceRoad(
        Simulation simulation,
        int planeCode,
        int q,
        int r,
        float amount,
        int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        ValidatePlane(planeCode);
        if (!simulation.ProgramIsEnabled(KernelProgramCodes.Movement) ||
            amount <= 0f ||
            !simulation.World.CellIsKnown(planeCode, q, r))
        {
            return false;
        }

        var terrainId = simulation.World.CellTerrainId(planeCode, q, r);
        var previousQuality = RoadQuality(planeCode, q, r, tick);
        State.RecordTravel(
            planeCode,
            q,
            r,
            tick,
            amount * simulation.KernelData.Terrain.RoadBuildMultiplier(terrainId),
            Data.RoadDecayPerTick,
            Tuning.MinorActorMainTickRatio);
        var currentQuality = RoadQuality(planeCode, q, r, tick);
        if (global::System.MathF.Abs(currentQuality - previousQuality) <= 0.0001f)
        {
            return false;
        }

        simulation.EmitCellRoadChanged(
            tick,
            planeCode,
            q,
            r,
            previousQuality,
            currentQuality,
            RoadTravelCount(planeCode, q, r));
        return true;
    }

    public bool SetTravelStructure(
        Simulation simulation,
        int planeCode,
        int q,
        int r,
        int travelStructureId,
        int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        ValidatePlane(planeCode);
        if (!simulation.ProgramIsEnabled(KernelProgramCodes.Movement) ||
            !simulation.World.CellIsKnown(planeCode, q, r) ||
            !Data.TravelStructureRegistered(travelStructureId) ||
            Data.TravelStructurePlaneCode(travelStructureId) != planeCode)
        {
            return false;
        }

        var previousStructureId = State.TravelStructureId(planeCode, q, r);
        if (!State.SetTravelStructure(planeCode, q, r, travelStructureId))
        {
            return false;
        }

        simulation.EmitCellTravelStructureChanged(
            tick,
            planeCode,
            q,
            r,
            previousStructureId,
            travelStructureId);
        return true;
    }

    internal void RecordTopologyChanged() => State.RecordTopologyChanged();

    public bool CanReach(
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
        return fromQ == targetQ && fromR == targetR ||
            TryPlanNextStep(
                simulation,
                entityId,
                planeCode,
                fromQ,
                fromR,
                targetQ,
                targetR,
                tick,
                out _,
                out _);
    }

    public int SnapshotEventCount() => State.RoadCellCount + State.TravelStructureCellCount;

    public void EmitSnapshotEvents(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        State.ForEachRoadCell((planeCode, coordinateKey, quality, lastTick, travelCount) =>
        {
            _ = quality;
            _ = lastTick;
            if (!simulation.World.TryResolveCoordinateKey(
                    planeCode,
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
        State.ForEachTravelStructureCell((planeCode, coordinateKey, travelStructureId) =>
        {
            if (!simulation.World.TryResolveCoordinateKey(
                    planeCode,
                    coordinateKey,
                    planeCode == PlaneCodes.Semantic ? 4 : 0,
                    out var q,
                    out var r))
            {
                return;
            }

            simulation.EmitCellTravelStructureChanged(
                tick,
                planeCode,
                q,
                r,
                0,
                travelStructureId);
        });
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        if (tick % LocalRoadMarkCadence == 0)
        {
            ReconcileRoadMarks(simulation, tick, LocalPathSegmentSize);
        }

        if (tick % RegionalRoadMarkCadence == 0)
        {
            ReconcileRoadMarks(simulation, tick, RegionalPathSegmentSize);
        }
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
        var fromQ = CurrentQ(world, entityId, planeCode);
        var fromR = CurrentR(world, entityId, planeCode);
        var fromS = planeCode == PlaneCodes.Semantic ? -fromQ - fromR : 0;

        var fromZ = CurrentZ(world, entityId, planeCode);
        var selectedDirection = SelectWanderDirection(
            simulation,
            entityId,
            planeCode,
            fromQ,
            fromR,
            fromZ,
            anchorQ,
            anchorR,
            maxDistanceFromAnchor);
        if (selectedDirection < 0)
        {
            return new MovementMoveResult(
                false, 0f, planeCode, fromQ, fromR, fromS, fromQ, fromR, fromS, MovementCauseCodes.Travel);
        }

        var toQ = fromQ + DirectionQ(selectedDirection);
        var toR = fromR + DirectionR(selectedDirection);
        var toZ = fromZ + DirectionZ(selectedDirection);
        var stepCost = toZ == 0
            ? BaseTravelCost(simulation, planeCode, entityId, toQ, toR) *
                DirectedEdgeFactor(
                    simulation,
                    planeCode,
                    Data.MovementProfileFor(world, entityId, planeCode),
                    fromQ,
                    fromR,
                    toQ,
                    toR,
                    tick,
                    roadAware: true)
            : 1f;
        return StepUnchecked(simulation, entityId, planeCode, toQ, toR, toZ, tick, MovementCauseCodes.Travel, stepCost);
    }

    private int SelectWanderDirection(
        Simulation simulation,
        int entityId,
        int planeCode,
        int fromQ,
        int fromR,
        int fromZ,
        int anchorQ,
        int anchorR,
        int maxDistanceFromAnchor)
    {
        var candidateCount = 0;
        var selectedDirection = -1;
        var directionCount = planeCode == PlaneCodes.Physical ? 8 : 6;
        for (var direction = 0; direction < directionCount; direction++)
        {
            var q = fromQ + DirectionQ(direction);
            var r = fromR + DirectionR(direction);
            var z = fromZ + DirectionZ(direction);
            if (!CanEnter(simulation, planeCode, entityId, q, r, z))
            {
                continue;
            }

            if (maxDistanceFromAnchor >= 0 &&
                KernelQuery.HexDistance(q, r, anchorQ, anchorR) > maxDistanceFromAnchor)
            {
                continue;
            }

            candidateCount++;
            if (simulation.NextFloat() * candidateCount < 1f)
            {
                selectedDirection = direction;
            }
        }

        return selectedDirection;
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
        var stepCost = StepCostForMove(simulation, planeCode, entityId, q, r, tick, causeCode);
        stepCost *= CarrierRoadSpeedFactor(simulation, planeCode, entityId, q, r, tick, causeCode);
        if (causeCode == MovementCauseCodes.Travel)
        {
            stepCost *= _ageStepRateMultiplier;
        }

        return StepUnchecked(simulation, entityId, planeCode, q, r, tick, causeCode, stepCost);
    }

    private MovementMoveResult StepUnchecked(
        Simulation simulation,
        int entityId,
        int planeCode,
        int q,
        int r,
        int tick,
        int causeCode,
        float stepCost) =>
        StepUnchecked(
            simulation,
            entityId,
            planeCode,
            q,
            r,
            CurrentZ(simulation.World, entityId, planeCode),
            tick,
            causeCode,
            stepCost);

    private MovementMoveResult StepUnchecked(
        Simulation simulation,
        int entityId,
        int planeCode,
        int q,
        int r,
        int z,
        int tick,
        int causeCode,
        float stepCost)
    {
        var world = simulation.World;
        if (causeCode == MovementCauseCodes.Travel && planeCode == PlaneCodes.Physical)
        {
            stepCost *= simulation.Knowledge.Modifier(
                world.EntityCultureId(entityId),
                KnowledgeGrantDomainCodes.MovementStepRate,
                world.EntityKindId(entityId));
        }

        var fromQ = CurrentQ(world, entityId, planeCode);
        var fromR = CurrentR(world, entityId, planeCode);
        var fromZ = CurrentZ(world, entityId, planeCode);
        var fromS = planeCode == PlaneCodes.Semantic ? -fromQ - fromR : 0;
        var toS = planeCode == PlaneCodes.Semantic ? -q - r : 0;
        if (!MoveAllowed(simulation, planeCode, entityId, q, r, z, stepCost, causeCode))
        {
            return new MovementMoveResult(false, stepCost, planeCode, fromQ, fromR, fromS, q, r, toS, causeCode);
        }

        var moved = ApplyMove(simulation, entityId, planeCode, fromQ, fromR, fromZ, fromS, q, r, z, toS, tick, causeCode);
        return new MovementMoveResult(moved, stepCost, planeCode, fromQ, fromR, fromS, q, r, toS, causeCode);
    }

    private bool ApplyMove(
        Simulation simulation,
        int entityId,
        int planeCode,
        int fromQ,
        int fromR,
        int fromZ,
        int fromS,
        int q,
        int r,
        int z,
        int toS,
        int tick,
        int causeCode)
    {
        if (planeCode == PlaneCodes.Physical)
        {
            simulation.MoveEntityPhysicalUnchecked(entityId, q, r, z, tick);
        }
        else
        {
            simulation.MoveEntitySemanticUnchecked(entityId, q, r, toS, tick);
        }

        var moved = fromQ != q || fromR != r || fromZ != z || (planeCode == PlaneCodes.Semantic && fromS != toS);
        if (moved && causeCode == MovementCauseCodes.Travel && z == 0)
        {
            ApplyTravelSurface(simulation, entityId, planeCode, q, r, tick);
        }

        return moved;
    }

    private const float CarrierRoadSpeedFloor = 0.5f;
    private const float CarrierRoadQualityThreshold = 40f;
    private float _ageStepRateMultiplier = 1f;

    public void SetCarrierRelationshipKindId(int relationshipKindId) =>
        Data.CarrierRelationshipKindId = relationshipKindId;

    /// <summary>
    /// Age charge: the world-beat program scales movement pace per open age
    /// each pulse (below 1 = faster, so a Thaw or Wandering quickens travel).
    /// Applied to travel step cost, so it reads as more ground per tick.
    /// </summary>
    public void SetAgeStepRateMultiplier(float multiplier) => _ageStepRateMultiplier = multiplier;

    public void ScaleAgeStepRateMultiplier(float multiplier) => _ageStepRateMultiplier *= multiplier;

    /// <summary>
    /// The railroad made concrete: a laden carrier on a good road covers
    /// ground faster than a walker. Speed comes from the road and the load,
    /// never from the creature - a carrier off-road or empty moves normally.
    /// The relationship check runs only on established road, keeping the hot
    /// path cheap.
    /// </summary>
    private float CarrierRoadSpeedFactor(Simulation simulation, int planeCode, int entityId, int q, int r, int tick, int causeCode)
    {
        if (causeCode != MovementCauseCodes.Travel || Data.CarrierRelationshipKindId <= 0)
        {
            return 1f;
        }

        var roadQuality = RoadQuality(planeCode, q, r, tick);
        if (roadQuality < CarrierRoadQualityThreshold ||
            !simulation.World.HasOutgoingRelationshipOfKind(entityId, Data.CarrierRelationshipKindId))
        {
            return 1f;
        }

        var strength = Clamp01(roadQuality / 100f);
        return 1f - ((1f - CarrierRoadSpeedFloor) * strength);
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

        var world = simulation.World;
        return TravelCost(simulation, planeCode, entityId, q, r, tick) *
            DirectedEdgeFactor(
                simulation,
                planeCode,
                Data.MovementProfileFor(world, entityId, planeCode),
                CurrentQ(world, entityId, planeCode),
                CurrentR(world, entityId, planeCode),
                q,
                r,
                tick,
                roadAware: true);
    }

    /// <summary>
    /// Directed travel cost between adjacent cells: the target cell's
    /// surface cost times the crossing factor of the edge between them.
    /// Used by travel reach so a great river or a ridge line gates reach the
    /// way it gates walking.
    /// </summary>
    internal float DirectedTravelCost(
        Simulation simulation,
        int planeCode,
        int entityId,
        int fromQ,
        int fromR,
        int toQ,
        int toR,
        int tick)
    {
        return TravelCost(simulation, planeCode, entityId, toQ, toR, tick) *
            DirectedEdgeFactor(
                simulation,
                planeCode,
                Data.MovementProfileFor(simulation.World, entityId, planeCode),
                fromQ,
                fromR,
                toQ,
                toR,
                tick,
                roadAware: true);
    }

    /// <summary>
    /// The crossing surcharge for stepping between two adjacent physical
    /// cells. Crossings never block: factors are at least 1, unregistered
    /// classes and non-adjacent pairs read as 1, and a road-negated class
    /// (rivers) has its surcharge erased as road quality rises - a ford
    /// becomes a bridge through use. Ridge and cliff surcharges are
    /// road-immune; worldgen already folded passes to open edges.
    /// </summary>
    private float DirectedEdgeFactor(
        Simulation simulation,
        int planeCode,
        int movementProfileId,
        int fromQ,
        int fromR,
        int toQ,
        int toR,
        int tick,
        bool roadAware)
    {
        if (planeCode != PlaneCodes.Physical)
        {
            return 1f;
        }

        var direction = DirectionBetween(toQ - fromQ, toR - fromR);
        if (direction < 0)
        {
            return 1f;
        }

        var crossingClass = PhysicalCrossingCodes.ClassAt(
            simulation.KernelData.PhysicalGeography.CrossingWord(fromQ, fromR),
            direction);
        if (crossingClass == PhysicalCrossingCodes.Open)
        {
            return 1f;
        }

        var factor = simulation.KernelData.Terrain.CrossingFactor(crossingClass, movementProfileId);
        if (roadAware && simulation.KernelData.Terrain.CrossingRoadNegated(crossingClass))
        {
            var roadQuality = global::System.MathF.Max(
                RoadQuality(planeCode, fromQ, fromR, tick),
                RoadQuality(planeCode, toQ, toR, tick));
            factor = 1f + ((factor - 1f) * (1f - Clamp01(roadQuality / 100f)));
        }

        return factor;
    }

    private static int DirectionBetween(int dq, int dr)
    {
        for (var direction = 0; direction < 6; direction++)
        {
            if (DirectionQ(direction) == dq && DirectionR(direction) == dr)
            {
                return direction;
            }
        }

        return -1;
    }

    private float BaseTravelCost(
        Simulation simulation,
        int planeCode,
        int entityId,
        int q,
        int r)
    {
        var movementProfileId = Data.MovementProfileFor(simulation.World, entityId, planeCode);
        return SurfaceBaseTravelCost(simulation, planeCode, movementProfileId, q, r);
    }

    private bool MoveAllowed(
        Simulation simulation,
        int planeCode,
        int entityId,
        int q,
        int r,
        int z,
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
            CanEnter(simulation, planeCode, entityId, q, r, z);
    }

    private void ApplyTravelSurface(Simulation simulation, int entityId, int planeCode, int q, int r, int tick)
    {
        if (!simulation.World.CellIsKnown(planeCode, q, r))
        {
            return;
        }

        var terrainId = simulation.World.CellTerrainId(planeCode, q, r);
        var roadWork = simulation.Knowledge.Modifier(
            simulation.World.EntityCultureId(entityId),
            KnowledgeGrantDomainCodes.RoadWork,
            simulation.World.EntityKindId(entityId));
        var previousQuality = RoadQuality(planeCode, q, r, tick);
        State.RecordTravel(
            planeCode,
            q,
            r,
            tick,
            Data.RoadBuildPerTravel * simulation.KernelData.Terrain.RoadBuildMultiplier(terrainId) * roadWork,
            Data.RoadDecayPerTick,
            Tuning.MinorActorMainTickRatio);
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

    private float SurfaceTravelCost(
        Simulation simulation,
        int planeCode,
        int movementProfileId,
        int q,
        int r,
        int tick)
    {
        if (!TrySurfaceBaseTravelCost(
                simulation,
                simulation.World,
                planeCode,
                movementProfileId,
                q,
                r,
                out var baseCost,
                out var terrainId))
        {
            return float.PositiveInfinity;
        }

        var normalizedRoadQuality = Clamp01(RoadQuality(planeCode, q, r, tick) / 100f);
        var roadSpeed = 1f + ((simulation.KernelData.Terrain.RoadSpeedMultiplier(terrainId) - 1f) * normalizedRoadQuality);
        return baseCost / roadSpeed;
    }

    private float SurfaceBaseTravelCost(
        Simulation simulation,
        int planeCode,
        int movementProfileId,
        int q,
        int r)
    {
        return TrySurfaceBaseTravelCost(
            simulation,
            simulation.World,
            planeCode,
            movementProfileId,
            q,
            r,
            out var baseCost,
            out _)
            ? baseCost
            : 0f;
    }

    private bool TrySurfaceBaseTravelCost(
        Simulation simulation,
        WorldState world,
        int planeCode,
        int movementProfileId,
        int q,
        int r,
        out float baseCost,
        out int terrainId)
    {
        baseCost = 0f;
        terrainId = 0;
        if (!world.CellIsKnown(planeCode, q, r))
        {
            return false;
        }

        terrainId = world.CellTerrainId(planeCode, q, r);
        var terrainCost = simulation.KernelData.Terrain.TravelCost(terrainId, movementProfileId);
        if (terrainCost > 0f && planeCode == PlaneCodes.Physical && Data.RoughnessCostWeight > 0f)
        {
            // Broken ground within a terrain is slower than smooth ground
            // of the same terrain - the hex's real roughness fact, not a
            // second terrain table.
            terrainCost *= 1f + (Data.RoughnessCostWeight *
                world.PhysicalHexFact(q, r, PhysicalHexFactCodes.Roughness) / 255f);
        }

        var structureCost = Data.TravelStructureCost(
            State.TravelStructureId(planeCode, q, r),
            planeCode,
            movementProfileId);
        baseCost = SelectSurfaceBaseCost(terrainCost, structureCost);
        return baseCost > 0f;
    }

    private static float SelectSurfaceBaseCost(float terrainCost, float structureCost)
    {
        if (terrainCost <= 0f)
        {
            return structureCost > 0f ? structureCost : 0f;
        }

        return structureCost > 0f && structureCost < terrainCost
            ? structureCost
            : terrainCost;
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private static int CurrentQ(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? world.EntitySemanticQ(entityId) : world.EntityPhysicalQ(entityId);

    private static int CurrentZ(WorldState world, int entityId, int planeCode) =>
        planeCode == PlaneCodes.Semantic ? 0 : world.EntityPhysicalZ(entityId);

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
        5 => 1,
        _ => 0,
    };

    private static int DirectionZ(int direction) => direction switch
    {
        6 => 1,
        7 => -1,
        _ => 0,
    };
}
