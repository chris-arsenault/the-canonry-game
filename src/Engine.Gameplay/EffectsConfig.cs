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
}

public enum EffectCondition
{
    RelatedInfluenceAtLocation,
    RelatedEntityDistance,
    MapSupportAtLocation,
    MapThreatAtLocation,
    MapCrowdingLoad,
    MapCrowdingComfort,
    OwnCultureInfluenceAtLocation,
    CultureSlotOccupancy,
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
