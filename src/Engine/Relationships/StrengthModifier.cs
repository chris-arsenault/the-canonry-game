namespace Engine.Relationships;

using Engine.Graph;

/// <summary>
/// A contributing factor that modifies relationship strength each tick.
/// Multiple modifiers compose additively per relationship.
/// </summary>
public interface IStrengthModifier
{
    /// <summary>Human-readable name for debugging.</summary>
    string Name { get; }

    /// <summary>
    /// Compute the per-tick strength delta for a relationship.
    /// Positive = strengthening, negative = weakening.
    /// Called each tick for every active relationship.
    /// </summary>
    float ComputeDelta(Relationship relationship, IRelationshipContext context);
}

/// <summary>
/// Context provided to strength modifiers for accessing world state.
/// </summary>
public interface IRelationshipContext
{
    Entity? GetEntity(EntityId id);
    float GetAttribute(EntityId entityId, string attributeName);
    (int Q, int R)? GetCoordinates(EntityId entityId);
    float? GetSemanticDistance(EntityId a, EntityId b, string planeName);
}
