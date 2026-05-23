#pragma warning disable CA1303
#pragma warning disable CA1305
#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA2213 // Godot nodes are managed by the scene tree
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
    private TableauPanel? _tableauPanel;
    private HexTooltipPanel? _hexTooltipPanel;
    private GameRenderTree? _renderTree;
    private Godot.Theme? _theme;
    private double _displayTicksPerSecond = NormalTicksPerSecond;
    private bool _displayPaused;
    private bool _moveLeft;
    private bool _moveRight;
    private bool _moveUp;
    private bool _moveDown;

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

        _renderTree?.Present(currentTick, FormatKernelTimeState());
        HandleCameraMovement((float)delta);
        _renderTree?.PresentTooltip(delta);
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
        if (@event is not InputEventKey key)
        {
            return;
        }

        TrackCameraMovementKey(key);
        if (!key.Pressed)
        {
            return;
        }

        HandlePressedShortcut(key.Keycode);
    }

    private void HandlePressedShortcut(Key keycode)
    {
        switch (keycode)
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

    private void TrackCameraMovementKey(InputEventKey key)
    {
        switch (key.Keycode)
        {
            case Key.A:
                _moveLeft = key.Pressed;
                break;
            case Key.D:
                _moveRight = key.Pressed;
                break;
            case Key.W:
                _moveUp = key.Pressed;
                break;
            case Key.S:
                _moveDown = key.Pressed;
                break;
        }
    }

    private void HandleMouseInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            _renderTree?.NotifyPointerMoved();
            return;
        }

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

    private void ToggleEntityNames() => _renderTree?.ToggleEntityNames();

    private void AdjustZoom(float factor)
    {
        var zoom = (_camera!.Zoom * factor).Clamp(new Vector2(0.3f, 0.3f), new Vector2(5f, 5f));
        _camera.Zoom = zoom;
        _renderTree?.SetCameraZoom(zoom);
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

        _renderTree = new GameRenderTree(
            _mapRenderer!,
            _entityLayer!,
            _browser!,
            _topBar!,
            _bottomBar!,
            _chroniclePanel!,
            _mapArea!,
            _rootColumn!,
            _cardLayer!,
            _uiRoot!,
            _tableauPanel!,
            _hexTooltipPanel!,
            this,
            _camera!.Zoom,
            _theme);
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
            _browser.EntityFilterChanged += OnBrowserEntityFilterChanged;
        }

        if (_mapArea is not null)
        {
            _mapArea.PlaneToggleRequested += OnTogglePlanePressed;
            _mapArea.MapLayerVisibilityChanged += OnMapLayerVisibilityChanged;
            _mapArea.MapLensRequested += OnMapLensRequested;
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
        _renderTree?.CloseOpenCards();
        _renderTree?.ClearTooltip();

        _runtime?.Dispose();
        _runtime = CanonryGameRuntimeFactory.Start(seedPolicy);

        if (pauseAfterStart)
        {
            PauseKernelDriver();
        }
        else
        {
            SetKernelTickRate(NormalTicksPerSecond);
        }

        _renderTree?.BindRuntime(_runtime);
        _renderTree?.Present(_runtime.ReadModels.Entities.CurrentTick, FormatKernelTimeState());

        var runLogText = _runtime.RunLogPath is { Length: > 0 } runLogPath
            ? $" Run log -> {runLogPath}"
            : " Run logging disabled.";
        GD.Print(
            $"Simulation initialized: seed {_runtime.Seed.BaseSeed}/{_runtime.Seed.Attempt}, " +
            $"{_runtime.ReadModels.Entities.Entities.Count} projected entities.{runLogText}");
    }

    private void OnTogglePlanePressed() => _renderTree?.TogglePlane();

    private void OnMapLayerVisibilityChanged(string layerKey, bool visible) =>
        _renderTree?.SetMapLayerVisible(layerKey, visible);

    private void OnMapLensRequested(string lensKey) =>
        _renderTree?.SetMapLens(lensKey);

    private void OnBrowserEntityFilterChanged() => _renderTree?.InvalidateEntityMap();

    private void ToggleAttributeGraph() => _renderTree?.ToggleAttributeGraph();

    private void ResetSimulation()
    {
        StartNewRun(GameSeedPolicy.Random(), pauseAfterStart: true);
        GD.Print("Simulation reset with a new random seed.");
    }

    private void OnBrowserEntitySelected(int entityIdValue)
    {
        FocusEntityOnMap(entityIdValue);
        _renderTree?.OpenCard(entityIdValue, new Vector2(300, 100));
    }

    private void OnMapEntitySelected(int entityIdValue)
    {
        _entityLayer?.SetHighlightedEntity(entityIdValue);
        _renderTree?.OpenCard(entityIdValue, GetViewport().GetMousePosition() + new Vector2(20, -20));
    }

    private void FocusEntityOnMap(int entityIdValue)
    {
        if (_entityLayer is null || _camera is null)
        {
            return;
        }

        _ = _entityLayer.SetHighlightedEntity(entityIdValue);
        if (!_entityLayer.TryGetEntityWorldPosition(entityIdValue, out var worldPosition))
        {
            return;
        }

        _camera.Position = worldPosition;
        var zoom = MathF.Max(_camera.Zoom.X, 2.25f);
        var clampedZoom = new Vector2(zoom, zoom).Clamp(new Vector2(0.3f, 0.3f), new Vector2(5f, 5f));
        _camera.Zoom = clampedZoom;
        _renderTree?.SetCameraZoom(clampedZoom);
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

    private void ToggleTableauVisibility() => _renderTree?.ToggleTableauVisibility();

    private void HandleCameraMovement(float delta)
    {
        if (_camera is null)
        {
            return;
        }

        var direction = new Vector2(
            (_moveRight ? 1f : 0f) - (_moveLeft ? 1f : 0f),
            (_moveDown ? 1f : 0f) - (_moveUp ? 1f : 0f));

        if (direction != Vector2.Zero)
        {
            var speed = 200f / _camera.Zoom.X;
            _camera.Position += direction.Normalized() * speed * delta;
        }
    }

}
