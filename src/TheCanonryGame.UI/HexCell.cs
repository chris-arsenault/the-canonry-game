using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public sealed record HexInfluenceRenderItem(int EntityId, float Value, Color Color);

public sealed record HexCultureInfluenceRenderItem(int CultureId, float Share, Color Color);

public sealed record HexCellRenderItem(
    HexProjection Cell,
    PlaneMode Plane,
    float HexSize,
    string TerrainStableName,
    string? TravelStructureStableName,
    Color TerrainColor,
    Color? OwnerColor,
    IReadOnlyList<HexInfluenceRenderItem> Influences,
    IReadOnlyList<HexCultureInfluenceRenderItem> CultureInfluences,
    bool ShowTerritory,
    bool ShowCultureInfluence,
    bool ShowInfluence,
    bool ShowRoads,
    bool ShowMapFeatures);

public partial class HexCell : Node2D
{
    private const float LightRoadThreshold = 0.25f;
    private const float HeavyRoadThreshold = 0.75f;
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
        DrawTerritory(_data);
        DrawCultureInfluence(_data);
        DrawInfluence(_data);
        DrawTravelStructure(_data);
        DrawRoad(_data);
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
            DrawPolyline(geometry.ClosedPoints, UiTokens.Hairline, 1f);
            return;
        }

        DrawPolyline(geometry.ClosedPoints, UiTokens.Hairline, 1f);
    }

    private void DrawTerritory(HexCellRenderItem data)
    {
        if (!data.ShowTerritory || data.OwnerColor is not { } ownerColor)
        {
            return;
        }

        var alpha = data.Plane == PlaneMode.Semantic ? 0.28f : 0.36f;
        DrawColoredPolygon(GetHexGeometry(data.HexSize * 0.85f).Points, new Color(ownerColor, alpha));
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
            var geometry = GetHexGeometry(size);
            DrawColoredPolygon(geometry.Points, new Color(influence.Color, alpha));
            DrawPolyline(geometry.ClosedPoints, new Color(influence.Color, alpha + 0.08f), 1.2f, antialiased: true);
            band++;
        }
    }

    private void DrawCultureInfluence(HexCellRenderItem data)
    {
        if (!data.ShowCultureInfluence || data.CultureInfluences.Count == 0)
        {
            return;
        }

        var dominant = data.CultureInfluences[0];
        var fillAlpha = 0.08f + Mathf.Clamp(dominant.Share / 100f, 0f, 1f) * 0.18f;
        DrawColoredPolygon(GetHexGeometry(data.HexSize * 0.92f).Points, new Color(dominant.Color, fillAlpha));

        var radius = data.HexSize * 0.76f;
        var width = MathF.Max(2.4f, data.HexSize * 0.12f);
        var start = -Mathf.Pi / 2f;
        foreach (var influence in data.CultureInfluences)
        {
            var share = Mathf.Clamp(influence.Share / 100f, 0f, 1f);
            var end = start + (Mathf.Tau * share);
            if (share < 0.02f)
            {
                start = end;
                continue;
            }

            DrawArc(
                Vector2.Zero,
                radius,
                start,
                end,
                Math.Max(5, (int)MathF.Ceiling(18 * share)),
                new Color(influence.Color, 0.88f),
                width,
                antialiased: true);
            start = end;
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
        string? TravelStructureStableName,
        Color TerrainColor,
        Color? OwnerColor,
        bool ShowTerritory,
        bool ShowCultureInfluence,
        bool ShowInfluence,
        bool ShowRoads,
        bool ShowMapFeatures,
        int RoadBucket,
        int TravelStructureId,
        int Influence0EntityId,
        int Influence0Value,
        int Influence1EntityId,
        int Influence1Value,
        int Influence2EntityId,
        int Influence2Value,
        int Influence3EntityId,
        int Influence3Value,
        int Culture0Id,
        int Culture0Share,
        int Culture1Id,
        int Culture1Share,
        int Culture2Id,
        int Culture2Share,
        int Culture3Id,
        int Culture3Share)
    {
        public static HexCellRenderFingerprint From(HexCellRenderItem data)
        {
            var influence0 = InfluenceAt(data.Influences, 0);
            var influence1 = InfluenceAt(data.Influences, 1);
            var influence2 = InfluenceAt(data.Influences, 2);
            var influence3 = InfluenceAt(data.Influences, 3);
            var culture0 = CultureAt(data.CultureInfluences, 0);
            var culture1 = CultureAt(data.CultureInfluences, 1);
            var culture2 = CultureAt(data.CultureInfluences, 2);
            var culture3 = CultureAt(data.CultureInfluences, 3);
            return new HexCellRenderFingerprint(
                data.Cell.HexId,
                data.Plane,
                (int)MathF.Round(
                    data.HexSize * GeometryCacheScale,
                    global::System.MidpointRounding.AwayFromZero),
                data.TerrainStableName,
                data.TravelStructureStableName,
                data.TerrainColor,
                data.OwnerColor,
                data.ShowTerritory,
                data.ShowCultureInfluence,
                data.ShowInfluence,
                data.ShowRoads,
                data.ShowMapFeatures,
                ComputeRoadBucket(data.Cell.RoadQuality),
                data.Cell.TravelStructureId,
                influence0.EntityId,
                influence0.Value,
                influence1.EntityId,
                influence1.Value,
                influence2.EntityId,
                influence2.Value,
                influence3.EntityId,
                influence3.Value,
                culture0.CultureId,
                culture0.Share,
                culture1.CultureId,
                culture1.Share,
                culture2.CultureId,
                culture2.Share,
                culture3.CultureId,
                culture3.Share);
        }

        private static (int EntityId, int Value) InfluenceAt(IReadOnlyList<HexInfluenceRenderItem> influences, int index) =>
            index < influences.Count
                ? (influences[index].EntityId, PercentBucket(influences[index].Value))
                : default;

        private static (int CultureId, int Share) CultureAt(IReadOnlyList<HexCultureInfluenceRenderItem> influences, int index) =>
            index < influences.Count
                ? (influences[index].CultureId, PercentBucket(influences[index].Share))
                : default;

        private static int PercentBucket(float value) =>
            (int)MathF.Round(
                Math.Clamp(value, 0f, 100f),
                global::System.MidpointRounding.AwayFromZero);

        private static int ComputeRoadBucket(float quality) =>
            quality switch
            {
                > HeavyRoadThreshold * 100f => 2,
                > LightRoadThreshold * 100f => 1,
                _ => 0,
            };
    }
}
