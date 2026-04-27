namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class PlanningProgramData : IKernelProgramData
{
    internal int DefaultPlannerKindCode;

    public PlanningProgramData(int defaultPlannerKindCode)
    {
        ValidatePlannerKind(defaultPlannerKindCode);
        DefaultPlannerKindCode = defaultPlannerKindCode;
    }

    public int ProgramId => KernelProgramCodes.Planning;

    public int SchemaVersion => 1;

    internal static void ValidatePlannerKind(int plannerKindCode)
    {
        if (plannerKindCode is not PlannerKindCodes.Goap and not PlannerKindCodes.Htn)
        {
            throw new global::System.InvalidOperationException();
        }
    }
}
