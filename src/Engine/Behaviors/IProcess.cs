namespace Engine.Behaviors;

using Engine.Graph;
using Engine.Messages;

/// <summary>
/// A mundane, always-available activity an entity can perform.
/// Processes are the entity's daily life: eating, moving, resting, harvesting.
/// One process is selected per entity per tick via utility AI weighted random.
/// Processes are NOT card-gated. All entity kinds have process menus.
/// </summary>
public interface IProcess
{
    /// <summary>Human-readable name.</summary>
    string Name { get; }

    /// <summary>
    /// Can this process run right now? Checked before scoring.
    /// Returns false if preconditions aren't met (e.g., "eat" requires food at location).
    /// </summary>
    bool CanExecute(Entity entity, IWorldState worldState);

    /// <summary>
    /// How much does this entity want to do this? Higher = more likely selected.
    /// Range [0, 1]. All viable processes compete via weighted random.
    /// </summary>
    float Score(Entity entity, IWorldState worldState);

    /// <summary>
    /// Execute this process: apply multi-effects to the world.
    /// Returns a message describing what happened (for the message bus).
    /// </summary>
    SimulationMessage Execute(Entity entity, IWorldState worldState);
}

/// <summary>
/// A significant, card-gated action an entity can perform.
/// Decisions are story-worthy: expanding borders, wielding artifacts, forging alliances.
/// One decision is selected per entity per tick via utility AI weighted random,
/// but only from decisions enabled by active cards in the tableau.
/// All entity kinds have decision menus. Produces event log entries.
/// </summary>
public interface IDecision
{
    /// <summary>Human-readable name.</summary>
    string Name { get; }

    /// <summary>ID of the card that enables this decision. Null for base (always-available) decisions.</summary>
    string? CardId { get; }

    /// <summary>
    /// Can this decision execute right now? Checked before scoring.
    /// Includes card-gate check (is the enabling card in the tableau?).
    /// </summary>
    bool CanExecute(Entity entity, IWorldState worldState);

    /// <summary>
    /// How much does this entity want to do this? Higher = more likely selected.
    /// Range [0, 1]. All viable decisions compete via weighted random.
    /// </summary>
    float Score(Entity entity, IWorldState worldState);

    /// <summary>
    /// Execute this decision: apply significant effects to the world.
    /// Returns a message describing what happened (for the message bus + event log).
    /// </summary>
    SimulationMessage Execute(Entity entity, IWorldState worldState);
}
