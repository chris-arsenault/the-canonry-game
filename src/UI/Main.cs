#pragma warning disable CA2000 // Godot nodes managed by scene tree
using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

using Engine.Host;
using Engine.Kernel.Driver;
using Engine.Observability;
using Engine.Projection;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

#pragma warning disable CA2213 // Godot nodes are managed by the scene tree
public partial class Main : Node2D
{
    private const ulong HexTooltipDelayMs = 500;
    private const double NormalTicksPerSecond = 1d;
    private const double FastTicksPerSecond = 5d;
    private const double VeryFastTicksPerSecond = 10d;

    private KernelHost? _host;
    private IKernelDriverTimeControl? _kernelTime;
    private ICommandSubmitter? _commandSubmitter;
    private HexMapRenderer? _mapRenderer;
    private Camera2D? _camera;
    private EntityBrowser? _browser;
    private VBoxContainer? _rootColumn;
    private Control? _mapArea;
    private CanvasLayer? _uiLayer;
    private CanvasLayer? _cardLayer;
    private Control? _uiRoot;
    private RichTextLabel? _eventLogLabel;
    private AttributeGraphPanel? _attributeGraph;
    private Label? _tickLabel;
    private Button? _planeToggleButton;
    private Button? _tableauToggleButton;
    private Button? _readingsToggleButton;
    private TableauPanel? _tableauPanel;
    private RunLogger? _runLogger;
    private UiProjectionState? _uiProjection;
    private HexMapProjection? _hexMapProjection;
    private IProjectionSymbolCatalog? _projectionSymbols;
    private PanelContainer? _hexTooltipPanel;
    private Label? _hexTooltipLabel;
    private PanelContainer? _legendPanel;
    private Godot.Theme? _theme;
    private int _lastObservedKernelTick;
    private double _displayTicksPerSecond = NormalTicksPerSecond;
    private bool _displayPaused;
    private HexCoord? _hoveredHex;
    private Vector2 _hoverScreenPosition;
    private ulong _hoverStartedAtMs;
    private readonly Dictionary<int, EntityCard> _openCards = [];

    public override void _ExitTree()
    {
        _host?.Dispose();
        _host = null;
        _runLogger?.Dispose();
        _runLogger = null;
        base._ExitTree();
    }

