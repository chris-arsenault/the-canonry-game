using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public sealed record HexInfluenceRenderItem(int EntityId, float Value, Color Color);

public sealed record HexCellRenderItem(
    HexProjection Cell,
    PlaneMode Plane,
    float HexSize,
    string TerrainStableName,
    string? TravelStructureStableName,
    Color TerrainColor,
    Color? OwnerColor,
    IReadOnlyList<HexInfluenceRenderItem> Influences,
    bool ShowTerritory,
    bool ShowInfluence,
    bool ShowRoads,
    bool ShowMapFeatures);

public partial class HexCell : Node2D
{
    private const float LightRoadThreshold = 0.25f;
    private const float HeavyRoadThreshold = 0.75f;

    private HexCellRenderItem? _data;

    public void Bind(HexCellRenderItem data)
    {
        _data = data;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_data is null)
        {
            return;
        }

        DrawBase(_data);
        DrawTerritory(_data);
        DrawInfluence(_data);
        DrawTravelStructure(_data);
        DrawRoad(_data);
    }

    private void DrawBase(HexCellRenderItem data)
    {
        var points = GetHexPoints(data.HexSize);
        var tile = AssetLibrary.Terrain(data.TerrainStableName);
        if (tile is null)
        {
            DrawColoredPolygon(points, data.TerrainColor);
        }
        else
        {
            DrawColoredPolygon(points, Colors.White, BuildHexUvs(points, data.HexSize), tile);
        }

        if (data.Plane == PlaneMode.Semantic)
        {
            DrawOverlayTexture(AssetLibrary.SemanticCellOverlay, data.HexSize, new Color(1f, 1f, 1f, 0.18f));
            DrawPolyline([.. points, points[0]], UiTokens.Hairline, 1f);
            return;
        }

        DrawPolyline([.. points, points[0]], UiTokens.Hairline, 1f);
    }

    private void DrawTerritory(HexCellRenderItem data)
    {
        if (!data.ShowTerritory || data.OwnerColor is not { } ownerColor)
        {
            return;
        }

        var alpha = data.Plane == PlaneMode.Semantic ? 0.28f : 0.36f;
        DrawColoredPolygon(GetHexPoints(data.HexSize * 0.85f), new Color(ownerColor, alpha));
    }

    private void DrawInfluence(HexCellRenderItem data)
    {
        if (!data.ShowInfluence)
        {
            return;
        }

        var band = 0;
        foreach (var influence in data.Influences)
        {
            var size = data.HexSize * (0.72f - band * 0.12f);
            if (size <= data.HexSize * 0.2f)
            {
                break;
            }

            var alpha = 0.08f + Mathf.Clamp(influence.Value / 100f, 0f, 1f) * 0.24f;
            var points = GetHexPoints(size);
            DrawColoredPolygon(points, new Color(influence.Color, alpha));
            DrawPolyline([.. points, points[0]], new Color(influence.Color, alpha + 0.08f), 1.2f, antialiased: true);
            band++;
        }
    }

    private void DrawRoad(HexCellRenderItem data)
    {
        if (!data.ShowRoads)
        {
            return;
        }

        var normalizedQuality = Mathf.Clamp(data.Cell.RoadQuality / 100f, 0f, 1f);
        if (normalizedQuality <= LightRoadThreshold)
        {
            return;
        }

        var heavy = normalizedQuality > HeavyRoadThreshold;
        var texture = heavy ? AssetLibrary.RoadHeavyOverlay : AssetLibrary.RoadLightOverlay;
        if (texture is not null)
        {
            DrawOverlayTexture(texture, data.HexSize);
            return;
        }

        var strokeWidth = heavy ? 4.8f : 2.4f;
        var roadColor = heavy
            ? new Color(UiTokens.Gilt.R, UiTokens.Gilt.G, UiTokens.Gilt.B, 0.96f)
            : new Color(UiTokens.Whisper.R, UiTokens.Whisper.G, UiTokens.Whisper.B, 0.72f);
        var shadowColor = new Color(UiTokens.Basalt.R, UiTokens.Basalt.G, UiTokens.Basalt.B, 0.82f);
        var road = new[]
        {
            new Vector2(-data.HexSize * 0.46f, data.HexSize * 0.12f),
            new Vector2(-data.HexSize * 0.08f, -data.HexSize * 0.09f),
            new Vector2(data.HexSize * 0.42f, data.HexSize * 0.07f),
        };

        DrawPolyline(road, shadowColor, strokeWidth + 2.4f, antialiased: true);
        DrawPolyline(road, roadColor, strokeWidth, antialiased: true);
        if (heavy)
        {
            DrawCircle(Vector2.Zero, strokeWidth * 0.55f, roadColor);
        }
    }

    private void DrawTravelStructure(HexCellRenderItem data)
    {
        if (!data.ShowMapFeatures || data.Cell.TravelStructureId <= 0)
        {
            return;
        }

        var texture = AssetLibrary.TravelStructureOverlay(data.TravelStructureStableName);
        if (texture is not null)
        {
            DrawOverlayTexture(texture, data.HexSize);
            return;
        }

        var bridgeColor = StructureColor(data.TravelStructureStableName);
        var shadow = new Color(UiTokens.Ink.R, UiTokens.Ink.G, UiTokens.Ink.B, 0.74f);
        var strokeWidth = data.HexSize * 0.12f;
        var span = new[]
        {
            new Vector2(-data.HexSize * 0.58f, 0f),
            new Vector2(-data.HexSize * 0.18f, data.HexSize * 0.08f),
            new Vector2(data.HexSize * 0.18f, -data.HexSize * 0.08f),
            new Vector2(data.HexSize * 0.58f, 0f),
        };

        DrawPolyline(span, shadow, strokeWidth + 2.6f, antialiased: true);
        DrawPolyline(span, bridgeColor, strokeWidth, antialiased: true);
        DrawCircle(Vector2.Zero, strokeWidth * 0.45f, bridgeColor);
    }

    private static Color StructureColor(string? stableName)
    {
        return stableName is not null && stableName.Contains("current", StringComparison.Ordinal)
            ? new Color(UiTokens.Aurora.R, UiTokens.Aurora.G, UiTokens.Aurora.B, 0.84f)
            : new Color(UiTokens.Gilt.R, UiTokens.Gilt.G, UiTokens.Gilt.B, 0.88f);
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

    private static Vector2[] BuildHexUvs(Vector2[] points, float hexSize)
    {
        var uvs = new Vector2[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            uvs[i] = new Vector2(
                0.5f + points[i].X / (hexSize * 2f),
                0.5f + points[i].Y / (hexSize * 2f));
        }

        return uvs;
    }

    private static Vector2[] GetHexPoints(float size)
    {
        var points = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.DegToRad(60f * i - 30f);
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size;
        }

        return points;
    }
}
