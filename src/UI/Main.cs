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
    private int _frameCount;

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

        // Subscribe to events
        _sim.EventLog.EventEmitted += (_, e) => GD.Print($"[Tick {e.Tick}] {e.Type}: {e.Description}");
        _sim.Lifecycle.EntityDied += (_, e) => GD.Print($"DEATH: {e.Entity.Name} ({e.Entity.Kind}) at tick {e.DeathTick}");

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

        // Detail panel on the right side
        var marginContainer = new MarginContainer();
        marginContainer.SetAnchorsPreset(Control.LayoutPreset.RightWide);
        marginContainer.AddThemeConstantOverride("margin_top", 10);
        marginContainer.AddThemeConstantOverride("margin_right", 10);
        marginContainer.AddThemeConstantOverride("margin_bottom", 10);
        _uiLayer.AddChild(marginContainer);

        _detailPanel = new EntityDetailPanel();
        _detailPanel.Initialize(_sim);
        marginContainer.AddChild(_detailPanel);

        GD.Print("Controls: Space=pause, 1/2/3=speed, WASD/arrows=pan, scroll=zoom, click=select entity");
    }

    public override void _Process(double delta)
    {
        if (_sim is null) return;

        var ticksProcessed = _sim.AdvanceFrame();

        // Refresh displays
        if (ticksProcessed > 0)
        {
            _mapRenderer?.Refresh();
            _detailPanel?.Refresh();
        }

        // Camera movement
        HandleCameraMovement((float)delta);

        // Log summary every 120 frames (~2 seconds at 60fps)
        _frameCount++;
        if (_frameCount % 120 == 0 && ticksProcessed > 0)
        {
            LogSimulationState();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_sim is null) return;

        if (@event is InputEventKey { Pressed: true } key)
        {
            switch (key.Keycode)
            {
                case Key.Space:
                    _sim.Clock.Speed = _sim.Clock.Speed == SimulationSpeed.Paused
                        ? SimulationSpeed.Normal
                        : SimulationSpeed.Paused;
                    GD.Print($"Speed: {_sim.Clock.Speed}");
                    break;
                case Key.Key1:
                    _sim.Clock.Speed = SimulationSpeed.Normal;
                    GD.Print("Speed: Normal (1x)");
                    break;
                case Key.Key2:
                    _sim.Clock.Speed = SimulationSpeed.Fast;
                    GD.Print("Speed: Fast (2x)");
                    break;
                case Key.Key3:
                    _sim.Clock.Speed = SimulationSpeed.VeryFast;
                    GD.Print("Speed: VeryFast (4x)");
                    break;
            }
        }

        // Zoom with mouse wheel
        if (@event is InputEventMouseButton { Pressed: true } mouse && _camera is not null)
        {
            var zoomFactor = 1.1f;
            if (mouse.ButtonIndex == MouseButton.WheelUp)
            {
                _camera.Zoom *= zoomFactor;
            }
            else if (mouse.ButtonIndex == MouseButton.WheelDown)
            {
                _camera.Zoom /= zoomFactor;
            }

            _camera.Zoom = new Vector2(
                Mathf.Clamp(_camera.Zoom.X, 0.3f, 5f),
                Mathf.Clamp(_camera.Zoom.Y, 0.3f, 5f));

            // Left click to select entity
            if (mouse.ButtonIndex == MouseButton.Left)
            {
                HandleEntityClick(mouse.GlobalPosition);
            }
        }
    }

    private void HandleEntityClick(Vector2 screenPos)
    {
        if (_sim is null || _camera is null || _mapRenderer is null) return;

        // Convert screen position to world position
        var worldPos = _camera.Position + (screenPos - GetViewportRect().Size / 2f) / _camera.Zoom;

        // Find nearest entity to click position
        EntityId? nearest = null;
        var nearestDist = 20f; // Click radius in pixels

        foreach (var (entityId, entity) in _sim.Graph.AllEntities)
        {
            if (entity.Coordinates is null || entity.Status != EntityStatus.Active) continue;

            var hexSize = _mapRenderer.HexSize;
            var entityPixel = HexToPixel(entity.Coordinates.Value.Q, entity.Coordinates.Value.R, hexSize);
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
            var name = _sim.Graph.GetEntity(nearest.Value)?.Name;
            GD.Print($"Selected: {name}");
        }
        else
        {
            _detailPanel?.ClearSelection();
        }
    }

    private static Vector2 HexToPixel(int q, int r, float hexSize)
    {
        var x = hexSize * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
        var y = hexSize * (1.5f * r);
        return new Vector2(x, y);
    }

    private void HandleCameraMovement(float delta)
    {
        if (_camera is null) return;

        var speed = 200f / _camera.Zoom.X; // Faster when zoomed out
        var direction = Vector2.Zero;

        if (Input.IsActionPressed("ui_left") || Input.IsKeyPressed(Key.A))
            direction.X -= 1;
        if (Input.IsActionPressed("ui_right") || Input.IsKeyPressed(Key.D))
            direction.X += 1;
        if (Input.IsActionPressed("ui_up") || Input.IsKeyPressed(Key.W))
            direction.Y -= 1;
        if (Input.IsActionPressed("ui_down") || Input.IsKeyPressed(Key.S))
            direction.Y += 1;

        if (direction != Vector2.Zero)
        {
            _camera.Position += direction.Normalized() * speed * delta;
        }
    }

    private void LogSimulationState()
    {
        if (_sim is null) return;

        var tick = _sim.Clock.CurrentTick;
        var activeNpcs = _sim.Graph.GetEntitiesByKind(EntityKind.Npc)
            .Count(id => _sim.Graph.GetEntity(id)?.Status == EntityStatus.Active);
        var activeSettlements = _sim.Graph.GetEntitiesByKind(EntityKind.Settlement)
            .Count(id => _sim.Graph.GetEntity(id)?.Status == EntityStatus.Active);

        GD.Print($"[Tick {tick}] NPCs: {activeNpcs} | Settlements: {activeSettlements} | Relationships: {_sim.Graph.ActiveRelationshipCount}");
    }
}
