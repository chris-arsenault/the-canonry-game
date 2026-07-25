namespace Engine.Kernel;

internal sealed partial class GambitProgramState : IKernelProgramState
{
    internal readonly bool[] GambitEnabled;
    internal readonly int[] CooldownExpiresAtTicks;
    internal readonly int[] EntityCooldownExpiresAtTicks;
    internal readonly int CooldownStride;

    public GambitProgramState(
        int gambitCapacity,
        int entityCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(gambitCapacity);
        NumericGuard.ThrowIfNegativeOrZero(entityCapacity);
        GambitEnabled = new bool[gambitCapacity + 1];
        CooldownStride = gambitCapacity + 1;
        CooldownExpiresAtTicks = new int[(entityCapacity + 1) * CooldownStride];
        EntityCooldownExpiresAtTicks = new int[entityCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Gambits;

    public int SchemaVersion => 4;
}
