namespace Engine.Kernel;

using Engine.Kernel.Programs;

public sealed partial class PressureProgramState : IKernelProgramState
{
    internal readonly bool[] Enabled;
    internal readonly PrimitiveFloatTable Values;
    internal readonly PrimitiveFloatTable PendingDeltas;

    public PressureProgramState(int pressureCapacity, int valueCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pressureCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valueCapacity);
        Enabled = new bool[pressureCapacity + 1];
        Values = new PrimitiveFloatTable(valueCapacity);
        PendingDeltas = new PrimitiveFloatTable(valueCapacity);
    }

    public int ProgramId => KernelProgramCodes.Pressures;

    public int SchemaVersion => 1;
}
