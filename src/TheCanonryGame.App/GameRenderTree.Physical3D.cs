using TheCanonryGame.UI;

namespace TheCanonryGame.App;

internal sealed partial class GameRenderTree
{
    public void SetPhysical3DInspection(bool active)
    {
        if (Physical3DInspectionActive == active)
        {
            return;
        }

        if (active)
        {
            PreparePhysical3DEntry();
        }

        _mapRenderer.Visible = !active;
        _entityLayer.Visible = !active;
        _ = _physicalMap3DInspector.SetActive(active);
        _topBar.SetPhysical3DInspectionEnabled(active);
        _mapArea.SetPhysical3DInspectionEnabled(active);
        _mapArea.SetPlane(active ? PlaneMode.Physical : _planeBeforePhysical3D);
        ClearTooltip();
        if (!active)
        {
            RestorePhysical3DExit();
        }
    }

    public void SetPhysical3DVerticalExaggeration(float exaggeration) =>
        _physicalMap3DInspector.SetVerticalExaggeration(exaggeration);

    public void SetPhysical3DWireframeVisible(bool visible) =>
        _physicalMap3DInspector.SetWireframeVisible(visible);

    public void SetPhysical3DWaterVisible(bool visible) =>
        _physicalMap3DInspector.SetWaterVisible(visible);

    private void PreparePhysical3DEntry()
    {
        _planeBeforePhysical3D = _mapRenderer.CurrentPlane;
        _physicalMap3DInspector.SetView(_mapRenderer.GeographyView);
        _physicalMap3DInspector.SetFeaturesVisible(_mapRenderer.GeographyFeaturesEnabled);
    }

    private void RestorePhysical3DExit()
    {
        _mapRenderer.CurrentPlane = _planeBeforePhysical3D;
        _ = _entityLayer.SetPlane(_planeBeforePhysical3D);
        _mapRender.Invalidate();
        _entityMapRender.Invalidate();
    }
}
