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
            if (!TryScatteredDestination(simulation, candidate, radius, out var q, out var r))
            {
                continue;
            }

            var weight = LocationWeight(simulation, candidate.RuleSlot, candidate.PlaneCode, q, r, tick, 0f);
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
        VerbTargetCandidate candidate,
        int radius,
        out int q,
        out int r)
    {
        var dq = simulation.NextInclusive(-radius, radius);
        var minDr = global::System.Math.Max(-radius, -dq - radius);
        var maxDr = global::System.Math.Min(radius, -dq + radius);
        var dr = simulation.NextInclusive(minDr, maxDr);
        q = candidate.Q + dq;
        r = candidate.R + dr;
        return CoordinateAllowed(simulation.World, candidate.PlaneCode, q, r);
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
            MoveOneStepToward(simulation, world, entityId, rule, tick, planeCode, currentQ, currentR, targetQ, targetR))
        {
            State.MarkStepped(entityId, tick);
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

        if (MoveIfSelected(simulation, entityId, tick, planeCode, currentQ, currentR, selectedQ, selectedR))
        {
            State.MarkStepped(entityId, tick);
        }
    }

    private void AddDwellCandidate(
        Simulation simulation,
        WorldState world,
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
        if (!CoordinateAllowed(world, planeCode, q, r))
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
        var activityWeight = LocationWeight(simulation, rule, planeCode, q, r, tick, 0f);
        var weight = (pull * 4f) + activityWeight;
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
        int targetR)
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

        return MoveIfSelected(simulation, entityId, tick, planeCode, currentQ, currentR, selectedQ, selectedR);
    }

    private void TryAddStepCandidate(
        Simulation simulation,
        WorldState world,
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
        if (!StepCandidateAllowed(world, rule, planeCode, q, r, currentQ, currentR, targetQ, targetR, pathLooseness))
        {
            return;
        }

        var currentDistance = HexDistance(currentQ, currentR, targetQ, targetR);
        var candidateDistance = HexDistance(q, r, targetQ, targetR);
        var weight = StepCandidateWeight(simulation, rule, planeCode, q, r, tick, currentDistance, candidateDistance, pathLooseness);
        totalWeight += weight;
        if (simulation.NextFloat() * totalWeight <= weight)
        {
            selectedQ = q;
            selectedR = r;
        }

    }

    private bool StepCandidateAllowed(
        WorldState world,
        int rule,
        int planeCode,
        int q,
        int r,
        int currentQ,
        int currentR,
        int targetQ,
        int targetR,
        int pathLooseness)
    {
        if (!CoordinateAllowed(world, planeCode, q, r))
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
        var activityWeight = LocationWeight(simulation, rule, planeCode, q, r, tick, 0f);
        return progressWeight * 8f + activityWeight;
    }

    private static bool MoveIfSelected(
        Simulation simulation,
        int entityId,
        int tick,
        int planeCode,
        int currentQ,
        int currentR,
        int selectedQ,
        int selectedR)
    {
        if (selectedQ == currentQ && selectedR == currentR)
        {
            return false;
        }

        if (planeCode == PlaneCodes.Physical)
        {
            simulation.MoveEntityPhysical(entityId, selectedQ, selectedR, tick);
        }
        else
        {
            simulation.MoveEntitySemantic(entityId, selectedQ, selectedR, -selectedQ - selectedR, tick);
        }

        return true;
    }

    private bool CoordinateAllowed(WorldState world, int planeCode, int q, int r)
    {
        if (planeCode == PlaneCodes.Semantic)
        {
            return SemanticCoordinateAllowed(world, q, r);
        }

        return world.CellIsKnown(q, r);
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
