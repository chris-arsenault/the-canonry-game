#pragma warning disable CA2000 // Godot nodes managed by scene tree
using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

using Engine.Clock;
using Engine.Domain.Config;
using Engine.Domain.Loader;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Observability;
using Engine.Projection;
using Engine.Processes;
using Engine.Resources;
using Engine.Simulation;
using TheCanonryGame.UI.Theme;

namespace TheCanonryGame.UI;

#pragma warning disable CA2213 // Godot nodes are managed by the scene tree
public partial class Main : Node2D
{
    private const ulong HexTooltipDelayMs = 500;

    private SimulationRunner? _sim;
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
    private KernelEventProjectionPump? _uiProjectionPump;
    private ProjectionSymbolCatalog? _projectionSymbols;
    private PanelContainer? _hexTooltipPanel;
    private Label? _hexTooltipLabel;
    private PanelContainer? _legendPanel;
    private Godot.Theme? _theme;
    private double _tickAccumulator;
    private bool _stepOneTick;
    private HexCoord? _hoveredHex;
    private Vector2 _hoverScreenPosition;
    private ulong _hoverStartedAtMs;
    private readonly Dictionary<EntityId, EntityCard> _openCards = [];

    public override void _ExitTree()
    {
        _uiProjectionPump?.Dispose();
        _uiProjectionPump = null;
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

        _sim = SimulationRunner.CreateFromTestMap();
        _sim.Clock.Speed = SimulationSpeed.Normal;
        InitializeUiProjection();

        var logPath = RunLogger.DefaultLogPath();
        _runLogger = new RunLogger(logPath, _sim);
        GD.Print($"Simulation initialized: {_sim.Graph.EntityCount} entities. Run log -> {logPath}");

        // Create map renderer
        _mapRenderer = new HexMapRenderer();
        _mapRenderer.Initialize(_sim);
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

        // Subscribe to events BEFORE we start building UI that listens to them,
        // so anything emitted during setup reaches the chronicle.
        _sim.EventLog.EventEmitted += OnEventEmitted;
        _sim.Lifecycle.EntityDied += (_, e) =>
        {
            _sim.EventLog.Emit(e.DeathTick, Engine.Events.EventType.Lifecycle,
                $"{e.Entity.Name} ({e.Entity.Kind}) died", "lifecycle", e.Entity.Id);
        };

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
        _browser.Initialize(_sim);
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
        _tableauPanel.Initialize(_sim, _uiProjection, _projectionSymbols);
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
        if (_sim is null) return;

        var ticksProcessed = 0;

        if (_stepOneTick)
        {
            _sim.Clock.Speed = SimulationSpeed.Normal;
            _sim.Tick();
            _runLogger?.OnTickEnded();
            _sim.Clock.Speed = SimulationSpeed.Paused;
            _stepOneTick = false;
            ticksProcessed = 1;
        }
        else if (_sim.Clock.Speed != SimulationSpeed.Paused)
        {
            var secondsPerTick = _sim.Clock.Speed switch
            {
                SimulationSpeed.Normal => 1.0,
                SimulationSpeed.Fast => 0.25,
                SimulationSpeed.VeryFast => 0.1,
                _ => 1.0,
            };

            _tickAccumulator += delta;
            while (_tickAccumulator >= secondsPerTick)
            {
                _sim.Tick();
                _runLogger?.OnTickEnded();
                _tickAccumulator -= secondsPerTick;
                ticksProcessed++;
            }
        }

        if (ticksProcessed > 0)
        {
            _uiProjectionPump?.DrainAvailable();
            _mapRenderer?.Refresh();
            _browser?.Refresh();
            RefreshOpenCards();
            _attributeGraph?.Refresh();
            _tableauPanel?.Refresh();
        }

        if (_tickLabel is not null)
        {
            var speedText = _sim.Clock.Speed switch
            {
                SimulationSpeed.Paused   => "paused",
                SimulationSpeed.Normal   => "1×",
                SimulationSpeed.Fast     => "4×",
                SimulationSpeed.VeryFast => "10×",
                _                        => _sim.Clock.Speed.ToString(),
            };
            _tickLabel.Text =
                $"t{_sim.Clock.CurrentTick,-5}  ·  {speedText,-6}  ·  {_sim.Graph.EntityCount,3} entities  ·  {_sim.GeneratorSystem.TotalFired,3} fires";
        }

        PositionFloatingHud();
        HandleCameraMovement((float)delta);
        UpdateHexHoverTooltip();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_sim is null) return;

