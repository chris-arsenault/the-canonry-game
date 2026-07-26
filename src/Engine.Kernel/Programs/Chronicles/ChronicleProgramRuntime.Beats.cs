namespace Engine.Kernel;

internal sealed partial class ChronicleProgramRuntime
{
    private void ProcessPendingExecutions(Simulation simulation, WorldState world, int tick)
    {
        for (var pending = 0; pending < State.PendingCount; pending++)
        {
            var kind = State.PendingKinds[pending];
            _ = world.TryResolveEntityOccurrence(
                State.PendingActorOccurrenceHandles[pending],
                out var actorId);
            _ = world.TryResolveEntityOccurrence(
                State.PendingTargetOccurrenceHandles[pending],
                out var targetId);
            var producerId = State.PendingProducerIds[pending];
            var succeeded = State.PendingSucceeded[pending] != 0;
            for (var chronicle = 1; chronicle <= State.HighWatermark; chronicle++)
            {
                if (!State.Active[chronicle])
                {
                    continue;
                }

                ApplyExecutionToChronicle(simulation, world, chronicle, kind, actorId, targetId, producerId, succeeded, tick);
            }
        }

        State.PendingCount = 0;
    }

    private void ApplyExecutionToChronicle(
        Simulation simulation,
        WorldState world,
        int chronicle,
        int pendingKind,
        int actorId,
        int targetId,
        int producerId,
        bool succeeded,
        int tick)
    {
        if (pendingKind == ChroniclePendingKindCodes.GambitExecution &&
            CastContains(chronicle, actorId) && CastContains(chronicle, targetId) && actorId != targetId)
        {
            AddHeat(chronicle, AmbientPairHeat);
        }

        var archetype = State.ArchetypeSlots[chronicle];
        var beatStart = Data.ArchetypeBeatStarts[archetype];
        var beatEnd = beatStart + Data.ArchetypeBeatCounts[archetype];
        for (var beat = beatStart; beat < beatEnd; beat++)
        {
            var ordinal = beat - beatStart;
            if (!BeatIsOpen(chronicle, ordinal) ||
                !ExecutionStrikes(chronicle, beat, pendingKind, actorId, targetId, producerId, succeeded) ||
                !TryJoinOnStrike(world, chronicle, archetype, beat, actorId, targetId))
            {
                continue;
            }

            Strike(simulation, world, chronicle, beat, ordinal, tick);
            if (!State.Active[chronicle])
            {
                return;
            }
        }
    }

    /// <summary>
    /// Entrance beats bind the outside party into an open role; a
    /// convergence entrance without a qualified entrant is no strike.
    /// </summary>
    private bool TryJoinOnStrike(WorldState world, int chronicle, int archetype, int beat, int actorId, int targetId)
    {
        if (Data.BeatJoinRoles[beat] == 0)
        {
            return true;
        }

        var joiner = JoinCandidate(world, chronicle, archetype, beat, actorId, targetId);
        if (joiner == 0)
        {
            return false;
        }

        SetRoleEntity(chronicle, Data.BeatJoinRoles[beat], joiner);
        return true;
    }

    /// <summary>
    /// The joining party of an entrance strike: the outsider acting on a
    /// role, or the outside target of a role's deed. Must be alive, of the
    /// open role's kind, not already cast, and the role must be empty.
    /// </summary>
    private int JoinCandidate(WorldState world, int chronicle, int archetype, int beat, int actorId, int targetId)
    {
        var joinRole = Data.BeatJoinRoles[beat];
        var candidate = Data.BeatStrikeKinds[beat] switch
        {
            ChronicleStrikeCodes.OutsiderGambitOnRole => actorId,
            ChronicleStrikeCodes.RoleGambit => targetId,
            _ => 0,
        };
        if (candidate <= 0 ||
            !world.EntityIsActive(candidate) ||
            CastContains(chronicle, candidate) ||
            RoleEntity(chronicle, joinRole) != 0)
        {
            return 0;
        }

        var roleKindId = Data.RoleKindIds[Data.ArchetypeRoleStarts[archetype] + joinRole];
        return world.EntityKindId(candidate) == roleKindId ? candidate : 0;
    }

