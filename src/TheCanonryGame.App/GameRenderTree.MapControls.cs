using TheCanonryGame.UI;

namespace TheCanonryGame.App;

internal sealed partial class GameRenderTree
{
    public void SetMapLayerVisible(string layerKey, bool visible)
    {
        if (!ApplyMapLayerVisibility(layerKey, visible))
        {
            return;
        }

        _mapLensKey = MapArea.LensCustom;
        _mapArea.SetLensKey(_mapLensKey);
        _mapArea.SetLayerVisible(layerKey, visible);
        _mapRender.InvalidateFrame();
    }

    public void SetMapLens(string lensKey)
    {
        _mapLensKey = lensKey switch
        {
            MapArea.LensCulture => MapArea.LensCulture,
            MapArea.LensControl => MapArea.LensControl,
            MapArea.LensInfluence => MapArea.LensInfluence,
            _ => MapArea.LensDefault,
        };
        ApplyMapLens();
        _mapRender.InvalidateFrame();
    }

    public void SetGeographyView(string viewKey)
    {
        var view = GeographyViewFor(viewKey);
        var changed2D = _mapRenderer.SetGeographyView(view);
        _physicalMap3DInspector.SetView(view);
        if (!changed2D && !Physical3DInspectionActive)
        {
            return;
        }

        _mapArea.SetGeographyViewKey(viewKey);
        ClearTooltip();
    }

    private bool ApplyMapLayerVisibility(string layerKey, bool visible) =>
        ApplyGeographyLayerVisibility(layerKey, visible) ||
        ApplySimulationLayerVisibility(layerKey, visible);

    private bool ApplyGeographyLayerVisibility(string layerKey, bool visible) => layerKey switch
    {
        MapArea.LayerWorldgenInspection => SetLayerVisibility(
            _mapRenderer.WorldgenInspectionEnabled,
            visible,
            value => _mapRenderer.SetWorldgenInspectionEnabled(value)),
        MapArea.LayerGeographyFeatures => SetLayerVisibility(
            _mapRenderer.GeographyFeaturesEnabled,
            visible,
            SetGeographyFeaturesVisible),
        _ => false,
    };

    private void SetGeographyFeaturesVisible(bool visible)
    {
        _ = _mapRenderer.SetGeographyFeaturesEnabled(visible);
        _physicalMap3DInspector.SetFeaturesVisible(visible);
    }

    private bool ApplySimulationLayerVisibility(string layerKey, bool visible) => layerKey switch
    {
        MapArea.LayerRoads => SetLayerVisibility(_mapRenderer.ShowRoads, visible, value => _mapRenderer.ShowRoads = value),
        MapArea.LayerRoutes => SetLayerVisibility(_mapRenderer.ShowRoutes, visible, value => _mapRenderer.ShowRoutes = value),
        MapArea.LayerBorders => SetLayerVisibility(_mapRenderer.ShowBorders, visible, value => _mapRenderer.ShowBorders = value),
        MapArea.LayerLabels => SetLayerVisibility(_mapRenderer.ShowLabels, visible, value => _mapRenderer.ShowLabels = value),
        MapArea.LayerTerritory => SetLayerVisibility(_mapRenderer.ShowTerritory, visible, value => _mapRenderer.ShowTerritory = value),
        MapArea.LayerInfluence => SetLayerVisibility(_mapRenderer.ShowInfluence, visible, value => _mapRenderer.ShowInfluence = value),
        MapArea.LayerLabor => SetLayerVisibility(_mapRenderer.ShowMinorActors, visible, value => _mapRenderer.ShowMinorActors = value),
        MapArea.LayerCultureInfluence => SetLayerVisibility(
            _mapRenderer.ShowCultureInfluence,
            visible,
            value => _mapRenderer.ShowCultureInfluence = value),
        _ => false,
    };

    private static PhysicalGeographyView GeographyViewFor(string viewKey) =>
        BaseGeographyViewFor(viewKey) ?? SpecialGeographyViewFor(viewKey);

    private static PhysicalGeographyView? BaseGeographyViewFor(string viewKey) => viewKey switch
    {
        MapArea.GeographyViewTerrain => PhysicalGeographyView.Terrain,
        MapArea.GeographyViewSurface => PhysicalGeographyView.Surface,
        MapArea.GeographyViewNaturalSurface => PhysicalGeographyView.NaturalSurface,
        MapArea.GeographyViewMaterial => PhysicalGeographyView.Material,
        MapArea.GeographyViewElevation => PhysicalGeographyView.Elevation,
        MapArea.GeographyViewTemperature => PhysicalGeographyView.Temperature,
        MapArea.GeographyViewMoisture => PhysicalGeographyView.Moisture,
        MapArea.GeographyViewGeothermal => PhysicalGeographyView.Geothermal,
        _ => null,
    };

    private static PhysicalGeographyView SpecialGeographyViewFor(string viewKey) =>
        GeneratedProcessViewFor(viewKey) ??
        TectonicGeographyViewFor(viewKey) ??
        MagicGeographyViewFor(viewKey) ??
        RemainingGeographyViewFor(viewKey);

    private static PhysicalGeographyView? GeneratedProcessViewFor(string viewKey) => viewKey switch
    {
        MapArea.GeographyViewOceanCurrents => PhysicalGeographyView.OceanCurrents,
        MapArea.GeographyViewWaveExposure => PhysicalGeographyView.WaveExposure,
        MapArea.GeographyViewPrecipitation => PhysicalGeographyView.Precipitation,
        MapArea.GeographyViewSeaIceSuitability => PhysicalGeographyView.SeaIceSuitability,
        MapArea.GeographyViewSediment => PhysicalGeographyView.Sediment,
        MapArea.GeographyViewErosionHistory => PhysicalGeographyView.ErosionHistory,
        MapArea.GeographyViewMaximumGlaciation => PhysicalGeographyView.MaximumGlaciation,
        MapArea.GeographyViewRiverGeometry => PhysicalGeographyView.RiverGeometry,
        _ => null,
    };

