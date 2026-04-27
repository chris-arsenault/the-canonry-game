namespace Engine.Kernel;

using Engine.Kernel.Programs;

public sealed partial class DegeneracyProgramState : IKernelProgramState
{
    internal readonly int[] PreviousSeverityCodes;
    internal readonly bool[] Enabled;
    internal readonly PrimitiveFloatTable PreviousAttributeValues;

    public DegeneracyProgramState(int detectorCapacity, int stateCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(detectorCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stateCapacity);
        PreviousSeverityCodes = new int[detectorCapacity + 1];
        Enabled = new bool[detectorCapacity + 1];
        PreviousAttributeValues = new PrimitiveFloatTable(stateCapacity);
    }

    public int ProgramId => KernelProgramCodes.Degeneracy;

    public int SchemaVersion => 1;
}
