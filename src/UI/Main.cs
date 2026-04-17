#pragma warning disable CA2000 // Godot nodes managed by scene tree
using System.Collections.Generic;
using System.Linq;

using Godot;

using Engine.Clock;
using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Simulation;

namespace TheCanonryGame.UI;

#pragma warning disable CA2213 // Godot nodes are managed by the scene tree
public partial class Main : Node2D
{
    private const ulong HexTooltipDelayMs = 500;

    private SimulationRunner? _sim;
    private HexMapRenderer? _mapRenderer;
    private Camera2D? _camera;
    private EntityBrowser? _browser;
    private HBoxContainer? _rootLayout;
    private Control? _mapSpacer;
    private CanvasLayer? _uiLayer;
    private CanvasLayer? _cardLayer;
    private RichTextLabel? _eventLogLabel;
    private AttributeGraphPanel? _attributeGraph;
    private Label? _tickLabel;
    private PanelContainer? _hexTooltipPanel;
    private Label? _hexTooltipLabel;
    private double _tickAccumulator;
    private bool _stepOneTick;
    private HexCoord? _hoveredHex;
    private Vector2 _hoverScreenPosition;
    private ulong _hoverStartedAtMs;
    private readonly Dictionary<EntityId, EntityCard> _openCards = [];

    public override void _Ready()
    {
        GD.Print("The Canonry Game - Initializing simulation...");

        _sim = SimulationRunner.CreateFromTestMap();
        _sim.Clock.Speed = SimulationSpeed.Normal;

        GD.Print($"Simulation initialized: {_sim.Graph.EntityCount} entities");

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

        // UI layer (not affected by camera zoom/pan)
        _uiLayer = new CanvasLayer();
        AddChild(_uiLayer);

        // Card layer (floating cards, above UI)
        _cardLayer = new CanvasLayer { Layer = 2 };
        AddChild(_cardLayer);

        // Root layout: browser on left, spacer in middle, event log on right
        _rootLayout = new HBoxContainer();
        _rootLayout.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _uiLayer.AddChild(_rootLayout);

        // Left: entity browser
        _browser = new EntityBrowser();
        _browser.Initialize(_sim);
        _browser.EntitySelected += OnBrowserEntitySelected;
        _rootLayout.AddChild(_browser);

        // Middle spacer (map area -- transparent, passes mouse through)
        _mapSpacer = new Control
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _rootLayout.AddChild(_mapSpacer);

        // Right: event log
        var rightVBox = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(300, 0),
        };
        _rootLayout.AddChild(rightVBox);

        var eventPanel = new PanelContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        rightVBox.AddChild(eventPanel);

        var eventScroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        eventPanel.AddChild(eventScroll);

