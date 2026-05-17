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
    private int _seed;
    private int _seedAttempt;

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
        _mapArea.Resized += _floatingHudLayoutRender.Invalidate;
        _tableauPanel.VisibilityChanged += _floatingHudLayoutRender.Invalidate;
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

    public void ClearTooltip() => _hexTooltipPanel.ClearTooltip();

    public void Present(int currentTick, string timeState)
    {
        if (_runtime is null || _ui is null || _map is null || _chronicle is null)
        {
            return;
        }

        _chronicle.Refresh();
        _topBarRender.Present(_topBar, _ui, currentTick, timeState, _seed, _seedAttempt);
        _mapRender.Present(_mapRenderer, _map);
        _mapArea.BindCultureOverview(_map, _ui, _symbols, _mapRenderer.CurrentPlane);
        _browserRender.Present(_browser, _ui);
        _entityMapRender.Present(_entityLayer, _ui, _browser);
        _cardLayerRender.Present(_cardLayer, _ui);
        _attributeGraphRender.Present(ActiveAttributeGraph(), _ui);
        _chronicleRender.Present(_chroniclePanel, _chronicle);
        _tableauRender.Present(_tableauPanel, _ui);
        _floatingHudLayoutRender.Present(_mapArea, _tableauPanel);
    }

    public void PresentTooltip() => _hexTooltipPanel.UpdateTooltip();

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

    public void SetMapLayerVisible(string layerKey, bool visible)
    {
        switch (layerKey)
        {
            case MapArea.LayerRoads:
                if (_mapRenderer.ShowRoads == visible)
                {
                    return;
                }

                _mapRenderer.ShowRoads = visible;
                _mapArea.SetLayerVisible(layerKey, visible);
                _mapRender.InvalidateCells();
                break;
            case MapArea.LayerMapFeatures:
                if (_mapRenderer.ShowMapFeatures == visible)
                {
                    return;
                }

                _mapRenderer.ShowMapFeatures = visible;
                _mapArea.SetLayerVisible(layerKey, visible);
                _mapRender.Invalidate();
                break;
            case MapArea.LayerInfluence:
                if (_mapRenderer.ShowInfluence == visible)
                {
                    return;
                }

                _mapRenderer.ShowInfluence = visible;
                _mapArea.SetLayerVisible(layerKey, visible);
                _mapRender.InvalidateCells();
                break;
            case MapArea.LayerCultureInfluence:
                if (_mapRenderer.ShowCultureInfluence == visible)
                {
                    return;
                }

                _mapRenderer.ShowCultureInfluence = visible;
                _mapArea.SetLayerVisible(layerKey, visible);
                _mapRender.InvalidateCells();
                break;
        }
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
        _tableauPanel.Visible = !_tableauPanel.Visible;
        _bottomBar.SetTableauVisible(_tableauPanel.Visible);
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
        _mapRenderer.Initialize(_map!, _symbols);
        _entityLayer.Initialize(_ui, _symbols, _mapRenderer.HexSize);
        _ = _entityLayer.SetPlane(_mapRenderer.CurrentPlane);
        _browser.Initialize(_ui, _symbols);
        _attributeGraph?.Initialize(_ui, _symbols);
        _cardLayer.Initialize(_ui, _symbols, _theme);
        _chroniclePanel.Clear();
        _hexTooltipPanel.Initialize(_map, _ui, _symbols, _mapRenderer, _worldCanvas, [_rootColumn, _mapArea, _uiRoot]);
        _tableauPanel.Initialize(_runtime?.Tableau);
        _mapArea.SetPlane(_mapRenderer.CurrentPlane);
        _mapArea.SetLayerVisible(MapArea.LayerRoads, _mapRenderer.ShowRoads);
        _mapArea.SetLayerVisible(MapArea.LayerMapFeatures, _mapRenderer.ShowMapFeatures);
        _mapArea.SetLayerVisible(MapArea.LayerCultureInfluence, _mapRenderer.ShowCultureInfluence);
        _mapArea.SetLayerVisible(MapArea.LayerInfluence, _mapRenderer.ShowInfluence);
        _mapArea.BindCultureOverview(_map, _ui, _symbols, _mapRenderer.CurrentPlane);
        _bottomBar.SetTableauVisible(_tableauPanel.Visible);
        _bottomBar.SetReadingsVisible(_attributeGraph is not null);
    }

    private void ResetPresenters()
    {
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

        public void InvalidateCells() => _lastCells = null;

        public void Present(HexMapRenderer renderer, HexMapProjection map)
        {
            var state = MapRenderState.From(renderer, map);
            var cellState = state with { FrameVersion = 0 };
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

            if (_lastFrame != state)
            {
                renderer.RefreshFrame();
                _lastFrame = state;
            }
        }

        private static bool CellLayoutChanged(MapRenderState previous, MapRenderState current) =>
            previous.Plane != current.Plane ||
            previous.ShowTerritory != current.ShowTerritory ||
            previous.ShowCultureInfluence != current.ShowCultureInfluence ||
            previous.ShowInfluence != current.ShowInfluence ||
            previous.ShowRoads != current.ShowRoads ||
            previous.ShowMapFeatures != current.ShowMapFeatures ||
            previous.HexSize != current.HexSize;
    }

    private sealed class EntityMapRender
    {
        private EntityMapState? _lastState;

        public void Reset() => _lastState = null;

        public void Invalidate() => Reset();

        public void Present(EntityLayer layer, UiProjectionState ui, EntityBrowser browser)
        {
            var state = new EntityMapState(
                ui.EntityMapVersion,
                layer.CurrentPlane,
                layer.ShowEntityNames,
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
        private long _lastVersion = -1;

        public void Reset() => _lastVersion = -1;

        public void Present(ChroniclePanel panel, IGameChronicleFeed chronicle)
        {
            if (_lastVersion == chronicle.Version)
            {
                return;
            }

            _lastVersion = chronicle.Version;
            panel.SetLines(chronicle.Lines);
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

        public void Present(TableauPanel panel, UiProjectionState ui)
        {
            var state = new TableauState(panel.Visible, ui.TableauVersion);
            if (_lastState == state)
            {
                return;
            }

            _lastState = state;
            if (panel.Visible)
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

        public void Present(MapArea mapArea, TableauPanel tableauPanel)
        {
            if (!tableauPanel.Visible)
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
        PlaneMode Plane,
        bool ShowTerritory,
        bool ShowCultureInfluence,
        bool ShowInfluence,
        bool ShowRoads,
        bool ShowMapFeatures,
        bool ShowRegionBorders,
        float HexSize)
    {
        public static MapRenderState From(HexMapRenderer renderer, HexMapProjection map) =>
            new(
                map.CellVersion,
                map.FrameVersion,
                renderer.CurrentPlane,
                renderer.ShowTerritory,
                renderer.ShowCultureInfluence,
                renderer.ShowInfluence,
                renderer.ShowRoads,
                renderer.ShowMapFeatures,
                renderer.ShowRegionBorders,
                renderer.HexSize);
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

    private readonly record struct CardLayerState(long CardVersion, long EntityDetailVersion, long RelationshipVersion);

    private readonly record struct TableauState(bool Visible, long Version);

    private readonly record struct FloatingHudLayoutState(Vector2 MapPosition, Vector2 MapSize);
}
