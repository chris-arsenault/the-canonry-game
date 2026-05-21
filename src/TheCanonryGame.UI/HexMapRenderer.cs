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
    private readonly Dictionary<int, HexCell> _cellNodes = [];
    private readonly Dictionary<int, Vector2> _cellPositions = [];
    private readonly Dictionary<int, Color> _ownerColors = [];
    private HexMapProjection? _map;
    private UiProjectionState? _ui;
    private IProjectionSymbolCatalog? _symbols;
    private PackedScene? _cellScene;
    private Node2D? _cellLayer;
    private HexMapBackdropLayer? _backdropLayer;
    private HexMapFrameLayer? _frameLayer;
    private long _ownerColorVersion = -1;

    [Export] public float HexSize { get; set; } = 30f;

    [Export] public bool ShowTerritory { get; set; }

    [Export] public bool ShowCultureInfluence { get; set; }

    [Export] public bool ShowInfluence { get; set; }

    [Export] public bool ShowMinorActors { get; set; }

    [Export] public bool ShowRoads { get; set; } = true;

    [Export] public bool ShowRoutes { get; set; } = true;

    [Export] public bool ShowBorders { get; set; } = true;

    [Export] public bool ShowLabels { get; set; }

    public PlaneMode CurrentPlane { get; set; } = PlaneMode.Physical;

    public int CurrentPlaneCode => PlaneCodeForCurrentPlane();

    public override void _Ready()
    {
        _cellScene = GD.Load<PackedScene>("res://scenes/ui/HexCell.tscn");
        _backdropLayer = GetNodeOrNull<HexMapBackdropLayer>("BackdropLayer");
        _cellLayer = GetNodeOrNull<Node2D>("CellLayer");
        _frameLayer = GetNodeOrNull<HexMapFrameLayer>("FrameLayer");
    }

    public void Initialize(HexMapProjection map, UiProjectionState? ui, IProjectionSymbolCatalog? symbols)
    {
        _map = map;
        _ui = ui;
        _symbols = symbols;
        _ownerColors.Clear();
        _ownerColorVersion = -1;
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

    public void RefreshDirtyCells(IReadOnlyCollection<int> dirtyHexIds)
    {
        ArgumentNullException.ThrowIfNull(dirtyHexIds);

        if (_map is null || _cellLayer is null)
        {
            ClearCellLayer();
            return;
        }

        if (dirtyHexIds.Count == 0)
        {
            return;
        }

        var planeCode = PlaneCodeForCurrentPlane();
        foreach (var hexId in dirtyHexIds)
        {
            if (_map.TryGetHex(planeCode, hexId, out var cell))
            {
                UpdateCell(cell);
                continue;
            }

            RemoveCell(hexId);
        }
    }

    public void RefreshFrame()
    {
        _backdropLayer?.Bind(_map, CurrentPlane, HexSize);
        _frameLayer?.Bind(
            _map,
            _ui,
            _symbols,
            CurrentPlane,
            HexSize,
            ShowBorders,
            ShowLabels,
            ShowTerritory,
            ShowCultureInfluence,
            ShowInfluence,
            ShowMinorActors,
            ShowRoads,
            ShowRoutes,
            ResolveOwnerColors());
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
            ClearCellLayer();
            return;
        }

        var currentIds = new HashSet<int>();
        var index = 0;
        foreach (var cell in OrderedHexes(PlaneCodeForCurrentPlane()))
        {
            currentIds.Add(cell.HexId);
            var node = UpdateCell(cell);
            if (node.GetParent() == _cellLayer && _cellLayer.GetChild(index) != node)
            {
                _cellLayer.MoveChild(node, index);
            }

            index++;
        }

        foreach (var hexId in _cellNodes.Keys.Where(hexId => !currentIds.Contains(hexId)).ToArray())
        {
            RemoveCell(hexId);
        }
    }

    private HexCell UpdateCell(HexProjection cell)
    {
        if (!_cellNodes.TryGetValue(cell.HexId, out var node))
        {
            node = _cellScene?.Instantiate<HexCell>()
                ?? throw new InvalidOperationException("HexCell scene is not loaded.");
            _cellLayer!.AddChild(node);
            _cellNodes[cell.HexId] = node;
        }

        var position = HexToPixel(cell.Coord);
        if (!_cellPositions.TryGetValue(cell.HexId, out var previousPosition) || previousPosition != position)
        {
            node.Position = position;
            _cellPositions[cell.HexId] = position;
        }

        node.Bind(BuildCellRenderItem(cell));
        return node;
    }

    private void RemoveCell(int hexId)
    {
        if (!_cellNodes.TryGetValue(hexId, out var node))
        {
            return;
        }

        _cellNodes.Remove(hexId);
        _cellPositions.Remove(hexId);
        node.QueueFree();
    }

    private void ClearCellLayer()
    {
        foreach (var node in _cellNodes.Values)
        {
            node.QueueFree();
        }

        _cellNodes.Clear();
        _cellPositions.Clear();
    }

    private HexCellRenderItem BuildCellRenderItem(HexProjection cell)
    {
        var terrainStableName = ResolveStableName(
            ProjectionSymbolKind.Terrain,
            cell.TerrainId,
            $"Terrain {cell.TerrainId}");

        return new HexCellRenderItem(
            cell,
            CurrentPlane,
            HexSize,
            terrainStableName,
            UiTokens.TerrainColor(terrainStableName));
    }

    private Dictionary<int, Color> ResolveOwnerColors()
    {
        if (_map is null)
        {
            return _ownerColors;
        }

        if (_ownerColorVersion == _map.OwnerColorVersion)
        {
            return _ownerColors;
        }

        foreach (var ownerId in _map.OwnerColorEntityIds)
        {
            if (_ownerColors.ContainsKey(ownerId))
            {
                continue;
            }

            _ownerColors[ownerId] = UiTokens.FactionPalette[_ownerColors.Count % UiTokens.FactionPalette.Length];
        }

        _ownerColorVersion = _map.OwnerColorVersion;
        return _ownerColors;
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
