using Engine.Projection;

using Godot;

namespace TheCanonryGame.UI;

public sealed partial class PhysicalMicrogeographyPainter
{
    internal Color ResolveTriangleColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle,
        PhysicalGeographyView view,
        bool applyReliefShading)
    {
        if (_canvas is not null)
        {
            throw new InvalidOperationException(
                "Triangle colors cannot be resolved while the physical-geography painter is drawing.");
        }

        _owner.View = view;
        _owner.FeaturesEnabled = false;
        _owner.DrawMicrotriangles = true;
        _owner.DrawAggregateHexes = false;
        _owner.ApplyReliefShading = applyReliefShading;
        return TriangleColor(cell, triangleIndex, triangle);
    }

    private Color ApplyTriangleRelief(
        Color color,
        in PhysicalMicrotriangleProjection triangle,
        PhysicalGeographyView view) =>
        _owner.ApplyReliefShading &&
        PhysicalMicrogeographyRenderDecisions.UsesReliefShading(view)
            ? ReliefShade(color, triangle.Elevation, triangle.Slope, triangle.Slope)
            : color;
}

/// <summary>
/// Presentation-only triangle styling shared by the flat map and the 3D
/// geography inspector. The inspector disables scalar relief multiplication
/// because displaced geometry and lighting own relief there.
/// </summary>
public sealed class PhysicalMicrogeographyStyle
{
    private readonly PhysicalMicrogeographyPainter _painter;

    public PhysicalMicrogeographyStyle(
        PhysicalGeographyProjection geography,
        PhysicalMicrogeographyGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geography);
        ArgumentNullException.ThrowIfNull(geometry);
        _painter = new PhysicalMicrogeographyPainter(geography, geometry);
    }

    public Color TriangleColor(
        in PhysicalGeographyCellProjection cell,
        int triangleIndex,
        in PhysicalMicrotriangleProjection triangle,
        PhysicalGeographyView view) =>
        _painter.ResolveTriangleColor(
            cell,
            triangleIndex,
            triangle,
            view,
            applyReliefShading: false);
}