    private bool ExecutionStrikes(int chronicle, int beat, int pendingKind, int actorId, int targetId, int producerId, bool succeeded)
    {
        var producerFilter = Data.BeatStrikeGambitIds[beat];
        if (producerFilter != 0 && producerFilter != producerId)
        {
            return false;
        }

        // A failed attempt only strikes beats authored to accept failure -
        // narratives must never claim a deed the world refused.
        if (!succeeded && Data.BeatStrikeAcceptFailure[beat] == 0)
        {
            return false;
        }

        if (pendingKind == ChroniclePendingKindCodes.GeneratorFire)
        {
            return Data.BeatStrikeKinds[beat] == ChronicleStrikeCodes.GeneratorOnRole &&
                targetId > 0 &&
                RoleEntity(chronicle, Data.BeatStrikeTargetRoles[beat]) == targetId;
        }

        return GambitStrikeMatches(chronicle, beat, actorId, targetId);
    }

    private bool GambitStrikeMatches(int chronicle, int beat, int actorId, int targetId) =>
        Data.BeatStrikeKinds[beat] switch
        {
            ChronicleStrikeCodes.RolePairGambit =>
                RoleEntity(chronicle, Data.BeatStrikeActorRoles[beat]) == actorId &&
                RoleEntity(chronicle, Data.BeatStrikeTargetRoles[beat]) == targetId,
            ChronicleStrikeCodes.OutsiderGambitOnRole =>
                RoleEntity(chronicle, Data.BeatStrikeTargetRoles[beat]) == targetId &&
                !CastContains(chronicle, actorId),
            ChronicleStrikeCodes.RoleGambit =>
                RoleEntity(chronicle, Data.BeatStrikeActorRoles[beat]) == actorId,
            _ => false,
        };

    /// <summary>
    /// Evaluates openings for a freshly cast chronicle so Always-open beats
    /// charge the world and accept strikes from the very first run.
    /// </summary>
    internal void OpenEligibleBeats(WorldState world, int chronicle)
    {
        var archetype = State.ArchetypeSlots[chronicle];
        var beatStart = Data.ArchetypeBeatStarts[archetype];
        var beatEnd = beatStart + Data.ArchetypeBeatCounts[archetype];
        for (var beat = beatStart; beat < beatEnd; beat++)
        {
            var ordinal = beat - beatStart;
            if (BeatIsOpen(chronicle, ordinal) || BeatIsStruck(chronicle, ordinal))
            {
                continue;
            }

            var opens = ConditionHolds(
                world,
                chronicle,
                Data.BeatOpenKinds[beat],
                Data.BeatOpenRoleA[beat],
                Data.BeatOpenRoleB[beat],
                Data.BeatOpenRelOrAttrIds[beat],
                Data.BeatOpenValues[beat]);
            if (opens)
            {
                State.OpenMasks[chronicle] |= 1L << ordinal;
            }
        }
    }

    private void StepChronicle(Simulation simulation, WorldState world, int chronicle, int tick)
    {
        var archetype = State.ArchetypeSlots[chronicle];
        var beatStart = Data.ArchetypeBeatStarts[archetype];
        var beatEnd = beatStart + Data.ArchetypeBeatCounts[archetype];
        for (var beat = beatStart; beat < beatEnd; beat++)
        {
            var ordinal = beat - beatStart;
            if (BeatIsStruck(chronicle, ordinal))
            {
                continue;
            }

            OpenAndStrikeFromState(simulation, world, chronicle, beat, ordinal, tick);
            if (!State.Active[chronicle])
            {
                return;
            }
        }

        TryFade(simulation, chronicle, tick);
    }

    private void OpenAndStrikeFromState(
        Simulation simulation,
        WorldState world,
        int chronicle,
        int beat,
        int ordinal,
        int tick)
    {
        if (!BeatIsOpen(chronicle, ordinal))
        {
            var opens = ConditionHolds(
                world,
                chronicle,
                Data.BeatOpenKinds[beat],
                Data.BeatOpenRoleA[beat],
                Data.BeatOpenRoleB[beat],
                Data.BeatOpenRelOrAttrIds[beat],
                Data.BeatOpenValues[beat]);
            if (!opens)
            {
                return;
            }

            State.OpenMasks[chronicle] |= 1L << ordinal;
        }

        if (Data.BeatStrikeKinds[beat] != ChronicleStrikeCodes.StateFact)
        {
            return;
        }

        var strikes = ConditionHolds(
            world,
            chronicle,
            Data.BeatStrikeFactKinds[beat],
            Data.BeatStrikeFactRoleA[beat],
            Data.BeatStrikeFactRoleB[beat],
            Data.BeatStrikeFactRelOrAttrIds[beat],
            Data.BeatStrikeFactValues[beat]);
        if (strikes)
        {
            Strike(simulation, world, chronicle, beat, ordinal, tick);
        }
    }

