#pragma warning disable CA1303
#pragma warning disable CA1001 // Godot nodes are managed by the scene tree
#pragma warning disable CA2000
#pragma warning disable CA2213
using Engine.Projection;

using Godot;

using TheCanonryGame.Game;
using TheCanonryGame.Game.Runtime;
using TheCanonryGame.UI;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.App;

internal sealed class GameRenderTree
{
    private const long TooltipRefreshIntervalMs = 100;

    private readonly HexMapRenderer _mapRenderer;
    private readonly EntityLayer _entityLayer;
    private readonly EntityBrowser _browser;
    private readonly TopBar _topBar;
    private readonly BottomBar _bottomBar;
    private readonly ChroniclePanel _chroniclePanel;
    private readonly MapArea _mapArea;
    private readonly VBoxContainer _rootColumn;
    private readonly EntityCardLayer _cardLayer;
    private readonly Control _uiRoot;
    private readonly TableauPanel _tableauPanel;
    private readonly HexTooltipPanel _hexTooltipPanel;
    private readonly Node2D _worldCanvas;
    private readonly Godot.Theme? _theme;
    private readonly TopBarRender _topBarRender = new();
    private readonly MapRender _mapRender = new();
    private readonly EntityMapRender _entityMapRender = new();
    private readonly EntityBrowserRender _browserRender = new();
    private readonly ChronicleRender _chronicleRender = new();
    private readonly CardLayerRender _cardLayerRender = new();
    private readonly AttributeGraphRender _attributeGraphRender = new();
    private readonly TableauRender _tableauRender = new();
    private readonly FloatingHudLayoutRender _floatingHudLayoutRender = new();
    private CanonryGameRuntime? _runtime;
    private HexMapProjection? _map;
    private UiProjectionState? _ui;
    private IProjectionSymbolCatalog? _symbols;
    private IGameChronicleFeed? _chronicle;
    private AttributeGraphPanel? _attributeGraph;
    private HexMapProjection? _lastCultureOverviewMap;
    private PlaneMode? _lastCultureOverviewPlane;
    private long _lastCultureOverviewActivityVersion = -1;
    private bool _tableauVisible;
    private int _seed;
    private int _seedAttempt;
    private int _lastCultureOverviewTick = -1;
    private bool _tooltipPollingEnabled;
    private bool _entityNamesVisibleForZoom;
    private double _tooltipRefreshElapsedMs = TooltipRefreshIntervalMs;
    private string _mapLensKey = MapArea.LensDefault;

    internal GameRenderTree(
        HexMapRenderer mapRenderer,
        EntityLayer entityLayer,
        EntityBrowser browser,
        TopBar topBar,
        BottomBar bottomBar,
        ChroniclePanel chroniclePanel,
        MapArea mapArea,
        VBoxContainer rootColumn,
        EntityCardLayer cardLayer,
        Control uiRoot,
        TableauPanel tableauPanel,
        HexTooltipPanel hexTooltipPanel,
        Node2D worldCanvas,
        Vector2 cameraZoom,
        Godot.Theme? theme)
    {
        _mapRenderer = mapRenderer;
        _entityLayer = entityLayer;
        _browser = browser;
        _topBar = topBar;
        _bottomBar = bottomBar;
        _chroniclePanel = chroniclePanel;
        _mapArea = mapArea;
        _rootColumn = rootColumn;
        _cardLayer = cardLayer;
        _uiRoot = uiRoot;
        _tableauPanel = tableauPanel;
        _hexTooltipPanel = hexTooltipPanel;
        _worldCanvas = worldCanvas;
        _theme = theme;
        SetCameraZoom(cameraZoom);
        _mapArea.Resized += _floatingHudLayoutRender.Invalidate;
        _tableauPanel.VisibilityChanged += _floatingHudLayoutRender.Invalidate;
        _tableauPanel.InterventionArmRequested += ArmIntervention;
    }

    private int _armedInterventionId;

