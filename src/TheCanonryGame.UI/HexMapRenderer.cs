using System.Collections.Generic;
using System.Linq;

using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

/// <summary>Which plane the renderer is currently drawing.</summary>
public enum PlaneMode
{
    Physical,
    Semantic,
}

/// <summary>
/// Renders the hex grid with terrain colors and territory overlays.
/// Uses Godot's _Draw() for per-cell rendering with full control over colors.
///
/// Supports two plane modes: Physical (map hex grid with terrain/regions/territory)
/// and Semantic (neutral grid over the autonomy / resonance / tradition axes).
/// </summary>
public partial class HexMapRenderer : Node2D
{
    private HexMapProjection? _map;
    private IProjectionSymbolCatalog? _symbols;

    /// <summary>Pixel size of each hex cell (outer radius).</summary>
    [Export] public float HexSize { get; set; } = 30f;

    /// <summary>Whether to show territory overlay (faction colors on controlled settlements).</summary>
    [Export] public bool ShowTerritory { get; set; } = true;

    /// <summary>Whether to draw a thicker line along hex edges where two regions meet.</summary>
    [Export] public bool ShowRegionBorders { get; set; } = true;

    /// <summary>Which plane is rendered. Physical = map hex grid; Semantic = autonomy/resonance/tradition.</summary>
    public PlaneMode CurrentPlane { get; set; } = PlaneMode.Physical;

    public void Initialize(
        HexMapProjection map,
        IProjectionSymbolCatalog? symbols)
    {
        _map = map;
        _symbols = symbols;
    }

    public override void _Draw()
    {
        if (_map is null) return;

        if (CurrentPlane == PlaneMode.Physical)
        {
            foreach (var cell in _map.HexesById.Values.OrderBy(hex => hex.HexId))
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
        if (_map is null)
        {
            return null;
        }

        HexCoord? nearest = null;
        var nearestDist = HexSize;
        var coords = CurrentPlane == PlaneMode.Physical
            ? _map.HexIdsByCoord.Keys
            : HexCoord.CoordsInRadius(new HexCoord(0, 0), _map.MapRadius);

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

    private void DrawPhysicalHexCell(HexProjection cell)
    {
        var center = HexToPixel(cell.Coord);
        var terrainStableName = ResolveStableName(ProjectionSymbolKind.Terrain, cell.TerrainId, $"Terrain {cell.TerrainId}");
        var color = UiTokens.TerrainColor(terrainStableName);
        var points = GetHexPoints(center);

        // Illustrated terrain tile if the asset loaded; colored polygon
        // otherwise. Godot's DrawColoredPolygon accepts a texture alongside
        // UVs for clip-masking a tile into the hex shape. The texture's
        // hex-masked alpha already matches our hex, so the UVs just scale
        // the texture to fit.
        var tile = AssetLibrary.Terrain(terrainStableName);
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
        if (_map is null) return;

        // Rose renders first as the background — the Semantic plane IS the
        // rose window. The hex grid overlays on top of it as the coordinate
        // scaffolding for Q/R/S axes.
        DrawSemanticRoseWindow();

        // Grid outlines drawn as a two-pass halo so they read over arbitrary
        // jewel-tone glass: a thick dark underlayer provides the "shadow",
        // a thinner light stroke provides the "line". This same trick keeps
        // axis highlights legible against both bright and dark glass panels.
        var radius = _map.MapRadius;
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
        if (_map is null) return;

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
        var roseRadius = HexSize * (_map.MapRadius + 1) * Mathf.Sqrt(3f);
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
        if (_map is null) return;

        var ownerColors = AssignOwnerColors();

        foreach (var hex in _map.HexesById.Values)
        {
            if (hex.OwnerEntityId <= 0 || !ownerColors.TryGetValue(hex.OwnerEntityId, out var ownerColor))
            {
                continue;
            }

            var center = HexToPixel(hex.Coord);
            var points = GetHexPoints(center, HexSize * 0.85f);
            DrawColoredPolygon(points, new Color(ownerColor, 0.36f));
        }
    }

    private void DrawRegionBorders()
    {
        if (_map is null) return;

        var borderColor = UiTokens.Ink;
        const float borderWidth = 2.5f;

        foreach (var cell in _map.HexesById.Values)
        {
            var center = HexToPixel(cell.Coord);
            var points = GetHexPoints(center);

            for (var d = 0; d < 6; d++)
            {
                var neighborCoord = cell.Coord.Neighbor(d);

                // Canonicalize: only draw each shared edge once. For edges against
                // cells outside the grid, always draw (no duplicate partner to handle).
                if (_map.TryGetHex(neighborCoord, out var neighbor))
                {
                    if (!IsCanonicalEdgeOwner(cell.Coord, neighborCoord)) continue;
                    if (cell.OwnerEntityId == neighbor.OwnerEntityId) continue;
                }

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

    private Dictionary<int, Color> AssignOwnerColors()
    {
        var colors = new Dictionary<int, Color>();
        if (_map is null) return colors;

        var ownerIds = _map.HexesById.Values
            .Select(hex => hex.OwnerEntityId)
            .Where(ownerId => ownerId > 0)
            .Distinct()
            .Order()
            .ToArray();

        for (var i = 0; i < ownerIds.Length; i++)
        {
            colors[ownerIds[i]] = UiTokens.FactionPalette[i % UiTokens.FactionPalette.Length];
        }

        return colors;
    }

    private string ResolveStableName(ProjectionSymbolKind kind, int id, string fallback)
    {
        if (_symbols?.TryResolve(kind, id, out var symbol) == true)
        {
            return symbol.StableName;
        }

        return fallback;
    }
}
