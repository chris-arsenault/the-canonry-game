namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private bool SemanticCoordinateAllowed(WorldState world, int q, int r) =>
        world.CellIsKnown(PlaneCodes.Semantic, q, r);

    private static int EffectiveSemanticRadius(WorldState world) =>
        world.MapRadius;
}
