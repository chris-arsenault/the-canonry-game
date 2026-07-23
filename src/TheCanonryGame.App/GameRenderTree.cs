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

internal sealed partial class GameRenderTree
{
    private const long TooltipRefreshIntervalMs = 100;

    private readonly HexMapRenderer _mapRenderer;
    private readonly EntityLayer _entityLayer;
    private readonly PhysicalMap3DInspector _physicalMap3DInspector;
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
    private readonly TechnologyOverviewRender _technologyOverviewRender = new();
    private readonly ChronicleAuraLayer _chronicleAura = new();
    private readonly MinorActorFlowLayer _minorActorFlow = new();
    private readonly EntityTrailLayer _entityTrails = new();
    private readonly CardLayerRender _cardLayerRender = new();
    private readonly AttributeGraphRender _attributeGraphRender = new();
    private readonly TableauRender _tableauRender = new();
    private readonly FloatingHudLayoutRender _floatingHudLayoutRender = new();
    private CanonryGameRuntime? _runtime;
    private HexMapProjection? _map;
    private PhysicalGeographyProjection? _physicalGeography;
    private UiProjectionState? _ui;
    private IProjectionSymbolCatalog? _symbols;
    private IGameChronicleFeed? _chronicle;
    private AttributeGraphPanel? _attributeGraph;
    private ChronicleJournal? _journal;
    private TechnologyOverview? _technologyOverview;
    private readonly WorldBeatsStrip _worldBeatsStrip = new();
    private WorldBeatsPanel? _worldBeatsPanel;

    public event Action<int>? JournalEntitySelected;
    private HexMapProjection? _lastCultureOverviewMap;
    private PlaneMode? _lastCultureOverviewPlane;
    private long _lastCultureOverviewCellVersion = -1;
    private long _lastCultureOverviewEntityStructureVersion = -1;
    private int _lastCultureOverviewActivityBucket = -1;
    private bool _tableauVisible;
    private int _seed;
    private int _seedAttempt;
    private bool _tooltipPollingEnabled;
    private bool _entityNamesVisibleForZoom;
    private double _tooltipRefreshElapsedMs = TooltipRefreshIntervalMs;
    private string _mapLensKey = MapArea.LensDefault;
    private long _worldBeatAgesVersion = -1;
    private string _worldBeatAgesLabel = string.Empty;
    private PlaneMode _planeBeforePhysical3D = PlaneMode.Physical;

    public bool Physical3DInspectionActive => _physicalMap3DInspector.Active;

