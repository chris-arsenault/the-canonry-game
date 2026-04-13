using System.Linq;

using Godot;

using Engine.Clock;
using Engine.Graph;
using Engine.Kinds;
using Engine.Simulation;

namespace TheCanonryGame.UI;

public partial class Main : Node2D
{
    private SimulationRunner? _sim;
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
    }

    public override void _Process(double delta)
    {
        if (_sim is null) return;

        var ticksProcessed = _sim.AdvanceFrame();

        // Log summary every 60 frames (~1 second at 60fps)
        _frameCount++;
        if (_frameCount % 60 == 0 && ticksProcessed > 0)
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