        if (@event is InputEventKey { Pressed: true } key)
        {
            switch (key.Keycode)
            {
                case Key.Space:
                    if (_sim.Clock.Speed == SimulationSpeed.Paused)
                    {
                        _sim.Clock.Speed = SimulationSpeed.Normal;
                    }
                    else
                    {
                        _sim.Clock.Speed = SimulationSpeed.Paused;
                        _tickAccumulator = 0;
                    }

                    break;
                case Key.Key1:
                    _sim.Clock.Speed = SimulationSpeed.Normal;
                    _tickAccumulator = 0;
                    break;
                case Key.Key2:
                    _sim.Clock.Speed = SimulationSpeed.Fast;
                    _tickAccumulator = 0;
                    break;
                case Key.Key3:
                    _sim.Clock.Speed = SimulationSpeed.VeryFast;
                    _tickAccumulator = 0;
                    break;
                case Key.Right:
                    _stepOneTick = true;
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
        _attributeGraph.Initialize(_sim);
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

        _sim = SimulationRunner.CreateFromTestMap();
        _sim.Clock.Speed = SimulationSpeed.Paused;
        InitializeUiProjection();
        _tickAccumulator = 0;
        ClearHexHoverTooltip();

        var logPath = RunLogger.DefaultLogPath();
        _runLogger = new RunLogger(logPath, _sim);
        GD.Print($"Simulation reset. New run log -> {logPath}");

        _sim.EventLog.EventEmitted += OnEventEmitted;
        _sim.Lifecycle.EntityDied += (_, e) =>
        {
            _sim.EventLog.Emit(e.DeathTick, Engine.Events.EventType.Lifecycle,
                $"{e.Entity.Name} ({e.Entity.Kind}) died", "lifecycle", e.Entity.Id);
        };

        _mapRenderer?.Initialize(_sim);
        _mapRenderer?.Refresh();
        _attributeGraph?.Initialize(_sim);
        _browser?.Initialize(_sim);
        _browser?.Refresh();
        _tableauPanel?.Initialize(_sim, _uiProjection, _projectionSymbols);
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
        _projectionSymbols = BuildProjectionSymbolCatalog(_sim);
        _uiProjection = new UiProjectionState();
        _uiProjectionPump = new KernelEventProjectionPump(
            _sim.KernelEvents,
            Engine.Eventing.KernelEventCursorStart.EarliestAvailable,
            _uiProjection);

        // MIGRATION: The UI projection replays every primitive event currently
        // available. Any remaining live SimulationRunner reads in panels mark
        // missing primitive events or startup snapshots, not a fallback bus.
        _uiProjectionPump.DrainAvailable();
    }

    private static ProjectionSymbolCatalog BuildProjectionSymbolCatalog(SimulationRunner sim)
    {
        var catalog = new ProjectionSymbolCatalog();

        foreach (var (kind, config) in sim.DomainConfig.Kinds.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal))
        {
            catalog.RegisterSymbol(
                ProjectionSymbolKind.Kind,
                kind.Id,
                kind.Value,
                description: config.Description);

            foreach (var subtype in config.Subtypes.OrderBy(value => value, StringComparer.Ordinal))
            {
                catalog.RegisterSymbol(
                    ProjectionSymbolKind.Subtype,
                    sim.DomainIds.SubtypeIdOf(subtype).Value,
                    subtype);
            }

            foreach (var attribute in config.Attributes.OrderBy(attr => attr.Name, StringComparer.Ordinal))
            {
                catalog.RegisterSymbol(
                    ProjectionSymbolKind.Attribute,
                    sim.DomainIds.AttributeIdOf(attribute.Name).Value,
                    attribute.Name,
                    attribute.DisplayName,
                    attribute.CriticalEffect?.Description);
            }
        }

        foreach (var culture in sim.DomainConfig.Cultures.OrderBy(culture => culture.Id, StringComparer.Ordinal))
        {
            catalog.RegisterSymbol(
                ProjectionSymbolKind.Culture,
                sim.DomainIds.CultureIdOf(culture.Id).Value,
                culture.Id,
                culture.DisplayName);
        }

        catalog.RegisterSymbol(
            ProjectionSymbolKind.Culture,
            sim.DomainIds.CultureIdOf(DomainRuntimeIdLoader.NoneCultureId).Value,
            DomainRuntimeIdLoader.NoneCultureId,
            "None");

        foreach (var relationship in sim.DomainConfig.Relationships.OrderBy(rel => rel.Name, StringComparer.Ordinal))
        {
            catalog.RegisterSymbol(
                ProjectionSymbolKind.RelationshipKind,
                sim.DomainIds.RelationshipKindIdOf(relationship.Name).Value,
                relationship.Name);
        }

        foreach (var fact in sim.DomainConfig.RelationshipFacts.OrderBy(fact => fact.Name, StringComparer.Ordinal))
        {
            catalog.RegisterSymbol(
                ProjectionSymbolKind.RelationshipFact,
                sim.DomainIds.RelationshipFactIdOf(fact.Name).Value,
                fact.Name,
                fact.DisplayName,
                fact.Description);
        }

        foreach (var pressure in sim.DomainConfig.Pressures.OrderBy(pressure => pressure.Name, StringComparer.Ordinal))
        {
            catalog.RegisterSymbol(
                ProjectionSymbolKind.Pressure,
                sim.DomainIds.PressureIdOf(pressure.Name).Value,
                pressure.Name);
        }

        foreach (var resourceType in Enum.GetValues<ResourceType>())
        {
            var stableName = resourceType.ToDomainName();
            catalog.RegisterSymbol(
                ProjectionSymbolKind.Resource,
                sim.DomainIds.ResourceIdOf(stableName).Value,
                stableName);
        }

        foreach (var resource in sim.DomainConfig.Resources.OrderBy(resource => resource.Name, StringComparer.Ordinal))
        {
            catalog.RegisterSymbol(
                ProjectionSymbolKind.Resource,
                sim.DomainIds.ResourceIdOf(resource.Name).Value,
                resource.Name,
                resource.DisplayName);
        }

        foreach (var terrain in Enum.GetValues<TerrainType>())
        {
            var stableName = terrain.ToString();
            catalog.RegisterSymbol(
                ProjectionSymbolKind.Terrain,
                sim.DomainIds.TerrainIdOf(stableName).Value,
                stableName);
        }

        foreach (var generator in sim.DefinitionGraph.Generators.Values.OrderBy(generator => generator.Id, StringComparer.Ordinal))
        {
            catalog.RegisterSymbol(
                ProjectionSymbolKind.Generator,
                generator.RuntimeId,
                generator.Id,
                generator.Name,
                generator.Description);
        }

        foreach (var process in sim.DefinitionGraph.Processes.Values.OrderBy(process => process.Id, StringComparer.Ordinal))
        {
            var symbolKind = process.SourceDefinition.Role == DefinitionRole.Decision
                ? ProjectionSymbolKind.Decision
                : ProjectionSymbolKind.Process;
            catalog.RegisterSymbol(
                symbolKind,
                process.SourceDefinition.RuntimeId.Value,
                process.Id,
                process.Name);
        }

        foreach (var card in sim.SpawnCards.Values.OrderBy(card => card.Id, StringComparer.Ordinal))
        {
            catalog.RegisterSymbol(
                ProjectionSymbolKind.Card,
                card.RuntimeId.Value,
                card.Id,
                card.Name,
                card.Description);
        }

        RegisterKernelRuntimeSymbols(catalog, sim);

        foreach (var entity in sim.Graph.AllEntities.Values.OrderBy(entity => entity.Id.Value))
        {
            // MIGRATION: Initial entity names come from the startup graph until
            // userspace receives a formal entity-name seed snapshot.
            catalog.RegisterEntityName(entity.Id.Value, entity.Name);

            if (entity.SubtypeId.Value > 0)
            {
                catalog.RegisterSymbol(
                    ProjectionSymbolKind.Subtype,
                    entity.SubtypeId.Value,
                    entity.Subtype);
            }

            if (entity.CultureId.Value > 0)
            {
                catalog.RegisterSymbol(
                    ProjectionSymbolKind.Culture,
                    entity.CultureId.Value,
                    entity.Culture,
                    sim.DomainConfig.GetCultureDisplayName(entity.Culture));
            }
        }

        return catalog;
    }

