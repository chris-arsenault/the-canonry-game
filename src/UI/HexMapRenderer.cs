using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Simulation;

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

        DrawColoredPolygon(points, color);
        DrawPolyline([.. points, points[0]], new Color(0.2f, 0.2f, 0.2f, 0.3f), 1f);
    }

    private void DrawSemanticGrid()
    {
        if (_sim is null) return;

        // Semantic hex grid matches the physical grid's radius so the view has the
        // same footprint. Cells are purely visual scaffolding on the
        // autonomy (Q) × resonance (R) × tradition (S) plane — no terrain, no regions.
        var radius = _sim.Grid.Radius;
        var fill = new Color(0.18f, 0.19f, 0.22f);
        var outline = new Color(0.35f, 0.37f, 0.42f, 0.6f);
        var axisHighlight = new Color(0.55f, 0.55f, 0.60f, 0.85f);

        foreach (var coord in HexCoord.CoordsInRadius(new HexCoord(0, 0), radius))
        {
            var center = HexToPixel(coord);
            var points = GetHexPoints(center);

            DrawColoredPolygon(points, fill);

            // Highlight the three axes (Q=0, R=0, S=0) so autonomy/resonance/tradition
            // are readable at a glance. Origin gets all three.
            var onAxis = coord.Q == 0 || coord.R == 0 || coord.S == 0;
            DrawPolyline([.. points, points[0]], onAxis ? axisHighlight : outline, onAxis ? 1.5f : 1f);
        }
    }

    private void DrawTerritoryOverlay()
    {
        if (_sim is null) return;

        var factionColors = AssignFactionColors();

        // Factions manifest through relationships (controlled_by, loyal_to, member_of, ...).
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
                if (target?.Kind != EntityKind.Faction) continue;

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

        var borderColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        const float borderWidth = 2.5f;

        foreach (var cell in _sim.Grid.AllCells)
        {
            var region = _sim.RegionMap.GetRegionForCell(cell.Coord);
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

                var neighborRegion = _sim.RegionMap.GetRegionForCell(neighborCoord);
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

        var font = ThemeDB.FallbackFont;

        foreach (var (entityId, entity) in _sim.Graph.AllEntities)
        {
            if (entity.Status != EntityStatus.Active) continue;

            var coord = EntityCoordForCurrentPlane(entity);
            var center = HexToPixel(coord);

            switch (entity.Kind)
            {
                case EntityKind.Settlement:
                    var residents = entity.Attributes.GetValue("residents");
                    var radius = Mathf.Clamp(residents / 100f * 3f, 3f, 12f);
                    DrawCircle(center, radius, new Color(0.9f, 0.85f, 0.3f));
                    DrawCircle(center, radius, new Color(0.1f, 0.1f, 0.1f), filled: false);
                    break;

                case EntityKind.Npc:
                    DrawCircle(center + new Vector2(5, -5), 4f, new Color(0.3f, 0.8f, 0.3f));
                    break;

                case EntityKind.Artifact:
                    DrawRect(new Rect2(center.X - 3 + 8, center.Y - 3 + 5, 6, 6),
                        new Color(0.8f, 0.2f, 0.8f));
                    break;
            }

            if (ShowEntityNames && font is not null)
            {
                var labelPos = center + new Vector2(-entity.Name.Length * 2.5f, -HexSize * 0.6f);
                DrawString(font, labelPos, entity.Name, HorizontalAlignment.Left, -1, 10,
                    new Color(1f, 1f, 1f, 0.9f));
            }
        }
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
        TerrainType.Plains => new Color(0.45f, 0.7f, 0.3f),
        TerrainType.Mountain => new Color(0.55f, 0.5f, 0.45f),
        TerrainType.Forest => new Color(0.2f, 0.5f, 0.2f),
        TerrainType.Desert => new Color(0.85f, 0.75f, 0.5f),
        TerrainType.Water => new Color(0.2f, 0.35f, 0.7f),
        TerrainType.Coastal => new Color(0.5f, 0.65f, 0.75f),
        TerrainType.Swamp => new Color(0.35f, 0.45f, 0.3f),
        TerrainType.Tundra => new Color(0.75f, 0.8f, 0.85f),
        _ => new Color(0.5f, 0.5f, 0.5f),
    };

    private Dictionary<EntityId, Color> AssignFactionColors()
    {
        var colors = new Dictionary<EntityId, Color>();
        if (_sim is null) return colors;

        var palette = new[]
        {
            new Color(0.8f, 0.2f, 0.2f),
            new Color(0.2f, 0.4f, 0.8f),
            new Color(0.2f, 0.7f, 0.3f),
            new Color(0.7f, 0.5f, 0.1f),
            new Color(0.6f, 0.2f, 0.6f),
        };

        // Order by EntityId value so each faction gets the same color every run.
        // The underlying kind index is a HashSet — enumeration order is not stable.
        var factionIds = _sim.Graph.GetEntitiesByKind(EntityKind.Faction)
            .OrderBy(id => id.Value)
            .ToList();

        for (var i = 0; i < factionIds.Count; i++)
        {
            colors[factionIds[i]] = palette[i % palette.Length];
        }

        return colors;
    }
}
