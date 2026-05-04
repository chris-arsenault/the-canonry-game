using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class HexMapFrameLayer : Node2D
{
    private HexMapProjection? _map;
    private IProjectionSymbolCatalog? _symbols;
    private PlaneMode _plane;
    private float _hexSize = 30f;
    private bool _showRegionBorders = true;

    public void Bind(
        HexMapProjection? map,
        IProjectionSymbolCatalog? symbols,
        PlaneMode plane,
        float hexSize,
        bool showRegionBorders)
    {
        _map = map;
        _symbols = symbols;
        _plane = plane;
        _hexSize = hexSize;
        _showRegionBorders = showRegionBorders;
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
        if (_showRegionBorders)
        {
            DrawMapBorders(planeCode, cells);
            DrawSubdivisionLabels(cells, semanticMode: _plane == PlaneMode.Semantic);
        }

        if (_plane == PlaneMode.Semantic)
        {
            DrawSemanticGridLines(cells);
        }
    }

    private void DrawMapBorders(int planeCode, IReadOnlyCollection<HexProjection> cells)
    {
        DrawSubdivisionBorders(
            planeCode,
            cells,
            (cell, neighbor) => cell.ContinentId != neighbor.ContinentId,
            new Color(UiTokens.Aurora.R, UiTokens.Aurora.G, UiTokens.Aurora.B, _plane == PlaneMode.Semantic ? 0.68f : 0.85f),
            _plane == PlaneMode.Semantic ? 4f : 4.5f);
        DrawSubdivisionBorders(
            planeCode,
            cells,
            (cell, neighbor) => cell.SuperregionId != neighbor.SuperregionId,
            new Color(UiTokens.Gilt.R, UiTokens.Gilt.G, UiTokens.Gilt.B, _plane == PlaneMode.Semantic ? 0.62f : 0.7f),
            _plane == PlaneMode.Semantic ? 2.8f : 3.2f);
        DrawRegionBorders(planeCode, cells);
    }

    private void DrawSemanticGridLines(IReadOnlyCollection<HexProjection> cells)
    {
        var haloColor = UiTokens.Basalt;
        var lineColor = UiTokens.Whisper;
        var axisColor = UiTokens.Ember;

        foreach (var coord in SemanticGridCoords(cells))
        {
            var points = GetHexPoints(HexToPixel(coord), _hexSize);
            var closed = new[] { points[0], points[1], points[2], points[3], points[4], points[5], points[0] };
            var onAxis = coord.Q == 0 || coord.R == 0 || coord.S == 0;
            DrawPolyline(closed, haloColor, onAxis ? 4f : 3f);
            DrawPolyline(closed, onAxis ? axisColor : lineColor, onAxis ? 2f : 1f);
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
                UiTokens.Ink,
                2.5f,
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

    private static bool IsCanonicalEdgeOwner(HexCoord self, HexCoord neighbor)
    {
        if (self.Q != neighbor.Q)
        {
            return self.Q < neighbor.Q;
        }

        return self.R < neighbor.R;
    }

    private delegate bool TryResolveNeighbor(HexCoord coord, out HexProjection neighbor);
}
