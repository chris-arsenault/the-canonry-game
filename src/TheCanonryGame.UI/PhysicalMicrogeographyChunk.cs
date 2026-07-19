#pragma warning disable CA2213
using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

internal partial class PhysicalMicrogeographyChunk : Node2D, IPhysicalMicrogeographyCanvas
{
    private PhysicalMicrogeographyLayer? _owner;
    private PhysicalGeographyCellProjection[] _cells = [];
    private bool _cullingVisible = true;

    public Rect2 Bounds { get; private set; }

    public void Bind(
        PhysicalMicrogeographyLayer owner,
        PhysicalGeographyCellProjection[] cells,
        Rect2 bounds)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(cells);
        _owner = owner;
        _cells = cells;
        Bounds = bounds;
        QueueRedraw();
    }

    public void SetCullingVisible(bool visible)
    {
        if (_cullingVisible == visible)
        {
            return;
        }

        _cullingVisible = visible;
        Visible = visible;
        if (visible)
        {
            QueueRedraw();
        }
    }

    public void Refresh()
    {
        if (_cullingVisible)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_owner is null || (!_owner.DrawMicrotriangles && !_owner.DrawAggregateHexes))
        {
            return;
        }

        foreach (var cell in _cells)
        {
            if (!_owner.CellIsRenderable(cell))
            {
                continue;
            }

            DrawSetTransform(_owner.Geometry.HexCenter(cell.Coord));
            _owner.Painter.PaintCell(
                this,
                cell,
                _owner.View,
                _owner.FeaturesEnabled,
                _owner.DrawMicrotriangles);
        }

        DrawSetTransform(Vector2.Zero);
    }

    void IPhysicalMicrogeographyCanvas.FillPolygon(Vector2[] points, Color color) =>
        DrawColoredPolygon(points, color);

    void IPhysicalMicrogeographyCanvas.StrokePolyline(
        Vector2[] points,
        Color color,
        float width,
        bool antialiased) =>
        DrawPolyline(points, color, width, antialiased);

    void IPhysicalMicrogeographyCanvas.StrokeLine(
        Vector2 from,
        Vector2 destination,
        Color color,
        float width,
        bool antialiased) =>
        DrawLine(from, destination, color, width, antialiased);

    void IPhysicalMicrogeographyCanvas.FillCircle(Vector2 center, float radius, Color color) =>
        DrawCircle(center, radius, color);
}
