namespace Engine.Kernel;

using Engine.Constants;
using Engine.Kernel.Programs;

internal sealed partial class CultureSlotProgramState : IKernelProgramState
{
    private readonly int[] _occupantEntityIds;
    private readonly int[] _occupiedAtTicks;
    private readonly FixedScalar[] _occupantScores;

    public CultureSlotProgramState(int slotCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(slotCapacity);
        _occupantEntityIds = new int[slotCapacity + 1];
        _occupiedAtTicks = new int[slotCapacity + 1];
        _occupantScores = new FixedScalar[slotCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.CultureSlots;

    public int SchemaVersion => 1;

    public int OccupantEntityId(int slot) => _occupantEntityIds[slot];

    public int OccupiedAtTick(int slot) => _occupiedAtTicks[slot];

    public FixedScalar OccupantScore(int slot) => _occupantScores[slot];

    public void SetOccupant(int slot, int entityId, FixedScalar score, int tick)
    {
        _occupantEntityIds[slot] = entityId;
        _occupantScores[slot] = score;
        _occupiedAtTicks[slot] = entityId > 0 ? tick : 0;
    }

    public void RefreshScore(int slot, FixedScalar score)
    {
        _occupantScores[slot] = score;
    }
}
