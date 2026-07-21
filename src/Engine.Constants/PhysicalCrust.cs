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
    public const int CompensationThicknessUnits = 2570;

    /// <summary>
    /// Height gained per unit of thickness above compensation, per mille.
    /// Calibrated so the mask term alone reproduces the range the painted
    /// sigmoid produced: oceanic crust at -900 authored units, unthickened
    /// continent at +820. Deriving height from thickness is meant to change
    /// where relief can come from, not to move the world's hypsometry, so
    /// this phase is numerically neutral by construction and anything that
    /// moves is attributable to the change in structure.
    /// </summary>
    public const int BuoyancyPerMille = 573;

    /// <summary>
    /// Half-range of per-province thickness variation, authored units.
    /// Continents are assembled from terranes that were never the same
    /// thickness. The continental mask cannot express that, because it
    /// saturates the moment it is confident the ground is land, so province
    /// identity carries the variation the mask cannot.
    /// </summary>
    public const int ProvinceThicknessRangeUnits = 520;

    /// <summary>
    /// Thickness bounds, chosen so derived height stays inside the elevation
    /// range the pipeline validates against. Crust cannot be arbitrarily thick
    /// any more than it can be arbitrarily thin; without this the relief terms
    /// can stack past the legal band.
    /// </summary>
    public const int MinimumThicknessUnits = 800;

    /// <inheritdoc cref="MinimumThicknessUnits"/>
    public const int MaximumThicknessUnits = 5700;

    /// <summary>Surface height in authored units for a given thickness.</summary>
    public static double HeightUnits(double thicknessUnits) =>
        (ClampThicknessUnits(thicknessUnits) - CompensationThicknessUnits) *
        (BuoyancyPerMille / 1000d);

    /// <summary>Thickness held inside the band height can legally express.</summary>
    public static double ClampThicknessUnits(double thicknessUnits) =>
        global::System.Math.Clamp(thicknessUnits, MinimumThicknessUnits, MaximumThicknessUnits);
}
