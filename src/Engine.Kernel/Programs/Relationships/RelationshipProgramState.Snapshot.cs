namespace Engine.Kernel;

internal sealed partial class RelationshipProgramState : IActiveKernelProgramStateSnapshot
{
    public void WriteActiveSnapshot(global::System.IO.BinaryWriter writer)
    {
        WriteContactState(writer);
        WriteFollowState(writer);
    }

    public void ReadActiveSnapshot(global::System.IO.BinaryReader reader)
    {
        ReadContactState(reader);
        ReadFollowState(reader);
    }

    private void WriteContactState(global::System.IO.BinaryWriter writer)
    {
        var contactCount = 0;
        for (var relationshipId = 1; relationshipId < _contactTicks.Length; relationshipId++)
        {
            if (_contactTicks[relationshipId] > 0 || _lastContactTicks[relationshipId] > 0)
            {
                contactCount++;
            }
        }

        writer.Write(contactCount);
        for (var relationshipId = 1; relationshipId < _contactTicks.Length; relationshipId++)
        {
            if (_contactTicks[relationshipId] == 0 && _lastContactTicks[relationshipId] == 0)
            {
                continue;
            }

            writer.Write(relationshipId);
            writer.Write(_contactTicks[relationshipId]);
            writer.Write(_lastContactTicks[relationshipId]);
        }
    }

    private void WriteFollowState(global::System.IO.BinaryWriter writer)
    {
        var followCount = 0;
        for (var entityId = 1; entityId < _followRelationshipIdsByTarget.Length; entityId++)
        {
            if (_followRelationshipIdsByTarget[entityId] > 0)
            {
                followCount++;
            }
        }

        writer.Write(followCount);
        for (var entityId = 1; entityId < _followRelationshipIdsByTarget.Length; entityId++)
        {
            if (_followRelationshipIdsByTarget[entityId] <= 0)
            {
                continue;
            }

            writer.Write(entityId);
            writer.Write(_followRelationshipIdsByTarget[entityId]);
            writer.Write(_followSourceIdsByTarget[entityId]);
        }
    }

    private void ReadContactState(global::System.IO.BinaryReader reader)
    {
        var contactCount = reader.ReadInt32();
        if ((uint)contactCount >= (uint)_contactTicks.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var item = 0; item < contactCount; item++)
        {
            var relationshipId = reader.ReadInt32();
            var contactTicks = reader.ReadInt32();
            var lastContactTick = reader.ReadInt32();
            if ((uint)relationshipId == 0u ||
                relationshipId >= _contactTicks.Length ||
                contactTicks < 0 ||
                lastContactTick < 0 ||
                _contactTicks[relationshipId] != 0 ||
                _lastContactTicks[relationshipId] != 0)
            {
                throw new global::System.InvalidOperationException();
            }

            _contactTicks[relationshipId] = contactTicks;
            _lastContactTicks[relationshipId] = lastContactTick;
        }
    }

    private void ReadFollowState(global::System.IO.BinaryReader reader)
    {
        var followCount = reader.ReadInt32();
        if ((uint)followCount >= (uint)_followRelationshipIdsByTarget.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var item = 0; item < followCount; item++)
        {
            var entityId = reader.ReadInt32();
            var relationshipId = reader.ReadInt32();
            var sourceEntityId = reader.ReadInt32();
            if (FollowSnapshotRowIsInvalid(entityId, relationshipId, sourceEntityId))
            {
                throw new global::System.InvalidOperationException();
            }

            _followRelationshipIdsByTarget[entityId] = relationshipId;
            _followSourceIdsByTarget[entityId] = sourceEntityId;
        }
    }

    private bool FollowSnapshotRowIsInvalid(
        int entityId,
        int relationshipId,
        int sourceEntityId)
    {
        return (uint)entityId == 0u ||
            entityId >= _followRelationshipIdsByTarget.Length ||
            relationshipId <= 0 ||
            relationshipId >= _contactTicks.Length ||
            sourceEntityId <= 0 ||
            sourceEntityId >= _followRelationshipIdsByTarget.Length ||
            _followRelationshipIdsByTarget[entityId] != 0;
    }
}
