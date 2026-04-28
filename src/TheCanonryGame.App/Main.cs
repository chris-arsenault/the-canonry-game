#pragma warning disable CA1303
#pragma warning disable CA1305
#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA2213 // Godot nodes are managed by the scene tree
using Engine.Projection;

using Godot;

using TheCanonryGame.Game;
using TheCanonryGame.UI;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.App;

public partial class Main : Node2D
{
    private const double NormalTicksPerSecond = 1d;
    private const double FastTicksPerSecond = 5d;
    private const double VeryFastTicksPerSecond = 10d;

    private CanonryGameRuntime? _runtime;
    private HexMapRenderer? _mapRenderer;
    private EntityLayer? _entityLayer;
    private Camera2D? _camera;
    private EntityBrowser? _browser;
    private TopBar? _topBar;
    private BottomBar? _bottomBar;
    private ChroniclePanel? _chroniclePanel;
    private MapArea? _mapArea;
    private VBoxContainer? _rootColumn;
    private EntityCardLayer? _cardLayer;
    private Control? _uiRoot;
    private AttributeGraphPanel? _attributeGraph;
    private TableauPanel? _tableauPanel;
    private UiProjectionState? _uiProjection;
    private HexMapProjection? _hexMapProjection;
    private IProjectionSymbolCatalog? _projectionSymbols;
    private UiChronicleProjection? _chronicleProjection;
    private HexTooltipPanel? _hexTooltipPanel;
    private Godot.Theme? _theme;
    private int _currentSeed;
    private int _currentSeedAttempt;
    private double _displayTicksPerSecond = NormalTicksPerSecond;
    private bool _displayPaused;

    public override void _ExitTree()
    {
        _runtime?.Dispose();
        _runtime = null;
        base._ExitTree();
    }

    public override void _Ready()
    {
        GD.Print("The Canonry Game - Initializing simulation...");

        CacheSceneNodes();
        ConfigureSceneNodes();
        WireSceneSignals();

        AssetLibrary.Load();
        RenderingServer.SetDefaultClearColor(UiTokens.Basalt);

        InitializeKernelHost();

        GD.Print("Controls: Space=pause, Right=step, 1/2/3=speed, R=reset, N=names, G=readings, WASD=pan, scroll=zoom");
    }

    public override void _Process(double delta)
    {
        if (_runtime is null)
        {
            return;
        }

        var pulse = _runtime.Update();
        var currentTick = pulse.CurrentTick;

        if (pulse.TicksProcessed > 0)
        {
            _mapRenderer?.Refresh();
            _entityLayer?.Refresh();
            _browser?.Refresh();
            _cardLayer?.Refresh();
            _attributeGraph?.Refresh();
            _tableauPanel?.Refresh();
        }

        RefreshChronicleLabel();
        RefreshTopBar(currentTick);
        PositionFloatingHud();
        HandleCameraMovement((float)delta);
        _hexTooltipPanel?.UpdateTooltip();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_runtime is null)
        {
            return;
        }

