namespace Engine.Behaviors;

using Engine.Graph;
using Engine.Resources;
using Engine.WorldGen;

/// <summary>
/// Read-only view of the world state provided to process/gambit scoring and execution.
/// Keeps behaviors decoupled from specific system implementations.
/// </summary>
public interface IWorldState
{
    WorldGraph Graph { get; }
    HexGrid Grid { get; }
    MapCoverageMap CoverageMap { get; }
    SpatialIndex SpatialIndex { get; }
    IWorldResourceReader? Resources { get; }
    int CurrentTick { get; }
}

public interface IWorldResourceReader
{
    float GetStockpile(ResourceType resourceType, HexCoord coord);
}

public interface IWorldVersionSource
{
    long WorldVersion { get; }
}

public interface IWorldVersionSink : IWorldVersionSource
{
    void AdvanceWorldVersion();
}
