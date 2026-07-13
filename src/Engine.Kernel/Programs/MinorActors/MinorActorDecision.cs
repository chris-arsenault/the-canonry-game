namespace Engine.Kernel;

internal sealed partial class MinorActorProgramRuntime
{
    private readonly record struct MinorActorDecision(
        bool IsActive,
        bool ActionDue,
        bool HasVerbLayer,
        bool HasSelectedVerb,
        MinorActorVerbCandidate Selected,
        bool Move,
        int MoveQ,
        int MoveR,
        int RemainingPulses);
}
