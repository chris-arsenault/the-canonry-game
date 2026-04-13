namespace Engine.Kinds;

using Engine.Graph;

/// <summary>
/// Defines the rules for a type of relationship. Relationship kinds are domain-defined
/// but validated by the engine using category-level constraints.
/// </summary>
public sealed class RelationshipKindDefinition
{
    public string Name { get; }
    public RelationshipPolarity Polarity { get; }
    public bool IsSymmetric { get; }
    public bool IsExclusive { get; }
    public float CullThreshold { get; }

    /// <summary>Which source categories can create this relationship.</summary>
    public IReadOnlySet<EntityCategory> ValidSourceCategories { get; }

    /// <summary>Which target categories can receive this relationship.</summary>
    public IReadOnlySet<EntityCategory> ValidTargetCategories { get; }

    public RelationshipKindDefinition(
        string name,
        RelationshipPolarity polarity,
        IReadOnlySet<EntityCategory> validSourceCategories,
        IReadOnlySet<EntityCategory> validTargetCategories,
        bool isSymmetric = false,
        bool isExclusive = false,
        float cullThreshold = 0.05f)
    {
        Name = name;
        Polarity = polarity;
        ValidSourceCategories = validSourceCategories;
        ValidTargetCategories = validTargetCategories;
        IsSymmetric = isSymmetric;
        IsExclusive = isExclusive;
        CullThreshold = cullThreshold;
    }
}
