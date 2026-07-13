namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class RelationshipProgramState : IKernelProgramState
{
    private readonly int[] _contactTicks;
    private readonly int[] _lastContactTicks;

    public RelationshipProgramState(int relationshipCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(relationshipCapacity);
        _contactTicks = new int[relationshipCapacity + 1];
        _lastContactTicks = new int[relationshipCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Relationships;

    public int SchemaVersion => 2;

    internal int RelationshipCapacity => _contactTicks.Length - 1;

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

    internal void SetContactState(int relationshipId, int contactTicks, int lastContactTick)
    {
        EnsureRelationship(relationshipId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(contactTicks);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThan(contactTicks, 1000);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(lastContactTick);
        _contactTicks[relationshipId] = contactTicks;
        _lastContactTicks[relationshipId] = lastContactTick;
    }

    private void EnsureRelationship(int relationshipId)
    {
        if ((uint)relationshipId == 0u || relationshipId >= _contactTicks.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(relationshipId));
        }
    }
}