    public override void _Ready()
    {
        GD.Print("The Canonry Game - Initializing simulation...");

        // Illustration assets — load synchronously before any UI is built, so
        // HexMapRenderer / EntityBrowser / Legend can read the sprite registry
        // on their first paint instead of starting with polygon fallbacks that
        // flicker to textures one tick later.
        AssetLibrary.Load();

        // MIGRATION-BLOCKER: Main must be wired to the real startup entrypoint
        // that creates KernelHost, SymbolCatalog, UiProjectionState, and
        // HexMapProjection. Do not seed UI-local startup data here; that
        // would be a compatibility facade and would hide startup debt.
        InitializeKernelHost();

        var logPath = RunLogger.DefaultLogPath();
        _runLogger = _host is null ? null : new RunLogger(logPath, _host.Events, _projectionSymbols);
        _kernelTime?.SetTickRate(NormalTicksPerSecond);
        _displayPaused = false;
        _displayTicksPerSecond = NormalTicksPerSecond;
        GD.Print($"Simulation initialized: {_uiProjection?.Entities.Count ?? 0} projected entities. Run log -> {logPath}");

        // Create map renderer
        _mapRenderer = new HexMapRenderer();
        if (_hexMapProjection is not null && _uiProjection is not null)
        {
            _mapRenderer.Initialize(_hexMapProjection, _uiProjection, _projectionSymbols);
        }
        AddChild(_mapRenderer);

        // Create camera
        _camera = new Camera2D
        {
            Zoom = new Vector2(1.5f, 1.5f),
        };
        _camera.MakeCurrent();
        AddChild(_camera);

        // Set the viewport clear color to Basalt so the area behind the map
        // (and any uncovered regions) reads as part of the archive, not Godot default.
        RenderingServer.SetDefaultClearColor(UiTokens.Basalt);

        // Build shared theme once and hang it off the UI root.
        _theme = UiTheme.Build();

        // UI layer (not affected by camera zoom/pan)
        _uiLayer = new CanvasLayer();
        AddChild(_uiLayer);

        // Card layer (floating cards, above UI)
        _cardLayer = new CanvasLayer { Layer = 2 };
        AddChild(_cardLayer);

        // Single Control root under the UI CanvasLayer. Carries the theme so every
        // descendant inherits it without per-node plumbing.
        _uiRoot = new Control
        {
            Name = "UiRoot",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _uiRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _uiRoot.Theme = _theme;
        _uiLayer.AddChild(_uiRoot);

        // -----------------------------------------------------------------
        // Root layout: TOP BAR  /  MIDDLE ROW  /  BOTTOM BAR
        // Mouse filter is "ignore" for transparent wrappers so the map beneath
        // receives clicks everywhere we didn't explicitly place chrome.
        // -----------------------------------------------------------------
        _rootColumn = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _rootColumn.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _rootColumn.AddThemeConstantOverride("separation", 0);
        _uiRoot.AddChild(_rootColumn);

        _rootColumn.AddChild(BuildTopBar());

        // Middle row: registry | map area | chronicle
        var middleRow = new HBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        middleRow.AddThemeConstantOverride("separation", 0);
        _rootColumn.AddChild(middleRow);

        // --- Left rail: Registry
        _browser = new EntityBrowser
        {
            CustomMinimumSize = new Vector2(260, 0),
        };
        _browser.AddThemeStyleboxOverride("panel", UiTheme.RailPanelStyle(onLeft: true));
        _browser.Initialize(_uiProjection, _projectionSymbols);
        _browser.EntitySelected += OnBrowserEntitySelected;
        middleRow.AddChild(_browser);

        // --- Center: transparent map area. Hosts contextual overlays (plane toggle, legend).
        _mapArea = new Control
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        middleRow.AddChild(_mapArea);

        // Plane toggle — anchored at bottom-left of the map
        _planeToggleButton = new Button
        {
            Text = "⌗  Physical",
            CustomMinimumSize = new Vector2(150, 30),
            TooltipText = "Toggle physical ↔ semantic plane",
        };
        _planeToggleButton.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _planeToggleButton.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        _planeToggleButton.Position = new Vector2(UiTokens.Space4, -UiTokens.Space6 - UiTokens.Space2);
        _planeToggleButton.Pressed += OnTogglePlanePressed;
        _mapArea.AddChild(_planeToggleButton);

        // Legend — anchored at bottom-right of the map
        _legendPanel = BuildLegendPanel();
        _legendPanel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        // Hold position relative to the bottom-right corner. The Size will
        // resolve after layout; we'll reposition in PositionFloatingHud.
        _mapArea.AddChild(_legendPanel);

        // --- Right rail: Chronicle (event log)
        middleRow.AddChild(BuildChroniclePanel(width: 340));

        _rootColumn.AddChild(BuildBottomBar());

        // -----------------------------------------------------------------
        // Floating overlays — live above the layout, positioned by code.
        // -----------------------------------------------------------------
        _tableauPanel = new TableauPanel();
        _uiRoot.AddChild(_tableauPanel);
        _tableauPanel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _tableauPanel.Visible = false;

        _hexTooltipPanel = new PanelContainer
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _hexTooltipPanel.AddThemeStyleboxOverride("panel", UiTheme.OverlayPanelStyle());
        _hexTooltipLabel = new Label
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.Off,
        };
        _hexTooltipLabel.AddThemeFontOverride("font", UiTheme.MonoFont);
        _hexTooltipLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _hexTooltipLabel.AddThemeColorOverride("font_color", UiTokens.Vellum);
        _hexTooltipPanel.AddChild(_hexTooltipLabel);
        _uiRoot.AddChild(_hexTooltipPanel);

        // Initial browser population
        _browser.Refresh();

        GD.Print("Controls: Space=pause, Right=step, 1/2/3=speed, R=reset, N=names, G=graph, WASD=pan, scroll=zoom");
    }

