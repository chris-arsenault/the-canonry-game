namespace Engine.Domain.Compilation;

using Engine.Behaviors;
using Engine.Domain.Compilation.KernelPrograms;

public static partial class DomainCompiler
{
    private static bool TryCompileKernelStateStep(
        List<BehaviorProgramStepRule> steps,
        IMutation mutation)
    {
        if (mutation is not IncreaseWorkTierMutation increase)
        {
            return false;
        }

        steps.Add(new BehaviorProgramStepRule(
            BehaviorProgramOperation.IncreaseWorkTier,
            increase.MaxTier));
        return true;
    }
}
