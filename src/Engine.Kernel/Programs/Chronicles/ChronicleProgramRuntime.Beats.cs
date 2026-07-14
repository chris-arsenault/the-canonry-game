namespace Engine.Kernel;

internal sealed partial class ChronicleProgramRuntime
{
    private void ProcessPendingExecutions(Simulation simulation, WorldState world, int tick)
    {
        for (var pending = 0; pending < State.PendingCount; pending++)
        {
            var actorId = State.PendingActorIds[pending];
            var targetId = State.PendingTargetIds[pending];
            var gambitId = State.PendingGambitIds[pending];
            for (var chronicle = 1; chronicle <= State.HighWatermark; chronicle++)
            {
                if (!State.Active[chronicle])
                {
                    continue;
                }

                ApplyExecutionToChronicle(simulation, world, chronicle, actorId, targetId, gambitId, tick);
            }
        }

        State.PendingCount = 0;
    }

    private void ApplyExecutionToChronicle(
        Simulation simulation,
        WorldState world,
        int chronicle,
        int actorId,
        int targetId,
        int gambitId,
        int tick)
    {
        if (CastContains(chronicle, actorId) && CastContains(chronicle, targetId) && actorId != targetId)
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
                !ExecutionStrikes(chronicle, beat, actorId, targetId, gambitId))
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

    private bool ExecutionStrikes(int chronicle, int beat, int actorId, int targetId, int gambitId)
    {
        var gambitFilter = Data.BeatStrikeGambitIds[beat];
        if (gambitFilter != 0 && gambitFilter != gambitId)
        {
            return false;
        }

        return Data.BeatStrikeKinds[beat] switch
        {
            ChronicleStrikeCodes.RolePairGambit =>
                RoleEntity(chronicle, Data.BeatStrikeActorRoles[beat]) == actorId &&
                RoleEntity(chronicle, Data.BeatStrikeTargetRoles[beat]) == targetId,
            ChronicleStrikeCodes.OutsiderGambitOnRole =>
                RoleEntity(chronicle, Data.BeatStrikeTargetRoles[beat]) == targetId &&
                !CastContains(chronicle, actorId),
            _ => false,
        };
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
        if (State.Heat[chronicle] >= FadeHeatThreshold ||
            tick - State.StartTicks[chronicle] <= FadeGraceTicks ||
            tick - State.LastStrikeTicks[chronicle] <= FadeQuietTicks)
        {
            return;
        }

        var archetype = State.ArchetypeSlots[chronicle];
        Resolve(simulation, chronicle, Data.ArchetypeFadeOutcomeCodeIds[archetype], tick);
    }

    private void Resolve(Simulation simulation, int chronicle, int outcomeCodeId, int tick)
    {
        EmitChronicleChanged(simulation, chronicle, ChronicleEventTypeCodes.Resolved, 0, outcomeCodeId, tick);
        RememberCast(chronicle, tick);
        State.Active[chronicle] = false;
        State.ArchetypeActiveCounts[State.ArchetypeSlots[chronicle]]--;
    }

    /// <summary>
    /// Resolved cast members enter the suppression ring so the same entities
    /// are not immediately recast as primaries of a fresh tale.
    /// </summary>
    private void RememberCast(int chronicle, int tick)
    {
        for (var role = 0; role < ChronicleProgramData.MaxRolesPerArchetype; role++)
        {
            var entityId = RoleEntity(chronicle, role);
            if (entityId <= 0)
            {
                continue;
            }

            State.RecentCastEntityIds[State.RecentCastNext] = entityId;
            State.RecentCastTicks[State.RecentCastNext] = tick;
            State.RecentCastNext = (State.RecentCastNext + 1) % ChronicleProgramState.SuppressionRingCapacity;
        }
    }

    private bool RecentlyChronicled(int entityId, int tick)
    {
        for (var slot = 0; slot < ChronicleProgramState.SuppressionRingCapacity; slot++)
        {
            if (State.RecentCastEntityIds[slot] == entityId &&
                tick - State.RecentCastTicks[slot] < SuppressionTicks)
            {
                return true;
            }
        }

        return false;
    }

    private void AddHeat(int chronicle, float delta)
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
        float value)
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
        float value)
    {
        switch (conditionKind)
        {
            case ChronicleConditionCodes.RoleHistorical:
                var entityId = RoleEntity(chronicle, roleA);
                return entityId > 0 && !world.EntityIsActive(entityId);
            case ChronicleConditionCodes.RoleAttributeBelow:
                var subjectId = RoleEntity(chronicle, roleA);
                return subjectId > 0 &&
                    world.EntityIsActive(subjectId) &&
                    world.GetAttribute(subjectId, relOrAttrId) < value;
            case ChronicleConditionCodes.RolePairWithinDistance:
                return RolePairDistanceHolds(world, chronicle, roleA, roleB, value, within: true);
            case ChronicleConditionCodes.RolePairBeyondDistance:
                return RolePairDistanceHolds(world, chronicle, roleA, roleB, value, within: false);
            default:
                return false;
        }
    }

    private bool RolePairRelationshipHolds(
        WorldState world,
        int chronicle,
        int roleA,
        int roleB,
        int relKindId,
        float minStrength)
    {
        var sourceId = RoleEntity(chronicle, roleA);
        var targetId = RoleEntity(chronicle, roleB);
        if (sourceId <= 0 || targetId <= 0)
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
        float distance,
        bool within)
    {
        var sourceId = RoleEntity(chronicle, roleA);
        var targetId = RoleEntity(chronicle, roleB);
        if (sourceId <= 0 || targetId <= 0 ||
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