    private PanelContainer BuildTopBar()
    {
        var bar = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 48),
        };
        bar.AddThemeStyleboxOverride("panel", UiTheme.ChromeBarStyle());

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", UiTokens.Space4);
        bar.AddChild(row);

        // Title cluster
        var titleColumn = new VBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        titleColumn.AddThemeConstantOverride("separation", 0);
        row.AddChild(titleColumn);

        var title = new Label { Text = "The Canonry" };
        title.AddThemeFontOverride("font", UiTheme.DisplayFont);
        title.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleSmall);
        title.AddThemeColorOverride("font_color", UiTokens.Vellum);
        titleColumn.AddChild(title);

        var subtitle = new Label { Text = "THE ICE REMEMBERS" };
        subtitle.AddThemeFontOverride("font", UiTheme.BodyFont);
        subtitle.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        subtitle.AddThemeColorOverride("font_color", UiTokens.Whisper);
        titleColumn.AddChild(subtitle);

        // Spacer
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        // Tick readout (center)
        _tickLabel = new Label
        {
            Text = "t 0",
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        _tickLabel.AddThemeFontOverride("font", UiTheme.MonoFont);
        _tickLabel.AddThemeFontSizeOverride("font_size", UiTokens.SizeBody);
        _tickLabel.AddThemeColorOverride("font_color", UiTokens.Parchment);
        row.AddChild(_tickLabel);

        // Spacer
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        // Speed controls
        row.AddChild(BuildSimulationControls());
        return bar;
    }

    private PanelContainer BuildBottomBar()
    {
        var bar = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 40),
        };
        bar.AddThemeStyleboxOverride("panel", UiTheme.ChromeBarStyle(bottom: true));

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", UiTokens.Space2);
        bar.AddChild(row);

        _tableauToggleButton = new Button
        {
            Text = "Tableau",
            CustomMinimumSize = new Vector2(110, 28),
        };
        _tableauToggleButton.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _tableauToggleButton.Pressed += ToggleTableauVisibility;
        row.AddChild(_tableauToggleButton);

        _readingsToggleButton = new Button
        {
            Text = "Readings",
            CustomMinimumSize = new Vector2(110, 28),
        };
        _readingsToggleButton.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        _readingsToggleButton.Pressed += ToggleAttributeGraph;
        row.AddChild(_readingsToggleButton);

        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        var hint = new Label
        {
            Text = "SPACE pause   →  step   1 / 4× / 10×   N  names   G  readings   R  reset   WASD  pan   scroll  zoom",
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        hint.AddThemeFontOverride("font", UiTheme.BodyFont);
        hint.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        hint.AddThemeColorOverride("font_color", UiTokens.Whisper);
        row.AddChild(hint);

        return bar;
    }

    private PanelContainer BuildChroniclePanel(float width)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(width, 0),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        panel.AddThemeStyleboxOverride("panel", UiTheme.RailPanelStyle(onLeft: false));

        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", UiTokens.Space2);
        panel.AddChild(outer);

        var title = new Label
        {
            Text = "Chronicle",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontOverride("font", UiTheme.DisplayFont);
        title.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        title.AddThemeColorOverride("font_color", UiTokens.Vellum);
        outer.AddChild(title);

        var subtitle = new Label
        {
            Text = "EVENTS, IN THE ORDER THEY HAPPENED",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        subtitle.AddThemeFontOverride("font", UiTheme.BodyFont);
        subtitle.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        subtitle.AddThemeColorOverride("font_color", UiTokens.Whisper);
        outer.AddChild(subtitle);

        outer.AddChild(BuildHairlineRule());

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        outer.AddChild(scroll);

        _eventLogLabel = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ScrollActive = false,
        };
        _eventLogLabel.AddThemeFontOverride("normal_font", UiTheme.BodyFont);
        _eventLogLabel.AddThemeFontOverride("mono_font", UiTheme.MonoFont);
        _eventLogLabel.AddThemeFontSizeOverride("normal_font_size", UiTokens.SizeBodySmall);
        _eventLogLabel.AddThemeFontSizeOverride("mono_font_size", UiTokens.SizeBodySmall);
        _eventLogLabel.AddThemeColorOverride("default_color", UiTokens.Vellum);
        scroll.AddChild(_eventLogLabel);

        return panel;
    }

    private PanelContainer BuildLegendPanel()
    {
        var panel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride("panel", UiTheme.OverlayPanelStyle());

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", UiTokens.Space1);
        panel.AddChild(column);

        var title = new Label { Text = "LEGEND" };
        title.AddThemeFontOverride("font", UiTheme.BodyFont);
        title.AddThemeFontSizeOverride("font_size", UiTokens.SizeMicro);
        title.AddThemeColorOverride("font_color", UiTokens.Parchment);
        column.AddChild(title);

        AddLegendRow(column, "penguin", UiTokens.KindPenguin, "penguin");
        AddLegendRow(column, "settlement", UiTokens.KindSettlement, "settlement");
        AddLegendRow(column, "faction", UiTokens.KindFaction, "faction");
        AddLegendRow(column, "artifact", UiTokens.KindArtifact, "artifact");
        AddLegendRow(column, "orca", UiTokens.KindOrca, "orca");

        return panel;
    }

    private static void AddLegendRow(VBoxContainer parent, string kindId, Color accent, string label)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", UiTokens.Space2);
        parent.AddChild(row);

        Control glyph;
        var sprite = AssetLibrary.KindMapIcon(kindId)
            ?? AssetLibrary.KindSprite(kindId);
        if (sprite is not null)
        {
            glyph = new TextureRect
            {
                Texture = sprite,
                CustomMinimumSize = new Vector2(22, 22),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
        }
        else
        {
            glyph = new KindGlyphRect
            {
                KindId = kindId,
                Accent = accent,
                CustomMinimumSize = new Vector2(18, 18),
            };
        }
        row.AddChild(glyph);

        var name = new Label { Text = label };
        name.AddThemeFontOverride("font", UiTheme.BodyFont);
        name.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        name.AddThemeColorOverride("font_color", UiTokens.Parchment);
        row.AddChild(name);
    }

    public override void _Process(double delta)
    {
        if (_host is null) return;

        _host.DrainConsumers();
        _runLogger?.DrainAvailable();

        var previousObservedTick = _lastObservedKernelTick;
        var currentTick = _uiProjection?.CurrentTick ?? 0;
        var ticksProcessed = Math.Max(0, currentTick - previousObservedTick);
        _lastObservedKernelTick = currentTick;

        if (ticksProcessed > 0)
        {
            for (var tick = previousObservedTick + 1; tick <= currentTick; tick++)
            {
                _runLogger?.OnTickEnded(tick);
            }
            _mapRenderer?.Refresh();
            _browser?.Refresh();
            RefreshOpenCards();
            _tableauPanel?.Refresh();
        }

        if (_tickLabel is not null)
        {
            var entityCount = _uiProjection?.Entities.Count ?? 0;
            var generatorFireCount = _uiProjection?.GeneratorFires.Count ?? 0;
            _tickLabel.Text =
                $"t{currentTick,-5}  ·  {FormatKernelTimeState(),-8}  ·  {entityCount,3} entities  ·  {generatorFireCount,3} fires";
        }

        PositionFloatingHud();
        HandleCameraMovement((float)delta);
        UpdateHexHoverTooltip();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_host is null) return;

        if (@event is InputEventKey { Pressed: true } key)
        {
            switch (key.Keycode)
            {
                case Key.Space:
                    if (_displayPaused)
                    {
                        SetKernelTickRate(NormalTicksPerSecond);
                    }
                    else
                    {
                        PauseKernelDriver();
                    }

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
                    if (_mapRenderer is not null)
                    {
                        _mapRenderer.ShowEntityNames = !_mapRenderer.ShowEntityNames;
                        _mapRenderer.Refresh();
                    }

                    break;
                case Key.G:
                    ToggleAttributeGraph();
                    break;
            }
        }

        if (@event is InputEventMouseButton { Pressed: true } mouse)
        {
            var zoomFactor = 1.1f;
            if (mouse.ButtonIndex == MouseButton.WheelUp && _camera is not null)
            {
                _camera.Zoom *= zoomFactor;
                _camera.Zoom = _camera.Zoom.Clamp(new Vector2(0.3f, 0.3f), new Vector2(5f, 5f));
            }
            else if (mouse.ButtonIndex == MouseButton.WheelDown && _camera is not null)
            {
                _camera.Zoom /= zoomFactor;
                _camera.Zoom = _camera.Zoom.Clamp(new Vector2(0.3f, 0.3f), new Vector2(5f, 5f));
            }
            else if (mouse.ButtonIndex == MouseButton.Left)
            {
                HandleMapClick(mouse.Position);
            }
        }
    }

    private void OnTogglePlanePressed()
    {
        if (_mapRenderer is null || _planeToggleButton is null) return;

        _mapRenderer.CurrentPlane = _mapRenderer.CurrentPlane == PlaneMode.Physical
            ? PlaneMode.Semantic
            : PlaneMode.Physical;
        _planeToggleButton.Text = _mapRenderer.CurrentPlane == PlaneMode.Physical
            ? "⌗  Physical"
            : "✦  Semantic";
        _mapRenderer.Refresh();
        ClearHexHoverTooltip();
    }

    private void ToggleAttributeGraph()
    {
        if (_attributeGraph is not null)
        {
            _attributeGraph.QueueFree();
            _attributeGraph = null;
            if (_readingsToggleButton is not null) _readingsToggleButton.Text = "Readings";
            return;
        }

        if (_sim is null || _cardLayer is null) return;
        _attributeGraph = new AttributeGraphPanel
        {
            Theme = _theme,
        };
        _cardLayer.AddChild(_attributeGraph);
        _attributeGraph.Initialize(_sim, _uiProjection);
        _attributeGraph.Position = new Vector2(320, UiTokens.Space7);
        if (_readingsToggleButton is not null) _readingsToggleButton.Text = "Hide Readings";
    }

    private void ResetSimulation()
    {
        if (_cardLayer is not null)
        {
            foreach (var (_, card) in _openCards)
            {
                card.QueueFree();
            }

            _openCards.Clear();
        }

        _runLogger?.Dispose();
        _uiProjectionPump?.Dispose();
        _commandCompletionPump?.Dispose();
        _kernelTime?.Dispose();

        _sim = SimulationRunner.CreateFromTestMap();
        _kernelSimulation = KernelSimulationFactory.CreateFromTestMap(IceRemembersDefinition.Create());
        _kernelTime = CreateKernelTimeController(_kernelSimulation);
        InitializeCommandBus(_kernelSimulation);
        InitializeUiProjection();
        _displayPaused = true;
        _displayTicksPerSecond = NormalTicksPerSecond;
        ClearHexHoverTooltip();

        var logPath = RunLogger.DefaultLogPath();
        _runLogger = new RunLogger(logPath, _kernelSimulation.Events, _projectionSymbols);
        GD.Print($"Simulation reset. New run log -> {logPath}");

        _mapRenderer?.Initialize(_sim);
        _mapRenderer?.Refresh();
        _attributeGraph?.Initialize(_sim, _uiProjection);
        _browser?.Initialize(_sim, _uiProjection);
        _browser?.Refresh();
        _tableauPanel?.Initialize(_sim, _uiProjection, _projectionSymbols, _commandSubmitter);
        _tableauPanel?.Refresh();

        if (_eventLogLabel is not null) _eventLogLabel.Text = "";

        GD.Print("Simulation reset");
    }

    private void InitializeUiProjection()
    {
        if (_sim is null)
        {
            return;
        }

        _uiProjectionPump?.Dispose();
        var kernelEvents = _kernelSimulation?.Events ?? _sim.KernelEvents;
        _uiProjection = new UiProjectionState();
        _uiProjectionPump = new KernelEventProjectionPump(
            kernelEvents,
            Engine.Eventing.KernelEventCursorStart.EarliestAvailable,
            _uiProjection);

        _uiProjectionPump.DrainAvailable();
        _lastObservedKernelTick = _uiProjection.CurrentTick;
    }

    private void InitializeCommandBus(Engine.Kernel.Simulation simulation)
    {
        _commandCompletionTable = new KernelCommandCompletionTable(simulation.Commands.Capacity);
        _commandSubmitter = new KernelCommandSubmitter(simulation.Commands, _commandCompletionTable);
        _commandCompletionPump = new KernelCommandCompletionPump(simulation.Events, _commandCompletionTable);
    }

    private static KernelDriverTimeController CreateKernelTimeController(Engine.Kernel.Simulation simulation) =>
        new(simulation, new RealtimeDriverConfig(
            NormalTicksPerSecond,
            MaxTicksPerSlice: 8,
            SliceInterval: TimeSpan.FromMilliseconds(16)));

    private void OnBrowserEntitySelected(int entityIdValue)
    {
        OpenEntityCard(new EntityId(entityIdValue), new Vector2(300, 100));
    }

    private HBoxContainer BuildSimulationControls()
    {
        var row = new HBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        row.AddThemeConstantOverride("separation", UiTokens.Space2);

        row.AddChild(CreateControlButton("⏸", "Pause", PauseKernelDriver));
        row.AddChild(CreateControlButton("▸", "Step one tick", StepKernelDriver));
        row.AddChild(CreateControlButton("1×", "1 tick/sec", () => SetKernelTickRate(NormalTicksPerSecond)));
        row.AddChild(CreateControlButton("5×", "5 ticks/sec", () => SetKernelTickRate(FastTicksPerSecond)));
        row.AddChild(CreateControlButton("10×", "10 ticks/sec", () => SetKernelTickRate(VeryFastTicksPerSecond)));
        row.AddChild(CreateControlButton("↺", "Reset simulation", ResetSimulation));

        return row;
    }

    private Button CreateControlButton(string text, string tooltip, Action onPressed)
    {
        var button = new Button
        {
            Text = text,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(46, 28),
        };
        button.AddThemeFontSizeOverride("font_size", UiTokens.SizeBodySmall);
        button.Pressed += onPressed;
        return button;
    }

    private static Label BuildPanelTitle(string text)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        label.AddThemeFontOverride("font", UiTheme.DisplayFont);
        label.AddThemeFontSizeOverride("font_size", UiTokens.SizeTitleMedium);
        label.AddThemeColorOverride("font_color", UiTokens.Vellum);
        return label;
    }

    private static ColorRect BuildHairlineRule()
    {
        return new ColorRect
        {
            Color = UiTokens.Hairline,
            CustomMinimumSize = new Vector2(0, 1),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
    }

    private void PauseKernelDriver()
    {
        _kernelTime?.RequestPause();
        _displayPaused = true;
    }

    private void StepKernelDriver()
    {
        _kernelTime?.RequestSingleStep(1);
        _displayPaused = true;
    }

    private void SetKernelTickRate(double ticksPerSecond)
    {
        _kernelTime?.SetTickRate(ticksPerSecond);
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
        if (_tableauPanel is null || _tableauToggleButton is null)
        {
            return;
        }

        _tableauPanel.Visible = !_tableauPanel.Visible;
        _tableauToggleButton.Text = _tableauPanel.Visible ? "Hide Tableau" : "Tableau";
        if (_tableauPanel.Visible)
        {
            _tableauPanel.Refresh();
        }
    }

    private void PositionFloatingHud()
    {
        if (_mapArea is null)
        {
            return;
        }

        var mapRect = _mapArea.GetGlobalRect();

        if (_tableauPanel is not null)
        {
            // Tableau: floating overlay centered in the map area. We drive
            // CustomMinimumSize here (not Size) because PanelContainer auto-
            // clamps to its combined min — setting Size directly gets
            // overridden on the next layout pass and leaks the backdrop
            // past the map bounds. Tune up to a preferred ceiling but
            // never exceed the available map space.
            var padding = UiTokens.Space4 * 2f;
            var availableW = Mathf.Max(1000f, mapRect.Size.X - padding);
            var availableH = Mathf.Max(640f, mapRect.Size.Y - padding);
            var width = Mathf.Min(availableW, 1080f);
            var height = Mathf.Min(availableH, 780f);
            _tableauPanel.CustomMinimumSize = new Vector2(width, height);
            _tableauPanel.Position = new Vector2(
                mapRect.Position.X + (mapRect.Size.X - width) * 0.5f,
                mapRect.Position.Y + (mapRect.Size.Y - height) * 0.5f);
        }

        if (_legendPanel is not null)
        {
            // Anchor legend at bottom-right of the map area via explicit offsets.
            var legendSize = _legendPanel.GetCombinedMinimumSize();
            _legendPanel.Position = new Vector2(
                _mapArea.Size.X - legendSize.X - UiTokens.Space4,
                _mapArea.Size.Y - legendSize.Y - UiTokens.Space6 - UiTokens.Space2);
        }
    }

    private void HandleMapClick(Vector2 screenPos)
    {
        if (_sim is null || _camera is null || _mapRenderer is null || _uiProjection is null) return;

        var worldPos = GetCanvasTransform().AffineInverse() * screenPos;

        EntityId? nearest = null;
        var nearestDist = _mapRenderer.HexSize * 1.2f;

        foreach (var entity in _uiProjection.Entities.Values)
        {
            if (entity.StatusCode != UiProjectionStatusCodes.Active) continue;

            var entityPixel = _mapRenderer.GetHexCenter(
                ProjectedEntityCoordForCurrentPlane(entity));
            var dist = worldPos.DistanceTo(entityPixel);

            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = new EntityId(entity.EntityId);
            }
        }

        if (nearest is not null)
        {
            OpenEntityCard(nearest.Value, screenPos + new Vector2(20, -20));
        }
    }

    private void UpdateHexHoverTooltip()
    {
        if (_sim is null || _mapRenderer is null || _hexTooltipPanel is null || _hexTooltipLabel is null)
        {
            return;
        }

        var hoveredControl = GetViewport().GuiGetHoveredControl();
        if (ShouldSuppressHexTooltip(hoveredControl))
        {
            ClearHexHoverTooltip();
            return;
        }

        var screenPos = GetViewport().GetMousePosition();
        var worldPos = GetCanvasTransform().AffineInverse() * screenPos;
        var pointedHex = _mapRenderer.GetHexAtWorldPosition(worldPos);
        if (pointedHex is null)
        {
            ClearHexHoverTooltip();
            return;
        }

        _hoverScreenPosition = screenPos;
        if (_hoveredHex is null || _hoveredHex.Value != pointedHex.Value)
        {
            _hoveredHex = pointedHex.Value;
            _hoverStartedAtMs = Time.GetTicksMsec();
            _hexTooltipPanel.Visible = false;
            return;
        }

        if (!_hexTooltipPanel.Visible && Time.GetTicksMsec() - _hoverStartedAtMs < HexTooltipDelayMs)
        {
            return;
        }

        _hexTooltipLabel.Text = FormatHexTooltip(pointedHex.Value);
        _hexTooltipPanel.Visible = true;
        PositionHexTooltip(_hoverScreenPosition);
    }

    private void ClearHexHoverTooltip()
    {
        _hoveredHex = null;
        if (_hexTooltipPanel is not null)
        {
            _hexTooltipPanel.Visible = false;
        }
    }

    private bool ShouldSuppressHexTooltip(Control? hoveredControl)
    {
        if (hoveredControl is null)
        {
            return false;
        }

        // The root layout spans the whole screen, including the transparent map gap.
        // Treat it as pass-through so hovering the map does not suppress the tooltip.
        // The map area and the root column are both transparent pass-through containers;
        // hovering them should not suppress the hex tooltip. Anything else (rails, bars,
        // overlays) should.
        return hoveredControl != _rootColumn && hoveredControl != _mapArea;
    }

    private string FormatHexTooltip(HexCoord coord)
    {
        if (_sim is null)
        {
            return string.Empty;
        }

        if (_mapRenderer?.CurrentPlane == PlaneMode.Semantic)
        {
            // Semantic plane: Q = autonomy, R = resonance, S = tradition.
            var s = -coord.Q - coord.R;
            var occupants = (_uiProjection?.Entities.Values ?? Enumerable.Empty<UiEntityProjection>())
                .Where(entity => entity.StatusCode == UiProjectionStatusCodes.Active &&
                                 entity.SemanticQ == coord.Q &&
                                 entity.SemanticR == coord.R)
                .Select(FormatProjectedEntityLine)
                .ToList();
            var occupantsLine = occupants.Count == 0 ? "— uninhabited —" : string.Join("\n", occupants);
            return $"autonomy {coord.Q,3}    resonance {coord.R,3}    tradition {s,3}\n{occupantsLine}";
        }

        var cell = _sim.Grid.GetCell(coord);
        var terrain = cell?.Terrain.ToString() ?? "Unknown";
        var regionName = _sim.CoverageMap.GetCoverageForCell(coord)?.Name ?? "Unassigned";
        var food = _sim.Resources.Food.GetStockpile(coord);
        var wealth = _sim.Resources.Wealth.GetStockpile(coord);
        var population = _sim.Resources.PopulationLayer.GetStockpile(coord);
        return
            $"{coord.Q,3} , {coord.R,3}   {terrain,-10}   {regionName}\n" +
            $"food       {food,7:0.0}\n" +
            $"wealth     {wealth,7:0.0}\n" +
            $"population {population,7:0.0}";
    }

    private HexCoord ProjectedEntityCoordForCurrentPlane(UiEntityProjection entity)
    {
        return _mapRenderer?.CurrentPlane == PlaneMode.Physical
            ? new HexCoord(entity.PhysicalQ, entity.PhysicalR)
            : new HexCoord(entity.SemanticQ, entity.SemanticR);
    }

    private string FormatProjectedEntityLine(UiEntityProjection entity)
    {
        var name = ResolveProjectedEntityName(entity);
        var kind = _projectionSymbols?.ResolveDisplayName(
            ProjectionSymbolKind.Kind,
            entity.KindId,
            $"Kind {entity.KindId}") ?? $"Kind {entity.KindId}";
        return $"{name}  {kind}";
    }

    private string ResolveProjectedEntityName(UiEntityProjection entity)
    {
        return _projectionSymbols?.ResolveEntityName(
            new ProjectionEntityNameKey(
                entity.EntityId,
                entity.KindId,
                entity.SubtypeId,
                entity.CultureId,
                entity.NameSeed),
            $"E{entity.EntityId}") ?? $"E{entity.EntityId}";
    }

    private void PositionHexTooltip(Vector2 screenPos)
    {
        if (_hexTooltipPanel is null)
        {
            return;
        }

        var viewportSize = GetViewportRect().Size;
        var panelSize = _hexTooltipPanel.GetCombinedMinimumSize();
        var pos = screenPos + new Vector2(16f, 18f);
        pos.X = Mathf.Min(pos.X, viewportSize.X - panelSize.X - 8f);
        pos.Y = Mathf.Min(pos.Y, viewportSize.Y - panelSize.Y - 8f);
        pos.X = Mathf.Max(pos.X, 8f);
        pos.Y = Mathf.Max(pos.Y, 8f);
        _hexTooltipPanel.Position = pos;
    }

    private void OpenEntityCard(EntityId entityId, Vector2 position)
    {
        if (_sim is null || _cardLayer is null) return;

        // If card already open, bring it to front
        if (_openCards.TryGetValue(entityId, out var existing))
        {
            _cardLayer.RemoveChild(existing);
            _cardLayer.AddChild(existing); // Re-add to move to top
            return;
        }

        var card = new EntityCard
        {
            Theme = _theme,
        };
        card.Initialize(_sim, _uiProjection, entityId, position);
        card.CardClosed += OnCardClosed;
        _cardLayer.AddChild(card);
        _openCards[entityId] = card;
    }

    private void OnCardClosed(EntityCard card)
    {
        _openCards.Remove(card.EntityId);
    }

    private void RefreshOpenCards()
    {
        foreach (var (_, card) in _openCards)
        {
            card.Refresh();
        }
    }

    internal static string ColorHex(Color color)
    {
        var r = Mathf.RoundToInt(color.R * 255);
        var g = Mathf.RoundToInt(color.G * 255);
        var b = Mathf.RoundToInt(color.B * 255);
        return $"#{r:x2}{g:x2}{b:x2}";
    }

    private void HandleCameraMovement(float delta)
    {
        if (_camera is null) return;

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
