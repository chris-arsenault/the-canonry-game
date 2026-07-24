namespace Engine.Domain.Compilation;

using Engine.Behaviors;
using Engine.Domain.Compilation.KernelPrograms;

public static partial class DomainCompiler
{
    private static bool TryCompileKernelStateStep(
        List<BehaviorProgramStepRule> steps,
        IMutation mutation)
    {
        switch (mutation)
        {
            case IncreaseWorkTierMutation increase:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.IncreaseWorkTier,
                    increase.MaxTier));
                return true;
            case IncreaseTargetWorkTierMutation increaseTarget:
                steps.Add(new BehaviorProgramStepRule(
                    BehaviorProgramOperation.IncreaseWorkTier,
                    increaseTarget.MaxTier,
                    B: (int)BehaviorProgramEndpoint.Target));
                return true;
            default:
                return false;
        }
    }
}
