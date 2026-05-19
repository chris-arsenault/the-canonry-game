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

            var weight = MovementShapeCellWeight(simulation, candidate.RuleSlot, candidate.PlaneCode, q, r);
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
        _ = entityId;
        _ = intentId;
        _ = rule;
        return 0;
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
            TryDwellRoam(simulation, entityId, rule, tick, planeCode, targetQ, targetR);
            return;
        }

        if (!State.StepIsReady(entityId, tick))
        {
            return;
        }

        var result = simulation.Movement.Navigate(simulation, entityId, planeCode, targetQ, targetR, tick);
        if (result.Moved)
        {
            ApplyTravelInfluence(simulation, entityId, rule, result, tick);
            State.MarkStepped(entityId, tick, result.StepCost);
            return;
        }

        if (float.IsInfinity(result.StepCost))
        {
            ClearIntentAndPushRecent(entityId, tick);
        }
    }

    private void TryDwellRoam(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int planeCode,
        int anchorQ,
        int anchorR)
    {
        if (!State.StepIsReady(entityId, tick))
        {
            return;
        }

        var localityRadius = DwellLocalityRadius(rule);
        var result = simulation.Movement.Wander(
            simulation,
            entityId,
            planeCode,
            tick,
            anchorQ,
            anchorR,
            localityRadius);
        if (result.Moved)
        {
            ApplyTravelInfluence(simulation, entityId, rule, result, tick);
            State.MarkStepped(entityId, tick, result.StepCost);
        }
    }

    private void ApplyTravelInfluence(
        Simulation simulation,
        int entityId,
        int rule,
        MovementMoveResult result,
        int tick)
    {
        var delta = Data.InfluenceDelta(rule);
        if (delta <= 0f)
        {
            return;
        }

        ApplyInfluenceAround(
            simulation,
            entityId,
            result.PlaneCode,
            result.ToQ,
            result.ToR,
            Data.InfluenceRadius(rule),
            delta,
            tick);
    }

    private int DwellLocalityRadius(int rule)
    {
        var radius = Data.TargetRadius(rule) + 1;
        return radius < 1 ? 1 : radius;
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
        if (planeCode == PlaneCodes.Semantic)
        {
            return SemanticCoordinateAllowed(simulation.World, q, r) &&
                simulation.Movement.CanEnter(simulation, planeCode, entityId, q, r) &&
                MovementTargetReachable(simulation, entityId, rule, planeCode, q, r, tick);
        }

        return simulation.Movement.CanEnter(simulation, entityId, q, r) &&
            MovementTargetReachable(simulation, entityId, rule, planeCode, q, r, tick);
    }

    private float MovementShapeCellWeight(Simulation simulation, int rule, int planeCode, int q, int r)
    {
        var saturation = CellSaturationDivisor(simulation, planeCode, q, r);
        return Data.Weight(rule) * saturation;
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
