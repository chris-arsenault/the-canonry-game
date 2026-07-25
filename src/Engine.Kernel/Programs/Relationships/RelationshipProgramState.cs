namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class RelationshipProgramState : IKernelProgramState
{
    private readonly int[] _contactTicks;
    private readonly int[] _lastContactTicks;
    private readonly int[] _followRelationshipIdsByTarget;
    private readonly int[] _followSourceIdsByTarget;

    public RelationshipProgramState(int relationshipCapacity, int entityCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(relationshipCapacity);
        NumericGuard.ThrowIfNegativeOrZero(entityCapacity);
        _contactTicks = new int[relationshipCapacity + 1];
        _lastContactTicks = new int[relationshipCapacity + 1];
        _followRelationshipIdsByTarget = new int[entityCapacity + 1];
        _followSourceIdsByTarget = new int[entityCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Relationships;

    public int SchemaVersion => 4;

    public int ContactTicks(int relationshipId) => _contactTicks[relationshipId];

    public int LastContactTick(int relationshipId) => _lastContactTicks[relationshipId];

    public void RecordContact(int relationshipId, int tick)
    {
        EnsureRelationship(relationshipId);
        if (_contactTicks[relationshipId] < 1000)
        {
            _contactTicks[relationshipId]++;
        }

        _lastContactTicks[relationshipId] = tick;
    }

    public void DecayContact(int relationshipId)
    {
        EnsureRelationship(relationshipId);
        if (_contactTicks[relationshipId] > 0)
        {
            _contactTicks[relationshipId]--;
        }
    }

    public void Reset(int relationshipId)
    {
        EnsureRelationship(relationshipId);
        _contactTicks[relationshipId] = 0;
        _lastContactTicks[relationshipId] = 0;
    }

    public int FollowRelationshipId(int targetEntityId) =>
        (uint)targetEntityId < (uint)_followRelationshipIdsByTarget.Length
            ? _followRelationshipIdsByTarget[targetEntityId]
            : 0;

    public int FollowSourceId(int targetEntityId) =>
        (uint)targetEntityId < (uint)_followSourceIdsByTarget.Length
            ? _followSourceIdsByTarget[targetEntityId]
            : 0;

    public void SetFollow(int targetEntityId, int relationshipId, int sourceEntityId)
    {
        EnsureEntity(targetEntityId);
        EnsureRelationship(relationshipId);
        NumericGuard.ThrowIfNegativeOrZero(sourceEntityId);
        _followRelationshipIdsByTarget[targetEntityId] = relationshipId;
        _followSourceIdsByTarget[targetEntityId] = sourceEntityId;
    }

    public void ClearFollow(int targetEntityId)
    {
        EnsureEntity(targetEntityId);
        _followRelationshipIdsByTarget[targetEntityId] = 0;
        _followSourceIdsByTarget[targetEntityId] = 0;
    }

    private void EnsureRelationship(int relationshipId)
    {
        if ((uint)relationshipId == 0u || relationshipId >= _contactTicks.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(relationshipId));
        }
    }

    private void EnsureEntity(int entityId)
    {
        if ((uint)entityId == 0u || entityId >= _followRelationshipIdsByTarget.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(entityId));
        }
    }
}
