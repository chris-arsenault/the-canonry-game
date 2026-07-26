namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class ChronicleProgramState : IActiveKernelProgramStateSnapshot
{
    public void WriteActiveSnapshot(global::System.IO.BinaryWriter writer)
    {
        writer.Write(NextChronicleSequence);
        writer.Write(LegendaryResolutionCount);
        writer.Write(RecentCastNext);
        WriteSuppressionState(writer);
        WriteNextCastTicks(writer);
        WritePendingState(writer);

        var activeCount = 0;
        for (var chronicle = 1; chronicle <= HighWatermark; chronicle++)
        {
            if (Active[chronicle])
            {
                activeCount++;
            }
        }

        writer.Write(activeCount);
        for (var chronicle = 1; chronicle <= HighWatermark; chronicle++)
        {
            if (!Active[chronicle])
            {
                continue;
            }

            writer.Write(chronicle);
            writer.Write(ArchetypeSlots[chronicle]);
            writer.Write(StartTicks[chronicle]);
            writer.Write(LastStrikeTicks[chronicle]);
            writer.Write(Heat[chronicle].Units);
            writer.Write(OpenMasks[chronicle]);
            writer.Write(StruckMasks[chronicle]);
            writer.Write(RetiredRoleMasks[chronicle]);
            writer.Write(ChronicleSequences[chronicle]);
            var roleBase = chronicle * ChronicleProgramData.MaxRolesPerArchetype;
            for (var role = 0; role < ChronicleProgramData.MaxRolesPerArchetype; role++)
            {
                writer.Write(RoleEntityIds[roleBase + role]);
            }
        }
    }

    public void ReadActiveSnapshot(global::System.IO.BinaryReader reader)
    {
        NextChronicleSequence = reader.ReadInt32();
        LegendaryResolutionCount = reader.ReadInt32();
        RecentCastNext = reader.ReadInt32();
        if (NextChronicleSequence < 0 ||
            LegendaryResolutionCount < 0 ||
            (uint)RecentCastNext >= SuppressionRingCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        ReadSuppressionState(reader);
        ReadNextCastTicks(reader);
        ReadPendingState(reader);

        var activeCount = reader.ReadInt32();
        if ((uint)activeCount >= (uint)Active.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var item = 0; item < activeCount; item++)
        {
            ReadActiveChronicle(reader);
        }
    }

    private void ReadActiveChronicle(global::System.IO.BinaryReader reader)
    {
        var chronicle = reader.ReadInt32();
        if ((uint)chronicle == 0u || chronicle >= Active.Length || Active[chronicle])
        {
            throw new global::System.InvalidOperationException();
        }

        var archetype = reader.ReadInt32();
        if ((uint)archetype == 0u || archetype >= ArchetypeActiveCounts.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        ReadActiveChronicleFields(reader, chronicle, archetype);
        ReadChronicleRoles(reader, chronicle);
        ArchetypeActiveCounts[archetype]++;
        if (chronicle > HighWatermark)
        {
            HighWatermark = chronicle;
        }
    }

    private void ReadActiveChronicleFields(
        global::System.IO.BinaryReader reader,
        int chronicle,
        int archetype)
    {
        Active[chronicle] = true;
        ArchetypeSlots[chronicle] = archetype;
        StartTicks[chronicle] = reader.ReadInt32();
        LastStrikeTicks[chronicle] = reader.ReadInt32();
        Heat[chronicle] = FixedScalar.FromUnits(reader.ReadInt64());
        OpenMasks[chronicle] = reader.ReadInt64();
        StruckMasks[chronicle] = reader.ReadInt64();
        RetiredRoleMasks[chronicle] = reader.ReadInt64();
        ChronicleSequences[chronicle] = reader.ReadInt32();
        if (StartTicks[chronicle] < 0 ||
            LastStrikeTicks[chronicle] < 0 ||
            Heat[chronicle] < 0 ||
            ChronicleSequences[chronicle] <= 0)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void ReadChronicleRoles(global::System.IO.BinaryReader reader, int chronicle)
    {
        var roleBase = chronicle * ChronicleProgramData.MaxRolesPerArchetype;
        for (var role = 0; role < ChronicleProgramData.MaxRolesPerArchetype; role++)
        {
            RoleEntityIds[roleBase + role] = reader.ReadInt32();
            if (RoleEntityIds[roleBase + role] < 0)
            {
                throw new global::System.InvalidOperationException();
            }
        }
    }

    private void WriteSuppressionState(global::System.IO.BinaryWriter writer)
    {
        var count = 0;
        for (var slot = 0; slot < SuppressionRingCapacity; slot++)
        {
            if (RecentCastEntityOccurrenceHandles[slot] > 0)
            {
                count++;
            }
        }

        writer.Write(count);
        for (var slot = 0; slot < SuppressionRingCapacity; slot++)
        {
            if (RecentCastEntityOccurrenceHandles[slot] <= 0)
            {
                continue;
            }

            writer.Write(slot);
            writer.Write(RecentCastEntityOccurrenceHandles[slot]);
            writer.Write(RecentCastTicks[slot]);
            writer.Write(RecentCastHeat[slot].Units);
        }
    }

    private void ReadSuppressionState(global::System.IO.BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if ((uint)count > SuppressionRingCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var item = 0; item < count; item++)
        {
            var slot = reader.ReadInt32();
            var occurrenceHandle = reader.ReadInt32();
            var tick = reader.ReadInt32();
            var heat = FixedScalar.FromUnits(reader.ReadInt64());
            if ((uint)slot >= SuppressionRingCapacity ||
                occurrenceHandle <= 0 ||
                tick < 0 ||
                heat < 0 ||
                RecentCastEntityOccurrenceHandles[slot] != 0)
            {
                throw new global::System.InvalidOperationException();
            }

            RecentCastEntityOccurrenceHandles[slot] = occurrenceHandle;
            RecentCastTicks[slot] = tick;
            RecentCastHeat[slot] = heat;
        }
    }

    private void WriteNextCastTicks(global::System.IO.BinaryWriter writer)
    {
        var count = 0;
        for (var archetype = 1; archetype < NextCastTicks.Length; archetype++)
        {
            if (NextCastTicks[archetype] > 0)
            {
                count++;
            }
        }

        writer.Write(count);
        for (var archetype = 1; archetype < NextCastTicks.Length; archetype++)
        {
            if (NextCastTicks[archetype] <= 0)
            {
                continue;
            }

            writer.Write(archetype);
            writer.Write(NextCastTicks[archetype]);
        }
    }

    private void ReadNextCastTicks(global::System.IO.BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if ((uint)count >= (uint)NextCastTicks.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var item = 0; item < count; item++)
        {
            var archetype = reader.ReadInt32();
            var tick = reader.ReadInt32();
            if ((uint)archetype == 0u ||
                archetype >= NextCastTicks.Length ||
                tick <= 0 ||
                NextCastTicks[archetype] != 0)
            {
                throw new global::System.InvalidOperationException();
            }

            NextCastTicks[archetype] = tick;
        }
    }

    private void WritePendingState(global::System.IO.BinaryWriter writer)
    {
        writer.Write(PendingCount);
        for (var index = 0; index < PendingCount; index++)
        {
            writer.Write(PendingKinds[index]);
            writer.Write(PendingActorOccurrenceHandles[index]);
            writer.Write(PendingTargetOccurrenceHandles[index]);
            writer.Write(PendingProducerIds[index]);
            writer.Write(PendingSucceeded[index]);
        }
    }

    private void ReadPendingState(global::System.IO.BinaryReader reader)
    {
        PendingCount = reader.ReadInt32();
        if ((uint)PendingCount > (uint)PendingKinds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var index = 0; index < PendingCount; index++)
        {
            PendingKinds[index] = reader.ReadInt32();
            PendingActorOccurrenceHandles[index] = reader.ReadInt32();
            PendingTargetOccurrenceHandles[index] = reader.ReadInt32();
            PendingProducerIds[index] = reader.ReadInt32();
            PendingSucceeded[index] = reader.ReadInt32();
            if (PendingKinds[index] <= 0 ||
                PendingActorOccurrenceHandles[index] < 0 ||
                PendingTargetOccurrenceHandles[index] < 0 ||
                PendingProducerIds[index] <= 0 ||
                PendingSucceeded[index] is not (0 or 1))
            {
                throw new global::System.InvalidOperationException();
            }
        }
    }
}