    private static void RegisterKernelRuntimeSymbols(ProjectionSymbolCatalog catalog, SimulationRunner sim)
    {
        catalog.RegisterSymbol(
            ProjectionSymbolKind.EventCode,
            sim.DomainIds.EventCodeIdOf(DomainRuntimeIdLoader.RelationshipCreatedCode).Value,
            DomainRuntimeIdLoader.RelationshipCreatedCode);
        catalog.RegisterSymbol(
            ProjectionSymbolKind.EventCode,
            sim.DomainIds.EventCodeIdOf(DomainRuntimeIdLoader.RelationshipCulledCode).Value,
            DomainRuntimeIdLoader.RelationshipCulledCode);
        catalog.RegisterSymbol(
            ProjectionSymbolKind.EventCode,
            sim.DomainIds.EventCodeIdOf(DomainRuntimeIdLoader.RelationshipStrengthChangedCode).Value,
            DomainRuntimeIdLoader.RelationshipStrengthChangedCode);
        catalog.RegisterSymbol(
            ProjectionSymbolKind.SourceCode,
            sim.DomainIds.SourceCodeIdOf(DomainRuntimeIdLoader.ProcessSourceCode).Value,
            DomainRuntimeIdLoader.ProcessSourceCode);
        catalog.RegisterSymbol(
            ProjectionSymbolKind.SourceCode,
            sim.DomainIds.SourceCodeIdOf(DomainRuntimeIdLoader.CardSourceCode).Value,
            DomainRuntimeIdLoader.CardSourceCode);
        catalog.RegisterSymbol(
            ProjectionSymbolKind.Detector,
            sim.DomainIds.DetectorIdOf(DomainRuntimeIdLoader.SettlementDepopulationDetector).Value,
            DomainRuntimeIdLoader.SettlementDepopulationDetector);
        catalog.RegisterSymbol(
            ProjectionSymbolKind.Detector,
            sim.DomainIds.DetectorIdOf(DomainRuntimeIdLoader.PoliticalMonocultureDetector).Value,
            DomainRuntimeIdLoader.PoliticalMonocultureDetector);
        catalog.RegisterSymbol(
            ProjectionSymbolKind.Detector,
            sim.DomainIds.DetectorIdOf(DomainRuntimeIdLoader.NpcExtinctionDetector).Value,
            DomainRuntimeIdLoader.NpcExtinctionDetector);
        catalog.RegisterSymbol(
            ProjectionSymbolKind.ReasonCode,
            sim.DomainIds.ReasonCodeIdOf(DomainRuntimeIdLoader.SettlementDepopulationReason).Value,
            DomainRuntimeIdLoader.SettlementDepopulationReason);
        catalog.RegisterSymbol(
            ProjectionSymbolKind.ReasonCode,
            sim.DomainIds.ReasonCodeIdOf(DomainRuntimeIdLoader.PoliticalMonocultureReason).Value,
            DomainRuntimeIdLoader.PoliticalMonocultureReason);
        catalog.RegisterSymbol(
            ProjectionSymbolKind.ReasonCode,
            sim.DomainIds.ReasonCodeIdOf(DomainRuntimeIdLoader.NpcExtinctionReason).Value,
            DomainRuntimeIdLoader.NpcExtinctionReason);
    }

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