    public void ArmIntervention(int interventionId)
    {
        _armedInterventionId = interventionId;
    }

    public bool TryApplyArmedInterventionClick()
    {
        if (_armedInterventionId <= 0 || _runtime is null || _map is null || _ui is null)
        {
            return false;
        }

        var viewport = _worldCanvas.GetViewport();
        if (viewport.GuiGetHoveredControl() is not null)
        {
            return false;
        }

        var screenPos = viewport.GetMousePosition();
        var worldPos = _worldCanvas.GetCanvasTransform().AffineInverse() * screenPos;
        var pointedHex = _mapRenderer.GetHexAtWorldPosition(worldPos);
        if (pointedHex is null)
        {
            return false;
        }

        var planeCode = _mapRenderer.CurrentPlane == PlaneMode.Physical
            ? PlaneCodes.Physical
            : PlaneCodes.Semantic;
        if (!ApplyIntervention(_armedInterventionId, planeCode, pointedHex.Value))
        {
            return true;
        }

        _runtime.Tableau.NotifyInterventionUsed(_armedInterventionId);
        _armedInterventionId = 0;
        _tableauRender.Invalidate();
        return true;
    }

    private bool ApplyIntervention(int interventionId, int planeCode, HexCoord coord)
    {
        if (interventionId == TheCanonryGame.Game.Tableau.GameInterventionIds.Consecrate)
        {
            if (!_map!.TryGetHex(planeCode, coord, out var hex) ||
                hex.CultureInfluences.Count == 0)
            {
                return false;
            }

            var cultureId = hex.CultureInfluences[0].CultureId;
            return cultureId > 0 &&
                _runtime!.Intents.RequestConsecrate(cultureId, planeCode, coord.Q, coord.R);
        }

        if (interventionId == TheCanonryGame.Game.Tableau.GameInterventionIds.Favor)
        {
            var occupantId = _ui!.ActiveOccupants(planeCode, coord).FirstOrDefault();
            return occupantId > 0 && _runtime!.Intents.RequestFavor(occupantId);
        }

        return false;
    }

    public void BindRuntime(CanonryGameRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        _runtime = runtime;
        _map = runtime.ReadModels.Map;
        _ui = runtime.ReadModels.Entities;
        _symbols = runtime.ReadModels.Symbols;
        _chronicle = runtime.ReadModels.Chronicle;
        _seed = runtime.Seed.BaseSeed;
        _seedAttempt = runtime.Seed.Attempt;
        BindNodesToRuntime();
        ResetPresenters();
    }

    public void CloseOpenCards() => _cardLayer.CloseAll();

    public void ClearTooltip()
    {
        _tooltipPollingEnabled = false;
        _tooltipRefreshElapsedMs = TooltipRefreshIntervalMs;
        _hexTooltipPanel.ClearTooltip();
    }

    public void Present(int currentTick, string timeState)
    {
        if (_runtime is null || _ui is null || _map is null || _chronicle is null)
        {
            return;
        }

        _chronicle.Refresh();
        _topBarRender.Present(_topBar, _ui, currentTick, timeState, _seed, _seedAttempt);
        _mapRender.Present(_mapRenderer, _map, _ui);
        PresentCultureOverview(currentTick);
        _browserRender.Present(_browser, _ui);
        _entityMapRender.Present(_entityLayer, _ui, _browser, _entityNamesVisibleForZoom);
        _cardLayerRender.Present(_cardLayer, _ui);
        _attributeGraphRender.Present(ActiveAttributeGraph(), _ui);
        _chronicleRender.Present(_chroniclePanel, _chronicle);
        _tableauRender.Present(_tableauPanel, _ui, _tableauVisible);
        _floatingHudLayoutRender.Present(_mapArea, _tableauPanel, _tableauVisible);
    }

