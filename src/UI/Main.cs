#pragma warning disable CA2000 // Godot nodes managed by scene tree
using System.Linq;

using Godot;

using Engine.Clock;
using Engine.Graph;
using Engine.Kinds;
using Engine.Simulation;

namespace TheCanonryGame.UI;

#pragma warning disable CA2213 // Godot nodes are managed by the scene tree
public partial class Main : Node2D
{
    private SimulationRunner? _sim;
    private HexMapRenderer? _mapRenderer;
    private Camera2D? _camera;
    private EntityDetailPanel? _detailPanel;
    private CanvasLayer? _uiLayer;
    private RichTextLabel? _eventLogLabel;
    private int _frameCount;
    private double _tickAccumulator;
    private bool _stepOneTick;

    public override void _Ready()
    {
        GD.Print("The Canonry Game - Initializing simulation...");

        _sim = SimulationRunner.CreateFromTestMap();
        _sim.Clock.Speed = SimulationSpeed.Normal;

        GD.Print($"Simulation initialized: {_sim.Graph.EntityCount} entities, {_sim.Graph.ActiveRelationshipCount} relationships");
        GD.Print($"Settlements: {_sim.Graph.GetEntitiesByKind(EntityKind.Settlement).Count}");
        GD.Print($"Nations: {_sim.Graph.GetEntitiesByKind(EntityKind.Nation).Count}");
        GD.Print($"NPCs: {_sim.Graph.GetEntitiesByKind(EntityKind.Npc).Count}");
        GD.Print($"Artifacts: {_sim.Graph.GetEntitiesByKind(EntityKind.Artifact).Count}");
        GD.Print($"Regions: {_sim.Graph.GetEntitiesByKind(EntityKind.Region).Count}");

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

        // HBox to split right panel and event log
        var rootHBox = new HBoxContainer();
        rootHBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _uiLayer.AddChild(rootHBox);

        // Left spacer (map area -- transparent, passes mouse through)
        var mapSpacer = new Control
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rootHBox.AddChild(mapSpacer);

        // Right panel: detail panel + event log stacked vertically
        var rightVBox = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(320, 0),
        };
        rootHBox.AddChild(rightVBox);

        // Detail panel (top half)
        _detailPanel = new EntityDetailPanel
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        _detailPanel.Initialize(_sim);
        rightVBox.AddChild(_detailPanel);

        // Event log (bottom half)
        var eventPanel = new PanelContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 200),
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

        // Tick counter label
        var tickLabel = new Label
        {
            Position = new Vector2(10, 10),
            Text = "Tick: 0",
        };
        tickLabel.AddThemeFontSizeOverride("font_size", 14);
        _uiLayer.AddChild(tickLabel);
        _tickLabel = tickLabel;

        GD.Print("Controls: Space=pause/resume, Right=step 1 tick, 1/2/3=speed, WASD=pan, scroll=zoom, click=select");
    }

    private Label? _tickLabel;

    public override void _Process(double delta)
    {
        if (_sim is null) return;

        var ticksProcessed = 0;

        // Manual single-step (right arrow)
        if (_stepOneTick)
        {
            _sim.Clock.Speed = SimulationSpeed.Normal; // Temporarily unpause for one tick
            _sim.Tick();
            _sim.Clock.Speed = SimulationSpeed.Paused;
            _stepOneTick = false;
            ticksProcessed = 1;
        }
        else if (_sim.Clock.Speed != SimulationSpeed.Paused)
        {
            // Time-based ticking: seconds per tick varies by speed
            var secondsPerTick = _sim.Clock.Speed switch
            {
                SimulationSpeed.Normal => 1.0,   // 1 tick per second
                SimulationSpeed.Fast => 0.25,    // 4 ticks per second
                SimulationSpeed.VeryFast => 0.1, // 10 ticks per second
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

        // Refresh displays
        if (ticksProcessed > 0)
        {
            _mapRenderer?.Refresh();
            _detailPanel?.Refresh();
        }

        if (_tickLabel is not null)
        {
            var speedText = _sim.Clock.Speed == SimulationSpeed.Paused ? "PAUSED" : $"{_sim.Clock.Speed}";
            _tickLabel.Text = $"Tick: {_sim.Clock.CurrentTick} | {speedText}";
        }

        // Camera movement
        HandleCameraMovement((float)delta);
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

                    GD.Print($"Speed: {_sim.Clock.Speed}");
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
            }
        }

        // Mouse events
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
                HandleEntityClick(mouse.Position);
            }
        }
    }

    private void HandleEntityClick(Vector2 screenPos)
    {
        if (_sim is null || _camera is null || _mapRenderer is null) return;

        // Convert screen position to world position using the canvas transform
        var worldPos = GetCanvasTransform().AffineInverse() * screenPos;

        // Find nearest entity to click position
        EntityId? nearest = null;
        var nearestDist = _mapRenderer.HexSize * 1.2f; // Click radius scales with hex size

        foreach (var (entityId, entity) in _sim.Graph.AllEntities)
        {
            if (entity.Coordinates is null || entity.Status != EntityStatus.Active) continue;

            var entityPixel = HexToPixel(entity.Coordinates.Value.Q, entity.Coordinates.Value.R, _mapRenderer.HexSize);
            var dist = worldPos.DistanceTo(entityPixel);

            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = entityId;
            }
        }

        if (nearest is not null)
        {
            _detailPanel?.SelectEntity(nearest.Value);
            GD.Print($"Selected: {_sim.Graph.GetEntity(nearest.Value)?.Name}");
        }
        else
        {
            _detailPanel?.ClearSelection();
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

        var line = $"[color={color}][{e.Tick}] {e.Description}[/color]\n";

        // Prepend (most recent first) -- but RichTextLabel doesn't support prepend easily,
        // so we'll append and the scroll container shows latest at bottom
        _eventLogLabel.AppendText(line);

        // Also print to console
        GD.Print($"[Tick {e.Tick}] {e.Type}: {e.Description}");
    }

    private void HandleCameraMovement(float delta)
    {
        if (_camera is null) return;

        var speed = 200f / _camera.Zoom.X;
        var direction = Vector2.Zero;

        if (Input.IsKeyPressed(Key.A))
            direction.X -= 1;
        if (Input.IsKeyPressed(Key.D))
            direction.X += 1;
        if (Input.IsKeyPressed(Key.W))
            direction.Y -= 1;
        if (Input.IsKeyPressed(Key.S))
            direction.Y += 1;

        if (direction != Vector2.Zero)
        {
            _camera.Position += direction.Normalized() * speed * delta;
        }
    }

    private static Vector2 HexToPixel(int q, int r, float hexSize)
    {
        var x = hexSize * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
        var y = hexSize * (1.5f * r);
        return new Vector2(x, y);
    }
}
