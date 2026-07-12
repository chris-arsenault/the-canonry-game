#pragma warning disable CA1303
#pragma warning disable CA1305
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Constants;
using Engine.Projection;

using Godot;

using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

public partial class HexTooltipPanel : PanelContainer
{
    private const long TooltipDelayMs = 500;

    private readonly List<Control> _passThroughControls = [];
    private HexMapProjection? _map;
    private UiProjectionState? _projection;
    private IProjectionSymbolCatalog? _symbols;
    private HexMapRenderer? _renderer;
    private Node2D? _worldCanvas;
    private Label? _label;
    private HexCoord? _hoveredHex;
    private Vector2 _hoverScreenPosition;
    private long _hoverStartedAtMs;
    private TooltipTextState? _lastTextState;
    private TooltipPositionState? _lastPositionState;

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

    public bool UpdateTooltip()
    {
        if (_renderer is null || _label is null || _worldCanvas is null)
        {
            return false;
        }

        var hoveredControl = GetViewport().GuiGetHoveredControl();
        if (ShouldSuppressHexTooltip(hoveredControl))
        {
            ClearTooltip();
            return false;
        }

        var screenPos = GetViewport().GetMousePosition();
        var worldPos = _worldCanvas.GetCanvasTransform().AffineInverse() * screenPos;
        var pointedHex = _renderer.GetHexAtWorldPosition(worldPos);
        if (pointedHex is null)
        {
            ClearTooltip();
            return false;
        }

        var now = System.Environment.TickCount64;
        _hoverScreenPosition = screenPos;
        if (_hoveredHex is null || _hoveredHex.Value != pointedHex.Value)
        {
            _hoveredHex = pointedHex.Value;
            _hoverStartedAtMs = now;
            _lastTextState = null;
            _lastPositionState = null;
            Visible = false;
            return true;
        }

        if (!Visible && now - _hoverStartedAtMs < TooltipDelayMs)
        {
            return true;
        }

        PresentTooltipText(pointedHex.Value);
        Visible = true;
        PositionTooltip(_hoverScreenPosition);
        return true;
    }

    public void ClearTooltip()
    {
        _hoveredHex = null;
        _lastTextState = null;
        _lastPositionState = null;
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

        return _renderer?.CurrentPlane == PlaneMode.Semantic
            ? FormatSemanticHexTooltip(coord)
            : FormatPhysicalHexTooltip(coord);
    }

    private void PresentTooltipText(HexCoord coord)
    {
        if (_label is null || _renderer is null)
        {
            return;
        }

        var plane = _renderer.CurrentPlane;
        var textState = new TooltipTextState(
            coord,
            plane,
            _map?.CellVersion ?? -1,
            plane == PlaneMode.Semantic ? _projection?.EntityMapVersion ?? -1 : -1);
        if (_lastTextState == textState)
        {
            return;
        }

        _label.Text = FormatHexTooltip(coord);
        _lastTextState = textState;
        _lastPositionState = null;
    }

    private string FormatSemanticHexTooltip(HexCoord coord)
    {
        var s = -coord.Q - coord.R;
        var occupantsLine = FormatOccupants(PlaneCodes.Semantic, coord);
        if (!_map!.TryGetHex(PlaneCodes.Semantic, coord, out var cell))
        {
            return $"autonomy {coord.Q,3}    resonance {coord.R,3}    tradition {s,3}\n{occupantsLine}";
        }

        var terrain = FormatTerrain(cell);
        var owner = FormatOwner(cell);
        var continent = FormatSubdivision(ProjectionSymbolKind.Continent, cell.ContinentId);
        var superregion = FormatSubdivision(ProjectionSymbolKind.Superregion, cell.SuperregionId);
        return
            $"autonomy {coord.Q,3}    resonance {coord.R,3}    tradition {s,3}\n" +
            $"{terrain,-10}   {owner}   {FormatRoad(cell)}\n" +
            $"{continent}   {superregion}\n" +
            $"{FormatCultureInfluence(cell)}\n" +
            $"{FormatInfluence(cell)}\n" +
            occupantsLine;
    }