    public void PresentTooltip(double delta)
    {
        if (!_tooltipPollingEnabled)
        {
            return;
        }

        _tooltipRefreshElapsedMs += Math.Max(0d, delta * 1000d);
        if (_tooltipRefreshElapsedMs < TooltipRefreshIntervalMs)
        {
            return;
        }

        _tooltipRefreshElapsedMs = 0d;
        _tooltipPollingEnabled = _hexTooltipPanel.UpdateTooltip();
    }

    public void NotifyPointerMoved()
    {
        _tooltipPollingEnabled = true;
        _tooltipRefreshElapsedMs = TooltipRefreshIntervalMs;
    }

    private void PresentCultureOverview(int currentTick)
    {
        var ui = _ui;
        if (ui is null)
        {
            return;
        }

        var plane = _mapRenderer.CurrentPlane;
        if (_lastCultureOverviewTick == currentTick &&
            _lastCultureOverviewPlane == plane &&
            ReferenceEquals(_lastCultureOverviewMap, _map) &&
            _lastCultureOverviewActivityVersion == ui.MinorActorActivityVersion)
        {
            return;
        }

        _mapArea.BindCultureOverview(_map, ui, _symbols, plane, _runtime?.ReadModels.CulturePowers());
        _lastCultureOverviewTick = currentTick;
        _lastCultureOverviewPlane = plane;
        _lastCultureOverviewMap = _map;
        _lastCultureOverviewActivityVersion = ui.MinorActorActivityVersion;
    }

    public void ToggleEntityNames()
    {
        if (_entityLayer.SetShowEntityNames(!_entityLayer.ShowEntityNames))
        {
            _entityMapRender.Invalidate();
        }
    }

    public void TogglePlane()
    {
        _mapRenderer.CurrentPlane = _mapRenderer.CurrentPlane == PlaneMode.Physical
            ? PlaneMode.Semantic
            : PlaneMode.Physical;
        _ = _entityLayer.SetPlane(_mapRenderer.CurrentPlane);
        _mapArea.SetPlane(_mapRenderer.CurrentPlane);
        _hexTooltipPanel.ClearTooltip();
        _mapRender.Invalidate();
        _entityMapRender.Invalidate();
    }

    public void InvalidateEntityMap() => _entityMapRender.Invalidate();

    public void SetCameraZoom(Vector2 zoom)
    {
        _entityNamesVisibleForZoom = MathF.Max(zoom.X, zoom.Y) >= 3.1f;
    }

    public void SetMapLayerVisible(string layerKey, bool visible)
    {
        if (!ApplyMapLayerVisibility(layerKey, visible))
        {
            return;
        }

        _mapLensKey = MapArea.LensCustom;
        _mapArea.SetLensKey(_mapLensKey);
        _mapArea.SetLayerVisible(layerKey, visible);
        _mapRender.InvalidateFrame();
    }

    private bool ApplyMapLayerVisibility(string layerKey, bool visible) => layerKey switch
    {
        MapArea.LayerRoads => SetLayerVisibility(_mapRenderer.ShowRoads, visible, value => _mapRenderer.ShowRoads = value),
        MapArea.LayerRoutes => SetLayerVisibility(_mapRenderer.ShowRoutes, visible, value => _mapRenderer.ShowRoutes = value),
        MapArea.LayerBorders => SetLayerVisibility(_mapRenderer.ShowBorders, visible, value => _mapRenderer.ShowBorders = value),
        MapArea.LayerLabels => SetLayerVisibility(_mapRenderer.ShowLabels, visible, value => _mapRenderer.ShowLabels = value),
        MapArea.LayerTerritory => SetLayerVisibility(_mapRenderer.ShowTerritory, visible, value => _mapRenderer.ShowTerritory = value),
        MapArea.LayerInfluence => SetLayerVisibility(_mapRenderer.ShowInfluence, visible, value => _mapRenderer.ShowInfluence = value),
        MapArea.LayerLabor => SetLayerVisibility(_mapRenderer.ShowMinorActors, visible, value => _mapRenderer.ShowMinorActors = value),
        MapArea.LayerCultureInfluence => SetLayerVisibility(
            _mapRenderer.ShowCultureInfluence,
            visible,
            value => _mapRenderer.ShowCultureInfluence = value),
        _ => false,
    };

