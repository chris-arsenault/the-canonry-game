namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

internal readonly record struct GambitTestPredicate(
    int Code,
    int A = 0,
    int B = 0,
    int C = 0,
    int D = 0,
    FixedScalar X = default,
    FixedScalar Y = default,
    bool Negated = false);

internal static class GambitPreconditionPlanTestExtensions
{
    public static void SetConjunctivePreconditions(
        this GambitProgramRuntime gambits,
        int gambitSlot,
        GambitTestPredicate predicate) =>
        SetConjunctivePreconditionsCore(gambits, gambitSlot, [predicate]);

    public static void SetConjunctivePreconditions(
        this GambitProgramRuntime gambits,
        int gambitSlot,
        GambitTestPredicate first,
        GambitTestPredicate second) =>
        SetConjunctivePreconditionsCore(gambits, gambitSlot, [first, second]);

    public static void SetConjunctivePreconditions(
        this GambitProgramRuntime gambits,
        int gambitSlot,
        GambitTestPredicate first,
        GambitTestPredicate second,
        GambitTestPredicate third) =>
        SetConjunctivePreconditionsCore(gambits, gambitSlot, [first, second, third]);

    private static void SetConjunctivePreconditionsCore(
        GambitProgramRuntime gambits,
        int gambitSlot,
        ReadOnlySpan<GambitTestPredicate> predicates)
    {
        ArgumentNullException.ThrowIfNull(gambits);

        for (var index = 0; index < predicates.Length; index++)
        {
            var predicate = predicates[index];
            var passedDestination = index + 1 == predicates.Length
                ? GambitPreconditionDestinationCodes.Passed
                : index + 2;
            var trueDestination = predicate.Negated
                ? GambitPreconditionDestinationCodes.Failed
                : passedDestination;
            var falseDestination = predicate.Negated
                ? passedDestination
                : GambitPreconditionDestinationCodes.Failed;
            gambits.AddCandidatePrecondition(
                gambitSlot,
                predicate.Code,
                predicate.A,
                predicate.B,
                predicate.C,
                predicate.D,
                predicate.X,
                predicate.Y,
                actorValueSlot: 0,
                relationshipTargetSource: 0,
                trueDestination,
                falseDestination);
        }

        gambits.ConfigurePreconditionPlan(
            gambitSlot,
            predicates.Length == 0
                ? GambitPreconditionDestinationCodes.Passed
                : 1,
            actorEntry: GambitPreconditionDestinationCodes.Passed,
            actorValueCount: 0,
            relationshipTargetDirectionCode: 0,
            relationshipTargetKindId: 0,
            relationshipTargetHasMinimumStrength: 0,
            relationshipTargetMinimumStrength: FixedScalar.Zero);
    }
}