        row.AddChild(CreateControlButton("⏸", "Pause", () => SetSimulationSpeed(SimulationSpeed.Paused)));
        row.AddChild(CreateControlButton("▸", "Step one tick", () => _stepOneTick = true));
        row.AddChild(CreateControlButton("1×", "Normal speed", () => SetSimulationSpeed(SimulationSpeed.Normal)));
        row.AddChild(CreateControlButton("4×", "Fast", () => SetSimulationSpeed(SimulationSpeed.Fast)));
        row.AddChild(CreateControlButton("10×", "Very fast", () => SetSimulationSpeed(SimulationSpeed.VeryFast)));
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

    private void SetSimulationSpeed(SimulationSpeed speed)
    {
        if (_sim is null)
        {
            return;
        }

        _sim.Clock.Speed = speed;
        _tickAccumulator = 0;
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
        card.Initialize(_sim, entityId, position);
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

    private void OnEventEmitted(object? sender, Engine.Events.GameEventArgs e)
    {
        if (_eventLogLabel is null) return;

        var color = e.Type switch
        {
            Engine.Events.EventType.Lifecycle => UiTokens.Blood,
            Engine.Events.EventType.Threshold => UiTokens.Ember,
            Engine.Events.EventType.Creation  => UiTokens.Aurora,
            Engine.Events.EventType.Card      => UiTokens.Gilt,
            _                                 => UiTokens.Vellum,
        };
        var tickColor = UiTokens.Whisper;
        var tickHex = ColorHex(tickColor);
        var descHex = ColorHex(color);
        _eventLogLabel.AppendText($"[color={tickHex}][code]t{e.Tick,5}[/code][/color]  [color={descHex}]{e.Description}[/color]\n");
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