    private static PhysicalGeographyView? TectonicGeographyViewFor(string viewKey) => viewKey switch
    {
        MapArea.GeographyViewTectonicProvinces => PhysicalGeographyView.TectonicProvinces,
        MapArea.GeographyViewCrustType => PhysicalGeographyView.CrustType,
        MapArea.GeographyViewTectonicBoundaries => PhysicalGeographyView.TectonicBoundaries,
        MapArea.GeographyViewTectonicStrain => PhysicalGeographyView.TectonicStrain,
        _ => null,
    };

    private static PhysicalGeographyView RemainingGeographyViewFor(string viewKey) => viewKey switch
    {
        MapArea.GeographyViewLandform => PhysicalGeographyView.Landform,
        MapArea.GeographyViewHydrology => PhysicalGeographyView.Hydrology,
        MapArea.GeographyViewIce => PhysicalGeographyView.Ice,
        MapArea.GeographyViewFractures => PhysicalGeographyView.Fractures,
        _ => PhysicalGeographyView.Surface,
    };

    private static PhysicalGeographyView? MagicGeographyViewFor(string viewKey) => viewKey switch
    {
        MapArea.GeographyViewFire => PhysicalGeographyView.Fire,
        MapArea.GeographyViewAurora => PhysicalGeographyView.Aurora,
        MapArea.GeographyViewPressure => PhysicalGeographyView.Pressure,
        MapArea.GeographyViewMagicComposite => PhysicalGeographyView.MagicComposite,
        MapArea.GeographyViewMagicHistory => PhysicalGeographyView.MagicHistory,
        _ => null,
    };

    private static string GeographyViewKey(PhysicalGeographyView view) =>
        BaseGeographyViewKey(view) ?? SpecialGeographyViewKey(view);

    private static string? BaseGeographyViewKey(PhysicalGeographyView view) => view switch
    {
        PhysicalGeographyView.Terrain => MapArea.GeographyViewTerrain,
        PhysicalGeographyView.Surface => MapArea.GeographyViewSurface,
        PhysicalGeographyView.NaturalSurface => MapArea.GeographyViewNaturalSurface,
        PhysicalGeographyView.Material => MapArea.GeographyViewMaterial,
        PhysicalGeographyView.Elevation => MapArea.GeographyViewElevation,
        PhysicalGeographyView.Temperature => MapArea.GeographyViewTemperature,
        PhysicalGeographyView.Moisture => MapArea.GeographyViewMoisture,
        PhysicalGeographyView.Geothermal => MapArea.GeographyViewGeothermal,
        _ => null,
    };

    private static string SpecialGeographyViewKey(PhysicalGeographyView view) =>
        GeneratedProcessViewKey(view) ??
        TectonicGeographyViewKey(view) ??
        MagicGeographyViewKey(view) ??
        RemainingGeographyViewKey(view);

    private static string? GeneratedProcessViewKey(PhysicalGeographyView view) => view switch
    {
        PhysicalGeographyView.OceanCurrents => MapArea.GeographyViewOceanCurrents,
        PhysicalGeographyView.WaveExposure => MapArea.GeographyViewWaveExposure,
        PhysicalGeographyView.Precipitation => MapArea.GeographyViewPrecipitation,
        PhysicalGeographyView.SeaIceSuitability => MapArea.GeographyViewSeaIceSuitability,
        PhysicalGeographyView.Sediment => MapArea.GeographyViewSediment,
        PhysicalGeographyView.ErosionHistory => MapArea.GeographyViewErosionHistory,
        PhysicalGeographyView.MaximumGlaciation => MapArea.GeographyViewMaximumGlaciation,
        PhysicalGeographyView.RiverGeometry => MapArea.GeographyViewRiverGeometry,
        _ => null,
    };

    private static string? TectonicGeographyViewKey(PhysicalGeographyView view) => view switch
    {
        PhysicalGeographyView.TectonicProvinces => MapArea.GeographyViewTectonicProvinces,
        PhysicalGeographyView.CrustType => MapArea.GeographyViewCrustType,
        PhysicalGeographyView.TectonicBoundaries => MapArea.GeographyViewTectonicBoundaries,
        PhysicalGeographyView.TectonicStrain => MapArea.GeographyViewTectonicStrain,
        _ => null,
    };

    private static string RemainingGeographyViewKey(PhysicalGeographyView view) => view switch
    {
        PhysicalGeographyView.Landform => MapArea.GeographyViewLandform,
        PhysicalGeographyView.Hydrology => MapArea.GeographyViewHydrology,
        PhysicalGeographyView.Ice => MapArea.GeographyViewIce,
        PhysicalGeographyView.Fractures => MapArea.GeographyViewFractures,
        _ => MapArea.GeographyViewSurface,
    };

    private static string? MagicGeographyViewKey(PhysicalGeographyView view) => view switch
    {
        PhysicalGeographyView.Fire => MapArea.GeographyViewFire,
        PhysicalGeographyView.Aurora => MapArea.GeographyViewAurora,
        PhysicalGeographyView.Pressure => MapArea.GeographyViewPressure,
        PhysicalGeographyView.MagicComposite => MapArea.GeographyViewMagicComposite,
        PhysicalGeographyView.MagicHistory => MapArea.GeographyViewMagicHistory,
        _ => null,
    };

    private static bool SetLayerVisibility(bool current, bool visible, Action<bool> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        if (current == visible)
        {
            return false;
        }

        apply(visible);
        return true;
    }
}
