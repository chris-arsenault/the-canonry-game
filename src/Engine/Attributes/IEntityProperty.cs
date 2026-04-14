namespace Engine.Attributes;

/// <summary>
/// Base interface for all attribute types on an entity.
/// Scalar attributes have a single float value.
/// Semantic plane attributes have X/Y components with labels and distance.
/// </summary>
public interface IEntityProperty
{
    string Name { get; }

    /// <summary>Apply registered tick effects. Called once per tick by MaintenanceSystem.</summary>
    void ApplyTickEffects();

    /// <summary>Remove all effects from a source across all components.</summary>
    void RemoveEffectsBySource(string sourceId);
}
