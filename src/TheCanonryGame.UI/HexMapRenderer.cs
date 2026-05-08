#pragma warning disable CA2213
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
/// Scene-backed coordinator for map rendering. Per-cell visuals are repeated
/// HexCell scene instances; aggregate frame/backdrop layers stay separate.
/// </summary>
public partial class HexMapRenderer : Node2D
{
    private readonly ViewSync<int, HexCell> _cells = new();
    private HexMapProjection? _map;
    private IProjectionSymbolCatalog? _symbols;
    private PackedScene? _cellScene;
    private Node2D? _cellLayer;
    private HexMapBackdropLayer? _backdropLayer;
    private HexMapFrameLayer? _frameLayer;

    [Export] public float HexSize { get; set; } = 30f;

    [Export] public bool ShowTerritory { get; set; } = true;

    [Export] public bool ShowInfluence { get; set; } = true;

    [Export] public bool ShowRegionBorders { get; set; } = true;

    public PlaneMode CurrentPlane { get; set; } = PlaneMode.Physical;

    public override void _Ready()
    {
        _cellScene = GD.Load<PackedScene>("res://scenes/ui/HexCell.tscn");
        _backdropLayer = GetNodeOrNull<HexMapBackdropLayer>("BackdropLayer");
        _cellLayer = GetNodeOrNull<Node2D>("CellLayer");
        _frameLayer = GetNodeOrNull<HexMapFrameLayer>("FrameLayer");
    }

    public void Initialize(HexMapProjection map, IProjectionSymbolCatalog? symbols)
    {
        _map = map;
        _symbols = symbols;
        Refresh();
    }

    public void Refresh()
    {
        RefreshCells();
        RefreshFrame();
    }

    public void RefreshCells()
    {
        SyncCellLayer();
    }

    public void RefreshFrame()
    {
        _backdropLayer?.Bind(_map, CurrentPlane, HexSize);
        _frameLayer?.Bind(_map, _symbols, CurrentPlane, HexSize, ShowRegionBorders);
    }

    public Vector2 GetHexCenter(HexCoord coord) => HexToPixel(coord);

    public HexCoord? GetHexAtWorldPosition(Vector2 worldPos)
    {
        if (_map is null || HexSize <= 0f)
        {
            return null;
        }

        var coord = PixelToHex(worldPos);
        return _map.TryGetHex(PlaneCodeForCurrentPlane(), coord, out _)
            ? coord
            : null;
    }

    private void SyncCellLayer()
    {
        if (_map is null || _cellLayer is null)
        {
            _cells.Clear();
            return;
        }

        var ownerColors = AssignOwnerColors();
        var snapshot = OrderedHexes(PlaneCodeForCurrentPlane())
            .Select(cell => KeyValuePair.Create(cell.HexId, BuildCellRenderItem(cell, ownerColors)))
            .ToArray();
        _cells.Sync(_cellLayer, snapshot, CreateCellNode, UpdateCellNode);
    }

    private HexCell CreateCellNode(HexCellRenderItem data)
    {
        _ = data;
        return _cellScene?.Instantiate<HexCell>()
            ?? throw new InvalidOperationException("HexCell scene is not loaded.");
    }

    private void UpdateCellNode(HexCell node, HexCellRenderItem data)
    {
        node.Position = HexToPixel(data.Cell.Coord);
        node.Bind(data);
    }

    private HexCellRenderItem BuildCellRenderItem(
        HexProjection cell,
        Dictionary<int, Color> ownerColors)
    {
        var terrainStableName = ResolveStableName(
            ProjectionSymbolKind.Terrain,
            cell.TerrainId,
            $"Terrain {cell.TerrainId}");
        var ownerColor = cell.OwnerEntityId > 0 && ownerColors.TryGetValue(cell.OwnerEntityId, out var resolvedOwner)
            ? resolvedOwner
            : (Color?)null;
        var influences = BuildInfluenceRenderItems(cell, ownerColors);

        return new HexCellRenderItem(
            cell,
            CurrentPlane,
            HexSize,
            terrainStableName,
            UiTokens.TerrainColor(terrainStableName),
            ownerColor,
            influences,
            ShowTerritory,
            ShowInfluence);
    }

    private static HexInfluenceRenderItem[] BuildInfluenceRenderItems(
        HexProjection cell,
        Dictionary<int, Color> ownerColors)
    {
        var items = new List<HexInfluenceRenderItem>(4);
        foreach (var influence in cell.Influences.Where(influence => influence.Value > 0f))
        {
            if (!ownerColors.TryGetValue(influence.EntityId, out var color))
            {
                continue;
            }

            items.Add(new HexInfluenceRenderItem(influence.EntityId, influence.Value, color));
            if (items.Count == 4)
            {
                break;
            }
        }

        return [.. items];
    }

    private Dictionary<int, Color> AssignOwnerColors()
    {
        var colors = new Dictionary<int, Color>();
        if (_map is null)
        {
            return colors;
        }

        var ownerIds = _map.Hexes(PlaneCodes.Physical)
            .Concat(_map.Hexes(PlaneCodes.Semantic))
            .SelectMany(hex => hex.Influences.Select(influence => influence.EntityId).Append(hex.OwnerEntityId))
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

    private HexProjection[] OrderedHexes(int planeCode) =>
        _map?.Hexes(planeCode).OrderBy(hex => hex.HexId).ToArray() ?? [];

    private int PlaneCodeForCurrentPlane() =>
        CurrentPlane == PlaneMode.Physical ? PlaneCodes.Physical : PlaneCodes.Semantic;

    private HexCoord PixelToHex(Vector2 pixel)
    {
        var q = ((Mathf.Sqrt(3f) / 3f * pixel.X) - (pixel.Y / 3f)) / HexSize;
        var r = (2f / 3f * pixel.Y) / HexSize;
        return RoundAxial(q, r);
    }

    private static HexCoord RoundAxial(float q, float r)
    {
        var s = -q - r;
        var roundedQ = (int)MathF.Round(q, MidpointRounding.AwayFromZero);
        var roundedR = (int)MathF.Round(r, MidpointRounding.AwayFromZero);
        var roundedS = (int)MathF.Round(s, MidpointRounding.AwayFromZero);

        var qDiff = MathF.Abs(roundedQ - q);
        var rDiff = MathF.Abs(roundedR - r);
        var sDiff = MathF.Abs(roundedS - s);

        if (qDiff > rDiff && qDiff > sDiff)
        {
            roundedQ = -roundedR - roundedS;
        }
        else if (rDiff > sDiff)
        {
            roundedR = -roundedQ - roundedS;
        }

        return new HexCoord(roundedQ, roundedR);
    }

    private Vector2 HexToPixel(HexCoord coord)
    {
        var x = HexSize * (Mathf.Sqrt(3f) * coord.Q + Mathf.Sqrt(3f) / 2f * coord.R);
        var y = HexSize * (1.5f * coord.R);
        return new Vector2(x, y);
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
