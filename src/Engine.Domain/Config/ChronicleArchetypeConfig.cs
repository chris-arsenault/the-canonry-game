namespace Engine.Domain.Config;

/// <summary>
/// A chronicle archetype: cast (roles bound by generator-grade queries) and
/// an unordered pool of beats. Beats open on world or heat conditions, charge
/// the world while open (gambit bias, chronicle-granted gambits), and strike
/// when the cast acts or a recognized fact occurs. Terminal beats end the
/// chronicle by recognizing world facts; a cold, quiet chronicle fades. The
/// director never writes world state - every ending must be reachable through
/// ordinary gambits and generators.
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

    /// <summary>
    /// Convergence slots: roles that start empty and are filled when an
    /// entrance beat strikes - the drawn-in third party joins the cast.
    /// </summary>
    public IReadOnlyList<ChronicleOpenRoleConfig> OpenRoles { get; init; } = [];

    /// <summary>
    /// Dramatic stance of each role, e.g. "protagonist" -> "$hunter".
    /// Cross-story beats from the domain's shared library are written against
    /// stance names; a shared beat applies to this archetype only when every
    /// stance it references is mapped here.
    /// </summary>
    public IReadOnlyDictionary<string, string> Stances { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Archetype-specific beats, addressed by role refs.</summary>
    public IReadOnlyList<ChronicleBeatConfig> Beats { get; init; } = [];

    /// <summary>Outcome code recorded when the chronicle fades from the telling.</summary>
    public required string FadeOutcomeId { get; init; }

    /// <summary>Player-facing fade sentence template with role refs in braces.</summary>
    public string? FadeNarrative { get; init; }

    /// <summary>
    /// Player-facing title template. Role refs in braces are replaced with
    /// the cast member's reference name, e.g. "The Hunt of {$quarry}".
    /// Game-layer presentation only; never compiled into the kernel.
    /// </summary>
    public string? TitleTemplate { get; init; }

    /// <summary>Maximum concurrently active chronicles of this archetype.</summary>
    public int MaxActive { get; init; } = 4;

    /// <summary>Director pulses between casting attempts.</summary>
    public int CastingCadenceTicks { get; init; } = 40;
}

/// <summary>
/// One authored beat. Openings may reference world state and heat, never
/// another beat's strike - heat is the only narrative memory, which keeps the
/// pool from collapsing back into a scripted act order.
/// </summary>
public sealed class ChronicleBeatConfig
{
    /// <summary>Beat code surfaced on beat_struck telemetry rows.</summary>
    public required string Id { get; init; }

    /// <summary>
    /// Player-facing sentence template recorded when the beat strikes, with
    /// role refs (or stance names for shared beats) in braces.
    /// </summary>
    public string? Narrative { get; init; }

    /// <summary>Condition that opens the beat. Openings latch until struck.</summary>
    public required ChronicleConditionConfig Open { get; init; }

    /// <summary>What marks the beat as having happened. Kind None never strikes (charge-only).</summary>
    public ChronicleStrikeConfig Strike { get; init; } = new() { Kind = ChronicleStrikeKind.None };

    /// <summary>Heat added when the beat strikes.</summary>
    public float HeatGain { get; init; }

    /// <summary>When set, striking this beat ends the chronicle with this outcome.</summary>
    public string? TerminalOutcomeId { get; init; }

    /// <summary>Role-pair gambit score bonuses active while the beat is open.</summary>
    public IReadOnlyList<ChronicleChargeConfig> Charges { get; init; } = [];

    /// <summary>Chronicle-granted gambits available to roles while the beat is open.</summary>
    public IReadOnlyList<ChronicleGrantConfig> Grants { get; init; } = [];
}

public enum ChronicleConditionKind
{
    /// <summary>Holds from the moment the chronicle is cast.</summary>
    Always,

    /// <summary>A relationship of the kind exists between two roles at or above the strength.</summary>
    RolePairRelationship,

    /// <summary>No relationship of the kind at or above the strength exists between two roles.</summary>
    RolePairRelationshipAbsent,

    /// <summary>The role's entity has become historical.</summary>
    RoleHistorical,

    /// <summary>The role's attribute has fallen below the value.</summary>
    RoleAttributeBelow,

    /// <summary>The chronicle's heat is at or above the value.</summary>
    HeatAtLeast,

    /// <summary>The two roles are within the physical hex distance.</summary>
    RolePairWithinDistance,

    /// <summary>The two roles are beyond the physical hex distance.</summary>
    RolePairBeyondDistance,

    /// <summary>The role's attribute has risen to at least the value.</summary>
    RoleAttributeAtLeast,

    /// <summary>The chronicle's heat is below the value.</summary>
    HeatBelow,
}

public sealed class ChronicleConditionConfig
{
    public required ChronicleConditionKind Kind { get; init; }
    public string? RoleA { get; init; }
    public string? RoleB { get; init; }
    public string? RelationshipKind { get; init; }
    public string? Attribute { get; init; }
    public float Value { get; init; }
}

public enum ChronicleStrikeKind
{
    /// <summary>The beat never strikes; it only charges while open.</summary>
    None,

    /// <summary>A gambit executed by one role against another (any outcome).</summary>
    RolePairGambit,

    /// <summary>A gambit executed by a non-cast entity against a role.</summary>
    OutsiderGambitOnRole,

    /// <summary>A recognized world/heat fact, evaluated on the director pulse.</summary>
    StateFact,

    /// <summary>The role executes the named gambit against any target.</summary>
    RoleGambit,

    /// <summary>A generator fires with a role as its selected target.</summary>
    GeneratorOnRole,
}

public sealed class ChronicleStrikeConfig
{
    public required ChronicleStrikeKind Kind { get; init; }
    public string? ActorRole { get; init; }
    public string? TargetRole { get; init; }

    /// <summary>Restrict execution strikes to one gambit id; null accepts any gambit.</summary>
    public string? Gambit { get; init; }

    /// <summary>The generator whose fire strikes, for GeneratorOnRole strikes; null accepts any.</summary>
    public string? Generator { get; init; }

    /// <summary>
    /// Failed attempts strike too. Only safe for outcome-neutral narratives
    /// ("blow answers blow"); a narrative that claims the deed succeeded must
    /// never accept failure.
    /// </summary>
    public bool AcceptFailure { get; init; }

    /// <summary>
    /// Entrance: the outside party of this strike (the outsider acting on a
    /// role, or the outside target of a role's deed) joins the named open
    /// role. The beat only strikes when a qualified entrant joins.
    /// </summary>
    public string? JoinsRole { get; init; }

    /// <summary>The recognized fact, for StateFact strikes.</summary>
    public ChronicleConditionConfig? Fact { get; init; }
}

public sealed class ChronicleChargeConfig
{
    /// <summary>Role whose gambit scores are charged when acting…</summary>
    public required string ActorRole { get; init; }

    /// <summary>…against this role's entity.</summary>
    public required string TargetRole { get; init; }

    /// <summary>Restrict the charge to one gambit id; null charges any gambit between the pair.</summary>
    public string? Gambit { get; init; }

    public required float ScoreBonus { get; init; }

    /// <summary>Scale the bonus with the chronicle's heat - convergence pushing toward endings.</summary>
    public bool HeatScaled { get; init; }
}

public sealed class ChronicleGrantConfig
{
    public required string Role { get; init; }
    public required string Gambit { get; init; }
}

/// <summary>An open cast slot filled by an entrance beat, not by casting.</summary>
public sealed class ChronicleOpenRoleConfig
{
    public required string Ref { get; init; }
    public required EntityKind Kind { get; init; }
}
