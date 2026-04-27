#pragma warning disable CA1303
#pragma warning disable CA1305
#pragma warning disable CA2000
#pragma warning disable CA2213
using Godot;

using Engine.Projection;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class HexTooltipPanel : PanelContainer
{
    private const ulong TooltipDelayMs = 500;

    private readonly List<Control> _passThroughControls = [];
    private HexMapProjection? _map;
    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private HexMapRenderer? _renderer;
    private Node2D? _worldCanvas;
    private Label? _label;
    private HexCoord? _hoveredHex;
    private Vector2 _hoverScreenPosition;
    private ulong _hoverStartedAtMs;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeStyleboxOverride("panel", UiTheme.OverlayPanelStyle());

        _label = GetNode<Label>("TooltipLabel");
        _label.MouseFilter = MouseFilterEnum.Ignore;
        _label.AutowrapMode = TextServer.AutowrapMode.Off;
        _label.AddThemeFontOverride("font", UiTheme.MonoFont);
        _label.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _label.AddThemeColorOverride("font_color", UiTokens.Vellum);
    }

    public void Initialize(
        HexMapProjection? map,
        UiProjectionState? projection,
        IProjectionSymbolCatalog? symbols,
        HexMapRenderer? renderer,
        Node2D worldCanvas,
        IEnumerable<Control?> passThroughControls)
    {
        ArgumentNullException.ThrowIfNull(worldCanvas);
        ArgumentNullException.ThrowIfNull(passThroughControls);

        _map = map;
        _projection = projection;
        _symbols = symbols;
        _renderer = renderer;
        _worldCanvas = worldCanvas;
        _passThroughControls.Clear();
        _passThroughControls.AddRange(passThroughControls.OfType<Control>());
        ClearTooltip();
    }

    public void UpdateTooltip()
    {
        if (_renderer is null || _label is null || _worldCanvas is null)
        {
            return;
        }

        var hoveredControl = GetViewport().GuiGetHoveredControl();
        if (ShouldSuppressHexTooltip(hoveredControl))
        {
            ClearTooltip();
            return;
        }

        var screenPos = GetViewport().GetMousePosition();
        var worldPos = _worldCanvas.GetCanvasTransform().AffineInverse() * screenPos;
        var pointedHex = _renderer.GetHexAtWorldPosition(worldPos);
        if (pointedHex is null)
        {
            ClearTooltip();
            return;
        }

        _hoverScreenPosition = screenPos;
        if (_hoveredHex is null || _hoveredHex.Value != pointedHex.Value)
        {
            _hoveredHex = pointedHex.Value;
            _hoverStartedAtMs = Time.GetTicksMsec();
            Visible = false;
            return;
        }

        if (!Visible && Time.GetTicksMsec() - _hoverStartedAtMs < TooltipDelayMs)
        {
            return;
        }

        _label.Text = FormatHexTooltip(pointedHex.Value);
        Visible = true;
        PositionTooltip(_hoverScreenPosition);
    }

    public void ClearTooltip()
    {
        _hoveredHex = null;
        Visible = false;
    }

    private bool ShouldSuppressHexTooltip(Control? hoveredControl)
    {
        if (hoveredControl is null)
        {
            return false;
        }

        return !_passThroughControls.Contains(hoveredControl);
    }

    private string FormatHexTooltip(HexCoord coord)
    {
        if (_map is null || _projection is null)
        {
            return string.Empty;
        }

        if (_renderer?.CurrentPlane == PlaneMode.Semantic)
        {
            var s = -coord.Q - coord.R;
            var occupants = _projection.Entities.Values
                .Where(entity => entity.StatusCode == EntityStatusCodes.Active &&
                                 entity.SemanticQ == coord.Q &&
                                 entity.SemanticR == coord.R)
                .Select(FormatProjectedEntityLine)
                .ToList();
            var occupantsLine = occupants.Count == 0 ? "uninhabited" : string.Join("\n", occupants);
            return $"autonomy {coord.Q,3}    resonance {coord.R,3}    tradition {s,3}\n{occupantsLine}";
        }

        if (!_map.TryGetHex(coord, out var cell))
        {
            return $"{coord.Q,3} , {coord.R,3}   unknown";
        }

        var terrain = ProjectionDisplay.DisplayName(
            _symbols,
            ProjectionSymbolKind.Terrain,
            cell.TerrainId,
            $"Terrain {cell.TerrainId}");
        var owner = cell.OwnerEntityId > 0 &&
                    _projection.Entities.TryGetValue(cell.OwnerEntityId, out var ownerEntity)
            ? ProjectionDisplay.EntityName(_symbols, ownerEntity)
            : "unclaimed";
        var resources = _projection.Resources.Values
            .Where(resource => resource.Q == coord.Q && resource.R == coord.R)
            .OrderBy(resource => ProjectionDisplay.DisplayName(
                _symbols,
                ProjectionSymbolKind.Resource,
                resource.ResourceId,
                $"Resource {resource.ResourceId}"))
            .Select(resource =>
            {
                var name = ProjectionDisplay.DisplayName(
                    _symbols,
                    ProjectionSymbolKind.Resource,
                    resource.ResourceId,
                    $"Resource {resource.ResourceId}");
                return $"{name,-10} {resource.NewValue,7:0.0}";
            })
            .ToArray();
        var resourceLines = resources.Length == 0
            ? "resources  none"
            : string.Join('\n', resources);
        return $"{coord.Q,3} , {coord.R,3}   {terrain,-10}   {owner}\n{resourceLines}";
    }

    private string FormatProjectedEntityLine(UiEntityProjection entity)
    {
        var name = ProjectionDisplay.EntityName(_symbols, entity);
        var kind = ProjectionDisplay.DisplayName(
            _symbols,
            ProjectionSymbolKind.Kind,
            entity.KindId,
            $"Kind {entity.KindId}");
        return $"{name}  {kind}";
    }

    private void PositionTooltip(Vector2 screenPos)
    {
        var viewportSize = GetViewportRect().Size;
        var panelSize = GetCombinedMinimumSize();
        var pos = screenPos + new Vector2(16f, 18f);
        pos.X = Mathf.Min(pos.X, viewportSize.X - panelSize.X - 8f);
        pos.Y = Mathf.Min(pos.Y, viewportSize.Y - panelSize.Y - 8f);
        pos.X = Mathf.Max(pos.X, 8f);
        pos.Y = Mathf.Max(pos.Y, 8f);
        Position = pos;
    }
}
