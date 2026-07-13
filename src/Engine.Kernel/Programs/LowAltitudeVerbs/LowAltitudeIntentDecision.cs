namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private readonly record struct LowAltitudeIntentDecision(
        bool HasSelection,
        VerbTargetCandidate Selected,
        int SelectionBucketCode);
}
