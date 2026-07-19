namespace TheCanonryGame.UI;

public enum PhysicalGeographyView
{
    Terrain,
    Surface,
    NaturalSurface,
    Material,
    Elevation,
    Temperature,
    Moisture,
    Geothermal,
    OceanCurrents,
    WaveExposure,
    Precipitation,
    SeaIceSuitability,
    Sediment,
    ErosionHistory,
    MaximumGlaciation,
    Landform,
    Hydrology,
    RiverGeometry,
    Ice,
    Fractures,
    TectonicProvinces,
    CrustType,
    TectonicBoundaries,
    TectonicStrain,
    Fire,
    Aurora,
    Pressure,
    MagicComposite,
    MagicHistory,
}

internal enum PhysicalMicrogeographyFeatureDetail
{
    None,
    Aggregate,
    Microtriangle,
}

internal static class PhysicalMicrogeographyRenderDecisions
{
    public static PhysicalMicrogeographyFeatureDetail FeatureDetail(
        bool featuresEnabled,
        bool drawMicrotriangles,
        bool drawAggregateHexes)
    {
        if (drawMicrotriangles && drawAggregateHexes)
        {
            throw new InvalidOperationException(
                "Microtriangle and aggregate map geometry cannot be drawn at the same LOD.");
        }

        if (!featuresEnabled)
        {
            return PhysicalMicrogeographyFeatureDetail.None;
        }

        if (drawMicrotriangles)
        {
            return PhysicalMicrogeographyFeatureDetail.Microtriangle;
        }

        return drawAggregateHexes
            ? PhysicalMicrogeographyFeatureDetail.Aggregate
            : PhysicalMicrogeographyFeatureDetail.None;
    }

    public static bool OwnsSharedHexEdge(
        int q,
        int r,
        int sector,
        bool neighborExists)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sector);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(sector, 6);
        if (!neighborExists)
        {
            return true;
        }

        var (neighborQ, neighborR) = sector switch
        {
            0 => (q + 1, r),
            1 => (q + 1, r - 1),
            2 => (q, r - 1),
            3 => (q - 1, r),
            4 => (q - 1, r + 1),
            _ => (q, r + 1),
        };
        return q < neighborQ || (q == neighborQ && r < neighborR);
    }

    public static bool UsesWaterUnderlay(PhysicalGeographyView view) =>
        view is PhysicalGeographyView.Terrain or
            PhysicalGeographyView.Surface or
            PhysicalGeographyView.NaturalSurface ||
        view is >= PhysicalGeographyView.Material and <= PhysicalGeographyView.Landform ||
        view == PhysicalGeographyView.RiverGeometry;

    public static bool UsesStructuralRiverOverlay(PhysicalGeographyView view) =>
        view is PhysicalGeographyView.Terrain or
            PhysicalGeographyView.Surface or
            PhysicalGeographyView.NaturalSurface or
            PhysicalGeographyView.Hydrology or
            PhysicalGeographyView.RiverGeometry;

    public static bool UsesReliefShading(PhysicalGeographyView view) =>
        view is PhysicalGeographyView.Terrain or
            PhysicalGeographyView.Surface or
            PhysicalGeographyView.NaturalSurface;
}
