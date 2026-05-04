namespace Engine.Gameplay;

/// <summary>
/// Reusable operation authored on a trait. Individual trait ids should stay in
/// domain data; runtime systems interpret these primitives generically.
/// </summary>
public interface ITraitOperationConfig
{
    TraitOperationKind Kind { get; }
}

public enum TraitOperationKind
{
    AttributeDelta,
    LowAltitudeWeight,
    GambitModifier,
}

public enum TraitEffectBearer
{
    Target,
    RelatedEntity,
    Actor,
    GambitTarget,
}

public sealed class TraitAttributeDeltaOperationConfig : ITraitOperationConfig
{
    public TraitOperationKind Kind => TraitOperationKind.AttributeDelta;

    public required string TargetKind { get; init; }

    public required string Attribute { get; init; }

    public required EffectCondition Condition { get; init; }

    public TraitEffectBearer TraitBearer { get; init; } = TraitEffectBearer.Target;

    public string? RelationshipKind { get; init; }

    public EffectRelationshipDirection RelationshipDirection { get; init; } =
        EffectRelationshipDirection.Either;

    public string Plane { get; init; } = "physical";

    public int MaxDistance { get; init; }

    public float Threshold { get; init; }

    public float MinimumRelationshipStrength { get; init; } = 0.1f;

    public required float DeltaPerTick { get; init; }
}

public sealed class TraitLowAltitudeWeightOperationConfig : ITraitOperationConfig
{
    public TraitOperationKind Kind => TraitOperationKind.LowAltitudeWeight;

    /// <summary>Optional verb id. Null means all low-altitude verbs for the trait owner's kind.</summary>
    public string? Verb { get; init; }

    public float WeightDelta { get; init; }

    public float ExploreWeightDelta { get; init; }

    public float StalenessWeightDelta { get; init; }

    public float HotspotWeightDelta { get; init; }

    public float CrowdingWeightDelta { get; init; }

    public float AffordanceWeightDelta { get; init; }

    public float RelationshipAffinityWeightDelta { get; init; }
}

public sealed class TraitGambitModifierOperationConfig : ITraitOperationConfig
{
    public TraitOperationKind Kind => TraitOperationKind.GambitModifier;

    public required string Gambit { get; init; }

    public TraitEffectBearer TraitBearer { get; init; } = TraitEffectBearer.Actor;

    public float ScoreDelta { get; init; }

    public float SuccessChanceDelta { get; init; }
}

public sealed record TraitOperationDefinition(
    string OwnerKind,
    string Trait,
    ITraitOperationConfig Operation);
