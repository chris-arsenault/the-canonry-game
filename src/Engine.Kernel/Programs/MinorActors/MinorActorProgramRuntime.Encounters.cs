namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramRuntime
{
    private void ResolveLocalEncounters(Simulation simulation, int actorId, int tick)
    {
        if (!State.IsActive(actorId))
        {
            return;
        }

        ResolveEncountersInCell(
            simulation,
            actorId,
            State.PlaneCode(actorId),
            State.Q(actorId),
            State.R(actorId),
            tick);
        for (var direction = 0; direction < NeighborDQ.Length; direction++)
        {
            ResolveEncountersInCell(
                simulation,
                actorId,
                State.PlaneCode(actorId),
                State.Q(actorId) + NeighborDQ[direction],
                State.R(actorId) + NeighborDR[direction],
                tick);
        }
    }

    private void ResolveEncountersInCell(
        Simulation simulation,
        int actorId,
        int planeCode,
        int q,
        int r,
        int tick)
    {
        if (!simulation.World.TryGetCellId(planeCode, q, r, z: 0, out var cellId))
        {
            return;
        }

        for (var otherId = State.FirstActorInCell(cellId);
             otherId > 0;
             otherId = State.NextActorInCell(otherId))
        {
            if (otherId == actorId ||
                !State.IsActive(otherId) ||
                State.PlaneCode(otherId) != planeCode ||
                State.OccurrenceHandle(otherId) <= State.OccurrenceHandle(actorId))
            {
                continue;
            }

            ResolveEncounterPair(simulation, actorId, otherId, tick);
        }
    }

    private void ResolveEncounterPair(
        Simulation simulation,
        int actorId,
        int otherId,
        int tick)
    {
        var actorHandle = State.OccurrenceHandle(actorId);
        var otherHandle = State.OccurrenceHandle(otherId);
        var pairKey = ((long)(uint)actorHandle << 32) | (uint)otherHandle;
        var friendly = State.CultureId(actorId) == State.CultureId(otherId);
        var chance = friendly ? FriendlySupportChance : HostileClashChance;
        if (simulation.RandomScalar(
                pairKey,
                State.RoleCode(actorId),
                KernelRandomPurposeCodes.MinorActorEncounter,
                State.RoleCode(otherId)) >= chance)
        {
            return;
        }

        if (friendly)
        {
            ResolveFriendlyEncounter(simulation, actorId, otherId, tick);
            return;
        }

        ResolveHostileEncounter(simulation, actorId, otherId, tick);
    }

    private void ResolveFriendlyEncounter(
        Simulation simulation,
        int actorId,
        int otherId,
        int tick)
    {
        var support = FixedScalar.Min(
            State.Strength(actorId),
            State.Strength(otherId)) * FriendlySupportShare;
        State.SetWorkCharge(actorId, State.WorkCharge(actorId) + support);
        State.SetWorkCharge(otherId, State.WorkCharge(otherId) + support);
        simulation.EmitMinorActorEncountered(
            tick,
            State.OccurrenceHandle(actorId),
            State.OccurrenceHandle(otherId),
            State.CultureId(actorId),
            State.CultureId(otherId),
            State.RoleCode(actorId),
            State.RoleCode(otherId),
            State.PlaneCode(actorId),
            State.Q(actorId),
            State.R(actorId),
            MinorActorEncounterCodes.FriendlySupport,
            State.PostureCode(actorId),
            State.PostureCode(otherId),
            State.Strength(actorId),
            State.Strength(otherId),
            support);
    }

    private void ResolveHostileEncounter(
        Simulation simulation,
        int actorId,
        int otherId,
        int tick)
    {
        var campaignClash =
            State.RoleCode(actorId) == MinorActorRoleCodes.CampaignColumn ||
            State.RoleCode(otherId) == MinorActorRoleCodes.CampaignColumn;
        var damageShare = campaignClash ? CampaignDamageShare : HostileDamageShare;
        var actorStrength = State.Strength(actorId);
        var otherStrength = State.Strength(otherId);
        State.SetStrength(
            actorId,
            FixedScalar.Max(0, actorStrength - (otherStrength * damageShare)));
        State.SetStrength(
            otherId,
            FixedScalar.Max(0, otherStrength - (actorStrength * damageShare)));
        DisruptIfExhausted(actorId, tick);
        DisruptIfExhausted(otherId, tick);

        simulation.EmitMinorActorEncountered(
            tick,
            State.OccurrenceHandle(actorId),
            State.OccurrenceHandle(otherId),
            State.CultureId(actorId),
            State.CultureId(otherId),
            State.RoleCode(actorId),
            State.RoleCode(otherId),
            State.PlaneCode(actorId),
            State.Q(actorId),
            State.R(actorId),
            campaignClash
                ? MinorActorEncounterCodes.CampaignDisruption
                : MinorActorEncounterCodes.HostileClash,
            State.PostureCode(actorId),
            State.PostureCode(otherId),
            State.Strength(actorId),
            State.Strength(otherId),
            (otherStrength * damageShare) + (actorStrength * damageShare));
    }

    private void DisruptIfExhausted(int actorId, int tick)
    {
        if (State.Strength(actorId) > 0)
        {
            return;
        }

        State.ClearAssignment(actorId, tick);
        State.SetPosture(actorId, MinorActorPostureCodes.Disrupted);
    }
}
