namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private VerbTargetCandidate ScatterDestination(
        Simulation simulation,
        int entityId,
        VerbTargetCandidate candidate,
        int tick)
    {
        if (Data.OperationCode(candidate.RuleSlot) != ProgramOpcodeCodes.MoveActor)
        {
            return candidate;
        }

        var radius = DestinationScatterRadius(entityId, State.NextIntentId(), candidate.RuleSlot);
        if (radius == 0)
        {
            return candidate;
        }

        var selected = candidate;
        var totalWeight = 0f;
        var attempts = Data.CandidateSampleCount(candidate.RuleSlot);
        for (var i = 0; i < attempts; i++)
        {
            if (!TryScatteredDestination(simulation, entityId, candidate, radius, tick, out var q, out var r))
            {
                continue;
            }

            var weight = LocationWeight(simulation, entityId, candidate.RuleSlot, candidate.PlaneCode, q, r, tick, 0f);
            totalWeight += weight;
            if (simulation.NextFloat() * totalWeight <= weight)
            {
                selected = candidate with { Q = q, R = r, S = -q - r };
            }
        }

        return selected;
    }

    private bool TryScatteredDestination(
        Simulation simulation,
        int entityId,
        VerbTargetCandidate candidate,
        int radius,
        int tick,
        out int q,
        out int r)
    {
        var dq = simulation.NextInclusive(-radius, radius);
        var minDr = global::System.Math.Max(-radius, -dq - radius);
        var maxDr = global::System.Math.Min(radius, -dq + radius);
        var dr = simulation.NextInclusive(minDr, maxDr);
        q = candidate.Q + dq;
        r = candidate.R + dr;
        return CoordinateAllowed(simulation, entityId, candidate.RuleSlot, candidate.PlaneCode, q, r, tick);
    }

    private int DestinationScatterRadius(int entityId, int intentId, int rule)
    {
        if (Data.TargetModeCode(rule) is TargetSelectorCodes.EntityKind
            or TargetSelectorCodes.RelationshipTarget)
        {
            var baseRadius = Data.TargetRadius(rule);
            return baseRadius <= 0 ? 0 : baseRadius + EntityJitter(entityId, intentId, rule, maxExclusive: 2);
        }

        return Data.TargetModeCode(rule) == TargetSelectorCodes.Terrain
            ? Data.TargetRadius(rule)
            : 0;
    }

    private int IntentDwellTicks(int entityId, int intentId, int rule)
    {
        var dwellTicks = Data.DwellTicks(rule);
        if (dwellTicks <= 1)
        {
            return dwellTicks;
        }

        var minimum = dwellTicks / 2;
        var maximum = dwellTicks + ((dwellTicks + 1) / 2);
        return minimum + EntityJitter(entityId, intentId, rule + 17, maximum - minimum + 1);
    }

    private int IntentStepIntervalTicks(int entityId, int intentId, int rule)
    {
        if (Data.OperationCode(rule) != ProgramOpcodeCodes.MoveActor)
        {
            return 1;
        }

        var bucket = EntityJitter(entityId, intentId, rule + 31, maxExclusive: 10);
        if (bucket < 7)
        {
            return 1;
        }

        return bucket < 9 ? 2 : 3;
    }

    private int IntentPathLoosenessPermille(int entityId, int intentId, int rule)
    {
        if (Data.OperationCode(rule) != ProgramOpcodeCodes.MoveActor)
        {
            return 0;
        }

        return 100 + (EntityJitter(entityId, intentId, rule + 43, maxExclusive: 5) * 75);
    }

    private void MoveTowardIntent(Simulation simulation, int entityId, int rule, int tick)
    {
        var world = simulation.World;
        var planeCode = Data.PlaneCode(rule);
        var targetQ = State.TargetQ(entityId);
        var targetR = State.TargetR(entityId);
        var currentQ = CurrentQ(world, entityId, planeCode);
        var currentR = CurrentR(world, entityId, planeCode);
        if (HexDistance(currentQ, currentR, targetQ, targetR) <= Data.TargetRadius(rule))
        {
            State.MarkReached(entityId, tick, tick + State.IntentDwellTicks(entityId));
            TryDwellRoam(simulation, world, entityId, rule, tick, planeCode, currentQ, currentR, targetQ, targetR);
            return;
        }

        if (State.StepIsReady(entityId, tick) &&
            MoveOneStepToward(simulation, world, entityId, rule, tick, planeCode, currentQ, currentR, targetQ, targetR, out var stepCost))
        {
            State.MarkStepped(entityId, tick, stepCost);
        }
    }

    private void TryDwellRoam(
        Simulation simulation,
        WorldState world,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int currentQ,
        int currentR,
        int targetQ,
        int targetR)
    {
        if (!State.StepIsReady(entityId, tick))
        {
            return;
        }

        var selectedQ = currentQ;
        var selectedR = currentR;
        var totalWeight = 0f;
        for (var direction = 0; direction < DirectionCount; direction++)
        {
            AddDwellCandidate(
                simulation,
                world,
                entityId,
                rule,
                tick,
                planeCode,
                currentQ,
                currentR,
                targetQ,
                targetR,
                direction,
                ref totalWeight,
                ref selectedQ,
                ref selectedR);
        }

        if (MoveIfSelected(simulation, entityId, tick, planeCode, currentQ, currentR, selectedQ, selectedR, out var stepCost))
        {
            State.MarkStepped(entityId, tick, stepCost);
        }
    }

    private void AddDwellCandidate(
        Simulation simulation,
        WorldState world,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int currentQ,
        int currentR,
        int targetQ,
        int targetR,
        int direction,
        ref float totalWeight,
        ref int selectedQ,
        ref int selectedR)
    {
        var q = currentQ + DirectionQ(direction);
        var r = currentR + DirectionR(direction);
        if (!CoordinateAllowed(simulation, entityId, rule, planeCode, q, r, tick))
        {
            return;
        }

        var distance = HexDistance(q, r, targetQ, targetR);
        var localityRadius = DwellLocalityRadius(rule);
        if (distance > localityRadius)
        {
            return;
        }

        var pull = 1f / (1f + distance);
        var activityWeight = LocationWeight(simulation, entityId, rule, planeCode, q, r, tick, 0f);
        var terrainWeight = MovementCostWeight(simulation, entityId, planeCode, q, r, tick);
        var weight = ((pull * 4f) + activityWeight) * terrainWeight;
        totalWeight += weight;
        if (simulation.NextFloat() * totalWeight <= weight)
        {
            selectedQ = q;
            selectedR = r;
        }
    }

    private int DwellLocalityRadius(int rule)
    {
        var radius = Data.TargetRadius(rule) + 1;
        return radius < 1 ? 1 : radius;
    }

    private bool MoveOneStepToward(
        Simulation simulation,
        WorldState world,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int currentQ,
        int currentR,
        int targetQ,
        int targetR,
        out float stepCost)
    {
        var totalWeight = 0f;
        var selectedQ = currentQ;
        var selectedR = currentR;
        var pathLooseness = State.PathLoosenessPermille(entityId);
        for (var direction = 0; direction < DirectionCount; direction++)
        {
            TryAddStepCandidate(
                simulation,
                world,
                entityId,
                rule,
                tick,
                planeCode,
                currentQ,
                currentR,
                targetQ,
                targetR,
                direction,
                pathLooseness,
                ref totalWeight,
                ref selectedQ,
                ref selectedR);
        }

        return MoveIfSelected(simulation, entityId, tick, planeCode, currentQ, currentR, selectedQ, selectedR, out stepCost);
    }

    private void TryAddStepCandidate(
        Simulation simulation,
        WorldState world,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int currentQ,
        int currentR,
        int targetQ,
        int targetR,
        int direction,
        int pathLooseness,
        ref float totalWeight,
        ref int selectedQ,
        ref int selectedR)
    {
        var q = currentQ + DirectionQ(direction);
        var r = currentR + DirectionR(direction);
        if (!StepCandidateAllowed(simulation, entityId, rule, planeCode, q, r, tick, currentQ, currentR, targetQ, targetR, pathLooseness))
        {
            return;
        }

        var currentDistance = HexDistance(currentQ, currentR, targetQ, targetR);
        var candidateDistance = HexDistance(q, r, targetQ, targetR);
        var weight = StepCandidateWeight(simulation, entityId, rule, planeCode, q, r, tick, currentDistance, candidateDistance, pathLooseness);
        totalWeight += weight;
        if (simulation.NextFloat() * totalWeight <= weight)
        {
            selectedQ = q;
            selectedR = r;
        }

    }

    private bool StepCandidateAllowed(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int q,
        int r,
        int tick,
        int currentQ,
        int currentR,
        int targetQ,
        int targetR,
        int pathLooseness)
    {
        if (!CoordinateAllowed(simulation, entityId, rule, planeCode, q, r, tick))
        {
            return false;
        }

        var currentDistance = HexDistance(currentQ, currentR, targetQ, targetR);
        var candidateDistance = HexDistance(q, r, targetQ, targetR);
        return candidateDistance < currentDistance ||
            (candidateDistance == currentDistance &&
                pathLooseness > 0 &&
                currentDistance > Data.TargetRadius(rule) + 2);
    }

    private float StepCandidateWeight(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int q,
        int r,
        int tick,
        int currentDistance,
        int candidateDistance,
        int pathLooseness)
    {
        var progressWeight = candidateDistance < currentDistance
            ? 1f / (1f + candidateDistance)
            : pathLooseness / 1000f;
        var activityWeight = LocationWeight(simulation, entityId, rule, planeCode, q, r, tick, 0f);
        var terrainWeight = MovementCostWeight(simulation, entityId, planeCode, q, r, tick);
        return ((progressWeight * 8f) + activityWeight) * terrainWeight;
    }

    private bool MoveIfSelected(
        Simulation simulation,
        int entityId,
        int tick,
        int planeCode,
        int currentQ,
        int currentR,
        int selectedQ,
        int selectedR,
        out float stepCost)
    {
        stepCost = 1f;
        if (selectedQ == currentQ && selectedR == currentR)
        {
            return false;
        }

        if (planeCode == PlaneCodes.Physical)
        {
            var result = simulation.Movement.TryMovePhysical(
                simulation,
                entityId,
                selectedQ,
                selectedR,
                tick,
                MovementCauseCodes.Travel);
            stepCost = result.StepCost;
            return result.Moved;
        }

        var semanticResult = simulation.Movement.TryMoveSemantic(
            simulation,
            entityId,
            selectedQ,
            selectedR,
            -selectedQ - selectedR,
            tick,
            MovementCauseCodes.Travel);
        stepCost = semanticResult.StepCost;
        return semanticResult.Moved;
    }

    private bool CoordinateAllowed(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int q,
        int r,
        int tick)
    {
        _ = rule;
        _ = tick;
        if (planeCode == PlaneCodes.Semantic)
        {
            return SemanticCoordinateAllowed(simulation.World, q, r) &&
                simulation.Movement.CanEnter(simulation, planeCode, simulation.World.EntityKindId(entityId), q, r);
        }

        return simulation.Movement.CanEnter(simulation, simulation.World.EntityKindId(entityId), q, r);
    }

    private float MovementCostWeight(
        Simulation simulation,
        int entityId,
        int planeCode,
        int q,
        int r,
        int tick)
    {
        var travelCost = simulation.Movement.TravelCost(
            simulation,
            planeCode,
            simulation.World.EntityKindId(entityId),
            q,
            r,
            tick);
        if (float.IsInfinity(travelCost) || travelCost <= 0f)
        {
            return 0f;
        }

        return 1f / global::System.MathF.Max(0.25f, travelCost);
    }

    private static int EntityJitter(int entityId, int intentId, int salt, int maxExclusive)
    {
        if (maxExclusive <= 1)
        {
            return 0;
        }

        unchecked
        {
            var hash = (uint)entityId * 2654435761u;
            hash ^= (uint)intentId * 2246822519u;
            hash ^= (uint)salt * 3266489917u;
            hash ^= hash >> 16;
            return (int)(hash % (uint)maxExclusive);
        }
    }
}
