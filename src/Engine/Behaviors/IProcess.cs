namespace Engine.Behaviors;

using Engine.Graph;
using Engine.Messages;

/// <summary>
/// A mundane, always-available activity an entity can perform.
/// One process is selected per entity per tick via utility AI weighted random.
/// All entity kinds have process menus.
/// </summary>
public interface IProcess
{
    string Name { get; }

    /// <summary>Can this process run against the given target?</summary>
    bool CanExecute(Entity entity, IWorldState worldState, EntityId? targetId);

    /// <summary>Utility score. Higher = more likely selected.</summary>
    float Score(Entity entity, IWorldState worldState);

    /// <summary>Execute: apply mutations. Target is the goal's provider.</summary>
    SimulationMessage Execute(Entity entity, IWorldState worldState, EntityId? targetId);
}

/// <summary>
/// A significant, card-gated action an entity can perform.
/// </summary>
public interface IDecision
{
    string Name { get; }
    string? CardId { get; }
    bool CanExecute(Entity entity, IWorldState worldState);
    float Score(Entity entity, IWorldState worldState);
    SimulationMessage Execute(Entity entity, IWorldState worldState);
}