    private void Strike(Simulation simulation, WorldState world, int chronicle, int beat, int ordinal, int tick)
    {
        State.StruckMasks[chronicle] |= 1L << ordinal;
        State.OpenMasks[chronicle] &= ~(1L << ordinal);
        State.LastStrikeTicks[chronicle] = tick;
        AddHeat(chronicle, Data.BeatHeatGains[beat]);
        EmitChronicleChanged(
            simulation,
            chronicle,
            ChronicleEventTypeCodes.BeatStruck,
            ordinal,
            Data.BeatCodeIds[beat],
            tick);
        var outcomeCodeId = Data.BeatTerminalOutcomeCodeIds[beat];
        if (outcomeCodeId != 0)
        {
            Resolve(simulation, chronicle, outcomeCodeId, tick);
        }
    }

    private void TryFade(Simulation simulation, int chronicle, int tick)
    {
        // Slow-burn archetypes may author longer windows; 0 keeps defaults.
        var archetype = State.ArchetypeSlots[chronicle];
        var grace = Data.ArchetypeFadeGraceTicks[archetype];
        var quiet = Data.ArchetypeFadeQuietTicks[archetype];
        if (State.Heat[chronicle] >= FadeHeatThreshold ||
            tick - State.StartTicks[chronicle] <= (grace > 0 ? grace : FadeGraceTicks) ||
            tick - State.LastStrikeTicks[chronicle] <= (quiet > 0 ? quiet : FadeQuietTicks))
        {
            return;
        }

        Resolve(simulation, chronicle, Data.ArchetypeFadeOutcomeCodeIds[archetype], tick);
    }

    private void Resolve(Simulation simulation, int chronicle, int outcomeCodeId, int tick)
    {
        EmitChronicleChanged(simulation, chronicle, ChronicleEventTypeCodes.Resolved, 0, outcomeCodeId, tick);
        if (State.Heat[chronicle] >= LegendaryHeatThreshold)
        {
            State.LegendaryResolutionCount++;
        }

        RememberCast(simulation.World, chronicle, tick);
        State.Active[chronicle] = false;
        State.ArchetypeActiveCounts[State.ArchetypeSlots[chronicle]]--;
    }

    /// <summary>
    /// Resolved cast members enter the suppression ring so the same entities
    /// are not immediately recast as primaries of a fresh tale.
    /// </summary>
    private void RememberCast(WorldState world, int chronicle, int tick)
    {
        for (var role = 0; role < ChronicleProgramData.MaxRolesPerArchetype; role++)
        {
            var entityId = RoleEntity(chronicle, role);
            if (entityId <= 0)
            {
                continue;
            }

            State.RecentCastEntityOccurrenceHandles[State.RecentCastNext] =
                world.EntityOccurrenceHandle(entityId);
            State.RecentCastTicks[State.RecentCastNext] = tick;
            State.RecentCastHeat[State.RecentCastNext] = State.Heat[chronicle];
            State.RecentCastNext = (State.RecentCastNext + 1) % ChronicleProgramState.SuppressionRingCapacity;
        }
    }

    private bool RecentlyChronicled(WorldState world, int entityId, int tick)
    {
        var occurrenceHandle = world.EntityOccurrenceHandle(entityId);
        for (var slot = 0; slot < ChronicleProgramState.SuppressionRingCapacity; slot++)
        {
            if (State.RecentCastEntityOccurrenceHandles[slot] == occurrenceHandle &&
                tick - State.RecentCastTicks[slot] < SuppressionTicks)
            {
                return true;
            }
        }

        return false;
    }

    private void AddHeat(int chronicle, FixedScalar delta)
    {
        var heat = State.Heat[chronicle] + delta;
        State.Heat[chronicle] = heat > MaxHeat ? MaxHeat : heat;
    }

