namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private readonly record struct LowAltitudeMovementDecision(
        bool HasDecision,
        bool MarkReached,
        MovementMovePlan Plan);
}
