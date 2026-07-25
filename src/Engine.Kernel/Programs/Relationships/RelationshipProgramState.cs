namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class RelationshipProgramState : IKernelProgramState
{
    private readonly int[] _contactTicks;
    private readonly int[] _lastContactTicks;

    public RelationshipProgramState(int relationshipCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(relationshipCapacity);
        _contactTicks = new int[relationshipCapacity + 1];
        _lastContactTicks = new int[relationshipCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Relationships;

    public int SchemaVersion => 3;

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

    private void EnsureRelationship(int relationshipId)
    {
        if ((uint)relationshipId == 0u || relationshipId >= _contactTicks.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(relationshipId));
        }
    }
}
