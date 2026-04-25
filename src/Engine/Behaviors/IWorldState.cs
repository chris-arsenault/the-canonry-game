namespace Engine.Behaviors;

using Engine.Graph;

/// <summary>
/// Read-only view of the world state provided to process/decision scoring and execution.
/// Keeps behaviors decoupled from specific system implementations.
/// </summary>
public interface IWorldState
{
    WorldGraph Graph { get; }
    Engine.Attributes.EffectSystem Effects { get; }
    Engine.Pressure.PressureSystem? Pressure { get; }
    Engine.Map.HexGrid Grid { get; }
    Engine.Map.MapCoverageMap CoverageMap { get; }
    Engine.Map.SpatialIndex SpatialIndex { get; }
    Engine.Resources.ResourceSystem? Resources { get; }
    int CurrentTick { get; }
}

public interface IWorldVersionSource
{
    long WorldVersion { get; }
}

public interface IWorldVersionSink : IWorldVersionSource
{
    void AdvanceWorldVersion();
}