    private bool ConditionHolds(
        WorldState world,
        int chronicle,
        int conditionKind,
        int roleA,
        int roleB,
        int relOrAttrId,
        FixedScalar value)
    {
        switch (conditionKind)
        {
            case ChronicleConditionCodes.Always:
                return true;
            case ChronicleConditionCodes.RolePairRelationship:
                return RolePairRelationshipHolds(world, chronicle, roleA, roleB, relOrAttrId, value);
            case ChronicleConditionCodes.RolePairRelationshipAbsent:
                return !RolePairRelationshipHolds(world, chronicle, roleA, roleB, relOrAttrId, value);
            case ChronicleConditionCodes.HeatAtLeast:
                return State.Heat[chronicle] >= value;
            case ChronicleConditionCodes.HeatBelow:
                return State.Heat[chronicle] < value;
            default:
                return RoleConditionHolds(world, chronicle, conditionKind, roleA, roleB, relOrAttrId, value);
        }
    }

    private bool RoleConditionHolds(
        WorldState world,
        int chronicle,
        int conditionKind,
        int roleA,
        int roleB,
        int relOrAttrId,
        FixedScalar value)
    {
        switch (conditionKind)
        {
            case ChronicleConditionCodes.RoleHistorical:
                return (State.RetiredRoleMasks[chronicle] & (1L << roleA)) != 0;
            case ChronicleConditionCodes.RoleAttributeBelow:
                return RoleAttributeHolds(world, chronicle, roleA, relOrAttrId, value, below: true);
            case ChronicleConditionCodes.RoleAttributeAtLeast:
                return RoleAttributeHolds(world, chronicle, roleA, relOrAttrId, value, below: false);
            case ChronicleConditionCodes.RolePairWithinDistance:
                return RolePairDistanceHolds(world, chronicle, roleA, roleB, value, within: true);
            case ChronicleConditionCodes.RolePairBeyondDistance:
                return RolePairDistanceHolds(world, chronicle, roleA, roleB, value, within: false);
            default:
                return false;
        }
    }

    private bool RoleAttributeHolds(
        WorldState world,
        int chronicle,
        int roleA,
        int attributeId,
        FixedScalar value,
        bool below)
    {
        var subjectId = RoleEntity(chronicle, roleA);
        if (!RoleRemainsActive(chronicle, roleA) || !world.EntityIsActive(subjectId))
        {
            return false;
        }

        var current = world.GetAttribute(subjectId, attributeId);
        return below ? current < value : current >= value;
    }

    private bool RolePairRelationshipHolds(
        WorldState world,
        int chronicle,
        int roleA,
        int roleB,
        int relKindId,
        FixedScalar minStrength)
    {
        var sourceId = RoleEntity(chronicle, roleA);
        var targetId = RoleEntity(chronicle, roleB);
        if (!RoleRemainsActive(chronicle, roleA) ||
            !RoleRemainsActive(chronicle, roleB))
        {
            return false;
        }

        var relationshipId = KernelQuery.FindRelationship(world, sourceId, targetId, relKindId);
        return relationshipId != 0 && world.RelationshipStrength(relationshipId) >= minStrength;
    }

    private bool RolePairDistanceHolds(
        WorldState world,
        int chronicle,
        int roleA,
        int roleB,
        FixedScalar distance,
        bool within)
    {
        var sourceId = RoleEntity(chronicle, roleA);
        var targetId = RoleEntity(chronicle, roleB);
        if (!RoleRemainsActive(chronicle, roleA) ||
            !RoleRemainsActive(chronicle, roleB) ||
            !world.EntityIsActive(sourceId) || !world.EntityIsActive(targetId))
        {
            return false;
        }

        var dq = world.EntityPhysicalQ(sourceId) - world.EntityPhysicalQ(targetId);
        var dr = world.EntityPhysicalR(sourceId) - world.EntityPhysicalR(targetId);
        var ds = -dq - dr;
        var hexDistance = (Abs(dq) + Abs(dr) + Abs(ds)) / 2;
        return within ? hexDistance <= distance : hexDistance > distance;
    }

    private static int Abs(int value) => value < 0 ? -value : value;
}
