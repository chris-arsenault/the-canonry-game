namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class PlanningProgramState : IKernelProgramState
{
    private const int PlannerOverrideKey = 1;

    internal readonly PrimitiveIntTable KindPlannerKindCodes;
    internal readonly PrimitiveIntTable EntityPlannerKindCodes;

    public PlanningProgramState(int overrideCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(overrideCapacity);
        KindPlannerKindCodes = new PrimitiveIntTable(overrideCapacity);
        EntityPlannerKindCodes = new PrimitiveIntTable(overrideCapacity);
    }

    internal int PlannerOverrideSecondKey => PlannerOverrideKey;

    public int ProgramId => KernelProgramCodes.Planning;

    public int SchemaVersion => 2;
}