    private static bool SetLayerVisibility(bool current, bool visible, Action<bool> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        if (current == visible)
        {
            return false;
        }

        apply(visible);
        return true;
    }

    public void SetMapLens(string lensKey)
    {
        _mapLensKey = lensKey switch
        {
            MapArea.LensCulture => MapArea.LensCulture,
            MapArea.LensControl => MapArea.LensControl,
            MapArea.LensInfluence => MapArea.LensInfluence,
            _ => MapArea.LensDefault,
        };
        ApplyMapLens();
        _mapRender.InvalidateFrame();
    }

    public void ToggleAttributeGraph()
    {
        if (ActiveAttributeGraph() is not null)
        {
            _attributeGraph!.QueueFree();
            _attributeGraph = null;
            _bottomBar.SetReadingsVisible(false);
            _attributeGraphRender.Invalidate();
            return;
        }

        if (_ui is null)
        {
            return;
        }

        _attributeGraph = new AttributeGraphPanel { Theme = _theme };
        _cardLayer.AddChild(_attributeGraph);
        _attributeGraph.Initialize(_ui, _symbols);
        _attributeGraph.Position = new Vector2(320, UiTokens.Space7);
        _bottomBar.SetReadingsVisible(true);
        _attributeGraphRender.Invalidate();
    }

    public void ToggleTableauVisibility()
    {
        _tableauVisible = !_tableauVisible;
        _tableauPanel.Visible = _tableauVisible;
        _bottomBar.SetTableauVisible(_tableauVisible);
        _floatingHudLayoutRender.Invalidate();
        _tableauRender.Invalidate();
    }

    public void OpenCard(int entityId, Vector2 position)
    {
        _cardLayer.Open(entityId, position);
        _cardLayerRender.Invalidate();
    }

    private void BindNodesToRuntime()
    {
        _mapRenderer.Initialize(_map!, _ui, _symbols);
        _entityLayer.Initialize(_ui, _symbols, _mapRenderer.HexSize);
        _ = _entityLayer.SetPlane(_mapRenderer.CurrentPlane);
        _browser.Initialize(_ui, _symbols);
        _attributeGraph?.Initialize(_ui, _symbols);
        _cardLayer.Initialize(_ui, _symbols, _theme);
        _chroniclePanel.Initialize(_symbols);
        _chroniclePanel.Clear();
        _hexTooltipPanel.Initialize(_map, _ui, _symbols, _mapRenderer, _worldCanvas, [_rootColumn, _mapArea, _uiRoot]);
        _tableauPanel.Initialize(_runtime?.Tableau);
        _tableauVisible = _tableauPanel.Visible;
        _mapArea.SetPlane(_mapRenderer.CurrentPlane);
        ApplyMapLens();
        _mapArea.BindCultureOverview(_map, _ui, _symbols, _mapRenderer.CurrentPlane, _runtime?.ReadModels.CulturePowers());
        _bottomBar.SetTableauVisible(_tableauVisible);
        _bottomBar.SetReadingsVisible(_attributeGraph is not null);
    }

    private void ApplyMapLens()
    {
        if (!string.Equals(_mapLensKey, MapArea.LensCustom, StringComparison.Ordinal))
        {
            var settings = MapLensSettings.For(_mapLensKey);
            _mapRenderer.ShowRoads = settings.Roads;
            _mapRenderer.ShowRoutes = settings.Routes;
            _mapRenderer.ShowBorders = settings.Borders;
            _mapRenderer.ShowLabels = settings.Labels;
            _mapRenderer.ShowTerritory = settings.Territory;
            _mapRenderer.ShowCultureInfluence = settings.Culture;
            _mapRenderer.ShowInfluence = settings.Influence;
            _mapRenderer.ShowMinorActors = settings.Labor;
        }

        _mapArea.SetLensKey(_mapLensKey);
        SyncLayerControls();
    }

