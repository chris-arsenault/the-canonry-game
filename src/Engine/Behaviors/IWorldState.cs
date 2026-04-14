namespace Engine.Behaviors;

using Engine.Graph;

/// <summary>
/// Read-only view of the world state provided to process/decision scoring and execution.
/// Keeps behaviors decoupled from specific system implementations.
/// </summary>
public interface IWorldState
{
    WorldGraph Graph { get; }
    Engine.Attributes.AttributeStore Attributes { get; }
    Engine.Attributes.EffectSystem Effects { get; }
    Engine.Map.HexGrid Grid { get; }
    Engine.Map.RegionMap RegionMap { get; }
    Engine.Map.SpatialIndex SpatialIndex { get; }
    Engine.Resources.ResourceSystem? Resources { get; }
    int CurrentTick { get; }
}
