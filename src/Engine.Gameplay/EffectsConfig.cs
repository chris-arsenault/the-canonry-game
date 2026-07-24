namespace Engine.Gameplay;

using Engine.Constants;

public sealed class EffectsConfig
{
    public IReadOnlyList<AttributeDeltaEffectConfig> AttributeDeltaEffects { get; init; } = [];

    public IReadOnlyList<RelationshipTagRuleConfig> RelationshipTagRules { get; init; } = [];
}

/// <summary>
/// Discrete status rule: entities of a kind gain a tag while the summed
/// strength of their relationships of a kind crosses a threshold, and lose it
/// when the sum falls below the clear threshold (hysteresis). Entity-centric
/// state machines built from graph evidence, not scalar stocks.
/// </summary>
public sealed class RelationshipTagRuleConfig
{
    public required string Kind { get; init; }

    public required string Tag { get; init; }

    public required string RelationshipKind { get; init; }

    public EffectRelationshipDirection RelationshipDirection { get; init; } =
        EffectRelationshipDirection.Incoming;

    public required float SetAtOrAbove { get; init; }

    public required float ClearBelow { get; init; }

    /// <summary>
    /// Optional jurisdiction filter: the rule only sets the tag while the
    /// entity's position on <see cref="InsideCoveragePlane"/> is inside
    /// coverage owned by a living entity of this kind (and subtype, when
    /// given). Leaving the jurisdiction clears the tag. Violation as a
    /// state over existing primitives, not an interpreted event.
    /// </summary>
    public string? InsideCoverageOwnerKind { get; init; }

    public string? InsideCoverageOwnerSubtype { get; init; }

    public string InsideCoveragePlane { get; init; } = "semantic";
}

/// <summary>
/// Gameplay-authored per-tick attribute delta derived from live graph/map evidence.
/// Domain content supplies ids as names; gameplay compilation resolves them to kernel ids.
/// </summary>
public sealed class AttributeDeltaEffectConfig
{
    public required string Kind { get; init; }

    public required string Attribute { get; init; }

    public required EffectCondition Condition { get; init; }

    public string? RelationshipKind { get; init; }

    public EffectRelationshipDirection RelationshipDirection { get; init; } =
        EffectRelationshipDirection.Either;

    public string Plane { get; init; } = "physical";

    public int MaxDistance { get; init; }

    public float Threshold { get; init; }

    public float MinimumRelationshipStrength { get; init; } = 0.1f;

    public string? Culture { get; init; }

    public string? CultureSlot { get; init; }

    public EffectCultureSlotPolarity CultureSlotPolarity { get; init; } =
        EffectCultureSlotPolarity.Any;

    public required float DeltaPerTick { get; init; }

    /// <summary>
    /// For CrowdingBeyondCultureSupport: raw own-culture mass at which the
    /// crowding strain is halved. Strain = crowdingEvidence *
    /// scale / (scale + ownMass) — asymptotic, never a hard cap, and any
    /// future mechanism that raises local culture mass (or a later authored
    /// discount channel) escapes it. Steady-state design, not a limit.
    /// </summary>
    public float SupportScale { get; init; }

    /// <summary>
    /// For InsideOwnedCoverage: the owner kind (and optional subtype) whose
    /// coverage the entity must stand inside on the rule's plane.
    /// </summary>
    public string? CoverageOwnerKind { get; init; }

    public string? CoverageOwnerSubtype { get; init; }
}

public enum EffectCondition
{
    RelatedInfluenceAtLocation,
    RelatedEntityDistance,
    MapSupportAtLocation,
    MapThreatAtLocation,
    MapCrowdingLoad,
    MapCrowdingComfort,

    /// <summary>
    /// The inverse of crowding comfort: full when the entity stands alone,
    /// fading as local population approaches capacity, zero when crowded.
    /// The condition for things that wither without people around them.
    /// </summary>
    MapCrowdingSolitude,
    OwnCultureInfluenceAtLocation,
    CultureSlotOccupancy,
    CrowdingBeyondCultureSupport,
    InsideOwnedCoverage,
    BeyondCultureSupport,
}

public enum EffectRelationshipDirection
{
    Outgoing,
    Incoming,
    Either,
}

public enum EffectCultureSlotPolarity
{
    Any = -1,
    Positive = RelationshipPolarityCodes.Positive,
    Neutral = RelationshipPolarityCodes.Neutral,
    Negative = RelationshipPolarityCodes.Negative,
}