    private string FormatPhysicalHexTooltip(HexCoord coord)
    {
        if (!_map!.TryGetHex(PlaneCodes.Physical, coord, out var cell))
        {
            return $"{coord.Q,3} , {coord.R,3}   unknown";
        }

        var terrain = FormatTerrain(cell);
        var owner = FormatOwner(cell);
        var continent = FormatSubdivision(ProjectionSymbolKind.Continent, cell.ContinentId);
        var superregion = FormatSubdivision(ProjectionSymbolKind.Superregion, cell.SuperregionId);
        var occupantsLine = FormatOccupants(PlaneCodes.Physical, coord);
        var forgedLine = FormatForgedColumn(cell);
        return
            $"{coord.Q,3} , {coord.R,3}   {terrain,-10}   {owner}   {FormatSurface(cell)}\n" +
            (forgedLine.Length > 0 ? forgedLine + "\n" : string.Empty) +
            $"{continent}   {superregion}\n" +
            $"{FormatCultureInfluence(cell)}\n" +
            $"{FormatInfluence(cell)}\n" +
            occupantsLine;
    }

    private static string FormatForgedColumn(HexProjection cell)
    {
        if (cell.ForgedSpireTiers <= 0 && cell.ForgedTunnelDepth <= 0)
        {
            return string.Empty;
        }

        var spires = cell.ForgedSpireTiers > 0 ? $"spire tiers \u25b2{cell.ForgedSpireTiers}" : string.Empty;
        var tunnels = cell.ForgedTunnelDepth > 0 ? $"tunnels \u25bc{cell.ForgedTunnelDepth}" : string.Empty;
        return spires.Length > 0 && tunnels.Length > 0 ? $"{spires}   {tunnels}" : spires + tunnels;
    }

    private string FormatTerrain(HexProjection cell) =>
        ProjectionDisplay.DisplayName(
            _symbols,
            ProjectionSymbolKind.Terrain,
            cell.TerrainId,
            $"Terrain {cell.TerrainId}");

    private string FormatSubdivision(ProjectionSymbolKind kind, int id) =>
        ProjectionDisplay.DisplayName(_symbols, kind, id, $"{kind} {id}");

    private string FormatOwner(HexProjection cell)
    {
        if (cell.OwnerEntityId <= 0 ||
            !_projection!.Entities.TryGetValue(cell.OwnerEntityId, out var ownerEntity))
        {
            return "unclaimed";
        }

        return ProjectionDisplay.EntityName(_symbols, ownerEntity);
    }

    private string FormatRoad(HexProjection cell)
    {
        var roadQuality = Mathf.Clamp(cell.RoadQuality / 100f, 0f, 1f);
        return roadQuality switch
        {
            > 0.75f => $"heavy road {roadQuality:0.00}",
            > 0.25f => $"light road {roadQuality:0.00}",
            _ => "no road",
        };
    }

    private string FormatSurface(HexProjection cell)
    {
        var road = FormatRoad(cell);
        if (cell.TravelStructureId <= 0)
        {
            return road;
        }

        var structure = ProjectionDisplay.DisplayName(
            _symbols,
            ProjectionSymbolKind.TravelStructure,
            cell.TravelStructureId,
            $"Structure {cell.TravelStructureId}");
        return $"{structure}; {road}";
    }

    private string FormatInfluence(HexProjection cell)
    {
        var influences = cell.Influences
            .Where(influence => influence.Value > 0f)
            .Take(3)
            .Select(influence => $"{FormatEntityName(influence.EntityId)} {influence.Value:0}")
            .ToArray();
        return influences.Length == 0
            ? "influence none"
            : $"influence {string.Join(" | ", influences)}";
    }

    private string FormatCultureInfluence(HexProjection cell)
    {
        if (cell.CultureInfluences.Count == 0)
        {
            return "culture none";
        }

        var cultures = cell.CultureInfluences
            .Take(3)
            .Select(FormatCultureBoundary)
            .ToArray();
        var dominantCultureId = cell.CultureInfluences[0].CultureId;
        var ownerCultureId = cell.OwnerEntityId > 0 &&
            _projection!.Entities.TryGetValue(cell.OwnerEntityId, out var owner)
                ? owner.CultureId
                : 0;
        var mismatch = ownerCultureId > 0 && ownerCultureId != dominantCultureId
            ? "  control mismatch"
            : string.Empty;
        var runnerUp = FormatRunnerUpCulture(cell);
        return $"culture {string.Join(" | ", cultures)}{runnerUp}{mismatch}";
    }

