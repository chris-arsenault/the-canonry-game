namespace Engine.Gameplay;

/// <summary>
/// Gameplay-authored relationship drift rule. Relationship kinds remain domain
/// graph vocabulary; maintenance rules are reusable runtime behavior.
/// </summary>
public sealed class RelationshipMaintenanceRuleConfig
{
    public required RelationshipMaintenanceCondition Condition { get; init; }
    public string? Plane { get; init; }
    public int MaxDistance { get; init; }
    public float Threshold { get; init; }
    public required float Delta { get; init; }
}

public enum RelationshipMaintenanceCondition
{
    Always,
    SameCulture,
    DifferentCulture,
    DistanceWithin,
    CoPresence,
    SourceInfluenceAtTarget,
    TargetInfluenceAtSource,
    ContactMemory,
}
