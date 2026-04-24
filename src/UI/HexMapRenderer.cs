using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

using Engine.Domain.IceRemembers;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Simulation;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>Which plane the renderer is currently drawing.</summary>
public enum PlaneMode
{
    Physical,
    Semantic,
}

/// <summary>
/// Renders the hex grid with terrain colors, entity markers, and territory overlays.
/// Uses Godot's _Draw() for per-cell rendering with full control over colors.
///
/// Supports two plane modes: Physical (map hex grid with terrain/regions/territory)
/// and Semantic (neutral grid over the autonomy × resonance × tradition axes, with
/// entities drawn at their SemanticPosition so you can watch semantic-plane kinds
/// reposition toward their targets).
/// </summary>
public partial class HexMapRenderer : Node2D
{
    private SimulationRunner? _sim;

    /// <summary>Pixel size of each hex cell (outer radius).</summary>
    [Export] public float HexSize { get; set; } = 30f;

    /// <summary>Whether to show territory overlay (faction colors on controlled settlements).</summary>
    [Export] public bool ShowTerritory { get; set; } = true;

    /// <summary>Whether to show floating name labels attached to entity markers.</summary>
    [Export] public bool ShowEntityNames { get; set; } = true;

    /// <summary>Whether to draw a thicker line along hex edges where two regions meet.</summary>
    [Export] public bool ShowRegionBorders { get; set; } = true;

    /// <summary>Which plane is rendered. Physical = map hex grid; Semantic = autonomy/resonance/tradition.</summary>
    public PlaneMode CurrentPlane { get; set; } = PlaneMode.Physical;

    public void Initialize(SimulationRunner sim)
    {
        _sim = sim;
    }

    public override void _Draw()
    {
        if (_sim is null) return;

        if (CurrentPlane == PlaneMode.Physical)
        {
            foreach (var cell in _sim.Grid.AllCells)
            {
                DrawPhysicalHexCell(cell);
            }

            if (ShowTerritory)
            {
                DrawTerritoryOverlay();
            }

            if (ShowRegionBorders)
            {
                DrawRegionBorders();
            }
        }
        else
        {
            DrawSemanticGrid();
        }

        DrawEntityMarkers();
    }

    /// <summary>Call this each frame to refresh the display.</summary>
    public void Refresh()
    {
        QueueRedraw();
    }

    public Vector2 GetHexCenter(HexCoord coord)
    {
        return HexToPixel(coord);
    }

    public HexCoord? GetHexAtWorldPosition(Vector2 worldPos)
    {
        if (_sim is null)
        {
            return null;
        }

        HexCoord? nearest = null;
        var nearestDist = HexSize;
        var coords = CurrentPlane == PlaneMode.Physical
            ? _sim.Grid.AllCells.Select(c => c.Coord)
            : HexCoord.CoordsInRadius(new HexCoord(0, 0), _sim.Grid.Radius);

        foreach (var coord in coords)
        {
            var dist = worldPos.DistanceTo(HexToPixel(coord));
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = coord;
            }
        }

