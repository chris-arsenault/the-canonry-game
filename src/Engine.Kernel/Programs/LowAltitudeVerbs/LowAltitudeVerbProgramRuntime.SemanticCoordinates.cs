namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private bool SemanticCoordinateAllowed(WorldState world, int q, int r)
    {
        var s = -q - r;
        return MaxAbs(q, r, s) <= EffectiveSemanticRadius(world);
    }

    private static int EffectiveSemanticRadius(WorldState world) =>
        world.MapRadius;
}
