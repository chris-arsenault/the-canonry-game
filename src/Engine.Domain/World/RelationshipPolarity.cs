namespace Engine.Graph;

using Engine.Domain;

/// <summary>
/// Polarity of a relationship kind. Determines the semantic meaning of
/// strength without introducing a runtime relationship object.
/// </summary>
public enum RelationshipPolarity
{
    Positive = RelationshipPolarityCodes.Positive,
    Negative = RelationshipPolarityCodes.Negative,
    Neutral = RelationshipPolarityCodes.Neutral,
}
