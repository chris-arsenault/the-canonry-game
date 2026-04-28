using Godot;

namespace TheCanonryGame.UI.Theme;

/// <summary>
/// Draws per-kind glyphs — polygonal silhouettes that act as sprites for each entity kind.
/// Used by HexMapRenderer for on-map markers and by KindGlyphRect for list-row icons in
/// the Registry sidebar. A single source of truth keeps the sprite vocabulary consistent
/// wherever a kind is depicted.
///
/// All glyphs fit within a unit square centered on `center`, scaled by `size`. The fill
/// color is the kind accent; the outline is the deepest ink so the glyph holds its shape
/// on any terrain.
/// </summary>
public static class KindGlyph
{
    public static void Draw(CanvasItem ci, string kindId, Vector2 center, float size, Color fill)
    {
        System.ArgumentNullException.ThrowIfNull(ci);
        switch (kindId)
        {
            case "penguin": DrawPenguin(ci, center, size, fill); break;
            case "settlement": DrawSettlement(ci, center, size, fill); break;
            case "faction": DrawFaction(ci, center, size, fill); break;
            case "artifact": DrawArtifact(ci, center, size, fill); break;
            case "region": DrawRegion(ci, center, size, fill); break;
            case "orca": DrawOrca(ci, center, size, fill); break;
            default: DrawDefault(ci, center, size, fill); break;
        }
    }

    // Penguin: teardrop body + small round head. Classic silhouette.
    private static void DrawPenguin(CanvasItem ci, Vector2 c, float s, Color fill)
    {
        var bodyTop = c + new Vector2(0f, -s * 0.10f);
        var bodyLeft = c + new Vector2(-s * 0.55f, s * 0.30f);
        var bodyRight = c + new Vector2(s * 0.55f, s * 0.30f);
        var bodyBottom = c + new Vector2(0f, s * 0.75f);
        var body = new Vector2[]
        {
            bodyTop,
            c + new Vector2(-s * 0.40f, 0f),
            bodyLeft,
            bodyBottom,
            bodyRight,
            c + new Vector2(s * 0.40f, 0f),
        };
        ci.DrawColoredPolygon(body, fill);
        ci.DrawPolyline(Closed(body), UiTokens.Ink, 1f, antialiased: true);

        var headCenter = c + new Vector2(0f, -s * 0.55f);
        ci.DrawCircle(headCenter, s * 0.30f, fill);
        ci.DrawCircle(headCenter, s * 0.30f, UiTokens.Ink, filled: false);

        // beak — a small triangle
        var beak = new Vector2[]
        {
            headCenter + new Vector2(s * 0.22f, -s * 0.04f),
            headCenter + new Vector2(s * 0.45f, 0f),
            headCenter + new Vector2(s * 0.22f, s * 0.08f),
        };
        ci.DrawColoredPolygon(beak, UiTokens.Gilt);
    }

    // Settlement: trapezoidal tent body with a roof peak, like a cartographer's marker.
    private static void DrawSettlement(CanvasItem ci, Vector2 c, float s, Color fill)
    {
        var pts = new Vector2[]
        {
            c + new Vector2(0f, -s * 0.75f),              // peak
            c + new Vector2(s * 0.70f, -s * 0.15f),       // right shoulder
            c + new Vector2(s * 0.55f, s * 0.65f),        // right foot
            c + new Vector2(-s * 0.55f, s * 0.65f),       // left foot
            c + new Vector2(-s * 0.70f, -s * 0.15f),      // left shoulder
        };
        ci.DrawColoredPolygon(pts, fill);
        ci.DrawPolyline(Closed(pts), UiTokens.Ink, 1.2f, antialiased: true);

        // Doorway notch — small dark rectangle at base
        var door = new Rect2(c.X - s * 0.14f, c.Y + s * 0.15f, s * 0.28f, s * 0.50f);
        ci.DrawRect(door, UiTokens.Ink);
    }