    private string FormatCultureBoundary(HexCultureInfluenceProjection influence)
    {
        var cultureName = ProjectionDisplay.DisplayName(
            _symbols,
            ProjectionSymbolKind.Culture,
            influence.CultureId,
            $"Culture {influence.CultureId}");
        var kind = CultureCellKindName(influence.CellKindCode);
        var band = CultureStrengthBandName(influence.StrengthBandCode);
        var component = influence.ComponentId > 0
            ? $" c{influence.ComponentId}/{influence.ComponentCellCount}"
            : string.Empty;
        return $"{cultureName} {influence.Share:0}% raw {influence.RawMass:0} {kind}/{band}{component}";
    }

    private string FormatRunnerUpCulture(HexProjection cell)
    {
        if (cell.CultureInfluences.Count <= 1)
        {
            return string.Empty;
        }

        var runnerUp = cell.CultureInfluences[1];
        var cultureName = ProjectionDisplay.DisplayName(
            _symbols,
            ProjectionSymbolKind.Culture,
            runnerUp.CultureId,
            $"Culture {runnerUp.CultureId}");
        return $"  runner-up {cultureName} {runnerUp.Share:0}% raw {runnerUp.RawMass:0}";
    }

    private static string CultureCellKindName(int code) => code switch
    {
        CultureFieldCellKindCodes.Interior => "interior",
        CultureFieldCellKindCodes.Border => "border",
        CultureFieldCellKindCodes.Isolated => "isolated",
        CultureFieldCellKindCodes.Contested => "contested",
        _ => "none",
    };

    private static string CultureStrengthBandName(int code) => code switch
    {
        CultureFieldStrengthBandCodes.Nexus => "nexus",
        CultureFieldStrengthBandCodes.Entrenched => "entrenched",
        CultureFieldStrengthBandCodes.Controlled => "controlled",
        CultureFieldStrengthBandCodes.Claimed => "claimed",
        CultureFieldStrengthBandCodes.Trace => "trace",
        _ => "none",
    };

    private string FormatEntityName(int entityId)
    {
        return _projection!.Entities.TryGetValue(entityId, out var entity)
            ? ProjectionDisplay.EntityName(_symbols, entity)
            : $"Entity {entityId}";
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

    private string FormatOccupants(int planeCode, HexCoord coord)
    {
        var occupants = _projection!.ActiveOccupants(planeCode, coord)
            .Order()
            .Select(entityId => _projection.Entities.TryGetValue(entityId, out var entity) ? entity : null)
            .OfType<UiEntityProjection>()
            .Select(FormatProjectedEntityLine)
            .ToList();
        return occupants.Count == 0 ? "uninhabited" : string.Join('\n', occupants);
    }

    private void PositionTooltip(Vector2 screenPos)
    {
        var viewportSize = GetViewportRect().Size;
        var panelSize = GetCombinedMinimumSize();
        var state = new TooltipPositionState(screenPos, viewportSize, panelSize);
        if (_lastPositionState == state)
        {
            return;
        }

        var pos = screenPos + new Vector2(16f, 18f);
        pos.X = Mathf.Min(pos.X, viewportSize.X - panelSize.X - 8f);
        pos.Y = Mathf.Min(pos.Y, viewportSize.Y - panelSize.Y - 8f);
        pos.X = Mathf.Max(pos.X, 8f);
        pos.Y = Mathf.Max(pos.Y, 8f);
        Position = pos;
        _lastPositionState = state;
    }

    private readonly record struct TooltipTextState(
        HexCoord Coord,
        PlaneMode Plane,
        long CellVersion,
        long EntityMapVersion);

    private readonly record struct TooltipPositionState(
        Vector2 ScreenPosition,
        Vector2 ViewportSize,
        Vector2 PanelSize);
}
