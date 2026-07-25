namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramRuntime
{
    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var asCoord = -aq - ar;
        var bsCoord = -bq - br;
        return (global::System.Math.Abs(aq - bq) +
            global::System.Math.Abs(ar - br) +
            global::System.Math.Abs(asCoord - bsCoord)) / 2;
    }

    private static FixedScalar Saturating(FixedScalar value, FixedScalar scale) =>
        value <= 0 ? 0 : 1 - FixedScalar.Exp(-value / scale);

    private static FixedScalar Clamp01(FixedScalar value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value > 1 ? 1 : value;
    }

    private readonly record struct MinorActorCultureField(
        FixedScalar RawMass,
        FixedScalar LocalRaw,
        FixedScalar ConnectedRaw,
        int SupportedAdjacentCount,
        int SupportedRingTwoCount);

    private readonly record struct MinorActorCultureAttachment(
        FixedScalar ConnectedRawMass,
        int AdjacentSupportedCount,
        FixedScalar StrongCenterSupport);

    private readonly record struct MinorActorCellTarget(
        int Q,
        int R,
        int DistanceFromActor,
        int DistanceFromSource,
        FixedScalar OwnShare,
        FixedScalar OwnRaw,
        FixedScalar ForeignShare,
        FixedScalar ForeignRaw,
        MinorActorCultureField CultureField,
        MinorActorCultureAttachment Attachment,
        int CellKindCode);

    private readonly record struct MinorActorVerbCandidate(
        int VerbId,
        int ActionCode,
        int TargetingCode,
        int PlaneCode,
        int Q,
        int R,
        int CellKindCode,
        int EffectiveRadius,
        FixedScalar LocalRawMass,
        FixedScalar ConnectedRawMass,
        FixedScalar Score,
        FixedScalar WorkDeltaMultiplier);
}
