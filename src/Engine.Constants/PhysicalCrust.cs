namespace Engine.Constants;

/// <summary>
/// Isostasy: surface height is what crustal thickness floats to, not a value
/// painted onto the map.
///
/// The continental mask cannot supply interior height. To keep coastlines
/// crisp a mask has to saturate at "definitely land", and once it saturates it
/// contributes no gradient across the interior it just defined - which is what
/// made every continent a flat mesa ringed by cliffs. Thickness has no such
/// ceiling: two collisions stack where one collided once, rifting thins, and
/// erosion removes. Height then follows from it for free.
/// </summary>
public static class PhysicalCrust
{
    /// <summary>Thickness of unthickened continental crust, authored units.</summary>
    public const int ContinentalThicknessUnits = 4000;

    /// <summary>Thickness of oceanic crust, authored units.</summary>
    public const int OceanicThicknessUnits = 1000;

    /// <summary>
    /// Thickness that floats exactly at sea level. Crust thicker than this
    /// stands above the water, thinner sits below.
    /// </summary>
    public const int CompensationThicknessUnits = 3150;

    /// <summary>
    /// Height gained per unit of thickness above compensation, per mille.
    /// Calibrated so unthickened continent stands near 800 units and oceanic
    /// crust sits near 2000 below, matching the hypsometry the world already
    /// had before height derived from thickness.
    /// </summary>
    public const int BuoyancyPerMille = 933;

    /// <summary>Surface height in authored units for a given thickness.</summary>
    public static double HeightUnits(double thicknessUnits) =>
        (thicknessUnits - CompensationThicknessUnits) * (BuoyancyPerMille / 1000d);
}
