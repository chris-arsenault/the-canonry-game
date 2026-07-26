namespace Engine.Domain.Config;

/// <summary>The three regime tracks an age can occupy.</summary>
public enum WorldBeatTrack
{
    /// <summary>The Berg's own condition: the seasons and the rare physical/cosmic breaks.</summary>
    World = 0,

    /// <summary>What the three peoples are doing: the everyday activities and the epochal eras.</summary>
    Peoples = 1,
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
    GeneratorFireWeight,
    VerbWeight,
    GambitScore,
    ContestDecayTide,
    GlobalDecayTide,
    GambitSuccess,
    MovementStepRate,
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

    /// <summary>Minimum tick span before closing conditions are consulted.</summary>
    public int MinOpenTicks { get; init; } = 400;

    /// <summary>Tick span after closing during which this age cannot reopen.</summary>
    public int CooldownTicks { get; init; } = 400;

    /// <summary>All must hold to open. Empty means always eligible (the track's fallback).</summary>
    public IReadOnlyList<WorldBeatConditionConfig> OpenWhen { get; init; } = [];

    /// <summary>Any holding closes the age. Empty means the age yields only to nothing - use for fallbacks.</summary>
    public IReadOnlyList<WorldBeatConditionConfig> CloseWhen { get; init; } = [];

    public IReadOnlyList<WorldBeatChargeConfig> Charges { get; init; } = [];
}
