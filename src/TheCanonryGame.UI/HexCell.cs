using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public sealed record HexCellRenderItem(
    HexProjection Cell,
    PlaneMode Plane,
    float HexSize,
    string TerrainStableName,
    Color TerrainColor);

public partial class HexCell : Node2D
{
    private const float GeometryCacheScale = 1000f;

    private static readonly Dictionary<int, HexGeometry> GeometryCache = [];
    private HexCellRenderItem? _data;
    private HexCellRenderFingerprint? _lastFingerprint;

    public void Bind(HexCellRenderItem data)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);

        var fingerprint = HexCellRenderFingerprint.From(data);
        _data = data;
        if (_lastFingerprint == fingerprint)
        {
            return;
        }

        _lastFingerprint = fingerprint;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_data is null)
        {
            return;
        }

        DrawBase(_data);
    }

    private void DrawBase(HexCellRenderItem data)
    {
        var geometry = GetHexGeometry(data.HexSize);
        var tile = AssetLibrary.Terrain(data.TerrainStableName);
        if (tile is null)
        {
            DrawColoredPolygon(geometry.Points, data.TerrainColor);
        }
        else
        {
            DrawColoredPolygon(geometry.Points, Colors.White, geometry.Uvs, tile);
        }

        if (data.Plane == PlaneMode.Semantic)
        {
            DrawOverlayTexture(AssetLibrary.SemanticCellOverlay, data.HexSize, new Color(1f, 1f, 1f, 0.18f));
        }

        DrawPolyline(geometry.ClosedPoints, UiTokens.Hairline, 1f);
    }

    private void DrawOverlayTexture(Texture2D? texture, float hexSize, Color? modulate = null)
    {
        if (texture is null)
        {
            return;
        }

        var extent = hexSize * 2f;
        DrawTextureRect(
            texture,
            new Rect2(new Vector2(-extent / 2f, -extent / 2f), new Vector2(extent, extent)),
            false,
            modulate ?? Colors.White);
    }

    private static HexGeometry GetHexGeometry(float size)
    {
        var key = (int)MathF.Round(
            size * GeometryCacheScale,
            global::System.MidpointRounding.AwayFromZero);
        if (GeometryCache.TryGetValue(key, out var geometry))
        {
            return geometry;
        }

        geometry = BuildHexGeometry(size);
        GeometryCache[key] = geometry;
        return geometry;
    }

    private static HexGeometry BuildHexGeometry(float size)
    {
        var points = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.DegToRad(60f * i - 30f);
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size;
        }

        var closedPoints = new Vector2[7];
        for (var i = 0; i < points.Length; i++)
        {
            closedPoints[i] = points[i];
        }

        closedPoints[^1] = points[0];

        var uvs = new Vector2[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            uvs[i] = new Vector2(
                0.5f + points[i].X / (size * 2f),
                0.5f + points[i].Y / (size * 2f));
        }

        return new HexGeometry(points, closedPoints, uvs);
    }

    private sealed record HexGeometry(Vector2[] Points, Vector2[] ClosedPoints, Vector2[] Uvs);

    private readonly record struct HexCellRenderFingerprint(
        int HexId,
        PlaneMode Plane,
        int HexSizeKey,
        string TerrainStableName,
        Color TerrainColor)
    {
        public static HexCellRenderFingerprint From(HexCellRenderItem data) =>
            new(
                data.Cell.HexId,
                data.Plane,
                (int)MathF.Round(
                    data.HexSize * GeometryCacheScale,
                    global::System.MidpointRounding.AwayFromZero),
                data.TerrainStableName,
                data.TerrainColor);
    }
}
