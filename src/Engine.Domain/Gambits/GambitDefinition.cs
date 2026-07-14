namespace Engine.Gambits;

using Engine.Behaviors;
using Engine.Kinds;

/// <summary>
/// A high-stakes entity undertaking. Gambits run in their own program layer so
/// their scoring and risk model can diverge from routine behaviors over time.
/// </summary>
public sealed class GambitDefinition : BehaviorDefinition
{
    public GambitDefinition(
        string id,
        string name,
        EntityKind targetKind,
        IPrecondition precondition,
        IExpression scoreExpression)
        : base(id, name, targetKind, precondition, scoreExpression)
    {
    }

    /// <summary>
    /// Base chance that a selected gambit succeeds. Failure handling is currently
    /// deferred; the runtime treats successful selection as execution.
    /// </summary>
    public float SuccessChance { get; set; } = 1f;

    /// <summary>
    /// Chronicle-granted gambits are inert unless an open beat of a running
    /// chronicle grants them to the acting entity's role.
    /// </summary>
    public bool ChronicleGranted { get; set; }

    /// <summary>Authoring-time description of the gambit's intent.</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>Stable primitive gambit identity used by compiled runtime programs.</summary>
public readonly record struct GambitRuntimeId(int Value)
{
    public static readonly GambitRuntimeId None = new(0);

    public bool IsNone => Value == 0;
}