    private void SyncLayerControls()
    {
        _mapArea.SetLayerVisible(MapArea.LayerRoads, _mapRenderer.ShowRoads);
        _mapArea.SetLayerVisible(MapArea.LayerRoutes, _mapRenderer.ShowRoutes);
        _mapArea.SetLayerVisible(MapArea.LayerBorders, _mapRenderer.ShowBorders);
        _mapArea.SetLayerVisible(MapArea.LayerLabels, _mapRenderer.ShowLabels);
        _mapArea.SetLayerVisible(MapArea.LayerTerritory, _mapRenderer.ShowTerritory);
        _mapArea.SetLayerVisible(MapArea.LayerCultureInfluence, _mapRenderer.ShowCultureInfluence);
        _mapArea.SetLayerVisible(MapArea.LayerInfluence, _mapRenderer.ShowInfluence);
        _mapArea.SetLayerVisible(MapArea.LayerLabor, _mapRenderer.ShowMinorActors);
    }

    private void ResetPresenters()
    {
        _lastCultureOverviewTick = -1;
        _lastCultureOverviewPlane = null;
        _lastCultureOverviewMap = null;
        _lastCultureOverviewActivityVersion = -1;
        _topBarRender.Reset();
        _mapRender.Reset();
        _entityMapRender.Reset();
        _browserRender.Reset();
        _chronicleRender.Reset();
        _cardLayerRender.Reset();
        _attributeGraphRender.Reset();
        _tableauRender.Reset();
        _floatingHudLayoutRender.Reset();
    }

    private AttributeGraphPanel? ActiveAttributeGraph()
    {
        if (_attributeGraph is null)
        {
            return null;
        }

        if (GodotObject.IsInstanceValid(_attributeGraph) && !_attributeGraph.IsQueuedForDeletion())
        {
            return _attributeGraph;
        }

        _attributeGraph = null;
        _bottomBar.SetReadingsVisible(false);
        _attributeGraphRender.Invalidate();
        return null;
    }

    private sealed class TopBarRender
    {
        private TopBarState? _lastState;

        public void Reset() => _lastState = null;

        public void Present(
            TopBar topBar,
            UiProjectionState ui,
            int currentTick,
            string timeState,
            int seed,
            int seedAttempt)
        {
            var state = new TopBarState(
                currentTick,
                timeState,
                ui.ActiveEntityCount,
                ui.GeneratorFires.Count,
                seed,
                seedAttempt,
                ui.TopBarVersion);
            if (_lastState == state)
            {
                return;
            }

            _lastState = state;
            topBar.SetStatus(currentTick, timeState, ui.ActiveEntityCount, ui.GeneratorFires.Count, seed, seedAttempt);
        }
    }

    private sealed class MapRender
    {
        private MapRenderState? _lastCells;
        private MapRenderState? _lastFrame;

        public void Reset()
        {
            _lastCells = null;
            _lastFrame = null;
        }

        public void Invalidate() => Reset();

        public void InvalidateFrame() => _lastFrame = null;

        public void Present(HexMapRenderer renderer, HexMapProjection map, UiProjectionState ui)
        {
            var state = MapRenderState.From(renderer, map, ui);
            var cellState = state with
            {
                FrameVersion = 0,
                OwnerColorVersion = 0,
                CultureOverlayVersion = 0,
                InfluenceOverlayVersion = 0,
                MinorActorActivityVersion = 0,
                MinorActorActivityBucket = 0,
                RoadNetworkVersion = 0,
                RouteNetworkVersion = 0,
                ShowTerritory = false,
                ShowCultureInfluence = false,
                ShowInfluence = false,
                ShowMinorActors = false,
                ShowRoads = false,
                ShowRoutes = false,
                ShowBorders = false,
                ShowLabels = false,
            };
            var frameState = FrameState(state);
            if (_lastCells is null || CellLayoutChanged(_lastCells.Value, cellState))
            {
                renderer.RefreshCells();
                map.ClearDirtyCells(renderer.CurrentPlaneCode);
                _lastCells = cellState;
            }
            else if (_lastCells != cellState)
            {
                renderer.RefreshDirtyCells(map.DirtyCellIds(renderer.CurrentPlaneCode));
                map.ClearDirtyCells(renderer.CurrentPlaneCode);
                _lastCells = cellState;
            }

            if (_lastFrame != frameState)
            {
                renderer.RefreshFrame();
                _lastFrame = frameState;
            }
        }

