using System.Collections.Generic;
using System.Linq;

using Godot;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Simulation;

namespace TheCanonryGame.UI;

/// <summary>
/// Renders the hex grid with terrain colors, entity markers, and territory overlays.
/// Uses Godot's _Draw() for per-cell rendering with full control over colors.
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

    public void Initialize(SimulationRunner sim)
    {
        _sim = sim;
    }

    public override void _Draw()
    {
        if (_sim is null) return;

        foreach (var cell in _sim.Grid.AllCells)
        {
            DrawHexCell(cell);
        }

        if (ShowTerritory)
        {
            DrawTerritoryOverlay();
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

        foreach (var cell in _sim.Grid.AllCells)
        {
            var dist = worldPos.DistanceTo(HexToPixel(cell.Coord));
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = cell.Coord;
            }
        }

        return nearest;
    }

    private void DrawHexCell(HexCell cell)
    {
        var center = HexToPixel(cell.Coord);
        var color = GetTerrainColor(cell.Terrain);
        var points = GetHexPoints(center);

        DrawColoredPolygon(points, color);
        DrawPolyline([.. points, points[0]], new Color(0.2f, 0.2f, 0.2f, 0.3f), 1f);
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
            if (entity.Coordinates is null || entity.Status != EntityStatus.Active) continue;

            foreach (var rel in _sim.Graph.OutgoingRelationships(entityId))
            {
                var target = _sim.Graph.GetEntity(rel.TargetId);
                if (target?.Kind != EntityKind.Faction) continue;

                var coord = new HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R);
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

    private void DrawEntityMarkers()
    {
        if (_sim is null) return;

        var font = ThemeDB.FallbackFont;

        foreach (var (entityId, entity) in _sim.Graph.AllEntities)
        {
            if (entity.Coordinates is null || entity.Status != EntityStatus.Active) continue;

            var coord = new HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R);
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
