using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class HexMapBackdropLayer : Node2D
{
    private HexMapProjection? _map;
    private PlaneMode _plane;
    private float _hexSize = 30f;

    public void Bind(HexMapProjection? map, PlaneMode plane, float hexSize)
    {
        _map = map;
        _plane = plane;
        _hexSize = hexSize;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_map is null || _plane != PlaneMode.Semantic)
        {
            return;
        }

        DrawSemanticRoseWindow();
    }

    private void DrawSemanticRoseWindow()
    {
        if (_map is null)
        {
            return;
        }

        var panes = new[]
        {
            AssetLibrary.PaneBreakaway,
            AssetLibrary.PaneWitness,
            AssetLibrary.PaneSeverance,
            AssetLibrary.PaneYoke,
            AssetLibrary.PaneSilence,
            AssetLibrary.PaneLineage,
        };
        var center = HexToPixel(new HexCoord(0, 0));
        var roseRadius = _hexSize * (_map.MapRadius + 1) * Mathf.Sqrt(3f);
        var verts = BuildRoseVertices(center, roseRadius);
        var uvs = new Vector2[] { new(0.5f, 1f), new(0f, 0f), new(1f, 0f) };
        var colors = new[] { Colors.White, Colors.White, Colors.White };

        var drew = 0;
        for (var i = 0; i < 6; i++)
        {
            var pane = panes[i];
            if (pane is null)
            {
                continue;
            }

            var triangle = new[] { center, verts[i], verts[(i + 1) % 6] };
            DrawPolygon(triangle, colors, uvs, pane);
            drew++;
        }

        if (drew == 0)
        {
            GD.PushWarning("[HexMapBackdropLayer] Semantic rose drew no pane textures.");
        }
    }

    private static Vector2[] BuildRoseVertices(Vector2 center, float radius)
    {
        var verts = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.DegToRad(60f * i);
            verts[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        return verts;
    }

    private Vector2 HexToPixel(HexCoord coord)
    {
        var x = _hexSize * (Mathf.Sqrt(3f) * coord.Q + Mathf.Sqrt(3f) / 2f * coord.R);
        var y = _hexSize * (1.5f * coord.R);
        return new Vector2(x, y);
    }
}