    internal GameRenderTree(
        HexMapRenderer mapRenderer,
        EntityLayer entityLayer,
        PhysicalMap3DInspector physicalMap3DInspector,
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
        _physicalMap3DInspector = physicalMap3DInspector;
        _chronicleAura.Initialize(entityLayer);
        entityLayer.AddChild(_chronicleAura);
        entityLayer.AddChild(_minorActorFlow);
        entityLayer.AddChild(_entityTrails);
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
        if (Physical3DInspectionActive ||
            _armedInterventionId <= 0 ||
            _runtime is null ||
            _map is null ||
            _ui is null)
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
        _physicalGeography = runtime.ReadModels.PhysicalGeography;
        _ui = runtime.ReadModels.Entities;
        _symbols = runtime.ReadModels.Symbols;
        _chronicle = runtime.ReadModels.Chronicle;
        _minorActorFlow.Initialize(
            runtime.ReadModels.MinorActorFlow,
            _entityLayer,
            cultureId => UiTokens.CultureColor(ProjectionDisplay.StableName(
                _symbols,
                ProjectionSymbolKind.Culture,
                cultureId,
                cultureId == 0 ? "none" : $"culture-{cultureId}")));
        _entityTrails.Initialize(
            runtime.ReadModels.EntityTrails,
            _entityLayer,
            entityId =>
            {
                var kindId = _ui is not null && _ui.Entities.TryGetValue(entityId, out var entity)
                    ? entity.KindId
                    : 0;
                return UiTokens.KindAccent(ProjectionDisplay.StableName(
                    _symbols,
                    ProjectionSymbolKind.Kind,
                    kindId,
                    $"kind-{kindId}"));
            });
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
        _topBarRender.Present(_topBar, _ui, currentTick, timeState, _seed, _seedAttempt, WorldBeatAgesLabel());
        if (!Physical3DInspectionActive)
        {
            _mapRender.Present(_mapRenderer, _map, _ui);
            PresentCultureOverview();
        }

        _browserRender.Present(_browser, _ui);
        if (!Physical3DInspectionActive)
        {
            _entityMapRender.Present(_entityLayer, _ui, _browser, _entityNamesVisibleForZoom);
            _minorActorFlow.Present();
            _entityTrails.Present();
        }

        _cardLayerRender.Present(_cardLayer, _ui);
        _attributeGraphRender.Present(ActiveAttributeGraph(), _ui);
        _chronicleRender.Present(_chroniclePanel, _chronicle, _chronicleAura);
        if (_technologyOverview is not null)
        {
            _technologyOverviewRender.Present(_runtime.ReadModels.Technologies.Current());
        }
        _worldBeatsStrip.Refresh();
        _worldBeatsPanel?.Refresh();
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

    private void PresentCultureOverview()
    {
        var ui = _ui;
        if (ui is null)
        {
            return;
        }

        var plane = _mapRenderer.CurrentPlane;
        if (CultureOverviewUnchanged(ui, plane))
        {
            return;
        }

        _mapArea.BindCultureOverview(_map, ui, _symbols, plane, _runtime?.ReadModels.CulturePowers());
        _lastCultureOverviewPlane = plane;
        _lastCultureOverviewMap = _map;
        _lastCultureOverviewCellVersion = _map?.CellVersion ?? -1;
        _lastCultureOverviewEntityStructureVersion = ui.EntityBrowserStructureVersion;
        _lastCultureOverviewActivityBucket = ui.CurrentTick / 10;
    }

    private bool CultureOverviewUnchanged(UiProjectionState ui, PlaneMode plane) =>
        _lastCultureOverviewPlane == plane &&
        ReferenceEquals(_lastCultureOverviewMap, _map) &&
        _lastCultureOverviewCellVersion == _map?.CellVersion &&
        _lastCultureOverviewEntityStructureVersion == ui.EntityBrowserStructureVersion &&
        _lastCultureOverviewActivityBucket == ui.CurrentTick / 10;

    public void ToggleEntityNames()
    {
        _ = _entityLayer.SetShowEntityNames(!_entityLayer.ShowEntityNames);
    }

    public void TogglePlane()
    {
        if (Physical3DInspectionActive)
        {
            return;
        }

        _mapRenderer.CurrentPlane = _mapRenderer.CurrentPlane == PlaneMode.Physical
            ? PlaneMode.Semantic
            : PlaneMode.Physical;
        _ = _entityLayer.SetPlane(_mapRenderer.CurrentPlane);
        _mapArea.SetPlane(_mapRenderer.CurrentPlane);
        _hexTooltipPanel.ClearTooltip();
        _mapRender.Invalidate();
        _entityMapRender.Invalidate();
        _minorActorFlow.Invalidate();
        _entityTrails.Invalidate();
    }

    public void InvalidateEntityMap() => _entityMapRender.Invalidate();

    public void SetCameraZoom(Vector2 zoom)
    {
        var cameraZoom = MathF.Max(zoom.X, zoom.Y);
        _entityNamesVisibleForZoom = cameraZoom >= 3.1f;
        _ = _entityLayer.SetCameraZoom(cameraZoom);
        _ = _mapRenderer.SetCameraZoom(cameraZoom);
    }

    public void NotifyViewportChanged()
    {
        _entityLayer.NotifyViewportChanged();
        _mapRenderer.NotifyViewportChanged();
    }

    public void ToggleChronicleJournal()
    {
        if (_journal is not null)
        {
            _journal.QueueFree();
            _journal = null;
            _chronicleRender.Journal = null;
            return;
        }

        _journal = new ChronicleJournal { Theme = _theme };
        _cardLayer.AddChild(_journal);
        _journal.Position = new Vector2(240, UiTokens.Space7);
        _journal.EntitySelected += entityId => JournalEntitySelected?.Invoke(entityId);
        _journal.CloseRequested += ToggleChronicleJournal;
        _chronicleRender.Journal = _journal;
        if (_chronicle is not null)
        {
            _journal.SetTaleViews(_chronicle.TaleViews);
        }
    }

    public void ToggleTechnologyOverview()
    {
        if (_technologyOverview is not null)
        {
            _technologyOverview.QueueFree();
            _technologyOverview = null;
            _technologyOverviewRender.Panel = null;
            return;
        }

        if (_runtime is null)
        {
            return;
        }

        _technologyOverview = new TechnologyOverview { Theme = _theme };
        _cardLayer.AddChild(_technologyOverview);
        _technologyOverview.Position = new Vector2(280, UiTokens.Space7);
        _technologyOverview.CloseRequested += ToggleTechnologyOverview;
        _technologyOverviewRender.Panel = _technologyOverview;
        _technologyOverviewRender.Present(_runtime.ReadModels.Technologies.Current());
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
        _mapRenderer.Initialize(_map!, _physicalGeography!, _ui, _symbols);
        _physicalMap3DInspector.Bind(_physicalGeography, _mapRenderer.HexSize);
        _entityLayer.Initialize(_ui, _symbols, _mapRenderer.HexSize);
        _ = _entityLayer.SetPlane(_mapRenderer.CurrentPlane);
        _browser.Initialize(_ui, _symbols);
        _attributeGraph?.Initialize(_ui, _symbols);
        _cardLayer.Initialize(_ui, _symbols, _theme);
        _chroniclePanel.Initialize(_symbols);
        _chroniclePanel.Clear();
        _hexTooltipPanel.Initialize(
            _map,
            _physicalGeography,
            _ui,
            _symbols,
            _mapRenderer,
            _physicalMap3DInspector,
            _worldCanvas,
            [_rootColumn, _mapArea, _uiRoot]);
        _tableauPanel.Initialize(_runtime?.Tableau);
        _tableauVisible = _tableauPanel.Visible;
        _mapArea.SetPlane(_mapRenderer.CurrentPlane);
        ApplyMapLens();
        _mapArea.BindCultureOverview(_map, _ui, _symbols, _mapRenderer.CurrentPlane, _runtime?.ReadModels.CulturePowers());
        _bottomBar.SetTableauVisible(_tableauVisible);
        _bottomBar.SetReadingsVisible(_attributeGraph is not null);
        BindWorldBeatsStrip();
    }

    private void BindWorldBeatsStrip()
    {
        if (_worldBeatsStrip.GetParent() is null)
        {
            _cardLayer.AddChild(_worldBeatsStrip);
            _worldBeatsStrip.Position = new Vector2(240, UiTokens.Space2);
            _worldBeatsStrip.ToggleRequested += ToggleWorldBeatsPanel;
        }

        if (_ui is not null)
        {
            _worldBeatsStrip.Initialize(_ui, _symbols);
        }
    }

    public void ToggleWorldBeatsPanel()
    {
        if (_worldBeatsPanel is not null)
        {
            _worldBeatsPanel.QueueFree();
            _worldBeatsPanel = null;
            return;
        }

        if (_ui is null)
        {
            return;
        }

        _worldBeatsPanel = new WorldBeatsPanel { Theme = _theme };
        _cardLayer.AddChild(_worldBeatsPanel);
        _worldBeatsPanel.Position = new Vector2(240, UiTokens.Space7);
        _worldBeatsPanel.CloseRequested += ToggleWorldBeatsPanel;
        _worldBeatsPanel.Initialize(_ui, _symbols);
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
        _mapArea.SetLayerVisible(
            MapArea.LayerWorldgenInspection,
            _mapRenderer.WorldgenInspectionEnabled);
        _mapArea.SetLayerVisible(
            MapArea.LayerGeographyFeatures,
            _mapRenderer.GeographyFeaturesEnabled);
        _mapArea.SetGeographyViewKey(GeographyViewKey(_mapRenderer.GeographyView));
    }

    private void ResetPresenters()
    {
        _lastCultureOverviewPlane = null;
        _lastCultureOverviewMap = null;
        _lastCultureOverviewCellVersion = -1;
        _lastCultureOverviewEntityStructureVersion = -1;
        _lastCultureOverviewActivityBucket = -1;
        _worldBeatAgesVersion = -1;
        _worldBeatAgesLabel = string.Empty;
        _topBarRender.Reset();
        _mapRender.Reset();
        _entityMapRender.Reset();
        _browserRender.Reset();
        _chronicleRender.Reset();
        _technologyOverviewRender.Reset();
        _cardLayerRender.Reset();
        _attributeGraphRender.Reset();
        _tableauRender.Reset();
        _floatingHudLayoutRender.Reset();
    }

    private string WorldBeatAgesLabel()
    {
        if (_ui is null)
        {
            return string.Empty;
        }

        if (_worldBeatAgesVersion == _ui.WorldBeatVersion)
        {
            return _worldBeatAgesLabel;
        }

        var names = new List<string>(3);
        foreach (var ageId in _ui.WorldBeatAgeIds)
        {
            if (ageId > 0)
            {
                names.Add(ProjectionDisplay.DisplayName(_symbols, ProjectionSymbolKind.Generator, ageId, $"age-{ageId}"));
            }
        }

        _worldBeatAgesVersion = _ui.WorldBeatVersion;
        _worldBeatAgesLabel = names.Count == 0 ? string.Empty : string.Join(" · ", names);
        return _worldBeatAgesLabel;
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
            int seedAttempt,
            string agesLabel)
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
            topBar.SetStatus(currentTick, timeState, ui.ActiveEntityCount, ui.GeneratorFires.Count, seed, seedAttempt, agesLabel);
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
            layer.SetCurrentTick(ui.CurrentTick);
            _ = layer.SetShowEntityNamesForZoom(showNamesForZoom);
            var state = new EntityMapState(
                ui.EntityMapVersion,
                layer.CurrentPlane,
                browser.FilterVersion,
                browser.FilterMapToBrowser,
                browser.SelectedGeneratorFilterId);
            if (_lastState == state)
            {
                return;
            }

            var browserFilterChanged = _lastState is null ||
                _lastState.Value.BrowserFilterVersion != state.BrowserFilterVersion ||
                _lastState.Value.FilterMapToBrowser != state.FilterMapToBrowser ||
                _lastState.Value.SelectedGeneratorFilterId != state.SelectedGeneratorFilterId;
            var filterChanged = browserFilterChanged &&
                layer.SetVisibleEntityIds(browser.VisibleEntityIdsForMap());
            var shouldFullRefresh = _lastState is null ||
                _lastState.Value.Plane != state.Plane ||
                filterChanged;
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
            var state = new BrowserState(ui.EntityBrowserStructureVersion, ui.EntityBrowserRowVersion);
            if (_lastState == state)
            {
                return;
            }

            var structureChanged = _lastState is null ||
                _lastState.Value.StructureVersion != state.StructureVersion;
            _lastState = state;
            if (structureChanged)
            {
                browser.Refresh(rebuildFilterOptions: true);
            }
            else
            {
                browser.RefreshDirtyRows(ui.DirtyEntityBrowserRows);
            }

            ui.ClearDirtyEntityBrowserRows();
        }
    }

    private sealed class ChronicleRender
    {
        private ChronicleState? _lastState;
        private long _lastTalesVersion = -1;

        public void Reset()
        {
            _lastState = null;
            _lastTalesVersion = -1;
        }

        public void Present(
            ChroniclePanel panel,
            IGameChronicleFeed chronicle,
            ChronicleAuraLayer aura)
        {
            var state = new ChronicleState(chronicle.Version + (chronicle.TalesVersion << 20), panel.FilterVersion);
            if (_lastState != state)
            {
                _lastState = state;
                panel.SetEntries(chronicle.Entries);
                panel.SetTaleViews(chronicle.TaleViews);
                Journal?.SetTaleViews(chronicle.TaleViews);
            }

            if (_lastTalesVersion != chronicle.TalesVersion)
            {
                _lastTalesVersion = chronicle.TalesVersion;
                aura.SetTales(chronicle.TaleViews.Select(static view => view.Tale));
            }
        }

        public ChronicleJournal? Journal { get; set; }
    }

    private sealed class TechnologyOverviewRender
    {
        private long _lastVersion = -1;

        public TechnologyOverview? Panel { get; set; }

        public void Reset() => _lastVersion = -1;

        public void Present(GameTechnologyOverview overview)
        {
            if (Panel is null || _lastVersion == overview.Version)
            {
                return;
            }

            _lastVersion = overview.Version;
            Panel.SetOverview(overview);
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
        long BrowserFilterVersion,
        bool FilterMapToBrowser,
        int SelectedGeneratorFilterId);

    private readonly record struct BrowserState(long StructureVersion, long RowVersion);

    private readonly record struct ChronicleState(long Version, long FilterVersion);

    private readonly record struct CardLayerState(long CardVersion, long EntityDetailVersion, long RelationshipVersion);

    private readonly record struct TableauState(bool Visible, long Version);

    private readonly record struct FloatingHudLayoutState(Vector2 MapPosition, Vector2 MapSize);
}