    // Faction: rectangular banner with a triangular cutout on the fly end.
    private static void DrawFaction(CanvasItem ci, Vector2 c, float s, Color fill)
    {
        // Pole
        ci.DrawLine(
            c + new Vector2(-s * 0.55f, -s * 0.80f),
            c + new Vector2(-s * 0.55f, s * 0.80f),
            UiTokens.Ink, 2f);

        // Banner with swallowtail
        var banner = new Vector2[]
        {
            c + new Vector2(-s * 0.55f, -s * 0.70f),
            c + new Vector2(s * 0.70f, -s * 0.70f),
            c + new Vector2(s * 0.40f, -s * 0.15f),
            c + new Vector2(s * 0.70f, s * 0.40f),
            c + new Vector2(-s * 0.55f, s * 0.40f),
        };
        ci.DrawColoredPolygon(banner, fill);
        ci.DrawPolyline(Closed(banner), UiTokens.Ink, 1f, antialiased: true);
    }

    // Artifact: diamond (rotated square) with a smaller inner diamond.
    private static void DrawArtifact(CanvasItem ci, Vector2 c, float s, Color fill)
    {
        var outer = new Vector2[]
        {
            c + new Vector2(0f, -s * 0.80f),
            c + new Vector2(s * 0.65f, 0f),
            c + new Vector2(0f, s * 0.80f),
            c + new Vector2(-s * 0.65f, 0f),
        };
        ci.DrawColoredPolygon(outer, fill);
        ci.DrawPolyline(Closed(outer), UiTokens.Ink, 1.2f, antialiased: true);

        var inner = new Vector2[]
        {
            c + new Vector2(0f, -s * 0.40f),
            c + new Vector2(s * 0.32f, 0f),
            c + new Vector2(0f, s * 0.40f),
            c + new Vector2(-s * 0.32f, 0f),
        };
        ci.DrawPolyline(Closed(inner), UiTokens.Ink, 1f, antialiased: true);
    }

    // Region: outlined hex (pointy-top) with small dot at center.
    private static void DrawRegion(CanvasItem ci, Vector2 c, float s, Color fill)
    {
        var pts = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.DegToRad(60f * i - 30f);
            pts[i] = c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (s * 0.80f);
        }
        ci.DrawColoredPolygon(pts, UiTokens.WithAlpha(fill, 0.35f));
        ci.DrawPolyline(Closed(pts), fill, 1.2f, antialiased: true);
        ci.DrawCircle(c, s * 0.10f, fill);
    }

    // Orca: dorsal fin shape — a tall curved triangle.
    private static void DrawOrca(CanvasItem ci, Vector2 c, float s, Color fill)
    {
        var fin = new Vector2[]
        {
            c + new Vector2(-s * 0.40f, s * 0.70f),
            c + new Vector2(-s * 0.10f, -s * 0.80f),
            c + new Vector2(s * 0.20f, -s * 0.60f),
            c + new Vector2(s * 0.55f, s * 0.70f),
        };
        ci.DrawColoredPolygon(fin, fill);
        ci.DrawPolyline(Closed(fin), UiTokens.Ink, 1.2f, antialiased: true);

        // A small white patch near the base
        ci.DrawCircle(c + new Vector2(0f, s * 0.55f), s * 0.10f, UiTokens.Vellum);
    }

    private static void DrawDefault(CanvasItem ci, Vector2 c, float s, Color fill)
    {
        ci.DrawCircle(c, s * 0.55f, fill);
        ci.DrawCircle(c, s * 0.55f, UiTokens.Ink, filled: false);
    }

    private static Vector2[] Closed(Vector2[] pts)
    {
        var closed = new Vector2[pts.Length + 1];
        System.Array.Copy(pts, closed, pts.Length);
        closed[^1] = pts[0];
        return closed;
    }
}

/// <summary>
/// A Control-sized wrapper that draws a single kind glyph inside layout containers
/// (e.g. as an icon preceding entity names in the Registry sidebar).
/// </summary>
public partial class KindGlyphRect : Control
{
    public string KindId { get; set; } = string.Empty;
    public Color Accent { get; set; } = UiTokens.KindDefault;

    public KindGlyphRect()
    {
        CustomMinimumSize = new Vector2(16, 16);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        var size = Mathf.Min(Size.X, Size.Y);
        var center = new Vector2(Size.X * 0.5f, Size.Y * 0.5f);
        KindGlyph.Draw(this, KindId, center, size * 0.5f, Accent);
    }
}
