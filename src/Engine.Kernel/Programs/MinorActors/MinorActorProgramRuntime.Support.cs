namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramRuntime
{
    private void EmitWorkEvent(
        Simulation simulation,
        int actorId,
        FixedScalar delta,
        int tick)
    {
        var boundary = simulation.CultureLocality.CellAt(
            simulation.World,
            State.CultureId(actorId),
            State.PlaneCode(actorId),
            State.Q(actorId),
            State.R(actorId));
        simulation.EmitMinorActorWorked(
            tick,
            State.OccurrenceHandle(actorId),
            State.SourceEntityOccurrenceHandle(actorId),
            State.SourceRuleId(actorId),
            simulation.World.EntityWorkTier(State.SourceEntityId(actorId)),
            Data.AssignmentRadii[State.SourceRuleId(actorId)],
            State.CultureId(actorId),
            State.RoleCode(actorId),
            ActionCodeFor(actorId),
            State.TargetingCode(actorId),
            State.PlaneCode(actorId),
            State.Q(actorId),
            State.R(actorId),
            boundary.CellKindCode,
            boundary.LocalRawMass,
            boundary.ConnectedRawMass,
            State.Strength(actorId),
            delta);
    }

    private void EmitCreated(Simulation simulation, int actorId, int tick)
    {
        simulation.EmitMinorActorCreated(
            tick,
            State.OccurrenceHandle(actorId),
            State.SourceEntityOccurrenceHandle(actorId),
            State.SourceRuleId(actorId),
            State.BindingRelationshipOccurrenceHandle(actorId),
            State.CultureId(actorId),
            State.RoleCode(actorId),
            State.PlaneCode(actorId),
            State.Q(actorId),
            State.R(actorId),
            State.PostureCode(actorId),
            State.Strength(actorId));
    }

    private void EmitAssigned(
        Simulation simulation,
        int actorId,
        int previousTargetQ,
        int previousTargetR,
        int tick)
    {
        simulation.EmitMinorActorAssigned(
            tick,
            State.OccurrenceHandle(actorId),
            State.SourceEntityOccurrenceHandle(actorId),
            State.RoleCode(actorId),
            State.PlaneCode(actorId),
            previousTargetQ,
            previousTargetR,
            State.TargetQ(actorId),
            State.TargetR(actorId),
            State.TargetingCode(actorId),
            State.PostureCode(actorId),
            State.NextReplanTick(actorId),
            State.CommitmentUntilTick(actorId),
            State.Strength(actorId));
    }

    private void EmitMoved(
        Simulation simulation,
        int actorId,
        int fromQ,
        int fromR,
        int tick)
    {
        simulation.EmitMinorActorMoved(
            tick,
            State.OccurrenceHandle(actorId),
            State.SourceEntityOccurrenceHandle(actorId),
            State.CultureId(actorId),
            State.RoleCode(actorId),
            State.PlaneCode(actorId),
            fromQ,
            fromR,
            State.Q(actorId),
            State.R(actorId),
            State.TargetQ(actorId),
            State.TargetR(actorId),
            State.PostureCode(actorId),
            State.Strength(actorId));
    }

    private bool RetireActor(
        Simulation simulation,
        int actorId,
        int tick,
        int reasonCode)
    {
        if (!State.IsActive(actorId))
        {
            return false;
        }

        simulation.EmitMinorActorRetired(
            tick,
            State.OccurrenceHandle(actorId),
            State.SourceEntityOccurrenceHandle(actorId),
            State.SourceRuleId(actorId),
            State.BindingRelationshipOccurrenceHandle(actorId),
            State.CultureId(actorId),
            State.RoleCode(actorId),
            State.PlaneCode(actorId),
            State.Q(actorId),
            State.R(actorId),
            reasonCode,
            State.PostureCode(actorId),
            State.Strength(actorId));
        return State.Retire(actorId);
    }

    private int ActionCodeFor(int actorId) =>
        State.RoleCode(actorId) switch
        {
            MinorActorRoleCodes.RoadCrew => MinorActorActionCodes.RoadMaintenance,
            _ when State.TargetingCode(actorId) == MinorActorTargetingCodes.ContestCulture =>
                MinorActorActionCodes.ContestCulture,
            _ => MinorActorActionCodes.BuildCulture,
        };

    private FixedScalar EffectiveStrength(
        Simulation simulation,
        int sourceEntityId,
        int rule)
    {
        var world = simulation.World;
        var workTier = world.EntityWorkTier(sourceEntityId);
        var kindId = world.EntityKindId(sourceEntityId);
        var workerBonus = world.KindHasStages(kindId)
            ? world.StageWorkerBonus(world.StageRuleFor(kindId, workTier))
            : workTier;
        var stageMultiplier = 1 +
            (global::System.Math.Max(0, workerBonus) * StageStrengthStep);
        var slotMultiplier = 1 +
            ((global::System.Math.Max(
                1,
                simulation.CultureSlots.CausalSpendLimitMultiplier(sourceEntityId)) - 1) *
             StageStrengthStep);
        return Data.BaseStrengths[rule] * stageMultiplier * slotMultiplier;
    }

    private static bool SourceProbeIsDue(
        int sourceEntityId,
        int rule,
        int replanTicks,
        int tick) =>
        tick <= 1 || (tick + sourceEntityId + rule) % replanTicks == 0;

    private static bool TryResolveEntityPosition(
        WorldState world,
        int entityId,
        int planeCode,
        out int q,
        out int r)
    {
        if (!world.EntityIsActive(entityId))
        {
            q = 0;
            r = 0;
            return false;
        }

        if (planeCode == PlaneCodes.Physical)
        {
            q = world.EntityPhysicalQ(entityId);
            r = world.EntityPhysicalR(entityId);
            return true;
        }

        if (planeCode == PlaneCodes.Semantic)
        {
            q = world.EntitySemanticQ(entityId);
            r = world.EntitySemanticR(entityId);
            return true;
        }

        q = 0;
        r = 0;
        return false;
    }

    private static void SelectAssignment(
        int q,
        int r,
        int targetingCode,
        FixedScalar score,
        ref MinorActorAssignment selected)
    {
        if (score <= 0 ||
            (score < selected.Score ||
             (score == selected.Score && CellKey(q, r) >= CellKey(selected.Q, selected.R))))
        {
            return;
        }

        selected = new MinorActorAssignment(q, r, targetingCode, score);
    }

    private static long CellKey(int q, int r) =>
        ((long)(uint)(q ^ global::System.Int32.MinValue) << 32) |
        (uint)(r ^ global::System.Int32.MinValue);

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var dq = aq - bq;
        var dr = ar - br;
        return (global::System.Math.Abs(dq) +
                global::System.Math.Abs(dr) +
                global::System.Math.Abs(dq + dr)) / 2;
    }

    private static bool ThrowUnknownRole(out MinorActorAssignment assignment)
    {
        assignment = default;
        throw new global::System.InvalidOperationException();
    }

    private readonly record struct MinorActorAssignment(
        int Q,
        int R,
        int TargetingCode,
        FixedScalar Score);
}
