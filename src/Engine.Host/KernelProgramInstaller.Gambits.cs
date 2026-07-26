namespace Engine.Host;

using Engine.Constants;
using Engine.Domain.Compilation.KernelPrograms;

internal static partial class KernelProgramInstaller
{
    private static void InstallGambitPreconditions(
        KernelConfigurationContext context,
        int gambitSlot,
        CompiledGambitRule gambit)
    {
        var plan = gambit.PreconditionPlan;
        InstallGambitCandidatePreconditions(context, gambitSlot, plan);
        InstallGambitActorPreconditions(context, gambitSlot, plan);
        ConfigureGambitPreconditionPlan(context, gambitSlot, plan);
    }

    private static void InstallGambitCandidatePreconditions(
        KernelConfigurationContext context,
        int gambitSlot,
        BehaviorProgramPreconditionPlan plan)
    {
        foreach (var precondition in plan.CandidateInstructions)
        {
            context.AddGambitCandidatePrecondition(
                gambitSlot,
                PreconditionCode(precondition.Code),
                precondition.A,
                precondition.B,
                precondition.C,
                precondition.D,
                precondition.X,
                precondition.Y,
                precondition.ActorValueSlot,
                precondition.RelationshipTargetSource ? 1 : 0,
                precondition.TrueDestination,
                precondition.FalseDestination);
        }
    }

    private static void InstallGambitActorPreconditions(
        KernelConfigurationContext context,
        int gambitSlot,
        BehaviorProgramPreconditionPlan plan)
    {
        foreach (var actorPrecondition in plan.ActorInstructions)
        {
            context.AddGambitActorPrecondition(
                gambitSlot,
                actorPrecondition.ActorValueSlot,
                actorPrecondition.TrueDestination,
                actorPrecondition.FalseDestination);
        }
    }

    private static void ConfigureGambitPreconditionPlan(
        KernelConfigurationContext context,
        int gambitSlot,
        BehaviorProgramPreconditionPlan plan)
    {
        var targetSource = plan.RelationshipTargetSource;
        context.ConfigureGambitPreconditionPlan(
            gambitSlot,
            plan.CandidateEntry,
            plan.ActorEntry,
            plan.ActorValueCount,
            targetSource?.DirectionCode ?? 0,
            targetSource?.RelationshipKindId ?? 0,
            targetSource?.HasMinimumStrength == true ? 1 : 0,
            targetSource?.MinimumStrength ?? FixedScalar.Zero);
    }
}
