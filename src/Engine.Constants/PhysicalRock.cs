namespace Engine.Constants;

/// <summary>
/// Rock durability against erosion, per mille.
/// </summary>
public static class PhysicalRock
{
    /// <summary>Ordinary rock. Resistance is expressed relative to this.</summary>
    public const int BaselineResistance = 1000;

    /// <summary>Softest rock erosion will see, so nothing erodes instantly.</summary>
    public const int MinimumResistance = 250;

    /// <summary>Hardest rock erosion will see, so nothing is permanent.</summary>
    public const int MaximumResistance = 4000;

    /// <summary>
    /// Resistance an impact site gains at its centre. Shocked rock is
    /// genuinely among the most durable on a planet, which is why old craters
    /// survive as rings long after everything around them has worn flat.
    /// </summary>
    public const int ImpactShockResistance = 2600;

    public static int Clamp(int resistance) =>
        global::System.Math.Clamp(resistance, MinimumResistance, MaximumResistance);
}
