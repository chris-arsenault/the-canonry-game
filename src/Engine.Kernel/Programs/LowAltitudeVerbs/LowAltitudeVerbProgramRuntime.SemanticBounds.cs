namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private bool SemanticCoordinateAllowed(WorldState world, int q, int r)
    {
        var s = -q - r;
        return q >= Data.SemanticMinQ &&
            q <= Data.SemanticMaxQ &&
            r >= Data.SemanticMinR &&
            r <= Data.SemanticMaxR &&
            MaxAbs(q, r, s) <= EffectiveSemanticRadius(world);
    }

    private int EffectiveSemanticRadius(WorldState world)
    {
        var configuredRadius = MaxAbs(
            MaxAbs(Data.SemanticMinQ, Data.SemanticMaxQ, 0),
            MaxAbs(Data.SemanticMinR, Data.SemanticMaxR, 0),
            0);
        return global::System.Math.Max(0, global::System.Math.Min(world.MapRadius, configuredRadius));
    }
}
