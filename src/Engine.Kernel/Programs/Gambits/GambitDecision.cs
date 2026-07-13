namespace Engine.Kernel;

internal sealed partial class GambitProgramRuntime
{
    private readonly record struct GambitDecision(
        bool HasEvaluation,
        bool NoPositiveWeight,
        int KindId,
        int CandidateCount,
        int GambitSlot,
        int TargetId);
}