        private static bool CellLayoutChanged(MapRenderState previous, MapRenderState current) =>
            previous.Plane != current.Plane ||
            previous.ShowTerritory != current.ShowTerritory ||
            previous.ShowCultureInfluence != current.ShowCultureInfluence ||
            previous.ShowInfluence != current.ShowInfluence ||
            previous.ShowRoads != current.ShowRoads ||
            previous.ShowRoutes != current.ShowRoutes ||
            previous.ShowBorders != current.ShowBorders ||
            previous.ShowLabels != current.ShowLabels ||
            previous.HexSize != current.HexSize;

        private static MapRenderState FrameState(MapRenderState state)
        {
            return state with
            {
                CellVersion = 0,
                FrameVersion = ActiveFrameVersion(state),
                OwnerColorVersion = ActiveOwnerColorVersion(state),
                CultureOverlayVersion = ActiveLayerVersion(state.ShowCultureInfluence, state.CultureOverlayVersion),
                InfluenceOverlayVersion = ActiveLayerVersion(state.ShowInfluence, state.InfluenceOverlayVersion),
                MinorActorActivityVersion = ActiveLayerVersion(state.ShowMinorActors, state.MinorActorActivityVersion),
                MinorActorActivityBucket = state.ShowMinorActors ? state.MinorActorActivityBucket : 0,
                RoadNetworkVersion = ActiveLayerVersion(state.ShowRoads, state.RoadNetworkVersion),
                RouteNetworkVersion = ActiveLayerVersion(state.ShowRoutes, state.RouteNetworkVersion),
            };
        }

        private static long ActiveFrameVersion(MapRenderState state) =>
            FrameGeometryVisible(state) ? state.FrameVersion : 0;

        private static long ActiveOwnerColorVersion(MapRenderState state) =>
            state.ShowTerritory || state.ShowInfluence ? state.OwnerColorVersion : 0;

        private static long ActiveLayerVersion(bool visible, long version) =>
            visible ? version : 0;

        private static bool FrameGeometryVisible(MapRenderState state) =>
            AreaLayerVisible(state) ||
            NetworkLayerVisible(state) ||
            AnnotationLayerVisible(state) ||
            state.Plane == PlaneMode.Semantic;

        private static bool AreaLayerVisible(MapRenderState state) =>
            state.ShowTerritory || state.ShowCultureInfluence || state.ShowInfluence;

        private static bool NetworkLayerVisible(MapRenderState state) =>
            state.ShowRoads || state.ShowRoutes;

        private static bool AnnotationLayerVisible(MapRenderState state) =>
            state.ShowBorders || state.ShowLabels || state.ShowMinorActors;
    }

    private sealed class EntityMapRender
    {
        private EntityMapState? _lastState;

        public void Reset() => _lastState = null;

        public void Invalidate() => Reset();