        return nearest;
    }

    /// <summary>Returns the entity's position in the currently rendered plane as a HexCoord.</summary>
    public HexCoord EntityCoordForCurrentPlane(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return CurrentPlane == PlaneMode.Physical
            ? new HexCoord(entity.Coordinates.Q, entity.Coordinates.R)
            : new HexCoord(entity.SemanticPosition.Q, entity.SemanticPosition.R);
    }

    private void DrawPhysicalHexCell(HexCell cell)
    {
        var center = HexToPixel(cell.Coord);
        var color = GetTerrainColor(cell.Terrain);
        var points = GetHexPoints(center);

        // Illustrated terrain tile if the asset loaded; colored polygon
        // otherwise. Godot's DrawColoredPolygon accepts a texture alongside
        // UVs for clip-masking a tile into the hex shape. The texture's
        // hex-masked alpha already matches our hex, so the UVs just scale
        // the texture to fit.
        var tile = AssetLibrary.Terrain(cell.Terrain);
        if (tile is not null)
        {
            // Build UVs that map each hex vertex to the corresponding point
            // on the square texture. The texture is square, centered on the
            // hex, with radius == HexSize.
            var uvs = new Vector2[points.Length];
            for (var i = 0; i < points.Length; i++)
            {
                var local = points[i] - center;
                uvs[i] = new Vector2(
                    0.5f + local.X / (HexSize * 2f),
                    0.5f + local.Y / (HexSize * 2f));
            }
            DrawColoredPolygon(points, Colors.White, uvs, tile);
        }
        else
        {
            DrawColoredPolygon(points, color);
        }
        DrawPolyline([.. points, points[0]], UiTokens.Hairline, 1f);
    }

    private void DrawSemanticGrid()
    {
        if (_sim is null) return;

        // Rose renders first as the background — the Semantic plane IS the
        // rose window. The hex grid overlays on top of it as the coordinate
        // scaffolding for Q/R/S axes.
        DrawSemanticRoseWindow();

        // Grid outlines drawn as a two-pass halo so they read over arbitrary
        // jewel-tone glass: a thick dark underlayer provides the "shadow",
        // a thinner light stroke provides the "line". This same trick keeps
        // axis highlights legible against both bright and dark glass panels.
        var radius = _sim.Grid.Radius;
        var haloColor = UiTokens.Basalt;
        var lineColor = UiTokens.Whisper;
        var axisColor = UiTokens.Ember;

        foreach (var coord in HexCoord.CoordsInRadius(new HexCoord(0, 0), radius))
        {
            var center = HexToPixel(coord);
            var points = GetHexPoints(center);
            var closed = new[] { points[0], points[1], points[2], points[3], points[4], points[5], points[0] };

            var onAxis = coord.Q == 0 || coord.R == 0 || coord.S == 0;

            // Dark halo underneath
            DrawPolyline(closed, haloColor, onAxis ? 4f : 3f);
            // Bright stroke on top
            DrawPolyline(closed, onAxis ? axisColor : lineColor, onAxis ? 2f : 1f);
        }
    }

    /// <summary>
    /// Stained-glass rose window at the Semantic plane's origin. The origin hex
    /// (0,0,0) is split into six triangular wedges, each painted with a pane
    /// whose content maps to one of the six axis poles:
    ///
    ///   wedge 0 (E , verts 0→1): +autonomy   → Breakaway
    ///   wedge 1 (SE, verts 1→2): +resonance  → Witness
    ///   wedge 2 (SW, verts 2→3): -tradition  → Severance
    ///   wedge 3 (W , verts 3→4): -autonomy   → Yoke
    ///   wedge 4 (NW, verts 4→5): -resonance  → Silence
    ///   wedge 5 (NE, verts 5→0): +tradition  → Lineage
    ///
    /// Opposite wedges are the same axis — breakaway ↔ yoke, witness ↔ silence,
    /// lineage ↔ severance — so the symmetry of the axis shows as symmetry in
    /// the window.
    /// </summary>
    private void DrawSemanticRoseWindow()
    {
        if (_sim is null) return;

        var panes = new[]
        {
            AssetLibrary.PaneBreakaway, // wedge 0, E,  +Q
            AssetLibrary.PaneWitness,   // wedge 1, SE, +R
            AssetLibrary.PaneSeverance, // wedge 2, SW, -S
            AssetLibrary.PaneYoke,      // wedge 3, W,  -Q
            AssetLibrary.PaneSilence,   // wedge 4, NW, -R
            AssetLibrary.PaneLineage,   // wedge 5, NE, +S
        };

        // Rose spans the full grid radius and uses FLAT-TOP orientation so
        // the three Q/R/S axes pass through its vertices — each wedge
        // boundary sits on an axis line. Pointy-top would rotate the rose
        // 30° off that alignment.
        //
        // sqrt(3) multiplier: the grid's widest extent from center is
        // roughly HexSize * Grid.Radius * sqrt(3) (pointy-top hex centers
        // stack at √3 stride horizontally), so this puts the rose's
        // outermost vertex at the edge of the visible grid.
        var center = HexToPixel(new HexCoord(0, 0));
        var roseRadius = HexSize * (_sim.Grid.Radius + 1) * Mathf.Sqrt(3f);
        var verts = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.DegToRad(60f * i); // flat-top: vertices at 0°, 60°, ..., 300°
            verts[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * roseRadius;
        }

        // UV mapping per wedge: apex (hex center) → bottom-center of image
        // (0.5, 1), two outer verts → top corners (0, 0) and (1, 0). Pane
        // "up" points outward from the center so each image reads upright
        // looking along its radius.
        var uvs = new Vector2[]
        {
            new(0.5f, 1f),
            new(0f, 0f),
            new(1f, 0f),
        };
        var colors = new[] { Colors.White, Colors.White, Colors.White };

        var drew = 0;
        for (var i = 0; i < 6; i++)
        {
            var pane = panes[i];
            if (pane is null) continue;

            var triangle = new[] { center, verts[i], verts[(i + 1) % 6] };
            DrawPolygon(triangle, colors, uvs, pane);
            drew++;
        }

        if (drew == 0)
        {
            GD.PushWarning("[HexMapRenderer] Rose window drew no panes — pane textures all null. Check assets/game/semantic-plane/*.png.");
        }
    }

    private void DrawTerritoryOverlay()
    {
        if (_sim is null) return;

        var factionColors = AssignFactionColors();

        // Factions manifest through relationships (controlled_by, member_of, ...).
        // Rather than hardcoding a whitelist of relationship kinds, we shade any hex whose
        // resident entity has an outgoing relationship to a Faction-kind target.
        // Dominant-by-strength per hex: if two entities on one hex point at different factions,
        // the stronger relationship wins. Multi-faction disputes belong on the detail card,
        // not the map — shading stays readable at a glance.
        var hexFaction = new Dictionary<HexCoord, (EntityId FactionId, float Strength)>();

        foreach (var (entityId, entity) in _sim.Graph.AllEntities)
        {
            if (entity.Status != EntityStatus.Active) continue;

            foreach (var rel in _sim.Graph.OutgoingRelationships(entityId))
            {
                var target = _sim.Graph.GetEntity(rel.TargetId);
                if (target?.Kind != IceRemembersKinds.Faction) continue;

                var coord = new HexCoord(entity.Coordinates.Q, entity.Coordinates.R);
                if (!hexFaction.TryGetValue(coord, out var current) || rel.Strength > current.Strength)
                {
                    hexFaction[coord] = (rel.TargetId, rel.Strength);
                }
            }
        }

        foreach (var (coord, (factionId, strength)) in hexFaction)
        {
            if (!factionColors.TryGetValue(factionId, out var factionColor)) continue;

            var center = HexToPixel(coord);
            var points = GetHexPoints(center, HexSize * 0.85f);
            var alpha = Mathf.Clamp(0.25f + strength * 0.3f, 0.25f, 0.55f);
            DrawColoredPolygon(points, new Color(factionColor, alpha));
        }
    }

    private void DrawRegionBorders()
    {
        if (_sim is null) return;

        var borderColor = UiTokens.Ink;
        const float borderWidth = 2.5f;

        foreach (var cell in _sim.Grid.AllCells)
        {
            var region = _sim.CoverageMap.GetCoverageForCell(cell.Coord);
            var center = HexToPixel(cell.Coord);
            var points = GetHexPoints(center);

            for (var d = 0; d < 6; d++)
            {
                var neighborCoord = cell.Coord.Neighbor(d);

                // Canonicalize: only draw each shared edge once. For edges against
                // cells outside the grid, always draw (no duplicate partner to handle).
                if (_sim.Grid.ContainsCoord(neighborCoord))
                {
                    if (!IsCanonicalEdgeOwner(cell.Coord, neighborCoord)) continue;
                }

                var neighborRegion = _sim.CoverageMap.GetCoverageForCell(neighborCoord);
                if (ReferenceEquals(region, neighborRegion)) continue;

                // Edge `d` (direction index) sits between hex vertices (6-d)%6 and (7-d)%6.
                // See HexCoord.Directions vs GetHexPoints vertex ordering.
                var edgeIdx = (6 - d) % 6;
                var a = points[edgeIdx];
                var b = points[(edgeIdx + 1) % 6];
                DrawLine(a, b, borderColor, borderWidth, antialiased: true);
            }
        }
    }

    private static bool IsCanonicalEdgeOwner(HexCoord self, HexCoord neighbor)
    {
        if (self.Q != neighbor.Q) return self.Q < neighbor.Q;
        return self.R < neighbor.R;
    }

    private void DrawEntityMarkers()
    {
        if (_sim is null) return;

        var font = UiTheme.BodyFont;

        // When multiple entities occupy the same hex, fan them out radially so all
        // glyphs remain visible instead of stacking on top of each other.
        var offsetsByHex = new Dictionary<HexCoord, int>();

        foreach (var (entityId, entity) in _sim.Graph.AllEntities)
        {
            if (entity.Status != EntityStatus.Active) continue;

            var coord = EntityCoordForCurrentPlane(entity);
            var hexCenter = HexToPixel(coord);

            if (!offsetsByHex.TryGetValue(coord, out var index))
            {
                index = 0;
            }

            var offset = FanOffset(index, HexSize * 0.38f);
            offsetsByHex[coord] = index + 1;

            var center = hexCenter + offset;
            var glyphSize = GlyphSizeForEntity(entity);
            var accent = UiTokens.KindAccent(entity.Kind.Value);

            // Preference order for the map marker:
            //   1. Map icon — flat heraldic stamp, designed for small-scale
            //      readability over variable terrain (correct register).
            //   2. Portrait sprite — the painted illustration, works but
            //      noisy against terrain (this was run 1; kept as fallback).
            //   3. Polygon glyph — procedural shape, used when neither
            //      asset is on disk (fresh clone, pre-generation).
            var mapIcon = AssetLibrary.SubtypeMapIcon(entity.Kind.Value, entity.Subtype);
            var sprite = mapIcon ?? AssetLibrary.KindSprite(entity.Kind.Value);
            if (sprite is not null)
            {
                // Map icons are drawn larger than the polygon glyph so the
                // bold silhouette pops at target zoom without losing
                // definition to pixel minification.
                var drawSize = glyphSize * 2.4f;
                var rect = new Rect2(
                    center.X - drawSize,
                    center.Y - drawSize,
                    drawSize * 2f,
                    drawSize * 2f);
                DrawTextureRect(sprite, rect, tile: false);
            }
            else
            {
                KindGlyph.Draw(this, entity.Kind.Value, center, glyphSize, accent);
            }

            if (ShowEntityNames)
            {
                DrawEntityLabel(font, entity.Name, center, glyphSize);
            }
        }
    }

    private void DrawEntityLabel(Font font, string name, Vector2 center, float glyphSize)
    {
        // Labels need a dark backing or they vanish over light terrain (ice,
        // tundra). Measure the string, draw a filled Basalt pill with a
        // Vellum hairline, then the text on top. Padding keeps the text
        // from pressing against the border.
        const int padX = 5;
        const int padY = 2;
        var textSize = font.GetStringSize(name, HorizontalAlignment.Left, -1, UiTokens.SizeCaption);
        var labelBottom = -glyphSize - 4f;
        var labelPos = center + new Vector2(-textSize.X * 0.5f, labelBottom);

        var bgRect = new Rect2(
            labelPos.X - padX,
            labelPos.Y - textSize.Y + padY,
            textSize.X + padX * 2,
            textSize.Y + padY);
        DrawRect(bgRect, UiTokens.WithAlpha(UiTokens.Basalt, 0.88f));
        DrawRect(bgRect, UiTokens.HairlineStrong, filled: false, width: 1f);
        DrawString(font, labelPos, name, HorizontalAlignment.Left, -1, UiTokens.SizeCaption,
            UiTokens.Vellum);
    }

    private float GlyphSizeForEntity(Entity entity)
    {
        // Settlements scale with population; other kinds stay at a fixed readable size.
        if (entity.Kind == IceRemembersKinds.Settlement)
        {
            var residents = entity.Attributes.GetValue("residents");
            return Mathf.Clamp(8f + residents / 40f, 9f, 18f);
        }

        return 10f;
    }

    private static Vector2 FanOffset(int index, float radius)
    {
        if (index == 0) return Vector2.Zero;
        // Petal positions around the hex center, starting upper-right and rotating clockwise.
        var angle = Mathf.DegToRad(-60f + 60f * (index - 1));
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    // --- Hex math ---

    private Vector2 HexToPixel(HexCoord coord)
    {
        // Pointy-top hex: x = size * (sqrt(3)*q + sqrt(3)/2*r), y = size * (3/2*r)
        var x = HexSize * (Mathf.Sqrt(3f) * coord.Q + Mathf.Sqrt(3f) / 2f * coord.R);
        var y = HexSize * (1.5f * coord.R);
        return new Vector2(x, y);
    }

    private Vector2[] GetHexPoints(Vector2 center, float? size = null)
    {
        var s = size ?? HexSize;
        var points = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.DegToRad(60f * i - 30f); // Pointy-top
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * s;
        }

        return points;
    }

    private static Color GetTerrainColor(TerrainType terrain) => terrain switch
    {
        TerrainType.Wastes     => UiTokens.TerrainWastes,
        TerrainType.Crags      => UiTokens.TerrainCrags,
        TerrainType.Tundra     => UiTokens.TerrainTundra,
        TerrainType.Ice        => UiTokens.TerrainIce,
        TerrainType.HotSprings => UiTokens.TerrainHotSprings,
        TerrainType.Shelf      => UiTokens.TerrainShelf,
        TerrainType.Reef       => UiTokens.TerrainReef,
        TerrainType.Ocean      => UiTokens.TerrainOcean,
        _                      => UiTokens.Whisper,
    };

    private Dictionary<EntityId, Color> AssignFactionColors()
    {
        var colors = new Dictionary<EntityId, Color>();
        if (_sim is null) return colors;

        // Order by EntityId value so each faction gets the same color every run.
        // The underlying kind index is a HashSet — enumeration order is not stable.
        var factionIds = _sim.Graph.GetEntitiesByKind(IceRemembersKinds.Faction)
            .OrderBy(id => id.Value)
            .ToList();

        for (var i = 0; i < factionIds.Count; i++)
        {
            colors[factionIds[i]] = UiTokens.FactionPalette[i % UiTokens.FactionPalette.Length];
        }

        return colors;
    }
}
