using Engine.Constants;
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class HexMapFrameLayer : Node2D
{
    private const float LightRoadThreshold = 25f;
    private const float HeavyRoadThreshold = 75f;
    private const float VertexKeyScale = 1000f;
    private const float CultureFillAlphaBase = 0.18f;
    private const float CultureFillAlphaRange = 0.28f;
    private const float CultureStrokeAlphaBase = 0.54f;
    private const float CultureStrokeAlphaRange = 0.24f;
    private const int MinorActorActivityWindowTicks = 32;
    private const int MinorActorActivityGroupLimit = 96;

    private readonly Dictionary<int, Color> _ownerColors = [];
    private HexMapProjection? _map;
    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private PlaneMode _plane;
    private float _hexSize = 30f;
    private bool _showBorders = true;
    private bool _showLabels;
    private bool _showTerritory = true;
    private bool _showCultureInfluence = true;
    private bool _showInfluence = true;
    private bool _showMinorActors;
    private bool _showRoads = true;
    private bool _showRoutes = true;
    private AreaGeometryCache? _territoryAreaCache;
    private AreaGeometryCache? _cultureAreaCache;
    private AreaGeometryCache? _influenceAreaCache;

    public void Bind(
        HexMapProjection? map,
        UiProjectionState? projection,
        IProjectionSymbolCatalog? symbols,
        PlaneMode plane,
        float hexSize,
        bool showBorders,
        bool showLabels,
        bool showTerritory,
        bool showCultureInfluence,
        bool showInfluence,
        bool showMinorActors,
        bool showRoads,
        bool showRoutes,
        IReadOnlyDictionary<int, Color> ownerColors)
    {
        ArgumentNullException.ThrowIfNull(ownerColors);

        if (!ReferenceEquals(_map, map) || !ReferenceEquals(_symbols, symbols))
        {
            ClearAreaGeometryCaches();
        }

        _map = map;
        _projection = projection;
        _symbols = symbols;
        _plane = plane;
        _hexSize = hexSize;
        _showBorders = showBorders;
        _showLabels = showLabels;
        _showTerritory = showTerritory;
        _showCultureInfluence = showCultureInfluence;
        _showInfluence = showInfluence;
        _showMinorActors = showMinorActors;
        _showRoads = showRoads;
        _showRoutes = showRoutes;
        _ownerColors.Clear();
        foreach (var (entityId, color) in ownerColors)
        {
            _ownerColors[entityId] = color;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_map is null)
        {
            return;
        }

        var planeCode = PlaneCode();
        var cells = _map.Hexes(planeCode).ToArray();
        DrawAreaOverlays(cells);

        if (_showRoutes)
        {
            DrawTravelStructureNetwork(cells);
        }

        if (_showRoads)
        {
            DrawRoadNetwork(cells);
        }

        if (_showMinorActors)
        {
            DrawMinorActorActivity(planeCode);
        }

        if (_showBorders)
        {
            DrawMapBorders(planeCode, cells);
        }

        if (_showLabels)
        {
            DrawSubdivisionLabels(cells, semanticMode: _plane == PlaneMode.Semantic);
        }

        if (_plane == PlaneMode.Semantic)
        {
            DrawSemanticGridLines(cells);
        }
    }

    private void DrawAreaOverlays(IReadOnlyCollection<HexProjection> cells)
    {
        if (_showTerritory)
        {
            DrawTerritoryAreas(cells);
        }

        if (_showCultureInfluence)
        {
            DrawCultureAreas(cells);
        }

        if (_showInfluence)
        {
            DrawEntityInfluenceAreas(cells);
        }
    }

    private void DrawTerritoryAreas(IReadOnlyCollection<HexProjection> cells)
    {
        _territoryAreaCache = GetAreaGeometry(
            _territoryAreaCache,
            AreaCacheKey(_map!.FrameVersion, _map.OwnerColorVersion),
            () => BuildTerritoryAreaGeometry(cells));
        DrawAreaOverlay(_territoryAreaCache.Geometry);
    }

    private void DrawCultureAreas(IReadOnlyCollection<HexProjection> cells)
    {
        _cultureAreaCache = GetAreaGeometry(
            _cultureAreaCache,
            AreaCacheKey(_map!.CultureOverlayVersion, ownerColorVersion: 0),
            () => BuildCultureAreaGeometry(cells));
        DrawAreaOverlay(_cultureAreaCache.Geometry);
        DrawContestedCultureMarkers(_cultureAreaCache.Geometry.ContestedMarkers);
    }

    private void DrawEntityInfluenceAreas(IReadOnlyCollection<HexProjection> cells)
    {
        _influenceAreaCache = GetAreaGeometry(
            _influenceAreaCache,
            AreaCacheKey(_map!.InfluenceOverlayVersion, _map.OwnerColorVersion),
            () => BuildEntityInfluenceAreaGeometry(cells));
        DrawAreaOverlay(_influenceAreaCache.Geometry);
    }

    private AreaOverlayGeometry BuildTerritoryAreaGeometry(IReadOnlyCollection<HexProjection> cells)
    {
        var areas = cells
            .Where(cell => cell.OwnerEntityId > 0 && _ownerColors.ContainsKey(cell.OwnerEntityId))
            .Select(cell => new AreaCell(cell.OwnerEntityId, cell, _ownerColors[cell.OwnerEntityId], 100f));
        return new AreaOverlayGeometry(
            BuildAreaPolygons(
                areas,
                fillAlpha: strength => _plane == PlaneMode.Semantic ? 0.10f : 0.13f,
                strokeAlpha: strength => _plane == PlaneMode.Semantic ? 0.30f : 0.36f,
                strokeWidth: 1.2f),
            []);
    }

    private AreaOverlayGeometry BuildCultureAreaGeometry(IReadOnlyCollection<HexProjection> cells)
    {
        var areas = cells
            .Where(cell => cell.CultureInfluences.Count > 0)
            .Select(cell =>
            {
                var dominant = cell.CultureInfluences[0];
                return new AreaCell(
                    dominant.CultureId,
                    cell,
                    CultureColor(dominant.CultureId),
                    CultureOverlayStrength(dominant));
            });
        return new AreaOverlayGeometry(
            BuildAreaPolygons(
                areas,
                fillAlpha: strength => CultureFillAlphaBase + (Mathf.Clamp(strength / 100f, 0f, 1f) * CultureFillAlphaRange),
                strokeAlpha: strength => CultureStrokeAlphaBase + (Mathf.Clamp(strength / 100f, 0f, 1f) * CultureStrokeAlphaRange),
                strokeWidth: 1.6f),
            cells.Where(CellHasCultureContest).Select(BuildContestedCultureMarker).ToArray());
    }

    private AreaOverlayGeometry BuildEntityInfluenceAreaGeometry(IReadOnlyCollection<HexProjection> cells)
    {
        var areas = cells
            .Where(cell =>
                cell.Influences.Count > 0 &&
                cell.Influences[0].Value > 0f &&
                _ownerColors.ContainsKey(cell.Influences[0].EntityId))
            .Select(cell =>
            {
                var influence = cell.Influences[0];
                return new AreaCell(influence.EntityId, cell, _ownerColors[influence.EntityId], influence.Value);
            });
        return new AreaOverlayGeometry(
            BuildAreaPolygons(
                areas,
                fillAlpha: strength => 0.025f + (Mathf.Clamp(strength / 100f, 0f, 1f) * 0.09f),
                strokeAlpha: strength => 0.12f + (Mathf.Clamp(strength / 100f, 0f, 1f) * 0.18f),
                strokeWidth: 0.9f),
            []);
    }

    private void DrawMinorActorActivity(int planeCode)
    {
        if (_projection is null || _map is null)
        {
            return;
        }

        var currentTick = _projection.CurrentTick;
        var groups = _projection.MinorActorWork
            .Where(activity => MinorActorActivityIsVisible(activity, planeCode, currentTick))
            .GroupBy(activity => new MinorActorActivityKey(
                activity.Q,
                activity.R,
                activity.CultureId,
                activity.ActionCode))
            .Select(BuildMinorActorActivityGroup)
            .OrderByDescending(group => group.NewestTick)
            .ThenByDescending(group => group.TotalDelta)
            .Take(MinorActorActivityGroupLimit)
            .OrderBy(group => group.NewestTick);

        foreach (var group in groups)
        {
            if (group.ActionCode == MinorActorActionCodes.RoadMaintenance)
            {
                DrawMinorActorRoadActivity(group, currentTick);
                continue;
            }

            DrawMinorActorCultureActivity(group, currentTick);
        }
    }

    private bool MinorActorActivityIsVisible(UiMinorActorWorkProjection activity, int planeCode, int currentTick)
    {
        if (activity.PlaneCode != planeCode ||
            activity.CultureId <= 0 ||
            currentTick - activity.Tick > MinorActorActivityWindowTicks)
        {
            return false;
        }

        return _map?.TryGetHex(planeCode, new HexCoord(activity.Q, activity.R), out _) == true;
    }

    private static MinorActorActivityGroup BuildMinorActorActivityGroup(
        IGrouping<MinorActorActivityKey, UiMinorActorWorkProjection> group)
    {
        var count = 0;
        var newestTick = 0;
        var totalDelta = 0f;
        foreach (var activity in group)
        {
            count++;
            newestTick = Math.Max(newestTick, activity.Tick);
            totalDelta += activity.Delta;
        }

        return new MinorActorActivityGroup(
            new HexCoord(group.Key.Q, group.Key.R),
            group.Key.CultureId,
            group.Key.ActionCode,
            count,
            newestTick,
            totalDelta);
    }

    private void DrawMinorActorCultureActivity(MinorActorActivityGroup group, int currentTick)
    {
        var alpha = MinorActorActivityAlpha(group, currentTick);
        if (alpha <= 0.05f)
        {
            return;
        }

        var center = HexToPixel(group.Coord);
        var color = CultureColor(group.CultureId);
        var scale = Mathf.Clamp(0.11f + (MathF.Log2(group.Count + 1f) * 0.030f), 0.11f, 0.23f);
        var size = _hexSize * scale;
        var diamond = new[]
        {
            center + new Vector2(0f, -size),
            center + new Vector2(size * 0.72f, 0f),
            center + new Vector2(0f, size),
            center + new Vector2(-size * 0.72f, 0f),
        };

        DrawColoredPolygon(diamond, new Color(color, 0.14f + (alpha * 0.30f)));
        DrawClosedPolyline(diamond, new Color(color, 0.50f + (alpha * 0.38f)), 0.85f + MathF.Min(group.Count, 6) * 0.08f);
    }

    private void DrawMinorActorRoadActivity(MinorActorActivityGroup group, int currentTick)
    {
        var alpha = MinorActorActivityAlpha(group, currentTick);
        if (alpha <= 0.05f)
        {
            return;
        }

        var center = HexToPixel(group.Coord);
        var axis = DashAxis(group.Coord);
        var length = _hexSize * Mathf.Clamp(0.20f + (MathF.Log2(group.Count + 1f) * 0.035f), 0.20f, 0.36f);
        var width = Mathf.Clamp(0.70f + (MathF.Min(group.Count, 7) * 0.08f), 0.70f, 1.35f);
        var start = center - (axis * length);
        var end = center + (axis * length);
        DrawLine(start, end, new Color(UiTokens.Ink, 0.34f + (alpha * 0.24f)), width + 0.95f, antialiased: true);
        DrawLine(start, end, new Color(UiTokens.Gilt, 0.48f + (alpha * 0.36f)), width, antialiased: true);
    }

    private static float MinorActorActivityAlpha(MinorActorActivityGroup group, int currentTick)
    {
        var age = Math.Max(0, currentTick - group.NewestTick);
        var freshness = 1f - Mathf.Clamp(age / (float)MinorActorActivityWindowTicks, 0f, 1f);
        var density = Mathf.Clamp(0.55f + (MathF.Log2(group.Count + 1f) * 0.12f), 0.55f, 0.95f);
        return freshness * density;
    }

    private static AreaGeometryCache GetAreaGeometry(
        AreaGeometryCache? existing,
        AreaGeometryCacheKey key,
        Func<AreaOverlayGeometry> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        return existing is not null && existing.Key == key
            ? existing
            : new AreaGeometryCache(key, build());
    }

    private AreaGeometryCacheKey AreaCacheKey(long layerVersion, long ownerColorVersion) =>
        new(
            PlaneCode(),
            (int)MathF.Round(_hexSize * VertexKeyScale, MidpointRounding.AwayFromZero),
            _map?.FrameVersion ?? -1,
            layerVersion,
            ownerColorVersion);

    private void ClearAreaGeometryCaches()
    {
        _territoryAreaCache = null;
        _cultureAreaCache = null;
        _influenceAreaCache = null;
    }

    private void DrawAreaOverlay(AreaOverlayGeometry geometry)
    {
        foreach (var polygon in geometry.Polygons)
        {
            foreach (var triangle in polygon.FillTriangles)
            {
                DrawColoredPolygon(triangle, polygon.FillColor);
            }

            DrawPolyline(polygon.ClosedBoundary, polygon.StrokeColor, polygon.StrokeWidth, antialiased: true);
        }
    }

    private AreaPolygon[] BuildAreaPolygons(
        IEnumerable<AreaCell> areaCells,
        Func<float, float> fillAlpha,
        Func<float, float> strokeAlpha,
        float strokeWidth)
    {
        var polygons = new List<AreaPolygon>();
        foreach (var group in areaCells.GroupBy(area => area.Key))
        {
            foreach (var component in ConnectedComponents(group.ToArray()))
            {
                if (component.Length == 0)
                {
                    continue;
                }

                var averageStrength = component.Average(area => area.Strength);
                var color = component[0].Color;
                foreach (var loop in BuildAreaLoops(component.Select(area => area.Cell).ToArray()))
                {
                    polygons.Add(new AreaPolygon(
                        BuildFillTriangles(loop),
                        ClosePolygon(loop),
                        new Color(color, fillAlpha(averageStrength)),
                        new Color(color, strokeAlpha(averageStrength)),
                        strokeWidth));
                }
            }
        }

        return [.. polygons];
    }

    private Vector2[][] BuildAreaLoops(IReadOnlyCollection<HexProjection> cells)
    {
        var coords = cells.Select(cell => cell.Coord).ToHashSet();
        var edges = new List<BoundaryEdge>();
        foreach (var cell in cells)
        {
            var points = GetHexPoints(HexToPixel(cell.Coord), _hexSize);
            for (var direction = 0; direction < 6; direction++)
            {
                if (coords.Contains(cell.Coord.Neighbor(direction)))
                {
                    continue;
                }

                var edgeIndex = (6 - direction) % 6;
                var start = points[edgeIndex];
                var end = points[(edgeIndex + 1) % 6];
                edges.Add(new BoundaryEdge(start, end, VertexKey.From(start), VertexKey.From(end)));
            }
        }

        return ChainBoundaryLoops(edges);
    }

    private static Vector2[][] ChainBoundaryLoops(IReadOnlyCollection<BoundaryEdge> sourceEdges)
    {
        var remaining = sourceEdges.ToList();
        var loops = new List<Vector2[]>();
        while (remaining.Count > 0)
        {
            var edge = remaining[0];
            remaining.RemoveAt(0);

            var startKey = edge.StartKey;
            var currentKey = edge.EndKey;
            var loop = new List<Vector2> { edge.Start, edge.End };
            var guard = sourceEdges.Count + 1;
            while (currentKey != startKey && guard-- > 0)
            {
                var nextIndex = remaining.FindIndex(candidate => candidate.StartKey == currentKey);
                BoundaryEdge next;
                if (nextIndex >= 0)
                {
                    next = remaining[nextIndex];
                    remaining.RemoveAt(nextIndex);
                }
                else
                {
                    nextIndex = remaining.FindIndex(candidate => candidate.EndKey == currentKey);
                    if (nextIndex < 0)
                    {
                        break;
                    }

                    next = remaining[nextIndex].Reversed();
                    remaining.RemoveAt(nextIndex);
                }

                loop.Add(next.End);
                currentKey = next.EndKey;
            }

            if (currentKey == startKey && loop.Count >= 4)
            {
                loop.RemoveAt(loop.Count - 1);
                loops.Add([.. loop]);
            }
        }

        return [.. loops];
    }

    private static Vector2[][] BuildFillTriangles(Vector2[] polygon)
    {
        if (polygon.Length < 3)
        {
            return [];
        }

        var triangles = Geometry2D.TriangulatePolygon(polygon);
        if (triangles.Length < 3)
        {
            return [polygon];
        }

        var fillTriangles = new List<Vector2[]>(triangles.Length / 3);
        for (var i = 0; i + 2 < triangles.Length; i += 3)
        {
            fillTriangles.Add([polygon[triangles[i]], polygon[triangles[i + 1]], polygon[triangles[i + 2]]]);
        }

        return [.. fillTriangles];
    }

    private static Vector2[] ClosePolygon(Vector2[] polygon)
    {
        if (polygon.Length == 0)
        {
            return [];
        }

        var closed = new Vector2[polygon.Length + 1];
        Array.Copy(polygon, closed, polygon.Length);
        closed[^1] = polygon[0];
        return closed;
    }

    private void DrawClosedPolyline(Vector2[] polygon, Color color, float width)
    {
        if (polygon.Length < 2)
        {
            return;
        }

        var closed = new Vector2[polygon.Length + 1];
        Array.Copy(polygon, closed, polygon.Length);
        closed[^1] = polygon[0];
        DrawPolyline(closed, color, width, antialiased: true);
    }

    private void DrawRoadNetwork(IReadOnlyCollection<HexProjection> cells)
    {
        var roadCells = cells
            .Where(cell => cell.RoadQuality > LightRoadThreshold)
            .ToDictionary(cell => cell.Coord);
        var connected = new HashSet<HexCoord>();
        foreach (var cell in roadCells.Values.OrderBy(cell => cell.Coord.Q).ThenBy(cell => cell.Coord.R))
        {
            foreach (var neighborCoord in cell.Coord.Neighbors())
            {
                if (!roadCells.TryGetValue(neighborCoord, out var neighbor) ||
                    !IsCanonicalEdgeOwner(cell.Coord, neighborCoord))
                {
                    continue;
                }

                DrawRoadSegment(
                    HexToPixel(cell.Coord),
                    HexToPixel(neighbor.Coord),
                    (cell.RoadQuality + neighbor.RoadQuality) * 0.5f);
                connected.Add(cell.Coord);
                connected.Add(neighbor.Coord);
            }
        }

        foreach (var cell in roadCells.Values.Where(cell => !connected.Contains(cell.Coord)))
        {
            DrawRoadDash(cell);
        }
    }

    private void DrawRoadSegment(Vector2 start, Vector2 end, float quality)
    {
        var heavy = quality > HeavyRoadThreshold;
        var width = heavy ? 1.8f : 1.05f;
        var lineColor = heavy
            ? new Color(UiTokens.Gilt.R, UiTokens.Gilt.G, UiTokens.Gilt.B, 0.88f)
            : new Color(UiTokens.Parchment.R, UiTokens.Parchment.G, UiTokens.Parchment.B, 0.66f);
        var shadow = new Color(UiTokens.Ink.R, UiTokens.Ink.G, UiTokens.Ink.B, 0.58f);
        DrawLine(start, end, shadow, width + 1.35f, antialiased: true);
        DrawLine(start, end, lineColor, width, antialiased: true);
    }

    private void DrawRoadDash(HexProjection cell)
    {
        var center = HexToPixel(cell.Coord);
        var axis = DashAxis(cell.Coord);
        var length = _hexSize * 0.38f;
        DrawRoadSegment(center - (axis * length), center + (axis * length), cell.RoadQuality);
    }

    private void DrawTravelStructureNetwork(IReadOnlyCollection<HexProjection> cells)
    {
        var structureCells = cells
            .Where(cell => cell.TravelStructureId > 0)
            .ToDictionary(cell => cell.Coord);
        var connected = new HashSet<HexCoord>();
        foreach (var cell in structureCells.Values.OrderBy(cell => cell.Coord.Q).ThenBy(cell => cell.Coord.R))
        {
            var stableName = ResolveStableName(
                ProjectionSymbolKind.TravelStructure,
                cell.TravelStructureId,
                $"travel-structure-{cell.TravelStructureId}");
            foreach (var neighborCoord in cell.Coord.Neighbors())
            {
                if (!structureCells.TryGetValue(neighborCoord, out var neighbor) ||
                    neighbor.TravelStructureId != cell.TravelStructureId ||
                    !IsCanonicalEdgeOwner(cell.Coord, neighborCoord))
                {
                    continue;
                }

                DrawStructureSegment(
                    HexToPixel(cell.Coord),
                    HexToPixel(neighbor.Coord),
                    stableName);
                connected.Add(cell.Coord);
                connected.Add(neighbor.Coord);
            }
        }

        foreach (var cell in structureCells.Values.Where(cell => !connected.Contains(cell.Coord)))
        {
            DrawStructureMarker(
                HexToPixel(cell.Coord),
                ResolveStableName(
                    ProjectionSymbolKind.TravelStructure,
                    cell.TravelStructureId,
                    $"travel-structure-{cell.TravelStructureId}"));
        }
    }

    private void DrawStructureSegment(Vector2 start, Vector2 end, string stableName)
    {
        var color = StructureColor(stableName);
        DrawLine(start, end, new Color(UiTokens.Ink.R, UiTokens.Ink.G, UiTokens.Ink.B, 0.62f), 4.1f, antialiased: true);
        DrawLine(start, end, color, 2.4f, antialiased: true);
    }

    private void DrawStructureMarker(Vector2 center, string stableName)
    {
        var color = StructureColor(stableName);
        var size = _hexSize * 0.18f;
        var marker = new[]
        {
            center + new Vector2(0f, -size),
            center + new Vector2(size, 0f),
            center + new Vector2(0f, size),
            center + new Vector2(-size, 0f),
        };
        DrawColoredPolygon(marker, new Color(color, 0.20f));
        DrawClosedPolyline(marker, color, 1.4f);
    }

    private static Vector2 DashAxis(HexCoord coord)
    {
        var bucket = ((coord.Q - coord.R) % 3 + 3) % 3;
        return bucket switch
        {
            0 => Vector2.Right,
            1 => new Vector2(0.5f, 0.8660254f),
            _ => new Vector2(0.5f, -0.8660254f),
        };
    }

    private ContestedCultureMarker BuildContestedCultureMarker(HexProjection cell)
    {
        var center = HexToPixel(cell.Coord);
        return new ContestedCultureMarker(
            center + new Vector2(_hexSize * 0.34f, -_hexSize * 0.34f),
            _hexSize * 0.16f,
            CultureColor(cell.CultureInfluences[0].CultureId),
            cell.CultureInfluences.Count > 1
                ? CultureColor(cell.CultureInfluences[1].CultureId)
                : UiTokens.Ember);
    }

    private void DrawContestedCultureMarkers(IReadOnlyList<ContestedCultureMarker> markers)
    {
        foreach (var marker in markers)
        {
            DrawLine(
                marker.Anchor + new Vector2(-marker.Size, marker.Size),
                marker.Anchor + new Vector2(marker.Size, -marker.Size),
                new Color(UiTokens.Ink.R, UiTokens.Ink.G, UiTokens.Ink.B, 0.74f),
                3.1f,
                antialiased: true);
            DrawLine(
                marker.Anchor + new Vector2(-marker.Size, marker.Size),
                marker.Anchor + new Vector2(marker.Size, -marker.Size),
                new Color(marker.DominantColor, 0.82f),
                1.4f,
                antialiased: true);
            DrawLine(
                marker.Anchor + new Vector2(-marker.Size * 0.45f, marker.Size * 1.45f),
                marker.Anchor + new Vector2(marker.Size * 1.45f, -marker.Size * 0.45f),
                new Color(marker.RunnerUpColor, 0.72f),
                1.1f,
                antialiased: true);
        }
    }

    private static bool CellHasCultureContest(HexProjection cell)
    {
        if (cell.CultureInfluences.Count == 0)
        {
            return false;
        }

        var dominant = cell.CultureInfluences[0];
        return dominant.CellKindCode == CultureFieldCellKindCodes.Contested;
    }

    private static float CultureOverlayStrength(HexCultureInfluenceProjection influence)
    {
        if (influence.RawMass > 0f)
        {
            return Mathf.Clamp(influence.RawMass / 10f, 0f, 100f);
        }

        return influence.Share;
    }

    private void DrawMapBorders(int planeCode, IReadOnlyCollection<HexProjection> cells)
    {
        DrawSubdivisionBorders(
            planeCode,
            cells,
            (cell, neighbor) => cell.ContinentId != neighbor.ContinentId,
            new Color(UiTokens.Aurora.R, UiTokens.Aurora.G, UiTokens.Aurora.B, _plane == PlaneMode.Semantic ? 0.58f : 0.72f),
            _plane == PlaneMode.Semantic ? 3.2f : 3.8f);
        DrawSubdivisionBorders(
            planeCode,
            cells,
            (cell, neighbor) => cell.SuperregionId != neighbor.SuperregionId,
            new Color(UiTokens.Gilt.R, UiTokens.Gilt.G, UiTokens.Gilt.B, _plane == PlaneMode.Semantic ? 0.50f : 0.62f),
            _plane == PlaneMode.Semantic ? 2.2f : 2.6f);
        DrawRegionBorders(planeCode, cells);
    }

    private void DrawSemanticGridLines(IReadOnlyCollection<HexProjection> cells)
    {
        var haloColor = new Color(UiTokens.Basalt.R, UiTokens.Basalt.G, UiTokens.Basalt.B, 0.78f);
        var lineColor = new Color(UiTokens.Whisper.R, UiTokens.Whisper.G, UiTokens.Whisper.B, 0.62f);
        var axisColor = new Color(UiTokens.Ember.R, UiTokens.Ember.G, UiTokens.Ember.B, 0.68f);

        foreach (var coord in SemanticGridCoords(cells))
        {
            var points = GetHexPoints(HexToPixel(coord), _hexSize);
            var closed = new[] { points[0], points[1], points[2], points[3], points[4], points[5], points[0] };
            var onAxis = coord.Q == 0 || coord.R == 0 || coord.S == 0;
            DrawPolyline(closed, haloColor, onAxis ? 3f : 2.4f);
            DrawPolyline(closed, onAxis ? axisColor : lineColor, onAxis ? 1.4f : 0.8f);
        }
    }

    private IEnumerable<HexCoord> SemanticGridCoords(IReadOnlyCollection<HexProjection> cells)
    {
        if (_map is null)
        {
            return [];
        }

        return cells.Count == 0
            ? HexCoord.CoordsInRadius(new HexCoord(0, 0), _map.MapRadius)
            : cells.Select(cell => cell.Coord);
    }

    private void DrawRegionBorders(int planeCode, IReadOnlyCollection<HexProjection> cells)
    {
        foreach (var cell in cells)
        {
            DrawBoundaryEdges(
                cell,
                (HexCoord neighborCoord, out HexProjection neighbor) => _map!.TryGetHex(planeCode, neighborCoord, out neighbor),
                neighbor => cell.OwnerEntityId != neighbor.OwnerEntityId,
                new Color(UiTokens.Ink.R, UiTokens.Ink.G, UiTokens.Ink.B, 0.72f),
                1.7f,
                drawOuterEdges: true);
        }
    }

    private void DrawSubdivisionBorders(
        int planeCode,
        IReadOnlyCollection<HexProjection> cells,
        Func<HexProjection, HexProjection, bool> isBoundary,
        Color borderColor,
        float borderWidth)
    {
        foreach (var cell in cells)
        {
            DrawBoundaryEdges(
                cell,
                (HexCoord neighborCoord, out HexProjection neighbor) => _map!.TryGetHex(planeCode, neighborCoord, out neighbor),
                neighbor => isBoundary(cell, neighbor),
                borderColor,
                borderWidth,
                drawOuterEdges: false);
        }
    }

    private void DrawBoundaryEdges(
        HexProjection cell,
        TryResolveNeighbor tryResolveNeighbor,
        Func<HexProjection, bool> shouldDraw,
        Color borderColor,
        float borderWidth,
        bool drawOuterEdges)
    {
        var points = GetHexPoints(HexToPixel(cell.Coord), _hexSize);
        for (var d = 0; d < 6; d++)
        {
            var neighborCoord = cell.Coord.Neighbor(d);
            if (!tryResolveNeighbor(neighborCoord, out var neighbor))
            {
                if (drawOuterEdges)
                {
                    DrawEdge(points, d, borderColor, borderWidth);
                }

                continue;
            }

            if (IsCanonicalEdgeOwner(cell.Coord, neighborCoord) && shouldDraw(neighbor))
            {
                DrawEdge(points, d, borderColor, borderWidth);
            }
        }
    }

    private void DrawSubdivisionLabels(IReadOnlyCollection<HexProjection> cells, bool semanticMode)
    {
        DrawSubdivisionLabelGroups(
            cells,
            ProjectionSymbolKind.Continent,
            cell => cell.ContinentId,
            fontSize: semanticMode ? 24 : 34,
            minCells: semanticMode ? 14 : 80,
            color: new Color(UiTokens.Aurora.R, UiTokens.Aurora.G, UiTokens.Aurora.B, 0.34f));
        DrawSubdivisionLabelGroups(
            cells,
            ProjectionSymbolKind.Superregion,
            cell => cell.SuperregionId,
            fontSize: semanticMode ? 16 : 20,
            minCells: semanticMode ? 8 : 36,
            color: new Color(UiTokens.Gilt.R, UiTokens.Gilt.G, UiTokens.Gilt.B, 0.58f));
    }

    private void DrawSubdivisionLabelGroups(
        IReadOnlyCollection<HexProjection> cells,
        ProjectionSymbolKind symbolKind,
        Func<HexProjection, int> idSelector,
        int fontSize,
        int minCells,
        Color color)
    {
        foreach (var group in cells.GroupBy(idSelector).Where(group => group.Key > 0))
        {
            var groupCells = group.ToArray();
            if (groupCells.Length < minCells)
            {
                continue;
            }

            var text = ProjectionDisplay.DisplayName(_symbols, symbolKind, group.Key, $"{symbolKind} {group.Key}");
            DrawCenteredMapLabel(AverageHexCenter(groupCells), text, fontSize, color);
        }
    }

    private void DrawCenteredMapLabel(Vector2 center, string text, int fontSize, Color color)
    {
        var width = Mathf.Max(_hexSize * 12f, text.Length * fontSize * 0.75f);
        var origin = center - new Vector2(width / 2f, -fontSize / 2f);
        var shadow = new Color(UiTokens.Basalt.R, UiTokens.Basalt.G, UiTokens.Basalt.B, color.A * 0.75f);
        DrawString(UiTheme.DisplayFont, origin + new Vector2(2f, 2f), text, HorizontalAlignment.Center, width, fontSize, shadow);
        DrawString(UiTheme.DisplayFont, origin, text, HorizontalAlignment.Center, width, fontSize, color);
    }

    private Vector2 AverageHexCenter(IEnumerable<HexProjection> cells)
    {
        var total = Vector2.Zero;
        var count = 0;
        foreach (var cell in cells)
        {
            total += HexToPixel(cell.Coord);
            count++;
        }

        return count == 0 ? Vector2.Zero : total / count;
    }

    private Color CultureColor(int cultureId)
    {
        var cultureStableName = ProjectionDisplay.StableName(
            _symbols,
            ProjectionSymbolKind.Culture,
            cultureId,
            cultureId == 0 ? "none" : $"culture-{cultureId}");
        return UiTokens.CultureColor(cultureStableName);
    }

    private string ResolveStableName(ProjectionSymbolKind kind, int id, string fallback)
    {
        if (_symbols?.TryResolve(kind, id, out var symbol) == true)
        {
            return symbol.StableName;
        }

        return fallback;
    }

    private static Color StructureColor(string? stableName)
    {
        return stableName is not null && stableName.Contains("current", StringComparison.Ordinal)
            ? new Color(UiTokens.Aurora.R, UiTokens.Aurora.G, UiTokens.Aurora.B, 0.86f)
            : new Color(UiTokens.Gilt.R, UiTokens.Gilt.G, UiTokens.Gilt.B, 0.90f);
    }

    private void DrawEdge(Vector2[] points, int direction, Color color, float width)
    {
        var edgeIdx = (6 - direction) % 6;
        DrawLine(points[edgeIdx], points[(edgeIdx + 1) % 6], color, width, antialiased: true);
    }

    private int PlaneCode() =>
        _plane == PlaneMode.Physical ? PlaneCodes.Physical : PlaneCodes.Semantic;

    private Vector2 HexToPixel(HexCoord coord)
    {
        var x = _hexSize * (Mathf.Sqrt(3f) * coord.Q + Mathf.Sqrt(3f) / 2f * coord.R);
        var y = _hexSize * (1.5f * coord.R);
        return new Vector2(x, y);
    }

    private static Vector2[] GetHexPoints(Vector2 center, float size)
    {
        var points = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.DegToRad(60f * i - 30f);
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size;
        }

        return points;
    }

    private static AreaCell[][] ConnectedComponents(IReadOnlyCollection<AreaCell> cells)
    {
        var byCoord = cells.ToDictionary(area => area.Cell.Coord);
        var visited = new HashSet<HexCoord>();
        var components = new List<AreaCell[]>();
        foreach (var start in byCoord.Keys.OrderBy(coord => coord.Q).ThenBy(coord => coord.R))
        {
            if (!visited.Add(start))
            {
                continue;
            }

            var component = new List<AreaCell>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var coord = queue.Dequeue();
                component.Add(byCoord[coord]);
                foreach (var neighbor in coord.Neighbors())
                {
                    if (byCoord.ContainsKey(neighbor) && visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            components.Add([.. component]);
        }

        return [.. components];
    }

    private static bool IsCanonicalEdgeOwner(HexCoord self, HexCoord neighbor)
    {
        if (self.Q != neighbor.Q)
        {
            return self.Q < neighbor.Q;
        }

        return self.R < neighbor.R;
    }

    private readonly record struct AreaCell(int Key, HexProjection Cell, Color Color, float Strength);

    private readonly record struct MinorActorActivityKey(int Q, int R, int CultureId, int ActionCode);

    private readonly record struct MinorActorActivityGroup(
        HexCoord Coord,
        int CultureId,
        int ActionCode,
        int Count,
        int NewestTick,
        float TotalDelta);

    private readonly record struct AreaGeometryCacheKey(
        int PlaneCode,
        int HexSizeKey,
        long FrameVersion,
        long LayerVersion,
        long OwnerColorVersion);

    private sealed record AreaGeometryCache(AreaGeometryCacheKey Key, AreaOverlayGeometry Geometry);

    private sealed record AreaOverlayGeometry(
        AreaPolygon[] Polygons,
        ContestedCultureMarker[] ContestedMarkers);

    private readonly record struct AreaPolygon(
        Vector2[][] FillTriangles,
        Vector2[] ClosedBoundary,
        Color FillColor,
        Color StrokeColor,
        float StrokeWidth);

    private readonly record struct ContestedCultureMarker(
        Vector2 Anchor,
        float Size,
        Color DominantColor,
        Color RunnerUpColor);

    private readonly record struct VertexKey(int X, int Y)
    {
        public static VertexKey From(Vector2 value) =>
            new(
                (int)MathF.Round(value.X * VertexKeyScale, MidpointRounding.AwayFromZero),
                (int)MathF.Round(value.Y * VertexKeyScale, MidpointRounding.AwayFromZero));
    }

    private readonly record struct BoundaryEdge(Vector2 Start, Vector2 End, VertexKey StartKey, VertexKey EndKey)
    {
        public BoundaryEdge Reversed() =>
            this with
            {
                Start = End,
                End = Start,
                StartKey = EndKey,
                EndKey = StartKey,
            };
    }

    private delegate bool TryResolveNeighbor(HexCoord coord, out HexProjection neighbor);
}