        public void Present(EntityLayer layer, UiProjectionState ui, EntityBrowser browser, bool showNamesForZoom)
        {
            var zoomNamesChanged = layer.SetShowEntityNamesForZoom(showNamesForZoom);
            var state = new EntityMapState(
                ui.EntityMapVersion,
                layer.CurrentPlane,
                layer.EffectiveShowEntityNames,
                ui.CurrentTick / 20,
                browser.FilterVersion,
                browser.FilterMapToBrowser,
                browser.SelectedGeneratorFilterId);
            if (_lastState == state)
            {
                return;
            }

            var filterChanged = layer.SetVisibleEntityIds(browser.VisibleEntityIdsForMap());
            var shouldFullRefresh = _lastState is null ||
                _lastState.Value.Plane != state.Plane ||
                _lastState.Value.ShowNames != state.ShowNames ||
                _lastState.Value.ActionPulseBucket != state.ActionPulseBucket ||
                filterChanged ||
                zoomNamesChanged;
            _lastState = state;
            if (shouldFullRefresh)
            {
                layer.Refresh();
                ui.ClearDirtyEntityHexes(layer.CurrentPlaneCode);
                return;
            }

            layer.RefreshDirty(ui.DirtyEntityHexes(layer.CurrentPlaneCode));
            ui.ClearDirtyEntityHexes(layer.CurrentPlaneCode);
        }
    }

    private sealed class EntityBrowserRender
    {
        private BrowserState? _lastState;

        public void Reset() => _lastState = null;

        public void Present(EntityBrowser browser, UiProjectionState ui)
        {
            var state = new BrowserState(ui.EntityBrowserVersion);
            if (_lastState == state)
            {
                return;
            }

            _lastState = state;
            browser.Refresh();
        }
    }

    private sealed class ChronicleRender
    {
        private ChronicleState? _lastState;

        public void Reset() => _lastState = null;

        public void Present(ChroniclePanel panel, IGameChronicleFeed chronicle)
        {
            var state = new ChronicleState(chronicle.Version + (chronicle.TalesVersion << 20), panel.FilterVersion);
            if (_lastState == state)
            {
                return;
            }

            _lastState = state;
            panel.SetEntries(chronicle.Entries);
            panel.SetTales(chronicle.Tales);
        }
    }

    private sealed class CardLayerRender
    {
        private CardLayerState? _lastState;

        public void Reset() => _lastState = null;

        public void Invalidate() => Reset();

        public void Present(EntityCardLayer layer, UiProjectionState ui)
        {
            var state = new CardLayerState(layer.Version, ui.EntityDetailVersion, ui.RelationshipVersion);
            if (_lastState == state)
            {
                return;
            }

            _lastState = state;
            layer.Refresh();
        }
    }

    private sealed class AttributeGraphRender
    {
        private long _lastVersion = -1;

        public void Reset() => _lastVersion = -1;

        public void Invalidate() => Reset();

        public void Present(AttributeGraphPanel? panel, UiProjectionState ui)
        {
            if (panel is null || _lastVersion == ui.EntityDetailVersion)
            {
                return;
            }

            _lastVersion = ui.EntityDetailVersion;
            panel.Refresh();
        }
    }

    private sealed class TableauRender
    {
        private TableauState? _lastState;

        public void Reset() => _lastState = null;

        public void Invalidate() => Reset();

        public void Present(TableauPanel panel, UiProjectionState ui, bool visible)
        {
            var state = new TableauState(visible, ui.TableauVersion);
            if (_lastState == state)
            {
                return;
            }

            _lastState = state;
            if (visible)
            {
                panel.Refresh();
            }
        }
    }

    private sealed class FloatingHudLayoutRender
    {
        private FloatingHudLayoutState? _lastState;
        private bool _dirty = true;

        public void Reset()
        {
            _lastState = null;
            _dirty = true;
        }

        public void Invalidate() => _dirty = true;

        public void Present(MapArea mapArea, TableauPanel tableauPanel, bool tableauVisible)
        {
            if (!tableauVisible)
            {
                return;
            }

            if (!_dirty)
            {
                return;
            }

            var mapRect = mapArea.GetGlobalRect();
            var state = new FloatingHudLayoutState(mapRect.Position, mapRect.Size);
            if (_lastState == state)
            {
                _dirty = false;
                return;
            }

            PositionFloatingHud(tableauPanel, mapRect);
            _lastState = state;
            _dirty = false;
        }

