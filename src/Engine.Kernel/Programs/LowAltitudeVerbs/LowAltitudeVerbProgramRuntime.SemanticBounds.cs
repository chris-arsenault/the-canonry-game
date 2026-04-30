namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private bool TryRandomSemanticCoordinate(Simulation simulation, out int q, out int r, out int s)
    {
        var radius = EffectiveSemanticRadius(simulation.World);
        var minQ = global::System.Math.Max(Data.SemanticMinQ, -radius);
        var maxQ = global::System.Math.Min(Data.SemanticMaxQ, radius);
        if (minQ > maxQ)
        {
            q = 0;
            r = 0;
            s = 0;
            return SemanticCoordinateAllowed(simulation.World, q, r);
        }

        for (var attempt = 0; attempt < 16; attempt++)
        {
            q = simulation.NextInclusive(minQ, maxQ);
            var minR = global::System.Math.Max(global::System.Math.Max(Data.SemanticMinR, -radius), -q - radius);
            var maxR = global::System.Math.Min(global::System.Math.Min(Data.SemanticMaxR, radius), -q + radius);
            if (minR > maxR)
            {
                continue;
            }

            r = simulation.NextInclusive(minR, maxR);
            s = -q - r;
            return true;
        }

        q = 0;
        r = 0;
        s = 0;
        return SemanticCoordinateAllowed(simulation.World, q, r);
    }

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
