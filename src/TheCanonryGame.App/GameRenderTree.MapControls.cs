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
        if (!_mapRenderer.SetGeographyView(view))
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
            value => _mapRenderer.SetGeographyFeaturesEnabled(value)),
        _ => false,
    };

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

    private static PhysicalGeographyView GeographyViewFor(string viewKey) => viewKey switch
    {
        MapArea.GeographyViewTerrain => PhysicalGeographyView.Terrain,
        MapArea.GeographyViewElevation => PhysicalGeographyView.Elevation,
        MapArea.GeographyViewTemperature => PhysicalGeographyView.Temperature,
        MapArea.GeographyViewMoisture => PhysicalGeographyView.Moisture,
        MapArea.GeographyViewGeothermal => PhysicalGeographyView.Geothermal,
        MapArea.GeographyViewLandform => PhysicalGeographyView.Landform,
        MapArea.GeographyViewHydrology => PhysicalGeographyView.Hydrology,
        _ => PhysicalGeographyView.Material,
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
