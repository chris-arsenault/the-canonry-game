namespace Engine.Kernel;

internal sealed partial class ChronicleProgramRuntime
{
    private static readonly int[] EmptyRefs = new int[1];

    private void TryCast(Simulation simulation, WorldState world, int archetype, int tick)
    {
        if (State.ArchetypeActiveCounts[archetype] >= Data.ArchetypeMaxActive[archetype] ||
            tick < State.NextCastTicks[archetype])
        {
            return;
        }

        State.NextCastTicks[archetype] = tick + Data.ArchetypeCastingCadenceTicks[archetype];
        var primaryId = SelectPrimary(world, archetype, tick);
        if (primaryId == 0)
        {
            return;
        }

        var story = FindChronicleSlot();
        if (story == 0)
        {
            return;
        }

        SetRoleEntity(story, 0, primaryId);
        if (!BindSupportingRoles(simulation, world, archetype, story, primaryId))
        {
            ClearRoles(story);
            return;
        }

        State.Active[story] = true;
        State.ArchetypeSlots[story] = archetype;
        State.StartTicks[story] = tick;
        State.LastStrikeTicks[story] = tick;
        State.Heat[story] = InitialHeat;
        State.OpenMasks[story] = 0L;
        State.StruckMasks[story] = 0L;
        State.NextChronicleSequence++;
        State.ChronicleSequences[story] = State.NextChronicleSequence;
        State.ArchetypeActiveCounts[archetype]++;
        if (story > State.HighWatermark)
        {
            State.HighWatermark = story;
        }

        EmitChronicleChanged(simulation, story, ChronicleEventTypeCodes.Cast, beatOrdinal: 0, outcomeCodeId: 0, tick);
    }

    // The provenance merge: casting consumes the causal record. Among
    // candidates whose qualifying edge passes the archetype gate, the one
    // whose edge was most recently authored (by gambit, generator, or a
    // prior chronicle - the provenance invocation tick) wins the tale. The
    // director follows the freshest causal thread, not the lowest entity id.
    private int SelectPrimary(WorldState world, int archetype, int tick)
    {
        var kindId = Data.ArchetypePrimaryKindIds[archetype];
        var relKindId = Data.ArchetypePrimaryRequiredRelKindIds[archetype];
        var minStrength = Data.ArchetypePrimaryRequiredRelMinStrengths[archetype];
        var bestId = 0;
        var bestCausalTick = -1;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!world.EntityIsActive(entityId) ||
                world.EntityKindId(entityId) != kindId ||
                RoleIsCastInArchetype(archetype, entityId) ||
                RecentlyChronicled(entityId, tick))
            {
                continue;
            }

            if (relKindId == 0)
            {
                return entityId;
            }

            var causalTick = QualifyingEdgeCausalTick(world, entityId, relKindId, minStrength);
            if (causalTick > bestCausalTick)
            {
                bestCausalTick = causalTick;
                bestId = entityId;
            }
        }

        return bestId;
    }

    private static int QualifyingEdgeCausalTick(WorldState world, int entityId, int relKindId, float minStrength)
    {
        var best = -1;
        var cursor = world.OutgoingRelationshipsOfKind(entityId, relKindId);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            if (world.RelationshipStrength(relationshipId) < minStrength)
            {
                continue;
            }

            var invocationId = world.RelationshipCreatedByInvocationId(relationshipId);
            var causalTick = invocationId > 0 ? world.ProvenanceInvocationTick(invocationId) : 0;
            if (causalTick > best)
            {
                best = causalTick;
            }
        }

        return best;
    }

    private bool RoleIsCastInArchetype(int archetype, int entityId)
    {
        for (var story = 1; story <= State.HighWatermark; story++)
        {
            if (!State.Active[story] || State.ArchetypeSlots[story] != archetype)
            {
                continue;
            }

            for (var role = 0; role < ChronicleProgramData.MaxRolesPerArchetype; role++)
            {
                if (RoleEntity(story, role) == entityId)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool BindSupportingRoles(Simulation simulation, WorldState world, int archetype, int story, int primaryId)
    {
        var roleStart = Data.ArchetypeRoleStarts[archetype];
        var roleCount = Data.ArchetypeRoleCounts[archetype];
        for (var roleIndex = 1; roleIndex < roleCount; roleIndex++)
        {
            var role = roleStart + roleIndex;
            var entityId = SelectSupportingRole(simulation, world, role, primaryId);
            if (entityId == 0)
            {
                return false;
            }

            SetRoleEntity(story, roleIndex, entityId);
        }

        return true;
    }

    private int SelectSupportingRole(Simulation simulation, WorldState world, int role, int primaryId)
    {
        var query = new SharedQuery(
            Data.RoleKindIds[role],
            relatedToTargetKindId: Data.RoleRelatedKindIds[role],
            relatedToTargetIncoming: Data.RoleRelatedIncoming[role],
            relatedToTargetMinStrength: Data.RoleRelatedMinStrengths[role]);
        var binding = new QueryBinding(
            actorId: 0,
            primaryId,
            targetOriginQ: world.EntityPhysicalQ(primaryId),
            targetOriginR: world.EntityPhysicalR(primaryId),
            refEntityIds: EmptyRefs,
            refCount: 0);
        var count = KernelQuery.EvaluateSharedQuery(world, query, binding, selectedOrdinal: -1).Count;
        if (count == 0)
        {
            return 0;
        }

        var ordinal = simulation.NextInt(count);
        return KernelQuery.EvaluateSharedQuery(world, query, binding, ordinal).SelectedEntityId;
    }

    private int FindChronicleSlot()
    {
        for (var story = 1; story <= _chronicleCapacity; story++)
        {
            if (!State.Active[story])
            {
                return story;
            }
        }

        return 0;
    }

    private void ClearRoles(int story)
    {
        for (var role = 0; role < ChronicleProgramData.MaxRolesPerArchetype; role++)
        {
            SetRoleEntity(story, role, 0);
        }
    }
}