        HandleKeyboardInput(@event);
        HandleMouseInput(@event);
    }

    private void HandleKeyboardInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true } key)
        {
            return;
        }

        switch (key.Keycode)
        {
            case Key.Space:
                TogglePause();
                break;
            case Key.Key1:
                SetKernelTickRate(NormalTicksPerSecond);
                break;
            case Key.Key2:
                SetKernelTickRate(FastTicksPerSecond);
                break;
            case Key.Key3:
                SetKernelTickRate(VeryFastTicksPerSecond);
                break;
            case Key.Right:
                StepKernelDriver();
                break;
            case Key.R:
                ResetSimulation();
                break;
            case Key.N:
                ToggleEntityNames();
                break;
            case Key.G:
                ToggleAttributeGraph();
                break;
        }
    }

    private void HandleMouseInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true } mouse || _camera is null)
        {
            return;
        }

        const float zoomFactor = 1.1f;
        if (mouse.ButtonIndex == MouseButton.WheelUp)
        {
            AdjustZoom(zoomFactor);
        }
        else if (mouse.ButtonIndex == MouseButton.WheelDown)
        {
            AdjustZoom(1f / zoomFactor);
        }
    }

    private void TogglePause()
    {
        if (_displayPaused)
        {
            SetKernelTickRate(NormalTicksPerSecond);
            return;
        }

        PauseKernelDriver();
    }

    private void ToggleEntityNames()
    {
        if (_entityLayer is not null)
        {
            _entityLayer.SetShowEntityNames(!_entityLayer.ShowEntityNames);
        }
    }

    private void AdjustZoom(float factor)
    {
        _camera!.Zoom *= factor;
        _camera.Zoom = _camera.Zoom.Clamp(new Vector2(0.3f, 0.3f), new Vector2(5f, 5f));
    }

    private void CacheSceneNodes()
    {
        _mapRenderer = GetNode<HexMapRenderer>("MapRenderer");
        _entityLayer = GetNode<EntityLayer>("EntityLayer");
        _camera = GetNode<Camera2D>("Camera");
        _uiRoot = GetNode<Control>("UiLayer/UiRoot");
        _rootColumn = GetNode<VBoxContainer>("UiLayer/UiRoot/RootColumn");
        _topBar = GetNode<TopBar>("UiLayer/UiRoot/RootColumn/TopBar");
        _browser = GetNode<EntityBrowser>("UiLayer/UiRoot/RootColumn/MiddleRow/EntityBrowser");
        _mapArea = GetNode<MapArea>("UiLayer/UiRoot/RootColumn/MiddleRow/MapArea");
        _chroniclePanel = GetNode<ChroniclePanel>("UiLayer/UiRoot/RootColumn/MiddleRow/ChroniclePanel");
        _bottomBar = GetNode<BottomBar>("UiLayer/UiRoot/RootColumn/BottomBar");
        _tableauPanel = GetNode<TableauPanel>("UiLayer/UiRoot/TableauPanel");
        _hexTooltipPanel = GetNode<HexTooltipPanel>("UiLayer/UiRoot/HexTooltipPanel");
        _cardLayer = GetNode<EntityCardLayer>("CardLayer");
    }

    private void ConfigureSceneNodes()
    {
        _camera?.MakeCurrent();

        _theme = UiTheme.Build();
        if (_uiRoot is not null)
        {
            _uiRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _uiRoot.MouseFilter = Control.MouseFilterEnum.Ignore;
            _uiRoot.Theme = _theme;
        }

        if (_rootColumn is not null)
        {
            _rootColumn.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _rootColumn.MouseFilter = Control.MouseFilterEnum.Ignore;
            _rootColumn.AddThemeConstantOverride("separation", 0);
        }

    }

    private void WireSceneSignals()
    {
        if (_topBar is not null)
        {
            _topBar.PauseRequested += PauseKernelDriver;
            _topBar.StepRequested += StepKernelDriver;
            _topBar.SpeedRequested += SetKernelTickRate;
            _topBar.ResetRequested += ResetSimulation;
        }

        if (_bottomBar is not null)
        {
            _bottomBar.TableauToggleRequested += ToggleTableauVisibility;
            _bottomBar.ReadingsToggleRequested += ToggleAttributeGraph;
        }

        if (_browser is not null)
        {
            _browser.EntitySelected += OnBrowserEntitySelected;
        }

        if (_mapArea is not null)
        {
            _mapArea.PlaneToggleRequested += OnTogglePlanePressed;
        }

        if (_entityLayer is not null)
        {
            _entityLayer.EntitySelected += OnMapEntitySelected;
        }
    }

    private void InitializeKernelHost() =>
        StartNewRun(GameSeedPolicy.Random(), pauseAfterStart: false);

    private void StartNewRun(GameSeedPolicy seedPolicy, bool pauseAfterStart)
    {
        CloseOpenCards();
        _hexTooltipPanel?.ClearTooltip();

        _runtime?.Dispose();
        _runtime = CanonryGameRuntimeFactory.Start(seedPolicy);

        _hexMapProjection = _runtime.ReadModels.Map;
        _uiProjection = _runtime.ReadModels.Entities;
        _projectionSymbols = _runtime.ReadModels.Symbols;
        _chronicleProjection = _runtime.ReadModels.Chronicle;
        _currentSeed = _runtime.Seed.BaseSeed;
        _currentSeedAttempt = _runtime.Seed.Attempt;

        if (pauseAfterStart)
        {
            PauseKernelDriver();
        }
        else
        {
            SetKernelTickRate(NormalTicksPerSecond);
        }

        RebindRuntimeToUi();
        RefreshChronicleLabel();
        RefreshTopBar(_runtime.ReadModels.Entities.CurrentTick);

        GD.Print(
            $"Simulation initialized: seed {_runtime.Seed.BaseSeed}/{_runtime.Seed.Attempt}, " +
            $"{_runtime.ReadModels.Entities.Entities.Count} projected entities. Run log -> {_runtime.RunLogPath}");
    }

    private void RebindRuntimeToUi()
    {
        RebindMapRuntime();
        RebindEntityRuntime();
        RebindOverlayRuntime();
        _chroniclePanel?.Clear();
    }

    private void RebindMapRuntime()
    {
        if (_hexMapProjection is not null)
        {
            _mapRenderer?.Initialize(_hexMapProjection, _projectionSymbols);
            _mapRenderer?.Refresh();
        }

        _entityLayer?.Initialize(_uiProjection, _projectionSymbols, _mapRenderer?.HexSize ?? 30f);
        if (_mapRenderer is not null)
        {
            _entityLayer?.SetPlane(_mapRenderer.CurrentPlane);
            _mapArea?.SetPlane(_mapRenderer.CurrentPlane);
        }
    }

    private void RebindEntityRuntime()
    {
        _browser?.Initialize(_uiProjection, _projectionSymbols);
        _attributeGraph?.Initialize(_uiProjection, _projectionSymbols);
        _cardLayer?.Initialize(_uiProjection, _projectionSymbols, _theme);
    }

    private void RebindOverlayRuntime()
    {
        _hexTooltipPanel?.Initialize(
            _hexMapProjection,
            _uiProjection,
            _projectionSymbols,
            _mapRenderer,
            this,
            [_rootColumn, _mapArea, _uiRoot]);
        _tableauPanel?.Initialize(_runtime?.Tableau);
        _tableauPanel?.Refresh();
    }

    private void RefreshChronicleLabel()
    {
        if (_chronicleProjection is not null)
        {
            _chroniclePanel?.SetLines(_chronicleProjection.Lines);
        }
    }

    private void RefreshTopBar(int currentTick)
    {
        _topBar?.SetStatus(
            currentTick,
            FormatKernelTimeState(),
            _uiProjection?.Entities.Count ?? 0,
            _uiProjection?.GeneratorFires.Count ?? 0,
            _currentSeed,
            _currentSeedAttempt);
    }

    private void CloseOpenCards()
    {
        _cardLayer?.CloseAll();
    }

    private void OnTogglePlanePressed()
    {
        if (_mapRenderer is null)
        {
            return;
        }

        _mapRenderer.CurrentPlane = _mapRenderer.CurrentPlane == PlaneMode.Physical
            ? PlaneMode.Semantic
            : PlaneMode.Physical;
        _mapRenderer.Refresh();
        _entityLayer?.SetPlane(_mapRenderer.CurrentPlane);
        _mapArea?.SetPlane(_mapRenderer.CurrentPlane);
        _hexTooltipPanel?.ClearTooltip();
    }

    private void ToggleAttributeGraph()
    {
        if (_attributeGraph is not null)
        {
            _attributeGraph.QueueFree();
            _attributeGraph = null;
            _bottomBar?.SetReadingsVisible(false);
            return;
        }

        if (_uiProjection is null || _cardLayer is null)
        {
            return;
        }

        _attributeGraph = new AttributeGraphPanel
        {
            Theme = _theme,
        };
        _cardLayer.AddChild(_attributeGraph);
        _attributeGraph.Initialize(_uiProjection, _projectionSymbols);
        _attributeGraph.Position = new Vector2(320, UiTokens.Space7);
        _bottomBar?.SetReadingsVisible(true);
    }

    private void ResetSimulation()
    {
        StartNewRun(GameSeedPolicy.Random(), pauseAfterStart: true);
        GD.Print("Simulation reset with a new random seed.");
    }

    private void OnBrowserEntitySelected(int entityIdValue)
    {
        _cardLayer?.Open(entityIdValue, new Vector2(300, 100));
    }

    private void OnMapEntitySelected(int entityIdValue)
    {
        _cardLayer?.Open(entityIdValue, GetViewport().GetMousePosition() + new Vector2(20, -20));
    }

    private void PauseKernelDriver()
    {
        _runtime?.Controls.Pause();
        _displayPaused = true;
    }

    private void StepKernelDriver()
    {
        _runtime?.Controls.Step(1);
        _displayPaused = true;
    }

    private void SetKernelTickRate(double ticksPerSecond)
    {
        _runtime?.Controls.SetTickRate(ticksPerSecond);
        _displayPaused = ticksPerSecond <= 0d;
        if (ticksPerSecond > 0d)
        {
            _displayTicksPerSecond = ticksPerSecond;
        }
    }

    private string FormatKernelTimeState()
    {
        if (_displayPaused)
        {
            return "paused";
        }

        return $"{_displayTicksPerSecond:0.##} t/s";
    }

    private void ToggleTableauVisibility()
    {
        if (_tableauPanel is null)
        {
            return;
        }

        _tableauPanel.Visible = !_tableauPanel.Visible;
        _bottomBar?.SetTableauVisible(_tableauPanel.Visible);
        if (_tableauPanel.Visible)
        {
            _tableauPanel.Refresh();
        }
    }

    private void PositionFloatingHud()
    {
        if (_mapArea is null || _tableauPanel is null)
        {
            return;
        }

        var mapRect = _mapArea.GetGlobalRect();
        var padding = UiTokens.Space4 * 2f;
        var width = Mathf.Min(Mathf.Max(720f, mapRect.Size.X - padding), 1080f);
        var height = Mathf.Min(Mathf.Max(520f, mapRect.Size.Y - padding), 780f);
        _tableauPanel.CustomMinimumSize = new Vector2(width, height);
        _tableauPanel.Position = new Vector2(
            mapRect.Position.X + (mapRect.Size.X - width) * 0.5f,
            mapRect.Position.Y + (mapRect.Size.Y - height) * 0.5f);
    }

    private void HandleCameraMovement(float delta)
    {
        if (_camera is null)
        {
            return;
        }

        var speed = 200f / _camera.Zoom.X;
        var direction = Vector2.Zero;

        if (Input.IsKeyPressed(Key.A)) direction.X -= 1;
        if (Input.IsKeyPressed(Key.D)) direction.X += 1;
        if (Input.IsKeyPressed(Key.W)) direction.Y -= 1;
        if (Input.IsKeyPressed(Key.S)) direction.Y += 1;

        if (direction != Vector2.Zero)
        {
            _camera.Position += direction.Normalized() * speed * delta;
        }
    }

}
