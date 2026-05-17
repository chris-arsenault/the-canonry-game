namespace Engine.Kernel;

using Engine.Constants;
using Engine.Kernel.Programs;

internal sealed partial class CultureSlotProgramState : IKernelProgramState
{
    private const int SlotValueKey = 1;
    private readonly PrimitiveIntTable _occupantEntityIds;
    private readonly PrimitiveIntTable _occupiedAtTicks;
    private readonly PrimitiveFloatTable _occupantScores;

    public CultureSlotProgramState(int slotCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(slotCapacity);
        _occupantEntityIds = new PrimitiveIntTable(slotCapacity);
        _occupiedAtTicks = new PrimitiveIntTable(slotCapacity);
        _occupantScores = new PrimitiveFloatTable(slotCapacity);
    }

    public int ProgramId => KernelProgramCodes.CultureSlots;

    public int SchemaVersion => 1;

    public int OccupantEntityId(int slot) => _occupantEntityIds.GetOrDefault(slot, SlotValueKey);

    public int OccupiedAtTick(int slot) => _occupiedAtTicks.GetOrDefault(slot, SlotValueKey);

    public float OccupantScore(int slot) => _occupantScores.GetOrDefault(slot, SlotValueKey);

    public void SetOccupant(int slot, int entityId, float score, int tick)
    {
        _occupantEntityIds.Set(slot, SlotValueKey, entityId);
        _occupantScores.Set(slot, SlotValueKey, score);
        _occupiedAtTicks.Set(slot, SlotValueKey, entityId > 0 ? tick : 0);
    }

    public void RefreshScore(int slot, float score)
    {
        _occupantScores.Set(slot, SlotValueKey, score);
    }
}
