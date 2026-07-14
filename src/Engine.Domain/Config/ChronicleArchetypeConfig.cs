namespace Engine.Domain.Config;

using Engine.Graph;

/// <summary>
/// A story archetype: the forward-running counterpart of a chronicle plan.
/// The domain authors cast (roles bound by generator-grade queries), plot
/// (acts advancing on role-state evidence with deadlines), and direction
/// (role-pair gambit bias and cast sustain). Resolution is guaranteed: the
/// final act always resolves by deadline if no condition fires first.
/// </summary>
public sealed class ChronicleArchetypeConfig
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }

    /// <summary>Casting selector for the primary role (role index 0).</summary>
    public required SemanticGeneratorTarget PrimaryRole { get; init; }

    /// <summary>Primary role ref, e.g. "$quarry".</summary>
    public required string PrimaryRoleRef { get; init; }

    /// <summary>
    /// Supporting roles, bound like generator secondary targets (queries may
    /// use RelatedToTarget against the primary role's entity).
    /// </summary>
    public IReadOnlyList<SemanticGeneratorBindingTarget> SupportingRoles { get; init; } = [];

    public required IReadOnlyList<ChronicleActConfig> Acts { get; init; }

    /// <summary>
    /// Weighted endings evaluated at resolution. The first row whose
    /// condition holds is preferred; otherwise weights decide.
    /// </summary>
    public required IReadOnlyList<ChronicleResolutionConfig> Resolutions { get; init; }

    /// <summary>
    /// Player-facing title template. Role refs in braces are replaced with
    /// the cast member's reference name, e.g. "The Hunt of {$quarry}".
    /// Game-layer presentation only; never compiled into the kernel.
    /// </summary>
    public string? TitleTemplate { get; init; }

    /// <summary>Player-facing act names, parallel to Acts.</summary>
    public IReadOnlyList<string> ActNames { get; init; } = [];

    /// <summary>Maximum concurrently active stories of this archetype.</summary>
    public int MaxActive { get; init; } = 4;

    /// <summary>Director pulses between casting attempts.</summary>
    public int CastingCadenceTicks { get; init; } = 40;
}

public sealed class ChronicleActConfig
{
    /// <summary>Main ticks before the act force-advances (final act: resolves).</summary>
    public required int DeadlineTicks { get; init; }

    /// <summary>Optional early-advance condition over role state.</summary>
    public ChronicleConditionConfig? AdvanceWhen { get; init; }

    /// <summary>Role-pair gambit score bias active during this act.</summary>
    public IReadOnlyList<ChronicleBiasConfig> Bias { get; init; } = [];

    /// <summary>Per-pulse attribute sustain on cast members during this act.</summary>
    public IReadOnlyList<ChronicleSustainConfig> Sustain { get; init; } = [];
}

public enum ChronicleConditionKind
{
    /// <summary>A relationship of the kind exists between two roles at or above the strength.</summary>
    RolePairRelationship,

    /// <summary>The role's entity has become historical.</summary>
    RoleHistorical,
}

public sealed class ChronicleConditionConfig
{
    public required ChronicleConditionKind Kind { get; init; }
    public required string RoleA { get; init; }
    public string? RoleB { get; init; }
    public string? RelationshipKind { get; init; }
    public float MinStrength { get; init; } = 0.1f;
}

public sealed class ChronicleBiasConfig
{
    /// <summary>Role whose gambit scores are biased when acting…</summary>
    public required string ActorRole { get; init; }

    /// <summary>…against this role's entity.</summary>
    public required string TargetRole { get; init; }

    /// <summary>Restrict the bias to one gambit id; null biases any gambit between the pair.</summary>
    public string? Gambit { get; init; }

    public required float ScoreBonus { get; init; }
}

public sealed class ChronicleSustainConfig
{
    public required string Role { get; init; }
    public required string Attribute { get; init; }
    public required float DeltaPerPulse { get; init; }
}

public sealed class ChronicleResolutionConfig
{
    /// <summary>Outcome code surfaced on the story_resolved provenance/telemetry row.</summary>
    public required string OutcomeId { get; init; }

    /// <summary>Preferred when this condition holds at resolution time.</summary>
    public ChronicleConditionConfig? When { get; init; }

    public int Weight { get; init; } = 1;

    /// <summary>
    /// Player-facing outcome sentence template with role refs in braces,
    /// e.g. "{$quarry} fell to {$hunter}. The hunt is fulfilled."
    /// </summary>
    public string? Narrative { get; init; }

    /// <summary>Relationship writes applied between roles at resolution.</summary>
    public IReadOnlyList<ChronicleResolutionRelationshipConfig> Relationships { get; init; } = [];

    /// <summary>Roles marked historical at resolution (a death that ends the tale).</summary>
    public IReadOnlyList<string> MarkHistorical { get; init; } = [];
}

public sealed class ChronicleResolutionRelationshipConfig
{
    public required string SourceRole { get; init; }
    public required string TargetRole { get; init; }
    public required string RelationshipKind { get; init; }
    public required float Strength { get; init; }
    public required RelationshipPolarity Polarity { get; init; }
    public bool CullInstead { get; init; }
}
