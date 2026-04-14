namespace Engine.Processes;

using Engine.Kinds;
using Engine.Wiring;

/// <summary>
/// The role of a definition: how it's registered and when it can fire.
/// </summary>
public enum DefinitionRole
{
    /// <summary>Always available. One selected per entity per tick via utility AI.</summary>
    Process,

    /// <summary>Available only when a card enables it. One selected per entity per tick.</summary>
    Decision,
}

/// <summary>
/// A data-defined process or decision. Same DSL as cards (preconditions + mutations)
/// but with a scoring expression for utility AI and entity kind targeting.
/// No C# code needed -- the engine interprets this data structure.
/// </summary>
public sealed class ProcessDefinition
{
    public string Id { get; }
    public string Name { get; set; }
    public DefinitionRole Role { get; }

    /// <summary>Which entity kind this process applies to.</summary>
    public EntityKind TargetKind { get; }

    /// <summary>For decisions: which card enables this. Null for processes (always available).</summary>
    public string? EnablingCardId { get; set; }

    /// <summary>Precondition: can this fire right now?</summary>
    public IPrecondition Precondition { get; set; }

    /// <summary>Scoring expression for utility AI. Higher = more likely selected.</summary>
    public IScoreExpression ScoreExpression { get; set; }

    /// <summary>Mutations applied when this process/decision executes.</summary>
    public System.Collections.ObjectModel.Collection<IMutation> Mutations { get; } = [];

    /// <summary>Human-readable description template. {entity.name} etc replaced at runtime.</summary>
    public string DescriptionTemplate { get; set; } = "{name} performed {process}";

    /// <summary>Wiring contract: what this process reads from the simulation.</summary>
    public HashSet<AttributeRef> Reads { get; } = [];

    /// <summary>Wiring contract: what this process writes to the simulation.</summary>
    public HashSet<AttributeRef> Writes { get; } = [];


    public ProcessDefinition(
        string id,
        string name,
        DefinitionRole role,
        EntityKind targetKind,
        IPrecondition precondition,
        IScoreExpression scoreExpression)
    {
        Id = id;
        Name = name;
        Role = role;
        TargetKind = targetKind;
        Precondition = precondition;
        ScoreExpression = scoreExpression;
    }
}
