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

    private static float Saturating(float value, float scale) =>
        value <= 0f ? 0f : 1f - global::System.MathF.Exp(-value / scale);

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private readonly record struct MinorActorCultureField(
        float RawMass,
        float LocalRaw,
        float ConnectedRaw,
        int SupportedAdjacentCount,
        int SupportedRingTwoCount);

    private readonly record struct MinorActorCultureAttachment(
        float ComponentMass,
        int AdjacentSupportedCount,
        int ComponentCount,
        float LargestComponentSupport);

    private readonly record struct MinorActorCellTarget(
        int Q,
        int R,
        int DistanceFromActor,
        int DistanceFromSource,
        float OwnShare,
        float OwnRaw,
        float ForeignShare,
        float ForeignRaw,
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
        float LocalRawMass,
        float ConnectedRawMass,
        float Score,
        float WorkDeltaMultiplier);
}
