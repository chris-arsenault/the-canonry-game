namespace Engine.Domain.Compilation;

using Engine.Behaviors;
using Engine.Domain.Compilation.KernelPrograms;

public static partial class DomainCompiler
{
    private static bool TryCompileKernelStatePrecondition(
        List<BehaviorProgramPreconditionOp> ops,
        IPrecondition precondition)
    {
        switch (precondition)
        {
            case SelfCultureMassPrecondition mass:
                ops.Add(new BehaviorProgramPreconditionOp(
                    BehaviorProgramPrecondition.SelfCultureMass,
                    PlaneCode(mass.Plane),
                    X: KernelFixedPoint.CultureMassScalar(
                        KernelFixedPoint.CultureMassUnits(mass.MinimumRawMass)),
                    Y: KernelFixedPoint.CultureMassScalar(
                        KernelFixedPoint.CultureMassUnits(mass.MinimumConnectedRawMass))));
                return true;
            case SelfWorkTierBelowPrecondition tier:
                ops.Add(new BehaviorProgramPreconditionOp(
                    BehaviorProgramPrecondition.SelfWorkTierBelow,
                    tier.MaximumExclusive));
                return true;
            default:
                return false;
        }
    }
}