        private static void PositionFloatingHud(TableauPanel tableauPanel, Rect2 mapRect)
        {
            var padding = UiTokens.Space4 * 2f;
            var width = Mathf.Min(Mathf.Max(720f, mapRect.Size.X - padding), 1080f);
            var height = Mathf.Min(Mathf.Max(520f, mapRect.Size.Y - padding), 780f);
            tableauPanel.CustomMinimumSize = new Vector2(width, height);
            tableauPanel.Position = new Vector2(
                mapRect.Position.X + ((mapRect.Size.X - width) * 0.5f),
                mapRect.Position.Y + ((mapRect.Size.Y - height) * 0.5f));
        }
    }

    private readonly record struct TopBarState(
        int Tick,
        string TimeState,
        int EntityCount,
        int GeneratorFireCount,
        int Seed,
        int SeedAttempt,
        long Version);

    private readonly record struct MapRenderState(
        long CellVersion,
        long FrameVersion,
        long OwnerColorVersion,
        long CultureOverlayVersion,
        long InfluenceOverlayVersion,
        long MinorActorActivityVersion,
        int MinorActorActivityBucket,
        long RoadNetworkVersion,
        long RouteNetworkVersion,
        PlaneMode Plane,
        bool ShowTerritory,
        bool ShowCultureInfluence,
        bool ShowInfluence,
        bool ShowMinorActors,
        bool ShowRoads,
        bool ShowRoutes,
        bool ShowBorders,
        bool ShowLabels,
        float HexSize)
    {
        public static MapRenderState From(HexMapRenderer renderer, HexMapProjection map, UiProjectionState ui) =>
            new(
                map.CellVersion,
                map.FrameVersion,
                map.OwnerColorVersion,
                map.CultureOverlayVersion,
                map.InfluenceOverlayVersion,
                ui.MinorActorActivityVersion,
                ui.CurrentTick / 10,
                map.RoadNetworkVersion,
                map.RouteNetworkVersion,
                renderer.CurrentPlane,
                renderer.ShowTerritory,
                renderer.ShowCultureInfluence,
                renderer.ShowInfluence,
                renderer.ShowMinorActors,
                renderer.ShowRoads,
                renderer.ShowRoutes,
                renderer.ShowBorders,
                renderer.ShowLabels,
                renderer.HexSize);
    }

    private readonly record struct MapLensSettings(
        bool Roads,
        bool Routes,
        bool Borders,
        bool Labels,
        bool Territory,
        bool Culture,
        bool Influence,
        bool Labor)
    {
        public static MapLensSettings For(string lensKey) => lensKey switch
        {
            MapArea.LensCulture => new(
                Roads: false,
                Routes: false,
                Borders: true,
                Labels: false,
                Territory: false,
                Culture: true,
                Influence: false,
                Labor: true),
            MapArea.LensControl => new(
                Roads: true,
                Routes: true,
                Borders: true,
                Labels: true,
                Territory: true,
                Culture: false,
                Influence: false,
                Labor: false),
            MapArea.LensInfluence => new(
                Roads: false,
                Routes: false,
                Borders: false,
                Labels: false,
                Territory: false,
                Culture: false,
                Influence: true,
                Labor: false),
            _ => new(
                Roads: true,
                Routes: true,
                Borders: true,
                Labels: false,
                Territory: false,
                Culture: false,
                Influence: false,
                Labor: false),
        };
    }

    private readonly record struct EntityMapState(
        long Version,
        PlaneMode Plane,
        bool ShowNames,
        int ActionPulseBucket,
        long BrowserFilterVersion,
        bool FilterMapToBrowser,
        int SelectedGeneratorFilterId);

    private readonly record struct BrowserState(long Version);

    private readonly record struct ChronicleState(long Version, long FilterVersion);

    private readonly record struct CardLayerState(long CardVersion, long EntityDetailVersion, long RelationshipVersion);

    private readonly record struct TableauState(bool Visible, long Version);

    private readonly record struct FloatingHudLayoutState(Vector2 MapPosition, Vector2 MapSize);
}
