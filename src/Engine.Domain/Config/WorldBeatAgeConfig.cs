namespace Engine.Domain.Config;

/// <summary>The three regime tracks an age can occupy.</summary>
public enum WorldBeatTrack
{
    /// <summary>The physical regime of the land itself: thaw, hard winter, the breaking.</summary>
    Ice = 0,

    /// <summary>The social regime among the living kinds: striving, accords, wandering.</summary>
    Peoples = 1,

    /// <summary>The regime of the semantic world: signs, canons, the quiet.</summary>
    Memory = 2,
}

public enum WorldBeatFact
{
    TicksSinceStart,
    ActiveEntitiesOfKind,
    LivingRelationshipsOfKind,
    CulledRelationshipsOfKind,
    TotalCultureRawMass,
    CultureMassGrowthPermille,
    LegendaryResolutions,
}

public enum WorldBeatComparator
{
    AtLeast,
    Below,
}

public enum WorldBeatChargeDomain
{
    GeneratorPulseWeight,
    VerbWeight,
    GambitScore,
    ContestDecayTide,
}

/// <summary>A macro-fact predicate an age's opening or closing is recognized from.</summary>
public sealed record WorldBeatConditionConfig(
    WorldBeatFact Fact,
    WorldBeatComparator Comparator,
    float Value,
    string? Kind = null,
    string? RelationshipKind = null);

/// <summary>A rate modulation an open age applies to existing authored content. Null target charges the whole domain.</summary>
public sealed record WorldBeatChargeConfig(
    WorldBeatChargeDomain Domain,
    float Multiplier,
    string? Target = null);

/// <summary>
/// An authored Age: the beat treatment applied to the world itself. An age
/// opens on its track when every opening condition is recognized from macro
/// world state and closes when any closing condition holds (after its minimum
/// span) - never on timers. While open, its charges modulate the rates of
/// existing authored content; the framework writes no world state.
/// </summary>
public sealed class WorldBeatAgeConfig
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required WorldBeatTrack Track { get; init; }

    /// <summary>When several ages of a track are eligible, the highest priority opens.</summary>
    public int Priority { get; init; }

    /// <summary>Minimum span before closing conditions are consulted (raw ticks).</summary>
    public int MinOpenTicks { get; init; } = 2000;

    /// <summary>Span after closing during which this age cannot reopen (raw ticks).</summary>
    public int CooldownTicks { get; init; } = 2000;

    /// <summary>All must hold to open. Empty means always eligible (the track's fallback).</summary>
    public IReadOnlyList<WorldBeatConditionConfig> OpenWhen { get; init; } = [];

    /// <summary>Any holding closes the age. Empty means the age yields only to nothing - use for fallbacks.</summary>
    public IReadOnlyList<WorldBeatConditionConfig> CloseWhen { get; init; } = [];

    public IReadOnlyList<WorldBeatChargeConfig> Charges { get; init; } = [];
}
