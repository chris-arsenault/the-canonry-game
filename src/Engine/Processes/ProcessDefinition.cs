namespace Engine.Processes;

using Engine.Cards;
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
    private string? _enablingCardId;

    public string Id { get; }
    public ProcessRuntimeId RuntimeId { get; }
    public string Name { get; set; }
    public DefinitionRole Role { get; }

    /// <summary>Which entity kind this process applies to.</summary>
    public EntityKind TargetKind { get; }

    /// <summary>For decisions: which card enables this. Null for processes (always available).</summary>
    public string? EnablingCardId
    {
        get => _enablingCardId;
        set
        {
            _enablingCardId = value;
            EnablingCardRuntimeId = string.IsNullOrEmpty(value)
                ? null
                : CardRuntimeId.FromStableName(value);
        }
    }

    public CardRuntimeId? EnablingCardRuntimeId { get; private set; }

    /// <summary>Precondition: can this fire right now?</summary>
    public IPrecondition Precondition { get; set; }

    /// <summary>Scoring expression for utility AI. Higher = more likely selected.</summary>
    public IExpression ScoreExpression { get; set; }

    /// <summary>Estimates the reward of acting on a provider. Evaluated with self + candidate
    /// as target. Absolute value — not normalized. Goal weight = urgency * reward.
    /// Required for all processes.</summary>
    public IExpression RewardExpression { get; set; } = new Literal(1f);

    /// <summary>Mutations applied when this process/decision executes.</summary>
    public System.Collections.ObjectModel.Collection<IMutation> Mutations { get; } = [];

    /// <summary>Minimum ticks between executions of this process per entity.
    /// 0 = no cooldown. Applied with jitter (±25%).</summary>
    public int Cooldown { get; set; }

    /// <summary>Minimum ticks between ANY process execution for the entity.
    /// 0 = no entity cooldown. Shared across all processes.</summary>
    public int EntityCooldown { get; set; }

    /// <summary>Wiring contract: what this process reads from the simulation.</summary>
    public HashSet<AttributeRef> Reads { get; } = [];

    /// <summary>Wiring contract: what this process writes to the simulation.</summary>
    public HashSet<AttributeRef> Writes { get; } = [];

    /// <summary>
    /// Planner contract: the goal effects this process can satisfy.
    /// Primary goals are usually self-scoped; sub-goals may target another entity.
    /// </summary>
    public HashSet<AttributeRef> GoalEffects { get; } = [];

    /// <summary>
    /// Planner contract: refs used to enumerate external provider/target candidates.
    /// Empty means the process is self-directed and should only consider self.
    /// </summary>
    public HashSet<AttributeRef> ProviderSelectors { get; } = [];

    /// <summary>
    /// Planner contract: support opportunities that can improve this process's
    /// downstream utility even when the process is not hard-blocked.
    /// Example: a settlement can grow now, but irrigating first would improve
    /// local food and make growth substantially better.
    /// </summary>
    public System.Collections.ObjectModel.Collection<PlannerSupportRule> SupportRules { get; } = [];


    public ProcessDefinition(
        string id,
        string name,
        DefinitionRole role,
        EntityKind targetKind,
        IPrecondition precondition,
        IExpression scoreExpression)
    {
        Id = id;
        RuntimeId = ProcessRuntimeId.FromStableName(id);
        Name = name;
        Role = role;
        TargetKind = targetKind;
        Precondition = precondition;
        ScoreExpression = scoreExpression;
    }
}
