namespace Engine.Behaviors;

using Engine.Kinds;
using Engine.Wiring;

/// <summary>
/// Shared data-driven behavior contract used by low-altitude behaviors and
/// higher-stakes gambits. Runtime scheduling semantics live in their programs.
/// </summary>
public abstract class BehaviorDefinition
{
    protected BehaviorDefinition(
        string id,
        string name,
        EntityKind targetKind,
        IPrecondition precondition,
        IExpression scoreExpression)
    {
        Id = id;
        Name = name;
        TargetKind = targetKind;
        Precondition = precondition;
        ScoreExpression = scoreExpression;
    }

    public string Id { get; }
    public string Name { get; set; }

    /// <summary>Which entity kind this behavior applies to.</summary>
    public EntityKind TargetKind { get; }

    /// <summary>Precondition: can this fire right now?</summary>
    public IPrecondition Precondition { get; set; }

    /// <summary>Scoring expression for utility AI. Higher = more likely selected.</summary>
    public IExpression ScoreExpression { get; set; }

    /// <summary>Mutations applied when this behavior executes.</summary>
    public System.Collections.ObjectModel.Collection<IMutation> Mutations { get; } = [];

    /// <summary>Minimum ticks between executions of this behavior per entity.</summary>
    public int Cooldown { get; set; }

    /// <summary>Minimum ticks between any behavior execution for the entity in this program.</summary>
    public int EntityCooldown { get; set; }

    /// <summary>Wiring contract: what this behavior reads from the simulation.</summary>
    public HashSet<AttributeRef> Reads { get; } = [];

    /// <summary>Wiring contract: what this behavior writes to the simulation.</summary>
    public HashSet<AttributeRef> Writes { get; } = [];

    /// <summary>Program contract: refs used to enumerate external provider/target candidates.</summary>
    public HashSet<AttributeRef> ProviderSelectors { get; } = [];

    public int WorkCommitmentTicks { get; set; }
}
