#pragma warning disable CA1303
#pragma warning disable CA1305
#pragma warning disable CA2000 // Godot nodes managed by scene tree
#pragma warning disable CA2213 // Godot nodes are managed by the scene tree
#pragma warning disable CA1031 // The application shell must surface arbitrary background startup failures.
using System.Diagnostics;

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
    private const float MinimumCameraZoom = 0.07f;
    private const float MaximumCameraZoom = 5f;
#if DEBUG
    private const int DevelopmentWorldSeed = 4200;
#endif

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
    private StartupLoadingOverlay? _startupLoadingOverlay;
    private Godot.Theme? _theme;
    private Task<BackgroundStartupResult>? _startupTask;
    private Stopwatch? _startupStopwatch;
    private int _startupStage;
    private double _displayTicksPerSecond = NormalTicksPerSecond;
    private bool _displayPaused;
    private bool _moveLeft;
    private bool _moveRight;
    private bool _moveUp;
    private bool _moveDown;
    private int _startupPrewarmTicks;
    private int? _startupSeed;
    private double? _startupTickRate;
    private bool _startupUseWorldCache;
    private bool _startupRegenerateWorld;
    private bool _startupRandomSeedRequested;
    private bool _startupDisableWorldCache;

    public override void _ExitTree()
    {
        DisposePendingStartupWhenComplete();
        _runtime?.Dispose();
        _runtime = null;
        base._ExitTree();
    }

    private void DisposePendingStartupWhenComplete()
    {
        var startupTask = _startupTask;
        _startupTask = null;
        if (startupTask is null)
        {
            return;
        }

        _ = startupTask.ContinueWith(
            static task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    task.Result.Startup.Runtime.Dispose();
                }
                else
                {
                    _ = task.Exception;
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    public override void _Ready()
    {
        GD.Print("The Canonry Game - Initializing simulation...");

        CacheSceneNodes();
        ConfigureSceneNodes();
        WireSceneSignals();

        AssetLibrary.Load();
        RenderingServer.SetDefaultClearColor(UiTokens.Basalt);

        ReadStartupOptions();
        InitializeKernelHost();

        GD.Print("Controls: Space=pause, Right=step, 1/2/3=speed, R=reset, N=names, G=readings, J=journal, K=technology, WASD=pan, scroll=zoom");
    }

    public override void _Process(double delta)
    {
        PollStartup();
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
        if (HandlePanelShortcut(keycode))
        {
            return;
        }

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
        }
    }

    private bool HandlePanelShortcut(Key keycode)
    {
        switch (keycode)
        {
            case Key.G:
                ToggleAttributeGraph();
                return true;
            case Key.J:
                ToggleChronicleJournal();
                return true;
            case Key.K:
                ToggleTechnologyOverview();
                return true;
            default:
                return false;
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
        else if (mouse.ButtonIndex == MouseButton.Left)
        {
            _ = _renderTree?.TryApplyArmedInterventionClick();
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
        var zoom = (_camera!.Zoom * factor).Clamp(
            new Vector2(MinimumCameraZoom, MinimumCameraZoom),
            new Vector2(MaximumCameraZoom, MaximumCameraZoom));
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
        _startupLoadingOverlay = GetNode<StartupLoadingOverlay>("StartupLayer/StartupLoadingOverlay");
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
            _bottomBar.JournalToggleRequested += ToggleChronicleJournal;
            _bottomBar.TechnologyToggleRequested += ToggleTechnologyOverview;
        }

        if (_browser is not null)
        {
            _browser.EntitySelected += OnBrowserEntitySelected;
            _chroniclePanel!.EntitySelected += OnBrowserEntitySelected;
            _chroniclePanel!.JournalRequested += () => _renderTree?.ToggleChronicleJournal();
            _renderTree!.JournalEntitySelected += OnBrowserEntitySelected;
            _browser.EntityFilterChanged += OnBrowserEntityFilterChanged;
        }

        if (_mapArea is not null)
        {
            _mapArea.PlaneToggleRequested += OnTogglePlanePressed;
            _mapArea.MapLayerVisibilityChanged += OnMapLayerVisibilityChanged;
            _mapArea.MapLensRequested += OnMapLensRequested;
            _mapArea.GeographyViewRequested += OnGeographyViewRequested;
        }

        if (_entityLayer is not null)
        {
            _entityLayer.EntitySelected += OnMapEntitySelected;
        }
    }

    private void InitializeKernelHost()
    {
        var seedPolicy = _startupSeed is { } seed
            ? GameSeedPolicy.Requested(seed)
            : GameSeedPolicy.Random();
        var cacheOptions = _startupUseWorldCache && _startupSeed.HasValue
            ? new GameWorldCacheOptions(
                ProjectSettings.GlobalizePath("user://world-cache"),
                _startupRegenerateWorld)
            : null;
        StartNewRun(
            seedPolicy,
            pauseAfterStart: _startupPrewarmTicks > 0 && _startupTickRate is null,
            prewarmTicks: _startupPrewarmTicks,
            tickRateAfterStart: _startupTickRate,
            cacheOptions: cacheOptions);
    }

    private void StartNewRun(
        GameSeedPolicy seedPolicy,
        bool pauseAfterStart,
        int prewarmTicks = 0,
        double? tickRateAfterStart = null,
        GameWorldCacheOptions? cacheOptions = null)
    {
        _renderTree?.CloseOpenCards();
        _renderTree?.ClearTooltip();

        _runtime?.Dispose();
        _runtime = null;
        _displayPaused = true;
        Volatile.Write(ref _startupStage, (int)GameStartupStage.CheckingCachedWorld);
        _startupStopwatch = Stopwatch.StartNew();
        _startupLoadingOverlay?.ShowLoading(StartupStageText(GameStartupStage.CheckingCachedWorld));
        _startupTask = Task.Run(() => BuildRuntime(
            seedPolicy,
            prewarmTicks,
            pauseAfterStart,
            tickRateAfterStart,
            cacheOptions));
    }

    private BackgroundStartupResult BuildRuntime(
        GameSeedPolicy seedPolicy,
        int prewarmTicks,
        bool pauseAfterStart,
        double? tickRateAfterStart,
        GameWorldCacheOptions? cacheOptions)
    {
        var startup = CanonryGameRuntimeFactory.StartInteractive(
            seedPolicy,
            cacheOptions,
            stage => Volatile.Write(ref _startupStage, (int)stage));
        try
        {
            startup.Runtime.Controls.Pause();
            var prewarmElapsed = TimeSpan.Zero;
            if (prewarmTicks > 0)
            {
                Volatile.Write(ref _startupStage, (int)GameStartupStage.PrewarmingSimulation);
                prewarmElapsed = PrewarmRuntime(startup.Runtime, prewarmTicks);
            }

            return new BackgroundStartupResult(
                startup,
                prewarmElapsed,
                pauseAfterStart,
                tickRateAfterStart);
        }
        catch
        {
            startup.Runtime.Dispose();
            throw;
        }
    }

    private static TimeSpan PrewarmRuntime(CanonryGameRuntime runtime, int targetTick)
    {
        const int batchTicks = 25;
        var stopwatch = Stopwatch.StartNew();
        while (runtime.ReadModels.Entities.CurrentTick < targetTick)
        {
            var remaining = targetTick - runtime.ReadModels.Entities.CurrentTick;
            runtime.Controls.Step(Math.Min(batchTicks, remaining));
            _ = runtime.Update();
        }

        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    private void ReadStartupOptions()
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            ApplyStartupOption(argument);
        }

        if (_startupRandomSeedRequested)
        {
            _startupSeed = null;
        }
#if DEBUG
        else if (_startupSeed is null)
        {
            _startupSeed = DevelopmentWorldSeed;
        }
#endif

        _startupUseWorldCache = _startupSeed.HasValue && !_startupDisableWorldCache;
    }

    private void ApplyStartupOption(string argument)
    {
        if (TryReadPositiveOption(argument, "--prewarm-ticks=", out var prewarmTicks))
        {
            _startupPrewarmTicks = prewarmTicks;
        }
        else if (TryReadPositiveOption(argument, "--seed=", out var seed))
        {
            _startupSeed = seed;
        }
        else if (TryReadPositiveRateOption(argument, "--prewarm-tick-rate=", out var tickRate))
        {
            _startupTickRate = tickRate;
        }
        else
        {
            ApplyStartupFlag(argument);
        }
    }

    private void ApplyStartupFlag(string argument)
    {
        switch (argument)
        {
            case "--random-seed":
                _startupRandomSeedRequested = true;
                break;
            case "--no-world-cache":
                _startupDisableWorldCache = true;
                break;
            case "--regenerate-world":
                _startupRegenerateWorld = true;
                break;
        }
    }

    private static bool TryReadPositiveOption(string argument, string prefix, out int value)
    {
        value = 0;
        return argument.StartsWith(prefix, StringComparison.Ordinal) &&
            int.TryParse(
                argument.AsSpan(prefix.Length),
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out value) &&
            value > 0;
    }

    private static bool TryReadPositiveRateOption(string argument, string prefix, out double value)
    {
        value = 0d;
        return argument.StartsWith(prefix, StringComparison.Ordinal) &&
            double.TryParse(
                argument.AsSpan(prefix.Length),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value) &&
            double.IsFinite(value) &&
            value > 0d;
    }

    private void OnTogglePlanePressed() => _renderTree?.TogglePlane();

    private void OnMapLayerVisibilityChanged(string layerKey, bool visible) =>
        _renderTree?.SetMapLayerVisible(layerKey, visible);

    private void OnMapLensRequested(string lensKey) =>
        _renderTree?.SetMapLens(lensKey);

    private void OnGeographyViewRequested(string viewKey) =>
        _renderTree?.SetGeographyView(viewKey);

    private void OnBrowserEntityFilterChanged() => _renderTree?.InvalidateEntityMap();

    private void ToggleAttributeGraph() => _renderTree?.ToggleAttributeGraph();

    private void ToggleChronicleJournal() => _renderTree?.ToggleChronicleJournal();

    private void ToggleTechnologyOverview() => _renderTree?.ToggleTechnologyOverview();

    private void ResetSimulation()
    {
        StartNewRun(GameSeedPolicy.Random(), pauseAfterStart: true);
        GD.Print("Generating a new random simulation world.");
    }

    private void PollStartup()
    {
        var task = _startupTask;
        if (task is null)
        {
            return;
        }

        var elapsed = _startupStopwatch?.Elapsed.TotalSeconds ?? 0d;
        var stage = (GameStartupStage)Volatile.Read(ref _startupStage);
        _startupLoadingOverlay?.UpdateStatus(StartupStageText(stage), elapsed);
        if (!task.IsCompleted)
        {
            return;
        }

        _startupTask = null;
        try
        {
            CompleteStartup(task.GetAwaiter().GetResult());
        }
        catch (Exception exception)
        {
            _startupStopwatch?.Stop();
            _startupLoadingOverlay?.ShowFailure(exception.Message, elapsed);
            GD.PushError($"Simulation startup failed: {exception}");
        }
    }

    private void CompleteStartup(BackgroundStartupResult background)
    {
        BindStartupRuntime(background.Startup.Runtime);
        ApplyStartupTimeControl(background);
        FinishStartupPresentation(background, background.Startup.Runtime);
    }

    private void BindStartupRuntime(CanonryGameRuntime runtime)
    {
        _runtime = runtime;
        try
        {
            _renderTree?.BindRuntime(_runtime);
            _renderTree?.Present(_runtime.ReadModels.Entities.CurrentTick, FormatKernelTimeState());
        }
        catch
        {
            _runtime.Dispose();
            _runtime = null;
            throw;
        }
    }

    private void ApplyStartupTimeControl(BackgroundStartupResult background)
    {
        if (background.TickRateAfterStart is { } tickRate)
        {
            SetKernelTickRate(tickRate);
        }
        else if (background.PauseAfterStart)
        {
            _displayPaused = true;
        }
        else
        {
            SetKernelTickRate(NormalTicksPerSecond);
        }
    }

    private void FinishStartupPresentation(
        BackgroundStartupResult background,
        CanonryGameRuntime runtime)
    {
        _startupStopwatch?.Stop();
        _startupLoadingOverlay?.HideLoading();
        var elapsed = _startupStopwatch?.Elapsed.TotalSeconds ?? 0d;
        var cacheText = CacheStatusText(background.Startup);
        var runLogText = runtime.RunLogPath is { Length: > 0 } runLogPath
            ? $" Run log -> {runLogPath}"
            : " Run logging disabled.";
        GD.Print(
            $"Simulation initialized in {elapsed:0.00}s: seed {runtime.Seed.BaseSeed}/{runtime.Seed.Attempt}, " +
            $"{runtime.ReadModels.Entities.Entities.Count} projected entities.{cacheText}{runLogText}");
        if (background.PrewarmElapsed > TimeSpan.Zero)
        {
            GD.Print(
                $"Prewarmed simulation to tick {runtime.ReadModels.Entities.CurrentTick} " +
                $"in {background.PrewarmElapsed.TotalMilliseconds:0} ms for UI profiling.");
        }

        if (background.Startup.Warning is { Length: > 0 } warning)
        {
            GD.PushWarning(warning);
        }
    }

    private static string CacheStatusText(GameStartupResult startup)
    {
        if (startup.CachePath is null)
        {
            return " World cache disabled.";
        }

        return startup.CacheHit
            ? $" Loaded cached world from {startup.CachePath}."
            : $" Generated and cached world at {startup.CachePath}.";
    }

    private static string StartupStageText(GameStartupStage stage) => stage switch
    {
        GameStartupStage.CheckingCachedWorld => "Checking the remembered world",
        GameStartupStage.LoadingCachedWorld => "Restoring the remembered world",
        GameStartupStage.GeneratingWorld => "Shaping terrain and waters",
        GameStartupStage.SavingCachedWorld => "Remembering this world for next time",
        GameStartupStage.StartingSimulation => "Populating the living world",
        GameStartupStage.PrewarmingSimulation => "Advancing the simulation",
        _ => "Preparing the world",
    };

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
        var clampedZoom = new Vector2(zoom, zoom).Clamp(
            new Vector2(MinimumCameraZoom, MinimumCameraZoom),
            new Vector2(MaximumCameraZoom, MaximumCameraZoom));
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
            _entityLayer?.NotifyViewportChanged();
        }
    }

    private sealed record BackgroundStartupResult(
        GameStartupResult Startup,
        TimeSpan PrewarmElapsed,
        bool PauseAfterStart,
        double? TickRateAfterStart);

}