        _eventLogLabel = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ScrollActive = false,
        };
        eventScroll.AddChild(_eventLogLabel);

        // Subscribe to events
        _sim.EventLog.EventEmitted += OnEventEmitted;
        _sim.Lifecycle.EntityDied += (_, e) =>
        {
            _sim.EventLog.Emit(e.DeathTick, Engine.Events.EventType.Lifecycle,
                $"{e.Entity.Name} ({e.Entity.Kind}) died", "lifecycle", e.Entity.Id);
        };

        // Tick counter label (top center)
        _tickLabel = new Label
        {
            Position = new Vector2(10, 10),
            Text = "Tick: 0",
        };
        _tickLabel.AddThemeFontSizeOverride("font_size", 14);
        _uiLayer.AddChild(_tickLabel);

        _hexTooltipPanel = new PanelContainer
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _hexTooltipLabel = new Label
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.Off,
        };
        _hexTooltipLabel.AddThemeFontSizeOverride("font_size", 12);
        _hexTooltipPanel.AddChild(_hexTooltipLabel);
        _uiLayer.AddChild(_hexTooltipPanel);

        // Initial browser population
        _browser.Refresh();

        GD.Print("Controls: Space=pause, Right=step, 1/2/3=speed, R=reset, N=names, G=graph, WASD=pan, scroll=zoom");
    }

    public override void _Process(double delta)
    {
        if (_sim is null) return;

        var ticksProcessed = 0;

        if (_stepOneTick)
        {
            _sim.Clock.Speed = SimulationSpeed.Normal;
            _sim.Tick();
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
                _tickAccumulator -= secondsPerTick;
                ticksProcessed++;
            }
        }

        if (ticksProcessed > 0)
        {
            _mapRenderer?.Refresh();
            _browser?.Refresh();
            RefreshOpenCards();
            _attributeGraph?.Refresh();
        }

        if (_tickLabel is not null)
        {
            var speedText = _sim.Clock.Speed == SimulationSpeed.Paused ? "PAUSED" : $"{_sim.Clock.Speed}";
            _tickLabel.Text = $"Tick: {_sim.Clock.CurrentTick} | {speedText}";
        }

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

    private void ToggleAttributeGraph()
    {
        if (_attributeGraph is not null)
        {
            _attributeGraph.QueueFree();
            _attributeGraph = null;
            return;
        }

        if (_sim is null || _cardLayer is null) return;
        _attributeGraph = new AttributeGraphPanel();
        _cardLayer.AddChild(_attributeGraph);
        _attributeGraph.Initialize(_sim);
        _attributeGraph.Position = new Vector2(300, 50);
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

        _sim = SimulationRunner.CreateFromTestMap();
        _sim.Clock.Speed = SimulationSpeed.Paused;
        _tickAccumulator = 0;
        ClearHexHoverTooltip();

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

        if (_eventLogLabel is not null) _eventLogLabel.Text = "";

        GD.Print("Simulation reset");
    }

    private void OnBrowserEntitySelected(int entityIdValue)
    {
        OpenEntityCard(new EntityId(entityIdValue), new Vector2(300, 100));
    }

    private void HandleMapClick(Vector2 screenPos)
    {
        if (_sim is null || _camera is null || _mapRenderer is null) return;

        var worldPos = GetCanvasTransform().AffineInverse() * screenPos;

        EntityId? nearest = null;
        var nearestDist = _mapRenderer.HexSize * 1.2f;

        foreach (var (entityId, entity) in _sim.Graph.AllEntities)
        {
            if (entity.Status != EntityStatus.Active) continue;

            var entityPixel = _mapRenderer.GetHexCenter(
                new HexCoord(entity.Coordinates.Q, entity.Coordinates.R));
            var dist = worldPos.DistanceTo(entityPixel);

            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = entityId;
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
        return hoveredControl != _rootLayout && hoveredControl != _mapSpacer;
    }

    private string FormatHexTooltip(HexCoord coord)
    {
        if (_sim is null)
        {
            return string.Empty;
        }

        var cell = _sim.Grid.GetCell(coord);
        var terrain = cell?.Terrain.ToString() ?? "Unknown";
        var regionName = _sim.RegionMap.GetRegionForCell(coord)?.Name ?? "Unassigned";
        var food = _sim.Resources.Food.GetStockpile(coord);
        var wealth = _sim.Resources.Wealth.GetStockpile(coord);
        var population = _sim.Resources.PopulationLayer.GetStockpile(coord);
        return $"{coord.Q}, {coord.R}  {terrain}  ·  {regionName}\nfood: {food:0.0}\nwealth: {wealth:0.0}\npopulation: {population:0.0}";
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

        var card = new EntityCard();
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
            Engine.Events.EventType.Lifecycle => "red",
            Engine.Events.EventType.Threshold => "yellow",
            Engine.Events.EventType.Creation => "green",
            Engine.Events.EventType.Card => "cyan",
            _ => "white",
        };

        _eventLogLabel.AppendText($"[color={color}][{e.Tick}] {e.Description}[/color]\n");
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
